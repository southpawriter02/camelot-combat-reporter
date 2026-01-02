/**
 * Feature Extractor
 *
 * Main orchestrator for extracting and normalizing features from combat data.
 * Combines combat state and behavior feature extraction with normalization.
 */

import type { CombatEvent, Entity } from '../../types/index.js';
import type { CombatSession } from '../../analysis/types/index.js';
import type {
  CombatStateFeatures,
  BehaviorFeatures,
  PlayerHistoryFeatures,
  FeatureVector,
} from '../types.js';
import { CombatFeaturesExtractor } from './CombatFeatures.js';
import { BehaviorFeaturesExtractor } from './BehaviorFeatures.js';
import { Normalizer, type NormalizationMethod } from './Normalizer.js';
import {
  COMBAT_STATE_FEATURE_NAMES,
  BEHAVIOR_FEATURE_NAMES,
  DEFAULT_COMBAT_NORMALIZATION,
  DEFAULT_BEHAVIOR_NORMALIZATION,
} from '../config.js';

/**
 * Configuration for feature extraction
 */
export interface FeatureExtractorConfig {
  /** Normalization method */
  normalizationMethod: NormalizationMethod;
  /** Whether to include raw values in output */
  includeRawValues: boolean;
}

/**
 * Default feature extractor configuration
 */
export const DEFAULT_FEATURE_EXTRACTOR_CONFIG: FeatureExtractorConfig = {
  normalizationMethod: 'minmax',
  includeRawValues: true,
};

/**
 * Main feature extraction orchestrator
 */
export class FeatureExtractor {
  private combatExtractor: CombatFeaturesExtractor;
  private behaviorExtractor: BehaviorFeaturesExtractor;
  private combatNormalizer: Normalizer;
  private behaviorNormalizer: Normalizer;
  private config: FeatureExtractorConfig;

  constructor(config: Partial<FeatureExtractorConfig> = {}) {
    this.config = { ...DEFAULT_FEATURE_EXTRACTOR_CONFIG, ...config };

    this.combatExtractor = new CombatFeaturesExtractor();
    this.behaviorExtractor = new BehaviorFeaturesExtractor();

    // Initialize normalizers with default stats
    this.combatNormalizer = new Normalizer(
      this.config.normalizationMethod,
      DEFAULT_COMBAT_NORMALIZATION
    );
    this.behaviorNormalizer = new Normalizer(
      this.config.normalizationMethod,
      DEFAULT_BEHAVIOR_NORMALIZATION
    );
  }

  /**
   * Extract combat state features from events
   *
   * @param events - Combat events
   * @param selfEntity - The player entity
   * @param durationMs - Fight duration in ms
   * @returns Raw combat state features
   */
  extractCombatState(
    events: CombatEvent[],
    selfEntity: Entity,
    durationMs: number
  ): CombatStateFeatures {
    return this.combatExtractor.extract(events, selfEntity, durationMs);
  }

  /**
   * Extract behavior features from events
   *
   * @param events - Combat events
   * @param selfEntity - The player entity
   * @param durationMs - Fight duration in ms
   * @returns Raw behavior features
   */
  extractBehavior(
    events: CombatEvent[],
    selfEntity: Entity,
    durationMs: number
  ): BehaviorFeatures {
    return this.behaviorExtractor.extract(events, selfEntity, durationMs);
  }

  /**
   * Extract and normalize combat state features to a feature vector
   *
   * @param events - Combat events
   * @param selfEntity - The player entity
   * @param durationMs - Fight duration in ms
   * @returns Normalized feature vector
   */
  extractCombatStateVector(
    events: CombatEvent[],
    selfEntity: Entity,
    durationMs: number
  ): FeatureVector {
    const features = this.extractCombatState(events, selfEntity, durationMs);
    const flatFeatures = this.combatExtractor.toFlatObject(features);

    const vector = this.combatNormalizer.normalize(flatFeatures, COMBAT_STATE_FEATURE_NAMES);

    if (!this.config.includeRawValues) {
      delete vector.rawValues;
    }

    return vector;
  }

  /**
   * Extract and normalize behavior features to a feature vector
   *
   * @param events - Combat events
   * @param selfEntity - The player entity
   * @param durationMs - Fight duration in ms
   * @returns Normalized feature vector
   */
  extractBehaviorVector(
    events: CombatEvent[],
    selfEntity: Entity,
    durationMs: number
  ): FeatureVector {
    const features = this.extractBehavior(events, selfEntity, durationMs);
    const flatFeatures = this.behaviorExtractor.toFlatObject(features);

    const vector = this.behaviorNormalizer.normalize(flatFeatures, BEHAVIOR_FEATURE_NAMES);

    if (!this.config.includeRawValues) {
      delete vector.rawValues;
    }

    return vector;
  }

  /**
   * Extract features from a combat session
   *
   * @param session - Combat session
   * @param playerName - Name of the player to extract features for
   * @returns Both combat state and behavior features
   */
  extractFromSession(
    session: CombatSession,
    playerName: string
  ): {
    combatState: CombatStateFeatures;
    behavior: BehaviorFeatures;
    combatStateVector: FeatureVector;
    behaviorVector: FeatureVector;
  } | null {
    // Find the player entity in the session
    const participant = session.participants.find((p) => p.entity.name === playerName);
    if (!participant) {
      return null;
    }

    const selfEntity = participant.entity;
    const durationMs = session.durationMs;
    const events = session.events;

    const combatState = this.extractCombatState(events, selfEntity, durationMs);
    const behavior = this.extractBehavior(events, selfEntity, durationMs);
    const combatStateVector = this.extractCombatStateVector(events, selfEntity, durationMs);
    const behaviorVector = this.extractBehaviorVector(events, selfEntity, durationMs);

    return {
      combatState,
      behavior,
      combatStateVector,
      behaviorVector,
    };
  }

  /**
   * Extract player history features from multiple sessions
   *
   * @param sessions - Array of combat sessions
   * @param playerName - Name of the player
   * @returns Player history features
   */
  extractPlayerHistory(sessions: CombatSession[], playerName: string): PlayerHistoryFeatures {
    if (sessions.length === 0) {
      return this.getDefaultPlayerHistory();
    }

    const sessionStats: {
      dps: number;
      hps: number;
      kdr: number;
      performance: number;
      role: string;
    }[] = [];

    for (const session of sessions) {
      const features = this.extractFromSession(session, playerName);
      if (!features) continue;

      const { combatState, behavior } = features;
      const durationSeconds = session.durationMs / 1000;

      // Calculate per-session stats
      const dps = combatState.selfDamageDealt / durationSeconds;
      const hps = combatState.selfHealingDone / durationSeconds;
      const deaths = Math.max(combatState.selfDeaths, 1);
      const kdr = combatState.selfKills / deaths;
      const performance = behavior.efficiencyScore;

      // Determine role heuristically
      let role = 'dd';
      if (combatState.selfHealingDone > combatState.selfDamageDealt * 0.5) {
        role = 'healer';
      } else if (
        combatState.selfDamageTaken > combatState.selfDamageDealt &&
        behavior.mitigationRatio > 0.3
      ) {
        role = 'tank';
      }

      sessionStats.push({ dps, hps, kdr, performance, role });
    }

    if (sessionStats.length === 0) {
      return this.getDefaultPlayerHistory();
    }

    // Calculate averages
    const avgDps = sessionStats.reduce((sum, s) => sum + s.dps, 0) / sessionStats.length;
    const avgHps = sessionStats.reduce((sum, s) => sum + s.hps, 0) / sessionStats.length;
    const avgKdr = sessionStats.reduce((sum, s) => sum + s.kdr, 0) / sessionStats.length;
    const avgPerformance =
      sessionStats.reduce((sum, s) => sum + s.performance, 0) / sessionStats.length;

    // Calculate variance
    const dpsVariance =
      sessionStats.reduce((sum, s) => sum + Math.pow(s.dps - avgDps, 2), 0) / sessionStats.length;
    const performanceVariance =
      sessionStats.reduce((sum, s) => sum + Math.pow(s.performance - avgPerformance, 2), 0) /
      sessionStats.length;

    // Calculate role distribution
    const roleCounts = { dd: 0, healer: 0, tank: 0 };
    for (const s of sessionStats) {
      if (s.role === 'healer') roleCounts.healer++;
      else if (s.role === 'tank') roleCounts.tank++;
      else roleCounts.dd++;
    }
    const total = sessionStats.length;

    return {
      recentAvgDps: avgDps,
      recentAvgHps: avgHps,
      recentAvgKdr: avgKdr,
      recentAvgPerformance: avgPerformance,
      dpsVariance,
      performanceVariance,
      recentWinRate: 0.5, // Would need win/loss tracking to compute
      sessionCount: sessions.length,
      ddRoleRatio: roleCounts.dd / total,
      healerRoleRatio: roleCounts.healer / total,
      tankRoleRatio: roleCounts.tank / total,
    };
  }

  /**
   * Get default player history (for new players with no data)
   */
  private getDefaultPlayerHistory(): PlayerHistoryFeatures {
    return {
      recentAvgDps: 200,
      recentAvgHps: 50,
      recentAvgKdr: 1.0,
      recentAvgPerformance: 50,
      dpsVariance: 100,
      performanceVariance: 25,
      recentWinRate: 0.5,
      sessionCount: 0,
      ddRoleRatio: 1,
      healerRoleRatio: 0,
      tankRoleRatio: 0,
    };
  }

  /**
   * Update normalizer with new statistics computed from data
   *
   * @param data - Array of feature objects
   * @param type - Type of features ('combat' or 'behavior')
   */
  updateNormalizationStats(
    data: Record<string, number>[],
    type: 'combat' | 'behavior'
  ): void {
    const featureNames =
      type === 'combat' ? COMBAT_STATE_FEATURE_NAMES : BEHAVIOR_FEATURE_NAMES;
    const stats = Normalizer.computeStats(data, featureNames);

    const normalizer = type === 'combat' ? this.combatNormalizer : this.behaviorNormalizer;
    normalizer.loadStats(stats);
  }

  /**
   * Get the combat normalizer (for testing/inspection)
   */
  getCombatNormalizer(): Normalizer {
    return this.combatNormalizer;
  }

  /**
   * Get the behavior normalizer (for testing/inspection)
   */
  getBehaviorNormalizer(): Normalizer {
    return this.behaviorNormalizer;
  }
}
