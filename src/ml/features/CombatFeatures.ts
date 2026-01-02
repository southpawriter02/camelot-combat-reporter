/**
 * Combat State Feature Extraction
 *
 * Extracts combat state features from a sequence of combat events.
 * These features capture the current state of a fight at any point in time.
 */

import type { CombatEvent, Entity, DamageEvent, HealingEvent, CrowdControlEvent, DeathEvent } from '../../types/index.js';
import { EventType, ActionType } from '../../types/index.js';
import type { CombatStateFeatures } from '../types.js';
import { TYPICAL_FIGHT_DURATION_MS } from '../config.js';

/**
 * Extracts combat state features from events
 */
export class CombatFeaturesExtractor {
  /**
   * Extract combat state features from events at a point in time
   *
   * @param events - Combat events to analyze
   * @param selfEntity - The player we're extracting features for
   * @param durationMs - Duration of the fight in milliseconds
   * @returns Combat state features
   */
  extract(events: CombatEvent[], selfEntity: Entity, durationMs: number): CombatStateFeatures {
    const durationSeconds = Math.max(durationMs / 1000, 0.001);
    const selfName = selfEntity.name;

    // Initialize counters
    let selfDamageDealt = 0;
    let selfDamageTaken = 0;
    let selfHealingDone = 0;
    let selfHealingReceived = 0;
    let selfCritHits = 0;
    let selfTotalHits = 0;
    let selfBlockedHits = 0;
    let selfParriedHits = 0;
    let selfEvadedHits = 0;
    let selfIncomingHits = 0;
    let selfCCApplied = 0;
    let selfCCReceived = 0;
    let selfCCDurationApplied = 0;
    let selfCCDurationReceived = 0;
    let selfKills = 0;
    let selfDeaths = 0;

    // Track opponents and allies
    const opponents = new Set<string>();
    const allies = new Set<string>();
    let opponentTotalDamageDealt = 0;
    let opponentTotalHealingDone = 0;
    let opponentDeaths = 0;
    let allyTotalDamageDealt = 0;
    let allyTotalHealingDone = 0;
    let allyDeaths = 0;

    // Track unique sources
    const damageSources = new Set<string>();
    const healingSources = new Set<string>();

    // Track PvP indicator
    let hasPlayerOpponent = false;

    // Process each event
    for (const event of events) {
      switch (event.eventType) {
        case EventType.DAMAGE_DEALT:
        case EventType.DAMAGE_RECEIVED: {
          const damageEvent = event as DamageEvent;
          const sourceName = damageEvent.source?.name;
          const targetName = damageEvent.target?.name;
          const amount = damageEvent.effectiveAmount ?? damageEvent.amount ?? 0;

          if (sourceName === selfName) {
            // Self dealt damage
            selfDamageDealt += amount;
            selfTotalHits++;
            if (damageEvent.isCritical) selfCritHits++;
            if (targetName) {
              opponents.add(targetName);
              damageSources.add(targetName);
              if (damageEvent.target?.isPlayer) hasPlayerOpponent = true;
            }
          } else if (targetName === selfName) {
            // Self received damage
            selfDamageTaken += amount;
            selfIncomingHits++;
            if (damageEvent.isBlocked) selfBlockedHits++;
            if (damageEvent.isParried) selfParriedHits++;
            if (damageEvent.isEvaded) selfEvadedHits++;
            if (sourceName) {
              opponents.add(sourceName);
              damageSources.add(sourceName);
              opponentTotalDamageDealt += amount;
              if (damageEvent.source?.isPlayer) hasPlayerOpponent = true;
            }
          } else {
            // Third party damage
            if (sourceName && targetName) {
              // Check if source is ally (attacking our opponent)
              if (opponents.has(targetName)) {
                allies.add(sourceName);
                allyTotalDamageDealt += amount;
              }
              // Check if target is ally (being attacked by opponent)
              if (opponents.has(sourceName)) {
                allies.add(targetName);
              }
              // Track opponent damage
              if (opponents.has(sourceName)) {
                opponentTotalDamageDealt += amount;
              }
            }
          }
          break;
        }

        case EventType.HEALING_DONE:
        case EventType.HEALING_RECEIVED: {
          const healEvent = event as HealingEvent;
          const sourceName = healEvent.source?.name;
          const targetName = healEvent.target?.name;
          const amount = healEvent.effectiveAmount ?? healEvent.amount ?? 0;

          if (sourceName === selfName) {
            selfHealingDone += amount;
            if (targetName) healingSources.add(targetName);
          }
          if (targetName === selfName) {
            selfHealingReceived += amount;
            if (sourceName) {
              healingSources.add(sourceName);
              allies.add(sourceName);
            }
          }
          // Track opponent and ally healing
          if (sourceName && opponents.has(sourceName)) {
            opponentTotalHealingDone += amount;
          }
          if (sourceName && allies.has(sourceName) && sourceName !== selfName) {
            allyTotalHealingDone += amount;
          }
          break;
        }

        case EventType.CROWD_CONTROL: {
          const ccEvent = event as CrowdControlEvent;
          const sourceName = ccEvent.source?.name;
          const targetName = ccEvent.target?.name;
          const duration = ccEvent.duration ?? 0;

          if (sourceName === selfName) {
            selfCCApplied++;
            selfCCDurationApplied += duration;
          }
          if (targetName === selfName) {
            selfCCReceived++;
            selfCCDurationReceived += duration;
          }
          break;
        }

        case EventType.DEATH: {
          const deathEvent = event as DeathEvent;
          const targetName = deathEvent.target?.name;
          const killerName = deathEvent.killer?.name;

          if (targetName === selfName) {
            selfDeaths++;
          }
          if (killerName === selfName) {
            selfKills++;
          }
          // Track opponent and ally deaths
          if (targetName && opponents.has(targetName)) {
            opponentDeaths++;
          }
          if (targetName && allies.has(targetName)) {
            allyDeaths++;
          }
          break;
        }
      }
    }

    // Calculate derived metrics
    const totalDamage = selfDamageDealt + selfDamageTaken;
    const selfDamageRatio = totalDamage > 0 ? selfDamageDealt / totalDamage : 0.5;
    const selfDps = selfDamageDealt / durationSeconds;
    const selfDtps = selfDamageTaken / durationSeconds;
    const selfNetHealth = selfHealingReceived - selfDamageTaken;

    // Calculate rates
    const selfCritRate = selfTotalHits > 0 ? selfCritHits / selfTotalHits : 0;
    const selfBlockRate = selfIncomingHits > 0 ? selfBlockedHits / selfIncomingHits : 0;
    const selfParryRate = selfIncomingHits > 0 ? selfParriedHits / selfIncomingHits : 0;
    const selfEvadeRate = selfIncomingHits > 0 ? selfEvadedHits / selfIncomingHits : 0;

    return {
      // Time-based
      elapsedTimeMs: durationMs,
      elapsedTimeRatio: durationMs / TYPICAL_FIGHT_DURATION_MS,

      // Self damage
      selfDamageDealt,
      selfDamageTaken,
      selfDamageRatio,
      selfDps,
      selfDtps,

      // Self healing
      selfHealingDone,
      selfHealingReceived,
      selfNetHealth,

      // Combat efficiency
      selfCritRate,
      selfBlockRate,
      selfParryRate,
      selfEvadeRate,

      // Crowd control
      selfCCApplied,
      selfCCReceived,
      selfCCDurationApplied,
      selfCCDurationReceived,

      // Kill/Death
      selfKills,
      selfDeaths,

      // Opponent aggregates
      opponentCount: opponents.size,
      opponentTotalDamageDealt,
      opponentTotalHealingDone,
      opponentDeaths,

      // Ally aggregates
      allyCount: allies.size,
      allyTotalDamageDealt,
      allyTotalHealingDone,
      allyDeaths,

      // Context
      eventCount: events.length,
      isPvP: hasPlayerOpponent ? 1 : 0,
      uniqueDamageSources: damageSources.size,
      uniqueHealingSources: healingSources.size,
    };
  }

  /**
   * Extract features at multiple time points during a fight
   * Useful for training data generation
   *
   * @param events - All combat events
   * @param selfEntity - The player entity
   * @param timeRatios - Array of time ratios (0-1) to extract features at
   * @returns Array of features at each time point
   */
  extractAtTimePoints(
    events: CombatEvent[],
    selfEntity: Entity,
    timeRatios: number[] = [0.25, 0.5, 0.75, 1.0]
  ): { ratio: number; features: CombatStateFeatures }[] {
    if (events.length === 0) return [];

    // Sort events by timestamp
    const sortedEvents = [...events].sort(
      (a, b) => a.timestamp.getTime() - b.timestamp.getTime()
    );

    const firstEvent = sortedEvents[0];
    const lastEvent = sortedEvents[sortedEvents.length - 1];
    if (!firstEvent || !lastEvent) {
      return [];
    }

    const startTime = firstEvent.timestamp.getTime();
    const endTime = lastEvent.timestamp.getTime();
    const totalDuration = endTime - startTime;

    const results: { ratio: number; features: CombatStateFeatures }[] = [];

    for (const ratio of timeRatios) {
      const targetTime = startTime + totalDuration * ratio;
      const partialEvents = sortedEvents.filter((e) => e.timestamp.getTime() <= targetTime);
      const partialDuration = totalDuration * ratio;

      if (partialEvents.length > 0) {
        results.push({
          ratio,
          features: this.extract(partialEvents, selfEntity, partialDuration),
        });
      }
    }

    return results;
  }

  /**
   * Convert features to a flat object for normalization
   */
  toFlatObject(features: CombatStateFeatures): Record<string, number> {
    return { ...features };
  }
}
