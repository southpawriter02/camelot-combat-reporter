/**
 * Behavior Feature Extraction
 *
 * Extracts behavioral patterns from combat events to classify playstyle.
 * Analyzes action distribution, target selection, damage patterns, and more.
 */

import type { CombatEvent, Entity, DamageEvent, HealingEvent, CrowdControlEvent, DeathEvent } from '../../types/index.js';
import { EventType, ActionType } from '../../types/index.js';
import type { BehaviorFeatures } from '../types.js';
import { BIG_HIT_THRESHOLD, ROLLING_WINDOW_MS } from '../config.js';

/**
 * Extracts behavior features from combat events
 */
export class BehaviorFeaturesExtractor {
  /**
   * Extract behavior features from events
   *
   * @param events - Combat events to analyze
   * @param selfEntity - The player we're extracting features for
   * @param durationMs - Duration of the fight in milliseconds
   * @returns Behavior features
   */
  extract(events: CombatEvent[], selfEntity: Entity, durationMs: number): BehaviorFeatures {
    const selfName = selfEntity.name;
    const durationMinutes = Math.max(durationMs / 60000, 0.001);
    const durationSeconds = Math.max(durationMs / 1000, 0.001);

    // Filter self damage events
    const selfDamageEvents = events.filter(
      (e): e is DamageEvent =>
        (e.eventType === EventType.DAMAGE_DEALT || e.eventType === EventType.DAMAGE_RECEIVED) &&
        (e as DamageEvent).source?.name === selfName
    );

    // Filter self healing events
    const selfHealingEvents = events.filter(
      (e): e is HealingEvent =>
        (e.eventType === EventType.HEALING_DONE || e.eventType === EventType.HEALING_RECEIVED) &&
        (e as HealingEvent).source?.name === selfName
    );

    // Filter incoming damage events
    const incomingDamageEvents = events.filter(
      (e): e is DamageEvent =>
        (e.eventType === EventType.DAMAGE_DEALT || e.eventType === EventType.DAMAGE_RECEIVED) &&
        (e as DamageEvent).target?.name === selfName
    );

    // Action distribution
    const actionCounts = this.countActions(selfDamageEvents);
    const totalActions = selfDamageEvents.length || 1;
    const meleeRatio = (actionCounts.melee ?? 0) / totalActions;
    const spellRatio = (actionCounts.spell ?? 0) / totalActions;
    const styleRatio = (actionCounts.style ?? 0) / totalActions;
    const procRatio = (actionCounts.proc ?? 0) / totalActions;

    // Target selection analysis
    const targetAnalysis = this.analyzeTargetSelection(selfDamageEvents);
    const targetSwitchFrequency = targetAnalysis.switchCount / durationMinutes;
    const targetFocusScore = targetAnalysis.focusScore;
    const uniqueTargetsAttacked = targetAnalysis.uniqueTargets;

    // Low health target preference (heuristic: attacks after recent deaths)
    const lowHealthTargetPreference = this.estimateLowHealthPreference(events, selfName);

    // Damage patterns
    const damagePatterns = this.analyzeDamagePatterns(selfDamageEvents, durationMs);
    const burstDamageFrequency = damagePatterns.burstCount / durationMinutes;
    const sustainedDamageRatio = damagePatterns.sustainedRatio;
    const peakDps = damagePatterns.peakDps;
    const damageVariance = damagePatterns.variance;

    // Defensive behavior
    const defensiveAnalysis = this.analyzeDefensiveBehavior(
      selfHealingEvents,
      incomingDamageEvents,
      selfDamageEvents
    );
    const defensiveAbilityUsage = defensiveAnalysis.defensiveRatio;
    const selfHealingRatio = defensiveAnalysis.selfHealRatio;
    const mitigationRatio = defensiveAnalysis.mitigationRatio;
    const avgTimeBetweenHits = this.calculateAvgTimeBetweenHits(incomingDamageEvents);

    // Reaction patterns
    const reactionAnalysis = this.analyzeReactionPatterns(events, selfName);
    const avgReactionTimeMs = reactionAnalysis.avgReactionTime;
    const ccReactionTimeMs = reactionAnalysis.ccReactionTime;

    // Kill/assist analysis
    const killAssistAnalysis = this.analyzeKillsAndAssists(events, selfName);
    const killConfirmRate = killAssistAnalysis.killConfirmRate;
    const assistRate = killAssistAnalysis.assistRate;

    // Composite scores
    const totalSelfDamage = selfDamageEvents.reduce(
      (sum, e) => sum + (e.effectiveAmount ?? e.amount ?? 0),
      0
    );
    const totalSelfHealing = selfHealingEvents.reduce(
      (sum, e) => sum + (e.effectiveAmount ?? e.amount ?? 0),
      0
    );
    const totalIncomingDamage = incomingDamageEvents.reduce(
      (sum, e) => sum + (e.effectiveAmount ?? e.amount ?? 0),
      0
    );

    const aggressionScore = this.calculateAggressionScore({
      damageDealt: totalSelfDamage,
      damageTaken: totalIncomingDamage,
      targetSwitchFrequency,
      burstDamageFrequency,
      selfHealingRatio,
    });

    const survivabilityScore = this.calculateSurvivabilityScore({
      damageTaken: totalIncomingDamage,
      healingReceived: totalSelfHealing,
      mitigationRatio,
      selfHealingRatio,
      durationMs,
    });

    const teamPlayScore = this.calculateTeamPlayScore({
      assistRate,
      healingToOthers: this.countHealingToOthers(selfHealingEvents, selfName),
      focusScore: targetFocusScore,
    });

    const efficiencyScore = this.calculateEfficiencyScore({
      damageDealt: totalSelfDamage,
      damageTaken: totalIncomingDamage,
      healingDone: totalSelfHealing,
      killConfirmRate,
      durationMs,
    });

    return {
      // Action distribution
      meleeRatio,
      spellRatio,
      styleRatio,
      procRatio,

      // Target selection
      targetSwitchFrequency,
      targetFocusScore,
      lowHealthTargetPreference,
      uniqueTargetsAttacked,

      // Damage patterns
      burstDamageFrequency,
      sustainedDamageRatio,
      peakDps,
      damageVariance,

      // Defensive behavior
      defensiveAbilityUsage,
      selfHealingRatio,
      avgTimeBetweenHits,
      mitigationRatio,

      // Reaction patterns
      avgReactionTimeMs,
      ccReactionTimeMs,
      killConfirmRate,
      assistRate,

      // Composite scores
      aggressionScore,
      survivabilityScore,
      teamPlayScore,
      efficiencyScore,
    };
  }

  /**
   * Count actions by type
   */
  private countActions(events: DamageEvent[]): Record<string, number> {
    const counts = { melee: 0, spell: 0, style: 0, proc: 0, other: 0 };

    for (const event of events) {
      switch (event.actionType) {
        case ActionType.MELEE:
          counts.melee++;
          break;
        case ActionType.SPELL:
          counts.spell++;
          break;
        case ActionType.STYLE:
          counts.style++;
          break;
        case ActionType.PROC:
        case ActionType.DOT:
          counts.proc++;
          break;
        default:
          counts.other++;
      }
    }

    return counts;
  }

  /**
   * Analyze target selection patterns
   */
  private analyzeTargetSelection(events: DamageEvent[]): {
    switchCount: number;
    focusScore: number;
    uniqueTargets: number;
  } {
    if (events.length === 0) {
      return { switchCount: 0, focusScore: 1, uniqueTargets: 0 };
    }

    const targetDamage = new Map<string, number>();
    let lastTarget: string | null = null;
    let switchCount = 0;

    for (const event of events) {
      const targetName = event.target?.name;
      if (!targetName) continue;

      // Count target switches
      if (lastTarget !== null && lastTarget !== targetName) {
        switchCount++;
      }
      lastTarget = targetName;

      // Accumulate damage per target
      const current = targetDamage.get(targetName) || 0;
      targetDamage.set(targetName, current + (event.effectiveAmount ?? event.amount ?? 0));
    }

    // Calculate focus score (damage concentration on primary target)
    const damages = Array.from(targetDamage.values());
    const totalDamage = damages.reduce((a, b) => a + b, 0);
    const maxDamage = Math.max(...damages, 0);
    const focusScore = totalDamage > 0 ? maxDamage / totalDamage : 1;

    return {
      switchCount,
      focusScore,
      uniqueTargets: targetDamage.size,
    };
  }

  /**
   * Estimate preference for low-health targets (heuristic)
   */
  private estimateLowHealthPreference(events: CombatEvent[], selfName: string): number {
    // This is a heuristic: look for damage dealt shortly after deaths
    // indicating the player finished off weakened targets
    const deathEvents = events.filter(
      (e): e is DeathEvent => e.eventType === EventType.DEATH
    );

    if (deathEvents.length === 0) return 0;

    let finishingBlowCount = 0;
    for (const death of deathEvents) {
      if (death.killer?.name === selfName) {
        finishingBlowCount++;
      }
    }

    // Normalize by total deaths (targets that died)
    return Math.min(finishingBlowCount / deathEvents.length, 1);
  }

  /**
   * Analyze damage patterns (burst vs sustained)
   */
  private analyzeDamagePatterns(
    events: DamageEvent[],
    durationMs: number
  ): {
    burstCount: number;
    sustainedRatio: number;
    peakDps: number;
    variance: number;
  } {
    if (events.length === 0) {
      return { burstCount: 0, sustainedRatio: 1, peakDps: 0, variance: 0 };
    }

    // Sort by timestamp
    const sorted = [...events].sort((a, b) => a.timestamp.getTime() - b.timestamp.getTime());

    // Count burst events (damage > threshold)
    const burstCount = sorted.filter(
      (e) => (e.effectiveAmount ?? e.amount ?? 0) >= BIG_HIT_THRESHOLD
    ).length;

    // Calculate peak DPS using rolling window
    let peakDps = 0;
    const windowMs = ROLLING_WINDOW_MS;

    for (let i = 0; i < sorted.length; i++) {
      const event = sorted[i];
      if (!event) continue;
      const windowStart = event.timestamp.getTime();
      const windowEnd = windowStart + windowMs;

      let windowDamage = 0;
      for (let j = i; j < sorted.length; j++) {
        const jEvent = sorted[j];
        if (!jEvent || jEvent.timestamp.getTime() > windowEnd) break;
        windowDamage += jEvent.effectiveAmount ?? jEvent.amount ?? 0;
      }

      const windowDps = windowDamage / (windowMs / 1000);
      peakDps = Math.max(peakDps, windowDps);
    }

    // Calculate damage variance
    const damages = sorted.map((e) => e.effectiveAmount ?? e.amount ?? 0);
    const avgDamage = damages.reduce((a, b) => a + b, 0) / damages.length;
    const variance =
      damages.reduce((sum, d) => sum + Math.pow(d - avgDamage, 2), 0) / damages.length;

    // Calculate sustained ratio (non-burst damage / total)
    const totalDamage = damages.reduce((a, b) => a + b, 0);
    const burstDamage = sorted
      .filter((e) => (e.effectiveAmount ?? e.amount ?? 0) >= BIG_HIT_THRESHOLD)
      .reduce((sum, e) => sum + (e.effectiveAmount ?? e.amount ?? 0), 0);
    const sustainedRatio = totalDamage > 0 ? 1 - burstDamage / totalDamage : 1;

    return { burstCount, sustainedRatio, peakDps, variance };
  }

  /**
   * Analyze defensive behavior
   */
  private analyzeDefensiveBehavior(
    selfHealingEvents: HealingEvent[],
    incomingDamageEvents: DamageEvent[],
    selfDamageEvents: DamageEvent[]
  ): {
    defensiveRatio: number;
    selfHealRatio: number;
    mitigationRatio: number;
  } {
    const totalActions = selfDamageEvents.length + selfHealingEvents.length || 1;
    const defensiveRatio = selfHealingEvents.length / totalActions;

    // Self-healing ratio
    const totalHealing = selfHealingEvents.reduce(
      (sum, e) => sum + (e.effectiveAmount ?? e.amount ?? 0),
      0
    );
    const totalDamageDealt = selfDamageEvents.reduce(
      (sum, e) => sum + (e.effectiveAmount ?? e.amount ?? 0),
      0
    );
    const selfHealRatio =
      totalHealing + totalDamageDealt > 0 ? totalHealing / (totalHealing + totalDamageDealt) : 0;

    // Mitigation ratio (blocked + parried + evaded / total incoming)
    const mitigatedCount = incomingDamageEvents.filter(
      (e) => e.isBlocked || e.isParried || e.isEvaded
    ).length;
    const totalIncoming = incomingDamageEvents.length || 1;
    const mitigationRatio = mitigatedCount / totalIncoming;

    return { defensiveRatio, selfHealRatio, mitigationRatio };
  }

  /**
   * Calculate average time between incoming hits
   */
  private calculateAvgTimeBetweenHits(events: DamageEvent[]): number {
    if (events.length < 2) return 0;

    const sorted = [...events].sort((a, b) => a.timestamp.getTime() - b.timestamp.getTime());
    let totalGap = 0;

    for (let i = 1; i < sorted.length; i++) {
      const curr = sorted[i];
      const prev = sorted[i - 1];
      if (curr && prev) {
        totalGap += curr.timestamp.getTime() - prev.timestamp.getTime();
      }
    }

    return totalGap / (sorted.length - 1);
  }

  /**
   * Analyze reaction patterns
   */
  private analyzeReactionPatterns(
    events: CombatEvent[],
    selfName: string
  ): {
    avgReactionTime: number;
    ccReactionTime: number;
  } {
    // This is a simplified heuristic
    // A more sophisticated approach would track specific response patterns

    // Sort events by timestamp
    const sorted = [...events].sort((a, b) => a.timestamp.getTime() - b.timestamp.getTime());

    // Find CC events affecting self and measure time to next action
    const ccEvents = sorted.filter(
      (e): e is CrowdControlEvent =>
        e.eventType === EventType.CROWD_CONTROL &&
        (e as CrowdControlEvent).target?.name === selfName
    );

    let totalCCReactionTime = 0;
    let ccReactionCount = 0;

    for (const cc of ccEvents) {
      const ccEndTime = cc.timestamp.getTime() + (cc.duration ?? 0) * 1000;

      // Find next action after CC
      const nextAction = sorted.find(
        (e) =>
          e.timestamp.getTime() > ccEndTime &&
          ((e.eventType === EventType.DAMAGE_DEALT &&
            (e as DamageEvent).source?.name === selfName) ||
            (e.eventType === EventType.HEALING_DONE &&
              (e as HealingEvent).source?.name === selfName))
      );

      if (nextAction) {
        totalCCReactionTime += nextAction.timestamp.getTime() - ccEndTime;
        ccReactionCount++;
      }
    }

    const ccReactionTime = ccReactionCount > 0 ? totalCCReactionTime / ccReactionCount : 500;

    // General reaction time (time between receiving damage and dealing damage)
    // This is a rough heuristic
    const avgReactionTime = 1000; // Default placeholder

    return { avgReactionTime, ccReactionTime };
  }

  /**
   * Analyze kills and assists
   */
  private analyzeKillsAndAssists(
    events: CombatEvent[],
    selfName: string
  ): {
    killConfirmRate: number;
    assistRate: number;
  } {
    const deathEvents = events.filter(
      (e): e is DeathEvent => e.eventType === EventType.DEATH
    );

    if (deathEvents.length === 0) {
      return { killConfirmRate: 0, assistRate: 0 };
    }

    let kills = 0;
    let assists = 0;

    for (const death of deathEvents) {
      const targetName = death.target?.name;
      if (!targetName) continue;

      if (death.killer?.name === selfName) {
        kills++;
      } else {
        // Check if self dealt damage to this target (assist)
        const dealtDamageToTarget = events.some(
          (e): e is DamageEvent =>
            (e.eventType === EventType.DAMAGE_DEALT ||
              e.eventType === EventType.DAMAGE_RECEIVED) &&
            (e as DamageEvent).source?.name === selfName &&
            (e as DamageEvent).target?.name === targetName
        );
        if (dealtDamageToTarget) {
          assists++;
        }
      }
    }

    // Kill confirm rate: kills / (kills + assists if eligible)
    const totalParticipation = kills + assists;
    const killConfirmRate = totalParticipation > 0 ? kills / totalParticipation : 0;
    const assistRate = deathEvents.length > 0 ? assists / deathEvents.length : 0;

    return { killConfirmRate, assistRate };
  }

  /**
   * Count healing done to others (not self)
   */
  private countHealingToOthers(events: HealingEvent[], selfName: string): number {
    return events.filter((e) => e.target?.name !== selfName).length;
  }

  /**
   * Calculate aggression score (0-100)
   */
  private calculateAggressionScore(params: {
    damageDealt: number;
    damageTaken: number;
    targetSwitchFrequency: number;
    burstDamageFrequency: number;
    selfHealingRatio: number;
  }): number {
    const {
      damageDealt,
      damageTaken,
      targetSwitchFrequency,
      burstDamageFrequency,
      selfHealingRatio,
    } = params;

    // Damage ratio component (high damage dealt vs taken = aggressive)
    const total = damageDealt + damageTaken;
    const damageRatioScore = total > 0 ? (damageDealt / total) * 40 : 20;

    // Target switching (more = aggressive/opportunistic)
    const switchScore = Math.min(targetSwitchFrequency * 3, 20);

    // Burst damage (more = aggressive)
    const burstScore = Math.min(burstDamageFrequency * 5, 20);

    // Low self-healing = aggressive
    const healScore = (1 - selfHealingRatio) * 20;

    return Math.min(100, Math.max(0, damageRatioScore + switchScore + burstScore + healScore));
  }

  /**
   * Calculate survivability score (0-100)
   */
  private calculateSurvivabilityScore(params: {
    damageTaken: number;
    healingReceived: number;
    mitigationRatio: number;
    selfHealingRatio: number;
    durationMs: number;
  }): number {
    const { damageTaken, healingReceived, mitigationRatio, selfHealingRatio, durationMs } = params;

    // Net health component
    const netHealth = healingReceived - damageTaken;
    const netHealthScore = netHealth >= 0 ? 30 : Math.max(0, 30 + (netHealth / 1000) * 5);

    // Mitigation score
    const mitigationScore = mitigationRatio * 30;

    // Self-healing score
    const selfHealScore = selfHealingRatio * 20;

    // Duration score (longer survival = better)
    const durationScore = Math.min((durationMs / 60000) * 10, 20);

    return Math.min(100, Math.max(0, netHealthScore + mitigationScore + selfHealScore + durationScore));
  }

  /**
   * Calculate team play score (0-100)
   */
  private calculateTeamPlayScore(params: {
    assistRate: number;
    healingToOthers: number;
    focusScore: number;
  }): number {
    const { assistRate, healingToOthers, focusScore } = params;

    // Assist score
    const assistScore = assistRate * 40;

    // Healing others score
    const healOthersScore = Math.min(healingToOthers * 5, 30);

    // Focus score (lower = more team-oriented target selection)
    const focusComponent = (1 - focusScore) * 30;

    return Math.min(100, Math.max(0, assistScore + healOthersScore + focusComponent));
  }

  /**
   * Calculate efficiency score (0-100)
   */
  private calculateEfficiencyScore(params: {
    damageDealt: number;
    damageTaken: number;
    healingDone: number;
    killConfirmRate: number;
    durationMs: number;
  }): number {
    const { damageDealt, damageTaken, healingDone, killConfirmRate, durationMs } = params;

    const durationSeconds = durationMs / 1000;

    // DPS efficiency
    const dps = damageDealt / durationSeconds;
    const dpsScore = Math.min((dps / 200) * 30, 30);

    // Damage efficiency (damage dealt vs taken)
    const total = damageDealt + damageTaken;
    const damageEfficiency = total > 0 ? damageDealt / total : 0.5;
    const damageEfficiencyScore = damageEfficiency * 30;

    // Kill efficiency
    const killScore = killConfirmRate * 20;

    // HPS bonus for healers
    const hps = healingDone / durationSeconds;
    const hpsBonus = Math.min((hps / 100) * 20, 20);

    return Math.min(100, Math.max(0, dpsScore + damageEfficiencyScore + killScore + hpsBonus));
  }

  /**
   * Convert features to a flat object for normalization
   */
  toFlatObject(features: BehaviorFeatures): Record<string, number> {
    return { ...features };
  }
}
