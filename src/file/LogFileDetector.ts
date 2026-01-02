import * as fs from 'fs';
import * as path from 'path';
import type { LogFileInfo } from '../types/index.js';
import { hasValidTimestamp } from '../utils/index.js';

/**
 * Known DAoC log file names
 */
const LOG_FILE_NAMES = ['chat.log', 'combat.log'];

/**
 * Result of validating a log file
 */
export interface ValidationResult {
  /** Whether the file is a valid DAoC log */
  isValid: boolean;
  /** Reason for validation result */
  reason: string;
  /** Number of valid log lines found */
  validLineCount: number;
}

/**
 * Utility class for detecting and validating DAoC log files
 */
export class LogFileDetector {
  /**
   * Search for log files in a directory
   *
   * @param directory - Directory to search in
   * @returns Array of found log files
   */
  async findLogFiles(directory: string): Promise<LogFileInfo[]> {
    const results: LogFileInfo[] = [];

    try {
      const entries = await fs.promises.readdir(directory, { withFileTypes: true });

      for (const entry of entries) {
        if (entry.isFile() && LOG_FILE_NAMES.includes(entry.name.toLowerCase())) {
          const filePath = path.join(directory, entry.name);
          const stats = await fs.promises.stat(filePath);

          results.push({
            path: filePath,
            name: entry.name,
            size: stats.size,
            lastModified: stats.mtime,
          });
        }
      }
    } catch {
      // Directory doesn't exist or can't be read - return empty array
    }

    return results;
  }

  /**
   * Validate if a file is a valid DAoC log file
   *
   * @param filePath - Path to the file to validate
   * @returns ValidationResult
   */
  async validateLogFile(filePath: string): Promise<ValidationResult> {
    try {
      // Check if file exists and is readable
      await fs.promises.access(filePath, fs.constants.R_OK);

      // Read first portion of file to check format
      const content = await this.readFileHead(filePath, 50);
      const lines = content.split(/\r?\n/).filter((line) => line.trim());

      if (lines.length === 0) {
        return {
          isValid: false,
          reason: 'File is empty',
          validLineCount: 0,
        };
      }

      // Count lines with valid timestamps
      let validLineCount = 0;
      for (const line of lines) {
        if (hasValidTimestamp(line)) {
          validLineCount++;
        }
      }

      // Consider valid if at least 50% of lines have valid timestamps
      const validRatio = validLineCount / lines.length;
      const isValid = validRatio >= 0.5;

      return {
        isValid,
        reason: isValid
          ? `Found ${validLineCount} valid log lines`
          : `Only ${validLineCount} of ${lines.length} lines have valid timestamps`,
        validLineCount,
      };
    } catch (error) {
      return {
        isValid: false,
        reason: error instanceof Error ? error.message : 'Unknown error',
        validLineCount: 0,
      };
    }
  }

  /**
   * Read the first N lines from a file
   */
  private async readFileHead(filePath: string, maxLines: number): Promise<string> {
    const fd = await fs.promises.open(filePath, 'r');
    try {
      // Read first 10KB which should be enough for validation
      const buffer = Buffer.alloc(10240);
      const { bytesRead } = await fd.read(buffer, 0, buffer.length, 0);

      const content = buffer.toString('utf-8', 0, bytesRead);
      const lines = content.split(/\r?\n/);

      return lines.slice(0, maxLines).join('\n');
    } finally {
      await fd.close();
    }
  }
}
