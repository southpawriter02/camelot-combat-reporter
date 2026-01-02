import { DPSCalculator } from '../../../../src/analysis/metrics/DPSCalculator';
import { EventType, DamageType, ActionType, EntityType } from '../../../../src/types';
import type { DamageEvent, HealingEvent, Entity } from '../../../../src/types';

describe('DPSCalculator', () => {
  const calculator = new DPSCalculator(5000); // 5 second window

  const createEntity = (name: string): Entity => ({
    name,
    entityType: EntityType.PLAYER,
    isPlayer: true,
    isSelf: false,
  });

  const createDamageEvent = (
    timestamp: Date,
    amount: number,
    source: Entity,
    target: Entity
  ): DamageEvent => ({
    id: `dmg-${Date.now()}-${Math.random()}`,
    timestamp,
    rawTimestamp: '[12:00:00]',
    rawLine: 'test',
    lineNumber: 1,
    eventType: EventType.DAMAGE_DEALT,
    source,
    target,
    amount,
    effectiveAmount: amount,
    absorbedAmount: 0,
    damageType: DamageType.CRUSH,
    actionType: ActionType.MELEE,
    isCritical: false,
    isBlocked: false,
    isParried: false,
    isEvaded: false,
  });

  const createHealingEvent = (
    timestamp: Date,
    amount: number,
    source: Entity,
    target: Entity
  ): HealingEvent => ({
    id: `heal-${Date.now()}-${Math.random()}`,
    timestamp,
    rawTimestamp: '[12:00:00]',
    rawLine: 'test',
    lineNumber: 1,
    eventType: EventType.HEALING_DONE,
    source,
    target,
    amount,
    effectiveAmount: amount,
    overheal: 0,
    spellName: 'Greater Heal',
    isCritical: false,
  });

  describe('calculateAverageDPS', () => {
    it('should return 0 for empty events', () => {
      expect(calculator.calculateAverageDPS([], 10000)).toBe(0);
    });

    it('should return 0 for zero duration', () => {
      const player = createEntity('Player');
      const enemy = createEntity('Enemy');
      const events = [createDamageEvent(new Date(), 100, player, enemy)];
      expect(calculator.calculateAverageDPS(events, 0)).toBe(0);
    });

    it('should calculate correct average DPS', () => {
      const player = createEntity('Player');
      const enemy = createEntity('Enemy');
      const baseTime = new Date(2024, 0, 1, 12, 0, 0);

      const events = [
        createDamageEvent(new Date(baseTime.getTime()), 100, player, enemy),
        createDamageEvent(new Date(baseTime.getTime() + 1000), 200, player, enemy),
        createDamageEvent(new Date(baseTime.getTime() + 2000), 150, player, enemy),
      ];

      // Total damage: 450, Duration: 10 seconds = 45 DPS
      expect(calculator.calculateAverageDPS(events, 10000)).toBe(45);
    });
  });

  describe('calculatePeakDPS', () => {
    it('should return 0 for empty events', () => {
      const result = calculator.calculatePeakDPS([]);
      expect(result.value).toBe(0);
    });

    it('should calculate peak DPS correctly', () => {
      const player = createEntity('Player');
      const enemy = createEntity('Enemy');
      const baseTime = new Date(2024, 0, 1, 12, 0, 0);

      // Spread out events over time with a burst in the middle
      const events = [
        createDamageEvent(new Date(baseTime.getTime()), 50, player, enemy),
        createDamageEvent(new Date(baseTime.getTime() + 10000), 500, player, enemy),
        createDamageEvent(new Date(baseTime.getTime() + 11000), 500, player, enemy),
        createDamageEvent(new Date(baseTime.getTime() + 12000), 500, player, enemy),
        createDamageEvent(new Date(baseTime.getTime() + 20000), 50, player, enemy),
      ];

      const result = calculator.calculatePeakDPS(events);
      // Peak should be during the burst window (1500 damage over ~2 seconds)
      expect(result.value).toBeGreaterThan(0);
    });
  });

  describe('calculateAverageHPS', () => {
    it('should return 0 for empty events', () => {
      expect(calculator.calculateAverageHPS([], 10000)).toBe(0);
    });

    it('should calculate correct average HPS', () => {
      const healer = createEntity('Healer');
      const target = createEntity('Tank');
      const baseTime = new Date(2024, 0, 1, 12, 0, 0);

      const events = [
        createHealingEvent(new Date(baseTime.getTime()), 200, healer, target),
        createHealingEvent(new Date(baseTime.getTime() + 2000), 300, healer, target),
      ];

      // Total healing: 500, Duration: 10 seconds = 50 HPS
      expect(calculator.calculateAverageHPS(events, 10000)).toBe(50);
    });
  });

  describe('calculateDPSTimeline', () => {
    it('should return empty array for no events', () => {
      expect(calculator.calculateDPSTimeline([])).toEqual([]);
    });

    it('should calculate timeline points', () => {
      const player = createEntity('Player');
      const enemy = createEntity('Enemy');
      const baseTime = new Date(2024, 0, 1, 12, 0, 0);

      const events = [
        createDamageEvent(new Date(baseTime.getTime()), 100, player, enemy),
        createDamageEvent(new Date(baseTime.getTime() + 2000), 200, player, enemy),
      ];

      const timeline = calculator.calculateDPSTimeline(events, 1000);
      expect(timeline.length).toBeGreaterThan(0);
      expect(timeline[0]).toHaveProperty('timestamp');
      expect(timeline[0]).toHaveProperty('value');
      expect(timeline[0]).toHaveProperty('cumulativeAmount');
    });
  });

  describe('window size configuration', () => {
    it('should allow setting window size', () => {
      const calc = new DPSCalculator(3000);
      expect(calc.getWindowMs()).toBe(3000);

      calc.setWindowMs(10000);
      expect(calc.getWindowMs()).toBe(10000);
    });
  });
});
