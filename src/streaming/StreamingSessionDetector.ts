/**
 * StreamingSessionDetector - Incremental session detection state machine
 *
 * Detects combat sessions from a stream of events using a state machine:
 * - IDLE: No active session, waiting for combat event
 * - ACTIVE: Session in progress, collecting events
 * - Transitions to IDLE on timeout or rotation
 */
import { EventEmitter } from 'events';
import { v4 as uuidv4 } from 'uuid';
import type { CombatEvent, Entity } from '../types/index.js';
import { EventType } from '../types/index.js';
import type {
  CombatSession,
  SessionParticipant,
  SessionSummary,
  SessionUpdate,
} from '../analysis/types/session.js';
import type {
  SessionState,
  SessionParticipantState,
  ActiveSession,
  SessionDetectorConfig,
} from './types.js';
import { DEFAULT_SESSION_DETECTOR_CONFIG } from './types.js';

/**
 * Events emitted by StreamingSessionDetector
 */
export interface SessionDetectorEvents {
  'session:start': (update: SessionUpdate) => void;
  'session:update': (update: SessionUpdate) => void;
  'session:end': (update: SessionUpdate) => void;
}

/**
 * StreamingSessionDetector tracks combat sessions from streaming events
 */
export class StreamingSessionDetector extends EventEmitter {
  private config: SessionDetectorConfig;
  private activeSession: ActiveSession | null = null;
  private inactivityTimer: NodeJS.Timeout | null = null;

  constructor(config: Partial<SessionDetectorConfig> = {}) {
    super();
    this.config = { ...DEFAULT_SESSION_DETECTOR_CONFIG, ...config };
  }

  /**
   * Process a combat event
   */
  processEvent(event: CombatEvent): void {
    // Skip unknown events for session detection
    if (event.eventType === EventType.UNKNOWN) {
      return;
    }

    if (!this.activeSession) {
      // Start new session
      this.startSession(event);
    } else {
      // Update existing session
      this.updateSession(event);
    }

    // Reset inactivity timer
    this.resetInactivityTimer();
  }

  /**
   * Force end the current session (e.g., on file rotation)
   */
  forceEndSession(): void {
    if (this.activeSession) {
      this.endSession(true);
    }
  }

  /**
   * Get current session state
   */
  getState(): SessionState {
    return this.activeSession?.state ?? 'idle';
  }

  /**
   * Get active session info (if any)
   */
  getActiveSession(): ActiveSession | null {
    return this.activeSession;
  }

  /**
   * Check if there's an active session
   */
  hasActiveSession(): boolean {
    return this.activeSession !== null;
  }

  /**
   * Start a new session
   */
  private startSession(event: CombatEvent): void {
    const sessionId = uuidv4();

    this.activeSession = {
      id: sessionId,
      state: 'active',
      startTime: event.timestamp,
      lastEventTime: event.timestamp,
      events: [event],
      participantMap: new Map(),
    };

    // Track participants from this event
    this.updateParticipants(event);

    // Emit session start
    const session = this.buildCombatSession();
    this.emit('session:start', {
      type: 'SESSION_STARTED',
      session,
    });
  }

  /**
   * Update existing session with new event
   */
  private updateSession(event: CombatEvent): void {
    if (!this.activeSession) return;

    this.activeSession.events.push(event);
    this.activeSession.lastEventTime = event.timestamp;

    // Track participants
    this.updateParticipants(event);

    // Emit session update
    const session = this.buildCombatSession();
    this.emit('session:update', {
      type: 'SESSION_UPDATED',
      session,
    });
  }

  /**
   * End the current session
   */
  private endSession(forced: boolean = false): void {
    if (!this.activeSession) return;

    // Clear timer
    if (this.inactivityTimer) {
      clearTimeout(this.inactivityTimer);
      this.inactivityTimer = null;
    }

    // Check if session meets minimum requirements
    const meetsMinEvents =
      this.activeSession.events.length >= this.config.minEventsForSession;
    const duration =
      this.activeSession.lastEventTime.getTime() -
      this.activeSession.startTime.getTime();
    const meetsMinDuration = duration >= this.config.minDurationMs;

    if (meetsMinEvents && meetsMinDuration) {
      // Valid session - emit end event
      const session = this.buildCombatSession();
      this.emit('session:end', {
        type: 'SESSION_ENDED',
        session,
      });
    }
    // Otherwise, silently discard the session

    this.activeSession = null;
  }

  /**
   * Reset the inactivity timer
   */
  private resetInactivityTimer(): void {
    if (this.inactivityTimer) {
      clearTimeout(this.inactivityTimer);
    }

    this.inactivityTimer = setTimeout(() => {
      this.endSession(false);
    }, this.config.inactivityTimeoutMs);
  }

  /**
   * Update participant tracking from an event
   */
  private updateParticipants(event: CombatEvent): void {
    if (!this.activeSession) return;

    // Extract entities from event based on type
    const entities: Entity[] = [];

    if ('source' in event && event.source) {
      entities.push(event.source as Entity);
    }
    if ('target' in event && event.target) {
      entities.push(event.target as Entity);
    }

    for (const entity of entities) {
      const key = entity.name;
      const existing = this.activeSession.participantMap.get(key);

      if (existing) {
        existing.lastSeen = event.timestamp;
        existing.eventCount++;

        // Track damage/healing
        if (
          event.eventType === EventType.DAMAGE_DEALT &&
          'source' in event &&
          (event.source as Entity)?.name === entity.name
        ) {
          existing.damageDealt += (event as { amount?: number }).amount ?? 0;
        }
        if (
          event.eventType === EventType.HEALING_DONE &&
          'source' in event &&
          (event.source as Entity)?.name === entity.name
        ) {
          existing.healingDone += (event as { amount?: number }).amount ?? 0;
        }
        if (
          event.eventType === EventType.DEATH &&
          'target' in event &&
          (event.target as Entity)?.name === entity.name
        ) {
          existing.deathCount++;
        }
      } else {
        this.activeSession.participantMap.set(key, {
          entity,
          firstSeen: event.timestamp,
          lastSeen: event.timestamp,
          eventCount: 1,
          damageDealt: 0,
          healingDone: 0,
          deathCount: 0,
        });
      }
    }
  }

  /**
   * Build a CombatSession from the active session state
   */
  private buildCombatSession(): CombatSession {
    if (!this.activeSession) {
      throw new Error('No active session');
    }

    const participants = this.buildParticipants();
    const summary = this.buildSummary();

    return {
      id: this.activeSession.id,
      startTime: this.activeSession.startTime,
      endTime: this.activeSession.lastEventTime,
      durationMs:
        this.activeSession.lastEventTime.getTime() -
        this.activeSession.startTime.getTime(),
      events: [...this.activeSession.events],
      participants,
      summary,
    };
  }

  /**
   * Build participants list from participant map
   */
  private buildParticipants(): SessionParticipant[] {
    if (!this.activeSession) return [];

    const participants: SessionParticipant[] = [];

    for (const state of this.activeSession.participantMap.values()) {
      // Determine role based on activity
      let role: SessionParticipant['role'] = 'UNKNOWN';
      if (state.healingDone > state.damageDealt * 2) {
        role = 'HEALER';
      } else if (state.damageDealt > 0 && state.healingDone > 0) {
        role = 'HYBRID';
      } else if (state.damageDealt > 0) {
        role = 'DAMAGE_DEALER';
      }

      participants.push({
        entity: state.entity,
        role,
        firstSeen: state.firstSeen,
        lastSeen: state.lastSeen,
        eventCount: state.eventCount,
      });
    }

    return participants;
  }

  /**
   * Build session summary from events
   */
  private buildSummary(): SessionSummary {
    if (!this.activeSession) {
      return {
        totalDamageDealt: 0,
        totalDamageTaken: 0,
        totalHealingDone: 0,
        totalHealingReceived: 0,
        deathCount: 0,
        ccEventCount: 0,
        keyEvents: [],
      };
    }

    let totalDamageDealt = 0;
    let totalDamageTaken = 0;
    let totalHealingDone = 0;
    let totalHealingReceived = 0;
    let deathCount = 0;
    let ccEventCount = 0;

    for (const event of this.activeSession.events) {
      switch (event.eventType) {
        case EventType.DAMAGE_DEALT:
          totalDamageDealt += (event as { amount?: number }).amount ?? 0;
          break;
        case EventType.DAMAGE_RECEIVED:
          totalDamageTaken += (event as { amount?: number }).amount ?? 0;
          break;
        case EventType.HEALING_DONE:
          totalHealingDone += (event as { amount?: number }).amount ?? 0;
          break;
        case EventType.HEALING_RECEIVED:
          totalHealingReceived += (event as { amount?: number }).amount ?? 0;
          break;
        case EventType.DEATH:
          deathCount++;
          break;
        case EventType.CROWD_CONTROL:
          ccEventCount++;
          break;
      }
    }

    return {
      totalDamageDealt,
      totalDamageTaken,
      totalHealingDone,
      totalHealingReceived,
      deathCount,
      ccEventCount,
      keyEvents: [], // Key events would require more sophisticated detection
    };
  }

  /**
   * Clean up resources
   */
  destroy(): void {
    if (this.inactivityTimer) {
      clearTimeout(this.inactivityTimer);
      this.inactivityTimer = null;
    }
    this.activeSession = null;
    this.removeAllListeners();
  }

  /**
   * Type-safe event emitter methods
   */
  override on<K extends keyof SessionDetectorEvents>(
    event: K,
    listener: SessionDetectorEvents[K]
  ): this {
    return super.on(event, listener);
  }

  override once<K extends keyof SessionDetectorEvents>(
    event: K,
    listener: SessionDetectorEvents[K]
  ): this {
    return super.once(event, listener);
  }

  override emit<K extends keyof SessionDetectorEvents>(
    event: K,
    ...args: Parameters<SessionDetectorEvents[K]>
  ): boolean {
    return super.emit(event, ...args);
  }

  override off<K extends keyof SessionDetectorEvents>(
    event: K,
    listener: SessionDetectorEvents[K]
  ): this {
    return super.off(event, listener);
  }
}
