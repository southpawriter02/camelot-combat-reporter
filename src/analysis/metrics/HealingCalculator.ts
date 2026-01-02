import type { CombatEvent, Entity, HealingEvent } from '../../types/index.js';
import { EventType } from '../../types/index.js';
import type {
  HealingMetrics,
  SpellHealingBreakdown,
  HealingTargetBreakdown,
  HealingSourceBreakdown,
  CriticalStats,
  MetricsConfig,
  CombatSession,
} from '../types/index.js';
import { DEFAULT_METRICS_CONFIG } from '../types/index.js';
import { DPSCalculator } from './DPSCalculator.js';

/**
 * Calculates healing metrics from combat events
 */
export class HealingCalculator {
  private config: MetricsConfig;
  private dpsCalculator: DPSCalculator;

  constructor(config: Partial<MetricsConfig> = {}) {
    this.config = { ...DEFAULT_METRICS_CONFIG, ...config };
    this.dpsCalculator = new DPSCalculator(this.config.rollingWindowMs);
  }

  /**
   * Calculate healing metrics for all entities in a session
   */
  calculate(session: CombatSession): Map<string, HealingMetrics> {
    const metricsMap = new Map<string, HealingMetrics>();

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
   * Calculate healing metrics for a specific entity
   */
  calculateForEntity(
    events: CombatEvent[],
    entity: Entity,
    durationMs: number
  ): HealingMetrics {
    // Filter healing events involving this entity
    const healingDoneEvents = events.filter(
      (e): e is HealingEvent =>
        e.eventType === EventType.HEALING_DONE &&
        'source' in e &&
        e.source?.name === entity.name
    );

    const healingReceivedEvents = events.filter(
      (e): e is HealingEvent =>
        e.eventType === EventType.HEALING_RECEIVED &&
        'target' in e &&
        e.target?.name === entity.name
    );

    // Calculate totals
    const totalDone = healingDoneEvents.reduce((sum, e) => sum + e.amount, 0);
    const effectiveDone = healingDoneEvents.reduce((sum, e) => sum + e.effectiveAmount, 0);
    const totalOverheal = healingDoneEvents.reduce((sum, e) => sum + e.overheal, 0);
    const totalReceived = healingReceivedEvents.reduce((sum, e) => sum + e.amount, 0);
    const effectiveReceived = healingReceivedEvents.reduce(
      (sum, e) => sum + e.effectiveAmount,
      0
    );

    // Calculate HPS
    const hps = this.dpsCalculator.calculateAverageHPS(healingDoneEvents, durationMs);
    const peakHpsResult = this.dpsCalculator.calculatePeakHPS(healingDoneEvents);

    // Calculate breakdowns
    const bySpell = this.calculateSpellBreakdown(healingDoneEvents, totalDone);
    const byTarget = this.calculateTargetBreakdown(healingDoneEvents, totalDone);
    const bySource = this.calculateSourceBreakdown(healingReceivedEvents, totalReceived);

    // Calculate critical stats
    const critStats = this.calculateCriticalStats(healingDoneEvents);

    return {
      entity,
      totalDone,
      effectiveDone,
      totalOverheal,
      overhealRate: totalDone > 0 ? totalOverheal / totalDone : 0,
      totalReceived,
      effectiveReceived,
      hps,
      peakHps: peakHpsResult.value,
      bySpell,
      byTarget,
      bySource,
      critStats,
    };
  }

  /**
   * Calculate healing breakdown by spell
   */
  private calculateSpellBreakdown(
    events: HealingEvent[],
    totalHealing: number
  ): SpellHealingBreakdown[] {
    const spellMap = new Map<
      string,
      {
        total: number;
        effective: number;
        overheal: number;
        casts: number;
        crits: number;
      }
    >();

    for (const event of events) {
      const spellName = event.spellName;
      const existing = spellMap.get(spellName);

      if (existing) {
        existing.total += event.amount;
        existing.effective += event.effectiveAmount;
        existing.overheal += event.overheal;
        existing.casts++;
        if (event.isCritical) existing.crits++;
      } else {
        spellMap.set(spellName, {
          total: event.amount,
          effective: event.effectiveAmount,
          overheal: event.overheal,
          casts: 1,
          crits: event.isCritical ? 1 : 0,
        });
      }
    }

    const breakdown: SpellHealingBreakdown[] = [];
    for (const [spellName, data] of spellMap) {
      breakdown.push({
        spellName,
        totalHealing: data.total,
        effectiveHealing: data.effective,
        overheal: data.overheal,
        castCount: data.casts,
        averageHealing: data.casts > 0 ? data.total / data.casts : 0,
        critCount: data.crits,
        critRate: data.casts > 0 ? data.crits / data.casts : 0,
        percentage: totalHealing > 0 ? data.total / totalHealing : 0,
      });
    }

    return breakdown.sort((a, b) => b.totalHealing - a.totalHealing);
  }

  /**
   * Calculate healing breakdown by target
   */
  private calculateTargetBreakdown(
    events: HealingEvent[],
    totalHealing: number
  ): HealingTargetBreakdown[] {
    const targetMap = new Map<
      string,
      { target: Entity; total: number; effective: number }
    >();

    for (const event of events) {
      const targetName = event.target.name;
      const existing = targetMap.get(targetName);

      if (existing) {
        existing.total += event.amount;
        existing.effective += event.effectiveAmount;
      } else {
        targetMap.set(targetName, {
          target: event.target,
          total: event.amount,
          effective: event.effectiveAmount,
        });
      }
    }

    const breakdown: HealingTargetBreakdown[] = [];
    for (const [, data] of targetMap) {
      breakdown.push({
        target: data.target,
        totalHealing: data.total,
        effectiveHealing: data.effective,
        percentage: totalHealing > 0 ? data.total / totalHealing : 0,
      });
    }

    return breakdown.sort((a, b) => b.totalHealing - a.totalHealing);
  }

  /**
   * Calculate healing breakdown by source (for healing received)
   */
  private calculateSourceBreakdown(
    events: HealingEvent[],
    totalHealing: number
  ): HealingSourceBreakdown[] {
    const sourceMap = new Map<
      string,
      { source: Entity; total: number; effective: number }
    >();

    for (const event of events) {
      const sourceName = event.source.name;
      const existing = sourceMap.get(sourceName);

      if (existing) {
        existing.total += event.amount;
        existing.effective += event.effectiveAmount;
      } else {
        sourceMap.set(sourceName, {
          source: event.source,
          total: event.amount,
          effective: event.effectiveAmount,
        });
      }
    }

    const breakdown: HealingSourceBreakdown[] = [];
    for (const [, data] of sourceMap) {
      breakdown.push({
        source: data.source,
        totalHealing: data.total,
        effectiveHealing: data.effective,
        percentage: totalHealing > 0 ? data.total / totalHealing : 0,
      });
    }

    return breakdown.sort((a, b) => b.totalHealing - a.totalHealing);
  }

  /**
   * Calculate critical heal statistics
   */
  private calculateCriticalStats(events: HealingEvent[]): CriticalStats {
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
