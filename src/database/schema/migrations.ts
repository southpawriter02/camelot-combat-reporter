/**
 * Migration types and initial migration definitions
 */

/**
 * A database migration
 */
export interface Migration {
  /** Migration version number (must be sequential) */
  version: number;
  /** Human-readable name */
  name: string;
  /** SQL to run when migrating up */
  up: string[];
  /** SQL to run when migrating down */
  down: string[];
}

/**
 * Initial migration - creates all tables
 */
export const MIGRATION_001_INITIAL: Migration = {
  version: 1,
  name: 'initial_schema',
  up: [
    // Entities table
    `CREATE TABLE IF NOT EXISTS {{prefix}}entities (
      id TEXT PRIMARY KEY,
      name TEXT NOT NULL UNIQUE,
      entity_type TEXT NOT NULL,
      realm TEXT,
      is_player INTEGER NOT NULL DEFAULT 0,
      is_self INTEGER NOT NULL DEFAULT 0,
      first_seen TEXT NOT NULL,
      last_seen TEXT NOT NULL,
      created_at TEXT NOT NULL DEFAULT (datetime('now'))
    )`,
    `CREATE INDEX IF NOT EXISTS idx_entities_name ON {{prefix}}entities(name)`,
    `CREATE INDEX IF NOT EXISTS idx_entities_type ON {{prefix}}entities(entity_type)`,

    // Sessions table
    `CREATE TABLE IF NOT EXISTS {{prefix}}sessions (
      id TEXT PRIMARY KEY,
      start_time TEXT NOT NULL,
      end_time TEXT NOT NULL,
      duration_ms INTEGER NOT NULL,
      summary_data TEXT NOT NULL,
      created_at TEXT NOT NULL DEFAULT (datetime('now'))
    )`,
    `CREATE INDEX IF NOT EXISTS idx_sessions_start_time ON {{prefix}}sessions(start_time)`,
    `CREATE INDEX IF NOT EXISTS idx_sessions_duration ON {{prefix}}sessions(duration_ms)`,

    // Events table
    `CREATE TABLE IF NOT EXISTS {{prefix}}events (
      id TEXT PRIMARY KEY,
      session_id TEXT REFERENCES {{prefix}}sessions(id) ON DELETE CASCADE,
      event_type TEXT NOT NULL,
      timestamp TEXT NOT NULL,
      raw_timestamp TEXT NOT NULL,
      raw_line TEXT NOT NULL,
      line_number INTEGER NOT NULL,
      source_entity_id TEXT REFERENCES {{prefix}}entities(id),
      target_entity_id TEXT REFERENCES {{prefix}}entities(id),
      event_data TEXT NOT NULL,
      created_at TEXT NOT NULL DEFAULT (datetime('now'))
    )`,
    `CREATE INDEX IF NOT EXISTS idx_events_session ON {{prefix}}events(session_id)`,
    `CREATE INDEX IF NOT EXISTS idx_events_type ON {{prefix}}events(event_type)`,
    `CREATE INDEX IF NOT EXISTS idx_events_timestamp ON {{prefix}}events(timestamp)`,
    `CREATE INDEX IF NOT EXISTS idx_events_source ON {{prefix}}events(source_entity_id)`,
    `CREATE INDEX IF NOT EXISTS idx_events_target ON {{prefix}}events(target_entity_id)`,
    `CREATE INDEX IF NOT EXISTS idx_events_session_type ON {{prefix}}events(session_id, event_type)`,
    `CREATE INDEX IF NOT EXISTS idx_events_type_timestamp ON {{prefix}}events(event_type, timestamp)`,

    // Participants table
    `CREATE TABLE IF NOT EXISTS {{prefix}}participants (
      id TEXT PRIMARY KEY,
      session_id TEXT NOT NULL REFERENCES {{prefix}}sessions(id) ON DELETE CASCADE,
      entity_id TEXT NOT NULL REFERENCES {{prefix}}entities(id),
      role TEXT NOT NULL,
      first_seen TEXT NOT NULL,
      last_seen TEXT NOT NULL,
      event_count INTEGER NOT NULL,
      created_at TEXT NOT NULL DEFAULT (datetime('now')),
      UNIQUE(session_id, entity_id)
    )`,
    `CREATE INDEX IF NOT EXISTS idx_participants_session ON {{prefix}}participants(session_id)`,
    `CREATE INDEX IF NOT EXISTS idx_participants_entity ON {{prefix}}participants(entity_id)`,

    // Player session stats table
    `CREATE TABLE IF NOT EXISTS {{prefix}}player_session_stats (
      id TEXT PRIMARY KEY,
      player_name TEXT NOT NULL,
      session_id TEXT NOT NULL REFERENCES {{prefix}}sessions(id) ON DELETE CASCADE,
      session_start TEXT NOT NULL,
      session_end TEXT NOT NULL,
      duration_ms INTEGER NOT NULL,
      role TEXT NOT NULL,
      kills INTEGER NOT NULL DEFAULT 0,
      deaths INTEGER NOT NULL DEFAULT 0,
      assists INTEGER NOT NULL DEFAULT 0,
      kdr REAL NOT NULL DEFAULT 0,
      damage_dealt INTEGER NOT NULL DEFAULT 0,
      damage_taken INTEGER NOT NULL DEFAULT 0,
      dps REAL NOT NULL DEFAULT 0,
      peak_dps REAL NOT NULL DEFAULT 0,
      healing_done INTEGER NOT NULL DEFAULT 0,
      healing_received INTEGER NOT NULL DEFAULT 0,
      hps REAL NOT NULL DEFAULT 0,
      overheal_rate REAL NOT NULL DEFAULT 0,
      crit_rate REAL NOT NULL DEFAULT 0,
      performance_score REAL NOT NULL DEFAULT 0,
      performance_rating TEXT NOT NULL,
      created_at TEXT NOT NULL DEFAULT (datetime('now')),
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
      total_combat_time_ms INTEGER NOT NULL DEFAULT 0,
      total_kills INTEGER NOT NULL DEFAULT 0,
      total_deaths INTEGER NOT NULL DEFAULT 0,
      total_assists INTEGER NOT NULL DEFAULT 0,
      overall_kdr REAL NOT NULL DEFAULT 0,
      total_damage_dealt INTEGER NOT NULL DEFAULT 0,
      total_damage_taken INTEGER NOT NULL DEFAULT 0,
      total_healing_done INTEGER NOT NULL DEFAULT 0,
      total_healing_received INTEGER NOT NULL DEFAULT 0,
      avg_dps REAL NOT NULL DEFAULT 0,
      avg_hps REAL NOT NULL DEFAULT 0,
      avg_performance_score REAL NOT NULL DEFAULT 0,
      performance_variance REAL NOT NULL DEFAULT 0,
      consistency_rating TEXT NOT NULL,
      stats_data TEXT NOT NULL,
      created_at TEXT NOT NULL DEFAULT (datetime('now')),
      updated_at TEXT NOT NULL DEFAULT (datetime('now'))
    )`,
    `CREATE INDEX IF NOT EXISTS idx_player_aggregate_stats_player ON {{prefix}}player_aggregate_stats(player_name)`,

    // Migrations tracking table
    `CREATE TABLE IF NOT EXISTS {{prefix}}migrations (
      version INTEGER PRIMARY KEY,
      name TEXT NOT NULL,
      applied_at TEXT NOT NULL DEFAULT (datetime('now'))
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
};

/**
 * All migrations in order
 */
export const ALL_MIGRATIONS: Migration[] = [
  MIGRATION_001_INITIAL,
];

/**
 * Get the latest migration version
 */
export function getLatestMigrationVersion(): number {
  const lastMigration = ALL_MIGRATIONS[ALL_MIGRATIONS.length - 1];
  return lastMigration?.version ?? 0;
}

/**
 * Get migrations to run from a given version
 */
export function getMigrationsFrom(currentVersion: number): Migration[] {
  return ALL_MIGRATIONS.filter((m) => m.version > currentVersion);
}

/**
 * Get migrations to rollback to a target version
 */
export function getMigrationsToRollback(
  currentVersion: number,
  targetVersion: number
): Migration[] {
  return ALL_MIGRATIONS
    .filter((m) => m.version <= currentVersion && m.version > targetVersion)
    .reverse();
}

/**
 * Apply table prefix to SQL statements
 */
export function applyPrefix(sql: string, prefix: string): string {
  return sql.replace(/\{\{prefix\}\}/g, prefix);
}
