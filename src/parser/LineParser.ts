import { v4 as uuidv4 } from 'uuid';
import type { CombatEvent, UnknownEvent } from '../types/index.js';
import { EventType } from '../types/index.js';
import { extractTimestamp, hasValidTimestamp } from '../utils/index.js';
import { ParseLineError, ParseErrorReason } from '../errors/index.js';
import {
  PatternRegistry,
  DamagePatternHandler,
  HealingPatternHandler,
  CrowdControlPatternHandler,
} from './patterns/index.js';

/**
 * Result of parsing a single line
 */
export interface LineParseResult {
  /** Whether parsing succeeded */
  success: boolean;
  /** The parsed event (if successful or unknown) */
  event?: CombatEvent;
  /** Error details (if failed) */
  error?: ParseLineError;
}

/**
 * Options for the LineParser
 */
export interface LineParserOptions {
  /** Base date for timestamps (defaults to today) */
  baseDate?: Date;
}

/**
 * Parser that handles individual log lines
 */
export class LineParser {
  private registry: PatternRegistry;
  private baseDate?: Date;

  constructor(options: LineParserOptions = {}) {
    this.baseDate = options.baseDate;
    this.registry = new PatternRegistry();

    // Register all pattern handlers
    this.registry.register(new DamagePatternHandler());
    this.registry.register(new HealingPatternHandler());
    this.registry.register(new CrowdControlPatternHandler());
  }

  /**
   * Parse a single log line
   *
   * @param line - The raw log line
   * @param lineNumber - Line number for error reporting
   * @returns LineParseResult with the parsed event or error
   */
  parseLine(line: string, lineNumber: number): LineParseResult {
    // Skip empty lines
    if (!line.trim()) {
      return {
        success: true,
        event: this.createUnknownEvent(line, lineNumber, '', '[00:00:00]'),
      };
    }

    // Check if line has a valid timestamp
    if (!hasValidTimestamp(line)) {
      return {
        success: false,
        error: new ParseLineError(
          `Missing timestamp in line ${lineNumber}`,
          lineNumber,
          line,
          ParseErrorReason.MISSING_TIMESTAMP
        ),
        event: this.createUnknownEvent(line, lineNumber, line, ''),
      };
    }

    try {
      // Extract timestamp and message
      const { timestamp, rawTimestamp, message } = extractTimestamp(
        line,
        lineNumber,
        this.baseDate
      );

      // Find a handler that can parse this message
      const handler = this.registry.findHandler(message);

      if (handler) {
        const event = handler.parse(message, timestamp, rawTimestamp, line, lineNumber);
        if (event) {
          return { success: true, event };
        }
      }

      // No handler matched - return unknown event
      return {
        success: true,
        event: this.createUnknownEvent(line, lineNumber, message, rawTimestamp, timestamp),
      };
    } catch (error) {
      if (error instanceof ParseLineError) {
        return {
          success: false,
          error,
          event: this.createUnknownEvent(line, lineNumber, line, ''),
        };
      }
      throw error;
    }
  }

  /**
   * Create an unknown event for lines that couldn't be parsed
   */
  private createUnknownEvent(
    rawLine: string,
    lineNumber: number,
    _message: string,
    rawTimestamp: string,
    timestamp?: Date
  ): UnknownEvent {
    return {
      id: uuidv4(),
      timestamp: timestamp ?? new Date(),
      rawTimestamp,
      rawLine,
      lineNumber,
      eventType: EventType.UNKNOWN,
    };
  }

  /**
   * Get the pattern registry for testing/extension
   */
  getRegistry(): PatternRegistry {
    return this.registry;
  }
}
