import * as fs from 'fs';
import * as path from 'path';
import * as os from 'os';
import { FileWatcher } from '../../../src/streaming/FileWatcher';
import type { FileChangeEvent } from '../../../src/streaming/types';

describe('FileWatcher', () => {
  let tempDir: string;
  let testFile: string;
  let watcher: FileWatcher;

  beforeEach(async () => {
    // Create temp directory and file
    tempDir = await fs.promises.mkdtemp(path.join(os.tmpdir(), 'filewatcher-test-'));
    testFile = path.join(tempDir, 'test.log');
    await fs.promises.writeFile(testFile, 'initial content\n');

    watcher = new FileWatcher({ debounceMs: 10 });
  });

  afterEach(async () => {
    // Clean up
    if (watcher.watching) {
      await watcher.stop();
    }

    // Remove temp files
    try {
      await fs.promises.rm(tempDir, { recursive: true });
    } catch {
      // Ignore cleanup errors
    }
  });

  describe('start', () => {
    it('should start watching a file', async () => {
      await watcher.start(testFile);
      expect(watcher.watching).toBe(true);
      expect(watcher.watchedFile).toBe(testFile);
    });

    it('should throw error for non-existent file', async () => {
      const nonExistent = path.join(tempDir, 'nonexistent.log');
      await expect(watcher.start(nonExistent)).rejects.toThrow('Cannot watch file');
    });

    it('should stop previous watching when starting new watch', async () => {
      await watcher.start(testFile);

      const testFile2 = path.join(tempDir, 'test2.log');
      await fs.promises.writeFile(testFile2, 'content\n');

      await watcher.start(testFile2);
      expect(watcher.watchedFile).toBe(testFile2);
    });
  });

  describe('stop', () => {
    it('should stop watching', async () => {
      await watcher.start(testFile);
      await watcher.stop();
      expect(watcher.watching).toBe(false);
      expect(watcher.watchedFile).toBeNull();
    });

    it('should be safe to call stop multiple times', async () => {
      await watcher.start(testFile);
      await watcher.stop();
      await watcher.stop();
      expect(watcher.watching).toBe(false);
    });
  });

  describe('change events', () => {
    it('should emit change event when file content changes', async () => {
      const events: FileChangeEvent[] = [];
      watcher.on('change', (event) => events.push(event));

      await watcher.start(testFile);

      // Modify file
      await fs.promises.appendFile(testFile, 'new content\n');

      // Wait for debounce and fs.watch
      await new Promise(resolve => setTimeout(resolve, 200));

      expect(events.length).toBeGreaterThanOrEqual(1);
      expect(events[0]?.type).toBe('change');
      expect(events[0]?.filename).toBe(testFile);
    });

    it('should include size information in change event', async () => {
      const events: FileChangeEvent[] = [];
      watcher.on('change', (event) => events.push(event));

      await watcher.start(testFile);
      const initialSize = (await fs.promises.stat(testFile)).size;

      // Modify file
      await fs.promises.appendFile(testFile, 'more content\n');

      // Wait for event
      await new Promise(resolve => setTimeout(resolve, 200));

      expect(events.length).toBeGreaterThanOrEqual(1);
      expect(events[0]?.previousSize).toBe(initialSize);
      expect(events[0]?.currentSize).toBeGreaterThan(initialSize);
    });
  });

  describe('rotation detection', () => {
    it('should emit rotate event when file size decreases', async () => {
      const rotateEvents: FileChangeEvent[] = [];
      watcher.on('rotate', (event) => rotateEvents.push(event));

      await watcher.start(testFile);

      // Simulate rotation by truncating file
      await fs.promises.writeFile(testFile, 'x');

      // Wait for detection
      await new Promise(resolve => setTimeout(resolve, 200));

      expect(rotateEvents.length).toBeGreaterThanOrEqual(1);
      expect(rotateEvents[0]?.type).toBe('rotate');
    });
  });

  describe('polling mode', () => {
    it('should work with polling enabled', async () => {
      const pollingWatcher = new FileWatcher({
        usePolling: true,
        pollIntervalMs: 50,
        debounceMs: 10,
      });

      const events: FileChangeEvent[] = [];
      pollingWatcher.on('change', (event) => events.push(event));

      await pollingWatcher.start(testFile);

      // Modify file
      await fs.promises.appendFile(testFile, 'polled content\n');

      // Wait for polling
      await new Promise(resolve => setTimeout(resolve, 150));

      await pollingWatcher.stop();

      expect(events.length).toBeGreaterThanOrEqual(1);
    });
  });

  describe('getStats', () => {
    it('should return current file stats', async () => {
      await watcher.start(testFile);
      const stats = await watcher.getStats();

      expect(stats).not.toBeNull();
      expect(stats?.size).toBeGreaterThan(0);
      expect(stats?.inode).toBeGreaterThan(0);
    });

    it('should return null when not watching', async () => {
      const stats = await watcher.getStats();
      expect(stats).toBeNull();
    });
  });

  describe('forceCheck', () => {
    it('should manually trigger a check', async () => {
      const events: FileChangeEvent[] = [];
      watcher.on('change', (event) => events.push(event));

      await watcher.start(testFile);

      // Modify without waiting for fs.watch
      await fs.promises.appendFile(testFile, 'forced content\n');
      await watcher.forceCheck();

      expect(events.length).toBeGreaterThanOrEqual(1);
    });
  });
});
