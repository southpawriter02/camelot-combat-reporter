import { FightSummarizer } from '../../../../src/analysis/summary/FightSummarizer';
import { SessionDetector } from '../../../../src/analysis/session/SessionDetector';
import {
  EventType,
  DamageType,
  ActionType,
  EntityType,
  CrowdControlEffect,
} from '../../../../src/types';
import type {
  DamageEvent,
  HealingEvent,
  DeathEvent,
  CrowdControlEvent,
  Entity,
  CombatEvent,
} from '../../../../src/types';

describe('FightSummarizer', () => {
  const summarizer = new FightSummarizer();
  const detector = new SessionDetector({
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
    eventType: EventType.DAMAGE_DEALT | EventType.DAMAGE_RECEIVED = EventType.DAMAGE_DEALT,
    isCritical = false
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
    isCritical,
    isBlocked: false,
    isParried: false,
    isEvaded: false,
  });

  const createHealingEvent = (
    timestamp: Date,
    source: Entity,
    target: Entity,
    amount: number,
    overheal = 0
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
    effectiveAmount: amount - overheal,
    overheal,
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

  const createCCEvent = (
    timestamp: Date,
    source: Entity,
    target: Entity,
    effect: CrowdControlEffect = CrowdControlEffect.STUN,
    duration = 5
  ): CrowdControlEvent => ({
    id: `cc-${Date.now()}-${Math.random()}`,
    timestamp,
    rawTimestamp: '[12:00:00]',
    rawLine: 'test',
    lineNumber: 1,
    eventType: EventType.CROWD_CONTROL,
    source,
    target,
    effect,
    duration,
    isResisted: false,
  });

  describe('summarize', () => {
    it('should create a complete fight summary', () => {
      const player1 = createEntity('Player1', true);
      const player2 = createEntity('Player2');
      const enemy = createEntity('Enemy');
      const baseTime = new Date(2024, 0, 1, 12, 0, 0);

      const events: CombatEvent[] = [
        createDamageEvent(new Date(baseTime.getTime()), player1, enemy, 100),
        createDamageEvent(new Date(baseTime.getTime() + 1000), player2, enemy, 150),
        createHealingEvent(new Date(baseTime.getTime() + 2000), player2, player1, 50),
        createDamageEvent(new Date(baseTime.getTime() + 3000), player1, enemy, 200),
      ];

      const sessions = detector.detect(events);
      expect(sessions).toHaveLength(1);

      const summary = summarizer.summarize(sessions[0]!);

      expect(summary).toHaveProperty('session');
      expect(summary).toHaveProperty('durationFormatted');
      expect(summary).toHaveProperty('damageMeter');
      expect(summary).toHaveProperty('healingMeter');
      expect(summary).toHaveProperty('deathTimeline');
      expect(summary).toHaveProperty('ccTimeline');
      expect(summary).toHaveProperty('keyEvents');
    });

    it('should build damage meter correctly', () => {
      const player1 = createEntity('Player1', true);
      const player2 = createEntity('Player2');
      const enemy = createEntity('Enemy');
      const baseTime = new Date(2024, 0, 1, 12, 0, 0);

      const events: CombatEvent[] = [
        createDamageEvent(new Date(baseTime.getTime()), player1, enemy, 300),
        createDamageEvent(new Date(baseTime.getTime() + 1000), player2, enemy, 100),
      ];

      const sessions = detector.detect(events);
      const summary = summarizer.summarize(sessions[0]!);

      expect(summary.damageMeter.length).toBeGreaterThan(0);
      // Player1 did more damage, should be rank 1
      const player1Entry = summary.damageMeter.find(
        (e) => e.entity.name === 'Player1'
      );
      expect(player1Entry).toBeDefined();
      expect(player1Entry!.rank).toBe(1);
      expect(player1Entry!.totalDamage).toBe(300);
    });

    it('should build healing meter correctly', () => {
      const player1 = createEntity('Player1', true);
      const healer = createEntity('Healer');
      const enemy = createEntity('Enemy');
      const baseTime = new Date(2024, 0, 1, 12, 0, 0);

      const events: CombatEvent[] = [
        createDamageEvent(new Date(baseTime.getTime()), player1, enemy, 100),
        createHealingEvent(new Date(baseTime.getTime() + 1000), healer, player1, 200),
        createHealingEvent(new Date(baseTime.getTime() + 2000), healer, player1, 150),
      ];

      const sessions = detector.detect(events);
      const summary = summarizer.summarize(sessions[0]!);

      const healerEntry = summary.healingMeter.find(
        (e) => e.entity.name === 'Healer'
      );
      expect(healerEntry).toBeDefined();
      expect(healerEntry!.totalHealing).toBe(350);
      expect(healerEntry!.rank).toBe(1);
    });

    it('should build death timeline correctly', () => {
      const player1 = createEntity('Player1', true);
      const enemy = createEntity('Enemy');
      const baseTime = new Date(2024, 0, 1, 12, 0, 0);

      const events: CombatEvent[] = [
        createDamageEvent(new Date(baseTime.getTime()), player1, enemy, 100),
        createDamageEvent(
          new Date(baseTime.getTime() + 1000),
          enemy,
          player1,
          50,
          EventType.DAMAGE_RECEIVED
        ),
        createDeathEvent(new Date(baseTime.getTime() + 2000), player1, enemy),
      ];

      const sessions = detector.detect(events);
      const summary = summarizer.summarize(sessions[0]!);

      expect(summary.deathTimeline).toHaveLength(1);
      expect(summary.deathTimeline[0]!.target.name).toBe('Player1');
      expect(summary.deathTimeline[0]!.killer?.name).toBe('Enemy');
    });

    it('should build CC timeline correctly', () => {
      const player1 = createEntity('Player1', true);
      const enemy = createEntity('Enemy');
      const baseTime = new Date(2024, 0, 1, 12, 0, 0);

      const events: CombatEvent[] = [
        createDamageEvent(new Date(baseTime.getTime()), player1, enemy, 100),
        createCCEvent(
          new Date(baseTime.getTime() + 1000),
          enemy,
          player1,
          CrowdControlEffect.STUN,
          5
        ),
        createDamageEvent(new Date(baseTime.getTime() + 2000), player1, enemy, 100),
      ];

      const sessions = detector.detect(events);
      const summary = summarizer.summarize(sessions[0]!);

      expect(summary.ccTimeline).toHaveLength(1);
      expect(summary.ccTimeline[0]!.target.name).toBe('Player1');
      expect(summary.ccTimeline[0]!.effect).toBe(CrowdControlEffect.STUN);
    });

    it('should format duration correctly', () => {
      const player1 = createEntity('Player1', true);
      const enemy = createEntity('Enemy');
      const baseTime = new Date(2024, 0, 1, 12, 0, 0);

      // Use a longer session detector for this test to span 2.5 minutes
      const longSessionDetector = new SessionDetector({
        inactivityTimeoutMs: 180000, // 3 minutes
        minEventsForSession: 2,
      });

      const events: CombatEvent[] = [
        createDamageEvent(new Date(baseTime.getTime()), player1, enemy, 100),
        // 2 minutes 30 seconds later
        createDamageEvent(
          new Date(baseTime.getTime() + 150000),
          player1,
          enemy,
          100
        ),
      ];

      const sessions = longSessionDetector.detect(events);
      const summary = summarizer.summarize(sessions[0]!);

      expect(summary.durationFormatted).toBe('2m 30s');
    });
  });

  describe('getDamageReport', () => {
    it('should generate a text report', () => {
      const player1 = createEntity('Player1', true);
      const enemy = createEntity('Enemy');
      const baseTime = new Date(2024, 0, 1, 12, 0, 0);

      const events: CombatEvent[] = [
        createDamageEvent(new Date(baseTime.getTime()), player1, enemy, 100),
        createDamageEvent(new Date(baseTime.getTime() + 1000), player1, enemy, 200),
      ];

      const sessions = detector.detect(events);
      const report = summarizer.getDamageReport(sessions[0]!);

      expect(report).toContain('Fight Summary');
      expect(report).toContain('Damage Done');
      expect(report).toContain('Player1');
    });
  });
});
