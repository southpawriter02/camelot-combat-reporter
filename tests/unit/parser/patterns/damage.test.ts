import { DamagePatternHandler } from '../../../../src/parser/patterns/damage';
import { EventType, DamageType, ActionType } from '../../../../src/types';

describe('DamagePatternHandler', () => {
  const handler = new DamagePatternHandler();
  const baseTimestamp = new Date('2024-01-01T12:00:00');
  const rawTimestamp = '[12:00:00]';

  describe('canHandle', () => {
    it('should handle player hit target messages', () => {
      expect(handler.canHandle('You hit the goblin for 150 damage!')).toBe(true);
    });

    it('should handle target hit player messages', () => {
      expect(handler.canHandle('The goblin hits you for 75 damage!')).toBe(true);
    });

    it('should handle target hit player with absorption', () => {
      expect(handler.canHandle('The goblin hits you for 75 (-25) damage!')).toBe(true);
    });

    it('should handle weapon attack messages', () => {
      expect(
        handler.canHandle('You attack the goblin with your sword and hit for 100 damage!')
      ).toBe(true);
    });

    it('should handle style attack messages', () => {
      expect(handler.canHandle('You perform Side Stun and hit the troll for 180 damage!')).toBe(
        true
      );
    });

    it('should handle spell damage messages', () => {
      expect(handler.canHandle('Your Greater Fireball hits the goblin for 300 damage!')).toBe(
        true
      );
    });

    it('should handle spell damage with type', () => {
      expect(
        handler.canHandle('Your Greater Fireball hits the goblin for 300 heat damage!')
      ).toBe(true);
    });

    it('should handle critical hits', () => {
      expect(handler.canHandle('You critically hit the goblin for 250 damage!')).toBe(true);
    });

    it('should not handle non-damage messages', () => {
      expect(handler.canHandle('You cast Minor Heal on yourself for 200 hit points.')).toBe(
        false
      );
    });
  });

  describe('parse - player hit target', () => {
    it('should parse basic hit message', () => {
      const message = 'You hit the goblin for 150 damage!';
      const rawLine = `${rawTimestamp} ${message}`;
      const event = handler.parse(message, baseTimestamp, rawTimestamp, rawLine, 1);

      expect(event).not.toBeNull();
      expect(event!.eventType).toBe(EventType.DAMAGE_DEALT);

      if (event!.eventType === EventType.DAMAGE_DEALT) {
        expect(event!.source.isSelf).toBe(true);
        expect(event!.target.name).toBe('goblin');
        expect(event!.amount).toBe(150);
        expect(event!.actionType).toBe(ActionType.MELEE);
      }
    });

    it('should parse hit on named NPC without "the"', () => {
      const message = 'You hit Gokstad Warrior for 200 damage!';
      const rawLine = `${rawTimestamp} ${message}`;
      const event = handler.parse(message, baseTimestamp, rawTimestamp, rawLine, 1);

      expect(event).not.toBeNull();
      if (event!.eventType === EventType.DAMAGE_DEALT) {
        expect(event!.target.name).toBe('Gokstad Warrior');
      }
    });
  });

  describe('parse - target hits player', () => {
    it('should parse incoming damage without absorption', () => {
      const message = 'The goblin hits you for 75 damage!';
      const rawLine = `${rawTimestamp} ${message}`;
      const event = handler.parse(message, baseTimestamp, rawTimestamp, rawLine, 1);

      expect(event).not.toBeNull();
      expect(event!.eventType).toBe(EventType.DAMAGE_RECEIVED);

      if (event!.eventType === EventType.DAMAGE_RECEIVED) {
        expect(event!.source.name).toBe('goblin');
        expect(event!.target.isSelf).toBe(true);
        expect(event!.amount).toBe(75);
        expect(event!.absorbedAmount).toBe(0);
        expect(event!.effectiveAmount).toBe(75);
      }
    });

    it('should parse incoming damage with absorption', () => {
      const message = 'The goblin hits you for 75 (-25) damage!';
      const rawLine = `${rawTimestamp} ${message}`;
      const event = handler.parse(message, baseTimestamp, rawTimestamp, rawLine, 1);

      expect(event).not.toBeNull();
      if (event!.eventType === EventType.DAMAGE_RECEIVED) {
        expect(event!.amount).toBe(75);
        expect(event!.absorbedAmount).toBe(25);
        expect(event!.effectiveAmount).toBe(50);
      }
    });
  });

  describe('parse - weapon attack', () => {
    it('should parse attack with weapon', () => {
      const message = 'You attack the goblin with your sword and hit for 100 damage!';
      const rawLine = `${rawTimestamp} ${message}`;
      const event = handler.parse(message, baseTimestamp, rawTimestamp, rawLine, 1);

      expect(event).not.toBeNull();
      if (event!.eventType === EventType.DAMAGE_DEALT) {
        expect(event!.target.name).toBe('goblin');
        expect(event!.amount).toBe(100);
        expect(event!.weaponName).toBe('sword');
        expect(event!.actionType).toBe(ActionType.MELEE);
      }
    });
  });

  describe('parse - style attack', () => {
    it('should parse style attack', () => {
      const message = 'You perform Side Stun and hit the troll for 180 damage!';
      const rawLine = `${rawTimestamp} ${message}`;
      const event = handler.parse(message, baseTimestamp, rawTimestamp, rawLine, 1);

      expect(event).not.toBeNull();
      if (event!.eventType === EventType.DAMAGE_DEALT) {
        expect(event!.target.name).toBe('troll');
        expect(event!.amount).toBe(180);
        expect(event!.actionName).toBe('Side Stun');
        expect(event!.actionType).toBe(ActionType.STYLE);
      }
    });
  });

  describe('parse - spell damage', () => {
    it('should parse spell damage without type', () => {
      const message = 'Your Greater Fireball hits the goblin for 300 damage!';
      const rawLine = `${rawTimestamp} ${message}`;
      const event = handler.parse(message, baseTimestamp, rawTimestamp, rawLine, 1);

      expect(event).not.toBeNull();
      if (event!.eventType === EventType.DAMAGE_DEALT) {
        expect(event!.target.name).toBe('goblin');
        expect(event!.amount).toBe(300);
        expect(event!.actionName).toBe('Greater Fireball');
        expect(event!.actionType).toBe(ActionType.SPELL);
        expect(event!.damageType).toBe(DamageType.UNKNOWN);
      }
    });

    it('should parse spell damage with heat type', () => {
      const message = 'Your Greater Fireball hits the goblin for 300 heat damage!';
      const rawLine = `${rawTimestamp} ${message}`;
      const event = handler.parse(message, baseTimestamp, rawTimestamp, rawLine, 1);

      expect(event).not.toBeNull();
      if (event!.eventType === EventType.DAMAGE_DEALT) {
        expect(event!.damageType).toBe(DamageType.HEAT);
      }
    });

    it('should parse spell damage with cold type', () => {
      const message = 'Your Ice Bolt hits the frost giant for 450 cold damage!';
      const rawLine = `${rawTimestamp} ${message}`;
      const event = handler.parse(message, baseTimestamp, rawTimestamp, rawLine, 1);

      expect(event).not.toBeNull();
      if (event!.eventType === EventType.DAMAGE_DEALT) {
        expect(event!.damageType).toBe(DamageType.COLD);
      }
    });
  });

  describe('parse - critical hits', () => {
    it('should parse player critical hit', () => {
      const message = 'You critically hit the goblin for 250 damage!';
      const rawLine = `${rawTimestamp} ${message}`;
      const event = handler.parse(message, baseTimestamp, rawTimestamp, rawLine, 1);

      expect(event).not.toBeNull();
      if (event!.eventType === EventType.DAMAGE_DEALT) {
        expect(event!.amount).toBe(250);
        expect(event!.isCritical).toBe(true);
      }
    });

    it('should parse incoming critical hit', () => {
      const message = 'The troll critically hits you for 200 damage!';
      const rawLine = `${rawTimestamp} ${message}`;
      const event = handler.parse(message, baseTimestamp, rawTimestamp, rawLine, 1);

      expect(event).not.toBeNull();
      expect(event!.eventType).toBe(EventType.DAMAGE_RECEIVED);
      if (event!.eventType === EventType.DAMAGE_RECEIVED) {
        expect(event!.amount).toBe(200);
        expect(event!.isCritical).toBe(true);
      }
    });
  });
});
