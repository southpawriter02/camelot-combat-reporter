import { TimelineFilter } from '../../../../src/analysis/timeline/TimelineFilter';
import { DEFAULT_TIMELINE_FILTER } from '../../../../src/analysis/timeline/types';
import type { TimelineEntry, TimelineMarkerCategory } from '../../../../src/analysis/timeline/types';
import { EventType, EntityType } from '../../../../src/types';
import type { Entity, DamageEvent } from '../../../../src/types';

describe('TimelineFilter', () => {
  const filter = new TimelineFilter();

  const createEntity = (name: string): Entity => ({
    name,
    entityType: EntityType.PLAYER,
    isPlayer: true,
    isSelf: false,
  });

  const createTimelineEntry = (
    overrides: Partial<TimelineEntry> = {}
  ): TimelineEntry => {
    const baseEvent: DamageEvent = {
      id: 'test-event',
      timestamp: new Date(2024, 0, 1, 12, 0, 0),
      rawTimestamp: '[12:00:00]',
      rawLine: 'test',
      lineNumber: 1,
      eventType: EventType.DAMAGE_DEALT,
      source: createEntity('Source'),
      target: createEntity('Target'),
      amount: 100,
      effectiveAmount: 100,
      absorbedAmount: 0,
      damageType: 'CRUSH' as any,
      actionType: 'MELEE' as any,
      actionName: 'Attack',
      isCritical: false,
      isBlocked: false,
      isParried: false,
      isEvaded: false,
    };

    return {
      id: 'test-entry',
      event: baseEvent,
      timestamp: new Date(2024, 0, 1, 12, 0, 0),
      formattedTimestamp: '12:00:00',
      relativeTimeMs: 0,
      formattedRelativeTime: '+0:00',
      eventType: EventType.DAMAGE_DEALT,
      markerCategory: 'DAMAGE_OUTGOING' as TimelineMarkerCategory,
      description: 'Source hit Target for 100',
      source: createEntity('Source'),
      target: createEntity('Target'),
      primaryValue: 100,
      primaryValueUnit: 'damage',
      details: {},
      ...overrides,
    };
  };

  beforeEach(() => {
    filter.reset();
  });

  describe('filter with empty config', () => {
    it('should return all entries with default config', () => {
      const entries = [
        createTimelineEntry({ id: '1' }),
        createTimelineEntry({ id: '2' }),
        createTimelineEntry({ id: '3' }),
      ];

      const result = filter.filter(entries);

      expect(result).toHaveLength(3);
    });
  });

  describe('event type filter', () => {
    it('should filter by single event type', () => {
      const entries = [
        createTimelineEntry({ id: '1', eventType: EventType.DAMAGE_DEALT }),
        createTimelineEntry({ id: '2', eventType: EventType.HEALING_DONE }),
        createTimelineEntry({ id: '3', eventType: EventType.DAMAGE_DEALT }),
      ];

      filter.setConfig({ eventTypes: [EventType.DAMAGE_DEALT] });
      const result = filter.filter(entries);

      expect(result).toHaveLength(2);
      expect(result.every((e) => e.eventType === EventType.DAMAGE_DEALT)).toBe(true);
    });

    it('should filter by multiple event types', () => {
      const entries = [
        createTimelineEntry({ id: '1', eventType: EventType.DAMAGE_DEALT }),
        createTimelineEntry({ id: '2', eventType: EventType.HEALING_DONE }),
        createTimelineEntry({ id: '3', eventType: EventType.DEATH }),
      ];

      filter.setConfig({ eventTypes: [EventType.DAMAGE_DEALT, EventType.HEALING_DONE] });
      const result = filter.filter(entries);

      expect(result).toHaveLength(2);
    });

    it('should return all when eventTypes is empty', () => {
      const entries = [
        createTimelineEntry({ id: '1', eventType: EventType.DAMAGE_DEALT }),
        createTimelineEntry({ id: '2', eventType: EventType.HEALING_DONE }),
      ];

      filter.setConfig({ eventTypes: [] });
      const result = filter.filter(entries);

      expect(result).toHaveLength(2);
    });
  });

  describe('marker category filter', () => {
    it('should filter by single marker category', () => {
      const entries = [
        createTimelineEntry({ id: '1', markerCategory: 'DAMAGE_OUTGOING' }),
        createTimelineEntry({ id: '2', markerCategory: 'HEALING_OUTGOING' }),
        createTimelineEntry({ id: '3', markerCategory: 'DEATH' }),
      ];

      filter.setConfig({ markerCategories: ['DEATH'] });
      const result = filter.filter(entries);

      expect(result).toHaveLength(1);
      expect(result[0]!.markerCategory).toBe('DEATH');
    });

    it('should filter by multiple marker categories', () => {
      const entries = [
        createTimelineEntry({ id: '1', markerCategory: 'DAMAGE_OUTGOING' }),
        createTimelineEntry({ id: '2', markerCategory: 'HEALING_OUTGOING' }),
        createTimelineEntry({ id: '3', markerCategory: 'DEATH' }),
        createTimelineEntry({ id: '4', markerCategory: 'CROWD_CONTROL' }),
      ];

      filter.setConfig({ markerCategories: ['DEATH', 'CROWD_CONTROL'] });
      const result = filter.filter(entries);

      expect(result).toHaveLength(2);
    });
  });

  describe('entity name filter', () => {
    it('should filter by entity as source', () => {
      const entries = [
        createTimelineEntry({ id: '1', source: createEntity('Player1'), target: createEntity('Enemy') }),
        createTimelineEntry({ id: '2', source: createEntity('Player2'), target: createEntity('Enemy') }),
        createTimelineEntry({ id: '3', source: createEntity('Player1'), target: createEntity('Boss') }),
      ];

      filter.setConfig({ entityName: 'Player1', includeAsSource: true, includeAsTarget: false });
      const result = filter.filter(entries);

      expect(result).toHaveLength(2);
    });

    it('should filter by entity as target', () => {
      const entries = [
        createTimelineEntry({ id: '1', source: createEntity('Enemy'), target: createEntity('Player1') }),
        createTimelineEntry({ id: '2', source: createEntity('Enemy'), target: createEntity('Player2') }),
        createTimelineEntry({ id: '3', source: createEntity('Boss'), target: createEntity('Player1') }),
      ];

      filter.setConfig({ entityName: 'Player1', includeAsSource: false, includeAsTarget: true });
      const result = filter.filter(entries);

      expect(result).toHaveLength(2);
    });

    it('should filter by entity as source or target', () => {
      const entries = [
        createTimelineEntry({ id: '1', source: createEntity('Player1'), target: createEntity('Enemy') }),
        createTimelineEntry({ id: '2', source: createEntity('Enemy'), target: createEntity('Player1') }),
        createTimelineEntry({ id: '3', source: createEntity('Player2'), target: createEntity('Enemy') }),
      ];

      filter.setConfig({ entityName: 'Player1', includeAsSource: true, includeAsTarget: true });
      const result = filter.filter(entries);

      expect(result).toHaveLength(2);
    });
  });

  describe('time range filter', () => {
    it('should filter by start time', () => {
      const entries = [
        createTimelineEntry({ id: '1', relativeTimeMs: 5000 }),
        createTimelineEntry({ id: '2', relativeTimeMs: 15000 }),
        createTimelineEntry({ id: '3', relativeTimeMs: 25000 }),
      ];

      filter.setConfig({ startTimeMs: 10000 });
      const result = filter.filter(entries);

      expect(result).toHaveLength(2);
      expect(result.every((e) => e.relativeTimeMs >= 10000)).toBe(true);
    });

    it('should filter by end time', () => {
      const entries = [
        createTimelineEntry({ id: '1', relativeTimeMs: 5000 }),
        createTimelineEntry({ id: '2', relativeTimeMs: 15000 }),
        createTimelineEntry({ id: '3', relativeTimeMs: 25000 }),
      ];

      filter.setConfig({ endTimeMs: 20000 });
      const result = filter.filter(entries);

      expect(result).toHaveLength(2);
      expect(result.every((e) => e.relativeTimeMs <= 20000)).toBe(true);
    });

    it('should filter by time range', () => {
      const entries = [
        createTimelineEntry({ id: '1', relativeTimeMs: 5000 }),
        createTimelineEntry({ id: '2', relativeTimeMs: 15000 }),
        createTimelineEntry({ id: '3', relativeTimeMs: 25000 }),
        createTimelineEntry({ id: '4', relativeTimeMs: 35000 }),
      ];

      filter.setConfig({ startTimeMs: 10000, endTimeMs: 30000 });
      const result = filter.filter(entries);

      expect(result).toHaveLength(2);
    });
  });

  describe('minimum value filter', () => {
    it('should filter by minimum value', () => {
      const entries = [
        createTimelineEntry({ id: '1', primaryValue: 50 }),
        createTimelineEntry({ id: '2', primaryValue: 150 }),
        createTimelineEntry({ id: '3', primaryValue: 250 }),
      ];

      filter.setConfig({ minValue: 100 });
      const result = filter.filter(entries);

      expect(result).toHaveLength(2);
      expect(result.every((e) => (e.primaryValue ?? 0) >= 100)).toBe(true);
    });

    it('should exclude entries without primary value when minValue is set', () => {
      const entries = [
        createTimelineEntry({ id: '1', primaryValue: 150 }),
        createTimelineEntry({ id: '2', primaryValue: undefined }),
        createTimelineEntry({ id: '3', primaryValue: 250 }),
      ];

      filter.setConfig({ minValue: 100 });
      const result = filter.filter(entries);

      expect(result).toHaveLength(2);
    });
  });

  describe('critical only filter', () => {
    it('should filter only critical hits', () => {
      const entries = [
        createTimelineEntry({ id: '1', details: { isCritical: true } }),
        createTimelineEntry({ id: '2', details: { isCritical: false } }),
        createTimelineEntry({ id: '3', details: { isCritical: true } }),
      ];

      filter.setConfig({ criticalOnly: true });
      const result = filter.filter(entries);

      expect(result).toHaveLength(2);
      expect(result.every((e) => e.details.isCritical)).toBe(true);
    });

    it('should return all when criticalOnly is false', () => {
      const entries = [
        createTimelineEntry({ id: '1', details: { isCritical: true } }),
        createTimelineEntry({ id: '2', details: { isCritical: false } }),
      ];

      filter.setConfig({ criticalOnly: false });
      const result = filter.filter(entries);

      expect(result).toHaveLength(2);
    });
  });

  describe('combined filters', () => {
    it('should apply multiple filters with AND logic', () => {
      const entries = [
        createTimelineEntry({
          id: '1',
          eventType: EventType.DAMAGE_DEALT,
          primaryValue: 150,
          relativeTimeMs: 5000,
        }),
        createTimelineEntry({
          id: '2',
          eventType: EventType.DAMAGE_DEALT,
          primaryValue: 50,
          relativeTimeMs: 15000,
        }),
        createTimelineEntry({
          id: '3',
          eventType: EventType.HEALING_DONE,
          primaryValue: 200,
          relativeTimeMs: 25000,
        }),
        createTimelineEntry({
          id: '4',
          eventType: EventType.DAMAGE_DEALT,
          primaryValue: 300,
          relativeTimeMs: 35000,
        }),
      ];

      filter.setConfig({
        eventTypes: [EventType.DAMAGE_DEALT],
        minValue: 100,
        startTimeMs: 10000,
      });
      const result = filter.filter(entries);

      expect(result).toHaveLength(1);
      expect(result[0]!.id).toBe('4');
    });
  });

  describe('config management', () => {
    it('should return config copy', () => {
      filter.setConfig({ minValue: 100 });
      const config = filter.getConfig();

      expect(config.minValue).toBe(100);
      expect(config).not.toBe(filter.getConfig()); // Should be a copy
    });

    it('should reset to default config', () => {
      filter.setConfig({ minValue: 100, criticalOnly: true });
      filter.reset();
      const config = filter.getConfig();

      expect(config.minValue).toBeUndefined();
      expect(config.criticalOnly).toBe(false);
    });
  });
});
