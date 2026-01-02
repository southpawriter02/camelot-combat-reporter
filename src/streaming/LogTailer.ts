/**
 * LogTailer - Reads new lines from a log file as it grows
 *
 * Provides efficient file tailing with:
 * - Byte offset tracking for resume
 * - Line number tracking
 * - Partial line handling (incomplete lines at end of file)
 * - Async generator interface
 */
import * as fs from 'fs';
import * as path from 'path';
import type {
  TailLine,
  TailPosition,
  LogTailerConfig,
} from './types.js';
import { DEFAULT_LOG_TAILER_CONFIG } from './types.js';

/**
 * LogTailer reads new content from a file starting from a given position
 */
export class LogTailer {
  private config: LogTailerConfig;
  private filename: string | null = null;
  private currentPosition: TailPosition = { byteOffset: 0, lineNumber: 0 };
  private partialLine: string = '';
  private fileHandle: fs.promises.FileHandle | null = null;

  constructor(config: Partial<LogTailerConfig> = {}) {
    this.config = { ...DEFAULT_LOG_TAILER_CONFIG, ...config };
  }

  /**
   * Open a file for tailing
   */
  async open(filename: string): Promise<TailPosition> {
    if (this.fileHandle) {
      await this.close();
    }

    this.filename = path.resolve(filename);
    this.fileHandle = await fs.promises.open(this.filename, 'r');
    this.partialLine = '';

    // Determine starting position
    if (this.config.startPosition) {
      this.currentPosition = { ...this.config.startPosition };
    } else if (this.config.fromBeginning) {
      this.currentPosition = { byteOffset: 0, lineNumber: 0 };
    } else {
      // Start from end of file
      const stats = await this.fileHandle.stat();
      this.currentPosition = {
        byteOffset: stats.size,
        lineNumber: await this.countLines(),
      };
    }

    return { ...this.currentPosition };
  }

  /**
   * Close the file
   */
  async close(): Promise<void> {
    if (this.fileHandle) {
      await this.fileHandle.close();
      this.fileHandle = null;
    }
    this.filename = null;
    this.partialLine = '';
  }

  /**
   * Get current position for resume
   */
  getPosition(): TailPosition {
    return { ...this.currentPosition };
  }

  /**
   * Set position (for seeking)
   */
  setPosition(position: TailPosition): void {
    this.currentPosition = { ...position };
    this.partialLine = '';
  }

  /**
   * Read new lines since last read
   * Returns an async generator of TailLine objects
   */
  async *tail(): AsyncGenerator<TailLine, void, unknown> {
    if (!this.fileHandle) {
      throw new Error('File not opened. Call open() first.');
    }

    const buffer = Buffer.alloc(this.config.bufferSize);
    let bytesRead: number;

    // Read from current position
    const result = await this.fileHandle.read(
      buffer,
      0,
      this.config.bufferSize,
      this.currentPosition.byteOffset
    );
    bytesRead = result.bytesRead;

    if (bytesRead === 0) {
      // No new data
      return;
    }

    // Convert to string
    const chunk = buffer.toString('utf-8', 0, bytesRead);

    // Prepend any partial line from previous read
    const content = this.partialLine + chunk;

    // Split into lines
    const lines = content.split(/\r?\n/);

    // Last element might be partial (no trailing newline)
    this.partialLine = lines.pop() || '';

    // Calculate byte position for complete lines
    // Note: if we had a partial line from last read, we need to account for it
    // The partial line bytes were already included in the last offset update
    let currentByteOffset = this.currentPosition.byteOffset;

    // Yield complete lines
    for (const line of lines) {
      this.currentPosition.lineNumber++;

      const tailLine: TailLine = {
        content: line,
        lineNumber: this.currentPosition.lineNumber,
        byteOffset: currentByteOffset,
      };

      // Update byte offset (line + newline character)
      // For the first line, this might include the previously-saved partial line content
      // but we only add the newline for this line
      currentByteOffset += Buffer.byteLength(line, 'utf-8') + 1;

      yield tailLine;
    }

    // Update position to end of all bytes we just read
    // This includes both complete lines and the new partial line (if any)
    this.currentPosition.byteOffset += bytesRead;
  }

  /**
   * Read all new lines at once (non-generator version)
   */
  async readNewLines(): Promise<TailLine[]> {
    const lines: TailLine[] = [];
    for await (const line of this.tail()) {
      lines.push(line);
    }
    return lines;
  }

  /**
   * Check if there's new content available
   */
  async hasNewContent(): Promise<boolean> {
    if (!this.fileHandle) {
      return false;
    }

    const stats = await this.fileHandle.stat();
    return stats.size > this.currentPosition.byteOffset;
  }

  /**
   * Get file size
   */
  async getFileSize(): Promise<number> {
    if (!this.fileHandle) {
      return 0;
    }
    const stats = await this.fileHandle.stat();
    return stats.size;
  }

  /**
   * Reset to handle file rotation
   * Opens the new file and resets position
   */
  async handleRotation(): Promise<TailPosition> {
    if (!this.filename) {
      throw new Error('No file to handle rotation for');
    }

    // Save filename before close (which sets it to null)
    const savedFilename = this.filename;

    // Close the file handle but don't reset filename yet
    if (this.fileHandle) {
      await this.fileHandle.close();
      this.fileHandle = null;
    }
    this.partialLine = '';
    this.filename = null;

    // Reopen with fromBeginning to read new file from start
    const originalFromBeginning = this.config.fromBeginning;
    this.config.fromBeginning = true;
    const position = await this.open(savedFilename);
    this.config.fromBeginning = originalFromBeginning;

    return position;
  }

  /**
   * Flush any partial line (force emit incomplete line)
   */
  flushPartial(): TailLine | null {
    if (this.partialLine.length === 0) {
      return null;
    }

    this.currentPosition.lineNumber++;
    const line: TailLine = {
      content: this.partialLine,
      lineNumber: this.currentPosition.lineNumber,
      byteOffset: this.currentPosition.byteOffset,
    };

    this.currentPosition.byteOffset += Buffer.byteLength(this.partialLine, 'utf-8');
    this.partialLine = '';

    return line;
  }

  /**
   * Count total lines in file (for starting from end)
   */
  private async countLines(): Promise<number> {
    if (!this.fileHandle) return 0;

    const stats = await this.fileHandle.stat();
    if (stats.size === 0) return 0;

    let lineCount = 0;
    const buffer = Buffer.alloc(this.config.bufferSize);
    let position = 0;

    while (position < stats.size) {
      const { bytesRead } = await this.fileHandle.read(
        buffer,
        0,
        this.config.bufferSize,
        position
      );

      if (bytesRead === 0) break;

      for (let i = 0; i < bytesRead; i++) {
        if (buffer[i] === 0x0a) {
          // newline
          lineCount++;
        }
      }

      position += bytesRead;
    }

    return lineCount;
  }
}
