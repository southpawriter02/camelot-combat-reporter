/**
 * Schema exports
 */
export {
  Migration,
  ALL_MIGRATIONS,
  MIGRATION_001_INITIAL,
  getLatestMigrationVersion,
  getMigrationsFrom,
  getMigrationsToRollback,
  applyPrefix,
} from './migrations.js';
