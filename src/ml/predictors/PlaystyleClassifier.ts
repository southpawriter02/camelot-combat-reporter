/**
 * Playstyle Classifier
 *
 * Classifies player behavior as aggressive, defensive, balanced, or opportunistic.
 * Uses heuristics as a placeholder until trained models are available.
 */

import type { Entity } from '../../types/index.js';
import type { CombatSession } from '../../analysis/types/index.js';
import type {
  PlaystyleClassification,
  PlaystyleType,
  PlaystyleTrait,
  BehaviorFeatures,
  MLConfig,
} from '../types.js';
import { FeatureExtractor } from '../features/index.js';
import { ModelLoader } from '../models/index.js';
import { MODEL_NAMES, MODEL_VERSIONS, DEFAULT_ML_CONFIG, PLAYSTYLE_THRESHOLDS } from '../config.js';

/**
 * Playstyle classifier
 */
export class PlaystyleClassifier {
  private featureExtractor: FeatureExtractor;
  private modelLoader: ModelLoader;
  private config: MLConfig;
  private isLoaded = false;

  constructor(config: Partial<MLConfig> = {}) {
    this.config = { ...DEFAULT_ML_CONFIG, ...config };
    this.featureExtractor = new FeatureExtractor();
    this.modelLoader = new ModelLoader();
  }

  /**
   * Load the classification model
   */
  async load(): Promise<void> {
    if (this.isLoaded) return;

    await this.modelLoader.load({
      weightsPath: this.config.weightsPath,
      modelName: MODEL_NAMES.PLAYSTYLE,
      version: MODEL_VERSIONS[MODEL_NAMES.PLAYSTYLE],
    });

    this.isLoaded = true;
  }

  /**
   * Unload the model and free resources
   */
  unload(): void {
    this.modelLoader.unload(MODEL_NAMES.PLAYSTYLE, MODEL_VERSIONS[MODEL_NAMES.PLAYSTYLE]);
    this.isLoaded = false;
  }

  /**
   * Classify playstyle from a combat session
   *
   * @param session - Combat session to classify
   * @param playerName - Name of the player to classify
   * @returns Playstyle classification
   */
  async classify(session: CombatSession, playerName: string): Promise<PlaystyleClassification> {
    // Ensure model is loaded
    if (!this.isLoaded && this.config.lazyLoad) {
      await this.load();
    }

    // Find the player entity
    const participant = session.participants.find((p) => p.entity.name === playerName);
    if (!participant) {
      return this.createDefaultClassification('Player not found in session');
    }

    const selfEntity = participant.entity;

    // Check minimum events
    if (session.events.length < this.config.minEventsThreshold) {
      return this.createDefaultClassification('Not enough events for classification');
    }

    // Extract behavior features
    const behavior = this.featureExtractor.extractBehavior(
      session.events,
      selfEntity,
      session.durationMs
    );

    return this.classifyFromFeatures(behavior, session.events.length);
  }

  /**
   * Classify from pre-extracted behavior features
   */
  async classifyFromFeatures(
    behavior: BehaviorFeatures,
    eventsAnalyzed: number
  ): Promise<PlaystyleClassification> {
    // Ensure model is loaded
    if (!this.isLoaded && this.config.lazyLoad) {
      await this.load();
    }

    const model = this.modelLoader.getModel(
      MODEL_NAMES.PLAYSTYLE,
      MODEL_VERSIONS[MODEL_NAMES.PLAYSTYLE]
    );

    if (!model) {
      return this.createDefaultClassification('Model not loaded');
    }

    // Convert features to array for model
    const featureArray = this.behaviorToArray(behavior);

    // Get prediction
    const output = await model.predict(featureArray);

    // Output: [aggressive, defensive, balanced, opportunistic]
    const styleScores: Record<PlaystyleType, number> = {
      AGGRESSIVE: output[0] ?? 0.25,
      DEFENSIVE: output[1] ?? 0.25,
      BALANCED: output[2] ?? 0.25,
      OPPORTUNISTIC: output[3] ?? 0.25,
    };

    // Find primary style (highest score)
    let primaryStyle: PlaystyleType = 'BALANCED';
    let maxScore = 0;
    for (const [style, score] of Object.entries(styleScores)) {
      if (score > maxScore) {
        maxScore = score;
        primaryStyle = style as PlaystyleType;
      }
    }

    // Calculate confidence based on score separation
    const scores = Object.values(styleScores).sort((a, b) => b - a);
    const firstScore = scores[0] ?? 0;
    const secondScore = scores[1] ?? 0;
    const separation = firstScore - secondScore; // Gap between top two
    const confidence = Math.min(separation * 2 + 0.3, 1);

    // Extract behavioral traits
    const traits = this.extractTraits(behavior, primaryStyle);

    return {
      primaryStyle,
      styleScores,
      confidence,
      traits,
      eventsAnalyzed,
      isHeuristic: model.isPlaceholder,
    };
  }

  /**
   * Convert behavior features to Float32Array for model input
   */
  private behaviorToArray(features: BehaviorFeatures): Float32Array {
    // Order must match BEHAVIOR_FEATURE_NAMES
    return new Float32Array([
      features.meleeRatio,
      features.spellRatio,
      features.styleRatio,
      features.procRatio,
      features.targetSwitchFrequency,
      features.targetFocusScore,
      features.lowHealthTargetPreference,
      features.uniqueTargetsAttacked,
      features.burstDamageFrequency,
      features.sustainedDamageRatio,
      features.peakDps,
      features.damageVariance,
      features.defensiveAbilityUsage,
      features.selfHealingRatio,
      features.avgTimeBetweenHits,
      features.mitigationRatio,
      features.avgReactionTimeMs,
      features.ccReactionTimeMs,
      features.killConfirmRate,
      features.assistRate,
      features.aggressionScore,
      features.survivabilityScore,
      features.teamPlayScore,
      features.efficiencyScore,
    ]);
  }

  /**
   * Extract notable behavioral traits
   */
  private extractTraits(behavior: BehaviorFeatures, primaryStyle: PlaystyleType): PlaystyleTrait[] {
    const traits: PlaystyleTrait[] = [];

    // Aggression traits
    if (behavior.aggressionScore > 70) {
      traits.push({
        name: 'Highly Aggressive',
        value: behavior.aggressionScore / 100,
        description: 'Prioritizes dealing damage over personal safety',
      });
    } else if (behavior.aggressionScore < 30) {
      traits.push({
        name: 'Cautious',
        value: 1 - behavior.aggressionScore / 100,
        description: 'Takes a careful, measured approach to combat',
      });
    }

    // Target selection traits
    if (behavior.targetFocusScore > 0.8) {
      traits.push({
        name: 'Focused Attacker',
        value: behavior.targetFocusScore,
        description: 'Concentrates damage on a single target',
      });
    } else if (behavior.targetSwitchFrequency > 10) {
      traits.push({
        name: 'Target Switcher',
        value: Math.min(behavior.targetSwitchFrequency / 15, 1),
        description: 'Frequently changes targets during combat',
      });
    }

    // Damage pattern traits
    if (behavior.burstDamageFrequency > 5) {
      traits.push({
        name: 'Burst Damage',
        value: Math.min(behavior.burstDamageFrequency / 10, 1),
        description: 'Delivers damage in powerful bursts',
      });
    }
    if (behavior.sustainedDamageRatio > 0.7) {
      traits.push({
        name: 'Sustained Pressure',
        value: behavior.sustainedDamageRatio,
        description: 'Maintains consistent damage over time',
      });
    }

    // Defensive traits
    if (behavior.mitigationRatio > 0.3) {
      traits.push({
        name: 'Damage Mitigator',
        value: behavior.mitigationRatio,
        description: 'Effectively blocks, parries, or evades attacks',
      });
    }
    if (behavior.selfHealingRatio > 0.2) {
      traits.push({
        name: 'Self-Sustaining',
        value: behavior.selfHealingRatio,
        description: 'Uses self-healing abilities frequently',
      });
    }

    // Team play traits
    if (behavior.teamPlayScore > 60) {
      traits.push({
        name: 'Team Player',
        value: behavior.teamPlayScore / 100,
        description: 'Coordinates well with allies',
      });
    }
    if (behavior.assistRate > 0.5) {
      traits.push({
        name: 'Assist Provider',
        value: behavior.assistRate,
        description: 'Contributes to enemy takedowns',
      });
    }

    // Kill efficiency
    if (behavior.killConfirmRate > 0.7) {
      traits.push({
        name: 'Finisher',
        value: behavior.killConfirmRate,
        description: 'Excels at securing kills',
      });
    }

    // Efficiency traits
    if (behavior.efficiencyScore > 70) {
      traits.push({
        name: 'High Efficiency',
        value: behavior.efficiencyScore / 100,
        description: 'Makes the most of combat opportunities',
      });
    }

    // Sort by value (highest first) and limit to top traits
    traits.sort((a, b) => b.value - a.value);
    return traits.slice(0, 4);
  }

  /**
   * Create a default classification for edge cases
   */
  private createDefaultClassification(reason: string): PlaystyleClassification {
    return {
      primaryStyle: 'BALANCED',
      styleScores: {
        AGGRESSIVE: 0.25,
        DEFENSIVE: 0.25,
        BALANCED: 0.25,
        OPPORTUNISTIC: 0.25,
      },
      confidence: 0,
      traits: [
        {
          name: 'Insufficient Data',
          value: 0,
          description: reason,
        },
      ],
      eventsAnalyzed: 0,
      isHeuristic: true,
    };
  }
}
