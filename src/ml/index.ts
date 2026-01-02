/**
 * Machine Learning Module
 *
 * Local ML predictions for combat analysis.
 * All inference runs locally using TensorFlow.js - no cloud APIs required.
 *
 * @module ml
 *
 * @example
 * ```typescript
 * import { MLPredictor } from 'camelot-combat-reporter';
 *
 * const predictor = new MLPredictor();
 *
 * // Fight outcome prediction
 * const outcome = await predictor.predictFightOutcome(session, 'PlayerName');
 * console.log(`Win probability: ${outcome.winProbability * 100}%`);
 *
 * // Playstyle classification
 * const style = await predictor.classifyPlaystyle(session, 'PlayerName');
 * console.log(`Primary style: ${style.primaryStyle}`);
 *
 * // Threat assessment
 * const threats = await predictor.assessThreats(session, selfEntity);
 * for (const threat of threats) {
 *   console.log(`${threat.entity.name}: ${threat.threatCategory}`);
 * }
 * ```
 */

// Main facade
export { MLPredictor } from './predictors/index.js';

// Configuration
export { DEFAULT_ML_CONFIG, MODEL_NAMES, MODEL_VERSIONS } from './config.js';

// Types - Configuration
export type { MLConfig, PartialMLConfig, ModelMetadata } from './types.js';

// Types - Features
export type {
  FeatureVector,
  NormalizationStats,
  NormalizationConfig,
  CombatStateFeatures,
  BehaviorFeatures,
  PlayerHistoryFeatures,
} from './types.js';

// Types - Prediction Inputs
export type {
  FightOutcomePredictionInput,
  PlaystyleClassificationInput,
  PerformancePredictionInput,
  ThreatAssessmentInput,
} from './types.js';

// Types - Prediction Outputs
export type {
  PredictionFactor,
  FightOutcomePrediction,
  PlaystyleType,
  PlaystyleTrait,
  PlaystyleClassification,
  PerformancePrediction,
  ThreatCategory,
  ThreatFactor,
  ThreatAssessment,
} from './types.js';

// Types - Training Data
export type {
  FightOutcomeTrainingExample,
  PlaystyleTrainingExample,
  TrainingDataset,
  TrainingDatasetMetadata,
} from './types.js';

// Types - Events
export type { MLPredictionEvent } from './types.js';

// Feature extraction (for advanced users)
export {
  FeatureExtractor,
  DEFAULT_FEATURE_EXTRACTOR_CONFIG,
  type FeatureExtractorConfig,
} from './features/index.js';
export { Normalizer, type NormalizationMethod } from './features/index.js';
export { CombatFeaturesExtractor } from './features/index.js';
export { BehaviorFeaturesExtractor } from './features/index.js';

// Model management (for advanced users)
export { ModelLoader, type LoadedModel, type ModelLoadOptions } from './models/index.js';
export {
  ModelRegistry,
  defaultModelRegistry,
  type ModelRegistryEntry,
} from './models/index.js';

// Individual predictors (for advanced users)
export { FightOutcomePredictor } from './predictors/index.js';
export { PlaystyleClassifier } from './predictors/index.js';
export { PerformancePredictor } from './predictors/index.js';
export { ThreatAssessor } from './predictors/index.js';

// Training data export
export {
  TrainingDataExporter,
  resolveOutcomeByDeaths,
  resolveOutcomeByDamageRatio,
  type TrainingExportConfig,
  type TrainingDataStats,
  type OutcomeLabel,
} from './training/index.js';
