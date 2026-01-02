/**
 * Machine Learning Types and Interfaces
 *
 * All type definitions for the ML prediction module.
 * Inference runs locally using TensorFlow.js - no cloud APIs required.
 */

import type { Entity, CombatEvent } from '../types/index.js';
import type { CombatSession } from '../analysis/types/index.js';

// ============================================================================
// Configuration Types
// ============================================================================

/**
 * Configuration for the ML module
 */
export interface MLConfig {
  /** Enable/disable ML features (default: true) */
  enabled: boolean;
  /** Path to model weights directory (default: bundled weights) */
  weightsPath: string;
  /** Lazy load models on first use (default: true) */
  lazyLoad: boolean;
  /** Prediction interval for real-time mode in ms (default: 5000) */
  predictionIntervalMs: number;
  /** Minimum events required for predictions (default: 10) */
  minEventsThreshold: number;
  /** Minimum confidence to return predictions (default: 0.3) */
  minConfidenceThreshold: number;
}

/**
 * Partial ML config for user overrides
 */
export type PartialMLConfig = Partial<MLConfig>;

/**
 * Model metadata stored with weights
 */
export interface ModelMetadata {
  /** Model identifier */
  name: string;
  /** Semantic version */
  version: string;
  /** Input tensor shape */
  inputShape: number[];
  /** Output tensor shape */
  outputShape: number[];
  /** Ordered feature names for input */
  featureNames: string[];
  /** ISO date string when model was trained */
  trainedOn: string;
  /** Test accuracy if available */
  accuracy?: number;
  /** F1 score if available */
  f1Score?: number;
  /** Whether this is a heuristic placeholder */
  isPlaceholder?: boolean;
}

// ============================================================================
// Feature Vector Types
// ============================================================================

/**
 * Normalized feature vector ready for model input
 */
export interface FeatureVector {
  /** Ordered feature names for debugging/explainability */
  featureNames: string[];
  /** Normalized numeric values */
  values: Float32Array;
  /** Original unnormalized values (for debugging) */
  rawValues?: Record<string, number>;
}

/**
 * Normalization statistics for a feature
 */
export interface NormalizationStats {
  min: number;
  max: number;
  mean: number;
  std: number;
}

/**
 * Complete normalization config for all features
 */
export type NormalizationConfig = Record<string, NormalizationStats>;

// ============================================================================
// Combat State Features
// ============================================================================

/**
 * Combat state features extracted at a point in time during a fight
 * Total: 32 features
 */
export interface CombatStateFeatures {
  // Time-based (2)
  /** Elapsed time in milliseconds */
  elapsedTimeMs: number;
  /** Elapsed time ratio vs typical fight duration */
  elapsedTimeRatio: number;

  // Self damage features (5)
  /** Total damage dealt by self */
  selfDamageDealt: number;
  /** Total damage taken by self */
  selfDamageTaken: number;
  /** Damage ratio: dealt / (dealt + taken) */
  selfDamageRatio: number;
  /** Damage per second */
  selfDps: number;
  /** Damage taken per second */
  selfDtps: number;

  // Self healing features (3)
  /** Total healing done by self */
  selfHealingDone: number;
  /** Total healing received by self */
  selfHealingReceived: number;
  /** Net health: healing received - damage taken */
  selfNetHealth: number;

  // Combat efficiency (4)
  /** Critical hit rate (0-1) */
  selfCritRate: number;
  /** Block rate (0-1) */
  selfBlockRate: number;
  /** Parry rate (0-1) */
  selfParryRate: number;
  /** Evade rate (0-1) */
  selfEvadeRate: number;

  // Crowd control features (4)
  /** Number of CC effects applied */
  selfCCApplied: number;
  /** Number of CC effects received */
  selfCCReceived: number;
  /** Total duration of CC applied (seconds) */
  selfCCDurationApplied: number;
  /** Total duration of CC received (seconds) */
  selfCCDurationReceived: number;

  // Kill/Death (2)
  /** Number of kills */
  selfKills: number;
  /** Number of deaths */
  selfDeaths: number;

  // Opponent aggregates (4)
  /** Number of opponents */
  opponentCount: number;
  /** Total damage dealt by opponents */
  opponentTotalDamageDealt: number;
  /** Total healing done by opponents */
  opponentTotalHealingDone: number;
  /** Number of opponent deaths */
  opponentDeaths: number;

  // Ally aggregates (4)
  /** Number of allies */
  allyCount: number;
  /** Total damage dealt by allies */
  allyTotalDamageDealt: number;
  /** Total healing done by allies */
  allyTotalHealingDone: number;
  /** Number of ally deaths */
  allyDeaths: number;

  // Context (4)
  /** Total event count analyzed */
  eventCount: number;
  /** Is this a PvP fight */
  isPvP: number;
  /** Number of unique damage sources */
  uniqueDamageSources: number;
  /** Number of unique healing sources */
  uniqueHealingSources: number;
}

// ============================================================================
// Behavior Features
// ============================================================================

/**
 * Behavior features extracted from action patterns
 * Total: 24 features
 */
export interface BehaviorFeatures {
  // Action distribution (4)
  /** Ratio of melee attacks */
  meleeRatio: number;
  /** Ratio of spell attacks */
  spellRatio: number;
  /** Ratio of style attacks */
  styleRatio: number;
  /** Ratio of proc/DoT damage */
  procRatio: number;

  // Target selection (4)
  /** Target switches per minute */
  targetSwitchFrequency: number;
  /** Focus score: how much damage on primary target (0-1) */
  targetFocusScore: number;
  /** Preference for low-health targets (0-1) */
  lowHealthTargetPreference: number;
  /** Number of unique targets attacked */
  uniqueTargetsAttacked: number;

  // Damage patterns (4)
  /** Burst damage events per minute */
  burstDamageFrequency: number;
  /** Ratio of sustained vs burst damage */
  sustainedDamageRatio: number;
  /** Peak DPS achieved */
  peakDps: number;
  /** Variance in damage output */
  damageVariance: number;

  // Defensive behavior (4)
  /** Defensive ability usage ratio */
  defensiveAbilityUsage: number;
  /** Self-healing ratio */
  selfHealingRatio: number;
  /** Average time between damage received */
  avgTimeBetweenHits: number;
  /** Damage mitigation ratio (blocked/parried/evaded) */
  mitigationRatio: number;

  // Reaction patterns (4)
  /** Average time to respond to incoming damage */
  avgReactionTimeMs: number;
  /** CC reaction speed (time to act after CC ends) */
  ccReactionTimeMs: number;
  /** Kill confirm rate (finishing low targets) */
  killConfirmRate: number;
  /** Assist rate (damage to targets others are attacking) */
  assistRate: number;

  // Composite scores (4)
  /** Aggression score (0-100) */
  aggressionScore: number;
  /** Survivability score (0-100) */
  survivabilityScore: number;
  /** Team play score (0-100) */
  teamPlayScore: number;
  /** Overall efficiency score (0-100) */
  efficiencyScore: number;
}

// ============================================================================
// Player History Features
// ============================================================================

/**
 * Historical player features for context-aware predictions
 */
export interface PlayerHistoryFeatures {
  // Performance trends (4)
  /** Recent average DPS */
  recentAvgDps: number;
  /** Recent average HPS */
  recentAvgHps: number;
  /** Recent average KDR */
  recentAvgKdr: number;
  /** Recent average performance score */
  recentAvgPerformance: number;

  // Variability (2)
  /** DPS variance across sessions */
  dpsVariance: number;
  /** Performance variance across sessions */
  performanceVariance: number;

  // Win/loss (2)
  /** Recent win rate (if tracked) */
  recentWinRate: number;
  /** Session count used for history */
  sessionCount: number;

  // Role distribution (3)
  /** Ratio of sessions as damage dealer */
  ddRoleRatio: number;
  /** Ratio of sessions as healer */
  healerRoleRatio: number;
  /** Ratio of sessions as tank */
  tankRoleRatio: number;
}

// ============================================================================
// Prediction Input Types
// ============================================================================

/**
 * Input for fight outcome prediction
 */
export interface FightOutcomePredictionInput {
  /** Current combat state features */
  combatState: CombatStateFeatures;
  /** Player history (optional, improves accuracy) */
  playerHistory?: PlayerHistoryFeatures;
}

/**
 * Input for playstyle classification
 */
export interface PlaystyleClassificationInput {
  /** Behavior features from events */
  behavior: BehaviorFeatures;
  /** Combat state for context */
  combatState?: CombatStateFeatures;
}

/**
 * Input for performance prediction
 */
export interface PerformancePredictionInput {
  /** Player history features */
  playerHistory: PlayerHistoryFeatures;
  /** Current session context */
  sessionContext?: {
    fightDurationMs: number;
    participantCount: number;
    isPvP: boolean;
  };
}

/**
 * Input for threat assessment
 */
export interface ThreatAssessmentInput {
  /** Target entity to assess */
  target: Entity;
  /** Combat events involving this target */
  events: CombatEvent[];
  /** Current session context */
  session: CombatSession;
}

// ============================================================================
// Prediction Output Types
// ============================================================================

/**
 * Factor contributing to a prediction (for explainability)
 */
export interface PredictionFactor {
  /** Feature or factor name */
  name: string;
  /** Raw value */
  value: number;
  /** Impact direction */
  impact: 'POSITIVE' | 'NEGATIVE' | 'NEUTRAL';
  /** Contribution weight (0-1) */
  contribution: number;
  /** Human-readable description */
  description?: string;
}

/**
 * Fight outcome prediction result
 */
export interface FightOutcomePrediction {
  /** Probability of winning (0-1) */
  winProbability: number;
  /** Probability of losing (0-1) */
  lossProbability: number;
  /** Confidence in the prediction (0-1) */
  confidence: number;
  /** Key factors influencing prediction */
  factors: PredictionFactor[];
  /** Timestamp of prediction */
  timestamp: Date;
  /** Number of events analyzed */
  eventsAnalyzed: number;
  /** Whether this came from heuristics vs trained model */
  isHeuristic: boolean;
}

/**
 * Playstyle categories
 */
export type PlaystyleType = 'AGGRESSIVE' | 'DEFENSIVE' | 'BALANCED' | 'OPPORTUNISTIC';

/**
 * Playstyle trait detected
 */
export interface PlaystyleTrait {
  /** Trait name */
  name: string;
  /** Trait strength (0-1) */
  value: number;
  /** Human-readable description */
  description: string;
}

/**
 * Playstyle classification result
 */
export interface PlaystyleClassification {
  /** Primary playstyle */
  primaryStyle: PlaystyleType;
  /** Confidence scores for each style (0-1) */
  styleScores: Record<PlaystyleType, number>;
  /** Overall confidence in classification */
  confidence: number;
  /** Detected behavioral traits */
  traits: PlaystyleTrait[];
  /** Number of events analyzed */
  eventsAnalyzed: number;
  /** Whether this came from heuristics vs trained model */
  isHeuristic: boolean;
}

/**
 * Performance prediction result
 */
export interface PerformancePrediction {
  /** Predicted DPS */
  predictedDps: number;
  /** DPS confidence interval */
  dpsRange: { low: number; high: number };
  /** Predicted HPS (if applicable) */
  predictedHps?: number;
  /** HPS confidence interval */
  hpsRange?: { low: number; high: number };
  /** Predicted performance score (0-100) */
  predictedPerformance: number;
  /** Performance confidence interval */
  performanceRange: { low: number; high: number };
  /** Overall confidence */
  confidence: number;
  /** Sessions used for prediction */
  sessionsAnalyzed: number;
  /** Whether this came from heuristics vs trained model */
  isHeuristic: boolean;
}

/**
 * Threat categories
 */
export type ThreatCategory = 'LOW' | 'MEDIUM' | 'HIGH' | 'CRITICAL';

/**
 * Factor contributing to threat assessment
 */
export interface ThreatFactor {
  /** Factor name */
  name: string;
  /** Contribution to threat level (0-1) */
  contribution: number;
  /** Human-readable description */
  description: string;
}

/**
 * Threat assessment for a single entity
 */
export interface ThreatAssessment {
  /** Entity being assessed */
  entity: Entity;
  /** Threat level (0-100) */
  threatLevel: number;
  /** Threat category */
  threatCategory: ThreatCategory;
  /** Factors contributing to threat */
  factors: ThreatFactor[];
  /** Recommended actions */
  recommendations: string[];
  /** Whether this came from heuristics vs trained model */
  isHeuristic: boolean;
}

// ============================================================================
// Training Data Types
// ============================================================================

/**
 * Labeled training example for fight outcome
 */
export interface FightOutcomeTrainingExample {
  /** Combat state features at prediction time */
  features: CombatStateFeatures;
  /** Player history if available */
  playerHistory?: PlayerHistoryFeatures;
  /** Outcome label */
  label: 'WIN' | 'LOSS' | 'DRAW';
  /** Session ID for reference */
  sessionId: string;
  /** Time ratio when features were extracted (0-1) */
  timeRatio: number;
  /** Export timestamp */
  exportedAt: Date;
}

/**
 * Labeled training example for playstyle
 */
export interface PlaystyleTrainingExample {
  /** Behavior features */
  features: BehaviorFeatures;
  /** Playstyle label */
  label: PlaystyleType;
  /** Player name for reference */
  playerName: string;
  /** Session ID for reference */
  sessionId: string;
  /** Export timestamp */
  exportedAt: Date;
}

/**
 * Training dataset with metadata
 */
export interface TrainingDataset<T> {
  /** Training examples */
  examples: T[];
  /** Dataset metadata */
  metadata: TrainingDatasetMetadata;
}

/**
 * Training dataset metadata
 */
export interface TrainingDatasetMetadata {
  /** Export timestamp */
  exportedAt: Date;
  /** Number of examples */
  exampleCount: number;
  /** Feature extraction version */
  featureVersion: string;
  /** Label distribution */
  labelDistribution: Record<string, number>;
  /** Session IDs included */
  sessionIds: string[];
  /** Data source info */
  source?: string;
}

// ============================================================================
// Event Types for Real-time Integration
// ============================================================================

/**
 * ML prediction update event data
 */
export interface MLPredictionEvent {
  /** Prediction type */
  type: 'outcome' | 'playstyle' | 'performance' | 'threat';
  /** The prediction result */
  prediction: FightOutcomePrediction | PlaystyleClassification | PerformancePrediction | ThreatAssessment[];
  /** Session ID if applicable */
  sessionId?: string;
  /** Player name if applicable */
  playerName?: string;
}
