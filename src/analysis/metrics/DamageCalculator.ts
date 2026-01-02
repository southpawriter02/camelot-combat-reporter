import type { CombatEvent, Entity, DamageEvent } from '../../types/index.js';
import { EventType, DamageType, ActionType } from '../../types/index.js';
import type {
  DamageMetrics,
  ActionBreakdown,
  DamageTypeBreakdown,
  TargetBreakdown,
  SourceBreakdown,
  CriticalStats,
  MetricsConfig,
  CombatSession,
} from '../types/index.js';
import { DEFAULT_METRICS_CONFIG } from '../types/index.js';
import { DPSCalculator } from './DPSCalculator.js';

/**
 * Calculates damage metrics from combat events
 */
export class DamageCalculator {
  private config: MetricsConfig;
  private dpsCalculator: DPSCalculator;

  constructor(config: Partial<MetricsConfig> = {}) {
    this.config = { ...DEFAULT_METRICS_CONFIG, ...config };
    this.dpsCalculator = new DPSCalculator(this.config.rollingWindowMs);
  }

  /**
   * Calculate damage metrics for all entities in a session
   */
  calculate(session: CombatSession): Map<string, DamageMetrics> {
    const metricsMap = new Map<string, DamageMetrics>();

    for (const participant of session.participants) {
      const metrics = this.calculateForEntity(
        session.events,
        participant.entity,
        session.durationMs
      );
      metricsMap.set(participant.entity.name, metrics);
    }

    return metricsMap;
  }

  /**
   * Calculate damage metrics for a specific entity
   */
  calculateForEntity(
    events: CombatEvent[],
    entity: Entity,
    durationMs: number
  ): DamageMetrics {
    // Filter damage events involving this entity
    const damageDealtEvents = events.filter(
      (e): e is DamageEvent =>
        e.eventType === EventType.DAMAGE_DEALT &&
        'source' in e &&
        e.source?.name === entity.name
    );

    const damageReceivedEvents = events.filter(
      (e): e is DamageEvent =>
        e.eventType === EventType.DAMAGE_RECEIVED &&
        'target' in e &&
        e.target?.name === entity.name
    );

    // Calculate totals
    const totalDealt = damageDealtEvents.reduce((sum, e) => sum + e.amount, 0);
    const effectiveDealt = damageDealtEvents.reduce((sum, e) => sum + e.effectiveAmount, 0);
    const totalTaken = damageReceivedEvents.reduce((sum, e) => sum + e.amount, 0);
    const effectiveTaken = damageReceivedEvents.reduce((sum, e) => sum + e.effectiveAmount, 0);

    // Calculate DPS/DTPS
    const dps = this.dpsCalculator.calculateAverageDPS(damageDealtEvents, durationMs);
    const dtps = this.dpsCalculator.calculateAverageDPS(damageReceivedEvents, durationMs);
    const peakDpsResult = this.dpsCalculator.calculatePeakDPS(damageDealtEvents);

    // Calculate breakdowns
    const byAction = this.calculateActionBreakdown(damageDealtEvents, totalDealt);
    const byDamageType = this.calculateDamageTypeBreakdown(damageDealtEvents, totalDealt);
    const byTarget = this.calculateTargetBreakdown(damageDealtEvents, totalDealt);
    const bySource = this.calculateSourceBreakdown(damageReceivedEvents, totalTaken);

    // Calculate critical stats
    const critStats = this.calculateCriticalStats(damageDealtEvents);

    return {
      entity,
      totalDealt,
      effectiveDealt,
      totalTaken,
      effectiveTaken,
      dps,
      dtps,
      peakDps: peakDpsResult.value,
      byAction,
      byDamageType,
      byTarget,
      bySource,
      critStats,
    };
  }

  /**
   * Calculate damage breakdown by action (spell/style)
   */
  private calculateActionBreakdown(
    events: DamageEvent[],
    totalDamage: number
  ): ActionBreakdown[] {
    const actionMap = new Map<
      string,
      { damage: number; hits: number; crits: number; actionType: ActionType }
    >();

    for (const event of events) {
      const actionName = event.actionName || 'Auto Attack';
      const existing = actionMap.get(actionName);

      if (existing) {
        existing.damage += event.effectiveAmount;
        existing.hits++;
        if (event.isCritical) existing.crits++;
      } else {
        actionMap.set(actionName, {
          damage: event.effectiveAmount,
          hits: 1,
          crits: event.isCritical ? 1 : 0,
          actionType: event.actionType,
        });
      }
    }

    const breakdown: ActionBreakdown[] = [];
    for (const [actionName, data] of actionMap) {
      breakdown.push({
        actionName,
        actionType: data.actionType,
        totalDamage: data.damage,
        hitCount: data.hits,
        averageDamage: data.hits > 0 ? data.damage / data.hits : 0,
        critCount: data.crits,
        critRate: data.hits > 0 ? data.crits / data.hits : 0,
        percentage: totalDamage > 0 ? data.damage / totalDamage : 0,
      });
    }

    // Sort by total damage descending
    return breakdown.sort((a, b) => b.totalDamage - a.totalDamage);
  }

  /**
   * Calculate damage breakdown by damage type
   */
  private calculateDamageTypeBreakdown(
    events: DamageEvent[],
    totalDamage: number
  ): DamageTypeBreakdown[] {
    const typeMap = new Map<DamageType, { damage: number; hits: number }>();

    for (const event of events) {
      const existing = typeMap.get(event.damageType);
      if (existing) {
        existing.damage += event.effectiveAmount;
        existing.hits++;
      } else {
        typeMap.set(event.damageType, {
          damage: event.effectiveAmount,
          hits: 1,
        });
      }
    }

    const breakdown: DamageTypeBreakdown[] = [];
    for (const [damageType, data] of typeMap) {
      breakdown.push({
        damageType,
        totalDamage: data.damage,
        hitCount: data.hits,
        percentage: totalDamage > 0 ? data.damage / totalDamage : 0,
      });
    }

    return breakdown.sort((a, b) => b.totalDamage - a.totalDamage);
  }

  /**
   * Calculate damage breakdown by target
   */
  private calculateTargetBreakdown(
    events: DamageEvent[],
    totalDamage: number
  ): TargetBreakdown[] {
    const targetMap = new Map<string, { target: Entity; damage: number; hits: number }>();

    for (const event of events) {
      const targetName = event.target.name;
      const existing = targetMap.get(targetName);

      if (existing) {
        existing.damage += event.effectiveAmount;
        existing.hits++;
      } else {
        targetMap.set(targetName, {
          target: event.target,
          damage: event.effectiveAmount,
          hits: 1,
        });
      }
    }

    const breakdown: TargetBreakdown[] = [];
    for (const [, data] of targetMap) {
      breakdown.push({
        target: data.target,
        totalDamage: data.damage,
        hitCount: data.hits,
        percentage: totalDamage > 0 ? data.damage / totalDamage : 0,
      });
    }

    return breakdown.sort((a, b) => b.totalDamage - a.totalDamage);
  }

  /**
   * Calculate damage breakdown by source (for damage taken)
   */
  private calculateSourceBreakdown(
    events: DamageEvent[],
    totalDamage: number
  ): SourceBreakdown[] {
    const sourceMap = new Map<string, { source: Entity; damage: number; hits: number }>();

    for (const event of events) {
      const sourceName = event.source.name;
      const existing = sourceMap.get(sourceName);

      if (existing) {
        existing.damage += event.effectiveAmount;
        existing.hits++;
      } else {
        sourceMap.set(sourceName, {
          source: event.source,
          damage: event.effectiveAmount,
          hits: 1,
        });
      }
    }

    const breakdown: SourceBreakdown[] = [];
    for (const [, data] of sourceMap) {
      breakdown.push({
        source: data.source,
        totalDamage: data.damage,
        hitCount: data.hits,
        percentage: totalDamage > 0 ? data.damage / totalDamage : 0,
      });
    }

    return breakdown.sort((a, b) => b.totalDamage - a.totalDamage);
  }

  /**
   * Calculate critical hit statistics
   */
  private calculateCriticalStats(events: DamageEvent[]): CriticalStats {
    const crits = events.filter((e) => e.isCritical);
    const totalCrits = crits.length;
    const totalHits = events.length;
    const totalCritAmount = crits.reduce((sum, e) => sum + e.effectiveAmount, 0);

    return {
      totalCrits,
      totalHits,
      critRate: totalHits > 0 ? totalCrits / totalHits : 0,
      totalCritAmount,
      averageCritAmount: totalCrits > 0 ? totalCritAmount / totalCrits : 0,
    };
  }
}
