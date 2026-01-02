import { StreamingSessionDetector } from '../../../src/streaming/StreamingSessionDetector';
import type { SessionUpdate } from '../../../src/streaming/types';
import type { CombatEvent, DamageEvent } from '../../../src/types';
import { EventType, DamageType, ActionType, EntityType } from '../../../src/types';
import { v4 as uuidv4 } from 'uuid';

describe('StreamingSessionDetector', () => {
  let detector: StreamingSessionDetector;

  beforeEach(() => {
    detector = new StreamingSessionDetector({
      inactivityTimeoutMs: 100, // Short timeout for testing
      minEventsForSession: 2,
      minDurationMs: 10,
    });
  });

  afterEach(() => {
    detector.destroy();
  });

  const createDamageEvent = (timeOffset: number = 0): DamageEvent => ({
    id: uuidv4(),
    timestamp: new Date(Date.now() + timeOffset),
    rawTimestamp: '[12:34:56]',
    rawLine: 'test line',
    lineNumber: 1,
    eventType: EventType.DAMAGE_DEALT,
    source: {
      name: 'Player',
      entityType: EntityType.SELF,
      isPlayer: true,
      isSelf: true,
    },
    target: {
      name: 'Monster',
      entityType: EntityType.NPC,
      isPlayer: false,
      isSelf: false,
    },
    amount: 100,
    absorbedAmount: 0,
    effectiveAmount: 100,
    damageType: DamageType.SLASH,
    actionType: ActionType.MELEE,
    isCritical: false,
    isBlocked: false,
    isParried: false,
    isEvaded: false,
  });

  describe('state management', () => {
    it('should start in idle state', () => {
      expect(detector.getState()).toBe('idle');
      expect(detector.hasActiveSession()).toBe(false);
    });

    it('should transition to active on first event', () => {
      detector.processEvent(createDamageEvent());
      expect(detector.getState()).toBe('active');
      expect(detector.hasActiveSession()).toBe(true);
    });

    it('should return to idle after inactivity timeout', async () => {
      const endUpdates: SessionUpdate[] = [];
      detector.on('session:end', (update) => endUpdates.push(update));

      detector.processEvent(createDamageEvent());
      detector.processEvent(createDamageEvent(10));

      expect(detector.getState()).toBe('active');

      // Wait for timeout
      await new Promise(resolve => setTimeout(resolve, 200));

      expect(detector.getState()).toBe('idle');
      expect(endUpdates).toHaveLength(1);
    });
  });

  describe('session events', () => {
    it('should emit session:start on first combat event', () => {
      const startUpdates: SessionUpdate[] = [];
      detector.on('session:start', (update) => startUpdates.push(update));

      detector.processEvent(createDamageEvent());

      expect(startUpdates).toHaveLength(1);
      expect(startUpdates[0]?.type).toBe('SESSION_STARTED');
    });

    it('should emit session:update on subsequent events', () => {
      const updates: SessionUpdate[] = [];
      detector.on('session:update', (update) => updates.push(update));

      detector.processEvent(createDamageEvent());
      detector.processEvent(createDamageEvent(10));
      detector.processEvent(createDamageEvent(20));

      expect(updates).toHaveLength(2); // 2 updates (not counting first event)
    });

    it('should emit session:end when session ends', async () => {
      const endUpdates: SessionUpdate[] = [];
      detector.on('session:end', (update) => endUpdates.push(update));

      detector.processEvent(createDamageEvent());
      detector.processEvent(createDamageEvent(10));

      // Wait for timeout
      await new Promise(resolve => setTimeout(resolve, 200));

      expect(endUpdates).toHaveLength(1);
      expect(endUpdates[0]?.type).toBe('SESSION_ENDED');
    });
  });

  describe('session requirements', () => {
    it('should not emit session:end if min events not met', async () => {
      const endUpdates: SessionUpdate[] = [];
      detector.on('session:end', (update) => endUpdates.push(update));

      // Only one event (need 2)
      detector.processEvent(createDamageEvent());

      // Wait for timeout
      await new Promise(resolve => setTimeout(resolve, 200));

      expect(endUpdates).toHaveLength(0); // Session discarded
    });

    it('should emit session:end if min events met', async () => {
      const endUpdates: SessionUpdate[] = [];
      detector.on('session:end', (update) => endUpdates.push(update));

      detector.processEvent(createDamageEvent());
      detector.processEvent(createDamageEvent(15)); // 15ms > 10ms minDuration

      // Wait for timeout
      await new Promise(resolve => setTimeout(resolve, 200));

      expect(endUpdates).toHaveLength(1);
    });
  });

  describe('forceEndSession', () => {
    it('should immediately end active session', () => {
      const endUpdates: SessionUpdate[] = [];
      detector.on('session:end', (update) => endUpdates.push(update));

      detector.processEvent(createDamageEvent());
      detector.processEvent(createDamageEvent(10));

      detector.forceEndSession();

      expect(endUpdates).toHaveLength(1);
      expect(detector.getState()).toBe('idle');
    });

    it('should be safe to call when no session', () => {
      expect(() => detector.forceEndSession()).not.toThrow();
    });
  });

  describe('unknown events', () => {
    it('should ignore unknown events', () => {
      const startUpdates: SessionUpdate[] = [];
      detector.on('session:start', (update) => startUpdates.push(update));

      const unknownEvent: CombatEvent = {
        id: uuidv4(),
        timestamp: new Date(),
        rawTimestamp: '[12:34:56]',
        rawLine: 'test',
        lineNumber: 1,
        eventType: EventType.UNKNOWN,
      };

      detector.processEvent(unknownEvent);

      expect(startUpdates).toHaveLength(0);
      expect(detector.getState()).toBe('idle');
    });
  });

  describe('getActiveSession', () => {
    it('should return null when idle', () => {
      expect(detector.getActiveSession()).toBeNull();
    });

    it('should return session info when active', () => {
      detector.processEvent(createDamageEvent());

      const session = detector.getActiveSession();
      expect(session).not.toBeNull();
      expect(session?.id).toBeDefined();
      expect(session?.events).toHaveLength(1);
    });
  });

  describe('session content', () => {
    it('should track participants', () => {
      const endUpdates: SessionUpdate[] = [];
      detector.on('session:end', (update) => endUpdates.push(update));

      detector.processEvent(createDamageEvent());
      detector.processEvent(createDamageEvent(15));

      detector.forceEndSession();

      const session = endUpdates[0]?.session;
      expect(session?.participants).toHaveLength(2); // Player and Monster
    });

    it('should calculate summary stats', () => {
      const endUpdates: SessionUpdate[] = [];
      detector.on('session:end', (update) => endUpdates.push(update));

      detector.processEvent(createDamageEvent());
      detector.processEvent(createDamageEvent(15));

      detector.forceEndSession();

      const session = endUpdates[0]?.session;
      expect(session?.summary.totalDamageDealt).toBe(200); // 100 + 100
    });

    it('should track session duration', () => {
      const endUpdates: SessionUpdate[] = [];
      detector.on('session:end', (update) => endUpdates.push(update));

      const event1 = createDamageEvent(0);
      const event2 = createDamageEvent(50);

      detector.processEvent(event1);
      detector.processEvent(event2);

      detector.forceEndSession();

      const session = endUpdates[0]?.session;
      expect(session?.durationMs).toBeGreaterThanOrEqual(40);
    });
  });

  describe('cleanup', () => {
    it('should clean up on destroy', () => {
      detector.processEvent(createDamageEvent());
      detector.destroy();

      expect(detector.getState()).toBe('idle');
      expect(detector.getActiveSession()).toBeNull();
    });
  });
});
