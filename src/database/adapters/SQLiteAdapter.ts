/**
 * SQLiteAdapter - SQLite database implementation
 *
 * Uses better-sqlite3 for synchronous, high-performance SQLite access.
 */
import Database from 'better-sqlite3';
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
  SQLiteConnectionConfig,
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
import { DEFAULT_SQLITE_CONFIG } from '../types.js';
import {
  ConnectionError,
  NotConnectedError,
  QueryError,
  MigrationError,
  TransactionError,
} from '../errors.js';
import {
  ALL_MIGRATIONS,
  getMigrationsFrom,
  getMigrationsToRollback,
  applyPrefix,
} from '../schema/migrations.js';

/**
 * SQLite transaction implementation
 */
class SQLiteTransaction implements Transaction {
  readonly id: string;
  private _isActive: boolean = true;
  private db: Database.Database;

  constructor(db: Database.Database) {
    this.id = uuidv4();
    this.db = db;
    this.db.exec('BEGIN TRANSACTION');
  }

  async commit(): Promise<void> {
    if (!this._isActive) {
      throw new TransactionError('Transaction already completed', this.id);
    }
    this.db.exec('COMMIT');
    this._isActive = false;
  }

  async rollback(): Promise<void> {
    if (!this._isActive) {
      return; // Already rolled back or committed
    }
    this.db.exec('ROLLBACK');
    this._isActive = false;
  }

  isActive(): boolean {
    return this._isActive;
  }
}

/**
 * SQLite database adapter
 */
export class SQLiteAdapter extends DatabaseAdapter {
  readonly backend = 'sqlite' as const;
  private db: Database.Database | null = null;
  private config: SQLiteConnectionConfig;
  private entityCache = new Map<string, string>(); // name -> id

  constructor(config: Partial<SQLiteConnectionConfig> & { tablePrefix?: string } = {}) {
    super();
    const { tablePrefix, ...connectionConfig } = config;
    this.config = { ...DEFAULT_SQLITE_CONFIG, ...connectionConfig } as SQLiteConnectionConfig;
    if (tablePrefix) {
      this.tablePrefix = tablePrefix;
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
      this.db = new Database(this.config.filepath, {
        timeout: this.config.timeout,
      });

      // Enable WAL mode for better concurrency
      if (this.config.walMode) {
        this.db.pragma('journal_mode = WAL');
      }

      // Enable foreign keys
      this.db.pragma('foreign_keys = ON');

      this._isConnected = true;
      this.emit('connected');
    } catch (error) {
      throw new ConnectionError(
        `Failed to connect to SQLite: ${(error as Error).message}`,
        'sqlite',
        error as Error
      );
    }
  }

  async disconnect(): Promise<void> {
    if (!this._isConnected || !this.db) {
      return;
    }

    try {
      this.db.close();
      this.db = null;
      this._isConnected = false;
      this.entityCache.clear();
      this.emit('disconnected');
    } catch (error) {
      throw new ConnectionError(
        `Failed to disconnect from SQLite: ${(error as Error).message}`,
        'sqlite',
        error as Error
      );
    }
  }

  private ensureConnected(): Database.Database {
    if (!this._isConnected || !this.db) {
      throw new NotConnectedError();
    }
    return this.db;
  }

  // ============================================================================
  // Transaction Support
  // ============================================================================

  async beginTransaction(): Promise<Transaction> {
    const db = this.ensureConnected();
    return new SQLiteTransaction(db);
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

  // ============================================================================
  // Migration Support
  // ============================================================================

  async getCurrentMigrationVersion(): Promise<number> {
    const db = this.ensureConnected();

    // Check if migrations table exists
    const tableExists = db
      .prepare(
        `SELECT name FROM sqlite_master WHERE type='table' AND name=?`
      )
      .get(this.tableName('migrations'));

    if (!tableExists) {
      return 0;
    }

    const result = db
      .prepare(`SELECT MAX(version) as version FROM ${this.tableName('migrations')}`)
      .get() as { version: number | null };

    return result?.version ?? 0;
  }

  async runMigrations(): Promise<void> {
    const db = this.ensureConnected();
    const currentVersion = await this.getCurrentMigrationVersion();
    const migrationsToRun = getMigrationsFrom(currentVersion);

    for (const migration of migrationsToRun) {
      this.emit('migration:start', migration.version);

      try {
        // Run all up statements
        for (const sql of migration.up) {
          const prefixedSql = applyPrefix(sql, this.tablePrefix);
          db.exec(prefixedSql);
        }

        // Record migration (skip for version 1 since migrations table is created in that migration)
        if (migration.version === 1) {
          // Table was just created, now insert
          db.prepare(
            `INSERT INTO ${this.tableName('migrations')} (version, name) VALUES (?, ?)`
          ).run(migration.version, migration.name);
        } else {
          db.prepare(
            `INSERT INTO ${this.tableName('migrations')} (version, name) VALUES (?, ?)`
          ).run(migration.version, migration.name);
        }

        this.emit('migration:complete', migration.version);
      } catch (error) {
        throw new MigrationError(
          `Failed to run migration ${migration.name}: ${(error as Error).message}`,
          migration.version,
          migration.name,
          error as Error
        );
      }
    }
  }

  async rollbackMigration(targetVersion: number): Promise<void> {
    const db = this.ensureConnected();
    const currentVersion = await this.getCurrentMigrationVersion();
    const migrationsToRollback = getMigrationsToRollback(
      currentVersion,
      targetVersion
    );

    for (const migration of migrationsToRollback) {
      try {
        // Run all down statements
        for (const sql of migration.down) {
          const prefixedSql = applyPrefix(sql, this.tablePrefix);
          db.exec(prefixedSql);
        }

        // Remove migration record
        db.prepare(
          `DELETE FROM ${this.tableName('migrations')} WHERE version = ?`
        ).run(migration.version);
      } catch (error) {
        throw new MigrationError(
          `Failed to rollback migration ${migration.name}: ${(error as Error).message}`,
          migration.version,
          migration.name,
          error as Error
        );
      }
    }
  }

  // ============================================================================
  // Entity CRUD
  // ============================================================================

  async insertEntity(entity: Entity, _tx?: Transaction): Promise<string> {
    const db = this.ensureConnected();
    const id = uuidv4();
    const now = new Date().toISOString();

    try {
      db.prepare(
        `INSERT INTO ${this.tableName('entities')}
         (id, name, entity_type, realm, is_player, is_self, first_seen, last_seen)
         VALUES (?, ?, ?, ?, ?, ?, ?, ?)`
      ).run(
        id,
        entity.name,
        entity.entityType,
        entity.realm ?? null,
        entity.isPlayer ? 1 : 0,
        entity.isSelf ? 1 : 0,
        now,
        now
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
    _tx?: Transaction
  ): Promise<void> {
    const db = this.ensureConnected();
    const updates: string[] = [];
    const values: unknown[] = [];

    if (entity.name !== undefined) {
      updates.push('name = ?');
      values.push(entity.name);
    }
    if (entity.entityType !== undefined) {
      updates.push('entity_type = ?');
      values.push(entity.entityType);
    }
    if (entity.realm !== undefined) {
      updates.push('realm = ?');
      values.push(entity.realm);
    }
    if (entity.isPlayer !== undefined) {
      updates.push('is_player = ?');
      values.push(entity.isPlayer ? 1 : 0);
    }
    if (entity.isSelf !== undefined) {
      updates.push('is_self = ?');
      values.push(entity.isSelf ? 1 : 0);
    }

    updates.push('last_seen = ?');
    values.push(new Date().toISOString());
    values.push(id);

    if (updates.length > 1) {
      db.prepare(
        `UPDATE ${this.tableName('entities')} SET ${updates.join(', ')} WHERE id = ?`
      ).run(...values);
    }
  }

  async getEntityById(id: string): Promise<Entity | null> {
    const db = this.ensureConnected();
    const row = db
      .prepare(`SELECT * FROM ${this.tableName('entities')} WHERE id = ?`)
      .get(id) as Record<string, unknown> | undefined;

    return row ? this.rowToEntity(row) : null;
  }

  async getEntityByName(name: string): Promise<Entity | null> {
    const db = this.ensureConnected();
    const row = db
      .prepare(`SELECT * FROM ${this.tableName('entities')} WHERE name = ?`)
      .get(name) as Record<string, unknown> | undefined;

    return row ? this.rowToEntity(row) : null;
  }

  async findOrCreateEntity(entity: Entity, tx?: Transaction): Promise<string> {
    // Check cache first
    if (this.entityCache.has(entity.name)) {
      return this.entityCache.get(entity.name)!;
    }

    // Check database
    const db = this.ensureConnected();
    const row = db
      .prepare(`SELECT id FROM ${this.tableName('entities')} WHERE name = ?`)
      .get(entity.name) as { id: string } | undefined;

    if (row) {
      this.entityCache.set(entity.name, row.id);
      // Update last_seen
      db.prepare(
        `UPDATE ${this.tableName('entities')} SET last_seen = ? WHERE id = ?`
      ).run(new Date().toISOString(), row.id);
      return row.id;
    }

    // Create new
    return this.insertEntity(entity, tx);
  }

  private rowToEntity(row: Record<string, unknown>): Entity {
    return {
      name: row.name as string,
      entityType: row.entity_type as string,
      realm: row.realm as string | undefined,
      isPlayer: Boolean(row.is_player),
      isSelf: Boolean(row.is_self),
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
    const db = this.ensureConnected();

    // Find or create entities
    let sourceEntityId: string | null = null;
    let targetEntityId: string | null = null;

    if ('source' in event && event.source) {
      sourceEntityId = await this.findOrCreateEntity(event.source as Entity, tx);
    }
    if ('target' in event && event.target) {
      targetEntityId = await this.findOrCreateEntity(event.target as Entity, tx);
    }

    // Serialize type-specific data
    const eventData = this.serializeEventData(event);

    try {
      db.prepare(
        `INSERT INTO ${this.tableName('events')}
         (id, session_id, event_type, timestamp, raw_timestamp, raw_line, line_number,
          source_entity_id, target_entity_id, event_data)
         VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?)`
      ).run(
        event.id,
        sessionId ?? null,
        event.eventType,
        event.timestamp.toISOString(),
        event.rawTimestamp,
        event.rawLine,
        event.lineNumber,
        sourceEntityId,
        targetEntityId,
        eventData
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
    const db = this.ensureConnected();
    const row = db
      .prepare(`SELECT * FROM ${this.tableName('events')} WHERE id = ?`)
      .get(id) as Record<string, unknown> | undefined;

    if (!row) return null;

    return this.rowToEvent(row);
  }

  async deleteEvent(id: string, _tx?: Transaction): Promise<boolean> {
    const db = this.ensureConnected();
    const result = db
      .prepare(`DELETE FROM ${this.tableName('events')} WHERE id = ?`)
      .run(id);
    return result.changes > 0;
  }

  async deleteEvents(ids: string[], _tx?: Transaction): Promise<number> {
    if (ids.length === 0) return 0;

    const db = this.ensureConnected();
    const placeholders = ids.map(() => '?').join(',');
    const result = db
      .prepare(
        `DELETE FROM ${this.tableName('events')} WHERE id IN (${placeholders})`
      )
      .run(...ids);
    return result.changes;
  }

  async updateEventSession(
    eventId: string,
    sessionId: string,
    _tx?: Transaction
  ): Promise<void> {
    const db = this.ensureConnected();
    db.prepare(
      `UPDATE ${this.tableName('events')} SET session_id = ? WHERE id = ?`
    ).run(sessionId, eventId);
  }

  private serializeEventData(event: CombatEvent): string {
    const data: Record<string, unknown> = {};

    // Extract type-specific fields
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

    return JSON.stringify(data);
  }

  private async rowToEvent(row: Record<string, unknown>): Promise<CombatEvent> {
    const eventData = JSON.parse(row.event_data as string);

    // Get entities
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

    // Reconstruct based on event type
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

    // Unknown event
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
    _tx?: Transaction
  ): Promise<string> {
    const db = this.ensureConnected();

    try {
      db.prepare(
        `INSERT INTO ${this.tableName('sessions')}
         (id, start_time, end_time, duration_ms, summary_data)
         VALUES (?, ?, ?, ?, ?)`
      ).run(
        session.id,
        session.startTime.toISOString(),
        session.endTime.toISOString(),
        session.durationMs,
        JSON.stringify(session.summary)
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
    _tx?: Transaction
  ): Promise<void> {
    const db = this.ensureConnected();
    const updates: string[] = [];
    const values: unknown[] = [];

    if (session.startTime !== undefined) {
      updates.push('start_time = ?');
      values.push(session.startTime.toISOString());
    }
    if (session.endTime !== undefined) {
      updates.push('end_time = ?');
      values.push(session.endTime.toISOString());
    }
    if (session.durationMs !== undefined) {
      updates.push('duration_ms = ?');
      values.push(session.durationMs);
    }
    if (session.summary !== undefined) {
      updates.push('summary_data = ?');
      values.push(JSON.stringify(session.summary));
    }

    if (updates.length > 0) {
      values.push(id);
      db.prepare(
        `UPDATE ${this.tableName('sessions')} SET ${updates.join(', ')} WHERE id = ?`
      ).run(...values);
    }
  }

  async getSessionById(id: string): Promise<CombatSession | null> {
    const db = this.ensureConnected();
    const row = db
      .prepare(`SELECT * FROM ${this.tableName('sessions')} WHERE id = ?`)
      .get(id) as Record<string, unknown> | undefined;

    if (!row) return null;

    // Get participants
    const participants = await this.getParticipantsBySession(id);

    // Get events
    const eventRows = db
      .prepare(
        `SELECT * FROM ${this.tableName('events')} WHERE session_id = ? ORDER BY timestamp`
      )
      .all(id) as Record<string, unknown>[];

    const events = await Promise.all(eventRows.map((r) => this.rowToEvent(r)));

    return {
      id: row.id as string,
      startTime: new Date(row.start_time as string),
      endTime: new Date(row.end_time as string),
      durationMs: row.duration_ms as number,
      summary: JSON.parse(row.summary_data as string),
      events,
      participants,
    };
  }

  async deleteSession(id: string, _tx?: Transaction): Promise<boolean> {
    const db = this.ensureConnected();
    const result = db
      .prepare(`DELETE FROM ${this.tableName('sessions')} WHERE id = ?`)
      .run(id);
    return result.changes > 0;
  }

  // ============================================================================
  // Participant CRUD
  // ============================================================================

  async insertParticipant(
    sessionId: string,
    participant: SessionParticipant,
    tx?: Transaction
  ): Promise<string> {
    const db = this.ensureConnected();
    const id = uuidv4();

    // Find or create entity
    const entityId = await this.findOrCreateEntity(participant.entity, tx);

    try {
      db.prepare(
        `INSERT INTO ${this.tableName('participants')}
         (id, session_id, entity_id, role, first_seen, last_seen, event_count)
         VALUES (?, ?, ?, ?, ?, ?, ?)`
      ).run(
        id,
        sessionId,
        entityId,
        participant.role,
        participant.firstSeen.toISOString(),
        participant.lastSeen.toISOString(),
        participant.eventCount
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
    const db = this.ensureConnected();
    const rows = db
      .prepare(
        `SELECT p.*, e.name, e.entity_type, e.realm, e.is_player, e.is_self
         FROM ${this.tableName('participants')} p
         JOIN ${this.tableName('entities')} e ON p.entity_id = e.id
         WHERE p.session_id = ?`
      )
      .all(sessionId) as Record<string, unknown>[];

    return rows.map((row) => ({
      entity: {
        name: row.name as string,
        entityType: row.entity_type as string,
        realm: row.realm as string | undefined,
        isPlayer: Boolean(row.is_player),
        isSelf: Boolean(row.is_self),
      } as Entity,
      role: row.role as string,
      firstSeen: new Date(row.first_seen as string),
      lastSeen: new Date(row.last_seen as string),
      eventCount: row.event_count as number,
    })) as SessionParticipant[];
  }

  // ============================================================================
  // Player Stats CRUD
  // ============================================================================

  async insertPlayerSessionStats(
    stats: PlayerSessionStats,
    _tx?: Transaction
  ): Promise<string> {
    const db = this.ensureConnected();
    const id = uuidv4();

    try {
      db.prepare(
        `INSERT INTO ${this.tableName('player_session_stats')}
         (id, player_name, session_id, session_start, session_end, duration_ms, role,
          kills, deaths, assists, kdr, damage_dealt, damage_taken, dps, peak_dps,
          healing_done, healing_received, hps, overheal_rate, crit_rate,
          performance_score, performance_rating)
         VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)`
      ).run(
        id,
        stats.playerName,
        stats.sessionId,
        stats.sessionStart.toISOString(),
        stats.sessionEnd.toISOString(),
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
        stats.performanceRating
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
    _tx?: Transaction
  ): Promise<void> {
    const db = this.ensureConnected();

    // Check if exists
    const existing = db
      .prepare(
        `SELECT id FROM ${this.tableName('player_aggregate_stats')} WHERE player_name = ?`
      )
      .get(stats.playerName) as { id: string } | undefined;

    const statsData = JSON.stringify({
      bestFight: stats.bestFight,
      worstFight: stats.worstFight,
      dpsOverTime: stats.dpsOverTime,
      kdrOverTime: stats.kdrOverTime,
      performanceOverTime: stats.performanceOverTime,
      performanceDistribution: stats.performanceDistribution,
      avgKillsPerSession: stats.avgKillsPerSession,
      avgDeathsPerSession: stats.avgDeathsPerSession,
    });

    if (existing) {
      db.prepare(
        `UPDATE ${this.tableName('player_aggregate_stats')} SET
         total_sessions = ?, total_combat_time_ms = ?, total_kills = ?, total_deaths = ?,
         total_assists = ?, overall_kdr = ?, total_damage_dealt = ?, total_damage_taken = ?,
         total_healing_done = ?, total_healing_received = ?, avg_dps = ?, avg_hps = ?,
         avg_performance_score = ?, performance_variance = ?, consistency_rating = ?,
         stats_data = ?, updated_at = ?
         WHERE id = ?`
      ).run(
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
        new Date().toISOString(),
        existing.id
      );
    } else {
      const id = uuidv4();
      db.prepare(
        `INSERT INTO ${this.tableName('player_aggregate_stats')}
         (id, player_name, total_sessions, total_combat_time_ms, total_kills, total_deaths,
          total_assists, overall_kdr, total_damage_dealt, total_damage_taken,
          total_healing_done, total_healing_received, avg_dps, avg_hps,
          avg_performance_score, performance_variance, consistency_rating, stats_data)
         VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)`
      ).run(
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
        statsData
      );
    }
  }

  async getPlayerSessionStats(
    playerName: string,
    sessionId: string
  ): Promise<PlayerSessionStats | null> {
    const db = this.ensureConnected();
    const row = db
      .prepare(
        `SELECT * FROM ${this.tableName('player_session_stats')}
         WHERE player_name = ? AND session_id = ?`
      )
      .get(playerName, sessionId) as Record<string, unknown> | undefined;

    if (!row) return null;

    return this.rowToPlayerSessionStats(row);
  }

  async getPlayerAggregateStats(
    playerName: string
  ): Promise<PlayerAggregateStats | null> {
    const db = this.ensureConnected();
    const row = db
      .prepare(
        `SELECT * FROM ${this.tableName('player_aggregate_stats')} WHERE player_name = ?`
      )
      .get(playerName) as Record<string, unknown> | undefined;

    if (!row) return null;

    return this.rowToPlayerAggregateStats(row);
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
    const statsData = JSON.parse(row.stats_data as string);

    return {
      playerName: row.player_name as string,
      totalSessions: row.total_sessions as number,
      totalCombatTimeMs: row.total_combat_time_ms as number,
      totalKills: row.total_kills as number,
      totalDeaths: row.total_deaths as number,
      totalAssists: row.total_assists as number,
      overallKDR: row.overall_kdr as number,
      totalDamageDealt: row.total_damage_dealt as number,
      totalDamageTaken: row.total_damage_taken as number,
      totalHealingDone: row.total_healing_done as number,
      totalHealingReceived: row.total_healing_received as number,
      avgDPS: row.avg_dps as number,
      avgHPS: row.avg_hps as number,
      avgPerformanceScore: row.avg_performance_score as number,
      performanceVariance: row.performance_variance as number,
      consistencyRating: row.consistency_rating as string,
      avgKillsPerSession: statsData.avgKillsPerSession,
      avgDeathsPerSession: statsData.avgDeathsPerSession,
      bestFight: statsData.bestFight,
      worstFight: statsData.worstFight,
      dpsOverTime: statsData.dpsOverTime,
      kdrOverTime: statsData.kdrOverTime,
      performanceOverTime: statsData.performanceOverTime,
      performanceDistribution: statsData.performanceDistribution,
    } as PlayerAggregateStats;
  }

  // ============================================================================
  // Query Builders
  // ============================================================================

  events(): EventQuery {
    return new SQLiteEventQuery(this);
  }

  sessions(): SessionQuery {
    return new SQLiteSessionQuery(this);
  }

  stats(): StatsQuery {
    return new SQLiteStatsQuery(this);
  }

  aggregations(): AggregationQuery {
    return new SQLiteAggregationQuery(this);
  }

  // ============================================================================
  // Maintenance Operations
  // ============================================================================

  async vacuum(): Promise<void> {
    const db = this.ensureConnected();
    db.exec('VACUUM');
  }

  async getTableRowCounts(): Promise<Record<string, number>> {
    const db = this.ensureConnected();
    const tables = ['entities', 'sessions', 'events', 'participants', 'player_session_stats', 'player_aggregate_stats'];
    const counts: Record<string, number> = {};

    for (const table of tables) {
      const result = db
        .prepare(`SELECT COUNT(*) as count FROM ${this.tableName(table)}`)
        .get() as { count: number };
      counts[table] = result.count;
    }

    return counts;
  }

  async getOldestRecordDate(table: string): Promise<Date | null> {
    const db = this.ensureConnected();
    const dateColumn = table === 'sessions' ? 'start_time' : 'created_at';

    const result = db
      .prepare(`SELECT MIN(${dateColumn}) as oldest FROM ${this.tableName(table)}`)
      .get() as { oldest: string | null };

    return result.oldest ? new Date(result.oldest) : null;
  }

  async deleteRecordsOlderThan(
    table: string,
    date: Date,
    _tx?: Transaction
  ): Promise<number> {
    const db = this.ensureConnected();
    const dateColumn = table === 'sessions' ? 'start_time' : 'created_at';

    const result = db
      .prepare(
        `DELETE FROM ${this.tableName(table)} WHERE ${dateColumn} < ?`
      )
      .run(date.toISOString());

    return result.changes;
  }

  async getRecordsOlderThan<T>(
    table: string,
    date: Date,
    limit?: number
  ): Promise<T[]> {
    const db = this.ensureConnected();
    const dateColumn = table === 'sessions' ? 'start_time' : 'created_at';

    let sql = `SELECT * FROM ${this.tableName(table)} WHERE ${dateColumn} < ?`;
    if (limit) {
      sql += ` LIMIT ${limit}`;
    }

    return db.prepare(sql).all(date.toISOString()) as T[];
  }

  // Internal method for queries to access the database
  _getDb(): Database.Database {
    return this.ensureConnected();
  }

  _getTablePrefix(): string {
    return this.tablePrefix;
  }
}

// ============================================================================
// Query Builder Implementations
// ============================================================================

class SQLiteEventQuery implements EventQuery {
  private adapter: SQLiteAdapter;
  private conditions: string[] = [];
  private params: unknown[] = [];
  private _pagination: PaginationOptions = {};
  private _orderBy: { field: string; direction: 'asc' | 'desc' } | null = null;
  private _withEntities: boolean = false;

  constructor(adapter: SQLiteAdapter) {
    this.adapter = adapter;
  }

  byType(...types: EventType[]): this {
    if (types.length === 1) {
      this.conditions.push('event_type = ?');
      this.params.push(types[0]);
    } else if (types.length > 1) {
      const placeholders = types.map(() => '?').join(',');
      this.conditions.push(`event_type IN (${placeholders})`);
      this.params.push(...types);
    }
    return this;
  }

  inTimeRange(start: Date, end?: Date): this {
    this.conditions.push('timestamp >= ?');
    this.params.push(start.toISOString());
    if (end) {
      this.conditions.push('timestamp <= ?');
      this.params.push(end.toISOString());
    }
    return this;
  }

  inSession(sessionId: string): this {
    this.conditions.push('session_id = ?');
    this.params.push(sessionId);
    return this;
  }

  fromEntity(entityName: string): this {
    this.conditions.push(
      `source_entity_id IN (SELECT id FROM ${this.adapter._getTablePrefix()}entities WHERE name = ?)`
    );
    this.params.push(entityName);
    return this;
  }

  toEntity(entityName: string): this {
    this.conditions.push(
      `target_entity_id IN (SELECT id FROM ${this.adapter._getTablePrefix()}entities WHERE name = ?)`
    );
    this.params.push(entityName);
    return this;
  }

  involvingEntity(entityName: string): this {
    this.conditions.push(
      `(source_entity_id IN (SELECT id FROM ${this.adapter._getTablePrefix()}entities WHERE name = ?) OR
        target_entity_id IN (SELECT id FROM ${this.adapter._getTablePrefix()}entities WHERE name = ?))`
    );
    this.params.push(entityName, entityName);
    return this;
  }

  withMinAmount(amount: number): this {
    this.conditions.push(`json_extract(event_data, '$.amount') >= ?`);
    this.params.push(amount);
    return this;
  }

  byActionType(...types: ActionType[]): this {
    if (types.length === 1) {
      this.conditions.push(`json_extract(event_data, '$.actionType') = ?`);
      this.params.push(types[0]);
    } else if (types.length > 1) {
      const placeholders = types.map(() => '?').join(',');
      this.conditions.push(
        `json_extract(event_data, '$.actionType') IN (${placeholders})`
      );
      this.params.push(...types);
    }
    return this;
  }

  byDamageType(...types: DamageType[]): this {
    if (types.length === 1) {
      this.conditions.push(`json_extract(event_data, '$.damageType') = ?`);
      this.params.push(types[0]);
    } else if (types.length > 1) {
      const placeholders = types.map(() => '?').join(',');
      this.conditions.push(
        `json_extract(event_data, '$.damageType') IN (${placeholders})`
      );
      this.params.push(...types);
    }
    return this;
  }

  criticalOnly(): this {
    this.conditions.push(`json_extract(event_data, '$.isCritical') = 1`);
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
    const db = this.adapter._getDb();
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

    const rows = db.prepare(sql).all(...this.params) as Record<string, unknown>[];

    // Convert rows to events (would need async for entity lookup)
    const events: CombatEvent[] = [];
    for (const row of rows) {
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
    const db = this.adapter._getDb();
    let sql = `SELECT COUNT(*) as count FROM ${this.adapter._getTablePrefix()}events`;

    if (this.conditions.length > 0) {
      sql += ` WHERE ${this.conditions.join(' AND ')}`;
    }

    const result = db.prepare(sql).get(...this.params) as { count: number };
    return result.count;
  }
}

class SQLiteSessionQuery implements SessionQuery {
  private adapter: SQLiteAdapter;
  private conditions: string[] = [];
  private params: unknown[] = [];
  private _pagination: PaginationOptions = {};
  private _orderBy: { field: string; direction: 'asc' | 'desc' } | null = null;
  private _includeEvents: boolean = false;
  private _includeParticipants: boolean = false;
  private _includeSummary: boolean = true;

  constructor(adapter: SQLiteAdapter) {
    this.adapter = adapter;
  }

  inTimeRange(start: Date, end?: Date): this {
    this.conditions.push('start_time >= ?');
    this.params.push(start.toISOString());
    if (end) {
      this.conditions.push('start_time <= ?');
      this.params.push(end.toISOString());
    }
    return this;
  }

  minDuration(ms: number): this {
    this.conditions.push('duration_ms >= ?');
    this.params.push(ms);
    return this;
  }

  maxDuration(ms: number): this {
    this.conditions.push('duration_ms <= ?');
    this.params.push(ms);
    return this;
  }

  withParticipant(entityName: string): this {
    this.conditions.push(
      `id IN (SELECT session_id FROM ${this.adapter._getTablePrefix()}participants p
              JOIN ${this.adapter._getTablePrefix()}entities e ON p.entity_id = e.id
              WHERE e.name = ?)`
    );
    this.params.push(entityName);
    return this;
  }

  minParticipants(count: number): this {
    this.conditions.push(
      `(SELECT COUNT(*) FROM ${this.adapter._getTablePrefix()}participants WHERE session_id = ${this.adapter._getTablePrefix()}sessions.id) >= ?`
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
    const db = this.adapter._getDb();
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

    const rows = db.prepare(sql).all(...this.params) as Record<string, unknown>[];

    const sessions: CombatSession[] = [];
    for (const row of rows) {
      const session: CombatSession = {
        id: row.id as string,
        startTime: new Date(row.start_time as string),
        endTime: new Date(row.end_time as string),
        durationMs: row.duration_ms as number,
        summary: this._includeSummary ? JSON.parse(row.summary_data as string) : {} as any,
        events: [],
        participants: [],
      };

      if (this._includeParticipants) {
        session.participants = await this.adapter.getParticipantsBySession(session.id);
      }

      if (this._includeEvents) {
        const eventRows = db
          .prepare(
            `SELECT * FROM ${this.adapter._getTablePrefix()}events WHERE session_id = ? ORDER BY timestamp`
          )
          .all(session.id) as Record<string, unknown>[];
        for (const eventRow of eventRows) {
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
    const db = this.adapter._getDb();
    let sql = `SELECT COUNT(*) as count FROM ${this.adapter._getTablePrefix()}sessions`;

    if (this.conditions.length > 0) {
      sql += ` WHERE ${this.conditions.join(' AND ')}`;
    }

    const result = db.prepare(sql).get(...this.params) as { count: number };
    return result.count;
  }
}

class SQLitePlayerSessionStatsQuery implements PlayerSessionStatsQuery {
  private adapter: SQLiteAdapter;
  private playerName: string;
  private conditions: string[] = [];
  private params: unknown[] = [];
  private _pagination: PaginationOptions = {};
  private _orderBy: { field: string; direction: 'asc' | 'desc' } | null = null;

  constructor(adapter: SQLiteAdapter, playerName: string) {
    this.adapter = adapter;
    this.playerName = playerName;
    this.conditions.push('player_name = ?');
    this.params.push(playerName);
  }

  inTimeRange(start: Date, end?: Date): this {
    this.conditions.push('session_start >= ?');
    this.params.push(start.toISOString());
    if (end) {
      this.conditions.push('session_start <= ?');
      this.params.push(end.toISOString());
    }
    return this;
  }

  byRole(role: string): this {
    this.conditions.push('role = ?');
    this.params.push(role);
    return this;
  }

  minPerformance(score: number): this {
    this.conditions.push('performance_score >= ?');
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
    const db = this.adapter._getDb();
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

    const rows = db.prepare(sql).all(...this.params) as Record<string, unknown>[];
    return rows.map((row) => (this.adapter as any).rowToPlayerSessionStats(row));
  }

  async first(): Promise<PlayerSessionStats | null> {
    this._pagination.limit = 1;
    const results = await this.execute();
    return results[0] ?? null;
  }

  async count(): Promise<number> {
    const db = this.adapter._getDb();
    let sql = `SELECT COUNT(*) as count FROM ${this.adapter._getTablePrefix()}player_session_stats`;

    if (this.conditions.length > 0) {
      sql += ` WHERE ${this.conditions.join(' AND ')}`;
    }

    const result = db.prepare(sql).get(...this.params) as { count: number };
    return result.count;
  }
}

class SQLiteStatsQuery implements StatsQuery {
  private adapter: SQLiteAdapter;

  constructor(adapter: SQLiteAdapter) {
    this.adapter = adapter;
  }

  playerSessions(playerName: string): PlayerSessionStatsQuery {
    return new SQLitePlayerSessionStatsQuery(this.adapter, playerName);
  }

  async playerAggregate(playerName: string): Promise<PlayerAggregateStats | null> {
    return this.adapter.getPlayerAggregateStats(playerName);
  }

  async allPlayerAggregates(): Promise<PlayerAggregateStats[]> {
    const db = this.adapter._getDb();
    const rows = db
      .prepare(`SELECT * FROM ${this.adapter._getTablePrefix()}player_aggregate_stats`)
      .all() as Record<string, unknown>[];

    return rows.map((row) => (this.adapter as any).rowToPlayerAggregateStats(row));
  }

  async topPerformers(
    metric: 'damage' | 'healing' | 'kills' | 'kdr' | 'performance',
    limit: number = 10
  ): Promise<PlayerAggregateStats[]> {
    const db = this.adapter._getDb();
    const columnMap: Record<string, string> = {
      damage: 'total_damage_dealt',
      healing: 'total_healing_done',
      kills: 'total_kills',
      kdr: 'overall_kdr',
      performance: 'avg_performance_score',
    };

    const column = columnMap[metric];
    const rows = db
      .prepare(
        `SELECT * FROM ${this.adapter._getTablePrefix()}player_aggregate_stats
         ORDER BY ${column} DESC LIMIT ?`
      )
      .all(limit) as Record<string, unknown>[];

    return rows.map((row) => (this.adapter as any).rowToPlayerAggregateStats(row));
  }
}

class SQLiteAggregationQuery implements AggregationQuery {
  private adapter: SQLiteAdapter;

  constructor(adapter: SQLiteAdapter) {
    this.adapter = adapter;
  }

  async eventsByTimeBucket(
    bucketSize: TimeBucket,
    start?: Date,
    end?: Date
  ): Promise<TimeBucketResult[]> {
    const db = this.adapter._getDb();

    // SQLite date functions for bucketing
    const bucketExpr: Record<TimeBucket, string> = {
      hour: `strftime('%Y-%m-%d %H:00:00', timestamp)`,
      day: `strftime('%Y-%m-%d', timestamp)`,
      week: `strftime('%Y-%W', timestamp)`,
      month: `strftime('%Y-%m', timestamp)`,
    };

    let sql = `
      SELECT
        ${bucketExpr[bucketSize]} as bucket,
        COUNT(*) as event_count,
        SUM(CASE WHEN event_type IN ('DAMAGE_DEALT', 'DAMAGE_RECEIVED')
            THEN json_extract(event_data, '$.amount') ELSE 0 END) as total_damage,
        SUM(CASE WHEN event_type IN ('HEALING_DONE', 'HEALING_RECEIVED')
            THEN json_extract(event_data, '$.amount') ELSE 0 END) as total_healing,
        COUNT(DISTINCT session_id) as session_count
      FROM ${this.adapter._getTablePrefix()}events
    `;

    const params: unknown[] = [];
    const conditions: string[] = [];

    if (start) {
      conditions.push('timestamp >= ?');
      params.push(start.toISOString());
    }
    if (end) {
      conditions.push('timestamp <= ?');
      params.push(end.toISOString());
    }

    if (conditions.length > 0) {
      sql += ` WHERE ${conditions.join(' AND ')}`;
    }

    sql += ` GROUP BY ${bucketExpr[bucketSize]} ORDER BY bucket`;

    const rows = db.prepare(sql).all(...params) as Record<string, unknown>[];

    return rows.map((row) => ({
      bucket: new Date(row.bucket as string),
      eventCount: row.event_count as number,
      totalDamage: (row.total_damage as number) ?? 0,
      totalHealing: (row.total_healing as number) ?? 0,
      sessionCount: row.session_count as number,
    }));
  }

  async damageByEntity(sessionId?: string): Promise<EntityAggregateResult[]> {
    const db = this.adapter._getDb();

    let sql = `
      SELECT
        e.name as entity_name,
        SUM(json_extract(ev.event_data, '$.amount')) as total_amount,
        COUNT(*) as event_count,
        AVG(json_extract(ev.event_data, '$.amount')) as average_amount,
        MAX(json_extract(ev.event_data, '$.amount')) as peak_amount
      FROM ${this.adapter._getTablePrefix()}events ev
      JOIN ${this.adapter._getTablePrefix()}entities e ON ev.source_entity_id = e.id
      WHERE ev.event_type = 'DAMAGE_DEALT'
    `;

    const params: unknown[] = [];
    if (sessionId) {
      sql += ` AND ev.session_id = ?`;
      params.push(sessionId);
    }

    sql += ` GROUP BY e.name ORDER BY total_amount DESC`;

    const rows = db.prepare(sql).all(...params) as Record<string, unknown>[];

    return rows.map((row) => ({
      entityName: row.entity_name as string,
      totalAmount: (row.total_amount as number) ?? 0,
      eventCount: row.event_count as number,
      averageAmount: (row.average_amount as number) ?? 0,
      peakAmount: (row.peak_amount as number) ?? 0,
    }));
  }

  async healingByEntity(sessionId?: string): Promise<EntityAggregateResult[]> {
    const db = this.adapter._getDb();

    let sql = `
      SELECT
        e.name as entity_name,
        SUM(json_extract(ev.event_data, '$.amount')) as total_amount,
        COUNT(*) as event_count,
        AVG(json_extract(ev.event_data, '$.amount')) as average_amount,
        MAX(json_extract(ev.event_data, '$.amount')) as peak_amount
      FROM ${this.adapter._getTablePrefix()}events ev
      JOIN ${this.adapter._getTablePrefix()}entities e ON ev.source_entity_id = e.id
      WHERE ev.event_type = 'HEALING_DONE'
    `;

    const params: unknown[] = [];
    if (sessionId) {
      sql += ` AND ev.session_id = ?`;
      params.push(sessionId);
    }

    sql += ` GROUP BY e.name ORDER BY total_amount DESC`;

    const rows = db.prepare(sql).all(...params) as Record<string, unknown>[];

    return rows.map((row) => ({
      entityName: row.entity_name as string,
      totalAmount: (row.total_amount as number) ?? 0,
      eventCount: row.event_count as number,
      averageAmount: (row.average_amount as number) ?? 0,
      peakAmount: (row.peak_amount as number) ?? 0,
    }));
  }

  async eventTypeDistribution(
    start?: Date,
    end?: Date
  ): Promise<Record<string, number>> {
    const db = this.adapter._getDb();

    let sql = `
      SELECT event_type, COUNT(*) as count
      FROM ${this.adapter._getTablePrefix()}events
    `;

    const params: unknown[] = [];
    const conditions: string[] = [];

    if (start) {
      conditions.push('timestamp >= ?');
      params.push(start.toISOString());
    }
    if (end) {
      conditions.push('timestamp <= ?');
      params.push(end.toISOString());
    }

    if (conditions.length > 0) {
      sql += ` WHERE ${conditions.join(' AND ')}`;
    }

    sql += ` GROUP BY event_type`;

    const rows = db.prepare(sql).all(...params) as Record<string, unknown>[];

    const distribution: Record<string, number> = {};
    for (const row of rows) {
      distribution[row.event_type as string] = row.count as number;
    }

    return distribution;
  }
}
