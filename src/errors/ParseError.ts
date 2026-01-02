/**
 * Error codes for parser errors
 */
export enum ErrorCode {
  FILE_NOT_FOUND = 'FILE_NOT_FOUND',
  FILE_READ_ERROR = 'FILE_READ_ERROR',
  INVALID_LOG_FORMAT = 'INVALID_LOG_FORMAT',
  PARSE_LINE_ERROR = 'PARSE_LINE_ERROR',
  INVALID_TIMESTAMP = 'INVALID_TIMESTAMP',
  PATTERN_MATCH_ERROR = 'PATTERN_MATCH_ERROR',
}

/**
 * Reasons why a line failed to parse
 */
export enum ParseErrorReason {
  MISSING_TIMESTAMP = 'MISSING_TIMESTAMP',
  INVALID_TIMESTAMP_FORMAT = 'INVALID_TIMESTAMP_FORMAT',
  UNRECOGNIZED_FORMAT = 'UNRECOGNIZED_FORMAT',
  MALFORMED_CONTENT = 'MALFORMED_CONTENT',
}

/**
 * Base error class for all parser errors
 */
export class ParserError extends Error {
  public readonly code: ErrorCode;
  public readonly details?: unknown;

  constructor(message: string, code: ErrorCode, details?: unknown) {
    super(message);
    this.name = 'ParserError';
    this.code = code;
    this.details = details;

    // Maintains proper stack trace for where error was thrown
    if (Error.captureStackTrace) {
      Error.captureStackTrace(this, ParserError);
    }
  }
}

/**
 * Error thrown when a specific line fails to parse
 */
export class ParseLineError extends ParserError {
  public readonly lineNumber: number;
  public readonly rawLine: string;
  public readonly reason: ParseErrorReason;

  constructor(message: string, lineNumber: number, rawLine: string, reason: ParseErrorReason) {
    super(message, ErrorCode.PARSE_LINE_ERROR, { lineNumber, rawLine, reason });
    this.name = 'ParseLineError';
    this.lineNumber = lineNumber;
    this.rawLine = rawLine;
    this.reason = reason;

    if (Error.captureStackTrace) {
      Error.captureStackTrace(this, ParseLineError);
    }
  }
}

/**
 * Error thrown for file-related issues
 */
export class FileError extends ParserError {
  public readonly filePath: string;
  public readonly originalError?: Error;

  constructor(message: string, code: ErrorCode, filePath: string, originalError?: Error) {
    super(message, code, { filePath, originalError });
    this.name = 'FileError';
    this.filePath = filePath;
    this.originalError = originalError;

    if (Error.captureStackTrace) {
      Error.captureStackTrace(this, FileError);
    }
  }
}
