/**
 * Database types and configuration for camelot-combat-reporter
 */

import type { CombatEvent, Entity, EventType, ActionType, DamageType } from '../types/index.js';
import type { CombatSession, SessionParticipant, ParticipantRole, SessionSummary } from '../analysis/types/session.js';
import type {
  PlayerSessionStats,
  PlayerAggregateStats,
  PerformanceRating,
  ConsistencyRating,
  TrendPoint,
} from '../analysis/player-stats/types.js';

// Re-export types needed by consumers
export type {
  CombatEvent,
  Entity,
  EventType,
  ActionType,
  DamageType,
  CombatSession,
  SessionParticipant,
  ParticipantRole,
  SessionSummary,
  PlayerSessionStats,
  PlayerAggregateStats,
  PerformanceRating,
  ConsistencyRating,
  TrendPoint,
};

// ============================================================================
// Database Backend Types
// ============================================================================

/**
 * Supported database backends
 */
export type DatabaseBackend = 'sqlite' | 'postgresql';

/**
 * Connection configuration for SQLite
 */
export interface SQLiteConnectionConfig {
  backend: 'sqlite';
  /** Path to database file */
  filepath: string;
  /** Enable WAL mode for better concurrency (default: true) */
  walMode?: boolean;
  /** Connection timeout in ms (default: 5000) */
  timeout?: number;
}

/**
 * Connection configuration for PostgreSQL
 */
export interface PostgreSQLConnectionConfig {
  backend: 'postgresql';
  /** Connection string (alternative to individual parts) */
  connectionString?: string;
  /** Host (default: localhost) */
  host?: string;
  /** Port (default: 5432) */
  port?: number;
  /** Database name */
  database?: string;
  /** Username */
  user?: string;
  /** Password */
  password?: string;
  /** Connection pool settings */
  pool?: {
    /** Minimum pool size (default: 2) */
    min?: number;
    /** Maximum pool size (default: 10) */
    max?: number;
    /** Idle timeout in ms (default: 30000) */
    idleTimeoutMs?: number;
  };
  /** SSL configuration */
  ssl?: boolean | object;
}

/**
 * Union type for connection configuration
 */
export type DatabaseConnectionConfig = SQLiteConnectionConfig | PostgreSQLConnectionConfig;

// ============================================================================
// Database Configuration
// ============================================================================

/**
 * Full database configuration
 */
export interface DatabaseConfig {
  /** Connection configuration */
  connection: DatabaseConnectionConfig;
  /** Auto-run migrations on connect (default: true) */
  autoMigrate: boolean;
  /** Table name prefix (default: 'ccr_') */
  tablePrefix: string;
  /** Enable query logging (default: false) */
  logging: boolean;
  /** Retention policy configuration */
  retention?: RetentionConfig;
}

/**
 * Default SQLite connection configuration
 */
export const DEFAULT_SQLITE_CONFIG: SQLiteConnectionConfig = {
  backend: 'sqlite',
  filepath: './combat-reporter.db',
  walMode: true,
  timeout: 5000,
};

/**
 * Default PostgreSQL connection configuration
 */
export const DEFAULT_POSTGRESQL_CONFIG: Omit<PostgreSQLConnectionConfig, 'database' | 'user' | 'password'> = {
  backend: 'postgresql',
  host: 'localhost',
  port: 5432,
  pool: {
    min: 2,
    max: 10,
    idleTimeoutMs: 30000,
  },
};

/**
 * Default database configuration (excluding connection)
 */
export const DEFAULT_DATABASE_CONFIG: Omit<DatabaseConfig, 'connection'> = {
  autoMigrate: true,
  tablePrefix: 'ccr_',
  logging: false,
};

// ============================================================================
// Transaction Types
// ============================================================================

/**
 * Transaction handle for atomic operations
 */
export interface Transaction {
  /** Unique transaction ID */
  id: string;
  /** Commit the transaction */
  commit(): Promise<void>;
  /** Rollback the transaction */
  rollback(): Promise<void>;
  /** Whether the transaction is still active */
  isActive(): boolean;
}

// ============================================================================
// Query Types
// ============================================================================

/**
 * Pagination options for queries
 */
export interface PaginationOptions {
  /** Maximum number of results */
  limit?: number;
  /** Number of results to skip */
  offset?: number;
}

/**
 * Sort options for queries
 */
export interface SortOptions {
  /** Field to sort by */
  field: string;
  /** Sort direction */
  direction: 'asc' | 'desc';
}

/**
 * Base query result with pagination info
 */
export interface QueryResult<T> {
  /** The result data */
  data: T[];
  /** Total count (if available) */
  total?: number;
  /** Whether there are more results */
  hasMore?: boolean;
}

// ============================================================================
// Row Types (Database Records)
// ============================================================================

/**
 * Entity row in the database
 */
export interface EntityRow {
  id: string;
  name: string;
  entity_type: string;
  realm: string | null;
  is_player: boolean;
  is_self: boolean;
  first_seen: Date;
  last_seen: Date;
  created_at: Date;
}

/**
 * Session row in the database
 */
export interface SessionRow {
  id: string;
  start_time: Date;
  end_time: Date;
  duration_ms: number;
  /** JSON blob for SessionSummary */
  summary_data: string;
  created_at: Date;
}

/**
 * Event row in the database
 */
export interface EventRow {
  id: string;
  session_id: string | null;
  event_type: string;
  timestamp: Date;
  raw_timestamp: string;
  raw_line: string;
  line_number: number;
  source_entity_id: string | null;
  target_entity_id: string | null;
  /** JSON blob for type-specific fields */
  event_data: string;
  created_at: Date;
}

/**
 * Participant row in the database
 */
export interface ParticipantRow {
  id: string;
  session_id: string;
  entity_id: string;
  role: string;
  first_seen: Date;
  last_seen: Date;
  event_count: number;
  created_at: Date;
}

/**
 * Player session stats row in the database
 */
export interface PlayerSessionStatsRow {
  id: string;
  player_name: string;
  session_id: string;
  session_start: Date;
  session_end: Date;
  duration_ms: number;
  role: string;
  kills: number;
  deaths: number;
  assists: number;
  kdr: number;
  damage_dealt: number;
  damage_taken: number;
  dps: number;
  peak_dps: number;
  healing_done: number;
  healing_received: number;
  hps: number;
  overheal_rate: number;
  crit_rate: number;
  performance_score: number;
  performance_rating: string;
  created_at: Date;
}

/**
 * Player aggregate stats row in the database
 */
export interface PlayerAggregateStatsRow {
  id: string;
  player_name: string;
  total_sessions: number;
  total_combat_time_ms: number;
  total_kills: number;
  total_deaths: number;
  total_assists: number;
  overall_kdr: number;
  total_damage_dealt: number;
  total_damage_taken: number;
  total_healing_done: number;
  total_healing_received: number;
  avg_dps: number;
  avg_hps: number;
  avg_performance_score: number;
  performance_variance: number;
  consistency_rating: string;
  /** JSON blob for additional data (trends, distribution, best/worst) */
  stats_data: string;
  created_at: Date;
  updated_at: Date;
}

/**
 * Migration row in the database
 */
export interface MigrationRow {
  version: number;
  name: string;
  applied_at: Date;
}

// ============================================================================
// Retention Policy Types
// ============================================================================

/**
 * Retention action to take when limit is reached
 */
export type RetentionAction = 'delete' | 'archive';

/**
 * Tables that support retention policies
 */
export type RetentionTable = 'events' | 'sessions' | 'participants' | 'player_session_stats' | 'entities';

/**
 * Retention policy for a specific table
 */
export interface TableRetentionPolicy {
  /** Table name (without prefix) */
  table: RetentionTable;
  /** Enable this policy */
  enabled: boolean;
  /** Time-based retention: delete/archive records older than this (in days) */
  maxAgeDays?: number;
  /** Count-based retention: keep only this many records */
  maxCount?: number;
  /** Action to take when retention limit is reached */
  action: RetentionAction;
  /** For archive action: directory path to archive to */
  archivePath?: string;
  /** Priority (lower runs first, default: 10) */
  priority?: number;
}

/**
 * Archive format options
 */
export type ArchiveFormat = 'json' | 'sqlite';

/**
 * Full retention configuration
 */
export interface RetentionConfig {
  /** Enable retention system */
  enabled: boolean;
  /** Run cleanup on this schedule (interval in ms, default: 86400000 = 24h) */
  scheduleMs: number;
  /** Default action for all tables */
  defaultAction: RetentionAction;
  /** Per-table policies */
  policies: TableRetentionPolicy[];
  /** Archive format */
  archiveFormat: ArchiveFormat;
  /** Archive compression (gzip) */
  archiveCompression: boolean;
  /** Max concurrent cleanup operations */
  maxConcurrency: number;
}

/**
 * Default retention configuration
 */
export const DEFAULT_RETENTION_CONFIG: RetentionConfig = {
  enabled: false,
  scheduleMs: 86400000, // 24 hours
  defaultAction: 'delete',
  policies: [
    {
      table: 'events',
      enabled: true,
      maxAgeDays: 90,
      action: 'archive',
      priority: 1,
    },
    {
      table: 'sessions',
      enabled: true,
      maxAgeDays: 180,
      action: 'archive',
      priority: 2,
    },
    {
      table: 'participants',
      enabled: true,
      maxAgeDays: 180,
      action: 'delete',
      priority: 3,
    },
    {
      table: 'player_session_stats',
      enabled: true,
      maxAgeDays: 365,
      action: 'archive',
      priority: 4,
    },
  ],
  archiveFormat: 'json',
  archiveCompression: true,
  maxConcurrency: 1,
};

/**
 * Result of a retention cleanup operation
 */
export interface RetentionCleanupResult {
  /** Table that was cleaned */
  table: RetentionTable;
  /** Action taken */
  action: RetentionAction;
  /** Number of records processed */
  recordsProcessed: number;
  /** Number of records archived (if action was 'archive') */
  recordsArchived: number;
  /** Number of records deleted */
  recordsDeleted: number;
  /** Path to archive file (if created) */
  archivePath?: string;
  /** When cleanup started */
  startedAt: Date;
  /** When cleanup completed */
  completedAt: Date;
  /** Error message if cleanup failed */
  error?: string;
}

// ============================================================================
// Persistence Adapter Types
// ============================================================================

/**
 * Configuration for the persistence adapter
 */
export interface PersistenceConfig {
  /** Enable auto-persistence */
  enabled: boolean;
  /** Batch size for writes (default: 100) */
  batchSize: number;
  /** Flush interval in ms (default: 1000) */
  flushIntervalMs: number;
  /** Auto-persist events */
  persistEvents: boolean;
  /** Auto-persist sessions */
  persistSessions: boolean;
  /** Auto-compute player stats on session end */
  computePlayerStats: boolean;
  /** Use transactions for session persistence */
  useTransactions: boolean;
}

/**
 * Default persistence configuration
 */
export const DEFAULT_PERSISTENCE_CONFIG: PersistenceConfig = {
  enabled: true,
  batchSize: 100,
  flushIntervalMs: 1000,
  persistEvents: true,
  persistSessions: true,
  computePlayerStats: true,
  useTransactions: true,
};

// ============================================================================
// Aggregation Types
// ============================================================================

/**
 * Time bucket sizes for aggregation
 */
export type TimeBucket = 'hour' | 'day' | 'week' | 'month';

/**
 * Result of time bucket aggregation
 */
export interface TimeBucketResult {
  /** Start of the time bucket */
  bucket: Date;
  /** Number of events in this bucket */
  eventCount: number;
  /** Total damage dealt in this bucket */
  totalDamage: number;
  /** Total healing done in this bucket */
  totalHealing: number;
  /** Number of sessions that overlap this bucket */
  sessionCount: number;
}

/**
 * Result of entity aggregation
 */
export interface EntityAggregateResult {
  /** Entity name */
  entityName: string;
  /** Total amount (damage or healing) */
  totalAmount: number;
  /** Number of events */
  eventCount: number;
  /** Average amount per event */
  averageAmount: number;
  /** Peak amount in a single event */
  peakAmount: number;
}

// ============================================================================
// Adapter Events
// ============================================================================

/**
 * Events emitted by DatabaseAdapter
 */
export interface DatabaseAdapterEvents {
  'connected': () => void;
  'disconnected': () => void;
  'error': (error: Error) => void;
  'migration:start': (version: number) => void;
  'migration:complete': (version: number) => void;
}

/**
 * Events emitted by PersistenceAdapter
 */
export interface PersistenceAdapterEvents {
  'event:persisted': (event: CombatEvent) => void;
  'events:flushed': (count: number) => void;
  'session:persisted': (session: CombatSession) => void;
  'stats:computed': (stats: PlayerSessionStats[]) => void;
  'error': (error: Error, context: string) => void;
}

/**
 * Events emitted by RetentionScheduler
 */
export interface RetentionSchedulerEvents {
  'cleanup:start': () => void;
  'cleanup:complete': (results: RetentionCleanupResult[]) => void;
  'cleanup:error': (error: Error) => void;
  'policy:applied': (result: RetentionCleanupResult) => void;
}
