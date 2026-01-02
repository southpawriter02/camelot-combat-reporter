import { CrowdControlPatternHandler } from '../../../../src/parser/patterns/crowdControl';
import { EventType, CrowdControlEffect } from '../../../../src/types';

describe('CrowdControlPatternHandler', () => {
  const handler = new CrowdControlPatternHandler();
  const baseTimestamp = new Date('2024-01-01T12:00:00');
  const rawTimestamp = '[12:00:00]';

  describe('canHandle', () => {
    it('should handle stun messages', () => {
      expect(handler.canHandle('The troll is stunned for 9 seconds!')).toBe(true);
      expect(handler.canHandle('You are stunned for 5 seconds!')).toBe(true);
    });

    it('should handle mez messages', () => {
      expect(handler.canHandle('The goblin is mesmerized!')).toBe(true);
      expect(handler.canHandle('You are mesmerized!')).toBe(true);
    });

    it('should handle root messages', () => {
      expect(handler.canHandle('The troll is rooted for 15 seconds!')).toBe(true);
      expect(handler.canHandle('You are rooted for 10 seconds!')).toBe(true);
    });

    it('should handle snare messages', () => {
      expect(handler.canHandle('The goblin is snared!')).toBe(true);
      expect(handler.canHandle('You are snared!')).toBe(true);
    });

    it('should handle resist messages', () => {
      expect(handler.canHandle('The goblin resists the effect!')).toBe(true);
      expect(handler.canHandle('You resist the effect!')).toBe(true);
    });

    it('should not handle damage messages', () => {
      expect(handler.canHandle('You hit the goblin for 150 damage!')).toBe(false);
    });
  });

  describe('parse - stun', () => {
    it('should parse target stun', () => {
      const message = 'The troll is stunned for 9 seconds!';
      const rawLine = `${rawTimestamp} ${message}`;
      const event = handler.parse(message, baseTimestamp, rawTimestamp, rawLine, 1);

      expect(event).not.toBeNull();
      expect(event!.eventType).toBe(EventType.CROWD_CONTROL);

      if (event!.eventType === EventType.CROWD_CONTROL) {
        expect(event!.target.name).toBe('troll');
        expect(event!.effect).toBe(CrowdControlEffect.STUN);
        expect(event!.duration).toBe(9);
        expect(event!.isResisted).toBe(false);
      }
    });

    it('should parse player stun', () => {
      const message = 'You are stunned for 5 seconds!';
      const rawLine = `${rawTimestamp} ${message}`;
      const event = handler.parse(message, baseTimestamp, rawTimestamp, rawLine, 1);

      expect(event).not.toBeNull();
      if (event!.eventType === EventType.CROWD_CONTROL) {
        expect(event!.target.isSelf).toBe(true);
        expect(event!.effect).toBe(CrowdControlEffect.STUN);
        expect(event!.duration).toBe(5);
      }
    });

    it('should handle singular "second"', () => {
      const message = 'You are stunned for 1 second!';
      const rawLine = `${rawTimestamp} ${message}`;
      const event = handler.parse(message, baseTimestamp, rawTimestamp, rawLine, 1);

      expect(event).not.toBeNull();
      if (event!.eventType === EventType.CROWD_CONTROL) {
        expect(event!.duration).toBe(1);
      }
    });
  });

  describe('parse - mez', () => {
    it('should parse target mez', () => {
      const message = 'The goblin is mesmerized!';
      const rawLine = `${rawTimestamp} ${message}`;
      const event = handler.parse(message, baseTimestamp, rawTimestamp, rawLine, 1);

      expect(event).not.toBeNull();
      if (event!.eventType === EventType.CROWD_CONTROL) {
        expect(event!.target.name).toBe('goblin');
        expect(event!.effect).toBe(CrowdControlEffect.MESMERIZE);
        expect(event!.duration).toBe(0); // Duration unknown for mez
      }
    });

    it('should parse player mez', () => {
      const message = 'You are mesmerized!';
      const rawLine = `${rawTimestamp} ${message}`;
      const event = handler.parse(message, baseTimestamp, rawTimestamp, rawLine, 1);

      expect(event).not.toBeNull();
      if (event!.eventType === EventType.CROWD_CONTROL) {
        expect(event!.target.isSelf).toBe(true);
        expect(event!.effect).toBe(CrowdControlEffect.MESMERIZE);
      }
    });
  });

  describe('parse - root', () => {
    it('should parse target root', () => {
      const message = 'The troll is rooted for 15 seconds!';
      const rawLine = `${rawTimestamp} ${message}`;
      const event = handler.parse(message, baseTimestamp, rawTimestamp, rawLine, 1);

      expect(event).not.toBeNull();
      if (event!.eventType === EventType.CROWD_CONTROL) {
        expect(event!.target.name).toBe('troll');
        expect(event!.effect).toBe(CrowdControlEffect.ROOT);
        expect(event!.duration).toBe(15);
      }
    });

    it('should parse player root', () => {
      const message = 'You are rooted for 10 seconds!';
      const rawLine = `${rawTimestamp} ${message}`;
      const event = handler.parse(message, baseTimestamp, rawTimestamp, rawLine, 1);

      expect(event).not.toBeNull();
      if (event!.eventType === EventType.CROWD_CONTROL) {
        expect(event!.target.isSelf).toBe(true);
        expect(event!.effect).toBe(CrowdControlEffect.ROOT);
        expect(event!.duration).toBe(10);
      }
    });
  });

  describe('parse - snare', () => {
    it('should parse target snare', () => {
      const message = 'The goblin is snared!';
      const rawLine = `${rawTimestamp} ${message}`;
      const event = handler.parse(message, baseTimestamp, rawTimestamp, rawLine, 1);

      expect(event).not.toBeNull();
      if (event!.eventType === EventType.CROWD_CONTROL) {
        expect(event!.target.name).toBe('goblin');
        expect(event!.effect).toBe(CrowdControlEffect.SNARE);
      }
    });
  });

  describe('parse - resist', () => {
    it('should parse target resist', () => {
      const message = 'The goblin resists the effect!';
      const rawLine = `${rawTimestamp} ${message}`;
      const event = handler.parse(message, baseTimestamp, rawTimestamp, rawLine, 1);

      expect(event).not.toBeNull();
      if (event!.eventType === EventType.CROWD_CONTROL) {
        expect(event!.target.name).toBe('goblin');
        expect(event!.isResisted).toBe(true);
      }
    });

    it('should parse player resist', () => {
      const message = 'You resist the effect!';
      const rawLine = `${rawTimestamp} ${message}`;
      const event = handler.parse(message, baseTimestamp, rawTimestamp, rawLine, 1);

      expect(event).not.toBeNull();
      if (event!.eventType === EventType.CROWD_CONTROL) {
        expect(event!.target.isSelf).toBe(true);
        expect(event!.isResisted).toBe(true);
      }
    });
  });
});
