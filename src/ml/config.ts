/**
 * ML Module Configuration
 *
 * Default configuration and constants for the ML prediction module.
 */

import type { MLConfig, NormalizationConfig } from './types.js';

/**
 * Default ML configuration
 */
export const DEFAULT_ML_CONFIG: MLConfig = {
  enabled: true,
  weightsPath: './ml/weights', // Relative path, can be overridden
  lazyLoad: true,
  predictionIntervalMs: 5000,
  minEventsThreshold: 10,
  minConfidenceThreshold: 0.3,
};

/**
 * Typical fight duration in milliseconds (used for normalization)
 */
export const TYPICAL_FIGHT_DURATION_MS = 60000; // 1 minute

/**
 * Threshold for "big hit" detection
 */
export const BIG_HIT_THRESHOLD = 500;

/**
 * Threshold for "big heal" detection
 */
export const BIG_HEAL_THRESHOLD = 500;

/**
 * Rolling window for peak DPS/HPS calculation (ms)
 */
export const ROLLING_WINDOW_MS = 5000;

/**
 * Model names/identifiers
 */
export const MODEL_NAMES = {
  FIGHT_OUTCOME: 'fight-outcome',
  PLAYSTYLE: 'playstyle',
  PERFORMANCE: 'performance',
  THREAT: 'threat',
} as const;

/**
 * Model versions (for versioned weights directories)
 */
export const MODEL_VERSIONS = {
  [MODEL_NAMES.FIGHT_OUTCOME]: 'v1',
  [MODEL_NAMES.PLAYSTYLE]: 'v1',
  [MODEL_NAMES.PERFORMANCE]: 'v1',
  [MODEL_NAMES.THREAT]: 'v1',
} as const;

/**
 * Feature names for combat state features (order matters for model input)
 */
export const COMBAT_STATE_FEATURE_NAMES: readonly string[] = [
  'elapsedTimeMs',
  'elapsedTimeRatio',
  'selfDamageDealt',
  'selfDamageTaken',
  'selfDamageRatio',
  'selfDps',
  'selfDtps',
  'selfHealingDone',
  'selfHealingReceived',
  'selfNetHealth',
  'selfCritRate',
  'selfBlockRate',
  'selfParryRate',
  'selfEvadeRate',
  'selfCCApplied',
  'selfCCReceived',
  'selfCCDurationApplied',
  'selfCCDurationReceived',
  'selfKills',
  'selfDeaths',
  'opponentCount',
  'opponentTotalDamageDealt',
  'opponentTotalHealingDone',
  'opponentDeaths',
  'allyCount',
  'allyTotalDamageDealt',
  'allyTotalHealingDone',
  'allyDeaths',
  'eventCount',
  'isPvP',
  'uniqueDamageSources',
  'uniqueHealingSources',
] as const;

/**
 * Feature names for behavior features (order matters for model input)
 */
export const BEHAVIOR_FEATURE_NAMES: readonly string[] = [
  'meleeRatio',
  'spellRatio',
  'styleRatio',
  'procRatio',
  'targetSwitchFrequency',
  'targetFocusScore',
  'lowHealthTargetPreference',
  'uniqueTargetsAttacked',
  'burstDamageFrequency',
  'sustainedDamageRatio',
  'peakDps',
  'damageVariance',
  'defensiveAbilityUsage',
  'selfHealingRatio',
  'avgTimeBetweenHits',
  'mitigationRatio',
  'avgReactionTimeMs',
  'ccReactionTimeMs',
  'killConfirmRate',
  'assistRate',
  'aggressionScore',
  'survivabilityScore',
  'teamPlayScore',
  'efficiencyScore',
] as const;

/**
 * Default normalization stats for combat state features
 * These are placeholder values - should be computed from real training data
 */
export const DEFAULT_COMBAT_NORMALIZATION: NormalizationConfig = {
  elapsedTimeMs: { min: 0, max: 300000, mean: 60000, std: 60000 },
  elapsedTimeRatio: { min: 0, max: 5, mean: 1, std: 1 },
  selfDamageDealt: { min: 0, max: 100000, mean: 10000, std: 15000 },
  selfDamageTaken: { min: 0, max: 100000, mean: 8000, std: 12000 },
  selfDamageRatio: { min: 0, max: 1, mean: 0.5, std: 0.2 },
  selfDps: { min: 0, max: 2000, mean: 200, std: 150 },
  selfDtps: { min: 0, max: 2000, mean: 150, std: 120 },
  selfHealingDone: { min: 0, max: 50000, mean: 5000, std: 8000 },
  selfHealingReceived: { min: 0, max: 50000, mean: 4000, std: 6000 },
  selfNetHealth: { min: -50000, max: 50000, mean: 0, std: 10000 },
  selfCritRate: { min: 0, max: 1, mean: 0.15, std: 0.1 },
  selfBlockRate: { min: 0, max: 1, mean: 0.1, std: 0.1 },
  selfParryRate: { min: 0, max: 1, mean: 0.1, std: 0.1 },
  selfEvadeRate: { min: 0, max: 1, mean: 0.1, std: 0.1 },
  selfCCApplied: { min: 0, max: 20, mean: 2, std: 3 },
  selfCCReceived: { min: 0, max: 20, mean: 2, std: 3 },
  selfCCDurationApplied: { min: 0, max: 120, mean: 10, std: 15 },
  selfCCDurationReceived: { min: 0, max: 120, mean: 10, std: 15 },
  selfKills: { min: 0, max: 10, mean: 1, std: 1.5 },
  selfDeaths: { min: 0, max: 5, mean: 0.5, std: 0.8 },
  opponentCount: { min: 1, max: 20, mean: 3, std: 3 },
  opponentTotalDamageDealt: { min: 0, max: 200000, mean: 20000, std: 30000 },
  opponentTotalHealingDone: { min: 0, max: 100000, mean: 10000, std: 15000 },
  opponentDeaths: { min: 0, max: 20, mean: 2, std: 3 },
  allyCount: { min: 0, max: 20, mean: 2, std: 3 },
  allyTotalDamageDealt: { min: 0, max: 200000, mean: 15000, std: 25000 },
  allyTotalHealingDone: { min: 0, max: 100000, mean: 8000, std: 12000 },
  allyDeaths: { min: 0, max: 10, mean: 1, std: 2 },
  eventCount: { min: 10, max: 1000, mean: 100, std: 150 },
  isPvP: { min: 0, max: 1, mean: 0.5, std: 0.5 },
  uniqueDamageSources: { min: 1, max: 20, mean: 3, std: 3 },
  uniqueHealingSources: { min: 0, max: 10, mean: 1, std: 2 },
};

/**
 * Default normalization stats for behavior features
 * These are placeholder values - should be computed from real training data
 */
export const DEFAULT_BEHAVIOR_NORMALIZATION: NormalizationConfig = {
  meleeRatio: { min: 0, max: 1, mean: 0.4, std: 0.3 },
  spellRatio: { min: 0, max: 1, mean: 0.3, std: 0.3 },
  styleRatio: { min: 0, max: 1, mean: 0.2, std: 0.2 },
  procRatio: { min: 0, max: 1, mean: 0.1, std: 0.1 },
  targetSwitchFrequency: { min: 0, max: 30, mean: 5, std: 5 },
  targetFocusScore: { min: 0, max: 1, mean: 0.6, std: 0.2 },
  lowHealthTargetPreference: { min: 0, max: 1, mean: 0.3, std: 0.2 },
  uniqueTargetsAttacked: { min: 1, max: 20, mean: 3, std: 3 },
  burstDamageFrequency: { min: 0, max: 20, mean: 3, std: 3 },
  sustainedDamageRatio: { min: 0, max: 1, mean: 0.6, std: 0.2 },
  peakDps: { min: 0, max: 3000, mean: 400, std: 300 },
  damageVariance: { min: 0, max: 10000, mean: 1000, std: 1500 },
  defensiveAbilityUsage: { min: 0, max: 1, mean: 0.2, std: 0.2 },
  selfHealingRatio: { min: 0, max: 1, mean: 0.1, std: 0.15 },
  avgTimeBetweenHits: { min: 0, max: 10000, mean: 2000, std: 2000 },
  mitigationRatio: { min: 0, max: 1, mean: 0.2, std: 0.15 },
  avgReactionTimeMs: { min: 0, max: 5000, mean: 1000, std: 800 },
  ccReactionTimeMs: { min: 0, max: 5000, mean: 500, std: 500 },
  killConfirmRate: { min: 0, max: 1, mean: 0.5, std: 0.3 },
  assistRate: { min: 0, max: 1, mean: 0.3, std: 0.2 },
  aggressionScore: { min: 0, max: 100, mean: 50, std: 20 },
  survivabilityScore: { min: 0, max: 100, mean: 50, std: 20 },
  teamPlayScore: { min: 0, max: 100, mean: 50, std: 20 },
  efficiencyScore: { min: 0, max: 100, mean: 50, std: 20 },
};

/**
 * Playstyle thresholds for heuristic classification
 */
export const PLAYSTYLE_THRESHOLDS = {
  /** Aggression score threshold for AGGRESSIVE */
  aggressiveThreshold: 65,
  /** Aggression score threshold for DEFENSIVE (below this) */
  defensiveThreshold: 35,
  /** Target switch frequency threshold for OPPORTUNISTIC */
  opportunisticSwitchFrequency: 8,
  /** Focus score threshold for OPPORTUNISTIC (below this) */
  opportunisticFocusThreshold: 0.4,
} as const;

/**
 * Threat level thresholds
 */
export const THREAT_THRESHOLDS = {
  /** Score for LOW threat (0-25) */
  low: 25,
  /** Score for MEDIUM threat (25-50) */
  medium: 50,
  /** Score for HIGH threat (50-75) */
  high: 75,
  /** Score for CRITICAL threat (75-100) */
  critical: 100,
} as const;
