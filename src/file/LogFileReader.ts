import * as fs from 'fs';
import * as readline from 'readline';
import { FileError, ErrorCode } from '../errors/index.js';

/**
 * Information about a line read from a file
 */
export interface LineInfo {
  /** The line content */
  content: string;
  /** Line number (1-indexed) */
  lineNumber: number;
}

/**
 * Utility class for reading log files
 */
export class LogFileReader {
  /**
   * Read an entire log file and return all lines
   *
   * @param filePath - Path to the log file
   * @returns Array of line strings
   * @throws FileError if file cannot be read
   */
  async readFile(filePath: string): Promise<string[]> {
    try {
      // Check if file exists
      await fs.promises.access(filePath, fs.constants.R_OK);

      // Read file content
      const content = await fs.promises.readFile(filePath, { encoding: 'utf-8' });

      // Split into lines, handling both Windows and Unix line endings
      return content.split(/\r?\n/);
    } catch (error) {
      if (error instanceof Error) {
        if ((error as NodeJS.ErrnoException).code === 'ENOENT') {
          throw new FileError(
            `File not found: ${filePath}`,
            ErrorCode.FILE_NOT_FOUND,
            filePath,
            error
          );
        }
        throw new FileError(
          `Failed to read file: ${filePath}`,
          ErrorCode.FILE_READ_ERROR,
          filePath,
          error
        );
      }
      throw error;
    }
  }

  /**
   * Stream a log file line by line (memory efficient for large files)
   *
   * @param filePath - Path to the log file
   * @yields LineInfo for each line
   * @throws FileError if file cannot be read
   */
  async *streamLines(filePath: string): AsyncGenerator<LineInfo> {
    // Check if file exists
    try {
      await fs.promises.access(filePath, fs.constants.R_OK);
    } catch (error) {
      if (error instanceof Error) {
        if ((error as NodeJS.ErrnoException).code === 'ENOENT') {
          throw new FileError(
            `File not found: ${filePath}`,
            ErrorCode.FILE_NOT_FOUND,
            filePath,
            error
          );
        }
        throw new FileError(
          `Failed to access file: ${filePath}`,
          ErrorCode.FILE_READ_ERROR,
          filePath,
          error
        );
      }
      throw error;
    }

    const fileStream = fs.createReadStream(filePath, { encoding: 'utf-8' });
    const rl = readline.createInterface({
      input: fileStream,
      crlfDelay: Infinity, // Handle Windows line endings
    });

    let lineNumber = 0;

    for await (const line of rl) {
      lineNumber++;
      yield {
        content: line,
        lineNumber,
      };
    }
  }

  /**
   * Get file information
   *
   * @param filePath - Path to the file
   * @returns File stats
   * @throws FileError if file cannot be accessed
   */
  async getFileInfo(filePath: string): Promise<fs.Stats> {
    try {
      return await fs.promises.stat(filePath);
    } catch (error) {
      if (error instanceof Error) {
        if ((error as NodeJS.ErrnoException).code === 'ENOENT') {
          throw new FileError(
            `File not found: ${filePath}`,
            ErrorCode.FILE_NOT_FOUND,
            filePath,
            error
          );
        }
        throw new FileError(
          `Failed to get file info: ${filePath}`,
          ErrorCode.FILE_READ_ERROR,
          filePath,
          error
        );
      }
      throw error;
    }
  }
}
