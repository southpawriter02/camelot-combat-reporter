import type { Entity, CombatEvent, CrowdControlEffect } from '../../types/index.js';
import type { CombatSession, KeyEvent } from './session.js';
import type { ParticipantMetrics } from './metrics.js';

/**
 * Entry in the damage meter
 */
export interface DamageMeterEntry {
  /** The entity */
  entity: Entity;
  /** Total damage dealt */
  totalDamage: number;
  /** Damage per second */
  dps: number;
  /** Percentage of total group damage */
  percentage: number;
  /** Rank in the meter (1 = highest) */
  rank: number;
}

/**
 * Entry in the healing meter
 */
export interface HealingMeterEntry {
  /** The entity */
  entity: Entity;
  /** Total healing done */
  totalHealing: number;
  /** Effective healing (minus overheal) */
  effectiveHealing: number;
  /** Healing per second */
  hps: number;
  /** Overheal rate (0-1) */
  overhealRate: number;
  /** Percentage of total group healing */
  percentage: number;
  /** Rank in the meter (1 = highest) */
  rank: number;
}

/**
 * Death entry in the timeline
 */
export interface DeathTimelineEntry {
  /** Timestamp of death */
  timestamp: Date;
  /** Time relative to fight start (ms) */
  relativeTimeMs: number;
  /** Who died */
  target: Entity;
  /** Who killed them (if known) */
  killer?: Entity;
  /** Last few damage events before death */
  lastDamageEvents: CombatEvent[];
}

/**
 * CC event in the timeline
 */
export interface CCTimelineEntry {
  /** Timestamp of CC */
  timestamp: Date;
  /** Time relative to fight start (ms) */
  relativeTimeMs: number;
  /** Who applied CC (if known) */
  source?: Entity;
  /** Who was CC'd */
  target: Entity;
  /** Type of CC effect */
  effect: CrowdControlEffect;
  /** Duration in seconds */
  duration: number;
  /** Whether it was resisted */
  wasResisted: boolean;
}

/**
 * Complete fight summary with all analysis
 */
export interface FightSummary {
  /** The combat session */
  session: CombatSession;
  /** Formatted duration string (e.g., "2m 35s") */
  durationFormatted: string;
  /** All participant metrics */
  participantMetrics: ParticipantMetrics[];
  /** Damage meter (sorted by damage dealt descending) */
  damageMeter: DamageMeterEntry[];
  /** Healing meter (sorted by healing done descending) */
  healingMeter: HealingMeterEntry[];
  /** Deaths in chronological order */
  deathTimeline: DeathTimelineEntry[];
  /** CC events in chronological order */
  ccTimeline: CCTimelineEntry[];
  /** Key events from the fight */
  keyEvents: KeyEvent[];
}

/**
 * Result from analyzing a log
 */
export interface AnalysisResult {
  /** All detected combat sessions */
  sessions: CombatSession[];
  /** Total events analyzed */
  totalEvents: number;
  /** Events included in sessions */
  sessionEvents: number;
  /** Events outside sessions (downtime) */
  downtimeEvents: number;
  /** Overall time span of the log */
  timeSpan: {
    start: Date;
    end: Date;
    durationMs: number;
  };
}
