/**
 * Abstract DatabaseAdapter - Base class for database implementations
 *
 * Provides the interface that SQLite and PostgreSQL adapters must implement.
 * Uses EventEmitter for connection state changes and migration events.
 */
import { EventEmitter } from 'events';
import type {
  DatabaseBackend,
  DatabaseConfig,
  DatabaseAdapterEvents,
  Transaction,
  PaginationOptions,
  SortOptions,
  CombatEvent,
  Entity,
  CombatSession,
  SessionParticipant,
  PlayerSessionStats,
  PlayerAggregateStats,
  EventType,
  ActionType,
  DamageType,
  TimeBucket,
  TimeBucketResult,
  EntityAggregateResult,
} from '../types.js';

// ============================================================================
// Query Interfaces (returned by adapter methods)
// ============================================================================

/**
 * Base query interface with common methods
 */
export interface BaseQuery<T> {
  /** Execute and return all results */
  execute(): Promise<T[]>;
  /** Execute and return first result */
  first(): Promise<T | null>;
  /** Execute and return count */
  count(): Promise<number>;
  /** Apply pagination */
  paginate(options: PaginationOptions): this;
  /** Apply sorting */
  orderBy(field: string, direction?: 'asc' | 'desc'): this;
}

/**
 * Event query builder interface
 */
export interface EventQuery extends BaseQuery<CombatEvent> {
  /** Filter by event type(s) */
  byType(...types: EventType[]): this;
  /** Filter by time range */
  inTimeRange(start: Date, end?: Date): this;
  /** Filter by session */
  inSession(sessionId: string): this;
  /** Filter by source entity */
  fromEntity(entityName: string): this;
  /** Filter by target entity */
  toEntity(entityName: string): this;
  /** Filter by participant (either source or target) */
  involvingEntity(entityName: string): this;
  /** Filter by minimum damage/healing amount */
  withMinAmount(amount: number): this;
  /** Filter by action type */
  byActionType(...types: ActionType[]): this;
  /** Filter by damage type */
  byDamageType(...types: DamageType[]): this;
  /** Only critical hits/heals */
  criticalOnly(): this;
  /** Get with related entities hydrated */
  withEntities(): this;
}

/**
 * Session query builder interface
 */
export interface SessionQuery extends BaseQuery<CombatSession> {
  /** Filter by time range */
  inTimeRange(start: Date, end?: Date): this;
  /** Filter by minimum duration */
  minDuration(ms: number): this;
  /** Filter by maximum duration */
  maxDuration(ms: number): this;
  /** Filter by participant */
  withParticipant(entityName: string): this;
  /** Filter by minimum participant count */
  minParticipants(count: number): this;
  /** Include events in result */
  withEvents(): this;
  /** Include participants in result */
  withParticipants(): this;
  /** Include summary in result */
  withSummary(): this;
}

/**
 * Player session stats query builder interface
 */
export interface PlayerSessionStatsQuery extends BaseQuery<PlayerSessionStats> {
  /** Filter by time range */
  inTimeRange(start: Date, end?: Date): this;
  /** Filter by role */
  byRole(role: string): this;
  /** Filter by minimum performance score */
  minPerformance(score: number): this;
}

/**
 * Stats query builder interface
 */
export interface StatsQuery {
  /** Get player session stats */
  playerSessions(playerName: string): PlayerSessionStatsQuery;
  /** Get player aggregate stats */
  playerAggregate(playerName: string): Promise<PlayerAggregateStats | null>;
  /** Get all player aggregates */
  allPlayerAggregates(): Promise<PlayerAggregateStats[]>;
  /** Get top performers by metric */
  topPerformers(
    metric: 'damage' | 'healing' | 'kills' | 'kdr' | 'performance',
    limit?: number
  ): Promise<PlayerAggregateStats[]>;
}

/**
 * Aggregation query interface for analytics
 */
export interface AggregationQuery {
  /** Group events by time buckets */
  eventsByTimeBucket(
    bucketSize: TimeBucket,
    start?: Date,
    end?: Date
  ): Promise<TimeBucketResult[]>;
  /** Aggregate damage by entity */
  damageByEntity(sessionId?: string): Promise<EntityAggregateResult[]>;
  /** Aggregate healing by entity */
  healingByEntity(sessionId?: string): Promise<EntityAggregateResult[]>;
  /** Get event type distribution */
  eventTypeDistribution(
    start?: Date,
    end?: Date
  ): Promise<Record<string, number>>;
}

// ============================================================================
// Abstract Database Adapter
// ============================================================================

/**
 * Abstract database adapter that all implementations must extend
 */
export abstract class DatabaseAdapter extends EventEmitter {
  /** Database backend identifier */
  abstract readonly backend: DatabaseBackend;

  /** Table name prefix */
  protected tablePrefix: string = 'ccr_';

  /** Whether currently connected */
  protected _isConnected: boolean = false;

  // ============================================================================
  // Connection Lifecycle
  // ============================================================================

  /**
   * Connect to the database
   */
  abstract connect(): Promise<void>;

  /**
   * Disconnect from the database
   */
  abstract disconnect(): Promise<void>;

  /**
   * Check if connected to the database
   */
  isConnected(): boolean {
    return this._isConnected;
  }

  // ============================================================================
  // Transaction Support
  // ============================================================================

  /**
   * Begin a new transaction
   */
  abstract beginTransaction(): Promise<Transaction>;

  /**
   * Execute a function within a transaction
   * Automatically commits on success, rolls back on error
   */
  abstract executeInTransaction<T>(
    fn: (tx: Transaction) => Promise<T>
  ): Promise<T>;

  // ============================================================================
  // Migration Support
  // ============================================================================

  /**
   * Get current migration version
   */
  abstract getCurrentMigrationVersion(): Promise<number>;

  /**
   * Run all pending migrations
   */
  abstract runMigrations(): Promise<void>;

  /**
   * Rollback to a specific migration version
   */
  abstract rollbackMigration(targetVersion: number): Promise<void>;

  // ============================================================================
  // Entity CRUD
  // ============================================================================

  /**
   * Insert a new entity
   * @returns The entity ID
   */
  abstract insertEntity(entity: Entity, tx?: Transaction): Promise<string>;

  /**
   * Update an existing entity
   */
  abstract updateEntity(
    id: string,
    entity: Partial<Entity>,
    tx?: Transaction
  ): Promise<void>;

  /**
   * Get an entity by ID
   */
  abstract getEntityById(id: string): Promise<Entity | null>;

  /**
   * Get an entity by name
   */
  abstract getEntityByName(name: string): Promise<Entity | null>;

  /**
   * Find or create an entity by name
   * @returns The entity ID (existing or new)
   */
  abstract findOrCreateEntity(entity: Entity, tx?: Transaction): Promise<string>;

  // ============================================================================
  // Event CRUD
  // ============================================================================

  /**
   * Insert a single event
   * @returns The event ID
   */
  abstract insertEvent(
    event: CombatEvent,
    sessionId?: string,
    tx?: Transaction
  ): Promise<string>;

  /**
   * Insert multiple events in a batch
   * @returns Array of event IDs
   */
  abstract insertEvents(
    events: CombatEvent[],
    sessionId?: string,
    tx?: Transaction
  ): Promise<string[]>;

  /**
   * Get an event by ID
   */
  abstract getEventById(id: string): Promise<CombatEvent | null>;

  /**
   * Delete an event by ID
   * @returns true if deleted, false if not found
   */
  abstract deleteEvent(id: string, tx?: Transaction): Promise<boolean>;

  /**
   * Delete multiple events by ID
   * @returns Number of events deleted
   */
  abstract deleteEvents(ids: string[], tx?: Transaction): Promise<number>;

  /**
   * Update event's session ID
   */
  abstract updateEventSession(
    eventId: string,
    sessionId: string,
    tx?: Transaction
  ): Promise<void>;

  // ============================================================================
  // Session CRUD
  // ============================================================================

  /**
   * Insert a new session
   * @returns The session ID
   */
  abstract insertSession(
    session: CombatSession,
    tx?: Transaction
  ): Promise<string>;

  /**
   * Update an existing session
   */
  abstract updateSession(
    id: string,
    session: Partial<CombatSession>,
    tx?: Transaction
  ): Promise<void>;

  /**
   * Get a session by ID
   */
  abstract getSessionById(id: string): Promise<CombatSession | null>;

  /**
   * Delete a session by ID
   * @returns true if deleted, false if not found
   */
  abstract deleteSession(id: string, tx?: Transaction): Promise<boolean>;

  // ============================================================================
  // Participant CRUD
  // ============================================================================

  /**
   * Insert a session participant
   * @returns The participant row ID
   */
  abstract insertParticipant(
    sessionId: string,
    participant: SessionParticipant,
    tx?: Transaction
  ): Promise<string>;

  /**
   * Get participants for a session
   */
  abstract getParticipantsBySession(
    sessionId: string
  ): Promise<SessionParticipant[]>;

  // ============================================================================
  // Player Stats CRUD
  // ============================================================================

  /**
   * Insert player session stats
   * @returns The stats row ID
   */
  abstract insertPlayerSessionStats(
    stats: PlayerSessionStats,
    tx?: Transaction
  ): Promise<string>;

  /**
   * Update or insert player aggregate stats
   */
  abstract upsertPlayerAggregateStats(
    stats: PlayerAggregateStats,
    tx?: Transaction
  ): Promise<void>;

  /**
   * Get player session stats
   */
  abstract getPlayerSessionStats(
    playerName: string,
    sessionId: string
  ): Promise<PlayerSessionStats | null>;

  /**
   * Get player aggregate stats
   */
  abstract getPlayerAggregateStats(
    playerName: string
  ): Promise<PlayerAggregateStats | null>;

  // ============================================================================
  // Query Builders
  // ============================================================================

  /**
   * Get event query builder
   */
  abstract events(): EventQuery;

  /**
   * Get session query builder
   */
  abstract sessions(): SessionQuery;

  /**
   * Get stats query builder
   */
  abstract stats(): StatsQuery;

  /**
   * Get aggregation query builder
   */
  abstract aggregations(): AggregationQuery;

  // ============================================================================
  // Maintenance Operations
  // ============================================================================

  /**
   * Vacuum/optimize the database
   */
  abstract vacuum(): Promise<void>;

  /**
   * Get row counts for all tables
   */
  abstract getTableRowCounts(): Promise<Record<string, number>>;

  /**
   * Get the oldest record date for a table
   */
  abstract getOldestRecordDate(table: string): Promise<Date | null>;

  /**
   * Delete records older than a date
   * @returns Number of records deleted
   */
  abstract deleteRecordsOlderThan(
    table: string,
    date: Date,
    tx?: Transaction
  ): Promise<number>;

  /**
   * Get records older than a date (for archival)
   */
  abstract getRecordsOlderThan<T>(
    table: string,
    date: Date,
    limit?: number
  ): Promise<T[]>;

  // ============================================================================
  // Helper Methods
  // ============================================================================

  /**
   * Get full table name with prefix
   */
  protected tableName(name: string): string {
    return `${this.tablePrefix}${name}`;
  }

  // ============================================================================
  // Type-safe EventEmitter overrides
  // ============================================================================

  override on<K extends keyof DatabaseAdapterEvents>(
    event: K,
    listener: DatabaseAdapterEvents[K]
  ): this {
    return super.on(event, listener);
  }

  override once<K extends keyof DatabaseAdapterEvents>(
    event: K,
    listener: DatabaseAdapterEvents[K]
  ): this {
    return super.once(event, listener);
  }

  override emit<K extends keyof DatabaseAdapterEvents>(
    event: K,
    ...args: Parameters<DatabaseAdapterEvents[K]>
  ): boolean {
    return super.emit(event, ...args);
  }

  override off<K extends keyof DatabaseAdapterEvents>(
    event: K,
    listener: DatabaseAdapterEvents[K]
  ): this {
    return super.off(event, listener);
  }
}
