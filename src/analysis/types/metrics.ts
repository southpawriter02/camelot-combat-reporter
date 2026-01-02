import type { Entity, DamageType, ActionType } from '../../types/index.js';
import type { ParticipantRole } from './session.js';

/**
 * Critical hit statistics
 */
export interface CriticalStats {
  /** Total critical hits */
  totalCrits: number;
  /** Total hits (including non-crits) */
  totalHits: number;
  /** Critical hit rate (0-1) */
  critRate: number;
  /** Total damage/healing from criticals */
  totalCritAmount: number;
  /** Average critical damage/healing */
  averageCritAmount: number;
}

/**
 * Damage breakdown by action (spell/style)
 */
export interface ActionBreakdown {
  /** Name of the action (spell/style name) */
  actionName: string;
  /** Type of action */
  actionType: ActionType;
  /** Total damage from this action */
  totalDamage: number;
  /** Number of hits */
  hitCount: number;
  /** Average damage per hit */
  averageDamage: number;
  /** Number of critical hits */
  critCount: number;
  /** Critical hit rate for this action */
  critRate: number;
  /** Percentage of total damage */
  percentage: number;
}

/**
 * Damage breakdown by damage type
 */
export interface DamageTypeBreakdown {
  /** Type of damage */
  damageType: DamageType;
  /** Total damage of this type */
  totalDamage: number;
  /** Number of hits */
  hitCount: number;
  /** Percentage of total damage */
  percentage: number;
}

/**
 * Damage breakdown by target
 */
export interface TargetBreakdown {
  /** Target entity */
  target: Entity;
  /** Total damage to this target */
  totalDamage: number;
  /** Number of hits */
  hitCount: number;
  /** Percentage of total damage */
  percentage: number;
}

/**
 * Damage breakdown by source (for damage taken)
 */
export interface SourceBreakdown {
  /** Source entity */
  source: Entity;
  /** Total damage from this source */
  totalDamage: number;
  /** Number of hits */
  hitCount: number;
  /** Percentage of total damage */
  percentage: number;
}

/**
 * Comprehensive damage metrics for an entity
 */
export interface DamageMetrics {
  /** Entity these metrics are for */
  entity: Entity;
  /** Total damage dealt */
  totalDealt: number;
  /** Total effective damage dealt (minus absorbed) */
  effectiveDealt: number;
  /** Total damage taken */
  totalTaken: number;
  /** Total effective damage taken */
  effectiveTaken: number;
  /** Damage per second (average over fight duration) */
  dps: number;
  /** Damage taken per second */
  dtps: number;
  /** Peak DPS in any rolling window */
  peakDps: number;
  /** Breakdown by spell/style name */
  byAction: ActionBreakdown[];
  /** Breakdown by damage type */
  byDamageType: DamageTypeBreakdown[];
  /** Breakdown by target */
  byTarget: TargetBreakdown[];
  /** Breakdown by source (for damage taken) */
  bySource: SourceBreakdown[];
  /** Critical hit statistics */
  critStats: CriticalStats;
}

/**
 * Healing breakdown by spell
 */
export interface SpellHealingBreakdown {
  /** Name of the healing spell */
  spellName: string;
  /** Total healing from this spell */
  totalHealing: number;
  /** Effective healing (minus overheal) */
  effectiveHealing: number;
  /** Total overheal */
  overheal: number;
  /** Number of casts */
  castCount: number;
  /** Average healing per cast */
  averageHealing: number;
  /** Number of critical heals */
  critCount: number;
  /** Critical heal rate */
  critRate: number;
  /** Percentage of total healing */
  percentage: number;
}

/**
 * Healing breakdown by target
 */
export interface HealingTargetBreakdown {
  /** Target entity */
  target: Entity;
  /** Total healing to this target */
  totalHealing: number;
  /** Effective healing */
  effectiveHealing: number;
  /** Percentage of total healing */
  percentage: number;
}

/**
 * Healing breakdown by source
 */
export interface HealingSourceBreakdown {
  /** Source entity */
  source: Entity;
  /** Total healing from this source */
  totalHealing: number;
  /** Effective healing */
  effectiveHealing: number;
  /** Percentage of total healing */
  percentage: number;
}

/**
 * Comprehensive healing metrics for an entity
 */
export interface HealingMetrics {
  /** Entity these metrics are for */
  entity: Entity;
  /** Total healing done */
  totalDone: number;
  /** Total effective healing (minus overheal) */
  effectiveDone: number;
  /** Total overheal */
  totalOverheal: number;
  /** Overheal percentage (0-1) */
  overhealRate: number;
  /** Total healing received */
  totalReceived: number;
  /** Effective healing received */
  effectiveReceived: number;
  /** Healing per second */
  hps: number;
  /** Peak HPS in any rolling window */
  peakHps: number;
  /** Breakdown by spell name */
  bySpell: SpellHealingBreakdown[];
  /** Breakdown by target */
  byTarget: HealingTargetBreakdown[];
  /** Breakdown by source (for healing received) */
  bySource: HealingSourceBreakdown[];
  /** Critical heal statistics */
  critStats: CriticalStats;
}

/**
 * Combined metrics for a participant
 */
export interface ParticipantMetrics {
  /** The entity */
  entity: Entity;
  /** Role in the fight */
  role: ParticipantRole;
  /** Damage metrics */
  damage: DamageMetrics;
  /** Healing metrics */
  healing: HealingMetrics;
}
