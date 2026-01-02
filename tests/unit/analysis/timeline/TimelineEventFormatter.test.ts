import { TimelineEventFormatter } from '../../../../src/analysis/timeline/TimelineEventFormatter';
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
} from '../../../../src/types';

describe('TimelineEventFormatter', () => {
  const formatter = new TimelineEventFormatter();

  const baseTime = new Date(2024, 0, 1, 12, 0, 0);
  const sessionStartTime = new Date(2024, 0, 1, 12, 0, 0);

  const createEntity = (name: string, isSelf = false): Entity => ({
    name,
    entityType: isSelf ? EntityType.SELF : EntityType.PLAYER,
    isPlayer: true,
    isSelf,
  });

  describe('formatEvent - Damage Events', () => {
    const createDamageEvent = (
      timestamp: Date,
      source: Entity,
      target: Entity,
      amount: number,
      eventType: EventType.DAMAGE_DEALT | EventType.DAMAGE_RECEIVED = EventType.DAMAGE_DEALT,
      options: Partial<{
        isCritical: boolean;
        absorbedAmount: number;
        actionName: string;
      }> = {}
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
      effectiveAmount: amount - (options.absorbedAmount ?? 0),
      absorbedAmount: options.absorbedAmount ?? 0,
      damageType: DamageType.CRUSH,
      actionType: ActionType.MELEE,
      actionName: options.actionName ?? 'Attack',
      isCritical: options.isCritical ?? false,
      isBlocked: false,
      isParried: false,
      isEvaded: false,
    });

    it('should format outgoing damage event correctly', () => {
      const player = createEntity('Player', true);
      const enemy = createEntity('Enemy');
      const event = createDamageEvent(baseTime, player, enemy, 150);

      const entry = formatter.formatEvent(event, sessionStartTime);

      expect(entry.markerCategory).toBe('DAMAGE_OUTGOING');
      expect(entry.description).toContain('Player hit Enemy for 150');
      expect(entry.source).toBe(player);
      expect(entry.target).toBe(enemy);
      expect(entry.primaryValue).toBe(150);
      expect(entry.primaryValueUnit).toBe('damage');
    });

    it('should format incoming damage event correctly', () => {
      const player = createEntity('Player', true);
      const enemy = createEntity('Enemy');
      const event = createDamageEvent(
        baseTime,
        enemy,
        player,
        200,
        EventType.DAMAGE_RECEIVED
      );

      const entry = formatter.formatEvent(event, sessionStartTime);

      expect(entry.markerCategory).toBe('DAMAGE_INCOMING');
      expect(entry.description).toContain('Enemy hit Player for 200');
    });

    it('should include critical hit in description', () => {
      const player = createEntity('Player', true);
      const enemy = createEntity('Enemy');
      const event = createDamageEvent(baseTime, player, enemy, 300, EventType.DAMAGE_DEALT, {
        isCritical: true,
      });

      const entry = formatter.formatEvent(event, sessionStartTime);

      expect(entry.description).toContain('(CRITICAL)');
      expect(entry.details.isCritical).toBe(true);
    });

    it('should include absorbed amount in description', () => {
      const player = createEntity('Player', true);
      const enemy = createEntity('Enemy');
      const event = createDamageEvent(baseTime, player, enemy, 200, EventType.DAMAGE_DEALT, {
        absorbedAmount: 50,
      });

      const entry = formatter.formatEvent(event, sessionStartTime);

      expect(entry.description).toContain('(50 absorbed)');
      expect(entry.details.absorbedAmount).toBe(50);
    });

    it('should include action name in description', () => {
      const player = createEntity('Player', true);
      const enemy = createEntity('Enemy');
      const event = createDamageEvent(baseTime, player, enemy, 250, EventType.DAMAGE_DEALT, {
        actionName: 'Fireball',
      });

      const entry = formatter.formatEvent(event, sessionStartTime);

      expect(entry.description).toContain('Fireball');
      expect(entry.details.actionName).toBe('Fireball');
    });
  });

  describe('formatEvent - Healing Events', () => {
    const createHealingEvent = (
      timestamp: Date,
      source: Entity,
      target: Entity,
      amount: number,
      options: Partial<{
        overheal: number;
        isCritical: boolean;
      }> = {}
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
      effectiveAmount: amount - (options.overheal ?? 0),
      overheal: options.overheal ?? 0,
      spellName: 'Greater Heal',
      isCritical: options.isCritical ?? false,
    });

    it('should format healing event correctly', () => {
      const healer = createEntity('Healer');
      const tank = createEntity('Tank');
      const event = createHealingEvent(baseTime, healer, tank, 500);

      const entry = formatter.formatEvent(event, sessionStartTime);

      expect(entry.markerCategory).toBe('HEALING_OUTGOING');
      expect(entry.description).toContain('Healer healed Tank for 500');
      expect(entry.source).toBe(healer);
      expect(entry.target).toBe(tank);
      expect(entry.primaryValue).toBe(500);
      expect(entry.primaryValueUnit).toBe('healing');
    });

    it('should include overheal in description', () => {
      const healer = createEntity('Healer');
      const tank = createEntity('Tank');
      const event = createHealingEvent(baseTime, healer, tank, 500, {
        overheal: 100,
      });

      const entry = formatter.formatEvent(event, sessionStartTime);

      expect(entry.description).toContain('(100 overheal)');
      expect(entry.details.overheal).toBe(100);
      expect(entry.primaryValue).toBe(400); // effectiveAmount
    });

    it('should include critical heal in description', () => {
      const healer = createEntity('Healer');
      const tank = createEntity('Tank');
      const event = createHealingEvent(baseTime, healer, tank, 750, {
        isCritical: true,
      });

      const entry = formatter.formatEvent(event, sessionStartTime);

      expect(entry.description).toContain('(CRITICAL)');
      expect(entry.details.isCritical).toBe(true);
    });
  });

  describe('formatEvent - CC Events', () => {
    const createCCEvent = (
      timestamp: Date,
      source: Entity | undefined,
      target: Entity,
      effect: CrowdControlEffect = CrowdControlEffect.STUN,
      duration = 5,
      isResisted = false
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
      isResisted,
    });

    it('should format CC event correctly', () => {
      const caster = createEntity('Caster');
      const target = createEntity('Target');
      const event = createCCEvent(baseTime, caster, target, CrowdControlEffect.STUN, 5);

      const entry = formatter.formatEvent(event, sessionStartTime);

      expect(entry.markerCategory).toBe('CROWD_CONTROL');
      expect(entry.description).toContain('Caster stunned Target for 5s');
      expect(entry.source).toBe(caster);
      expect(entry.target).toBe(target);
      expect(entry.primaryValue).toBe(5);
      expect(entry.primaryValueUnit).toBe('seconds');
      expect(entry.details.ccEffect).toBe(CrowdControlEffect.STUN);
    });

    it('should handle resisted CC', () => {
      const caster = createEntity('Caster');
      const target = createEntity('Target');
      const event = createCCEvent(baseTime, caster, target, CrowdControlEffect.MESMERIZE, 8, true);

      const entry = formatter.formatEvent(event, sessionStartTime);

      expect(entry.description).toContain('(RESISTED)');
      expect(entry.details.wasResisted).toBe(true);
    });

    it('should handle unknown source in CC', () => {
      const target = createEntity('Target');
      const event = createCCEvent(baseTime, undefined, target, CrowdControlEffect.ROOT, 3);

      const entry = formatter.formatEvent(event, sessionStartTime);

      expect(entry.description).toContain('Unknown');
    });

    it('should format different CC effects', () => {
      const caster = createEntity('Caster');
      const target = createEntity('Target');

      const rootEvent = createCCEvent(baseTime, caster, target, CrowdControlEffect.ROOT, 4);
      expect(formatter.formatEvent(rootEvent, sessionStartTime).description).toContain('rooted');

      const snareEvent = createCCEvent(baseTime, caster, target, CrowdControlEffect.SNARE, 6);
      expect(formatter.formatEvent(snareEvent, sessionStartTime).description).toContain('snared');
    });
  });

  describe('formatEvent - Death Events', () => {
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

    it('should format death event with killer', () => {
      const victim = createEntity('Victim');
      const killer = createEntity('Killer');
      const event = createDeathEvent(baseTime, victim, killer);

      const entry = formatter.formatEvent(event, sessionStartTime);

      expect(entry.markerCategory).toBe('DEATH');
      expect(entry.description).toContain('Victim was killed by Killer');
      expect(entry.source).toBe(killer);
      expect(entry.target).toBe(victim);
      expect(entry.primaryValue).toBeUndefined();
      expect(entry.details.killer).toBe(killer);
    });

    it('should format death event without killer', () => {
      const victim = createEntity('Victim');
      const event = createDeathEvent(baseTime, victim);

      const entry = formatter.formatEvent(event, sessionStartTime);

      expect(entry.description).toBe('Victim was killed');
      expect(entry.source).toBeUndefined();
    });
  });

  describe('formatEvent - Time Formatting', () => {
    it('should calculate relative time correctly', () => {
      const player = createEntity('Player', true);
      const enemy = createEntity('Enemy');
      const eventTime = new Date(sessionStartTime.getTime() + 65000); // 1 minute 5 seconds later
      const event: DamageEvent = {
        id: 'test',
        timestamp: eventTime,
        rawTimestamp: '[12:01:05]',
        rawLine: 'test',
        lineNumber: 1,
        eventType: EventType.DAMAGE_DEALT,
        source: player,
        target: enemy,
        amount: 100,
        effectiveAmount: 100,
        absorbedAmount: 0,
        damageType: DamageType.CRUSH,
        actionType: ActionType.MELEE,
        actionName: 'Attack',
        isCritical: false,
        isBlocked: false,
        isParried: false,
        isEvaded: false,
      };

      const entry = formatter.formatEvent(event, sessionStartTime);

      expect(entry.relativeTimeMs).toBe(65000);
      expect(entry.formattedRelativeTime).toBe('+1:05');
    });

    it('should format absolute timestamp', () => {
      const player = createEntity('Player', true);
      const enemy = createEntity('Enemy');
      const eventTime = new Date(2024, 0, 1, 14, 30, 45);
      const event: DamageEvent = {
        id: 'test',
        timestamp: eventTime,
        rawTimestamp: '[14:30:45]',
        rawLine: 'test',
        lineNumber: 1,
        eventType: EventType.DAMAGE_DEALT,
        source: player,
        target: enemy,
        amount: 100,
        effectiveAmount: 100,
        absorbedAmount: 0,
        damageType: DamageType.CRUSH,
        actionType: ActionType.MELEE,
        actionName: 'Attack',
        isCritical: false,
        isBlocked: false,
        isParried: false,
        isEvaded: false,
      };

      const entry = formatter.formatEvent(event, sessionStartTime);

      expect(entry.formattedTimestamp).toBe('14:30:45');
    });
  });
});
