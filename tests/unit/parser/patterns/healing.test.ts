import { HealingPatternHandler } from '../../../../src/parser/patterns/healing';
import { EventType } from '../../../../src/types';

describe('HealingPatternHandler', () => {
  const handler = new HealingPatternHandler();
  const baseTimestamp = new Date('2024-01-01T12:00:00');
  const rawTimestamp = '[12:00:00]';

  describe('canHandle', () => {
    it('should handle self heal messages', () => {
      expect(handler.canHandle('You cast Minor Heal on yourself for 200 hit points.')).toBe(true);
    });

    it('should handle target heal messages', () => {
      expect(handler.canHandle('You cast Major Heal on Playername for 500 hit points.')).toBe(
        true
      );
    });

    it('should handle incoming heal messages', () => {
      expect(handler.canHandle('Healer heals you for 300 hit points.')).toBe(true);
    });

    it('should handle passive heal messages', () => {
      expect(handler.canHandle('You are healed for 150 hit points.')).toBe(true);
    });

    it('should not handle damage messages', () => {
      expect(handler.canHandle('You hit the goblin for 150 damage!')).toBe(false);
    });
  });

  describe('parse - self heal', () => {
    it('should parse self heal', () => {
      const message = 'You cast Minor Heal on yourself for 200 hit points.';
      const rawLine = `${rawTimestamp} ${message}`;
      const event = handler.parse(message, baseTimestamp, rawTimestamp, rawLine, 1);

      expect(event).not.toBeNull();
      expect(event!.eventType).toBe(EventType.HEALING_DONE);

      if (event!.eventType === EventType.HEALING_DONE) {
        expect(event!.source.isSelf).toBe(true);
        expect(event!.target.isSelf).toBe(true);
        expect(event!.amount).toBe(200);
        expect(event!.spellName).toBe('Minor Heal');
      }
    });
  });

  describe('parse - heal target', () => {
    it('should parse heal on another player', () => {
      const message = 'You cast Major Heal on Playername for 500 hit points.';
      const rawLine = `${rawTimestamp} ${message}`;
      const event = handler.parse(message, baseTimestamp, rawTimestamp, rawLine, 1);

      expect(event).not.toBeNull();
      expect(event!.eventType).toBe(EventType.HEALING_DONE);

      if (event!.eventType === EventType.HEALING_DONE) {
        expect(event!.source.isSelf).toBe(true);
        expect(event!.target.name).toBe('Playername');
        expect(event!.target.isSelf).toBe(false);
        expect(event!.amount).toBe(500);
        expect(event!.spellName).toBe('Major Heal');
      }
    });
  });

  describe('parse - incoming heal', () => {
    it('should parse heal received from another player', () => {
      const message = 'Healer heals you for 300 hit points.';
      const rawLine = `${rawTimestamp} ${message}`;
      const event = handler.parse(message, baseTimestamp, rawTimestamp, rawLine, 1);

      expect(event).not.toBeNull();
      expect(event!.eventType).toBe(EventType.HEALING_RECEIVED);

      if (event!.eventType === EventType.HEALING_RECEIVED) {
        expect(event!.source.name).toBe('Healer');
        expect(event!.target.isSelf).toBe(true);
        expect(event!.amount).toBe(300);
      }
    });
  });

  describe('parse - passive heal', () => {
    it('should parse passive heal', () => {
      const message = 'You are healed for 150 hit points.';
      const rawLine = `${rawTimestamp} ${message}`;
      const event = handler.parse(message, baseTimestamp, rawTimestamp, rawLine, 1);

      expect(event).not.toBeNull();
      expect(event!.eventType).toBe(EventType.HEALING_RECEIVED);

      if (event!.eventType === EventType.HEALING_RECEIVED) {
        expect(event!.target.isSelf).toBe(true);
        expect(event!.amount).toBe(150);
      }
    });
  });
});
