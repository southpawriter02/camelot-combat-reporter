import { LineParser } from '../../../src/parser/LineParser';
import { EventType } from '../../../src/types';

describe('LineParser', () => {
  const parser = new LineParser();

  describe('parseLine', () => {
    it('should parse a damage line correctly', () => {
      const result = parser.parseLine('[12:34:56] You hit the goblin for 150 damage!', 1);

      expect(result.success).toBe(true);
      expect(result.event).not.toBeNull();
      expect(result.event!.eventType).toBe(EventType.DAMAGE_DEALT);
    });

    it('should parse a healing line correctly', () => {
      const result = parser.parseLine(
        '[12:34:56] You cast Minor Heal on yourself for 200 hit points.',
        1
      );

      expect(result.success).toBe(true);
      expect(result.event).not.toBeNull();
      expect(result.event!.eventType).toBe(EventType.HEALING_DONE);
    });

    it('should parse a crowd control line correctly', () => {
      const result = parser.parseLine('[12:34:56] The troll is stunned for 9 seconds!', 1);

      expect(result.success).toBe(true);
      expect(result.event).not.toBeNull();
      expect(result.event!.eventType).toBe(EventType.CROWD_CONTROL);
    });

    it('should return unknown event for unrecognized format', () => {
      const result = parser.parseLine('[12:34:56] This is a chat message', 1);

      expect(result.success).toBe(true);
      expect(result.event).not.toBeNull();
      expect(result.event!.eventType).toBe(EventType.UNKNOWN);
    });

    it('should handle empty lines', () => {
      const result = parser.parseLine('', 1);

      expect(result.success).toBe(true);
      expect(result.event).not.toBeNull();
      expect(result.event!.eventType).toBe(EventType.UNKNOWN);
    });

    it('should handle lines without timestamp', () => {
      const result = parser.parseLine('You hit the goblin for 150 damage!', 1);

      expect(result.success).toBe(false);
      expect(result.error).not.toBeNull();
      expect(result.event).not.toBeNull();
      expect(result.event!.eventType).toBe(EventType.UNKNOWN);
    });

    it('should preserve timestamp in event', () => {
      const result = parser.parseLine('[12:34:56] You hit the goblin for 150 damage!', 1);

      expect(result.success).toBe(true);
      expect(result.event!.rawTimestamp).toBe('[12:34:56]');
      expect(result.event!.timestamp.getHours()).toBe(12);
      expect(result.event!.timestamp.getMinutes()).toBe(34);
      expect(result.event!.timestamp.getSeconds()).toBe(56);
    });

    it('should preserve raw line in event', () => {
      const line = '[12:34:56] You hit the goblin for 150 damage!';
      const result = parser.parseLine(line, 1);

      expect(result.success).toBe(true);
      expect(result.event!.rawLine).toBe(line);
    });

    it('should preserve line number in event', () => {
      const result = parser.parseLine('[12:34:56] You hit the goblin for 150 damage!', 42);

      expect(result.success).toBe(true);
      expect(result.event!.lineNumber).toBe(42);
    });
  });

  describe('getRegistry', () => {
    it('should return the pattern registry', () => {
      const registry = parser.getRegistry();

      expect(registry).not.toBeNull();
      expect(registry.getHandlers().length).toBeGreaterThan(0);
    });
  });
});
