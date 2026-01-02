import type { CombatEvent, Entity, CrowdControlEffect } from '../../types/index.js';

/**
 * Role of a participant determined by their actions
 */
export type ParticipantRole = 'DAMAGE_DEALER' | 'HEALER' | 'TANK' | 'HYBRID' | 'UNKNOWN';

/**
 * A participant in a combat session
 */
export interface SessionParticipant {
  /** The entity */
  entity: Entity;
  /** Role in the fight (based on actions) */
  role: ParticipantRole;
  /** First appearance timestamp */
  firstSeen: Date;
  /** Last appearance timestamp */
  lastSeen: Date;
  /** Number of events involving this participant */
  eventCount: number;
}

/**
 * Why a key event is notable
 */
export type KeyEventReason =
  | 'DEATH'
  | 'BIG_HIT'
  | 'BIG_HEAL'
  | 'CRITICAL_HIT'
  | 'CROWD_CONTROL'
  | 'SESSION_START'
  | 'SESSION_END';

/**
 * A notable event in a combat session
 */
export interface KeyEvent {
  /** The original event */
  event: CombatEvent;
  /** Why this event is notable */
  reason: KeyEventReason;
  /** Description of the key event */
  description: string;
  /** Timestamp relative to session start (ms) */
  relativeTimeMs: number;
}

/**
 * High-level summary of a combat session
 */
export interface SessionSummary {
  /** Total damage dealt by all participants */
  totalDamageDealt: number;
  /** Total damage taken by all participants */
  totalDamageTaken: number;
  /** Total healing done by all participants */
  totalHealingDone: number;
  /** Total healing received by all participants */
  totalHealingReceived: number;
  /** Number of deaths in the session */
  deathCount: number;
  /** Number of crowd control events */
  ccEventCount: number;
  /** Key events (big hits, deaths, CC) */
  keyEvents: KeyEvent[];
}

/**
 * Represents a detected combat session/fight
 */
export interface CombatSession {
  /** Unique session identifier */
  id: string;
  /** Start timestamp */
  startTime: Date;
  /** End timestamp */
  endTime: Date;
  /** Duration in milliseconds */
  durationMs: number;
  /** All events in this session */
  events: CombatEvent[];
  /** All participants in this session */
  participants: SessionParticipant[];
  /** Summary statistics for this session */
  summary: SessionSummary;
}

/**
 * Update emitted during streaming session detection
 */
export interface SessionUpdate {
  type: 'SESSION_STARTED' | 'SESSION_UPDATED' | 'SESSION_ENDED';
  session: CombatSession;
}
