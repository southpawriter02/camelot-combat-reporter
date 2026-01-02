import type {
  CombatEvent,
  DamageEvent,
  HealingEvent,
  DeathEvent,
  CrowdControlEvent,
  Entity,
} from '../../types/index.js';
import { EventType } from '../../types/index.js';
import type {
  CombatSession,
  FightSummary,
  DamageMeterEntry,
  HealingMeterEntry,
  DeathTimelineEntry,
  CCTimelineEntry,
  ParticipantMetrics,
  MetricsConfig,
} from '../types/index.js';
import { DEFAULT_METRICS_CONFIG } from '../types/index.js';
import { DamageCalculator } from '../metrics/DamageCalculator.js';
import { HealingCalculator } from '../metrics/HealingCalculator.js';
import { KeyEventDetector, type KeyEventConfig, DEFAULT_KEY_EVENT_CONFIG } from './KeyEventDetector.js';
import { formatDuration, getRelativeTime } from '../utils/timeUtils.js';

/**
 * Configuration for fight summarizer
 */
export interface FightSummarizerConfig {
  metrics: MetricsConfig;
  keyEvents: KeyEventConfig;
  /** Number of last damage events to include in death timeline */
  lastDamageEventsCount: number;
}

/**
 * Default configuration
 */
export const DEFAULT_FIGHT_SUMMARIZER_CONFIG: FightSummarizerConfig = {
  metrics: DEFAULT_METRICS_CONFIG,
  keyEvents: DEFAULT_KEY_EVENT_CONFIG,
  lastDamageEventsCount: 5,
};

/**
 * Creates comprehensive fight summaries from combat sessions
 */
export class FightSummarizer {
  private config: FightSummarizerConfig;
  private damageCalculator: DamageCalculator;
  private healingCalculator: HealingCalculator;
  private keyEventDetector: KeyEventDetector;

  constructor(config: Partial<FightSummarizerConfig> = {}) {
    this.config = {
      ...DEFAULT_FIGHT_SUMMARIZER_CONFIG,
      ...config,
      metrics: { ...DEFAULT_FIGHT_SUMMARIZER_CONFIG.metrics, ...config.metrics },
      keyEvents: { ...DEFAULT_FIGHT_SUMMARIZER_CONFIG.keyEvents, ...config.keyEvents },
    };

    this.damageCalculator = new DamageCalculator(this.config.metrics);
    this.healingCalculator = new HealingCalculator(this.config.metrics);
    this.keyEventDetector = new KeyEventDetector(this.config.keyEvents);
  }

  /**
   * Create a complete fight summary from a combat session
   */
  summarize(session: CombatSession): FightSummary {
    // Calculate metrics for all participants
    const damageMetricsMap = this.damageCalculator.calculate(session);
    const healingMetricsMap = this.healingCalculator.calculate(session);

    // Build participant metrics
    const participantMetrics: ParticipantMetrics[] = [];
    for (const participant of session.participants) {
      const entityName = participant.entity.name;
      const damageMetrics = damageMetricsMap.get(entityName);
      const healingMetrics = healingMetricsMap.get(entityName);

      if (damageMetrics && healingMetrics) {
        participantMetrics.push({
          entity: participant.entity,
          role: participant.role,
          damage: damageMetrics,
          healing: healingMetrics,
        });
      }
    }

    // Build meters
    const damageMeter = this.buildDamageMeter(participantMetrics);
    const healingMeter = this.buildHealingMeter(participantMetrics);

    // Build timelines
    const deathTimeline = this.buildDeathTimeline(session);
    const ccTimeline = this.buildCCTimeline(session);

    // Detect key events
    const keyEvents = this.keyEventDetector.detect(session.events, session.startTime);

    return {
      session,
      durationFormatted: formatDuration(session.durationMs),
      participantMetrics,
      damageMeter,
      healingMeter,
      deathTimeline,
      ccTimeline,
      keyEvents,
    };
  }

  /**
   * Build the damage meter from participant metrics
   */
  private buildDamageMeter(metrics: ParticipantMetrics[]): DamageMeterEntry[] {
    // Calculate total group damage
    const totalGroupDamage = metrics.reduce(
      (sum, m) => sum + m.damage.totalDealt,
      0
    );

    // Build entries
    const entries: DamageMeterEntry[] = metrics
      .filter((m) => m.damage.totalDealt > 0)
      .map((m) => ({
        entity: m.entity,
        totalDamage: m.damage.totalDealt,
        dps: m.damage.dps,
        percentage: totalGroupDamage > 0 ? m.damage.totalDealt / totalGroupDamage : 0,
        rank: 0, // Will be set after sorting
      }))
      .sort((a, b) => b.totalDamage - a.totalDamage);

    // Assign ranks
    entries.forEach((entry, index) => {
      entry.rank = index + 1;
    });

    return entries;
  }

  /**
   * Build the healing meter from participant metrics
   */
  private buildHealingMeter(metrics: ParticipantMetrics[]): HealingMeterEntry[] {
    // Calculate total group healing
    const totalGroupHealing = metrics.reduce(
      (sum, m) => sum + m.healing.totalDone,
      0
    );

    // Build entries
    const entries: HealingMeterEntry[] = metrics
      .filter((m) => m.healing.totalDone > 0)
      .map((m) => ({
        entity: m.entity,
        totalHealing: m.healing.totalDone,
        effectiveHealing: m.healing.effectiveDone,
        hps: m.healing.hps,
        overhealRate: m.healing.overhealRate,
        percentage: totalGroupHealing > 0 ? m.healing.totalDone / totalGroupHealing : 0,
        rank: 0, // Will be set after sorting
      }))
      .sort((a, b) => b.totalHealing - a.totalHealing);

    // Assign ranks
    entries.forEach((entry, index) => {
      entry.rank = index + 1;
    });

    return entries;
  }

  /**
   * Build the death timeline from session events
   */
  private buildDeathTimeline(session: CombatSession): DeathTimelineEntry[] {
    const deathEvents = session.events.filter(
      (e): e is DeathEvent => e.eventType === EventType.DEATH
    );

    return deathEvents.map((deathEvent) => {
      // Find last damage events before this death
      const lastDamageEvents = this.findLastDamageEvents(
        session.events,
        deathEvent.target,
        deathEvent.timestamp,
        this.config.lastDamageEventsCount
      );

      return {
        timestamp: deathEvent.timestamp,
        relativeTimeMs: getRelativeTime(deathEvent.timestamp, session.startTime),
        target: deathEvent.target,
        killer: deathEvent.killer,
        lastDamageEvents,
      };
    });
  }

  /**
   * Find the last N damage events before a death
   */
  private findLastDamageEvents(
    events: CombatEvent[],
    target: Entity,
    beforeTime: Date,
    count: number
  ): CombatEvent[] {
    const damageToTarget = events.filter(
      (e): e is DamageEvent =>
        e.eventType === EventType.DAMAGE_RECEIVED &&
        'target' in e &&
        (e as DamageEvent).target.name === target.name &&
        e.timestamp.getTime() < beforeTime.getTime()
    );

    // Sort by timestamp descending and take the last N
    return damageToTarget
      .sort((a, b) => b.timestamp.getTime() - a.timestamp.getTime())
      .slice(0, count)
      .reverse(); // Reverse to chronological order
  }

  /**
   * Build the CC timeline from session events
   */
  private buildCCTimeline(session: CombatSession): CCTimelineEntry[] {
    const ccEvents = session.events.filter(
      (e): e is CrowdControlEvent => e.eventType === EventType.CROWD_CONTROL
    );

    return ccEvents.map((ccEvent) => ({
      timestamp: ccEvent.timestamp,
      relativeTimeMs: getRelativeTime(ccEvent.timestamp, session.startTime),
      source: ccEvent.source,
      target: ccEvent.target,
      effect: ccEvent.effect,
      duration: ccEvent.duration,
      wasResisted: ccEvent.isResisted,
    }));
  }

  /**
   * Get a condensed damage report for a session
   */
  getDamageReport(session: CombatSession): string {
    const summary = this.summarize(session);
    const lines: string[] = [];

    lines.push(`=== Fight Summary (${summary.durationFormatted}) ===`);
    lines.push('');
    lines.push('Damage Done:');

    for (const entry of summary.damageMeter) {
      const pct = (entry.percentage * 100).toFixed(1);
      lines.push(
        `  ${entry.rank}. ${entry.entity.name}: ${entry.totalDamage} (${entry.dps.toFixed(1)} DPS, ${pct}%)`
      );
    }

    if (summary.healingMeter.length > 0) {
      lines.push('');
      lines.push('Healing Done:');

      for (const entry of summary.healingMeter) {
        const pct = (entry.percentage * 100).toFixed(1);
        const overheal = (entry.overhealRate * 100).toFixed(1);
        lines.push(
          `  ${entry.rank}. ${entry.entity.name}: ${entry.totalHealing} (${entry.hps.toFixed(1)} HPS, ${pct}%, ${overheal}% overheal)`
        );
      }
    }

    if (summary.deathTimeline.length > 0) {
      lines.push('');
      lines.push('Deaths:');

      for (const death of summary.deathTimeline) {
        const killerInfo = death.killer ? ` by ${death.killer.name}` : '';
        lines.push(`  - ${death.target.name}${killerInfo}`);
      }
    }

    return lines.join('\n');
  }
}
