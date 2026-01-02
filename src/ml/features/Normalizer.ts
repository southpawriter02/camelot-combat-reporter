/**
 * Feature Normalization
 *
 * Normalizes raw feature values to a consistent range for model input.
 * Supports min-max scaling and z-score normalization.
 */

import type { FeatureVector, NormalizationConfig, NormalizationStats } from '../types.js';

/**
 * Normalization method type
 */
export type NormalizationMethod = 'minmax' | 'zscore' | 'none';

/**
 * Feature normalizer for ML model inputs
 */
export class Normalizer {
  private method: NormalizationMethod;
  private stats: Map<string, NormalizationStats>;
  private epsilon = 1e-8; // Small value to prevent division by zero

  /**
   * Create a new normalizer
   *
   * @param method - Normalization method: 'minmax' (0-1), 'zscore' (-∞ to +∞), or 'none'
   * @param stats - Pre-computed normalization statistics per feature
   */
  constructor(method: NormalizationMethod = 'minmax', stats?: NormalizationConfig) {
    this.method = method;
    this.stats = stats ? new Map(Object.entries(stats)) : new Map();
  }

  /**
   * Get the normalization method
   */
  getMethod(): NormalizationMethod {
    return this.method;
  }

  /**
   * Set normalization statistics for a feature
   */
  setStats(featureName: string, stats: NormalizationStats): void {
    this.stats.set(featureName, stats);
  }

  /**
   * Get normalization statistics for a feature
   */
  getStats(featureName: string): NormalizationStats | undefined {
    return this.stats.get(featureName);
  }

  /**
   * Check if stats exist for a feature
   */
  hasStats(featureName: string): boolean {
    return this.stats.has(featureName);
  }

  /**
   * Load stats from a configuration object
   */
  loadStats(config: NormalizationConfig): void {
    for (const [name, stats] of Object.entries(config)) {
      this.stats.set(name, stats);
    }
  }

  /**
   * Export stats as a configuration object
   */
  exportStats(): NormalizationConfig {
    const config: NormalizationConfig = {};
    for (const [name, stats] of this.stats.entries()) {
      config[name] = stats;
    }
    return config;
  }

  /**
   * Normalize a single value
   *
   * @param featureName - Name of the feature (for stats lookup)
   * @param value - Raw value to normalize
   * @returns Normalized value
   */
  normalizeValue(featureName: string, value: number): number {
    if (this.method === 'none') {
      return value;
    }

    const stats = this.stats.get(featureName);
    if (!stats) {
      // No stats available, return raw value
      return value;
    }

    if (this.method === 'minmax') {
      const range = stats.max - stats.min;
      if (range < this.epsilon) {
        return 0.5; // Constant feature
      }
      // Clamp to [0, 1] range
      const normalized = (value - stats.min) / range;
      return Math.max(0, Math.min(1, normalized));
    }

    if (this.method === 'zscore') {
      if (stats.std < this.epsilon) {
        return 0; // Constant feature
      }
      return (value - stats.mean) / stats.std;
    }

    return value;
  }

  /**
   * Denormalize a single value (inverse transform)
   *
   * @param featureName - Name of the feature
   * @param normalizedValue - Normalized value
   * @returns Original-scale value
   */
  denormalizeValue(featureName: string, normalizedValue: number): number {
    if (this.method === 'none') {
      return normalizedValue;
    }

    const stats = this.stats.get(featureName);
    if (!stats) {
      return normalizedValue;
    }

    if (this.method === 'minmax') {
      const range = stats.max - stats.min;
      return normalizedValue * range + stats.min;
    }

    if (this.method === 'zscore') {
      return normalizedValue * stats.std + stats.mean;
    }

    return normalizedValue;
  }

  /**
   * Normalize a feature object to a feature vector
   *
   * @param features - Object with feature name -> value mappings
   * @param featureOrder - Ordered list of feature names (determines output order)
   * @returns FeatureVector with normalized values in specified order
   */
  normalize(features: Record<string, number>, featureOrder: readonly string[]): FeatureVector {
    const values = new Float32Array(featureOrder.length);

    for (let i = 0; i < featureOrder.length; i++) {
      const name = featureOrder[i];
      if (name === undefined) continue;
      const rawValue = features[name] ?? 0;
      values[i] = this.normalizeValue(name, rawValue);
    }

    return {
      featureNames: [...featureOrder],
      values,
      rawValues: { ...features },
    };
  }

  /**
   * Normalize an array of feature objects (batch normalization)
   *
   * @param featuresArray - Array of feature objects
   * @param featureOrder - Ordered list of feature names
   * @returns Array of normalized feature vectors
   */
  normalizeBatch(
    featuresArray: Record<string, number>[],
    featureOrder: readonly string[]
  ): FeatureVector[] {
    return featuresArray.map((features) => this.normalize(features, featureOrder));
  }

  /**
   * Compute normalization statistics from a dataset
   * Useful for training data preprocessing
   *
   * @param data - Array of feature objects
   * @param featureNames - Names of features to compute stats for
   * @returns Computed normalization config
   */
  static computeStats(
    data: Record<string, number>[],
    featureNames: readonly string[]
  ): NormalizationConfig {
    const config: NormalizationConfig = {};

    for (const name of featureNames) {
      const values = data.map((d) => d[name] ?? 0).filter((v) => !isNaN(v) && isFinite(v));

      if (values.length === 0) {
        config[name] = { min: 0, max: 1, mean: 0.5, std: 0.5 };
        continue;
      }

      const min = Math.min(...values);
      const max = Math.max(...values);
      const mean = values.reduce((a, b) => a + b, 0) / values.length;
      const variance = values.reduce((sum, v) => sum + Math.pow(v - mean, 2), 0) / values.length;
      const std = Math.sqrt(variance);

      config[name] = { min, max, mean, std };
    }

    return config;
  }

  /**
   * Create a normalizer with pre-computed stats from training data
   *
   * @param data - Training data
   * @param featureNames - Feature names
   * @param method - Normalization method
   * @returns Configured normalizer
   */
  static fromTrainingData(
    data: Record<string, number>[],
    featureNames: readonly string[],
    method: NormalizationMethod = 'minmax'
  ): Normalizer {
    const stats = Normalizer.computeStats(data, featureNames);
    return new Normalizer(method, stats);
  }
}
