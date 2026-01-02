/**
 * Database module - Persistent storage for camelot-combat-reporter
 *
 * Provides:
 * - SQLite and PostgreSQL adapters with the same interface
 * - Fluent query builders for events, sessions, and stats
 * - Retention policies with archival support
 * - Integration with RealTimeMonitor for auto-persistence
 */

// Types and configuration
export type {
  DatabaseBackend,
  SQLiteConnectionConfig,
  PostgreSQLConnectionConfig,
  DatabaseConnectionConfig,
  DatabaseConfig,
  Transaction,
  PaginationOptions,
  SortOptions,
  QueryResult,
  EntityRow,
  SessionRow,
  EventRow,
  ParticipantRow,
  PlayerSessionStatsRow,
  PlayerAggregateStatsRow,
  MigrationRow,
  RetentionAction,
  RetentionTable,
  TableRetentionPolicy,
  ArchiveFormat,
  RetentionConfig,
  RetentionCleanupResult,
  PersistenceConfig,
  TimeBucket,
  TimeBucketResult,
  EntityAggregateResult,
  DatabaseAdapterEvents,
  PersistenceAdapterEvents,
  RetentionSchedulerEvents,
} from './types.js';

export {
  DEFAULT_SQLITE_CONFIG,
  DEFAULT_POSTGRESQL_CONFIG,
  DEFAULT_DATABASE_CONFIG,
  DEFAULT_RETENTION_CONFIG,
  DEFAULT_PERSISTENCE_CONFIG,
} from './types.js';

// Errors
export {
  DatabaseError,
  ConnectionError,
  QueryError,
  MigrationError,
  TransactionError,
  NotConnectedError,
  NotFoundError,
  ConstraintError,
  RetentionError,
  ArchivalError,
  isDatabaseError,
  wrapError,
} from './errors.js';

// Adapters
export {
  DatabaseAdapter,
  SQLiteAdapter,
  PostgreSQLAdapter,
} from './adapters/index.js';

export type {
  BaseQuery,
  EventQuery,
  SessionQuery,
  PlayerSessionStatsQuery,
  StatsQuery,
  AggregationQuery,
} from './adapters/index.js';

// Schema
export {
  ALL_MIGRATIONS,
  MIGRATION_001_INITIAL,
  getLatestMigrationVersion,
  getMigrationsFrom,
  getMigrationsToRollback,
  applyPrefix,
} from './schema/index.js';

export type { Migration } from './schema/migrations.js';

// Retention
export {
  validateRetentionPolicy,
  validateRetentionConfig,
  getCutoffDate,
  sortPoliciesByPriority,
  getEnabledPolicies,
  createDefaultPolicy,
  mergeRetentionConfig,
  getPolicyForTable,
  shouldRunRetention,
  ArchivalManager,
  RetentionScheduler,
} from './retention/index.js';

export type {
  ArchiveResult,
  ArchiveMetadata,
} from './retention/index.js';

// Persistence
export {
  BatchWriter,
  DEFAULT_BATCH_WRITER_CONFIG,
  TransactionManager,
  PersistenceAdapter,
} from './persistence/index.js';

export type {
  BatchWriterConfig,
  TransactionState,
  ManagedTransaction,
} from './persistence/index.js';
