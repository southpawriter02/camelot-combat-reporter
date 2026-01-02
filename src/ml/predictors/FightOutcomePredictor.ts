/**
 * Fight Outcome Predictor
 *
 * Predicts win/loss probability based on current fight state.
 * Uses heuristics as a placeholder until trained models are available.
 */

import type { Entity } from '../../types/index.js';
import type { CombatSession } from '../../analysis/types/index.js';
import type {
  FightOutcomePrediction,
  FightOutcomePredictionInput,
  CombatStateFeatures,
  PredictionFactor,
  MLConfig,
} from '../types.js';
import { FeatureExtractor } from '../features/index.js';
import { ModelLoader } from '../models/index.js';
import { MODEL_NAMES, MODEL_VERSIONS, DEFAULT_ML_CONFIG } from '../config.js';

/**
 * Fight outcome predictor
 */
export class FightOutcomePredictor {
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
   * Load the prediction model
   */
  async load(): Promise<void> {
    if (this.isLoaded) return;

    await this.modelLoader.load({
      weightsPath: this.config.weightsPath,
      modelName: MODEL_NAMES.FIGHT_OUTCOME,
      version: MODEL_VERSIONS[MODEL_NAMES.FIGHT_OUTCOME],
    });

    this.isLoaded = true;
  }

  /**
   * Unload the model and free resources
   */
  unload(): void {
    this.modelLoader.unload(
      MODEL_NAMES.FIGHT_OUTCOME,
      MODEL_VERSIONS[MODEL_NAMES.FIGHT_OUTCOME]
    );
    this.isLoaded = false;
  }

  /**
   * Predict fight outcome from a combat session
   *
   * @param session - Combat session to predict outcome for
   * @param playerName - Name of the player to predict for
   * @returns Fight outcome prediction
   */
  async predict(session: CombatSession, playerName: string): Promise<FightOutcomePrediction> {
    // Ensure model is loaded
    if (!this.isLoaded && this.config.lazyLoad) {
      await this.load();
    }

    // Find the player entity
    const participant = session.participants.find((p) => p.entity.name === playerName);
    if (!participant) {
      return this.createLowConfidencePrediction('Player not found in session');
    }

    const selfEntity = participant.entity;

    // Check minimum events
    if (session.events.length < this.config.minEventsThreshold) {
      return this.createLowConfidencePrediction('Not enough events for prediction');
    }

    // Extract features
    const combatState = this.featureExtractor.extractCombatState(
      session.events,
      selfEntity,
      session.durationMs
    );

    // Get prediction from model
    const input: FightOutcomePredictionInput = { combatState };
    return this.predictFromFeatures(input, session.events.length);
  }

  /**
   * Predict from pre-extracted features
   */
  async predictFromFeatures(
    input: FightOutcomePredictionInput,
    eventsAnalyzed: number
  ): Promise<FightOutcomePrediction> {
    // Ensure model is loaded
    if (!this.isLoaded && this.config.lazyLoad) {
      await this.load();
    }

    const model = this.modelLoader.getModel(
      MODEL_NAMES.FIGHT_OUTCOME,
      MODEL_VERSIONS[MODEL_NAMES.FIGHT_OUTCOME]
    );

    if (!model) {
      return this.createLowConfidencePrediction('Model not loaded');
    }

    // Use raw features directly for the heuristic model
    const rawFeatures = this.combatStateToArray(input.combatState);

    // Get prediction
    const output = await model.predict(rawFeatures);
    const winProbability = output[0] ?? 0.5;
    const lossProbability = output[1] ?? 0.5;

    // Calculate confidence based on event count and prediction certainty
    const predictionCertainty = Math.abs(winProbability - 0.5) * 2; // 0-1
    const eventCountFactor = Math.min(eventsAnalyzed / 50, 1); // More events = higher confidence
    const confidence = predictionCertainty * 0.6 + eventCountFactor * 0.4;

    // Extract key factors
    const factors = this.extractFactors(input.combatState, winProbability);

    return {
      winProbability: winProbability,
      lossProbability: lossProbability,
      confidence,
      factors,
      timestamp: new Date(),
      eventsAnalyzed,
      isHeuristic: model.isPlaceholder,
    };
  }

  /**
   * Convert combat state features to a Float32Array for model input
   */
  private combatStateToArray(features: CombatStateFeatures): Float32Array {
    // Order must match COMBAT_STATE_FEATURE_NAMES
    return new Float32Array([
      features.elapsedTimeMs,
      features.elapsedTimeRatio,
      features.selfDamageDealt,
      features.selfDamageTaken,
      features.selfDamageRatio,
      features.selfDps,
      features.selfDtps,
      features.selfHealingDone,
      features.selfHealingReceived,
      features.selfNetHealth,
      features.selfCritRate,
      features.selfBlockRate,
      features.selfParryRate,
      features.selfEvadeRate,
      features.selfCCApplied,
      features.selfCCReceived,
      features.selfCCDurationApplied,
      features.selfCCDurationReceived,
      features.selfKills,
      features.selfDeaths,
      features.opponentCount,
      features.opponentTotalDamageDealt,
      features.opponentTotalHealingDone,
      features.opponentDeaths,
      features.allyCount,
      features.allyTotalDamageDealt,
      features.allyTotalHealingDone,
      features.allyDeaths,
      features.eventCount,
      features.isPvP,
      features.uniqueDamageSources,
      features.uniqueHealingSources,
    ]);
  }

  /**
   * Extract key factors influencing the prediction
   */
  private extractFactors(
    features: CombatStateFeatures,
    winProbability: number
  ): PredictionFactor[] {
    const factors: PredictionFactor[] = [];

    // Damage ratio factor
    const damageRatioImpact = features.selfDamageRatio > 0.5 ? 'POSITIVE' : 'NEGATIVE';
    factors.push({
      name: 'Damage Ratio',
      value: features.selfDamageRatio,
      impact: damageRatioImpact,
      contribution: Math.abs(features.selfDamageRatio - 0.5) * 0.4,
      description:
        damageRatioImpact === 'POSITIVE'
          ? 'Dealing more damage than taking'
          : 'Taking more damage than dealing',
    });

    // K/D factor
    const kd = features.selfDeaths > 0 ? features.selfKills / features.selfDeaths : features.selfKills;
    const kdImpact = kd >= 1 ? 'POSITIVE' : features.selfDeaths > 0 ? 'NEGATIVE' : 'NEUTRAL';
    factors.push({
      name: 'Kill/Death Ratio',
      value: kd,
      impact: kdImpact,
      contribution: Math.min(Math.abs(kd - 1) * 0.15, 0.3),
      description:
        kdImpact === 'POSITIVE'
          ? `${features.selfKills} kills with ${features.selfDeaths} deaths`
          : `Unfavorable K/D ratio`,
    });

    // Opponent deaths factor
    if (features.opponentDeaths > 0) {
      factors.push({
        name: 'Enemy Casualties',
        value: features.opponentDeaths,
        impact: 'POSITIVE',
        contribution: Math.min(features.opponentDeaths * 0.1, 0.3),
        description: `${features.opponentDeaths} enemies eliminated`,
      });
    }

    // Ally support factor
    if (features.allyCount > 0) {
      factors.push({
        name: 'Team Support',
        value: features.allyCount,
        impact: 'POSITIVE',
        contribution: Math.min(features.allyCount * 0.05, 0.15),
        description: `${features.allyCount} allies contributing`,
      });
    }

    // Net health factor
    const netHealthImpact = features.selfNetHealth >= 0 ? 'POSITIVE' : 'NEGATIVE';
    if (Math.abs(features.selfNetHealth) > 1000) {
      factors.push({
        name: 'Health Status',
        value: features.selfNetHealth,
        impact: netHealthImpact,
        contribution: Math.min(Math.abs(features.selfNetHealth) / 10000, 0.2),
        description:
          netHealthImpact === 'POSITIVE'
            ? 'Healing outpacing damage taken'
            : 'Taking more damage than healing received',
      });
    }

    // Sort by contribution (highest first)
    factors.sort((a, b) => b.contribution - a.contribution);

    return factors.slice(0, 5); // Return top 5 factors
  }

  /**
   * Create a low-confidence prediction for edge cases
   */
  private createLowConfidencePrediction(reason: string): FightOutcomePrediction {
    return {
      winProbability: 0.5,
      lossProbability: 0.5,
      confidence: 0,
      factors: [
        {
          name: 'Insufficient Data',
          value: 0,
          impact: 'NEUTRAL',
          contribution: 0,
          description: reason,
        },
      ],
      timestamp: new Date(),
      eventsAnalyzed: 0,
      isHeuristic: true,
    };
  }
}
