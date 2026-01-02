/**
 * FileWatcher - Monitors a file for changes with rotation detection
 *
 * Uses Node's native fs.watch() with optional polling fallback.
 * Detects log rotation by monitoring inode changes or size decreases.
 */
import { EventEmitter } from 'events';
import * as fs from 'fs';
import * as path from 'path';
import type {
  FileChangeEvent,
  FileWatcherConfig,
} from './types.js';
import { DEFAULT_FILE_WATCHER_CONFIG } from './types.js';

/**
 * Events emitted by FileWatcher
 */
export interface FileWatcherEvents {
  change: (event: FileChangeEvent) => void;
  rotate: (event: FileChangeEvent) => void;
  error: (error: Error) => void;
}

/**
 * FileWatcher monitors a file for changes and rotation
 */
export class FileWatcher extends EventEmitter {
  private config: FileWatcherConfig;
  private filename: string | null = null;
  private watcher: fs.FSWatcher | null = null;
  private pollTimer: NodeJS.Timeout | null = null;
  private debounceTimer: NodeJS.Timeout | null = null;
  private lastSize: number = 0;
  private lastInode: number = 0;
  private isWatching: boolean = false;

  constructor(config: Partial<FileWatcherConfig> = {}) {
    super();
    this.config = { ...DEFAULT_FILE_WATCHER_CONFIG, ...config };
  }

  /**
   * Start watching a file
   */
  async start(filename: string): Promise<void> {
    if (this.isWatching) {
      await this.stop();
    }

    this.filename = path.resolve(filename);

    // Get initial file stats
    try {
      const stats = await fs.promises.stat(this.filename);
      this.lastSize = stats.size;
      this.lastInode = stats.ino;
    } catch (error) {
      throw new Error(`Cannot watch file: ${(error as Error).message}`);
    }

    this.isWatching = true;

    if (this.config.usePolling) {
      this.startPolling();
    } else {
      this.startFsWatch();
    }
  }

  /**
   * Stop watching
   */
  async stop(): Promise<void> {
    this.isWatching = false;

    if (this.watcher) {
      this.watcher.close();
      this.watcher = null;
    }

    if (this.pollTimer) {
      clearInterval(this.pollTimer);
      this.pollTimer = null;
    }

    if (this.debounceTimer) {
      clearTimeout(this.debounceTimer);
      this.debounceTimer = null;
    }

    this.filename = null;
  }

  /**
   * Check if currently watching
   */
  get watching(): boolean {
    return this.isWatching;
  }

  /**
   * Get the file being watched
   */
  get watchedFile(): string | null {
    return this.filename;
  }

  /**
   * Start fs.watch based monitoring
   */
  private startFsWatch(): void {
    if (!this.filename) return;

    try {
      this.watcher = fs.watch(this.filename, (eventType, filename) => {
        this.handleFsWatchEvent(eventType, filename);
      });

      this.watcher.on('error', (error) => {
        this.emit('error', error);
        // Fall back to polling on error
        if (this.isWatching && this.filename) {
          this.watcher?.close();
          this.watcher = null;
          this.startPolling();
        }
      });
    } catch (error) {
      // Fall back to polling if fs.watch fails
      this.startPolling();
    }
  }

  /**
   * Handle fs.watch events with debouncing
   */
  private handleFsWatchEvent(eventType: string, _filename: string | null): void {
    // Clear existing debounce timer
    if (this.debounceTimer) {
      clearTimeout(this.debounceTimer);
    }

    // Debounce rapid events
    this.debounceTimer = setTimeout(() => {
      this.checkFileChanges(eventType === 'rename' ? 'rename' : 'change');
    }, this.config.debounceMs);
  }

  /**
   * Start polling-based monitoring
   */
  private startPolling(): void {
    this.pollTimer = setInterval(() => {
      this.checkFileChanges('change');
    }, this.config.pollIntervalMs);
  }

  /**
   * Check for file changes and rotation
   */
  private async checkFileChanges(
    eventType: 'change' | 'rename'
  ): Promise<void> {
    if (!this.filename || !this.isWatching) return;

    try {
      const stats = await fs.promises.stat(this.filename);
      const currentSize = stats.size;
      const currentInode = stats.ino;

      // Detect rotation: inode changed or size significantly decreased
      const inodeChanged = currentInode !== this.lastInode;
      const sizeDecreased = currentSize < this.lastSize && this.lastSize > 0;

      if (inodeChanged || sizeDecreased) {
        const event: FileChangeEvent = {
          type: 'rotate',
          filename: this.filename,
          currentSize,
          currentInode,
          previousSize: this.lastSize,
          previousInode: this.lastInode,
        };

        this.lastSize = currentSize;
        this.lastInode = currentInode;

        this.emit('rotate', event);
      } else if (currentSize !== this.lastSize) {
        // Normal change (file grew or content modified)
        const event: FileChangeEvent = {
          type: eventType,
          filename: this.filename,
          currentSize,
          currentInode,
          previousSize: this.lastSize,
          previousInode: this.lastInode,
        };

        this.lastSize = currentSize;
        this.lastInode = currentInode;

        this.emit('change', event);
      }
    } catch (error) {
      // File might have been deleted or rotated
      if ((error as NodeJS.ErrnoException).code === 'ENOENT') {
        // File was deleted - wait for it to reappear
        this.emit('error', new Error('File was deleted or moved'));
      } else {
        this.emit('error', error as Error);
      }
    }
  }

  /**
   * Force a check for changes (useful after resuming)
   */
  async forceCheck(): Promise<void> {
    await this.checkFileChanges('change');
  }

  /**
   * Get current file stats
   */
  async getStats(): Promise<{ size: number; inode: number } | null> {
    if (!this.filename) return null;

    try {
      const stats = await fs.promises.stat(this.filename);
      return {
        size: stats.size,
        inode: stats.ino,
      };
    } catch {
      return null;
    }
  }

  /**
   * Type-safe event emitter methods
   */
  override on<K extends keyof FileWatcherEvents>(
    event: K,
    listener: FileWatcherEvents[K]
  ): this {
    return super.on(event, listener);
  }

  override once<K extends keyof FileWatcherEvents>(
    event: K,
    listener: FileWatcherEvents[K]
  ): this {
    return super.once(event, listener);
  }

  override emit<K extends keyof FileWatcherEvents>(
    event: K,
    ...args: Parameters<FileWatcherEvents[K]>
  ): boolean {
    return super.emit(event, ...args);
  }

  override off<K extends keyof FileWatcherEvents>(
    event: K,
    listener: FileWatcherEvents[K]
  ): this {
    return super.off(event, listener);
  }
}
