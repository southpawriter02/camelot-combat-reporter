import * as path from 'path';
import type {
  CombatEvent,
  ParsedLog,
  LogMetadata,
  ParserConfig,
} from '../types/index.js';
import { EventType } from '../types/index.js';
import { LogFileReader } from '../file/index.js';
import { LineParser } from './LineParser.js';
import { ParseLineError } from '../errors/index.js';

/**
 * Default parser configuration
 */
const DEFAULT_CONFIG: ParserConfig = {
  continueOnError: true,
  maxErrors: 0,
  includeUnknownEvents: true,
};

/**
 * Main log parser that orchestrates file reading and line parsing
 */
export class LogParser {
  private config: ParserConfig;
  private lineParser: LineParser;
  private fileReader: LogFileReader;

  constructor(config: Partial<ParserConfig> = {}) {
    this.config = { ...DEFAULT_CONFIG, ...config };
    this.lineParser = new LineParser();
    this.fileReader = new LogFileReader();
  }

  /**
   * Parse an entire log file
   *
   * @param filePath - Path to the log file
   * @returns ParsedLog with all events and metadata
   */
  async parseFile(filePath: string): Promise<ParsedLog> {
    const parseStartTime = new Date();
    const events: CombatEvent[] = [];
    const errors: Error[] = [];
    let totalLines = 0;
    let parsedLines = 0;
    let errorLines = 0;

    // Stream the file line by line
    for await (const { content, lineNumber } of this.fileReader.streamLines(filePath)) {
      totalLines++;

      // Skip empty lines
      if (!content.trim()) {
        continue;
      }

      const result = this.lineParser.parseLine(content, lineNumber);

      if (result.success) {
        parsedLines++;

        // Add event to results
        if (result.event) {
          // Check if we should include unknown events
          if (
            result.event.eventType === EventType.UNKNOWN &&
            !this.config.includeUnknownEvents
          ) {
            continue;
          }
          events.push(result.event);
        }
      } else {
        errorLines++;

        if (result.error) {
          errors.push(result.error);
          this.config.onError?.(result.error);
        }

        // Check if we've hit max errors
        if (this.config.maxErrors > 0 && errors.length >= this.config.maxErrors) {
          if (!this.config.continueOnError) {
            break;
          }
        }

        // Still include the unknown event from failed parses
        if (result.event && this.config.includeUnknownEvents) {
          events.push(result.event);
        }
      }
    }

    const parseEndTime = new Date();
    const metadata = this.calculateMetadata(events);

    return {
      filename: path.basename(filePath),
      filePath,
      parseStartTime,
      parseEndTime,
      totalLines,
      parsedLines,
      errorLines,
      events,
      errors,
      metadata,
    };
  }

  /**
   * Parse a single line
   *
   * @param line - The log line to parse
   * @param lineNumber - Optional line number
   * @returns The parsed event or null
   */
  parseLine(line: string, lineNumber = 1): CombatEvent | null {
    const result = this.lineParser.parseLine(line, lineNumber);
    return result.event ?? null;
  }

  /**
   * Parse multiple lines
   *
   * @param lines - Array of log lines
   * @returns Array of parsed events
   */
  parseLines(lines: string[]): CombatEvent[] {
    const events: CombatEvent[] = [];

    for (let i = 0; i < lines.length; i++) {
      const line = lines[i];
      if (!line?.trim()) continue;

      const result = this.lineParser.parseLine(line, i + 1);

      if (result.event) {
        if (
          result.event.eventType === EventType.UNKNOWN &&
          !this.config.includeUnknownEvents
        ) {
          continue;
        }
        events.push(result.event);
      }
    }

    return events;
  }

  /**
   * Calculate metadata from parsed events
   */
  private calculateMetadata(events: CombatEvent[]): LogMetadata {
    const eventTypeCounts: Record<string, number> = {};
    const uniqueEntities = new Set<string>();
    let firstEventTime: Date | undefined;
    let lastEventTime: Date | undefined;

    for (const event of events) {
      // Count event types
      eventTypeCounts[event.eventType] = (eventTypeCounts[event.eventType] ?? 0) + 1;

      // Track timestamps
      if (!firstEventTime || event.timestamp < firstEventTime) {
        firstEventTime = event.timestamp;
      }
      if (!lastEventTime || event.timestamp > lastEventTime) {
        lastEventTime = event.timestamp;
      }

      // Track unique entities
      this.extractEntitiesFromEvent(event, uniqueEntities);
    }

    const logDuration =
      firstEventTime && lastEventTime
        ? lastEventTime.getTime() - firstEventTime.getTime()
        : undefined;

    return {
      firstEventTime,
      lastEventTime,
      logDuration,
      uniqueEntities: Array.from(uniqueEntities),
      eventTypeCounts,
    };
  }

  /**
   * Extract entity names from an event
   */
  private extractEntitiesFromEvent(event: CombatEvent, entities: Set<string>): void {
    // Handle different event types
    if ('source' in event && event.source) {
      entities.add(event.source.name);
    }
    if ('target' in event && event.target) {
      entities.add(event.target.name);
    }
    if ('killer' in event && event.killer) {
      entities.add(event.killer.name);
    }
  }

  /**
   * Get the internal line parser (for testing/extension)
   */
  getLineParser(): LineParser {
    return this.lineParser;
  }

  /**
   * Update parser configuration
   */
  updateConfig(config: Partial<ParserConfig>): void {
    this.config = { ...this.config, ...config };
  }
}
