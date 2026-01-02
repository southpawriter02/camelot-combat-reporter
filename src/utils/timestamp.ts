import { ParseLineError, ParseErrorReason } from '../errors/index.js';

/**
 * Regex pattern for DAoC timestamp format [HH:MM:SS]
 */
const TIMESTAMP_PATTERN = /^\[(\d{2}):(\d{2}):(\d{2})\]/;

/**
 * Result of extracting a timestamp from a log line
 */
export interface TimestampResult {
  /** Parsed Date object */
  timestamp: Date;
  /** Original timestamp string including brackets */
  rawTimestamp: string;
  /** The message content after the timestamp */
  message: string;
}

/**
 * Extracts and parses a timestamp from a DAoC log line
 *
 * @param line - The raw log line
 * @param lineNumber - Line number for error reporting
 * @param baseDate - Optional base date for the timestamp (defaults to today)
 * @returns TimestampResult with parsed timestamp and remaining message
 * @throws ParseLineError if timestamp is missing or invalid
 */
export function extractTimestamp(
  line: string,
  lineNumber: number,
  baseDate?: Date
): TimestampResult {
  const match = TIMESTAMP_PATTERN.exec(line);

  if (!match) {
    throw new ParseLineError(
      `Missing timestamp in line ${lineNumber}: "${line.substring(0, 50)}..."`,
      lineNumber,
      line,
      ParseErrorReason.MISSING_TIMESTAMP
    );
  }

  const [fullMatch, hoursStr, minutesStr, secondsStr] = match;

  const hours = parseInt(hoursStr!, 10);
  const minutes = parseInt(minutesStr!, 10);
  const seconds = parseInt(secondsStr!, 10);

  // Validate time values
  if (hours > 23 || minutes > 59 || seconds > 59) {
    throw new ParseLineError(
      `Invalid timestamp values in line ${lineNumber}: ${fullMatch}`,
      lineNumber,
      line,
      ParseErrorReason.INVALID_TIMESTAMP_FORMAT
    );
  }

  // Create timestamp using base date or today
  const base = baseDate ?? new Date();
  const timestamp = new Date(
    base.getFullYear(),
    base.getMonth(),
    base.getDate(),
    hours,
    minutes,
    seconds
  );

  // Extract the message (everything after the timestamp and space)
  const messageStart = fullMatch!.length;
  const message = line.substring(messageStart).trim();

  return {
    timestamp,
    rawTimestamp: fullMatch!,
    message,
  };
}

/**
 * Checks if a line has a valid timestamp format
 *
 * @param line - The log line to check
 * @returns true if the line starts with a valid timestamp
 */
export function hasValidTimestamp(line: string): boolean {
  return TIMESTAMP_PATTERN.test(line);
}
