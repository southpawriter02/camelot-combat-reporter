/**
 * Performance Predictor
 *
 * Predicts expected DPS, HPS, and performance score based on player history.
 * Uses heuristics as a placeholder until trained models are available.
 */

import type { CombatSession } from '../../analysis/types/index.js';
import type {
  PerformancePrediction,
  PlayerHistoryFeatures,
  MLConfig,
} from '../types.js';
import { FeatureExtractor } from '../features/index.js';
import { ModelLoader } from '../models/index.js';
import { MODEL_NAMES, MODEL_VERSIONS, DEFAULT_ML_CONFIG } from '../config.js';

/**
 * Performance predictor
 */
export class PerformancePredictor {
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
      modelName: MODEL_NAMES.PERFORMANCE,
      version: MODEL_VERSIONS[MODEL_NAMES.PERFORMANCE],
    });

    this.isLoaded = true;
  }

  /**
   * Unload the model and free resources
   */
  unload(): void {
    this.modelLoader.unload(MODEL_NAMES.PERFORMANCE, MODEL_VERSIONS[MODEL_NAMES.PERFORMANCE]);
    this.isLoaded = false;
  }

  /**
   * Predict performance from historical sessions
   *
   * @param sessions - Historical combat sessions
   * @param playerName - Name of the player to predict for
   * @returns Performance prediction
   */
  async predict(sessions: CombatSession[], playerName: string): Promise<PerformancePrediction> {
    // Ensure model is loaded
    if (!this.isLoaded && this.config.lazyLoad) {
      await this.load();
    }

    // Need at least some sessions for prediction
    if (sessions.length === 0) {
      return this.createDefaultPrediction('No historical sessions available');
    }

    // Extract player history features
    const history = this.featureExtractor.extractPlayerHistory(sessions, playerName);

    return this.predictFromHistory(history, sessions.length);
  }

  /**
   * Predict from pre-extracted history features
   */
  async predictFromHistory(
    history: PlayerHistoryFeatures,
    sessionsAnalyzed: number
  ): Promise<PerformancePrediction> {
    // Ensure model is loaded
    if (!this.isLoaded && this.config.lazyLoad) {
      await this.load();
    }

    const model = this.modelLoader.getModel(
      MODEL_NAMES.PERFORMANCE,
      MODEL_VERSIONS[MODEL_NAMES.PERFORMANCE]
    );

    if (!model) {
      return this.createDefaultPrediction('Model not loaded');
    }

    // Convert features to array for model
    const featureArray = this.historyToArray(history);

    // Get prediction
    const output = await model.predict(featureArray);

    // Output: [predicted_dps, predicted_hps, predicted_score]
    const predictedDps = output[0];
    const predictedHps = output[1];
    const predictedScore = output[2];

    // Calculate confidence based on session count and variance
    const sessionCountFactor = Math.min(sessionsAnalyzed / 10, 1); // More sessions = higher confidence
    const varianceFactor = Math.max(0, 1 - history.performanceVariance / 100); // Lower variance = higher confidence
    const confidence = sessionCountFactor * 0.6 + varianceFactor * 0.4;

    // Calculate prediction ranges based on historical variance
    const dpsStd = Math.sqrt(history.dpsVariance);
    const performanceStd = Math.sqrt(history.performanceVariance);

    const safePredictedDps = predictedDps ?? 0;
    const safePredictedHps = predictedHps ?? 0;
    const safePredictedScore = predictedScore ?? 50;

    const dpsRange = {
      low: Math.max(0, safePredictedDps - dpsStd * 1.5),
      high: safePredictedDps + dpsStd * 1.5,
    };

    const hpsRange = history.healerRoleRatio > 0.3
      ? {
          low: Math.max(0, safePredictedHps - dpsStd * 0.5), // Using DPS variance as proxy
          high: safePredictedHps + dpsStd * 0.5,
        }
      : undefined;

    const performanceRange = {
      low: Math.max(0, Math.min(100, safePredictedScore - performanceStd * 1.5)),
      high: Math.min(100, safePredictedScore + performanceStd * 1.5),
    };

    return {
      predictedDps: safePredictedDps,
      dpsRange,
      predictedHps: history.healerRoleRatio > 0.1 ? safePredictedHps : undefined,
      hpsRange,
      predictedPerformance: Math.min(100, Math.max(0, safePredictedScore)),
      performanceRange,
      confidence,
      sessionsAnalyzed,
      isHeuristic: model.isPlaceholder,
    };
  }

  /**
   * Convert player history features to Float32Array for model input
   */
  private historyToArray(history: PlayerHistoryFeatures): Float32Array {
    // Normalize values for model input
    return new Float32Array([
      history.recentAvgDps / 400, // Normalize to ~0-1 range
      history.recentAvgHps / 200,
      history.recentAvgKdr / 3, // KDR typically 0-3
      history.recentAvgPerformance / 100,
      Math.sqrt(history.dpsVariance) / 200, // Normalize std dev
      Math.sqrt(history.performanceVariance) / 30,
      history.recentWinRate,
      Math.min(history.sessionCount / 20, 1), // Cap at 20 sessions
      history.ddRoleRatio,
      history.healerRoleRatio,
      history.tankRoleRatio,
      // Padding to match expected input size if needed
      0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
    ]);
  }

  /**
   * Create a default prediction for edge cases
   */
  private createDefaultPrediction(reason: string): PerformancePrediction {
    return {
      predictedDps: 200, // Average default
      dpsRange: { low: 100, high: 300 },
      predictedHps: undefined,
      hpsRange: undefined,
      predictedPerformance: 50, // Average default
      performanceRange: { low: 30, high: 70 },
      confidence: 0,
      sessionsAnalyzed: 0,
      isHeuristic: true,
    };
  }
}
