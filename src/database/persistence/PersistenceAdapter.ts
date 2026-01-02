/**
 * PersistenceAdapter - Integrates database with RealTimeMonitor
 *
 * Automatically persists events and sessions from the streaming module.
 */
import { EventEmitter } from 'events';
import type {
  PersistenceConfig,
  PersistenceAdapterEvents,
  CombatEvent,
  CombatSession,
  PlayerSessionStats,
} from '../types.js';
import { DEFAULT_PERSISTENCE_CONFIG } from '../types.js';
import type { DatabaseAdapter } from '../adapters/DatabaseAdapter.js';
import { BatchWriter } from './BatchWriter.js';
import { TransactionManager } from './TransactionManager.js';

// Import types from streaming module
import type { RealTimeMonitor } from '../../streaming/RealTimeMonitor.js';
import type { SessionUpdate } from '../../analysis/types/session.js';

/**
 * PersistenceAdapter connects RealTimeMonitor to the database
 */
export class PersistenceAdapter extends EventEmitter {
  private db: DatabaseAdapter;
  private config: PersistenceConfig;
  private batchWriter: BatchWriter;
  private txManager: TransactionManager;
  private monitor: RealTimeMonitor | null = null;
  private eventCount: number = 0;
  private sessionCount: number = 0;

  constructor(db: DatabaseAdapter, config: Partial<PersistenceConfig> = {}) {
    super();
    this.db = db;
    this.config = { ...DEFAULT_PERSISTENCE_CONFIG, ...config };
    this.batchWriter = new BatchWriter(db, {
      batchSize: this.config.batchSize,
      flushIntervalMs: this.config.flushIntervalMs,
    });
    this.txManager = new TransactionManager(db);

    this.setupBatchWriter();
  }

  /**
   * Attach to a RealTimeMonitor to auto-persist events
   */
  attach(monitor: RealTimeMonitor): void {
    if (this.monitor) {
      this.detach();
    }

    this.monitor = monitor;

    if (!this.config.enabled) {
      return;
    }

    // Subscribe to events
    if (this.config.persistEvents) {
      monitor.on('event', this.handleEvent.bind(this));
    }

    if (this.config.persistSessions) {
      monitor.on('session:end', this.handleSessionEnd.bind(this));
    }

    // Track session starts for associating events
    monitor.on('session:start', this.handleSessionStart.bind(this));
  }

  /**
   * Detach from the current monitor
   */
  detach(): void {
    if (!this.monitor) {
      return;
    }

    // Remove listeners
    this.monitor.off('event', this.handleEvent.bind(this));
    this.monitor.off('session:end', this.handleSessionEnd.bind(this));
    this.monitor.off('session:start', this.handleSessionStart.bind(this));

    this.monitor = null;
  }

  /**
   * Handle incoming events
   */
  private handleEvent(event: CombatEvent): void {
    if (!this.config.persistEvents) {
      return;
    }

    this.batchWriter.add(event);
    this.eventCount++;
  }

  /**
   * Handle session start
   */
  private handleSessionStart(update: SessionUpdate): void {
    // Set session ID for batch writer
    this.batchWriter.setSessionId(update.session.id);
  }

  /**
   * Handle session end - persist session and optionally compute stats
   */
  private async handleSessionEnd(update: SessionUpdate): Promise<void> {
    if (!this.config.persistSessions) {
      return;
    }

    const session = update.session;

    try {
      if (this.config.useTransactions) {
        await this.persistSessionWithTransaction(session);
      } else {
        await this.persistSession(session);
      }

      this.sessionCount++;
      this.emit('session:persisted', session);

      // Clear session ID for new events
      this.batchWriter.setSessionId(null);
    } catch (error) {
      this.emit('error', error as Error, 'session:persist');
    }
  }

  /**
   * Persist session with transaction support
   */
  private async persistSessionWithTransaction(session: CombatSession): Promise<void> {
    await this.txManager.executeInTransaction(async (tx) => {
      // Flush any pending events first
      await this.batchWriter.flush(tx);

      // Insert session
      await this.db.insertSession(session, tx);

      // Update events with session ID
      for (const event of session.events) {
        await this.db.updateEventSession(event.id, session.id, tx);
      }

      // Insert participants
      for (const participant of session.participants) {
        await this.db.insertParticipant(session.id, participant, tx);
      }

      // Compute player stats if enabled
      if (this.config.computePlayerStats) {
        const stats = await this.computePlayerStats(session);
        for (const stat of stats) {
          await this.db.insertPlayerSessionStats(stat, tx);
          this.emit('event:persisted', stat as any);
        }
        if (stats.length > 0) {
          this.emit('stats:computed', stats);
        }
      }
    });
  }

  /**
   * Persist session without transaction
   */
  private async persistSession(session: CombatSession): Promise<void> {
    // Flush any pending events first
    await this.batchWriter.flush();

    // Insert session
    await this.db.insertSession(session);

    // Update events with session ID
    for (const event of session.events) {
      await this.db.updateEventSession(event.id, session.id);
    }

    // Insert participants
    for (const participant of session.participants) {
      await this.db.insertParticipant(session.id, participant);
    }

    // Compute player stats if enabled
    if (this.config.computePlayerStats) {
      const stats = await this.computePlayerStats(session);
      for (const stat of stats) {
        await this.db.insertPlayerSessionStats(stat);
      }
      if (stats.length > 0) {
        this.emit('stats:computed', stats);
      }
    }
  }

  /**
   * Compute player statistics for a session
   */
  private async computePlayerStats(
    session: CombatSession
  ): Promise<PlayerSessionStats[]> {
    const stats: PlayerSessionStats[] = [];

    for (const participant of session.participants) {
      if (!participant.entity.isPlayer) {
        continue;
      }

      const playerName = participant.entity.name;

      // Calculate stats from session events
      let damageDealt = 0;
      let damageTaken = 0;
      let healingDone = 0;
      let healingReceived = 0;
      let kills = 0;
      let deaths = 0;
      let criticalHits = 0;
      let totalHits = 0;

      for (const event of session.events) {
        if ('source' in event && event.source?.name === playerName) {
          if (event.eventType === 'DAMAGE_DEALT' && 'amount' in event) {
            damageDealt += event.amount as number;
            totalHits++;
            if ('isCritical' in event && event.isCritical) {
              criticalHits++;
            }
          }
          if (event.eventType === 'HEALING_DONE' && 'amount' in event) {
            healingDone += event.amount as number;
          }
        }

        if ('target' in event && event.target?.name === playerName) {
          if (event.eventType === 'DAMAGE_RECEIVED' && 'amount' in event) {
            damageTaken += event.amount as number;
          }
          if (event.eventType === 'HEALING_RECEIVED' && 'amount' in event) {
            healingReceived += event.amount as number;
          }
          if (event.eventType === 'DEATH') {
            deaths++;
          }
        }

        // Count kills (death events where this player is the killer)
        if (
          event.eventType === 'DEATH' &&
          'killer' in event &&
          event.killer?.name === playerName
        ) {
          kills++;
        }
      }

      const durationSec = session.durationMs / 1000;
      const dps = durationSec > 0 ? damageDealt / durationSec : 0;
      const hps = durationSec > 0 ? healingDone / durationSec : 0;
      const kdr = deaths > 0 ? kills / deaths : kills;
      const critRate = totalHits > 0 ? criticalHits / totalHits : 0;

      // Calculate performance score (simplified)
      const performanceScore = Math.min(
        100,
        (dps / 100) * 30 + // DPS contribution
        (hps / 50) * 20 +   // HPS contribution
        kdr * 20 +          // KDR contribution
        critRate * 30       // Crit rate contribution
      );

      const performanceRating =
        performanceScore >= 80 ? 'EXCELLENT' :
        performanceScore >= 60 ? 'GOOD' :
        performanceScore >= 40 ? 'AVERAGE' :
        performanceScore >= 20 ? 'BELOW_AVERAGE' : 'POOR';

      stats.push({
        playerName,
        sessionId: session.id,
        sessionStart: session.startTime,
        sessionEnd: session.endTime,
        durationMs: session.durationMs,
        role: participant.role,
        kills,
        deaths,
        assists: 0, // Would need more complex tracking
        kdr,
        damageDealt,
        damageTaken,
        dps,
        peakDps: dps, // Would need rolling window tracking
        healingDone,
        healingReceived,
        hps,
        overhealRate: 0, // Would need overheal tracking
        critRate,
        performanceScore,
        performanceRating,
      } as PlayerSessionStats);
    }

    return stats;
  }

  /**
   * Set up batch writer callbacks
   */
  private setupBatchWriter(): void {
    this.batchWriter.onFlush((count) => {
      this.emit('events:flushed', count);
    });

    this.batchWriter.onFlushError((error) => {
      this.emit('error', error, 'batch:flush');
    });
  }

  /**
   * Manually flush pending events
   */
  async flush(): Promise<number> {
    return this.batchWriter.flush();
  }

  /**
   * Get statistics
   */
  getStats(): { eventCount: number; sessionCount: number; pendingEvents: number } {
    return {
      eventCount: this.eventCount,
      sessionCount: this.sessionCount,
      pendingEvents: this.batchWriter.getBufferSize(),
    };
  }

  /**
   * Get current configuration
   */
  getConfig(): PersistenceConfig {
    return { ...this.config };
  }

  /**
   * Update configuration
   */
  updateConfig(config: Partial<PersistenceConfig>): void {
    this.config = { ...this.config, ...config };

    // Update batch writer config
    this.batchWriter.destroy();
    this.batchWriter = new BatchWriter(this.db, {
      batchSize: this.config.batchSize,
      flushIntervalMs: this.config.flushIntervalMs,
    });
    this.setupBatchWriter();
  }

  /**
   * Destroy the adapter
   */
  async destroy(): Promise<void> {
    this.detach();

    // Flush any pending events
    await this.batchWriter.flush().catch(() => {});

    // Rollback any pending transactions
    await this.txManager.rollbackAll();

    this.batchWriter.destroy();
    this.removeAllListeners();
  }

  // Type-safe EventEmitter overrides
  override on<K extends keyof PersistenceAdapterEvents>(
    event: K,
    listener: PersistenceAdapterEvents[K]
  ): this {
    return super.on(event, listener);
  }

  override once<K extends keyof PersistenceAdapterEvents>(
    event: K,
    listener: PersistenceAdapterEvents[K]
  ): this {
    return super.once(event, listener);
  }

  override emit<K extends keyof PersistenceAdapterEvents>(
    event: K,
    ...args: Parameters<PersistenceAdapterEvents[K]>
  ): boolean {
    return super.emit(event, ...args);
  }

  override off<K extends keyof PersistenceAdapterEvents>(
    event: K,
    listener: PersistenceAdapterEvents[K]
  ): this {
    return super.off(event, listener);
  }
}
