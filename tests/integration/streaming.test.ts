import * as fs from 'fs';
import * as path from 'path';
import * as os from 'os';
import { RealTimeMonitor } from '../../src/streaming/RealTimeMonitor';
import type { SessionUpdate, TailLine } from '../../src/streaming/types';
import type { CombatEvent } from '../../src/types';
import { EventType } from '../../src/types';

describe('Streaming Integration Tests', () => {
  let tempDir: string;
  let logFile: string;
  let monitor: RealTimeMonitor;

  beforeEach(async () => {
    // Create temp directory and empty log file
    tempDir = await fs.promises.mkdtemp(path.join(os.tmpdir(), 'streaming-test-'));
    logFile = path.join(tempDir, 'chat.log');
    await fs.promises.writeFile(logFile, '');

    monitor = new RealTimeMonitor({
      sessionDetector: {
        inactivityTimeoutMs: 100,
        minEventsForSession: 2,
        minDurationMs: 10,
      },
    });
  });

  afterEach(async () => {
    await monitor.destroy();

    try {
      await fs.promises.rm(tempDir, { recursive: true });
    } catch {
      // Ignore cleanup errors
    }
  });

  // Helper to append log lines
  const appendLogLine = async (line: string) => {
    await fs.promises.appendFile(logFile, line + '\n');
  };

  // Helper to wait for events
  const waitForEvents = (ms: number = 200) =>
    new Promise(resolve => setTimeout(resolve, ms));

  describe('full pipeline', () => {
    it('should monitor file and emit events', async () => {
      const events: CombatEvent[] = [];
      monitor.on('event', (event) => events.push(event));

      await monitor.start(logFile, { fromBeginning: true });

      // Append combat lines
      await appendLogLine('[12:34:56] You hit the goblin for 100 damage!');
      await appendLogLine('[12:34:57] The goblin hits you for 50 damage!');

      await waitForEvents();

      expect(events.length).toBeGreaterThanOrEqual(2);
      expect(events.some(e => e.eventType === EventType.DAMAGE_DEALT)).toBe(true);
      expect(events.some(e => e.eventType === EventType.DAMAGE_RECEIVED)).toBe(true);
    });

    it('should detect sessions from streaming events', async () => {
      const sessionStarts: SessionUpdate[] = [];
      const sessionEnds: SessionUpdate[] = [];

      monitor.on('session:start', (update) => sessionStarts.push(update));
      monitor.on('session:end', (update) => sessionEnds.push(update));

      await monitor.start(logFile, { fromBeginning: true });

      // Add all events quickly before timeout (100ms) expires
      await appendLogLine('[12:34:56] You hit the goblin for 100 damage!');
      await appendLogLine('[12:34:57] You hit the goblin for 150 damage!');
      await appendLogLine('[12:34:58] You hit the goblin for 200 damage!');

      // Wait for events to be processed
      await waitForEvents(250);

      expect(sessionStarts).toHaveLength(1);

      // Wait for session to end (timeout is 100ms + buffer)
      await waitForEvents(300);

      expect(sessionEnds).toHaveLength(1);
      expect(sessionEnds[0]?.session.events.length).toBeGreaterThanOrEqual(2);
    });

    it('should emit hierarchical events', async () => {
      const damageEvents: CombatEvent[] = [];
      const damageDealtEvents: CombatEvent[] = [];

      monitor.on('event:damage', (event) => damageEvents.push(event));
      monitor.on('event:damage:dealt', (event) => damageDealtEvents.push(event));

      await monitor.start(logFile, { fromBeginning: true });

      await appendLogLine('[12:34:56] You hit the goblin for 100 damage!');
      await appendLogLine('[12:34:57] The goblin hits you for 50 damage!');

      await waitForEvents();

      expect(damageEvents).toHaveLength(2);
      expect(damageDealtEvents).toHaveLength(1);
    });

    it('should emit line events', async () => {
      const lines: TailLine[] = [];
      monitor.on('line', (line) => lines.push(line));

      await monitor.start(logFile, { fromBeginning: true });

      await appendLogLine('[12:34:56] Test line 1');
      await appendLogLine('[12:34:57] Test line 2');

      await waitForEvents();

      expect(lines).toHaveLength(2);
      expect(lines[0]?.content).toBe('[12:34:56] Test line 1');
    });
  });

  describe('monitor lifecycle', () => {
    it('should start and stop', async () => {
      expect(monitor.getStatus().state).toBe('stopped');

      await monitor.start(logFile);
      expect(monitor.getStatus().state).toBe('running');

      await monitor.stop();
      expect(monitor.getStatus().state).toBe('stopped');
    });

    it('should pause and resume', async () => {
      await monitor.start(logFile);
      expect(monitor.getStatus().state).toBe('running');

      monitor.pause();
      expect(monitor.getStatus().state).toBe('paused');

      monitor.resume();
      expect(monitor.getStatus().state).toBe('running');
    });

    it('should save and restore position', async () => {
      await monitor.start(logFile, { fromBeginning: true });

      await appendLogLine('[12:34:56] Line 1');
      await appendLogLine('[12:34:57] Line 2');
      await waitForEvents();

      // Save position
      const position = monitor.getPosition();
      await monitor.stop();

      // Create new monitor and resume
      const monitor2 = new RealTimeMonitor();
      const lines: TailLine[] = [];
      monitor2.on('line', (line) => lines.push(line));

      await monitor2.start(logFile, { resumeFrom: position });

      // Add more lines
      await appendLogLine('[12:34:58] Line 3');
      await waitForEvents();

      await monitor2.destroy();

      // Should only get line 3
      expect(lines).toHaveLength(1);
      expect(lines[0]?.content).toBe('[12:34:58] Line 3');
    });
  });

  describe('status and stats', () => {
    it('should report status', async () => {
      await monitor.start(logFile, { fromBeginning: true });

      await appendLogLine('[12:34:56] You hit the goblin for 100 damage!');
      await waitForEvents();

      const status = monitor.getStatus();

      expect(status.state).toBe('running');
      expect(status.filename).toBe(logFile);
      expect(status.stats.linesProcessed).toBeGreaterThanOrEqual(1);
      expect(status.stats.eventsEmitted).toBeGreaterThanOrEqual(1);
    });

    it('should track active session in status', async () => {
      await monitor.start(logFile, { fromBeginning: true });

      await appendLogLine('[12:34:56] You hit the goblin for 100 damage!');
      await waitForEvents(150);

      const status = monitor.getStatus();
      expect(status.activeSession).toBeDefined();
      expect(status.activeSession?.eventCount).toBeGreaterThanOrEqual(1);
    });
  });

  describe('error handling', () => {
    it('should handle non-existent file', async () => {
      const nonExistent = path.join(tempDir, 'nonexistent.log');

      await expect(monitor.start(nonExistent)).rejects.toThrow();
      expect(monitor.getStatus().state).toBe('error');
    });

    it('should not allow starting when already running', async () => {
      await monitor.start(logFile);

      await expect(monitor.start(logFile)).rejects.toThrow('Cannot start');
    });

    it('should emit monitor:error on errors', async () => {
      const errors: Error[] = [];
      monitor.on('monitor:error', (error) => errors.push(error));

      const nonExistent = path.join(tempDir, 'nonexistent.log');

      try {
        await monitor.start(nonExistent);
      } catch {
        // Expected
      }

      expect(errors).toHaveLength(1);
    });
  });

  describe('monitor events', () => {
    it('should emit monitor:started', async () => {
      const startEvents: string[] = [];
      monitor.on('monitor:started', (filename) => startEvents.push(filename));

      await monitor.start(logFile);

      expect(startEvents).toHaveLength(1);
      expect(startEvents[0]).toBe(logFile);
    });

    it('should emit monitor:stopped', async () => {
      let stopped = false;
      monitor.on('monitor:stopped', () => { stopped = true; });

      await monitor.start(logFile);
      await monitor.stop();

      expect(stopped).toBe(true);
    });

    it('should emit monitor:paused and monitor:resumed', async () => {
      let paused = false;
      let resumed = false;

      monitor.on('monitor:paused', () => { paused = true; });
      monitor.on('monitor:resumed', () => { resumed = true; });

      await monitor.start(logFile);
      monitor.pause();
      monitor.resume();

      expect(paused).toBe(true);
      expect(resumed).toBe(true);
    });
  });

  describe('webhooks integration', () => {
    it('should manage webhooks', async () => {
      const id = monitor.addWebhook({ url: 'https://example.com/hook' });
      expect(monitor.getWebhooks()).toHaveLength(1);

      monitor.removeWebhook(id);
      expect(monitor.getWebhooks()).toHaveLength(0);
    });
  });

  describe('growing file scenario', () => {
    it('should continuously process growing log file', async () => {
      const events: CombatEvent[] = [];
      monitor.on('event', (event) => {
        if (event.eventType !== EventType.UNKNOWN) {
          events.push(event);
        }
      });

      await monitor.start(logFile, { fromBeginning: true });

      // Simulate log growth over time
      for (let i = 1; i <= 5; i++) {
        await appendLogLine(`[12:34:5${i}] You hit the goblin for ${i * 100} damage!`);
        await waitForEvents(50);
      }

      await waitForEvents(100);

      expect(events.length).toBeGreaterThanOrEqual(5);
    });
  });
});
