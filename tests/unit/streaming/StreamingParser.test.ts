import { StreamingParser } from '../../../src/streaming/StreamingParser';
import type { TailLine } from '../../../src/streaming/types';
import type { CombatEvent, DamageEvent } from '../../../src/types';
import { EventType } from '../../../src/types';

describe('StreamingParser', () => {
  let parser: StreamingParser;

  beforeEach(() => {
    parser = new StreamingParser();
  });

  const createTailLine = (content: string, lineNumber: number = 1): TailLine => ({
    content,
    lineNumber,
    byteOffset: 0,
  });

  describe('processLine', () => {
    it('should parse damage dealt line', () => {
      const line = createTailLine('[12:34:56] You hit the goblin for 150 damage!');
      const event = parser.processLine(line);

      expect(event).not.toBeNull();
      expect(event?.eventType).toBe(EventType.DAMAGE_DEALT);
    });

    it('should parse damage received line', () => {
      const line = createTailLine('[12:34:56] The goblin hits you for 50 damage!');
      const event = parser.processLine(line);

      expect(event).not.toBeNull();
      expect(event?.eventType).toBe(EventType.DAMAGE_RECEIVED);
    });

    it('should parse healing done line', () => {
      const line = createTailLine('[12:34:56] You cast Minor Heal on yourself for 100 hit points.');
      const event = parser.processLine(line);

      expect(event).not.toBeNull();
      expect(event?.eventType).toBe(EventType.HEALING_DONE);
    });

    it('should return unknown event for non-combat lines', () => {
      const line = createTailLine('[12:34:56] Some chat message');
      const event = parser.processLine(line);

      expect(event).not.toBeNull();
      expect(event?.eventType).toBe(EventType.UNKNOWN);
    });

    it('should return unknown event for empty lines', () => {
      const line = createTailLine('');
      const event = parser.processLine(line);

      expect(event?.eventType).toBe(EventType.UNKNOWN);
    });
  });

  describe('event emissions', () => {
    it('should emit "line" for every line', () => {
      const lines: TailLine[] = [];
      parser.on('line', (line) => lines.push(line));

      const tailLine = createTailLine('[12:34:56] test');
      parser.processLine(tailLine);

      expect(lines).toHaveLength(1);
      expect(lines[0]).toBe(tailLine);
    });

    it('should emit "event" for any parsed event', () => {
      const events: CombatEvent[] = [];
      parser.on('event', (event) => events.push(event));

      parser.processLine(createTailLine('[12:34:56] You hit the goblin for 150 damage!'));

      expect(events).toHaveLength(1);
      expect(events[0]?.eventType).toBe(EventType.DAMAGE_DEALT);
    });

    it('should emit "event:damage" for damage events', () => {
      const events: CombatEvent[] = [];
      parser.on('event:damage', (event) => events.push(event));

      parser.processLine(createTailLine('[12:34:56] You hit the goblin for 150 damage!'));
      parser.processLine(createTailLine('[12:34:57] The goblin hits you for 50 damage!'));

      expect(events).toHaveLength(2);
    });

    it('should emit "event:damage:dealt" for damage dealt', () => {
      const events: CombatEvent[] = [];
      parser.on('event:damage:dealt', (event) => events.push(event));

      parser.processLine(createTailLine('[12:34:56] You hit the goblin for 150 damage!'));
      parser.processLine(createTailLine('[12:34:57] The goblin hits you for 50 damage!'));

      expect(events).toHaveLength(1);
      expect(events[0]?.eventType).toBe(EventType.DAMAGE_DEALT);
    });

    it('should emit "event:damage:received" for damage received', () => {
      const events: CombatEvent[] = [];
      parser.on('event:damage:received', (event) => events.push(event));

      parser.processLine(createTailLine('[12:34:56] The goblin hits you for 50 damage!'));

      expect(events).toHaveLength(1);
      expect(events[0]?.eventType).toBe(EventType.DAMAGE_RECEIVED);
    });

    it('should emit "event:healing" for healing events', () => {
      const events: CombatEvent[] = [];
      parser.on('event:healing', (event) => events.push(event));

      parser.processLine(createTailLine('[12:34:56] You cast Minor Heal on yourself for 100 hit points.'));

      expect(events).toHaveLength(1);
    });

    it('should emit "event:unknown" for unknown events', () => {
      const events: CombatEvent[] = [];
      parser.on('event:unknown', (event) => events.push(event));

      parser.processLine(createTailLine('[12:34:56] Random chat message'));

      expect(events).toHaveLength(1);
      expect(events[0]?.eventType).toBe(EventType.UNKNOWN);
    });

    it('should emit error for parse errors', () => {
      const errors: Error[] = [];
      parser.on('error', (error) => errors.push(error));

      // Line without timestamp
      parser.processLine(createTailLine('No timestamp here'));

      expect(errors).toHaveLength(1);
    });
  });

  describe('processLines', () => {
    it('should process multiple lines', () => {
      const lines = [
        createTailLine('[12:34:56] You hit the goblin for 100 damage!', 1),
        createTailLine('[12:34:57] The goblin hits you for 50 damage!', 2),
        createTailLine('[12:34:58] Random message', 3),
      ];

      const events = parser.processLines(lines);

      expect(events).toHaveLength(3);
      expect(events[0]?.eventType).toBe(EventType.DAMAGE_DEALT);
      expect(events[1]?.eventType).toBe(EventType.DAMAGE_RECEIVED);
      expect(events[2]?.eventType).toBe(EventType.UNKNOWN);
    });
  });

  describe('statistics', () => {
    it('should track lines processed', () => {
      parser.processLine(createTailLine('[12:34:56] test 1'));
      parser.processLine(createTailLine('[12:34:57] test 2'));

      const stats = parser.getStats();
      expect(stats.linesProcessed).toBe(2);
    });

    it('should track events emitted', () => {
      parser.processLine(createTailLine('[12:34:56] You hit the goblin for 100 damage!'));
      parser.processLine(createTailLine('[12:34:57] Random chat'));

      const stats = parser.getStats();
      expect(stats.eventsEmitted).toBe(2);
    });

    it('should track errors', () => {
      // Register error handler to prevent unhandled errors
      parser.on('error', () => {});

      parser.processLine(createTailLine('No timestamp'));
      parser.processLine(createTailLine('Also no timestamp'));

      const stats = parser.getStats();
      expect(stats.errors).toBe(2);
    });

    it('should track events by type', () => {
      parser.processLine(createTailLine('[12:34:56] You hit the goblin for 100 damage!'));
      parser.processLine(createTailLine('[12:34:57] You hit the goblin for 150 damage!'));
      parser.processLine(createTailLine('[12:34:58] The goblin hits you for 50 damage!'));

      const stats = parser.getStats();
      expect(stats.eventsByType['DAMAGE_DEALT']).toBe(2);
      expect(stats.eventsByType['DAMAGE_RECEIVED']).toBe(1);
    });

    it('should reset statistics', () => {
      parser.processLine(createTailLine('[12:34:56] test'));
      parser.resetStats();

      const stats = parser.getStats();
      expect(stats.linesProcessed).toBe(0);
      expect(stats.eventsEmitted).toBe(0);
    });
  });

  describe('getLineParser', () => {
    it('should return the underlying LineParser', () => {
      const lineParser = parser.getLineParser();
      expect(lineParser).toBeDefined();
      expect(lineParser.getRegistry).toBeDefined();
    });
  });
});
