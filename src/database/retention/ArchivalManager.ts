/**
 * ArchivalManager - Archive records before deletion
 */
import * as fs from 'fs';
import * as path from 'path';
import * as zlib from 'zlib';
import { promisify } from 'util';
import type { RetentionTable, ArchiveFormat } from '../types.js';
import { ArchivalError } from '../errors.js';

const gzip = promisify(zlib.gzip);

/**
 * Result of an archive operation
 */
export interface ArchiveResult {
  /** Path to the archive file */
  archivePath: string;
  /** Number of records archived */
  recordCount: number;
  /** Size of the archive file in bytes */
  fileSize: number;
  /** Whether compression was applied */
  compressed: boolean;
  /** Timestamp of archive creation */
  createdAt: Date;
}

/**
 * Archive metadata stored with the archive
 */
export interface ArchiveMetadata {
  /** Table that was archived */
  table: RetentionTable;
  /** Number of records */
  recordCount: number;
  /** Date range of archived records */
  dateRange: {
    oldest: Date;
    newest: Date;
  };
  /** Archive creation timestamp */
  createdAt: Date;
  /** Format of the archive */
  format: ArchiveFormat;
  /** Whether data is compressed */
  compressed: boolean;
  /** Original size before compression */
  originalSize: number;
}

/**
 * ArchivalManager handles archiving records before deletion
 */
export class ArchivalManager {
  private format: ArchiveFormat;
  private compression: boolean;

  constructor(format: ArchiveFormat = 'json', compression: boolean = true) {
    this.format = format;
    this.compression = compression;
  }

  /**
   * Archive records to a file
   */
  async archive<T extends Record<string, unknown>>(
    table: RetentionTable,
    records: T[],
    archivePath: string
  ): Promise<ArchiveResult> {
    if (records.length === 0) {
      throw new ArchivalError('No records to archive', archivePath);
    }

    // Ensure archive directory exists
    await this.ensureDirectory(archivePath);

    // Generate filename
    const filename = this.generateFilename(table);
    const fullPath = path.join(archivePath, filename);

    try {
      if (this.format === 'json') {
        return await this.archiveAsJson(table, records, fullPath);
      } else {
        return await this.archiveAsSqlite(table, records, fullPath);
      }
    } catch (error) {
      throw new ArchivalError(
        `Failed to archive records: ${(error as Error).message}`,
        fullPath,
        error as Error
      );
    }
  }

  /**
   * Archive records as JSON
   */
  private async archiveAsJson<T extends Record<string, unknown>>(
    table: RetentionTable,
    records: T[],
    basePath: string
  ): Promise<ArchiveResult> {
    // Find date range
    const dates = records
      .map((r) => {
        const dateField = table === 'sessions' ? r.start_time : r.created_at;
        return dateField ? new Date(dateField as string) : new Date();
      })
      .sort((a, b) => a.getTime() - b.getTime());

    const now = new Date();
    const metadata: ArchiveMetadata = {
      table,
      recordCount: records.length,
      dateRange: {
        oldest: dates[0] ?? now,
        newest: dates[dates.length - 1] ?? now,
      },
      createdAt: now,
      format: 'json',
      compressed: this.compression,
      originalSize: 0,
    };

    const archiveData = {
      metadata,
      records,
    };

    const jsonString = JSON.stringify(archiveData, null, 2);
    metadata.originalSize = Buffer.byteLength(jsonString, 'utf-8');

    let finalPath: string;
    let fileSize: number;

    if (this.compression) {
      finalPath = `${basePath}.json.gz`;
      const compressed = await gzip(Buffer.from(jsonString, 'utf-8'));
      await fs.promises.writeFile(finalPath, compressed);
      fileSize = compressed.length;
    } else {
      finalPath = `${basePath}.json`;
      await fs.promises.writeFile(finalPath, jsonString, 'utf-8');
      fileSize = metadata.originalSize;
    }

    return {
      archivePath: finalPath,
      recordCount: records.length,
      fileSize,
      compressed: this.compression,
      createdAt: metadata.createdAt,
    };
  }

  /**
   * Archive records as SQLite database
   * Note: This is a simplified implementation that exports as SQL statements
   */
  private async archiveAsSqlite<T extends Record<string, unknown>>(
    table: RetentionTable,
    records: T[],
    basePath: string
  ): Promise<ArchiveResult> {
    // Generate SQL insert statements
    const sqlStatements: string[] = [];

    // Find date range
    const dates = records
      .map((r) => {
        const dateField = table === 'sessions' ? r.start_time : r.created_at;
        return dateField ? new Date(dateField as string) : new Date();
      })
      .sort((a, b) => a.getTime() - b.getTime());

    // Add metadata as comment
    const now = new Date();
    const oldestDate = dates[0] ?? now;
    const newestDate = dates[dates.length - 1] ?? now;
    sqlStatements.push(`-- Archive of ${table}`);
    sqlStatements.push(`-- Records: ${records.length}`);
    sqlStatements.push(`-- Date range: ${oldestDate.toISOString()} to ${newestDate.toISOString()}`);
    sqlStatements.push(`-- Created: ${now.toISOString()}`);
    sqlStatements.push('');

    // Generate CREATE TABLE statement based on table
    sqlStatements.push(this.getCreateTableSql(table));
    sqlStatements.push('');

    // Generate INSERT statements
    for (const record of records) {
      sqlStatements.push(this.generateInsertSql(table, record));
    }

    const sqlContent = sqlStatements.join('\n');
    const originalSize = Buffer.byteLength(sqlContent, 'utf-8');

    let finalPath: string;
    let fileSize: number;

    if (this.compression) {
      finalPath = `${basePath}.sql.gz`;
      const compressed = await gzip(Buffer.from(sqlContent, 'utf-8'));
      await fs.promises.writeFile(finalPath, compressed);
      fileSize = compressed.length;
    } else {
      finalPath = `${basePath}.sql`;
      await fs.promises.writeFile(finalPath, sqlContent, 'utf-8');
      fileSize = originalSize;
    }

    return {
      archivePath: finalPath,
      recordCount: records.length,
      fileSize,
      compressed: this.compression,
      createdAt: new Date(),
    };
  }

  /**
   * Generate a unique filename for the archive
   */
  private generateFilename(table: RetentionTable): string {
    const timestamp = new Date().toISOString().replace(/[:.]/g, '-');
    return `${table}_archive_${timestamp}`;
  }

  /**
   * Ensure the archive directory exists
   */
  private async ensureDirectory(archivePath: string): Promise<void> {
    try {
      await fs.promises.mkdir(archivePath, { recursive: true });
    } catch (error) {
      throw new ArchivalError(
        `Failed to create archive directory: ${(error as Error).message}`,
        archivePath,
        error as Error
      );
    }
  }

  /**
   * Get CREATE TABLE SQL for a table
   */
  private getCreateTableSql(table: RetentionTable): string {
    const schemas: Record<RetentionTable, string> = {
      entities: `CREATE TABLE IF NOT EXISTS entities (
        id TEXT PRIMARY KEY,
        name TEXT NOT NULL,
        entity_type TEXT NOT NULL,
        realm TEXT,
        is_player INTEGER,
        is_self INTEGER,
        first_seen TEXT,
        last_seen TEXT,
        created_at TEXT
      )`,
      sessions: `CREATE TABLE IF NOT EXISTS sessions (
        id TEXT PRIMARY KEY,
        start_time TEXT NOT NULL,
        end_time TEXT NOT NULL,
        duration_ms INTEGER,
        summary_data TEXT,
        created_at TEXT
      )`,
      events: `CREATE TABLE IF NOT EXISTS events (
        id TEXT PRIMARY KEY,
        session_id TEXT,
        event_type TEXT NOT NULL,
        timestamp TEXT,
        raw_timestamp TEXT,
        raw_line TEXT,
        line_number INTEGER,
        source_entity_id TEXT,
        target_entity_id TEXT,
        event_data TEXT,
        created_at TEXT
      )`,
      participants: `CREATE TABLE IF NOT EXISTS participants (
        id TEXT PRIMARY KEY,
        session_id TEXT,
        entity_id TEXT,
        role TEXT,
        first_seen TEXT,
        last_seen TEXT,
        event_count INTEGER,
        created_at TEXT
      )`,
      player_session_stats: `CREATE TABLE IF NOT EXISTS player_session_stats (
        id TEXT PRIMARY KEY,
        player_name TEXT,
        session_id TEXT,
        session_start TEXT,
        session_end TEXT,
        duration_ms INTEGER,
        role TEXT,
        kills INTEGER,
        deaths INTEGER,
        assists INTEGER,
        kdr REAL,
        damage_dealt INTEGER,
        damage_taken INTEGER,
        dps REAL,
        peak_dps REAL,
        healing_done INTEGER,
        healing_received INTEGER,
        hps REAL,
        overheal_rate REAL,
        crit_rate REAL,
        performance_score REAL,
        performance_rating TEXT,
        created_at TEXT
      )`,
    };

    return schemas[table] + ';';
  }

  /**
   * Generate INSERT SQL for a record
   */
  private generateInsertSql(
    table: RetentionTable,
    record: Record<string, unknown>
  ): string {
    const columns = Object.keys(record);
    const values = columns.map((col) => {
      const value = record[col];
      if (value === null || value === undefined) {
        return 'NULL';
      }
      if (typeof value === 'string') {
        return `'${value.replace(/'/g, "''")}'`;
      }
      if (typeof value === 'object') {
        return `'${JSON.stringify(value).replace(/'/g, "''")}'`;
      }
      return String(value);
    });

    return `INSERT INTO ${table} (${columns.join(', ')}) VALUES (${values.join(', ')});`;
  }

  /**
   * List archives in a directory
   */
  async listArchives(archivePath: string): Promise<string[]> {
    try {
      const files = await fs.promises.readdir(archivePath);
      return files.filter(
        (f) =>
          f.endsWith('.json') ||
          f.endsWith('.json.gz') ||
          f.endsWith('.sql') ||
          f.endsWith('.sql.gz')
      );
    } catch {
      return [];
    }
  }

  /**
   * Read archive metadata (JSON only)
   */
  async readArchiveMetadata(archivePath: string): Promise<ArchiveMetadata | null> {
    try {
      let content: string;

      if (archivePath.endsWith('.gz')) {
        const compressed = await fs.promises.readFile(archivePath);
        const gunzip = promisify(zlib.gunzip);
        const decompressed = await gunzip(compressed);
        content = decompressed.toString('utf-8');
      } else {
        content = await fs.promises.readFile(archivePath, 'utf-8');
      }

      if (archivePath.includes('.json')) {
        const data = JSON.parse(content);
        return data.metadata;
      }

      // For SQL files, parse metadata from comments
      const lines = content.split('\n');
      const metadataLines = lines.filter((l) => l.startsWith('--'));

      if (metadataLines.length < 4) {
        return null;
      }

      // Parse metadata from comments
      const tableMatch = metadataLines[0]?.match(/Archive of (\w+)/);
      const recordsMatch = metadataLines[1]?.match(/Records: (\d+)/);
      const dateRangeMatch = metadataLines[2]?.match(/Date range: (.+) to (.+)/);
      const createdMatch = metadataLines[3]?.match(/Created: (.+)/);

      if (!tableMatch?.[1] || !recordsMatch?.[1] || !dateRangeMatch?.[1] || !dateRangeMatch?.[2] || !createdMatch?.[1]) {
        return null;
      }

      return {
        table: tableMatch[1] as RetentionTable,
        recordCount: parseInt(recordsMatch[1], 10),
        dateRange: {
          oldest: new Date(dateRangeMatch[1]),
          newest: new Date(dateRangeMatch[2]),
        },
        createdAt: new Date(createdMatch[1]),
        format: 'sqlite',
        compressed: archivePath.endsWith('.gz'),
        originalSize: 0,
      };
    } catch {
      return null;
    }
  }
}
