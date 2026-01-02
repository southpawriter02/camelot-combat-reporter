/**
 * BatchWriter - Buffers events for batch insertion
 */
import type { CombatEvent, Transaction } from '../types.js';
import type { DatabaseAdapter } from '../adapters/DatabaseAdapter.js';

/**
 * Configuration for BatchWriter
 */
export interface BatchWriterConfig {
  /** Maximum batch size before auto-flush (default: 100) */
  batchSize: number;
  /** Flush interval in ms (default: 1000) */
  flushIntervalMs: number;
}

/**
 * Default BatchWriter configuration
 */
export const DEFAULT_BATCH_WRITER_CONFIG: BatchWriterConfig = {
  batchSize: 100,
  flushIntervalMs: 1000,
};

/**
 * BatchWriter buffers events and writes them in batches for better performance
 */
export class BatchWriter {
  private db: DatabaseAdapter;
  private config: BatchWriterConfig;
  private buffer: CombatEvent[] = [];
  private sessionId: string | null = null;
  private flushTimer: NodeJS.Timeout | null = null;
  private onFlushed: ((count: number) => void) | null = null;
  private onError: ((error: Error) => void) | null = null;

  constructor(db: DatabaseAdapter, config: Partial<BatchWriterConfig> = {}) {
    this.db = db;
    this.config = { ...DEFAULT_BATCH_WRITER_CONFIG, ...config };
  }

  /**
   * Add an event to the buffer
   */
  add(event: CombatEvent): void {
    this.buffer.push(event);

    // Start flush timer if not already running
    if (!this.flushTimer && this.config.flushIntervalMs > 0) {
      this.startFlushTimer();
    }

    // Auto-flush if buffer is full
    if (this.buffer.length >= this.config.batchSize) {
      this.flush().catch((error) => {
        this.onError?.(error);
      });
    }
  }

  /**
   * Add multiple events to the buffer
   */
  addAll(events: CombatEvent[]): void {
    for (const event of events) {
      this.add(event);
    }
  }

  /**
   * Flush the buffer to the database
   */
  async flush(tx?: Transaction): Promise<number> {
    if (this.buffer.length === 0) {
      return 0;
    }

    this.stopFlushTimer();

    const eventsToFlush = [...this.buffer];
    this.buffer = [];

    try {
      await this.db.insertEvents(eventsToFlush, this.sessionId ?? undefined, tx);
      this.onFlushed?.(eventsToFlush.length);
      return eventsToFlush.length;
    } catch (error) {
      // Put events back in buffer on failure
      this.buffer = [...eventsToFlush, ...this.buffer];
      throw error;
    }
  }

  /**
   * Set the session ID for subsequent events
   */
  setSessionId(sessionId: string | null): void {
    this.sessionId = sessionId;
  }

  /**
   * Get the current session ID
   */
  getSessionId(): string | null {
    return this.sessionId;
  }

  /**
   * Get the current buffer size
   */
  getBufferSize(): number {
    return this.buffer.length;
  }

  /**
   * Check if buffer has pending events
   */
  hasPending(): boolean {
    return this.buffer.length > 0;
  }

  /**
   * Clear the buffer without flushing
   */
  clear(): void {
    this.buffer = [];
    this.stopFlushTimer();
  }

  /**
   * Set callback for when events are flushed
   */
  onFlush(callback: (count: number) => void): void {
    this.onFlushed = callback;
  }

  /**
   * Set callback for errors
   */
  onFlushError(callback: (error: Error) => void): void {
    this.onError = callback;
  }

  /**
   * Start the auto-flush timer
   */
  private startFlushTimer(): void {
    if (this.flushTimer) {
      return;
    }

    this.flushTimer = setInterval(() => {
      if (this.buffer.length > 0) {
        this.flush().catch((error) => {
          this.onError?.(error);
        });
      }
    }, this.config.flushIntervalMs);
  }

  /**
   * Stop the auto-flush timer
   */
  private stopFlushTimer(): void {
    if (this.flushTimer) {
      clearInterval(this.flushTimer);
      this.flushTimer = null;
    }
  }

  /**
   * Destroy the batch writer
   */
  destroy(): void {
    this.stopFlushTimer();
    this.buffer = [];
    this.onFlushed = null;
    this.onError = null;
  }
}
