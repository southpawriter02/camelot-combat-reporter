/**
 * StreamingParser - EventEmitter wrapper around LineParser
 *
 * Parses lines and emits typed events based on the parsed content.
 * Supports hierarchical event names (event:damage:dealt, event:healing, etc.)
 */
import { EventEmitter } from 'events';
import type { CombatEvent } from '../types/index.js';
import { EventType } from '../types/index.js';
import { LineParser, type LineParserOptions } from '../parser/LineParser.js';
import type { TailLine, StreamingEventType } from './types.js';

/**
 * Events emitted by StreamingParser
 */
export interface StreamingParserEvents {
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
  error: (error: Error, line: TailLine) => void;
}

/**
 * StreamingParser parses log lines and emits typed events
 */
export class StreamingParser extends EventEmitter {
  private lineParser: LineParser;
  private stats = {
    linesProcessed: 0,
    eventsEmitted: 0,
    errors: 0,
    eventsByType: {} as Record<string, number>,
  };

  constructor(options: LineParserOptions = {}) {
    super();
    this.lineParser = new LineParser(options);
  }

  /**
   * Process a line and emit appropriate events
   */
  processLine(line: TailLine): CombatEvent | null {
    this.stats.linesProcessed++;

    // Emit raw line event
    this.emit('line', line);

    // Parse the line
    const result = this.lineParser.parseLine(line.content, line.lineNumber);

    if (result.error) {
      this.stats.errors++;
      this.emit('error', result.error, line);
    }

    if (result.event) {
      this.emitEventsByType(result.event, line);
      return result.event;
    }

    return null;
  }

  /**
   * Process multiple lines
   */
  processLines(lines: TailLine[]): CombatEvent[] {
    const events: CombatEvent[] = [];
    for (const line of lines) {
      const event = this.processLine(line);
      if (event) {
        events.push(event);
      }
    }
    return events;
  }

  /**
   * Emit events based on event type hierarchy
   */
  private emitEventsByType(event: CombatEvent, line: TailLine): void {
    this.stats.eventsEmitted++;

    // Track event counts
    const typeName = event.eventType;
    this.stats.eventsByType[typeName] = (this.stats.eventsByType[typeName] || 0) + 1;

    // Always emit generic 'event'
    this.emit('event', event, line);

    // Emit specific events based on type
    switch (event.eventType) {
      case EventType.DAMAGE_DEALT:
        this.emit('event:damage', event, line);
        this.emit('event:damage:dealt', event, line);
        break;

      case EventType.DAMAGE_RECEIVED:
        this.emit('event:damage', event, line);
        this.emit('event:damage:received', event, line);
        break;

      case EventType.HEALING_DONE:
        this.emit('event:healing', event, line);
        this.emit('event:healing:done', event, line);
        break;

      case EventType.HEALING_RECEIVED:
        this.emit('event:healing', event, line);
        this.emit('event:healing:received', event, line);
        break;

      case EventType.DEATH:
        this.emit('event:death', event, line);
        break;

      case EventType.CROWD_CONTROL:
        this.emit('event:cc', event, line);
        break;

      case EventType.UNKNOWN:
        this.emit('event:unknown', event, line);
        break;
    }
  }

  /**
   * Get processing statistics
   */
  getStats(): typeof this.stats {
    return { ...this.stats };
  }

  /**
   * Reset statistics
   */
  resetStats(): void {
    this.stats = {
      linesProcessed: 0,
      eventsEmitted: 0,
      errors: 0,
      eventsByType: {},
    };
  }

  /**
   * Get the underlying LineParser for advanced use
   */
  getLineParser(): LineParser {
    return this.lineParser;
  }

  /**
   * Type-safe event emitter methods
   */
  override on<K extends keyof StreamingParserEvents>(
    event: K,
    listener: StreamingParserEvents[K]
  ): this {
    return super.on(event, listener);
  }

  override once<K extends keyof StreamingParserEvents>(
    event: K,
    listener: StreamingParserEvents[K]
  ): this {
    return super.once(event, listener);
  }

  override emit<K extends keyof StreamingParserEvents>(
    event: K,
    ...args: Parameters<StreamingParserEvents[K]>
  ): boolean {
    return super.emit(event, ...args);
  }

  override off<K extends keyof StreamingParserEvents>(
    event: K,
    listener: StreamingParserEvents[K]
  ): this {
    return super.off(event, listener);
  }
}
