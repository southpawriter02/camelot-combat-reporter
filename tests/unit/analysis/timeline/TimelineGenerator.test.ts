import { TimelineGenerator } from '../../../../src/analysis/timeline/TimelineGenerator';
import { DEFAULT_TIMELINE_CONFIG } from '../../../../src/analysis/timeline/types';
import type { CombatSession } from '../../../../src/analysis/types';
import { EventType, EntityType } from '../../../../src/types';
import type { Entity, DamageEvent, HealingEvent, DeathEvent } from '../../../../src/types';

describe('TimelineGenerator', () => {
  const generator = new TimelineGenerator();

  const createEntity = (name: string, isSelf = false): Entity => ({
    name,
    entityType: EntityType.PLAYER,
    isPlayer: true,
    isSelf,
  });

  const createDamageEvent = (
    id: string,
    timestamp: Date,
    source: Entity,
    target: Entity,
    amount: number
  ): DamageEvent => ({
    id,
    timestamp,
    rawTimestamp: `[${timestamp.toTimeString().slice(0, 8)}]`,
    rawLine: 'test',
    lineNumber: 1,
    eventType: EventType.DAMAGE_DEALT,
    source,
    target,
    amount,
    effectiveAmount: amount,
    absorbedAmount: 0,
    damageType: 'CRUSH' as any,
    actionType: 'MELEE' as any,
    actionName: 'Attack',
    isCritical: false,
    isBlocked: false,
    isParried: false,
    isEvaded: false,
  });

  const createHealingEvent = (
    id: string,
    timestamp: Date,
    source: Entity,
    target: Entity,
    amount: number
  ): HealingEvent => ({
    id,
    timestamp,
    rawTimestamp: `[${timestamp.toTimeString().slice(0, 8)}]`,
    rawLine: 'test',
    lineNumber: 1,
    eventType: EventType.HEALING_DONE,
    source,
    target,
    amount,
    effectiveAmount: amount,
    overheal: 0,
    spellName: 'Heal',
    isCritical: false,
  });

  const createDeathEvent = (
    id: string,
    timestamp: Date,
    target: Entity,
    killer?: Entity
  ): DeathEvent => ({
    id,
    timestamp,
    rawTimestamp: `[${timestamp.toTimeString().slice(0, 8)}]`,
    rawLine: 'test',
    lineNumber: 1,
    eventType: EventType.DEATH,
    target,
    killer,
  });

  const createSession = (events: any[], startTime: Date): CombatSession => {
    const endTime = new Date(startTime.getTime() + 60000); // 1 minute duration
    return {
      id: 'test-session',
      startTime,
      endTime,
      durationMs: 60000,
      events,
      participants: [],
      summary: {
        totalDamageDealt: 0,
        totalDamageTaken: 0,
        totalHealingDone: 0,
        totalHealingReceived: 0,
        deathCount: 0,
        ccEventCount: 0,
        keyEvents: [],
      },
    };
  };

  describe('generate', () => {
    it('should generate timeline view from session', () => {
      const startTime = new Date(2024, 0, 1, 12, 0, 0);
      const player = createEntity('Player', true);
      const enemy = createEntity('Enemy');

      const events = [
        createDamageEvent('1', new Date(startTime.getTime() + 5000), player, enemy, 100),
        createDamageEvent('2', new Date(startTime.getTime() + 10000), player, enemy, 150),
        createDamageEvent('3', new Date(startTime.getTime() + 15000), enemy, player, 80),
      ];

      const session = createSession(events, startTime);
      const timeline = generator.generate(session);

      expect(timeline.sessionId).toBe('test-session');
      expect(timeline.sessionStart).toEqual(startTime);
      expect(timeline.entries).toHaveLength(3);
      expect(timeline.entries[0]!.relativeTimeMs).toBe(5000);
      expect(timeline.entries[1]!.relativeTimeMs).toBe(10000);
      expect(timeline.entries[2]!.relativeTimeMs).toBe(15000);
    });

    it('should sort entries chronologically', () => {
      const startTime = new Date(2024, 0, 1, 12, 0, 0);
      const player = createEntity('Player', true);
      const enemy = createEntity('Enemy');

      // Events in non-chronological order
      const events = [
        createDamageEvent('3', new Date(startTime.getTime() + 15000), player, enemy, 100),
        createDamageEvent('1', new Date(startTime.getTime() + 5000), player, enemy, 100),
        createDamageEvent('2', new Date(startTime.getTime() + 10000), player, enemy, 100),
      ];

      const session = createSession(events, startTime);
      const timeline = generator.generate(session);

      expect(timeline.entries[0]!.relativeTimeMs).toBe(5000);
      expect(timeline.entries[1]!.relativeTimeMs).toBe(10000);
      expect(timeline.entries[2]!.relativeTimeMs).toBe(15000);
    });

    it('should apply filters when provided', () => {
      const startTime = new Date(2024, 0, 1, 12, 0, 0);
      const player = createEntity('Player', true);
      const enemy = createEntity('Enemy');
      const healer = createEntity('Healer');

      const events = [
        createDamageEvent('1', new Date(startTime.getTime() + 5000), player, enemy, 100),
        createHealingEvent('2', new Date(startTime.getTime() + 10000), healer, player, 50),
        createDamageEvent('3', new Date(startTime.getTime() + 15000), player, enemy, 200),
      ];

      const session = createSession(events, startTime);
      const timeline = generator.generate(session, {
        eventTypes: [EventType.DAMAGE_DEALT],
      });

      expect(timeline.entries).toHaveLength(2);
      expect(timeline.entries.every((e) => e.eventType === EventType.DAMAGE_DEALT)).toBe(true);
    });

    it('should calculate correct statistics', () => {
      const startTime = new Date(2024, 0, 1, 12, 0, 0);
      const player = createEntity('Player', true);
      const enemy = createEntity('Enemy');
      const healer = createEntity('Healer');

      const events = [
        createDamageEvent('1', new Date(startTime.getTime() + 5000), player, enemy, 100),
        createDamageEvent('2', new Date(startTime.getTime() + 10000), player, enemy, 150),
        createHealingEvent('3', new Date(startTime.getTime() + 15000), healer, player, 75),
        createDeathEvent('4', new Date(startTime.getTime() + 20000), enemy, player),
      ];

      const session = createSession(events, startTime);
      const timeline = generator.generate(session);

      expect(timeline.stats.totalEntries).toBe(4);
      expect(timeline.stats.totalDamage).toBe(250);
      expect(timeline.stats.totalHealing).toBe(75);
      expect(timeline.stats.deathCount).toBe(1);
    });

    it('should calculate visible time range', () => {
      const startTime = new Date(2024, 0, 1, 12, 0, 0);
      const player = createEntity('Player', true);
      const enemy = createEntity('Enemy');

      const events = [
        createDamageEvent('1', new Date(startTime.getTime() + 10000), player, enemy, 100),
        createDamageEvent('2', new Date(startTime.getTime() + 30000), player, enemy, 100),
      ];

      const session = createSession(events, startTime);
      const timeline = generator.generate(session);

      expect(timeline.visibleTimeRange.startMs).toBe(10000);
      expect(timeline.visibleTimeRange.endMs).toBe(30000);
      expect(timeline.visibleTimeRange.durationMs).toBe(20000);
    });

    it('should handle empty entries', () => {
      const startTime = new Date(2024, 0, 1, 12, 0, 0);
      const session = createSession([], startTime);
      const timeline = generator.generate(session);

      expect(timeline.entries).toHaveLength(0);
      expect(timeline.stats.totalEntries).toBe(0);
      expect(timeline.visibleTimeRange.startMs).toBe(0);
      expect(timeline.visibleTimeRange.endMs).toBe(60000);
    });

    it('should include applied filter in view', () => {
      const startTime = new Date(2024, 0, 1, 12, 0, 0);
      const session = createSession([], startTime);

      const filterConfig = {
        eventTypes: [EventType.DAMAGE_DEALT],
        minValue: 100,
      };

      const timeline = generator.generate(session, filterConfig);

      expect(timeline.appliedFilter.eventTypes).toEqual([EventType.DAMAGE_DEALT]);
      expect(timeline.appliedFilter.minValue).toBe(100);
    });
  });

  describe('maxEntries config', () => {
    it('should limit entries when maxEntries is set', () => {
      const customGenerator = new TimelineGenerator({ maxEntries: 2 });
      const startTime = new Date(2024, 0, 1, 12, 0, 0);
      const player = createEntity('Player', true);
      const enemy = createEntity('Enemy');

      const events = [
        createDamageEvent('1', new Date(startTime.getTime() + 5000), player, enemy, 100),
        createDamageEvent('2', new Date(startTime.getTime() + 10000), player, enemy, 100),
        createDamageEvent('3', new Date(startTime.getTime() + 15000), player, enemy, 100),
        createDamageEvent('4', new Date(startTime.getTime() + 20000), player, enemy, 100),
      ];

      const session = createSession(events, startTime);
      const timeline = customGenerator.generate(session);

      expect(timeline.entries).toHaveLength(2);
      // Should keep first 2 chronologically
      expect(timeline.entries[0]!.relativeTimeMs).toBe(5000);
      expect(timeline.entries[1]!.relativeTimeMs).toBe(10000);
    });

    it('should not limit when maxEntries is 0', () => {
      const customGenerator = new TimelineGenerator({ maxEntries: 0 });
      const startTime = new Date(2024, 0, 1, 12, 0, 0);
      const player = createEntity('Player', true);
      const enemy = createEntity('Enemy');

      const events = [
        createDamageEvent('1', new Date(startTime.getTime() + 5000), player, enemy, 100),
        createDamageEvent('2', new Date(startTime.getTime() + 10000), player, enemy, 100),
        createDamageEvent('3', new Date(startTime.getTime() + 15000), player, enemy, 100),
      ];

      const session = createSession(events, startTime);
      const timeline = customGenerator.generate(session);

      expect(timeline.entries).toHaveLength(3);
    });
  });

  describe('createEntry', () => {
    it('should create a single timeline entry', () => {
      const startTime = new Date(2024, 0, 1, 12, 0, 0);
      const player = createEntity('Player', true);
      const enemy = createEntity('Enemy');

      const event = createDamageEvent(
        'test',
        new Date(startTime.getTime() + 5000),
        player,
        enemy,
        100
      );

      const entry = generator.createEntry(event, startTime);

      expect(entry.event).toBe(event);
      expect(entry.relativeTimeMs).toBe(5000);
      expect(entry.eventType).toBe(EventType.DAMAGE_DEALT);
      expect(entry.source?.name).toBe('Player');
      expect(entry.target?.name).toBe('Enemy');
    });
  });

  describe('getConfig', () => {
    it('should return a copy of configuration', () => {
      const config = generator.getConfig();

      expect(config.maxEntries).toBe(DEFAULT_TIMELINE_CONFIG.maxEntries);
      expect(config).not.toBe(generator.getConfig()); // Should be a copy
    });

    it('should reflect custom configuration', () => {
      const customGenerator = new TimelineGenerator({
        maxEntries: 500,
        defaultFilter: { minValue: 50 },
      });

      const config = customGenerator.getConfig();

      expect(config.maxEntries).toBe(500);
      expect(config.defaultFilter.minValue).toBe(50);
    });
  });

  describe('statistics by category', () => {
    it('should count entries by event type', () => {
      const startTime = new Date(2024, 0, 1, 12, 0, 0);
      const player = createEntity('Player', true);
      const enemy = createEntity('Enemy');
      const healer = createEntity('Healer');

      const events = [
        createDamageEvent('1', new Date(startTime.getTime() + 5000), player, enemy, 100),
        createDamageEvent('2', new Date(startTime.getTime() + 10000), player, enemy, 100),
        createHealingEvent('3', new Date(startTime.getTime() + 15000), healer, player, 50),
      ];

      const session = createSession(events, startTime);
      const timeline = generator.generate(session);

      expect(timeline.stats.entriesByType[EventType.DAMAGE_DEALT]).toBe(2);
      expect(timeline.stats.entriesByType[EventType.HEALING_DONE]).toBe(1);
    });

    it('should count entries by marker category', () => {
      const startTime = new Date(2024, 0, 1, 12, 0, 0);
      const player = createEntity('Player', true);
      const enemy = createEntity('Enemy');

      const events = [
        createDamageEvent('1', new Date(startTime.getTime() + 5000), player, enemy, 100),
        createDeathEvent('2', new Date(startTime.getTime() + 10000), enemy, player),
      ];

      const session = createSession(events, startTime);
      const timeline = generator.generate(session);

      expect(timeline.stats.entriesByCategory['DAMAGE_OUTGOING']).toBe(1);
      expect(timeline.stats.entriesByCategory['DEATH']).toBe(1);
    });
  });
});
