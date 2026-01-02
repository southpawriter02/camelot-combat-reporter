/**
 * PostgreSQLAdapter - PostgreSQL database implementation
 *
 * Uses node-postgres (pg) with connection pooling for async access.
 */
import { Pool, PoolClient } from 'pg';
import { v4 as uuidv4 } from 'uuid';
import {
  DatabaseAdapter,
  type EventQuery,
  type SessionQuery,
  type StatsQuery,
  type AggregationQuery,
  type PlayerSessionStatsQuery,
} from './DatabaseAdapter.js';
import type {
  PostgreSQLConnectionConfig,
  Transaction,
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
  PaginationOptions,
} from '../types.js';
import { DEFAULT_POSTGRESQL_CONFIG } from '../types.js';
import {
  ConnectionError,
  NotConnectedError,
  QueryError,
  MigrationError,
  TransactionError,
} from '../errors.js';
import {
  getMigrationsFrom,
  getMigrationsToRollback,
  applyPrefix,
} from '../schema/migrations.js';

/**
 * PostgreSQL-specific migrations (JSONB enhancements)
 */
const POSTGRESQL_MIGRATIONS = [
  {
    version: 1,
    name: 'initial_schema_postgresql',
    up: [
      // Entities table
      `CREATE TABLE IF NOT EXISTS {{prefix}}entities (
        id TEXT PRIMARY KEY,
        name TEXT NOT NULL UNIQUE,
        entity_type TEXT NOT NULL,
        realm TEXT,
        is_player BOOLEAN NOT NULL DEFAULT FALSE,
        is_self BOOLEAN NOT NULL DEFAULT FALSE,
        first_seen TIMESTAMPTZ NOT NULL,
        last_seen TIMESTAMPTZ NOT NULL,
        created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
      )`,
      `CREATE INDEX IF NOT EXISTS idx_entities_name ON {{prefix}}entities(name)`,
      `CREATE INDEX IF NOT EXISTS idx_entities_type ON {{prefix}}entities(entity_type)`,

      // Sessions table
      `CREATE TABLE IF NOT EXISTS {{prefix}}sessions (
        id TEXT PRIMARY KEY,
        start_time TIMESTAMPTZ NOT NULL,
        end_time TIMESTAMPTZ NOT NULL,
        duration_ms INTEGER NOT NULL,
        summary_data JSONB NOT NULL,
        created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
      )`,
      `CREATE INDEX IF NOT EXISTS idx_sessions_start_time ON {{prefix}}sessions(start_time)`,
      `CREATE INDEX IF NOT EXISTS idx_sessions_duration ON {{prefix}}sessions(duration_ms)`,
      `CREATE INDEX IF NOT EXISTS idx_sessions_summary ON {{prefix}}sessions USING GIN (summary_data)`,

      // Events table
      `CREATE TABLE IF NOT EXISTS {{prefix}}events (
        id TEXT PRIMARY KEY,
        session_id TEXT REFERENCES {{prefix}}sessions(id) ON DELETE CASCADE,
        event_type TEXT NOT NULL,
        timestamp TIMESTAMPTZ NOT NULL,
        raw_timestamp TEXT NOT NULL,
        raw_line TEXT NOT NULL,
        line_number INTEGER NOT NULL,
        source_entity_id TEXT REFERENCES {{prefix}}entities(id),
        target_entity_id TEXT REFERENCES {{prefix}}entities(id),
        event_data JSONB NOT NULL,
        created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
      )`,
      `CREATE INDEX IF NOT EXISTS idx_events_session ON {{prefix}}events(session_id)`,
      `CREATE INDEX IF NOT EXISTS idx_events_type ON {{prefix}}events(event_type)`,
      `CREATE INDEX IF NOT EXISTS idx_events_timestamp ON {{prefix}}events(timestamp)`,
      `CREATE INDEX IF NOT EXISTS idx_events_source ON {{prefix}}events(source_entity_id)`,
      `CREATE INDEX IF NOT EXISTS idx_events_target ON {{prefix}}events(target_entity_id)`,
      `CREATE INDEX IF NOT EXISTS idx_events_session_type ON {{prefix}}events(session_id, event_type)`,
      `CREATE INDEX IF NOT EXISTS idx_events_type_timestamp ON {{prefix}}events(event_type, timestamp)`,
      `CREATE INDEX IF NOT EXISTS idx_events_data ON {{prefix}}events USING GIN (event_data)`,

      // Partial indexes for common event types
      `CREATE INDEX IF NOT EXISTS idx_events_damage ON {{prefix}}events(timestamp)
        WHERE event_type IN ('DAMAGE_DEALT', 'DAMAGE_RECEIVED')`,
      `CREATE INDEX IF NOT EXISTS idx_events_healing ON {{prefix}}events(timestamp)
        WHERE event_type IN ('HEALING_DONE', 'HEALING_RECEIVED')`,

      // Participants table
      `CREATE TABLE IF NOT EXISTS {{prefix}}participants (
        id TEXT PRIMARY KEY,
        session_id TEXT NOT NULL REFERENCES {{prefix}}sessions(id) ON DELETE CASCADE,
        entity_id TEXT NOT NULL REFERENCES {{prefix}}entities(id),
        role TEXT NOT NULL,
        first_seen TIMESTAMPTZ NOT NULL,
        last_seen TIMESTAMPTZ NOT NULL,
        event_count INTEGER NOT NULL,
        created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
        UNIQUE(session_id, entity_id)
      )`,
      `CREATE INDEX IF NOT EXISTS idx_participants_session ON {{prefix}}participants(session_id)`,
      `CREATE INDEX IF NOT EXISTS idx_participants_entity ON {{prefix}}participants(entity_id)`,

      // Player session stats table
      `CREATE TABLE IF NOT EXISTS {{prefix}}player_session_stats (
        id TEXT PRIMARY KEY,
        player_name TEXT NOT NULL,
        session_id TEXT NOT NULL REFERENCES {{prefix}}sessions(id) ON DELETE CASCADE,
        session_start TIMESTAMPTZ NOT NULL,
        session_end TIMESTAMPTZ NOT NULL,
        duration_ms INTEGER NOT NULL,
        role TEXT NOT NULL,
        kills INTEGER NOT NULL DEFAULT 0,
        deaths INTEGER NOT NULL DEFAULT 0,
        assists INTEGER NOT NULL DEFAULT 0,
        kdr REAL NOT NULL DEFAULT 0,
        damage_dealt BIGINT NOT NULL DEFAULT 0,
        damage_taken BIGINT NOT NULL DEFAULT 0,
        dps REAL NOT NULL DEFAULT 0,
        peak_dps REAL NOT NULL DEFAULT 0,
        healing_done BIGINT NOT NULL DEFAULT 0,
        healing_received BIGINT NOT NULL DEFAULT 0,
        hps REAL NOT NULL DEFAULT 0,
        overheal_rate REAL NOT NULL DEFAULT 0,
        crit_rate REAL NOT NULL DEFAULT 0,
        performance_score REAL NOT NULL DEFAULT 0,
        performance_rating TEXT NOT NULL,
        created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
        UNIQUE(player_name, session_id)
      )`,
      `CREATE INDEX IF NOT EXISTS idx_player_session_stats_player ON {{prefix}}player_session_stats(player_name)`,
      `CREATE INDEX IF NOT EXISTS idx_player_session_stats_session ON {{prefix}}player_session_stats(session_id)`,
      `CREATE INDEX IF NOT EXISTS idx_player_session_stats_time ON {{prefix}}player_session_stats(session_start)`,

      // Player aggregate stats table
      `CREATE TABLE IF NOT EXISTS {{prefix}}player_aggregate_stats (
        id TEXT PRIMARY KEY,
        player_name TEXT NOT NULL UNIQUE,
        total_sessions INTEGER NOT NULL DEFAULT 0,
        total_combat_time_ms BIGINT NOT NULL DEFAULT 0,
        total_kills INTEGER NOT NULL DEFAULT 0,
        total_deaths INTEGER NOT NULL DEFAULT 0,
        total_assists INTEGER NOT NULL DEFAULT 0,
        overall_kdr REAL NOT NULL DEFAULT 0,
        total_damage_dealt BIGINT NOT NULL DEFAULT 0,
        total_damage_taken BIGINT NOT NULL DEFAULT 0,
        total_healing_done BIGINT NOT NULL DEFAULT 0,
        total_healing_received BIGINT NOT NULL DEFAULT 0,
        avg_dps REAL NOT NULL DEFAULT 0,
        avg_hps REAL NOT NULL DEFAULT 0,
        avg_performance_score REAL NOT NULL DEFAULT 0,
        performance_variance REAL NOT NULL DEFAULT 0,
        consistency_rating TEXT NOT NULL,
        stats_data JSONB NOT NULL,
        created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
        updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
      )`,
      `CREATE INDEX IF NOT EXISTS idx_player_aggregate_stats_player ON {{prefix}}player_aggregate_stats(player_name)`,

      // Migrations tracking table
      `CREATE TABLE IF NOT EXISTS {{prefix}}migrations (
        version INTEGER PRIMARY KEY,
        name TEXT NOT NULL,
        applied_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
      )`,
    ],
    down: [
      `DROP TABLE IF EXISTS {{prefix}}migrations`,
      `DROP TABLE IF EXISTS {{prefix}}player_aggregate_stats`,
      `DROP TABLE IF EXISTS {{prefix}}player_session_stats`,
      `DROP TABLE IF EXISTS {{prefix}}participants`,
      `DROP TABLE IF EXISTS {{prefix}}events`,
      `DROP TABLE IF EXISTS {{prefix}}sessions`,
      `DROP TABLE IF EXISTS {{prefix}}entities`,
    ],
  },
];

/**
 * PostgreSQL transaction implementation
 */
class PostgreSQLTransaction implements Transaction {
  readonly id: string;
  private _isActive: boolean = true;
  private client: PoolClient;

  constructor(client: PoolClient) {
    this.id = uuidv4();
    this.client = client;
  }

  async commit(): Promise<void> {
    if (!this._isActive) {
      throw new TransactionError('Transaction already completed', this.id);
    }
    await this.client.query('COMMIT');
    this._isActive = false;
    this.client.release();
  }

  async rollback(): Promise<void> {
    if (!this._isActive) {
      return;
    }
    await this.client.query('ROLLBACK');
    this._isActive = false;
    this.client.release();
  }

  isActive(): boolean {
    return this._isActive;
  }

  getClient(): PoolClient {
    return this.client;
  }
}

/**
 * PostgreSQL database adapter
 */
export class PostgreSQLAdapter extends DatabaseAdapter {
  readonly backend = 'postgresql' as const;
  private pool: Pool | null = null;
  private config: PostgreSQLConnectionConfig;
  private entityCache = new Map<string, string>();

  constructor(config: Partial<PostgreSQLConnectionConfig> & { database: string }) {
    super();
    this.config = { ...DEFAULT_POSTGRESQL_CONFIG, ...config } as PostgreSQLConnectionConfig;
    if ((config as any).tablePrefix) {
      this.tablePrefix = (config as any).tablePrefix;
    }
  }

  // ============================================================================
  // Connection Lifecycle
  // ============================================================================

  async connect(): Promise<void> {
    if (this._isConnected) {
      return;
    }

    try {
      const poolConfig = this.config.connectionString
        ? { connectionString: this.config.connectionString }
        : {
            host: this.config.host,
            port: this.config.port,
            database: this.config.database,
            user: this.config.user,
            password: this.config.password,
            ssl: this.config.ssl,
            min: this.config.pool?.min ?? 2,
            max: this.config.pool?.max ?? 10,
            idleTimeoutMillis: this.config.pool?.idleTimeoutMs ?? 30000,
          };

      this.pool = new Pool(poolConfig);

      // Test connection
      const client = await this.pool.connect();
      client.release();

      this._isConnected = true;
      this.emit('connected');
    } catch (error) {
      throw new ConnectionError(
        `Failed to connect to PostgreSQL: ${(error as Error).message}`,
        'postgresql',
        error as Error
      );
    }
  }

  async disconnect(): Promise<void> {
    if (!this._isConnected || !this.pool) {
      return;
    }

    try {
      await this.pool.end();
      this.pool = null;
      this._isConnected = false;
      this.entityCache.clear();
      this.emit('disconnected');
    } catch (error) {
      throw new ConnectionError(
        `Failed to disconnect from PostgreSQL: ${(error as Error).message}`,
        'postgresql',
        error as Error
      );
    }
  }

  private ensureConnected(): Pool {
    if (!this._isConnected || !this.pool) {
      throw new NotConnectedError();
    }
    return this.pool;
  }

  // ============================================================================
  // Transaction Support
  // ============================================================================

  async beginTransaction(): Promise<Transaction> {
    const pool = this.ensureConnected();
    const client = await pool.connect();
    await client.query('BEGIN');
    return new PostgreSQLTransaction(client);
  }

  async executeInTransaction<T>(
    fn: (tx: Transaction) => Promise<T>
  ): Promise<T> {
    const tx = await this.beginTransaction();
    try {
      const result = await fn(tx);
      await tx.commit();
      return result;
    } catch (error) {
      await tx.rollback();
      throw error;
    }
  }

  private getClient(tx?: Transaction): PoolClient | Pool {
    if (tx && tx instanceof PostgreSQLTransaction) {
      return tx.getClient();
    }
    return this.ensureConnected();
  }

  // ============================================================================
  // Migration Support
  // ============================================================================

  async getCurrentMigrationVersion(): Promise<number> {
    const pool = this.ensureConnected();

    // Check if migrations table exists
    const tableCheck = await pool.query(
      `SELECT EXISTS (
        SELECT FROM information_schema.tables
        WHERE table_name = $1
      )`,
      [this.tableName('migrations')]
    );

    if (!tableCheck.rows[0].exists) {
      return 0;
    }

    const result = await pool.query(
      `SELECT MAX(version) as version FROM ${this.tableName('migrations')}`
    );

    return result.rows[0]?.version ?? 0;
  }

  async runMigrations(): Promise<void> {
    const pool = this.ensureConnected();
    const currentVersion = await this.getCurrentMigrationVersion();

    // Use PostgreSQL-specific migrations
    const migrationsToRun = POSTGRESQL_MIGRATIONS.filter(
      (m) => m.version > currentVersion
    );

    for (const migration of migrationsToRun) {
      this.emit('migration:start', migration.version);

      const client = await pool.connect();
      try {
        await client.query('BEGIN');

        for (const sql of migration.up) {
          const prefixedSql = applyPrefix(sql, this.tablePrefix);
          await client.query(prefixedSql);
        }

        // Record migration
        if (migration.version === 1) {
          await client.query(
            `INSERT INTO ${this.tableName('migrations')} (version, name) VALUES ($1, $2)`,
            [migration.version, migration.name]
          );
        } else {
          await client.query(
            `INSERT INTO ${this.tableName('migrations')} (version, name) VALUES ($1, $2)`,
            [migration.version, migration.name]
          );
        }

        await client.query('COMMIT');
        this.emit('migration:complete', migration.version);
      } catch (error) {
        await client.query('ROLLBACK');
        throw new MigrationError(
          `Failed to run migration ${migration.name}: ${(error as Error).message}`,
          migration.version,
          migration.name,
          error as Error
        );
      } finally {
        client.release();
      }
    }
  }

  async rollbackMigration(targetVersion: number): Promise<void> {
    const pool = this.ensureConnected();
    const currentVersion = await this.getCurrentMigrationVersion();

    const migrationsToRollback = POSTGRESQL_MIGRATIONS
      .filter((m) => m.version <= currentVersion && m.version > targetVersion)
      .reverse();

    for (const migration of migrationsToRollback) {
      const client = await pool.connect();
      try {
        await client.query('BEGIN');

        for (const sql of migration.down) {
          const prefixedSql = applyPrefix(sql, this.tablePrefix);
          await client.query(prefixedSql);
        }

        await client.query(
          `DELETE FROM ${this.tableName('migrations')} WHERE version = $1`,
          [migration.version]
        );

        await client.query('COMMIT');
      } catch (error) {
        await client.query('ROLLBACK');
        throw new MigrationError(
          `Failed to rollback migration ${migration.name}: ${(error as Error).message}`,
          migration.version,
          migration.name,
          error as Error
        );
      } finally {
        client.release();
      }
    }
  }

  // ============================================================================
  // Entity CRUD
  // ============================================================================

  async insertEntity(entity: Entity, tx?: Transaction): Promise<string> {
    const client = this.getClient(tx);
    const id = uuidv4();
    const now = new Date();

    try {
      await client.query(
        `INSERT INTO ${this.tableName('entities')}
         (id, name, entity_type, realm, is_player, is_self, first_seen, last_seen)
         VALUES ($1, $2, $3, $4, $5, $6, $7, $8)`,
        [
          id,
          entity.name,
          entity.entityType,
          entity.realm ?? null,
          entity.isPlayer,
          entity.isSelf,
          now,
          now,
        ]
      );

      this.entityCache.set(entity.name, id);
      return id;
    } catch (error) {
      throw new QueryError(
        `Failed to insert entity: ${(error as Error).message}`,
        undefined,
        undefined,
        error as Error
      );
    }
  }

  async updateEntity(
    id: string,
    entity: Partial<Entity>,
    tx?: Transaction
  ): Promise<void> {
    const client = this.getClient(tx);
    const updates: string[] = [];
    const values: unknown[] = [];
    let paramIndex = 1;

    if (entity.name !== undefined) {
      updates.push(`name = $${paramIndex++}`);
      values.push(entity.name);
    }
    if (entity.entityType !== undefined) {
      updates.push(`entity_type = $${paramIndex++}`);
      values.push(entity.entityType);
    }
    if (entity.realm !== undefined) {
      updates.push(`realm = $${paramIndex++}`);
      values.push(entity.realm);
    }
    if (entity.isPlayer !== undefined) {
      updates.push(`is_player = $${paramIndex++}`);
      values.push(entity.isPlayer);
    }
    if (entity.isSelf !== undefined) {
      updates.push(`is_self = $${paramIndex++}`);
      values.push(entity.isSelf);
    }

    updates.push(`last_seen = $${paramIndex++}`);
    values.push(new Date());
    values.push(id);

    if (updates.length > 1) {
      await client.query(
        `UPDATE ${this.tableName('entities')} SET ${updates.join(', ')} WHERE id = $${paramIndex}`,
        values
      );
    }
  }

  async getEntityById(id: string): Promise<Entity | null> {
    const pool = this.ensureConnected();
    const result = await pool.query(
      `SELECT * FROM ${this.tableName('entities')} WHERE id = $1`,
      [id]
    );

    return result.rows[0] ? this.rowToEntity(result.rows[0]) : null;
  }

  async getEntityByName(name: string): Promise<Entity | null> {
    const pool = this.ensureConnected();
    const result = await pool.query(
      `SELECT * FROM ${this.tableName('entities')} WHERE name = $1`,
      [name]
    );

    return result.rows[0] ? this.rowToEntity(result.rows[0]) : null;
  }

  async findOrCreateEntity(entity: Entity, tx?: Transaction): Promise<string> {
    if (this.entityCache.has(entity.name)) {
      return this.entityCache.get(entity.name)!;
    }

    const client = this.getClient(tx);
    const result = await client.query(
      `SELECT id FROM ${this.tableName('entities')} WHERE name = $1`,
      [entity.name]
    );

    if (result.rows[0]) {
      this.entityCache.set(entity.name, result.rows[0].id);
      await client.query(
        `UPDATE ${this.tableName('entities')} SET last_seen = $1 WHERE id = $2`,
        [new Date(), result.rows[0].id]
      );
      return result.rows[0].id;
    }

    return this.insertEntity(entity, tx);
  }

  private rowToEntity(row: Record<string, unknown>): Entity {
    return {
      name: row.name as string,
      entityType: row.entity_type as string,
      realm: row.realm as string | undefined,
      isPlayer: row.is_player as boolean,
      isSelf: row.is_self as boolean,
    } as Entity;
  }

  // ============================================================================
  // Event CRUD
  // ============================================================================

  async insertEvent(
    event: CombatEvent,
    sessionId?: string,
    tx?: Transaction
  ): Promise<string> {
    const client = this.getClient(tx);

    let sourceEntityId: string | null = null;
    let targetEntityId: string | null = null;

    if ('source' in event && event.source) {
      sourceEntityId = await this.findOrCreateEntity(event.source as Entity, tx);
    }
    if ('target' in event && event.target) {
      targetEntityId = await this.findOrCreateEntity(event.target as Entity, tx);
    }

    const eventData = this.serializeEventData(event);

    try {
      await client.query(
        `INSERT INTO ${this.tableName('events')}
         (id, session_id, event_type, timestamp, raw_timestamp, raw_line, line_number,
          source_entity_id, target_entity_id, event_data)
         VALUES ($1, $2, $3, $4, $5, $6, $7, $8, $9, $10)`,
        [
          event.id,
          sessionId ?? null,
          event.eventType,
          event.timestamp,
          event.rawTimestamp,
          event.rawLine,
          event.lineNumber,
          sourceEntityId,
          targetEntityId,
          eventData,
        ]
      );

      return event.id;
    } catch (error) {
      throw new QueryError(
        `Failed to insert event: ${(error as Error).message}`,
        undefined,
        undefined,
        error as Error
      );
    }
  }

  async insertEvents(
    events: CombatEvent[],
    sessionId?: string,
    tx?: Transaction
  ): Promise<string[]> {
    const ids: string[] = [];
    for (const event of events) {
      const id = await this.insertEvent(event, sessionId, tx);
      ids.push(id);
    }
    return ids;
  }

  async getEventById(id: string): Promise<CombatEvent | null> {
    const pool = this.ensureConnected();
    const result = await pool.query(
      `SELECT * FROM ${this.tableName('events')} WHERE id = $1`,
      [id]
    );

    if (!result.rows[0]) return null;
    return this.rowToEvent(result.rows[0]);
  }

  async deleteEvent(id: string, tx?: Transaction): Promise<boolean> {
    const client = this.getClient(tx);
    const result = await client.query(
      `DELETE FROM ${this.tableName('events')} WHERE id = $1`,
      [id]
    );
    return (result.rowCount ?? 0) > 0;
  }

  async deleteEvents(ids: string[], tx?: Transaction): Promise<number> {
    if (ids.length === 0) return 0;

    const client = this.getClient(tx);
    const placeholders = ids.map((_, i) => `$${i + 1}`).join(',');
    const result = await client.query(
      `DELETE FROM ${this.tableName('events')} WHERE id IN (${placeholders})`,
      ids
    );
    return result.rowCount ?? 0;
  }

  async updateEventSession(
    eventId: string,
    sessionId: string,
    tx?: Transaction
  ): Promise<void> {
    const client = this.getClient(tx);
    await client.query(
      `UPDATE ${this.tableName('events')} SET session_id = $1 WHERE id = $2`,
      [sessionId, eventId]
    );
  }

  private serializeEventData(event: CombatEvent): object {
    const data: Record<string, unknown> = {};

    if ('amount' in event) data.amount = event.amount;
    if ('absorbedAmount' in event) data.absorbedAmount = event.absorbedAmount;
    if ('effectiveAmount' in event) data.effectiveAmount = event.effectiveAmount;
    if ('damageType' in event) data.damageType = event.damageType;
    if ('actionType' in event) data.actionType = event.actionType;
    if ('actionName' in event) data.actionName = event.actionName;
    if ('isCritical' in event) data.isCritical = event.isCritical;
    if ('isBlocked' in event) data.isBlocked = event.isBlocked;
    if ('isParried' in event) data.isParried = event.isParried;
    if ('isEvaded' in event) data.isEvaded = event.isEvaded;
    if ('weaponName' in event) data.weaponName = event.weaponName;
    if ('effect' in event) data.effect = event.effect;
    if ('durationMs' in event) data.durationMs = event.durationMs;
    if ('overhealing' in event) data.overhealing = event.overhealing;
    if ('killer' in event && event.killer) {
      data.killer = {
        name: event.killer.name,
        entityType: event.killer.entityType,
        realm: event.killer.realm,
        isPlayer: event.killer.isPlayer,
        isSelf: event.killer.isSelf,
      };
    }

    return data;
  }

  private async rowToEvent(row: Record<string, unknown>): Promise<CombatEvent> {
    const eventData = row.event_data as Record<string, unknown>;

    let source: Entity | undefined;
    let target: Entity | undefined;

    if (row.source_entity_id) {
      source = (await this.getEntityById(row.source_entity_id as string)) ?? undefined;
    }
    if (row.target_entity_id) {
      target = (await this.getEntityById(row.target_entity_id as string)) ?? undefined;
    }

    const baseEvent = {
      id: row.id as string,
      timestamp: new Date(row.timestamp as string),
      rawTimestamp: row.raw_timestamp as string,
      rawLine: row.raw_line as string,
      lineNumber: row.line_number as number,
      eventType: row.event_type as string,
    };

    const eventType = row.event_type as string;

    if (eventType === 'DAMAGE_DEALT' || eventType === 'DAMAGE_RECEIVED') {
      return {
        ...baseEvent,
        eventType: eventType as EventType,
        source: source!,
        target: target!,
        amount: eventData.amount,
        absorbedAmount: eventData.absorbedAmount,
        effectiveAmount: eventData.effectiveAmount,
        damageType: eventData.damageType,
        actionType: eventData.actionType,
        actionName: eventData.actionName,
        isCritical: eventData.isCritical,
        isBlocked: eventData.isBlocked,
        isParried: eventData.isParried,
        isEvaded: eventData.isEvaded,
        weaponName: eventData.weaponName,
      } as CombatEvent;
    }

    if (eventType === 'HEALING_DONE' || eventType === 'HEALING_RECEIVED') {
      return {
        ...baseEvent,
        eventType: eventType as EventType,
        source: source!,
        target: target!,
        amount: eventData.amount,
        effectiveAmount: eventData.effectiveAmount,
        overhealing: eventData.overhealing,
        isCritical: eventData.isCritical,
        actionType: eventData.actionType,
        actionName: eventData.actionName,
      } as CombatEvent;
    }

    if (eventType === 'CROWD_CONTROL') {
      return {
        ...baseEvent,
        eventType: eventType as EventType,
        source: source!,
        target: target!,
        effect: eventData.effect,
        durationMs: eventData.durationMs,
        actionName: eventData.actionName,
      } as CombatEvent;
    }

    if (eventType === 'DEATH') {
      return {
        ...baseEvent,
        eventType: eventType as EventType,
        target: target!,
        killer: eventData.killer,
      } as CombatEvent;
    }

    return {
      ...baseEvent,
      eventType: 'UNKNOWN' as EventType,
    } as CombatEvent;
  }

  // ============================================================================
  // Session CRUD
  // ============================================================================

  async insertSession(
    session: CombatSession,
    tx?: Transaction
  ): Promise<string> {
    const client = this.getClient(tx);

    try {
      await client.query(
        `INSERT INTO ${this.tableName('sessions')}
         (id, start_time, end_time, duration_ms, summary_data)
         VALUES ($1, $2, $3, $4, $5)`,
        [
          session.id,
          session.startTime,
          session.endTime,
          session.durationMs,
          session.summary,
        ]
      );

      return session.id;
    } catch (error) {
      throw new QueryError(
        `Failed to insert session: ${(error as Error).message}`,
        undefined,
        undefined,
        error as Error
      );
    }
  }

  async updateSession(
    id: string,
    session: Partial<CombatSession>,
    tx?: Transaction
  ): Promise<void> {
    const client = this.getClient(tx);
    const updates: string[] = [];
    const values: unknown[] = [];
    let paramIndex = 1;

    if (session.startTime !== undefined) {
      updates.push(`start_time = $${paramIndex++}`);
      values.push(session.startTime);
    }
    if (session.endTime !== undefined) {
      updates.push(`end_time = $${paramIndex++}`);
      values.push(session.endTime);
    }
    if (session.durationMs !== undefined) {
      updates.push(`duration_ms = $${paramIndex++}`);
      values.push(session.durationMs);
    }
    if (session.summary !== undefined) {
      updates.push(`summary_data = $${paramIndex++}`);
      values.push(session.summary);
    }

    if (updates.length > 0) {
      values.push(id);
      await client.query(
        `UPDATE ${this.tableName('sessions')} SET ${updates.join(', ')} WHERE id = $${paramIndex}`,
        values
      );
    }
  }

  async getSessionById(id: string): Promise<CombatSession | null> {
    const pool = this.ensureConnected();
    const result = await pool.query(
      `SELECT * FROM ${this.tableName('sessions')} WHERE id = $1`,
      [id]
    );

    if (!result.rows[0]) return null;

    const row = result.rows[0];
    const participants = await this.getParticipantsBySession(id);

    const eventResult = await pool.query(
      `SELECT * FROM ${this.tableName('events')} WHERE session_id = $1 ORDER BY timestamp`,
      [id]
    );

    const events = await Promise.all(
      eventResult.rows.map((r) => this.rowToEvent(r))
    );

    return {
      id: row.id,
      startTime: new Date(row.start_time),
      endTime: new Date(row.end_time),
      durationMs: row.duration_ms,
      summary: row.summary_data,
      events,
      participants,
    };
  }

  async deleteSession(id: string, tx?: Transaction): Promise<boolean> {
    const client = this.getClient(tx);
    const result = await client.query(
      `DELETE FROM ${this.tableName('sessions')} WHERE id = $1`,
      [id]
    );
    return (result.rowCount ?? 0) > 0;
  }

  // ============================================================================
  // Participant CRUD
  // ============================================================================

  async insertParticipant(
    sessionId: string,
    participant: SessionParticipant,
    tx?: Transaction
  ): Promise<string> {
    const client = this.getClient(tx);
    const id = uuidv4();
    const entityId = await this.findOrCreateEntity(participant.entity, tx);

    try {
      await client.query(
        `INSERT INTO ${this.tableName('participants')}
         (id, session_id, entity_id, role, first_seen, last_seen, event_count)
         VALUES ($1, $2, $3, $4, $5, $6, $7)`,
        [
          id,
          sessionId,
          entityId,
          participant.role,
          participant.firstSeen,
          participant.lastSeen,
          participant.eventCount,
        ]
      );

      return id;
    } catch (error) {
      throw new QueryError(
        `Failed to insert participant: ${(error as Error).message}`,
        undefined,
        undefined,
        error as Error
      );
    }
  }

  async getParticipantsBySession(
    sessionId: string
  ): Promise<SessionParticipant[]> {
    const pool = this.ensureConnected();
    const result = await pool.query(
      `SELECT p.*, e.name, e.entity_type, e.realm, e.is_player, e.is_self
       FROM ${this.tableName('participants')} p
       JOIN ${this.tableName('entities')} e ON p.entity_id = e.id
       WHERE p.session_id = $1`,
      [sessionId]
    );

    return result.rows.map((row) => ({
      entity: {
        name: row.name,
        entityType: row.entity_type,
        realm: row.realm,
        isPlayer: row.is_player,
        isSelf: row.is_self,
      } as Entity,
      role: row.role,
      firstSeen: new Date(row.first_seen),
      lastSeen: new Date(row.last_seen),
      eventCount: row.event_count,
    })) as SessionParticipant[];
  }

  // ============================================================================
  // Player Stats CRUD
  // ============================================================================

  async insertPlayerSessionStats(
    stats: PlayerSessionStats,
    tx?: Transaction
  ): Promise<string> {
    const client = this.getClient(tx);
    const id = uuidv4();

    try {
      await client.query(
        `INSERT INTO ${this.tableName('player_session_stats')}
         (id, player_name, session_id, session_start, session_end, duration_ms, role,
          kills, deaths, assists, kdr, damage_dealt, damage_taken, dps, peak_dps,
          healing_done, healing_received, hps, overheal_rate, crit_rate,
          performance_score, performance_rating)
         VALUES ($1, $2, $3, $4, $5, $6, $7, $8, $9, $10, $11, $12, $13, $14, $15, $16, $17, $18, $19, $20, $21, $22)`,
        [
          id,
          stats.playerName,
          stats.sessionId,
          stats.sessionStart,
          stats.sessionEnd,
          stats.durationMs,
          stats.role,
          stats.kills,
          stats.deaths,
          stats.assists,
          stats.kdr,
          stats.damageDealt,
          stats.damageTaken,
          stats.dps,
          stats.peakDps,
          stats.healingDone,
          stats.healingReceived,
          stats.hps,
          stats.overhealRate,
          stats.critRate,
          stats.performanceScore,
          stats.performanceRating,
        ]
      );

      return id;
    } catch (error) {
      throw new QueryError(
        `Failed to insert player session stats: ${(error as Error).message}`,
        undefined,
        undefined,
        error as Error
      );
    }
  }

  async upsertPlayerAggregateStats(
    stats: PlayerAggregateStats,
    tx?: Transaction
  ): Promise<void> {
    const client = this.getClient(tx);

    const statsData = {
      bestFight: stats.bestFight,
      worstFight: stats.worstFight,
      dpsOverTime: stats.dpsOverTime,
      kdrOverTime: stats.kdrOverTime,
      performanceOverTime: stats.performanceOverTime,
      performanceDistribution: stats.performanceDistribution,
      avgKillsPerSession: stats.avgKillsPerSession,
      avgDeathsPerSession: stats.avgDeathsPerSession,
    };

    const id = uuidv4();

    await client.query(
      `INSERT INTO ${this.tableName('player_aggregate_stats')}
       (id, player_name, total_sessions, total_combat_time_ms, total_kills, total_deaths,
        total_assists, overall_kdr, total_damage_dealt, total_damage_taken,
        total_healing_done, total_healing_received, avg_dps, avg_hps,
        avg_performance_score, performance_variance, consistency_rating, stats_data)
       VALUES ($1, $2, $3, $4, $5, $6, $7, $8, $9, $10, $11, $12, $13, $14, $15, $16, $17, $18)
       ON CONFLICT (player_name) DO UPDATE SET
         total_sessions = EXCLUDED.total_sessions,
         total_combat_time_ms = EXCLUDED.total_combat_time_ms,
         total_kills = EXCLUDED.total_kills,
         total_deaths = EXCLUDED.total_deaths,
         total_assists = EXCLUDED.total_assists,
         overall_kdr = EXCLUDED.overall_kdr,
         total_damage_dealt = EXCLUDED.total_damage_dealt,
         total_damage_taken = EXCLUDED.total_damage_taken,
         total_healing_done = EXCLUDED.total_healing_done,
         total_healing_received = EXCLUDED.total_healing_received,
         avg_dps = EXCLUDED.avg_dps,
         avg_hps = EXCLUDED.avg_hps,
         avg_performance_score = EXCLUDED.avg_performance_score,
         performance_variance = EXCLUDED.performance_variance,
         consistency_rating = EXCLUDED.consistency_rating,
         stats_data = EXCLUDED.stats_data,
         updated_at = NOW()`,
      [
        id,
        stats.playerName,
        stats.totalSessions,
        stats.totalCombatTimeMs,
        stats.totalKills,
        stats.totalDeaths,
        stats.totalAssists,
        stats.overallKDR,
        stats.totalDamageDealt,
        stats.totalDamageTaken,
        stats.totalHealingDone,
        stats.totalHealingReceived,
        stats.avgDPS,
        stats.avgHPS,
        stats.avgPerformanceScore,
        stats.performanceVariance,
        stats.consistencyRating,
        statsData,
      ]
    );
  }

  async getPlayerSessionStats(
    playerName: string,
    sessionId: string
  ): Promise<PlayerSessionStats | null> {
    const pool = this.ensureConnected();
    const result = await pool.query(
      `SELECT * FROM ${this.tableName('player_session_stats')}
       WHERE player_name = $1 AND session_id = $2`,
      [playerName, sessionId]
    );

    if (!result.rows[0]) return null;
    return this.rowToPlayerSessionStats(result.rows[0]);
  }

  async getPlayerAggregateStats(
    playerName: string
  ): Promise<PlayerAggregateStats | null> {
    const pool = this.ensureConnected();
    const result = await pool.query(
      `SELECT * FROM ${this.tableName('player_aggregate_stats')} WHERE player_name = $1`,
      [playerName]
    );

    if (!result.rows[0]) return null;
    return this.rowToPlayerAggregateStats(result.rows[0]);
  }

  private rowToPlayerSessionStats(row: Record<string, unknown>): PlayerSessionStats {
    return {
      playerName: row.player_name as string,
      sessionId: row.session_id as string,
      sessionStart: new Date(row.session_start as string),
      sessionEnd: new Date(row.session_end as string),
      durationMs: row.duration_ms as number,
      role: row.role as string,
      kills: row.kills as number,
      deaths: row.deaths as number,
      assists: row.assists as number,
      kdr: row.kdr as number,
      damageDealt: row.damage_dealt as number,
      damageTaken: row.damage_taken as number,
      dps: row.dps as number,
      peakDps: row.peak_dps as number,
      healingDone: row.healing_done as number,
      healingReceived: row.healing_received as number,
      hps: row.hps as number,
      overhealRate: row.overheal_rate as number,
      critRate: row.crit_rate as number,
      performanceScore: row.performance_score as number,
      performanceRating: row.performance_rating as string,
    } as PlayerSessionStats;
  }

  private rowToPlayerAggregateStats(row: Record<string, unknown>): PlayerAggregateStats {
    const statsData = row.stats_data as Record<string, unknown>;

    return {
      playerName: row.player_name as string,
      totalSessions: row.total_sessions as number,
      totalCombatTimeMs: Number(row.total_combat_time_ms),
      totalKills: row.total_kills as number,
      totalDeaths: row.total_deaths as number,
      totalAssists: row.total_assists as number,
      overallKDR: row.overall_kdr as number,
      totalDamageDealt: Number(row.total_damage_dealt),
      totalDamageTaken: Number(row.total_damage_taken),
      totalHealingDone: Number(row.total_healing_done),
      totalHealingReceived: Number(row.total_healing_received),
      avgDPS: row.avg_dps as number,
      avgHPS: row.avg_hps as number,
      avgPerformanceScore: row.avg_performance_score as number,
      performanceVariance: row.performance_variance as number,
      consistencyRating: row.consistency_rating as string,
      avgKillsPerSession: statsData.avgKillsPerSession as number,
      avgDeathsPerSession: statsData.avgDeathsPerSession as number,
      bestFight: statsData.bestFight as PlayerSessionStats,
      worstFight: statsData.worstFight as PlayerSessionStats,
      dpsOverTime: statsData.dpsOverTime as any[],
      kdrOverTime: statsData.kdrOverTime as any[],
      performanceOverTime: statsData.performanceOverTime as any[],
      performanceDistribution: statsData.performanceDistribution as any,
    } as PlayerAggregateStats;
  }

  // ============================================================================
  // Query Builders
  // ============================================================================

  events(): EventQuery {
    return new PostgreSQLEventQuery(this);
  }

  sessions(): SessionQuery {
    return new PostgreSQLSessionQuery(this);
  }

  stats(): StatsQuery {
    return new PostgreSQLStatsQuery(this);
  }

  aggregations(): AggregationQuery {
    return new PostgreSQLAggregationQuery(this);
  }

  // ============================================================================
  // Maintenance Operations
  // ============================================================================

  async vacuum(): Promise<void> {
    const pool = this.ensureConnected();
    await pool.query('VACUUM ANALYZE');
  }

  async getTableRowCounts(): Promise<Record<string, number>> {
    const pool = this.ensureConnected();
    const tables = ['entities', 'sessions', 'events', 'participants', 'player_session_stats', 'player_aggregate_stats'];
    const counts: Record<string, number> = {};

    for (const table of tables) {
      const result = await pool.query(
        `SELECT COUNT(*) as count FROM ${this.tableName(table)}`
      );
      counts[table] = parseInt(result.rows[0].count, 10);
    }

    return counts;
  }

  async getOldestRecordDate(table: string): Promise<Date | null> {
    const pool = this.ensureConnected();
    const dateColumn = table === 'sessions' ? 'start_time' : 'created_at';

    const result = await pool.query(
      `SELECT MIN(${dateColumn}) as oldest FROM ${this.tableName(table)}`
    );

    return result.rows[0].oldest ? new Date(result.rows[0].oldest) : null;
  }

  async deleteRecordsOlderThan(
    table: string,
    date: Date,
    tx?: Transaction
  ): Promise<number> {
    const client = this.getClient(tx);
    const dateColumn = table === 'sessions' ? 'start_time' : 'created_at';

    const result = await client.query(
      `DELETE FROM ${this.tableName(table)} WHERE ${dateColumn} < $1`,
      [date]
    );

    return result.rowCount ?? 0;
  }

  async getRecordsOlderThan<T>(
    table: string,
    date: Date,
    limit?: number
  ): Promise<T[]> {
    const pool = this.ensureConnected();
    const dateColumn = table === 'sessions' ? 'start_time' : 'created_at';

    let sql = `SELECT * FROM ${this.tableName(table)} WHERE ${dateColumn} < $1`;
    if (limit) {
      sql += ` LIMIT ${limit}`;
    }

    const result = await pool.query(sql, [date]);
    return result.rows as T[];
  }

  // Internal methods for query builders
  _getPool(): Pool {
    return this.ensureConnected();
  }

  _getTablePrefix(): string {
    return this.tablePrefix;
  }
}

// ============================================================================
// PostgreSQL Query Builder Implementations
// ============================================================================

class PostgreSQLEventQuery implements EventQuery {
  private adapter: PostgreSQLAdapter;
  private conditions: string[] = [];
  private params: unknown[] = [];
  private paramIndex: number = 1;
  private _pagination: PaginationOptions = {};
  private _orderBy: { field: string; direction: 'asc' | 'desc' } | null = null;
  private _withEntities: boolean = false;

  constructor(adapter: PostgreSQLAdapter) {
    this.adapter = adapter;
  }

  byType(...types: EventType[]): this {
    if (types.length === 1) {
      this.conditions.push(`event_type = $${this.paramIndex++}`);
      this.params.push(types[0]);
    } else if (types.length > 1) {
      const placeholders = types.map(() => `$${this.paramIndex++}`).join(',');
      this.conditions.push(`event_type IN (${placeholders})`);
      this.params.push(...types);
    }
    return this;
  }

  inTimeRange(start: Date, end?: Date): this {
    this.conditions.push(`timestamp >= $${this.paramIndex++}`);
    this.params.push(start);
    if (end) {
      this.conditions.push(`timestamp <= $${this.paramIndex++}`);
      this.params.push(end);
    }
    return this;
  }

  inSession(sessionId: string): this {
    this.conditions.push(`session_id = $${this.paramIndex++}`);
    this.params.push(sessionId);
    return this;
  }

  fromEntity(entityName: string): this {
    this.conditions.push(
      `source_entity_id IN (SELECT id FROM ${this.adapter._getTablePrefix()}entities WHERE name = $${this.paramIndex++})`
    );
    this.params.push(entityName);
    return this;
  }

  toEntity(entityName: string): this {
    this.conditions.push(
      `target_entity_id IN (SELECT id FROM ${this.adapter._getTablePrefix()}entities WHERE name = $${this.paramIndex++})`
    );
    this.params.push(entityName);
    return this;
  }

  involvingEntity(entityName: string): this {
    const idx1 = this.paramIndex++;
    const idx2 = this.paramIndex++;
    this.conditions.push(
      `(source_entity_id IN (SELECT id FROM ${this.adapter._getTablePrefix()}entities WHERE name = $${idx1}) OR
        target_entity_id IN (SELECT id FROM ${this.adapter._getTablePrefix()}entities WHERE name = $${idx2}))`
    );
    this.params.push(entityName, entityName);
    return this;
  }

  withMinAmount(amount: number): this {
    this.conditions.push(`(event_data->>'amount')::numeric >= $${this.paramIndex++}`);
    this.params.push(amount);
    return this;
  }

  byActionType(...types: ActionType[]): this {
    if (types.length === 1) {
      this.conditions.push(`event_data->>'actionType' = $${this.paramIndex++}`);
      this.params.push(types[0]);
    } else if (types.length > 1) {
      const placeholders = types.map(() => `$${this.paramIndex++}`).join(',');
      this.conditions.push(`event_data->>'actionType' IN (${placeholders})`);
      this.params.push(...types);
    }
    return this;
  }

  byDamageType(...types: DamageType[]): this {
    if (types.length === 1) {
      this.conditions.push(`event_data->>'damageType' = $${this.paramIndex++}`);
      this.params.push(types[0]);
    } else if (types.length > 1) {
      const placeholders = types.map(() => `$${this.paramIndex++}`).join(',');
      this.conditions.push(`event_data->>'damageType' IN (${placeholders})`);
      this.params.push(...types);
    }
    return this;
  }

  criticalOnly(): this {
    this.conditions.push(`(event_data->>'isCritical')::boolean = true`);
    return this;
  }

  withEntities(): this {
    this._withEntities = true;
    return this;
  }

  paginate(options: PaginationOptions): this {
    this._pagination = options;
    return this;
  }

  orderBy(field: string, direction: 'asc' | 'desc' = 'asc'): this {
    this._orderBy = { field, direction };
    return this;
  }

  async execute(): Promise<CombatEvent[]> {
    const pool = this.adapter._getPool();
    let sql = `SELECT * FROM ${this.adapter._getTablePrefix()}events`;

    if (this.conditions.length > 0) {
      sql += ` WHERE ${this.conditions.join(' AND ')}`;
    }

    if (this._orderBy) {
      sql += ` ORDER BY ${this._orderBy.field} ${this._orderBy.direction.toUpperCase()}`;
    }

    if (this._pagination.limit) {
      sql += ` LIMIT ${this._pagination.limit}`;
    }

    if (this._pagination.offset) {
      sql += ` OFFSET ${this._pagination.offset}`;
    }

    const result = await pool.query(sql, this.params);

    const events: CombatEvent[] = [];
    for (const row of result.rows) {
      const event = await (this.adapter as any).rowToEvent(row);
      events.push(event);
    }

    return events;
  }

  async first(): Promise<CombatEvent | null> {
    this._pagination.limit = 1;
    const results = await this.execute();
    return results[0] ?? null;
  }

  async count(): Promise<number> {
    const pool = this.adapter._getPool();
    let sql = `SELECT COUNT(*) as count FROM ${this.adapter._getTablePrefix()}events`;

    if (this.conditions.length > 0) {
      sql += ` WHERE ${this.conditions.join(' AND ')}`;
    }

    const result = await pool.query(sql, this.params);
    return parseInt(result.rows[0].count, 10);
  }
}

class PostgreSQLSessionQuery implements SessionQuery {
  private adapter: PostgreSQLAdapter;
  private conditions: string[] = [];
  private params: unknown[] = [];
  private paramIndex: number = 1;
  private _pagination: PaginationOptions = {};
  private _orderBy: { field: string; direction: 'asc' | 'desc' } | null = null;
  private _includeEvents: boolean = false;
  private _includeParticipants: boolean = false;
  private _includeSummary: boolean = true;

  constructor(adapter: PostgreSQLAdapter) {
    this.adapter = adapter;
  }

  inTimeRange(start: Date, end?: Date): this {
    this.conditions.push(`start_time >= $${this.paramIndex++}`);
    this.params.push(start);
    if (end) {
      this.conditions.push(`start_time <= $${this.paramIndex++}`);
      this.params.push(end);
    }
    return this;
  }

  minDuration(ms: number): this {
    this.conditions.push(`duration_ms >= $${this.paramIndex++}`);
    this.params.push(ms);
    return this;
  }

  maxDuration(ms: number): this {
    this.conditions.push(`duration_ms <= $${this.paramIndex++}`);
    this.params.push(ms);
    return this;
  }

  withParticipant(entityName: string): this {
    this.conditions.push(
      `id IN (SELECT session_id FROM ${this.adapter._getTablePrefix()}participants p
              JOIN ${this.adapter._getTablePrefix()}entities e ON p.entity_id = e.id
              WHERE e.name = $${this.paramIndex++})`
    );
    this.params.push(entityName);
    return this;
  }

  minParticipants(count: number): this {
    this.conditions.push(
      `(SELECT COUNT(*) FROM ${this.adapter._getTablePrefix()}participants WHERE session_id = ${this.adapter._getTablePrefix()}sessions.id) >= $${this.paramIndex++}`
    );
    this.params.push(count);
    return this;
  }

  withEvents(): this {
    this._includeEvents = true;
    return this;
  }

  withParticipants(): this {
    this._includeParticipants = true;
    return this;
  }

  withSummary(): this {
    this._includeSummary = true;
    return this;
  }

  paginate(options: PaginationOptions): this {
    this._pagination = options;
    return this;
  }

  orderBy(field: string, direction: 'asc' | 'desc' = 'asc'): this {
    this._orderBy = { field, direction };
    return this;
  }

  async execute(): Promise<CombatSession[]> {
    const pool = this.adapter._getPool();
    let sql = `SELECT * FROM ${this.adapter._getTablePrefix()}sessions`;

    if (this.conditions.length > 0) {
      sql += ` WHERE ${this.conditions.join(' AND ')}`;
    }

    if (this._orderBy) {
      sql += ` ORDER BY ${this._orderBy.field} ${this._orderBy.direction.toUpperCase()}`;
    }

    if (this._pagination.limit) {
      sql += ` LIMIT ${this._pagination.limit}`;
    }

    if (this._pagination.offset) {
      sql += ` OFFSET ${this._pagination.offset}`;
    }

    const result = await pool.query(sql, this.params);

    const sessions: CombatSession[] = [];
    for (const row of result.rows) {
      const session: CombatSession = {
        id: row.id,
        startTime: new Date(row.start_time),
        endTime: new Date(row.end_time),
        durationMs: row.duration_ms,
        summary: this._includeSummary ? row.summary_data : {} as any,
        events: [],
        participants: [],
      };

      if (this._includeParticipants) {
        session.participants = await this.adapter.getParticipantsBySession(session.id);
      }

      if (this._includeEvents) {
        const eventResult = await pool.query(
          `SELECT * FROM ${this.adapter._getTablePrefix()}events WHERE session_id = $1 ORDER BY timestamp`,
          [session.id]
        );
        for (const eventRow of eventResult.rows) {
          const event = await (this.adapter as any).rowToEvent(eventRow);
          session.events.push(event);
        }
      }

      sessions.push(session);
    }

    return sessions;
  }

  async first(): Promise<CombatSession | null> {
    this._pagination.limit = 1;
    const results = await this.execute();
    return results[0] ?? null;
  }

  async count(): Promise<number> {
    const pool = this.adapter._getPool();
    let sql = `SELECT COUNT(*) as count FROM ${this.adapter._getTablePrefix()}sessions`;

    if (this.conditions.length > 0) {
      sql += ` WHERE ${this.conditions.join(' AND ')}`;
    }

    const result = await pool.query(sql, this.params);
    return parseInt(result.rows[0].count, 10);
  }
}

class PostgreSQLPlayerSessionStatsQuery implements PlayerSessionStatsQuery {
  private adapter: PostgreSQLAdapter;
  private conditions: string[] = [];
  private params: unknown[] = [];
  private paramIndex: number = 1;
  private _pagination: PaginationOptions = {};
  private _orderBy: { field: string; direction: 'asc' | 'desc' } | null = null;

  constructor(adapter: PostgreSQLAdapter, playerName: string) {
    this.adapter = adapter;
    this.conditions.push(`player_name = $${this.paramIndex++}`);
    this.params.push(playerName);
  }

  inTimeRange(start: Date, end?: Date): this {
    this.conditions.push(`session_start >= $${this.paramIndex++}`);
    this.params.push(start);
    if (end) {
      this.conditions.push(`session_start <= $${this.paramIndex++}`);
      this.params.push(end);
    }
    return this;
  }

  byRole(role: string): this {
    this.conditions.push(`role = $${this.paramIndex++}`);
    this.params.push(role);
    return this;
  }

  minPerformance(score: number): this {
    this.conditions.push(`performance_score >= $${this.paramIndex++}`);
    this.params.push(score);
    return this;
  }

  paginate(options: PaginationOptions): this {
    this._pagination = options;
    return this;
  }

  orderBy(field: string, direction: 'asc' | 'desc' = 'asc'): this {
    this._orderBy = { field, direction };
    return this;
  }

  async execute(): Promise<PlayerSessionStats[]> {
    const pool = this.adapter._getPool();
    let sql = `SELECT * FROM ${this.adapter._getTablePrefix()}player_session_stats`;

    if (this.conditions.length > 0) {
      sql += ` WHERE ${this.conditions.join(' AND ')}`;
    }

    if (this._orderBy) {
      sql += ` ORDER BY ${this._orderBy.field} ${this._orderBy.direction.toUpperCase()}`;
    }

    if (this._pagination.limit) {
      sql += ` LIMIT ${this._pagination.limit}`;
    }

    if (this._pagination.offset) {
      sql += ` OFFSET ${this._pagination.offset}`;
    }

    const result = await pool.query(sql, this.params);
    return result.rows.map((row) => (this.adapter as any).rowToPlayerSessionStats(row));
  }

  async first(): Promise<PlayerSessionStats | null> {
    this._pagination.limit = 1;
    const results = await this.execute();
    return results[0] ?? null;
  }

  async count(): Promise<number> {
    const pool = this.adapter._getPool();
    let sql = `SELECT COUNT(*) as count FROM ${this.adapter._getTablePrefix()}player_session_stats`;

    if (this.conditions.length > 0) {
      sql += ` WHERE ${this.conditions.join(' AND ')}`;
    }

    const result = await pool.query(sql, this.params);
    return parseInt(result.rows[0].count, 10);
  }
}

class PostgreSQLStatsQuery implements StatsQuery {
  private adapter: PostgreSQLAdapter;

  constructor(adapter: PostgreSQLAdapter) {
    this.adapter = adapter;
  }

  playerSessions(playerName: string): PlayerSessionStatsQuery {
    return new PostgreSQLPlayerSessionStatsQuery(this.adapter, playerName);
  }

  async playerAggregate(playerName: string): Promise<PlayerAggregateStats | null> {
    return this.adapter.getPlayerAggregateStats(playerName);
  }

  async allPlayerAggregates(): Promise<PlayerAggregateStats[]> {
    const pool = this.adapter._getPool();
    const result = await pool.query(
      `SELECT * FROM ${this.adapter._getTablePrefix()}player_aggregate_stats`
    );
    return result.rows.map((row) => (this.adapter as any).rowToPlayerAggregateStats(row));
  }

  async topPerformers(
    metric: 'damage' | 'healing' | 'kills' | 'kdr' | 'performance',
    limit: number = 10
  ): Promise<PlayerAggregateStats[]> {
    const pool = this.adapter._getPool();
    const columnMap: Record<string, string> = {
      damage: 'total_damage_dealt',
      healing: 'total_healing_done',
      kills: 'total_kills',
      kdr: 'overall_kdr',
      performance: 'avg_performance_score',
    };

    const column = columnMap[metric];
    const result = await pool.query(
      `SELECT * FROM ${this.adapter._getTablePrefix()}player_aggregate_stats
       ORDER BY ${column} DESC LIMIT $1`,
      [limit]
    );

    return result.rows.map((row) => (this.adapter as any).rowToPlayerAggregateStats(row));
  }
}

class PostgreSQLAggregationQuery implements AggregationQuery {
  private adapter: PostgreSQLAdapter;

  constructor(adapter: PostgreSQLAdapter) {
    this.adapter = adapter;
  }

  async eventsByTimeBucket(
    bucketSize: TimeBucket,
    start?: Date,
    end?: Date
  ): Promise<TimeBucketResult[]> {
    const pool = this.adapter._getPool();

    const bucketExpr: Record<TimeBucket, string> = {
      hour: `date_trunc('hour', timestamp)`,
      day: `date_trunc('day', timestamp)`,
      week: `date_trunc('week', timestamp)`,
      month: `date_trunc('month', timestamp)`,
    };

    const params: unknown[] = [];
    const conditions: string[] = [];
    let paramIndex = 1;

    if (start) {
      conditions.push(`timestamp >= $${paramIndex++}`);
      params.push(start);
    }
    if (end) {
      conditions.push(`timestamp <= $${paramIndex++}`);
      params.push(end);
    }

    let sql = `
      SELECT
        ${bucketExpr[bucketSize]} as bucket,
        COUNT(*) as event_count,
        COALESCE(SUM(CASE WHEN event_type IN ('DAMAGE_DEALT', 'DAMAGE_RECEIVED')
            THEN (event_data->>'amount')::numeric ELSE 0 END), 0) as total_damage,
        COALESCE(SUM(CASE WHEN event_type IN ('HEALING_DONE', 'HEALING_RECEIVED')
            THEN (event_data->>'amount')::numeric ELSE 0 END), 0) as total_healing,
        COUNT(DISTINCT session_id) as session_count
      FROM ${this.adapter._getTablePrefix()}events
    `;

    if (conditions.length > 0) {
      sql += ` WHERE ${conditions.join(' AND ')}`;
    }

    sql += ` GROUP BY ${bucketExpr[bucketSize]} ORDER BY bucket`;

    const result = await pool.query(sql, params);

    return result.rows.map((row) => ({
      bucket: new Date(row.bucket),
      eventCount: parseInt(row.event_count, 10),
      totalDamage: parseFloat(row.total_damage) || 0,
      totalHealing: parseFloat(row.total_healing) || 0,
      sessionCount: parseInt(row.session_count, 10),
    }));
  }

  async damageByEntity(sessionId?: string): Promise<EntityAggregateResult[]> {
    const pool = this.adapter._getPool();

    let sql = `
      SELECT
        e.name as entity_name,
        COALESCE(SUM((ev.event_data->>'amount')::numeric), 0) as total_amount,
        COUNT(*) as event_count,
        COALESCE(AVG((ev.event_data->>'amount')::numeric), 0) as average_amount,
        COALESCE(MAX((ev.event_data->>'amount')::numeric), 0) as peak_amount
      FROM ${this.adapter._getTablePrefix()}events ev
      JOIN ${this.adapter._getTablePrefix()}entities e ON ev.source_entity_id = e.id
      WHERE ev.event_type = 'DAMAGE_DEALT'
    `;

    const params: unknown[] = [];
    if (sessionId) {
      sql += ` AND ev.session_id = $1`;
      params.push(sessionId);
    }

    sql += ` GROUP BY e.name ORDER BY total_amount DESC`;

    const result = await pool.query(sql, params);

    return result.rows.map((row) => ({
      entityName: row.entity_name,
      totalAmount: parseFloat(row.total_amount) || 0,
      eventCount: parseInt(row.event_count, 10),
      averageAmount: parseFloat(row.average_amount) || 0,
      peakAmount: parseFloat(row.peak_amount) || 0,
    }));
  }

  async healingByEntity(sessionId?: string): Promise<EntityAggregateResult[]> {
    const pool = this.adapter._getPool();

    let sql = `
      SELECT
        e.name as entity_name,
        COALESCE(SUM((ev.event_data->>'amount')::numeric), 0) as total_amount,
        COUNT(*) as event_count,
        COALESCE(AVG((ev.event_data->>'amount')::numeric), 0) as average_amount,
        COALESCE(MAX((ev.event_data->>'amount')::numeric), 0) as peak_amount
      FROM ${this.adapter._getTablePrefix()}events ev
      JOIN ${this.adapter._getTablePrefix()}entities e ON ev.source_entity_id = e.id
      WHERE ev.event_type = 'HEALING_DONE'
    `;

    const params: unknown[] = [];
    if (sessionId) {
      sql += ` AND ev.session_id = $1`;
      params.push(sessionId);
    }

    sql += ` GROUP BY e.name ORDER BY total_amount DESC`;

    const result = await pool.query(sql, params);

    return result.rows.map((row) => ({
      entityName: row.entity_name,
      totalAmount: parseFloat(row.total_amount) || 0,
      eventCount: parseInt(row.event_count, 10),
      averageAmount: parseFloat(row.average_amount) || 0,
      peakAmount: parseFloat(row.peak_amount) || 0,
    }));
  }

  async eventTypeDistribution(
    start?: Date,
    end?: Date
  ): Promise<Record<string, number>> {
    const pool = this.adapter._getPool();

    const params: unknown[] = [];
    const conditions: string[] = [];
    let paramIndex = 1;

    if (start) {
      conditions.push(`timestamp >= $${paramIndex++}`);
      params.push(start);
    }
    if (end) {
      conditions.push(`timestamp <= $${paramIndex++}`);
      params.push(end);
    }

    let sql = `
      SELECT event_type, COUNT(*) as count
      FROM ${this.adapter._getTablePrefix()}events
    `;

    if (conditions.length > 0) {
      sql += ` WHERE ${conditions.join(' AND ')}`;
    }

    sql += ` GROUP BY event_type`;

    const result = await pool.query(sql, params);

    const distribution: Record<string, number> = {};
    for (const row of result.rows) {
      distribution[row.event_type] = parseInt(row.count, 10);
    }

    return distribution;
  }
}
