import { PlayerStatisticsCalculator } from '../../../../src/analysis/player-stats/PlayerStatisticsCalculator';
import { SessionDetector } from '../../../../src/analysis/session/SessionDetector';
import {
  EventType,
  DamageType,
  ActionType,
  EntityType,
} from '../../../../src/types';
import type {
  DamageEvent,
  HealingEvent,
  DeathEvent,
  Entity,
  CombatEvent,
} from '../../../../src/types';
import type { CombatSession } from '../../../../src/analysis/types/index';

describe('PlayerStatisticsCalculator', () => {
  const calculator = new PlayerStatisticsCalculator();
  const sessionDetector = new SessionDetector({
    inactivityTimeoutMs: 30000,
    minEventsForSession: 2,
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

  const createDeathEvent = (
    timestamp: Date,
    target: Entity,
    killer?: Entity
  ): DeathEvent => ({
    id: `death-${Date.now()}-${Math.random()}`,
    timestamp,
    rawTimestamp: '[12:00:00]',
    rawLine: 'test',
    lineNumber: 1,
    eventType: EventType.DEATH,
    target,
    killer,
  });

  const createSession = (events: CombatEvent[]): CombatSession => {
    const sessions = sessionDetector.detect(events);
    return sessions[0]!;
  };

  describe('calculateForSession', () => {
    it('should return null for non-existent player', () => {
      const player = createEntity('Player1', true);
      const enemy = createEntity('Enemy');
      const baseTime = new Date(2024, 0, 1, 12, 0, 0);

      const events: CombatEvent[] = [
        createDamageEvent(new Date(baseTime.getTime()), player, enemy, 100),
        createDamageEvent(new Date(baseTime.getTime() + 1000), player, enemy, 150),
      ];

      const session = createSession(events);
      const stats = calculator.calculateForSession(session, 'NonExistent');

      expect(stats).toBeNull();
    });

    it('should calculate basic stats for a player', () => {
      const player = createEntity('Player1', true);
      const enemy = createEntity('Enemy');
      const baseTime = new Date(2024, 0, 1, 12, 0, 0);

      const events: CombatEvent[] = [
        createDamageEvent(new Date(baseTime.getTime()), player, enemy, 100),
        createDamageEvent(new Date(baseTime.getTime() + 1000), player, enemy, 150),
        createDamageEvent(new Date(baseTime.getTime() + 2000), player, enemy, 200),
      ];

      const session = createSession(events);
      const stats = calculator.calculateForSession(session, 'Player1');

      expect(stats).not.toBeNull();
      expect(stats!.playerName).toBe('Player1');
      expect(stats!.damageDealt).toBe(450);
      expect(stats!.dps).toBeGreaterThan(0);
    });

    it('should count kills correctly', () => {
      const player = createEntity('Player1', true);
      const enemy1 = createEntity('Enemy1');
      const enemy2 = createEntity('Enemy2');
      const baseTime = new Date(2024, 0, 1, 12, 0, 0);

      const events: CombatEvent[] = [
        createDamageEvent(new Date(baseTime.getTime()), player, enemy1, 100),
        createDeathEvent(new Date(baseTime.getTime() + 1000), enemy1, player),
        createDamageEvent(new Date(baseTime.getTime() + 2000), player, enemy2, 100),
        createDeathEvent(new Date(baseTime.getTime() + 3000), enemy2, player),
      ];

      const session = createSession(events);
      const stats = calculator.calculateForSession(session, 'Player1');

      expect(stats).not.toBeNull();
      expect(stats!.kills).toBe(2);
    });

    it('should count deaths correctly', () => {
      const player = createEntity('Player1', true);
      const enemy = createEntity('Enemy');
      const baseTime = new Date(2024, 0, 1, 12, 0, 0);

      const events: CombatEvent[] = [
        createDamageEvent(new Date(baseTime.getTime()), player, enemy, 100),
        createDamageEvent(new Date(baseTime.getTime() + 1000), enemy, player, 500, EventType.DAMAGE_RECEIVED),
        createDeathEvent(new Date(baseTime.getTime() + 2000), player, enemy),
      ];

      const session = createSession(events);
      const stats = calculator.calculateForSession(session, 'Player1');

      expect(stats).not.toBeNull();
      expect(stats!.deaths).toBe(1);
    });

    it('should count assists correctly', () => {
      const player1 = createEntity('Player1', true);
      const player2 = createEntity('Player2');
      const enemy = createEntity('Enemy');
      const baseTime = new Date(2024, 0, 1, 12, 0, 0);

      const events: CombatEvent[] = [
        // Player1 damages enemy
        createDamageEvent(new Date(baseTime.getTime()), player1, enemy, 100),
        // Player2 damages enemy
        createDamageEvent(new Date(baseTime.getTime() + 1000), player2, enemy, 100),
        // Player2 gets the kill
        createDeathEvent(new Date(baseTime.getTime() + 2000), enemy, player2),
      ];

      const session = createSession(events);
      const player1Stats = calculator.calculateForSession(session, 'Player1');

      expect(player1Stats).not.toBeNull();
      expect(player1Stats!.kills).toBe(0);
      expect(player1Stats!.assists).toBe(1);
    });

    it('should calculate KDR correctly', () => {
      const player = createEntity('Player1', true);
      const enemy1 = createEntity('Enemy1');
      const enemy2 = createEntity('Enemy2');
      const baseTime = new Date(2024, 0, 1, 12, 0, 0);

      const events: CombatEvent[] = [
        createDamageEvent(new Date(baseTime.getTime()), player, enemy1, 100),
        createDeathEvent(new Date(baseTime.getTime() + 1000), enemy1, player),
        createDamageEvent(new Date(baseTime.getTime() + 2000), player, enemy2, 100),
        createDeathEvent(new Date(baseTime.getTime() + 3000), enemy2, player),
        createDamageEvent(new Date(baseTime.getTime() + 4000), enemy2, player, 500, EventType.DAMAGE_RECEIVED),
        createDeathEvent(new Date(baseTime.getTime() + 5000), player, enemy2),
      ];

      const session = createSession(events);
      const stats = calculator.calculateForSession(session, 'Player1');

      expect(stats).not.toBeNull();
      expect(stats!.kills).toBe(2);
      expect(stats!.deaths).toBe(1);
      expect(stats!.kdr).toBe(2);
    });

    it('should handle zero deaths in KDR calculation', () => {
      const player = createEntity('Player1', true);
      const enemy = createEntity('Enemy');
      const baseTime = new Date(2024, 0, 1, 12, 0, 0);

      const events: CombatEvent[] = [
        createDamageEvent(new Date(baseTime.getTime()), player, enemy, 100),
        createDeathEvent(new Date(baseTime.getTime() + 1000), enemy, player),
      ];

      const session = createSession(events);
      const stats = calculator.calculateForSession(session, 'Player1');

      expect(stats).not.toBeNull();
      expect(stats!.kills).toBe(1);
      expect(stats!.deaths).toBe(0);
      expect(stats!.kdr).toBe(1); // kills / max(deaths, 1)
    });

    it('should calculate healing stats for healer', () => {
      const player = createEntity('Player1', true);
      const healer = createEntity('Healer');
      const enemy = createEntity('Enemy');
      const baseTime = new Date(2024, 0, 1, 12, 0, 0);

      const events: CombatEvent[] = [
        createDamageEvent(new Date(baseTime.getTime()), player, enemy, 100),
        createHealingEvent(new Date(baseTime.getTime() + 1000), healer, player, 200),
        createHealingEvent(new Date(baseTime.getTime() + 2000), healer, player, 150),
      ];

      const session = createSession(events);
      const healerStats = calculator.calculateForSession(session, 'Healer');

      expect(healerStats).not.toBeNull();
      expect(healerStats!.healingDone).toBe(350);
      expect(healerStats!.hps).toBeGreaterThan(0);
    });

    it('should assign performance rating', () => {
      const player = createEntity('Player1', true);
      const enemy = createEntity('Enemy');
      const baseTime = new Date(2024, 0, 1, 12, 0, 0);

      const events: CombatEvent[] = [
        createDamageEvent(new Date(baseTime.getTime()), player, enemy, 100),
        createDamageEvent(new Date(baseTime.getTime() + 1000), player, enemy, 150),
      ];

      const session = createSession(events);
      const stats = calculator.calculateForSession(session, 'Player1');

      expect(stats).not.toBeNull();
      expect(stats!.performanceScore).toBeGreaterThanOrEqual(0);
      expect(stats!.performanceScore).toBeLessThanOrEqual(100);
      expect(['EXCELLENT', 'GOOD', 'AVERAGE', 'BELOW_AVERAGE', 'POOR']).toContain(
        stats!.performanceRating
      );
    });
  });

  describe('calculateAggregate', () => {
    it('should return null for player not found in any session', () => {
      const player = createEntity('Player1', true);
      const enemy = createEntity('Enemy');
      const baseTime = new Date(2024, 0, 1, 12, 0, 0);

      const events: CombatEvent[] = [
        createDamageEvent(new Date(baseTime.getTime()), player, enemy, 100),
        createDamageEvent(new Date(baseTime.getTime() + 1000), player, enemy, 150),
      ];

      const sessions = [createSession(events)];
      const stats = calculator.calculateAggregate(sessions, 'NonExistent');

      expect(stats).toBeNull();
    });

    it('should aggregate stats across multiple sessions', () => {
      const player = createEntity('Player1', true);
      const enemy = createEntity('Enemy');
      const baseTime1 = new Date(2024, 0, 1, 12, 0, 0);
      const baseTime2 = new Date(2024, 0, 1, 14, 0, 0);

      const session1Events: CombatEvent[] = [
        createDamageEvent(new Date(baseTime1.getTime()), player, enemy, 100),
        createDeathEvent(new Date(baseTime1.getTime() + 1000), enemy, player),
      ];

      const session2Events: CombatEvent[] = [
        createDamageEvent(new Date(baseTime2.getTime()), player, enemy, 200),
        createDeathEvent(new Date(baseTime2.getTime() + 1000), enemy, player),
        createDeathEvent(new Date(baseTime2.getTime() + 2000), player, enemy),
      ];

      const sessions = [createSession(session1Events), createSession(session2Events)];
      const aggregate = calculator.calculateAggregate(sessions, 'Player1');

      expect(aggregate).not.toBeNull();
      expect(aggregate!.totalSessions).toBe(2);
      expect(aggregate!.totalKills).toBe(2);
      expect(aggregate!.totalDeaths).toBe(1);
      expect(aggregate!.totalDamageDealt).toBe(300);
    });

    it('should identify best and worst fights', () => {
      const player = createEntity('Player1', true);
      const enemy = createEntity('Enemy');
      const baseTime1 = new Date(2024, 0, 1, 12, 0, 0);
      const baseTime2 = new Date(2024, 0, 1, 14, 0, 0);
      const baseTime3 = new Date(2024, 0, 1, 16, 0, 0);

      // Good session - high damage
      const session1Events: CombatEvent[] = [
        createDamageEvent(new Date(baseTime1.getTime()), player, enemy, 500),
        createDeathEvent(new Date(baseTime1.getTime() + 1000), enemy, player),
      ];

      // Mediocre session
      const session2Events: CombatEvent[] = [
        createDamageEvent(new Date(baseTime2.getTime()), player, enemy, 100),
        createDamageEvent(new Date(baseTime2.getTime() + 1000), player, enemy, 100),
      ];

      // Bad session - player dies
      const session3Events: CombatEvent[] = [
        createDamageEvent(new Date(baseTime3.getTime()), player, enemy, 50),
        createDeathEvent(new Date(baseTime3.getTime() + 1000), player, enemy),
      ];

      const sessions = [
        createSession(session1Events),
        createSession(session2Events),
        createSession(session3Events),
      ];
      const aggregate = calculator.calculateAggregate(sessions, 'Player1');

      expect(aggregate).not.toBeNull();
      expect(aggregate!.bestFight).toBeDefined();
      expect(aggregate!.worstFight).toBeDefined();
      expect(aggregate!.bestFight.performanceScore).toBeGreaterThanOrEqual(
        aggregate!.worstFight.performanceScore
      );
    });

    it('should calculate performance distribution', () => {
      const player = createEntity('Player1', true);
      const enemy = createEntity('Enemy');
      const baseTime = new Date(2024, 0, 1, 12, 0, 0);

      const sessions = [];
      for (let i = 0; i < 5; i++) {
        const events: CombatEvent[] = [
          createDamageEvent(new Date(baseTime.getTime() + i * 3600000), player, enemy, 100 + i * 50),
          createDamageEvent(new Date(baseTime.getTime() + i * 3600000 + 1000), player, enemy, 100),
        ];
        sessions.push(createSession(events));
      }

      const aggregate = calculator.calculateAggregate(sessions, 'Player1');

      expect(aggregate).not.toBeNull();
      expect(aggregate!.performanceDistribution).toBeDefined();

      const totalDistribution = Object.values(aggregate!.performanceDistribution).reduce(
        (sum, count) => sum + count,
        0
      );
      expect(totalDistribution).toBe(5);
    });

    it('should calculate trends over time', () => {
      const player = createEntity('Player1', true);
      const enemy = createEntity('Enemy');
      const baseTime = new Date(2024, 0, 1, 12, 0, 0);

      const sessions = [];
      for (let i = 0; i < 4; i++) {
        const events: CombatEvent[] = [
          createDamageEvent(new Date(baseTime.getTime() + i * 3600000), player, enemy, 100 + i * 50),
          createDamageEvent(new Date(baseTime.getTime() + i * 3600000 + 1000), player, enemy, 100),
        ];
        sessions.push(createSession(events));
      }

      const aggregate = calculator.calculateAggregate(sessions, 'Player1');

      expect(aggregate).not.toBeNull();
      expect(aggregate!.dpsOverTime.length).toBe(4);
      expect(aggregate!.kdrOverTime.length).toBe(4);
      expect(aggregate!.performanceOverTime.length).toBe(4);
    });

    it('should calculate consistency rating', () => {
      const player = createEntity('Player1', true);
      const enemy = createEntity('Enemy');
      const baseTime = new Date(2024, 0, 1, 12, 0, 0);

      // Create consistent sessions with similar damage
      const sessions = [];
      for (let i = 0; i < 5; i++) {
        const events: CombatEvent[] = [
          createDamageEvent(new Date(baseTime.getTime() + i * 3600000), player, enemy, 100),
          createDamageEvent(new Date(baseTime.getTime() + i * 3600000 + 1000), player, enemy, 100),
        ];
        sessions.push(createSession(events));
      }

      const aggregate = calculator.calculateAggregate(sessions, 'Player1');

      expect(aggregate).not.toBeNull();
      expect(['VERY_CONSISTENT', 'CONSISTENT', 'VARIABLE', 'INCONSISTENT']).toContain(
        aggregate!.consistencyRating
      );
    });
  });

  describe('calculateAllPlayers', () => {
    it('should return empty map for empty sessions', () => {
      const result = calculator.calculateAllPlayers([]);
      expect(result.size).toBe(0);
    });

    it('should return stats for all unique players', () => {
      const player1 = createEntity('Player1', true);
      const player2 = createEntity('Player2');
      const enemy = createEntity('Enemy');
      const baseTime = new Date(2024, 0, 1, 12, 0, 0);

      const events: CombatEvent[] = [
        createDamageEvent(new Date(baseTime.getTime()), player1, enemy, 100),
        createDamageEvent(new Date(baseTime.getTime() + 1000), player2, enemy, 150),
        createDamageEvent(new Date(baseTime.getTime() + 2000), player1, enemy, 100),
      ];

      const sessions = [createSession(events)];
      const allStats = calculator.calculateAllPlayers(sessions);

      expect(allStats.size).toBeGreaterThanOrEqual(2);
      expect(allStats.has('Player1')).toBe(true);
      expect(allStats.has('Player2')).toBe(true);
    });
  });
});
