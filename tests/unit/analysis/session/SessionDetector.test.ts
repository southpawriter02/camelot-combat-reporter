import { SessionDetector } from '../../../../src/analysis/session/SessionDetector';
import { EventType, DamageType, ActionType, EntityType } from '../../../../src/types';
import type { DamageEvent, Entity, CombatEvent } from '../../../../src/types';

describe('SessionDetector', () => {
  const detector = new SessionDetector({
    inactivityTimeoutMs: 5000, // 5 seconds for testing
    minEventsForSession: 2,
  });

  const createEntity = (name: string): Entity => ({
    name,
    entityType: EntityType.PLAYER,
    isPlayer: true,
    isSelf: false,
  });

  const createDamageEvent = (
    timestamp: Date,
    source: Entity,
    target: Entity,
    amount: number = 100
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

  describe('detect', () => {
    it('should return empty array for no events', () => {
      const sessions = detector.detect([]);
      expect(sessions).toHaveLength(0);
    });

    it('should detect a single session from continuous events', () => {
      const player = createEntity('Player');
      const enemy = createEntity('Enemy');
      const baseTime = new Date(2024, 0, 1, 12, 0, 0);

      const events: CombatEvent[] = [
        createDamageEvent(new Date(baseTime.getTime()), player, enemy),
        createDamageEvent(new Date(baseTime.getTime() + 1000), player, enemy),
        createDamageEvent(new Date(baseTime.getTime() + 2000), player, enemy),
      ];

      const sessions = detector.detect(events);
      expect(sessions).toHaveLength(1);
      expect(sessions[0]!.events).toHaveLength(3);
    });

    it('should detect multiple sessions with gaps', () => {
      const player = createEntity('Player');
      const enemy = createEntity('Enemy');
      const baseTime = new Date(2024, 0, 1, 12, 0, 0);

      const events: CombatEvent[] = [
        // First fight
        createDamageEvent(new Date(baseTime.getTime()), player, enemy),
        createDamageEvent(new Date(baseTime.getTime() + 1000), player, enemy),
        // Gap of 10 seconds (> 5 second threshold)
        // Second fight
        createDamageEvent(new Date(baseTime.getTime() + 11000), player, enemy),
        createDamageEvent(new Date(baseTime.getTime() + 12000), player, enemy),
      ];

      const sessions = detector.detect(events);
      expect(sessions).toHaveLength(2);
      expect(sessions[0]!.events).toHaveLength(2);
      expect(sessions[1]!.events).toHaveLength(2);
    });

    it('should filter out sessions with too few events', () => {
      const player = createEntity('Player');
      const enemy = createEntity('Enemy');
      const baseTime = new Date(2024, 0, 1, 12, 0, 0);

      const events: CombatEvent[] = [
        // Single event (below minEventsForSession)
        createDamageEvent(new Date(baseTime.getTime()), player, enemy),
        // Gap
        // Two events (meets threshold)
        createDamageEvent(new Date(baseTime.getTime() + 10000), player, enemy),
        createDamageEvent(new Date(baseTime.getTime() + 11000), player, enemy),
      ];

      const sessions = detector.detect(events);
      expect(sessions).toHaveLength(1);
      expect(sessions[0]!.events).toHaveLength(2);
    });

    it('should correctly identify participants', () => {
      const player1 = createEntity('Player1');
      const player2 = createEntity('Player2');
      const enemy = createEntity('Enemy');
      const baseTime = new Date(2024, 0, 1, 12, 0, 0);

      const events: CombatEvent[] = [
        createDamageEvent(new Date(baseTime.getTime()), player1, enemy),
        createDamageEvent(new Date(baseTime.getTime() + 1000), player2, enemy),
        createDamageEvent(new Date(baseTime.getTime() + 2000), enemy, player1),
      ];

      const sessions = detector.detect(events);
      expect(sessions).toHaveLength(1);

      const participants = sessions[0]!.participants;
      expect(participants.length).toBeGreaterThanOrEqual(2);

      const participantNames = participants.map((p) => p.entity.name);
      expect(participantNames).toContain('Player1');
      expect(participantNames).toContain('Player2');
    });

    it('should calculate session duration correctly', () => {
      const player = createEntity('Player');
      const enemy = createEntity('Enemy');
      const baseTime = new Date(2024, 0, 1, 12, 0, 0);

      const events: CombatEvent[] = [
        createDamageEvent(new Date(baseTime.getTime()), player, enemy),
        createDamageEvent(new Date(baseTime.getTime() + 3000), player, enemy),
      ];

      const sessions = detector.detect(events);
      expect(sessions[0]!.durationMs).toBe(3000);
    });

    it('should generate unique session IDs', () => {
      const player = createEntity('Player');
      const enemy = createEntity('Enemy');
      const baseTime = new Date(2024, 0, 1, 12, 0, 0);

      const events: CombatEvent[] = [
        createDamageEvent(new Date(baseTime.getTime()), player, enemy),
        createDamageEvent(new Date(baseTime.getTime() + 1000), player, enemy),
        createDamageEvent(new Date(baseTime.getTime() + 10000), player, enemy),
        createDamageEvent(new Date(baseTime.getTime() + 11000), player, enemy),
      ];

      const sessions = detector.detect(events);
      expect(sessions).toHaveLength(2);
      expect(sessions[0]!.id).not.toBe(sessions[1]!.id);
    });
  });

  describe('session summary', () => {
    it('should calculate total damage dealt', () => {
      const player = createEntity('Player');
      const enemy = createEntity('Enemy');
      const baseTime = new Date(2024, 0, 1, 12, 0, 0);

      const events: CombatEvent[] = [
        createDamageEvent(new Date(baseTime.getTime()), player, enemy, 100),
        createDamageEvent(new Date(baseTime.getTime() + 1000), player, enemy, 150),
      ];

      const sessions = detector.detect(events);
      expect(sessions[0]!.summary.totalDamageDealt).toBe(250);
    });
  });

  describe('configuration', () => {
    it('should respect custom inactivity timeout', () => {
      const customDetector = new SessionDetector({
        inactivityTimeoutMs: 2000, // 2 seconds
        minEventsForSession: 2,
      });

      const player = createEntity('Player');
      const enemy = createEntity('Enemy');
      const baseTime = new Date(2024, 0, 1, 12, 0, 0);

      const events: CombatEvent[] = [
        createDamageEvent(new Date(baseTime.getTime()), player, enemy),
        createDamageEvent(new Date(baseTime.getTime() + 1000), player, enemy),
        // 3 second gap (> 2 second threshold)
        createDamageEvent(new Date(baseTime.getTime() + 4000), player, enemy),
        createDamageEvent(new Date(baseTime.getTime() + 5000), player, enemy),
      ];

      const sessions = customDetector.detect(events);
      expect(sessions).toHaveLength(2);
    });

    it('should respect custom minEventsForSession', () => {
      const customDetector = new SessionDetector({
        inactivityTimeoutMs: 5000,
        minEventsForSession: 5,
      });

      const player = createEntity('Player');
      const enemy = createEntity('Enemy');
      const baseTime = new Date(2024, 0, 1, 12, 0, 0);

      const events: CombatEvent[] = [
        createDamageEvent(new Date(baseTime.getTime()), player, enemy),
        createDamageEvent(new Date(baseTime.getTime() + 1000), player, enemy),
        createDamageEvent(new Date(baseTime.getTime() + 2000), player, enemy),
      ];

      const sessions = customDetector.detect(events);
      expect(sessions).toHaveLength(0); // Only 3 events, need 5
    });
  });
});
