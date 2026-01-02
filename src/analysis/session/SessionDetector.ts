import { v4 as uuidv4 } from 'uuid';
import type { CombatEvent, Entity } from '../../types/index.js';
import { EventType } from '../../types/index.js';
import type {
  CombatSessionConfig,
  CombatSession,
  SessionParticipant,
  SessionSummary,
  KeyEvent,
  ParticipantRole,
} from '../types/index.js';
import { DEFAULT_SESSION_CONFIG } from '../types/index.js';
import { calculateDuration, getRelativeTime } from '../utils/index.js';

/**
 * Detects combat sessions from a stream of events
 * Uses inactivity-based detection with configurable timeout
 */
export class SessionDetector {
  private config: CombatSessionConfig;

  constructor(config: Partial<CombatSessionConfig> = {}) {
    this.config = { ...DEFAULT_SESSION_CONFIG, ...config };
  }

  /**
   * Detect all combat sessions in a list of events
   * Events should be sorted by timestamp
   */
  detect(events: CombatEvent[]): CombatSession[] {
    // Filter to combat-relevant events
    const combatEvents = this.filterCombatEvents(events);

    if (combatEvents.length === 0) {
      return [];
    }

    // Sort by timestamp
    const sorted = [...combatEvents].sort(
      (a, b) => a.timestamp.getTime() - b.timestamp.getTime()
    );

    const sessions: CombatSession[] = [];
    let currentSessionEvents: CombatEvent[] = [sorted[0]!];

    for (let i = 1; i < sorted.length; i++) {
      const prev = sorted[i - 1]!;
      const curr = sorted[i]!;
      const gap = curr.timestamp.getTime() - prev.timestamp.getTime();

      if (gap > this.config.inactivityTimeoutMs) {
        // Gap too large - end current session and start new one
        if (currentSessionEvents.length >= this.config.minEventsForSession) {
          sessions.push(this.createSession(currentSessionEvents));
        }
        currentSessionEvents = [curr];
      } else {
        currentSessionEvents.push(curr);
      }
    }

    // Don't forget the last session
    if (currentSessionEvents.length >= this.config.minEventsForSession) {
      sessions.push(this.createSession(currentSessionEvents));
    }

    // Optionally merge nearby sessions
    if (this.config.mergeNearbySessions && sessions.length > 1) {
      return this.mergeSessions(sessions);
    }

    return sessions;
  }

  /**
   * Filter to combat-relevant events (damage, healing, CC, death)
   */
  private filterCombatEvents(events: CombatEvent[]): CombatEvent[] {
    return events.filter(
      (e) =>
        e.eventType === EventType.DAMAGE_DEALT ||
        e.eventType === EventType.DAMAGE_RECEIVED ||
        e.eventType === EventType.HEALING_DONE ||
        e.eventType === EventType.HEALING_RECEIVED ||
        e.eventType === EventType.CROWD_CONTROL ||
        e.eventType === EventType.DEATH
    );
  }

  /**
   * Create a combat session from a list of events
   */
  private createSession(events: CombatEvent[]): CombatSession {
    const startTime = events[0]!.timestamp;
    const endTime = events[events.length - 1]!.timestamp;
    const durationMs = calculateDuration(startTime, endTime);

    const participants = this.extractParticipants(events);
    const summary = this.createSummary(events, startTime);

    return {
      id: uuidv4(),
      startTime,
      endTime,
      durationMs,
      events,
      participants,
      summary,
    };
  }

  /**
   * Extract all participants from events
   */
  private extractParticipants(events: CombatEvent[]): SessionParticipant[] {
    const participantMap = new Map<string, SessionParticipant>();

    for (const event of events) {
      // Extract entities from event
      const entities = this.extractEntitiesFromEvent(event);

      for (const entity of entities) {
        const existing = participantMap.get(entity.name);

        if (existing) {
          // Update existing participant
          if (event.timestamp < existing.firstSeen) {
            existing.firstSeen = event.timestamp;
          }
          if (event.timestamp > existing.lastSeen) {
            existing.lastSeen = event.timestamp;
          }
          existing.eventCount++;
        } else {
          // New participant
          participantMap.set(entity.name, {
            entity,
            role: 'UNKNOWN',
            firstSeen: event.timestamp,
            lastSeen: event.timestamp,
            eventCount: 1,
          });
        }
      }
    }

    // Determine roles for each participant
    const participants = Array.from(participantMap.values());
    for (const participant of participants) {
      participant.role = this.determineRole(participant.entity, events);
    }

    return participants;
  }

  /**
   * Extract entities from an event
   */
  private extractEntitiesFromEvent(event: CombatEvent): Entity[] {
    const entities: Entity[] = [];

    if ('source' in event && event.source) {
      entities.push(event.source);
    }
    if ('target' in event && event.target) {
      entities.push(event.target);
    }
    if ('killer' in event && event.killer) {
      entities.push(event.killer);
    }

    return entities;
  }

  /**
   * Determine participant role based on their actions
   */
  private determineRole(entity: Entity, events: CombatEvent[]): ParticipantRole {
    let damageDealt = 0;
    let healingDone = 0;
    let damageTaken = 0;

    for (const event of events) {
      if (event.eventType === EventType.DAMAGE_DEALT) {
        if ('source' in event && event.source?.name === entity.name) {
          damageDealt++;
        }
      }
      if (event.eventType === EventType.DAMAGE_RECEIVED) {
        if ('target' in event && event.target?.name === entity.name) {
          damageTaken++;
        }
      }
      if (event.eventType === EventType.HEALING_DONE) {
        if ('source' in event && event.source?.name === entity.name) {
          healingDone++;
        }
      }
    }

    const totalActions = damageDealt + healingDone;
    if (totalActions === 0) {
      return 'UNKNOWN';
    }

    const healRatio = healingDone / totalActions;
    const damageRatio = damageDealt / totalActions;

    // Check if primarily a healer
    if (healRatio > 0.5) {
      return 'HEALER';
    }

    // Check if primarily damage
    if (damageRatio > 0.5) {
      // Check if also taking a lot of damage (tank)
      const totalEvents = damageDealt + healingDone + damageTaken;
      if (totalEvents > 0 && damageTaken / totalEvents > 0.3) {
        return 'TANK';
      }
      return 'DAMAGE_DEALER';
    }

    // Mixed role
    if (healRatio > 0.2 && damageRatio > 0.2) {
      return 'HYBRID';
    }

    return 'UNKNOWN';
  }

  /**
   * Create a summary of the combat session
   */
  private createSummary(events: CombatEvent[], startTime: Date): SessionSummary {
    let totalDamageDealt = 0;
    let totalDamageTaken = 0;
    let totalHealingDone = 0;
    let totalHealingReceived = 0;
    let deathCount = 0;
    let ccEventCount = 0;
    const keyEvents: KeyEvent[] = [];

    for (const event of events) {
      const relativeTimeMs = getRelativeTime(event.timestamp, startTime);

      switch (event.eventType) {
        case EventType.DAMAGE_DEALT:
          if ('amount' in event) {
            totalDamageDealt += event.amount;
          }
          break;
        case EventType.DAMAGE_RECEIVED:
          if ('amount' in event) {
            totalDamageTaken += event.amount;
          }
          break;
        case EventType.HEALING_DONE:
          if ('amount' in event) {
            totalHealingDone += event.amount;
          }
          break;
        case EventType.HEALING_RECEIVED:
          if ('amount' in event) {
            totalHealingReceived += event.amount;
          }
          break;
        case EventType.DEATH:
          deathCount++;
          keyEvents.push({
            event,
            reason: 'DEATH',
            description: `${('target' in event && event.target?.name) || 'Someone'} died`,
            relativeTimeMs,
          });
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
      keyEvents,
    };
  }

  /**
   * Merge nearby sessions
   */
  private mergeSessions(sessions: CombatSession[]): CombatSession[] {
    if (sessions.length <= 1) {
      return sessions;
    }

    const merged: CombatSession[] = [];
    let current = sessions[0]!;

    for (let i = 1; i < sessions.length; i++) {
      const next = sessions[i]!;
      const gap = next.startTime.getTime() - current.endTime.getTime();

      if (gap <= this.config.mergeWindowMs) {
        // Merge sessions
        const combinedEvents = [...current.events, ...next.events].sort(
          (a, b) => a.timestamp.getTime() - b.timestamp.getTime()
        );
        current = this.createSession(combinedEvents);
      } else {
        merged.push(current);
        current = next;
      }
    }

    merged.push(current);
    return merged;
  }

  /**
   * Update configuration
   */
  updateConfig(config: Partial<CombatSessionConfig>): void {
    this.config = { ...this.config, ...config };
  }

  /**
   * Get current configuration
   */
  getConfig(): CombatSessionConfig {
    return { ...this.config };
  }
}
