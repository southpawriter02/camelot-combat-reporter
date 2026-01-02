/**
 * Training Data Module
 *
 * Exports utilities for generating training data for ML models.
 */

export {
  TrainingDataExporter,
  resolveOutcomeByDeaths,
  resolveOutcomeByDamageRatio,
  type TrainingExportConfig,
  type TrainingDataStats,
  type OutcomeLabel,
} from './TrainingDataExporter.js';
