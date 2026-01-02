import { CombatAnalyzer } from '../../../src/analysis/CombatAnalyzer';
import {
  EventType,
  DamageType,
  ActionType,
  EntityType,
} from '../../../src/types';
import type {
  DamageEvent,
  HealingEvent,
  Entity,
  CombatEvent,
  ParsedLog,
} from '../../../src/types';

describe('CombatAnalyzer', () => {
  const analyzer = new CombatAnalyzer({
    session: {
      inactivityTimeoutMs: 5000,
      minEventsForSession: 2,
    },
  });

  const createEntity = (name: string, isSelf = false): Entity => ({
    name,
    entityType: isSelf ? EntityType.SELF : EntityType.PLAYER,
    isPlayer: true,
    isSelf,
  });

  const createDamageEvent = (
    timestamp: Date,
    source: Entity,
    target: Entity,
    amount: number,
    eventType: EventType.DAMAGE_DEALT | EventType.DAMAGE_RECEIVED = EventType.DAMAGE_DEALT
  ): DamageEvent => ({
    id: `dmg-${Date.now()}-${Math.random()}`,
    timestamp,
    rawTimestamp: '[12:00:00]',
    rawLine: 'test',
    lineNumber: 1,
    eventType,
    source,
    target,
    amount,
    effectiveAmount: amount,
    absorbedAmount: 0,
    damageType: DamageType.CRUSH,
    actionType: ActionType.MELEE,
    actionName: 'Attack',
    isCritical: false,
    isBlocked: false,
    isParried: false,
    isEvaded: false,
  });

  const createHealingEvent = (
    timestamp: Date,
    source: Entity,
    target: Entity,
    amount: number
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

  describe('analyzeEvents', () => {
    it('should return empty result for no events', () => {
      const result = analyzer.analyzeEvents([]);
      expect(result.sessions).toHaveLength(0);
      expect(result.totalEvents).toBe(0);
    });

    it('should detect sessions from events', () => {
      const player = createEntity('Player', true);
      const enemy = createEntity('Enemy');
      const baseTime = new Date(2024, 0, 1, 12, 0, 0);

      const events: CombatEvent[] = [
        createDamageEvent(new Date(baseTime.getTime()), player, enemy, 100),
        createDamageEvent(new Date(baseTime.getTime() + 1000), player, enemy, 150),
        createDamageEvent(new Date(baseTime.getTime() + 2000), player, enemy, 200),
      ];

      const result = analyzer.analyzeEvents(events);

      expect(result.sessions).toHaveLength(1);
      expect(result.totalEvents).toBe(3);
      expect(result.sessionEvents).toBe(3);
      expect(result.downtimeEvents).toBe(0);
    });

    it('should calculate correct time span', () => {
      const player = createEntity('Player', true);
      const enemy = createEntity('Enemy');
      const baseTime = new Date(2024, 0, 1, 12, 0, 0);

      const events: CombatEvent[] = [
        createDamageEvent(new Date(baseTime.getTime()), player, enemy, 100),
        createDamageEvent(new Date(baseTime.getTime() + 5000), player, enemy, 150),
      ];

      const result = analyzer.analyzeEvents(events);

      expect(result.timeSpan.start.getTime()).toBe(baseTime.getTime());
      expect(result.timeSpan.end.getTime()).toBe(baseTime.getTime() + 5000);
      expect(result.timeSpan.durationMs).toBe(5000);
    });
  });

  describe('analyze', () => {
    it('should analyze a parsed log', () => {
      const player = createEntity('Player', true);
      const enemy = createEntity('Enemy');
      const baseTime = new Date(2024, 0, 1, 12, 0, 0);

      const events: CombatEvent[] = [
        createDamageEvent(new Date(baseTime.getTime()), player, enemy, 100),
        createDamageEvent(new Date(baseTime.getTime() + 1000), player, enemy, 150),
      ];

      const parsedLog: ParsedLog = {
        filename: 'test.log',
        filePath: '/test/test.log',
        parseStartTime: new Date(),
        parseEndTime: new Date(),
        totalLines: 2,
        parsedLines: 2,
        errorLines: 0,
        events,
        errors: [],
        metadata: {
          uniqueEntities: ['Player', 'Enemy'],
          eventTypeCounts: { DAMAGE_DEALT: 2 },
        },
      };

      const result = analyzer.analyze(parsedLog);

      expect(result.sessions).toHaveLength(1);
      expect(result.totalEvents).toBe(2);
    });
  });

  describe('getSummary', () => {
    it('should return a fight summary for a session', () => {
      const player = createEntity('Player', true);
      const enemy = createEntity('Enemy');
      const baseTime = new Date(2024, 0, 1, 12, 0, 0);

      const events: CombatEvent[] = [
        createDamageEvent(new Date(baseTime.getTime()), player, enemy, 100),
        createDamageEvent(new Date(baseTime.getTime() + 1000), player, enemy, 150),
      ];

      const result = analyzer.analyzeEvents(events);
      const summary = analyzer.getSummary(result.sessions[0]!);

      expect(summary).toHaveProperty('session');
      expect(summary).toHaveProperty('durationFormatted');
      expect(summary).toHaveProperty('damageMeter');
      expect(summary).toHaveProperty('healingMeter');
    });
  });

  describe('getEntityMetrics', () => {
    it('should return metrics for a specific entity', () => {
      const player = createEntity('Player', true);
      const enemy = createEntity('Enemy');
      const baseTime = new Date(2024, 0, 1, 12, 0, 0);

      const events: CombatEvent[] = [
        createDamageEvent(new Date(baseTime.getTime()), player, enemy, 100),
        createDamageEvent(new Date(baseTime.getTime() + 1000), player, enemy, 150),
      ];

      const result = analyzer.analyzeEvents(events);
      const metrics = analyzer.getEntityMetrics(result.sessions[0]!, 'Player');

      expect(metrics).not.toBeNull();
      expect(metrics!.entity.name).toBe('Player');
      expect(metrics!.damage.totalDealt).toBe(250);
    });

    it('should return null for non-existent entity', () => {
      const player = createEntity('Player', true);
      const enemy = createEntity('Enemy');
      const baseTime = new Date(2024, 0, 1, 12, 0, 0);

      const events: CombatEvent[] = [
        createDamageEvent(new Date(baseTime.getTime()), player, enemy, 100),
        createDamageEvent(new Date(baseTime.getTime() + 1000), player, enemy, 150),
      ];

      const result = analyzer.analyzeEvents(events);
      const metrics = analyzer.getEntityMetrics(
        result.sessions[0]!,
        'NonExistent'
      );

      expect(metrics).toBeNull();
    });
  });

  describe('getEntityDamageMetrics', () => {
    it('should return damage metrics for a specific entity', () => {
      const player = createEntity('Player', true);
      const enemy = createEntity('Enemy');
      const baseTime = new Date(2024, 0, 1, 12, 0, 0);

      const events: CombatEvent[] = [
        createDamageEvent(new Date(baseTime.getTime()), player, enemy, 100),
        createDamageEvent(new Date(baseTime.getTime() + 1000), player, enemy, 150),
      ];

      const result = analyzer.analyzeEvents(events);
      const damageMetrics = analyzer.getEntityDamageMetrics(
        result.sessions[0]!,
        'Player'
      );

      expect(damageMetrics).not.toBeNull();
      expect(damageMetrics!.totalDealt).toBe(250);
      expect(damageMetrics!.dps).toBeGreaterThan(0);
    });
  });

  describe('getEntityHealingMetrics', () => {
    it('should return healing metrics for a specific entity', () => {
      const player = createEntity('Player', true);
      const healer = createEntity('Healer');
      const enemy = createEntity('Enemy');
      const baseTime = new Date(2024, 0, 1, 12, 0, 0);

      const events: CombatEvent[] = [
        createDamageEvent(new Date(baseTime.getTime()), player, enemy, 100),
        createHealingEvent(
          new Date(baseTime.getTime() + 1000),
          healer,
          player,
          200
        ),
        createDamageEvent(new Date(baseTime.getTime() + 2000), player, enemy, 100),
      ];

      const result = analyzer.analyzeEvents(events);
      const healingMetrics = analyzer.getEntityHealingMetrics(
        result.sessions[0]!,
        'Healer'
      );

      expect(healingMetrics).not.toBeNull();
      expect(healingMetrics!.totalDone).toBe(200);
    });
  });

  describe('defineFight', () => {
    it('should create a custom fight from events', () => {
      const player = createEntity('Player', true);
      const enemy = createEntity('Enemy');
      const baseTime = new Date(2024, 0, 1, 12, 0, 0);

      const events: CombatEvent[] = [
        createDamageEvent(new Date(baseTime.getTime()), player, enemy, 100),
        createDamageEvent(new Date(baseTime.getTime() + 1000), player, enemy, 150),
        createDamageEvent(new Date(baseTime.getTime() + 2000), player, enemy, 200),
      ];

      const session = analyzer.defineFight(events);

      expect(session).not.toBeNull();
      expect(session!.events).toHaveLength(3);
    });

    it('should filter events by time range', () => {
      const player = createEntity('Player', true);
      const enemy = createEntity('Enemy');
      const baseTime = new Date(2024, 0, 1, 12, 0, 0);

      const events: CombatEvent[] = [
        createDamageEvent(new Date(baseTime.getTime()), player, enemy, 100),
        createDamageEvent(new Date(baseTime.getTime() + 1000), player, enemy, 150),
        createDamageEvent(new Date(baseTime.getTime() + 2000), player, enemy, 200),
        createDamageEvent(new Date(baseTime.getTime() + 3000), player, enemy, 250),
      ];

      // Filter to get only 2 events (minEventsForSession is 2)
      const session = analyzer.defineFight(
        events,
        new Date(baseTime.getTime() + 500),
        new Date(baseTime.getTime() + 2500)
      );

      expect(session).not.toBeNull();
      expect(session!.events).toHaveLength(2);
    });

    it('should return null for empty events', () => {
      const session = analyzer.defineFight([]);
      expect(session).toBeNull();
    });
  });

  describe('getDamageReport', () => {
    it('should generate a text damage report', () => {
      const player = createEntity('Player', true);
      const enemy = createEntity('Enemy');
      const baseTime = new Date(2024, 0, 1, 12, 0, 0);

      const events: CombatEvent[] = [
        createDamageEvent(new Date(baseTime.getTime()), player, enemy, 100),
        createDamageEvent(new Date(baseTime.getTime() + 1000), player, enemy, 150),
      ];

      const result = analyzer.analyzeEvents(events);
      const report = analyzer.getDamageReport(result.sessions[0]!);

      expect(report).toContain('Fight Summary');
      expect(report).toContain('Damage Done');
      expect(report).toContain('Player');
    });
  });

  describe('getConfig', () => {
    it('should return the current configuration', () => {
      const config = analyzer.getConfig();

      expect(config).toHaveProperty('session');
      expect(config).toHaveProperty('metrics');
      expect(config.session.inactivityTimeoutMs).toBe(5000);
    });
  });
});
