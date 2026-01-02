/**
 * Database-specific error classes for camelot-combat-reporter
 */

/**
 * Base error class for all database errors
 */
export class DatabaseError extends Error {
  constructor(
    message: string,
    public readonly cause?: Error
  ) {
    super(message);
    this.name = 'DatabaseError';

    // Maintain proper stack trace
    if (Error.captureStackTrace) {
      Error.captureStackTrace(this, this.constructor);
    }

    // Include cause in stack if available
    if (cause?.stack) {
      this.stack = `${this.stack}\nCaused by: ${cause.stack}`;
    }
  }
}

/**
 * Error thrown when connection to database fails
 */
export class ConnectionError extends DatabaseError {
  constructor(
    message: string,
    public readonly backend: string,
    cause?: Error
  ) {
    super(message, cause);
    this.name = 'ConnectionError';
  }
}

/**
 * Error thrown when a query fails
 */
export class QueryError extends DatabaseError {
  constructor(
    message: string,
    public readonly query?: string,
    public readonly params?: unknown[],
    cause?: Error
  ) {
    super(message, cause);
    this.name = 'QueryError';
  }
}

/**
 * Error thrown when a migration fails
 */
export class MigrationError extends DatabaseError {
  constructor(
    message: string,
    public readonly version?: number,
    public readonly migrationName?: string,
    cause?: Error
  ) {
    super(message, cause);
    this.name = 'MigrationError';
  }
}

/**
 * Error thrown when a transaction fails
 */
export class TransactionError extends DatabaseError {
  constructor(
    message: string,
    public readonly transactionId?: string,
    cause?: Error
  ) {
    super(message, cause);
    this.name = 'TransactionError';
  }
}

/**
 * Error thrown when database is not connected
 */
export class NotConnectedError extends DatabaseError {
  constructor(message = 'Database is not connected') {
    super(message);
    this.name = 'NotConnectedError';
  }
}

/**
 * Error thrown when a record is not found
 */
export class NotFoundError extends DatabaseError {
  constructor(
    public readonly table: string,
    public readonly id: string
  ) {
    super(`Record not found in ${table} with id ${id}`);
    this.name = 'NotFoundError';
  }
}

/**
 * Error thrown when a constraint is violated
 */
export class ConstraintError extends DatabaseError {
  constructor(
    message: string,
    public readonly constraint?: string,
    cause?: Error
  ) {
    super(message, cause);
    this.name = 'ConstraintError';
  }
}

/**
 * Error thrown when retention operation fails
 */
export class RetentionError extends DatabaseError {
  constructor(
    message: string,
    public readonly table?: string,
    cause?: Error
  ) {
    super(message, cause);
    this.name = 'RetentionError';
  }
}

/**
 * Error thrown when archival operation fails
 */
export class ArchivalError extends DatabaseError {
  constructor(
    message: string,
    public readonly archivePath?: string,
    cause?: Error
  ) {
    super(message, cause);
    this.name = 'ArchivalError';
  }
}

/**
 * Check if an error is a DatabaseError or subclass
 */
export function isDatabaseError(error: unknown): error is DatabaseError {
  return error instanceof DatabaseError;
}

/**
 * Wrap an unknown error in a DatabaseError if it isn't already
 */
export function wrapError(error: unknown, message?: string): DatabaseError {
  if (error instanceof DatabaseError) {
    return error;
  }

  const cause = error instanceof Error ? error : new Error(String(error));
  return new DatabaseError(message ?? cause.message, cause);
}
