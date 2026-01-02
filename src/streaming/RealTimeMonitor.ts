/**
 * RealTimeMonitor - Main orchestrator for real-time log monitoring
 *
 * Combines FileWatcher, LogTailer, StreamingParser, StreamingSessionDetector,
 * and WebhookNotifier into a unified interface for monitoring combat logs.
 */
import { EventEmitter } from 'events';
import * as path from 'path';
import type { CombatEvent } from '../types/index.js';
import type { SessionUpdate } from '../analysis/types/session.js';
import type {
  MonitorState,
  MonitorStatus,
  MonitorStats,
  MonitorStartOptions,
  RealTimeMonitorConfig,
  TailPosition,
  TailLine,
  WebhookConfig,
  WebhookPayload,
  FileChangeEvent,
} from './types.js';
import { DEFAULT_MONITOR_CONFIG } from './types.js';
import { FileWatcher } from './FileWatcher.js';
import { LogTailer } from './LogTailer.js';
import { StreamingParser } from './StreamingParser.js';
import { StreamingSessionDetector } from './StreamingSessionDetector.js';
import { WebhookNotifier } from './WebhookNotifier.js';

/**
 * Events emitted by RealTimeMonitor
 */
export interface RealTimeMonitorEvents {
  // Line & Event (forwarded from StreamingParser)
  line: (line: TailLine) => void;
  event: (event: CombatEvent, line: TailLine) => void;
  'event:damage': (event: CombatEvent, line: TailLine) => void;
  'event:damage:dealt': (event: CombatEvent, line: TailLine) => void;
  'event:damage:received': (event: CombatEvent, line: TailLine) => void;
  'event:healing': (event: CombatEvent, line: TailLine) => void;
  'event:healing:done': (event: CombatEvent, line: TailLine) => void;
  'event:healing:received': (event: CombatEvent, line: TailLine) => void;
  'event:death': (event: CombatEvent, line: TailLine) => void;
  'event:cc': (event: CombatEvent, line: TailLine) => void;
  'event:unknown': (event: CombatEvent, line: TailLine) => void;

  // Session (forwarded from StreamingSessionDetector)
  'session:start': (update: SessionUpdate) => void;
  'session:update': (update: SessionUpdate) => void;
  'session:end': (update: SessionUpdate) => void;

  // File (forwarded from FileWatcher)
  'file:change': (event: FileChangeEvent) => void;
  'file:rotate': (event: FileChangeEvent) => void;

  // Monitor state
  'monitor:started': (filename: string) => void;
  'monitor:stopped': () => void;
  'monitor:paused': () => void;
  'monitor:resumed': () => void;
  'monitor:error': (error: Error) => void;

  // Generic error
  error: (error: Error) => void;
}

/**
 * RealTimeMonitor orchestrates real-time log monitoring
 */
export class RealTimeMonitor extends EventEmitter {
  private config: RealTimeMonitorConfig;
  private state: MonitorState = 'stopped';
  private filename: string | null = null;

  // Components
  private fileWatcher: FileWatcher;
  private logTailer: LogTailer;
  private streamingParser: StreamingParser;
  private sessionDetector: StreamingSessionDetector;
  private webhookNotifier: WebhookNotifier;

  // Processing state
  private processLoopActive: boolean = false;
  private lastError: Error | null = null;

  // Stats
  private startedAt: Date | null = null;
  private webhooksDelivered: number = 0;
  private webhooksFailed: number = 0;

  constructor(config: Partial<RealTimeMonitorConfig> = {}) {
    super();
    this.config = { ...DEFAULT_MONITOR_CONFIG, ...config };

    // Initialize components
    this.fileWatcher = new FileWatcher(this.config.fileWatcher);
    this.logTailer = new LogTailer(this.config.logTailer);
    this.streamingParser = new StreamingParser();
    this.sessionDetector = new StreamingSessionDetector(
      this.config.sessionDetector
    );
    this.webhookNotifier = new WebhookNotifier();

    this.setupEventForwarding();
  }

  /**
   * Start monitoring a file
   */
  async start(
    filename: string,
    options: MonitorStartOptions = {}
  ): Promise<void> {
    if (this.state !== 'stopped') {
      throw new Error(`Cannot start: monitor is ${this.state}`);
    }

    this.state = 'starting';
    this.filename = path.resolve(filename);
    this.lastError = null;

    try {
      // Configure tailer based on options
      if (options.resumeFrom) {
        this.logTailer = new LogTailer({
          ...this.config.logTailer,
          startPosition: options.resumeFrom,
        });
      } else if (options.fromBeginning) {
        this.logTailer = new LogTailer({
          ...this.config.logTailer,
          fromBeginning: true,
        });
      }

      // Open file for tailing
      await this.logTailer.open(this.filename);

      // Start file watcher
      await this.fileWatcher.start(this.filename);

      this.state = 'running';
      this.startedAt = new Date();
      this.processLoopActive = true;

      this.emit('monitor:started', this.filename);

      // Start processing loop
      this.processLoop();
    } catch (error) {
      this.state = 'error';
      this.lastError = error as Error;
      this.emit('monitor:error', error as Error);
      this.emit('error', error as Error);
      throw error;
    }
  }

  /**
   * Stop monitoring
   */
  async stop(): Promise<void> {
    if (this.state === 'stopped') {
      return;
    }

    this.state = 'stopping';
    this.processLoopActive = false;

    // End any active session
    this.sessionDetector.forceEndSession();

    // Stop components
    await this.fileWatcher.stop();
    await this.logTailer.close();

    this.state = 'stopped';
    this.filename = null;
    this.startedAt = null;

    this.emit('monitor:stopped');
  }

  /**
   * Pause monitoring (keeps file open but stops processing)
   */
  pause(): void {
    if (this.state !== 'running') {
      throw new Error(`Cannot pause: monitor is ${this.state}`);
    }

    this.processLoopActive = false;
    this.state = 'paused';
    this.emit('monitor:paused');
  }

  /**
   * Resume monitoring after pause
   */
  resume(): void {
    if (this.state !== 'paused') {
      throw new Error(`Cannot resume: monitor is ${this.state}`);
    }

    this.state = 'running';
    this.processLoopActive = true;
    this.emit('monitor:resumed');

    // Restart processing loop
    this.processLoop();
  }

  /**
   * Get current position for saving
   */
  getPosition(): TailPosition {
    return this.logTailer.getPosition();
  }

  /**
   * Get monitor status
   */
  getStatus(): MonitorStatus {
    const parserStats = this.streamingParser.getStats();
    const activeSession = this.sessionDetector.getActiveSession();

    const stats: MonitorStats = {
      linesProcessed: parserStats.linesProcessed,
      eventsEmitted: parserStats.eventsEmitted,
      eventsByType: parserStats.eventsByType,
      sessionsDetected: 0, // Would need to track this
      webhooksDelivered: this.webhooksDelivered,
      webhooksFailed: this.webhooksFailed,
      startedAt: this.startedAt ?? undefined,
      runtimeMs: this.startedAt
        ? Date.now() - this.startedAt.getTime()
        : 0,
    };

    return {
      state: this.state,
      filename: this.filename ?? undefined,
      position: this.logTailer.getPosition(),
      activeSession: activeSession
        ? {
            id: activeSession.id,
            eventCount: activeSession.events.length,
            durationMs:
              activeSession.lastEventTime.getTime() -
              activeSession.startTime.getTime(),
          }
        : undefined,
      stats,
      lastError: this.lastError?.message,
    };
  }

  /**
   * Add a webhook
   */
  addWebhook(config: Partial<WebhookConfig> & { url: string }): string {
    return this.webhookNotifier.addWebhook(config);
  }

  /**
   * Remove a webhook
   */
  removeWebhook(id: string): boolean {
    return this.webhookNotifier.removeWebhook(id);
  }

  /**
   * Get all webhooks
   */
  getWebhooks(): WebhookConfig[] {
    return this.webhookNotifier.getWebhooks();
  }

  /**
   * Get dead letter queue
   */
  getDeadLetterQueue() {
    return this.webhookNotifier.getDeadLetterQueue();
  }

  /**
   * Main processing loop
   */
  private async processLoop(): Promise<void> {
    while (this.processLoopActive && this.state === 'running') {
      try {
        // Read new lines
        const lines = await this.logTailer.readNewLines();

        // Process each line
        for (const line of lines) {
          const event = this.streamingParser.processLine(line);

          if (event) {
            // Feed to session detector
            this.sessionDetector.processEvent(event);
          }
        }

        // Small delay to prevent busy-waiting
        await this.sleep(50);
      } catch (error) {
        this.lastError = error as Error;
        this.emit('error', error as Error);

        // Don't crash the loop on transient errors
        await this.sleep(1000);
      }
    }
  }

  /**
   * Set up event forwarding from components
   */
  private setupEventForwarding(): void {
    // Forward file watcher events
    this.fileWatcher.on('change', (event) => {
      this.emit('file:change', event);
    });

    this.fileWatcher.on('rotate', async (event) => {
      this.emit('file:rotate', event);

      // Handle rotation if configured
      if (this.config.followRotation) {
        // End current session
        this.sessionDetector.forceEndSession();

        // Reopen file from beginning
        try {
          await this.logTailer.handleRotation();
        } catch (error) {
          this.emit('error', error as Error);
        }
      }
    });

    this.fileWatcher.on('error', (error) => {
      this.emit('error', error);
    });

    // Forward streaming parser events
    this.streamingParser.on('line', (line) => {
      this.emit('line', line);
    });

    this.streamingParser.on('event', (event, line) => {
      this.emit('event', event, line);
      this.sendWebhook('event', event);
    });

    this.streamingParser.on('event:damage', (event, line) => {
      this.emit('event:damage', event, line);
    });

    this.streamingParser.on('event:damage:dealt', (event, line) => {
      this.emit('event:damage:dealt', event, line);
      this.sendWebhook('event:damage:dealt', event);
    });

    this.streamingParser.on('event:damage:received', (event, line) => {
      this.emit('event:damage:received', event, line);
      this.sendWebhook('event:damage:received', event);
    });

    this.streamingParser.on('event:healing', (event, line) => {
      this.emit('event:healing', event, line);
    });

    this.streamingParser.on('event:healing:done', (event, line) => {
      this.emit('event:healing:done', event, line);
      this.sendWebhook('event:healing:done', event);
    });

    this.streamingParser.on('event:healing:received', (event, line) => {
      this.emit('event:healing:received', event, line);
      this.sendWebhook('event:healing:received', event);
    });

    this.streamingParser.on('event:death', (event, line) => {
      this.emit('event:death', event, line);
      this.sendWebhook('event:death', event);
    });

    this.streamingParser.on('event:cc', (event, line) => {
      this.emit('event:cc', event, line);
      this.sendWebhook('event:cc', event);
    });

    this.streamingParser.on('event:unknown', (event, line) => {
      this.emit('event:unknown', event, line);
    });

    this.streamingParser.on('error', (error, line) => {
      this.emit('error', error);
    });

    // Forward session detector events
    this.sessionDetector.on('session:start', (update) => {
      this.emit('session:start', update);
      this.sendWebhook('session:start', update);
    });

    this.sessionDetector.on('session:update', (update) => {
      this.emit('session:update', update);
      // Don't webhook every update - too noisy
    });

    this.sessionDetector.on('session:end', (update) => {
      this.emit('session:end', update);
      this.sendWebhook('session:end', update);
    });

    // Track webhook stats
    this.webhookNotifier.on('delivered', () => {
      this.webhooksDelivered++;
    });

    this.webhookNotifier.on('failed', () => {
      this.webhooksFailed++;
    });
  }

  /**
   * Send webhook notification
   */
  private sendWebhook(
    eventType: string,
    data: CombatEvent | SessionUpdate
  ): void {
    if (this.webhookNotifier.getWebhooks().length === 0) {
      return;
    }

    const payload: WebhookPayload = {
      eventType,
      timestamp: new Date().toISOString(),
      data,
      metadata: {
        filename: this.filename || 'unknown',
        lineNumber: 'lineNumber' in data ? (data as CombatEvent).lineNumber : undefined,
        sessionId:
          'session' in data ? (data as SessionUpdate).session.id : undefined,
      },
    };

    // Fire and forget - don't await
    this.webhookNotifier.notify(payload).catch((error) => {
      this.emit('error', error);
    });
  }

  /**
   * Helper to sleep
   */
  private sleep(ms: number): Promise<void> {
    return new Promise((resolve) => setTimeout(resolve, ms));
  }

  /**
   * Clean up resources
   */
  async destroy(): Promise<void> {
    await this.stop();
    this.sessionDetector.destroy();
    this.webhookNotifier.destroy();
    this.removeAllListeners();
  }

  /**
   * Type-safe event emitter methods
   */
  override on<K extends keyof RealTimeMonitorEvents>(
    event: K,
    listener: RealTimeMonitorEvents[K]
  ): this {
    return super.on(event, listener);
  }

  override once<K extends keyof RealTimeMonitorEvents>(
    event: K,
    listener: RealTimeMonitorEvents[K]
  ): this {
    return super.once(event, listener);
  }

  override emit<K extends keyof RealTimeMonitorEvents>(
    event: K,
    ...args: Parameters<RealTimeMonitorEvents[K]>
  ): boolean {
    return super.emit(event, ...args);
  }

  override off<K extends keyof RealTimeMonitorEvents>(
    event: K,
    listener: RealTimeMonitorEvents[K]
  ): this {
    return super.off(event, listener);
  }
}
