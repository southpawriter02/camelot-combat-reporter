/**
 * Threat Assessor
 *
 * Assesses threat level of enemies based on their behavior.
 * Uses heuristics as a placeholder until trained models are available.
 */

import type { Entity, CombatEvent, DamageEvent, HealingEvent, CrowdControlEvent, DeathEvent } from '../../types/index.js';
import { EventType } from '../../types/index.js';
import type { CombatSession } from '../../analysis/types/index.js';
import type {
  ThreatAssessment,
  ThreatCategory,
  ThreatFactor,
  MLConfig,
} from '../types.js';
import { ModelLoader } from '../models/index.js';
import { MODEL_NAMES, MODEL_VERSIONS, DEFAULT_ML_CONFIG, THREAT_THRESHOLDS } from '../config.js';

/**
 * Threat assessor
 */
export class ThreatAssessor {
  private modelLoader: ModelLoader;
  private config: MLConfig;
  private isLoaded = false;

  constructor(config: Partial<MLConfig> = {}) {
    this.config = { ...DEFAULT_ML_CONFIG, ...config };
    this.modelLoader = new ModelLoader();
  }

  /**
   * Load the assessment model
   */
  async load(): Promise<void> {
    if (this.isLoaded) return;

    await this.modelLoader.load({
      weightsPath: this.config.weightsPath,
      modelName: MODEL_NAMES.THREAT,
      version: MODEL_VERSIONS[MODEL_NAMES.THREAT],
    });

    this.isLoaded = true;
  }

  /**
   * Unload the model and free resources
   */
  unload(): void {
    this.modelLoader.unload(MODEL_NAMES.THREAT, MODEL_VERSIONS[MODEL_NAMES.THREAT]);
    this.isLoaded = false;
  }

  /**
   * Assess threats for all enemies in a session
   *
   * @param session - Combat session
   * @param selfEntity - The player to assess threats for
   * @returns Array of threat assessments for each enemy
   */
  async assessAll(session: CombatSession, selfEntity: Entity): Promise<ThreatAssessment[]> {
    // Ensure model is loaded
    if (!this.isLoaded && this.config.lazyLoad) {
      await this.load();
    }

    const selfName = selfEntity.name;

    // Identify all enemies (entities that dealt damage to or received damage from self)
    const enemies = this.identifyEnemies(session.events, selfName);

    // Assess each enemy
    const assessments: ThreatAssessment[] = [];
    for (const enemy of enemies) {
      const assessment = await this.assessSingle(session, selfEntity, enemy);
      assessments.push(assessment);
    }

    // Sort by threat level (highest first)
    assessments.sort((a, b) => b.threatLevel - a.threatLevel);

    return assessments;
  }

  /**
   * Assess threat for a single entity
   *
   * @param session - Combat session
   * @param selfEntity - The player
   * @param targetEntity - The entity to assess
   * @returns Threat assessment
   */
  async assessSingle(
    session: CombatSession,
    selfEntity: Entity,
    targetEntity: Entity
  ): Promise<ThreatAssessment> {
    // Ensure model is loaded
    if (!this.isLoaded && this.config.lazyLoad) {
      await this.load();
    }

    const model = this.modelLoader.getModel(
      MODEL_NAMES.THREAT,
      MODEL_VERSIONS[MODEL_NAMES.THREAT]
    );

    // Extract threat features for this entity
    const features = this.extractThreatFeatures(
      session.events,
      selfEntity.name,
      targetEntity.name,
      session.durationMs
    );

    // Get model prediction
    let threatScore: number;
    if (model) {
      const featureArray = this.featuresToArray(features);
      const output = await model.predict(featureArray);
      threatScore = (output[0] ?? 0.5) * 100; // Scale to 0-100
    } else {
      // Fallback to heuristic
      threatScore = this.calculateHeuristicThreat(features);
    }

    // Determine threat category
    const threatCategory = this.getThreatCategory(threatScore);

    // Extract contributing factors
    const factors = this.extractFactors(features);

    // Generate recommendations
    const recommendations = this.generateRecommendations(threatCategory, factors);

    return {
      entity: targetEntity,
      threatLevel: Math.round(threatScore),
      threatCategory,
      factors,
      recommendations,
      isHeuristic: !model || model.isPlaceholder,
    };
  }

  /**
   * Identify enemy entities from combat events
   */
  private identifyEnemies(events: CombatEvent[], selfName: string): Entity[] {
    const enemyMap = new Map<string, Entity>();

    for (const event of events) {
      if (event.eventType === EventType.DAMAGE_DEALT || event.eventType === EventType.DAMAGE_RECEIVED) {
        const damageEvent = event as DamageEvent;
        const source = damageEvent.source;
        const target = damageEvent.target;

        // If self dealt damage to target, target is enemy
        if (source?.name === selfName && target && target.name !== selfName) {
          enemyMap.set(target.name, target);
        }

        // If source dealt damage to self, source is enemy
        if (target?.name === selfName && source && source.name !== selfName) {
          enemyMap.set(source.name, source);
        }
      }
    }

    return Array.from(enemyMap.values());
  }

  /**
   * Extract threat features for a specific entity
   */
  private extractThreatFeatures(
    events: CombatEvent[],
    selfName: string,
    targetName: string,
    durationMs: number
  ): Record<string, number> {
    const durationSeconds = Math.max(durationMs / 1000, 1);

    let damageToSelf = 0;
    let damageFromSelf = 0;
    let healingDone = 0;
    let ccAppliedToSelf = 0;
    let ccDurationToSelf = 0;
    let killsOnSelf = 0;
    let deathsFromSelf = 0;
    let eventCount = 0;
    let criticalHits = 0;
    let totalHits = 0;

    for (const event of events) {
      switch (event.eventType) {
        case EventType.DAMAGE_DEALT:
        case EventType.DAMAGE_RECEIVED: {
          const damageEvent = event as DamageEvent;
          // Target dealing damage to self
          if (damageEvent.source?.name === targetName && damageEvent.target?.name === selfName) {
            damageToSelf += damageEvent.effectiveAmount ?? damageEvent.amount ?? 0;
            totalHits++;
            if (damageEvent.isCritical) criticalHits++;
            eventCount++;
          }
          // Self dealing damage to target
          if (damageEvent.source?.name === selfName && damageEvent.target?.name === targetName) {
            damageFromSelf += damageEvent.effectiveAmount ?? damageEvent.amount ?? 0;
            eventCount++;
          }
          break;
        }

        case EventType.HEALING_DONE:
        case EventType.HEALING_RECEIVED: {
          const healEvent = event as HealingEvent;
          if (healEvent.source?.name === targetName) {
            healingDone += healEvent.effectiveAmount ?? healEvent.amount ?? 0;
            eventCount++;
          }
          break;
        }

        case EventType.CROWD_CONTROL: {
          const ccEvent = event as CrowdControlEvent;
          if (ccEvent.source?.name === targetName && ccEvent.target?.name === selfName) {
            ccAppliedToSelf++;
            ccDurationToSelf += ccEvent.duration ?? 0;
            eventCount++;
          }
          break;
        }

        case EventType.DEATH: {
          const deathEvent = event as DeathEvent;
          if (deathEvent.target?.name === selfName && deathEvent.killer?.name === targetName) {
            killsOnSelf++;
          }
          if (deathEvent.target?.name === targetName && deathEvent.killer?.name === selfName) {
            deathsFromSelf++;
          }
          break;
        }
      }
    }

    return {
      damageToSelf,
      damageFromSelf,
      dpsToSelf: damageToSelf / durationSeconds,
      healingDone,
      ccAppliedToSelf,
      ccDurationToSelf,
      killsOnSelf,
      deathsFromSelf,
      eventCount,
      critRate: totalHits > 0 ? criticalHits / totalHits : 0,
      netDamage: damageToSelf - damageFromSelf,
      threatRatio: damageFromSelf > 0 ? damageToSelf / damageFromSelf : damageToSelf,
      isHealer: healingDone > damageToSelf * 0.5 ? 1 : 0,
      durationSeconds,
    };
  }

  /**
   * Convert features to Float32Array for model input
   */
  private featuresToArray(features: Record<string, number>): Float32Array {
    // Helper to get value with default
    const get = (key: string, defaultVal = 0): number => features[key] ?? defaultVal;

    // Normalize features for model input
    return new Float32Array([
      get('damageToSelf') / 10000, // Normalize
      get('damageFromSelf') / 10000,
      get('dpsToSelf') / 200,
      get('healingDone') / 10000,
      get('ccAppliedToSelf') / 5,
      get('ccDurationToSelf') / 30,
      get('killsOnSelf'),
      get('deathsFromSelf'),
      get('eventCount') / 50,
      get('critRate'),
      Math.tanh(get('netDamage') / 5000), // Use tanh for bounded normalization
      Math.min(get('threatRatio') / 3, 1),
      get('isHealer'),
      Math.min(get('durationSeconds') / 60, 1),
      // Padding
      0, 0, 0, 0, 0, 0,
    ]);
  }

  /**
   * Calculate heuristic threat score
   */
  private calculateHeuristicThreat(features: Record<string, number>): number {
    // Helper to get value with default
    const get = (key: string, defaultVal = 0): number => features[key] ?? defaultVal;

    let threat = 0;

    // Damage dealt to self is primary factor (40%)
    threat += Math.min(get('dpsToSelf') / 200, 1) * 40;

    // Kills on self is major factor (25%)
    threat += Math.min(get('killsOnSelf') * 25, 25);

    // CC applied is significant (15%)
    threat += Math.min(get('ccAppliedToSelf') * 5, 15);

    // Critical hit rate increases threat (10%)
    threat += get('critRate') * 10;

    // Healers that enable others are threats (10%)
    if (get('isHealer')) {
      threat += Math.min(get('healingDone') / 10000, 1) * 10;
    }

    // Deaths from self reduce threat
    threat -= get('deathsFromSelf') * 15;

    return Math.max(0, Math.min(100, threat));
  }

  /**
   * Get threat category from score
   */
  private getThreatCategory(score: number): ThreatCategory {
    if (score >= THREAT_THRESHOLDS.high) return 'CRITICAL';
    if (score >= THREAT_THRESHOLDS.medium) return 'HIGH';
    if (score >= THREAT_THRESHOLDS.low) return 'MEDIUM';
    return 'LOW';
  }

  /**
   * Extract contributing factors
   */
  private extractFactors(features: Record<string, number>): ThreatFactor[] {
    // Helper to get value with default
    const get = (key: string, defaultVal = 0): number => features[key] ?? defaultVal;

    const factors: ThreatFactor[] = [];

    if (get('damageToSelf') > 0) {
      factors.push({
        name: 'Damage Output',
        contribution: Math.min(get('dpsToSelf') / 200, 1),
        description: `Dealing ${Math.round(get('dpsToSelf'))} DPS to you`,
      });
    }

    if (get('killsOnSelf') > 0) {
      factors.push({
        name: 'Kill Threat',
        contribution: 1,
        description: `Has killed you ${get('killsOnSelf')} time(s)`,
      });
    }

    if (get('ccAppliedToSelf') > 0) {
      factors.push({
        name: 'Crowd Control',
        contribution: Math.min(get('ccAppliedToSelf') / 3, 1),
        description: `Applied ${get('ccAppliedToSelf')} CC effects (${get('ccDurationToSelf')}s total)`,
      });
    }

    if (get('critRate') > 0.2) {
      factors.push({
        name: 'Critical Hits',
        contribution: get('critRate'),
        description: `${Math.round(get('critRate') * 100)}% critical hit rate`,
      });
    }

    if (get('isHealer')) {
      factors.push({
        name: 'Healer',
        contribution: 0.6,
        description: 'Providing healing support to enemies',
      });
    }

    if (get('deathsFromSelf') > 0) {
      factors.push({
        name: 'Vulnerability',
        contribution: -Math.min(get('deathsFromSelf') * 0.3, 0.6),
        description: `You have killed them ${get('deathsFromSelf')} time(s)`,
      });
    }

    // Sort by contribution (highest first)
    factors.sort((a, b) => Math.abs(b.contribution) - Math.abs(a.contribution));

    return factors.slice(0, 4);
  }

  /**
   * Generate tactical recommendations
   */
  private generateRecommendations(category: ThreatCategory, factors: ThreatFactor[]): string[] {
    const recommendations: string[] = [];

    switch (category) {
      case 'CRITICAL':
        recommendations.push('Priority target - focus fire immediately');
        recommendations.push('Consider using crowd control');
        break;
      case 'HIGH':
        recommendations.push('High threat - engage with caution');
        recommendations.push('Coordinate with allies before engaging');
        break;
      case 'MEDIUM':
        recommendations.push('Moderate threat - maintain awareness');
        break;
      case 'LOW':
        recommendations.push('Low priority - focus on higher threats first');
        break;
    }

    // Add factor-specific recommendations
    const hasHealer = factors.some((f) => f.name === 'Healer');
    const hasCCThreat = factors.some((f) => f.name === 'Crowd Control' && f.contribution > 0.5);
    const hasKillThreat = factors.some((f) => f.name === 'Kill Threat');

    if (hasHealer) {
      recommendations.push('Consider neutralizing healer support first');
    }
    if (hasCCThreat) {
      recommendations.push('Prepare CC breaks or immunities');
    }
    if (hasKillThreat) {
      recommendations.push('Use defensive cooldowns when engaging');
    }

    return recommendations.slice(0, 3);
  }
}
