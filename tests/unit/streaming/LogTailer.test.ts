import * as fs from 'fs';
import * as path from 'path';
import * as os from 'os';
import { LogTailer } from '../../../src/streaming/LogTailer';
import type { TailLine } from '../../../src/streaming/types';

describe('LogTailer', () => {
  let tempDir: string;
  let testFile: string;
  let tailer: LogTailer;

  beforeEach(async () => {
    // Create temp directory and file
    tempDir = await fs.promises.mkdtemp(path.join(os.tmpdir(), 'logtailer-test-'));
    testFile = path.join(tempDir, 'test.log');
    await fs.promises.writeFile(testFile, 'line 1\nline 2\nline 3\n');

    tailer = new LogTailer();
  });

  afterEach(async () => {
    // Clean up
    try {
      await tailer.close();
    } catch {
      // Ignore
    }

    try {
      await fs.promises.rm(tempDir, { recursive: true });
    } catch {
      // Ignore cleanup errors
    }
  });

  describe('open', () => {
    it('should open file and position at end by default', async () => {
      const position = await tailer.open(testFile);

      // Should be at end of file
      const fileSize = (await fs.promises.stat(testFile)).size;
      expect(position.byteOffset).toBe(fileSize);
      expect(position.lineNumber).toBe(3);
    });

    it('should position at beginning when fromBeginning is true', async () => {
      const tailerFromStart = new LogTailer({ fromBeginning: true });
      const position = await tailerFromStart.open(testFile);

      expect(position.byteOffset).toBe(0);
      expect(position.lineNumber).toBe(0);

      await tailerFromStart.close();
    });

    it('should use startPosition if provided', async () => {
      const tailerWithPos = new LogTailer({
        startPosition: { byteOffset: 7, lineNumber: 1 },
      });
      const position = await tailerWithPos.open(testFile);

      expect(position.byteOffset).toBe(7);
      expect(position.lineNumber).toBe(1);

      await tailerWithPos.close();
    });
  });

  describe('tail', () => {
    it('should return empty when no new content', async () => {
      await tailer.open(testFile);
      const lines = await tailer.readNewLines();
      expect(lines).toHaveLength(0);
    });

    it('should read new lines after append', async () => {
      await tailer.open(testFile);

      // Append new content
      await fs.promises.appendFile(testFile, 'line 4\nline 5\n');

      const lines = await tailer.readNewLines();

      expect(lines).toHaveLength(2);
      expect(lines[0]?.content).toBe('line 4');
      expect(lines[0]?.lineNumber).toBe(4);
      expect(lines[1]?.content).toBe('line 5');
      expect(lines[1]?.lineNumber).toBe(5);
    });

    it('should read from beginning when configured', async () => {
      const tailerFromStart = new LogTailer({ fromBeginning: true });
      await tailerFromStart.open(testFile);

      const lines = await tailerFromStart.readNewLines();

      expect(lines).toHaveLength(3);
      expect(lines[0]?.content).toBe('line 1');
      expect(lines[1]?.content).toBe('line 2');
      expect(lines[2]?.content).toBe('line 3');

      await tailerFromStart.close();
    });

    it('should track line numbers correctly', async () => {
      const tailerFromStart = new LogTailer({ fromBeginning: true });
      await tailerFromStart.open(testFile);

      const lines = await tailerFromStart.readNewLines();

      expect(lines[0]?.lineNumber).toBe(1);
      expect(lines[1]?.lineNumber).toBe(2);
      expect(lines[2]?.lineNumber).toBe(3);

      await tailerFromStart.close();
    });

    it('should include byte offsets', async () => {
      const tailerFromStart = new LogTailer({ fromBeginning: true });
      await tailerFromStart.open(testFile);

      const lines = await tailerFromStart.readNewLines();

      expect(lines[0]?.byteOffset).toBe(0);
      // "line 1\n" = 7 bytes
      expect(lines[1]?.byteOffset).toBe(7);

      await tailerFromStart.close();
    });

    it('should handle partial lines correctly', async () => {
      await tailer.open(testFile);

      // Append partial line (no newline)
      await fs.promises.appendFile(testFile, 'partial');

      let lines = await tailer.readNewLines();
      expect(lines).toHaveLength(0); // Partial line not returned

      // Complete the line
      await fs.promises.appendFile(testFile, ' complete\n');

      lines = await tailer.readNewLines();
      expect(lines).toHaveLength(1);
      expect(lines[0]?.content).toBe('partial complete');
    });
  });

  describe('async generator', () => {
    it('should yield lines via async generator', async () => {
      const tailerFromStart = new LogTailer({ fromBeginning: true });
      await tailerFromStart.open(testFile);

      const lines: TailLine[] = [];
      for await (const line of tailerFromStart.tail()) {
        lines.push(line);
      }

      expect(lines).toHaveLength(3);

      await tailerFromStart.close();
    });
  });

  describe('getPosition / setPosition', () => {
    it('should get and set position', async () => {
      const tailerFromStart = new LogTailer({ fromBeginning: true });
      await tailerFromStart.open(testFile);

      // Read first line
      const initialPosition = tailerFromStart.getPosition();
      expect(initialPosition.byteOffset).toBe(0);

      await tailerFromStart.readNewLines();
      const afterReadPosition = tailerFromStart.getPosition();
      expect(afterReadPosition.byteOffset).toBeGreaterThan(0);

      // Reset to initial
      tailerFromStart.setPosition(initialPosition);
      expect(tailerFromStart.getPosition().byteOffset).toBe(0);

      await tailerFromStart.close();
    });
  });

  describe('hasNewContent', () => {
    it('should return true when new content available', async () => {
      await tailer.open(testFile);

      expect(await tailer.hasNewContent()).toBe(false);

      await fs.promises.appendFile(testFile, 'new\n');

      expect(await tailer.hasNewContent()).toBe(true);
    });
  });

  describe('getFileSize', () => {
    it('should return file size', async () => {
      await tailer.open(testFile);
      const size = await tailer.getFileSize();
      const actualSize = (await fs.promises.stat(testFile)).size;
      expect(size).toBe(actualSize);
    });

    it('should return 0 when not opened', async () => {
      const size = await tailer.getFileSize();
      expect(size).toBe(0);
    });
  });

  describe('handleRotation', () => {
    it('should reset to beginning of rotated file', async () => {
      await tailer.open(testFile);

      // Simulate rotation
      await fs.promises.writeFile(testFile, 'rotated line 1\n');

      await tailer.handleRotation();

      const lines = await tailer.readNewLines();
      expect(lines).toHaveLength(1);
      expect(lines[0]?.content).toBe('rotated line 1');
      expect(lines[0]?.lineNumber).toBe(1);
    });
  });

  describe('flushPartial', () => {
    it('should return partial line content', async () => {
      await tailer.open(testFile);

      // Append partial line
      await fs.promises.appendFile(testFile, 'partial');
      await tailer.readNewLines();

      const partial = tailer.flushPartial();
      expect(partial?.content).toBe('partial');
    });

    it('should return null when no partial line', async () => {
      await tailer.open(testFile);
      const partial = tailer.flushPartial();
      expect(partial).toBeNull();
    });
  });

  describe('error handling', () => {
    it('should throw error when tailing without opening', async () => {
      await expect(tailer.readNewLines()).rejects.toThrow('File not opened');
    });
  });
});
