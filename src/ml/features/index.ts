/**
 * Feature Extraction Module
 *
 * Exports feature extraction classes and utilities for ML predictions.
 */

export { Normalizer, type NormalizationMethod } from './Normalizer.js';
export { CombatFeaturesExtractor } from './CombatFeatures.js';
export { BehaviorFeaturesExtractor } from './BehaviorFeatures.js';
export {
  FeatureExtractor,
  DEFAULT_FEATURE_EXTRACTOR_CONFIG,
  type FeatureExtractorConfig,
} from './FeatureExtractor.js';
