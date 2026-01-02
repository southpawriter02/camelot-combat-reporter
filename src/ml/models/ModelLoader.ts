/**
 * Model Loader
 *
 * Handles loading TensorFlow.js models from disk or using heuristic fallbacks.
 * Supports lazy loading and graceful degradation when models aren't available.
 */

import type { ModelMetadata } from '../types.js';

/**
 * Loaded model representation
 * For now, this is a placeholder that will be replaced with actual TensorFlow.js
 * model loading once models are trained.
 */
export interface LoadedModel {
  /** Model metadata */
  metadata: ModelMetadata;
  /** Whether this is a placeholder/heuristic model */
  isPlaceholder: boolean;
  /** Predict function (returns output tensor data as number array) */
  predict: (input: Float32Array) => Promise<Float32Array>;
  /** Dispose of model resources */
  dispose: () => void;
}

/**
 * Model loading options
 */
export interface ModelLoadOptions {
  /** Path to model weights directory */
  weightsPath: string;
  /** Model name/identifier */
  modelName: string;
  /** Model version */
  version: string;
}

/**
 * Model loader for TensorFlow.js models
 *
 * Currently implements heuristic-based predictions as placeholders.
 * When real models are trained, this will load actual TensorFlow.js models.
 */
export class ModelLoader {
  private loadedModels = new Map<string, LoadedModel>();

  /**
   * Load a model from disk or return a placeholder
   *
   * @param options - Model loading options
   * @returns Loaded model or placeholder
   */
  async load(options: ModelLoadOptions): Promise<LoadedModel> {
    const modelKey = `${options.modelName}-${options.version}`;

    // Check if already loaded
    if (this.loadedModels.has(modelKey)) {
      return this.loadedModels.get(modelKey)!;
    }

    // Try to load actual TensorFlow.js model
    // For now, we create placeholder models since we don't have trained weights yet
    const model = await this.createPlaceholderModel(options);

    this.loadedModels.set(modelKey, model);
    return model;
  }

  /**
   * Create a placeholder model that uses heuristics
   * This will be replaced with actual model loading when weights are available
   */
  private async createPlaceholderModel(options: ModelLoadOptions): Promise<LoadedModel> {
    const metadata: ModelMetadata = {
      name: options.modelName,
      version: options.version,
      inputShape: this.getInputShape(options.modelName),
      outputShape: this.getOutputShape(options.modelName),
      featureNames: [],
      trainedOn: new Date().toISOString(),
      isPlaceholder: true,
    };

    // Create a placeholder predict function based on model type
    const predict = this.createPlaceholderPredictor(options.modelName);

    return {
      metadata,
      isPlaceholder: true,
      predict,
      dispose: () => {
        // No resources to dispose for placeholder
      },
    };
  }

  /**
   * Get expected input shape for a model
   */
  private getInputShape(modelName: string): number[] {
    switch (modelName) {
      case 'fight-outcome':
        return [32]; // Combat state features
      case 'playstyle':
        return [24]; // Behavior features
      case 'performance':
        return [24]; // History + context features
      case 'threat':
        return [20]; // Threat features
      default:
        return [32];
    }
  }

  /**
   * Get expected output shape for a model
   */
  private getOutputShape(modelName: string): number[] {
    switch (modelName) {
      case 'fight-outcome':
        return [2]; // [win_prob, loss_prob]
      case 'playstyle':
        return [4]; // [aggressive, defensive, balanced, opportunistic]
      case 'performance':
        return [3]; // [dps, hps, score]
      case 'threat':
        return [1]; // [threat_score]
      default:
        return [2];
    }
  }

  /**
   * Create a placeholder predictor function based on model type
   */
  private createPlaceholderPredictor(
    modelName: string
  ): (input: Float32Array) => Promise<Float32Array> {
    switch (modelName) {
      case 'fight-outcome':
        return this.fightOutcomeHeuristic.bind(this);
      case 'playstyle':
        return this.playstyleHeuristic.bind(this);
      case 'performance':
        return this.performanceHeuristic.bind(this);
      case 'threat':
        return this.threatHeuristic.bind(this);
      default:
        return async () => new Float32Array([0.5, 0.5]);
    }
  }

  /**
   * Heuristic predictor for fight outcome
   * Uses damage ratio, K/D, and healing as primary factors
   */
  private async fightOutcomeHeuristic(input: Float32Array): Promise<Float32Array> {
    // Feature indices (from COMBAT_STATE_FEATURE_NAMES):
    // 4: selfDamageRatio, 18: selfKills, 19: selfDeaths, 23: opponentDeaths

    const damageRatio = input[4] ?? 0.5; // Already normalized 0-1
    const selfKills = input[18] ?? 0;
    const selfDeaths = input[19] ?? 0;
    const opponentDeaths = input[23] ?? 0;

    // Calculate win probability based on:
    // - Damage ratio (higher = better)
    // - K/D ratio
    // - Opponent deaths

    let winScore = 0.5;

    // Damage ratio contribution (40% weight)
    winScore += (damageRatio - 0.5) * 0.8;

    // K/D contribution (30% weight)
    const kd = selfDeaths > 0.1 ? selfKills / selfDeaths : selfKills + 0.5;
    const kdScore = Math.min(kd / 2, 1) - 0.5; // Normalize around 0
    winScore += kdScore * 0.6;

    // Opponent deaths contribution (30% weight)
    winScore += Math.min(opponentDeaths * 0.1, 0.3);

    // Self deaths penalty
    winScore -= selfDeaths * 0.15;

    // Clamp to valid probability range
    const winProb = Math.max(0.05, Math.min(0.95, winScore));
    const lossProb = 1 - winProb;

    return new Float32Array([winProb, lossProb]);
  }

  /**
   * Heuristic predictor for playstyle classification
   * Returns scores for [aggressive, defensive, balanced, opportunistic]
   */
  private async playstyleHeuristic(input: Float32Array): Promise<Float32Array> {
    // Feature indices (from BEHAVIOR_FEATURE_NAMES):
    // 4: targetSwitchFrequency, 5: targetFocusScore
    // 12: defensiveAbilityUsage, 13: selfHealingRatio
    // 20: aggressionScore, 21: survivabilityScore

    const targetSwitchFreq = input[4] ?? 0.5;
    const targetFocusScore = input[5] ?? 0.5;
    const defensiveUsage = input[12] ?? 0.5;
    const selfHealRatio = input[13] ?? 0.5;
    const aggressionScore = input[20] ?? 0.5;
    const survivabilityScore = input[21] ?? 0.5;

    // Calculate style scores
    let aggressive = aggressionScore * 0.6 + (1 - selfHealRatio) * 0.2 + (1 - defensiveUsage) * 0.2;
    let defensive = survivabilityScore * 0.4 + defensiveUsage * 0.3 + selfHealRatio * 0.3;
    let balanced = 0.5 - Math.abs(aggressive - defensive) * 0.5;
    let opportunistic = targetSwitchFreq * 0.4 + (1 - targetFocusScore) * 0.4 + 0.2;

    // Normalize to sum to 1
    const total = aggressive + defensive + balanced + opportunistic;
    if (total > 0) {
      aggressive /= total;
      defensive /= total;
      balanced /= total;
      opportunistic /= total;
    } else {
      balanced = 1;
      aggressive = defensive = opportunistic = 0;
    }

    return new Float32Array([aggressive, defensive, balanced, opportunistic]);
  }

  /**
   * Heuristic predictor for performance
   * Returns [predicted_dps, predicted_hps, predicted_score]
   */
  private async performanceHeuristic(input: Float32Array): Promise<Float32Array> {
    // Input contains player history features
    // 0: recentAvgDps, 1: recentAvgHps, 3: recentAvgPerformance

    // These are already normalized, so we denormalize for output
    // Using approximate denormalization based on typical ranges
    const normalizedDps = input[0] ?? 0.5;
    const normalizedHps = input[1] ?? 0.5;
    const normalizedPerformance = input[3] ?? 0.5;

    // Denormalize (approximate - actual stats would use real normalization params)
    const predictedDps = normalizedDps * 400; // Assume max ~400 DPS
    const predictedHps = normalizedHps * 200; // Assume max ~200 HPS
    const predictedScore = normalizedPerformance * 100; // 0-100 scale

    return new Float32Array([predictedDps, predictedHps, predictedScore]);
  }

  /**
   * Heuristic predictor for threat assessment
   * Returns [threat_score] normalized 0-1
   */
  private async threatHeuristic(input: Float32Array): Promise<Float32Array> {
    // Use a simple weighted combination of input features
    // Features would typically include: damage dealt, damage to self, CC applied, etc.

    // For placeholder, use average of inputs as base threat
    let threatSum = 0;
    for (let i = 0; i < input.length; i++) {
      threatSum += input[i] ?? 0;
    }
    const avgThreat = threatSum / (input.length || 1);

    // Scale to 0-1 range
    const threatScore = Math.max(0, Math.min(1, avgThreat));

    return new Float32Array([threatScore]);
  }

  /**
   * Check if a model is loaded
   */
  isLoaded(modelName: string, version: string): boolean {
    return this.loadedModels.has(`${modelName}-${version}`);
  }

  /**
   * Get a loaded model
   */
  getModel(modelName: string, version: string): LoadedModel | undefined {
    return this.loadedModels.get(`${modelName}-${version}`);
  }

  /**
   * Unload a model and free resources
   */
  unload(modelName: string, version: string): void {
    const key = `${modelName}-${version}`;
    const model = this.loadedModels.get(key);
    if (model) {
      model.dispose();
      this.loadedModels.delete(key);
    }
  }

  /**
   * Unload all models
   */
  unloadAll(): void {
    for (const model of this.loadedModels.values()) {
      model.dispose();
    }
    this.loadedModels.clear();
  }

  /**
   * Get list of currently loaded models
   */
  getLoadedModels(): ModelMetadata[] {
    return Array.from(this.loadedModels.values()).map((m) => m.metadata);
  }
}
