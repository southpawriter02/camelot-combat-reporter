/**
 * ML Predictor Facade
 *
 * Main entry point for all ML predictions.
 * Orchestrates loading of models and delegates to specialized predictors.
 */

import type { Entity } from '../../types/index.js';
import type { CombatSession } from '../../analysis/types/index.js';
import type {
  MLConfig,
  FightOutcomePrediction,
  PlaystyleClassification,
  PerformancePrediction,
  ThreatAssessment,
  ModelMetadata,
} from '../types.js';
import { DEFAULT_ML_CONFIG, MODEL_NAMES, MODEL_VERSIONS } from '../config.js';
import { FightOutcomePredictor } from './FightOutcomePredictor.js';
import { PlaystyleClassifier } from './PlaystyleClassifier.js';
import { PerformancePredictor } from './PerformancePredictor.js';
import { ThreatAssessor } from './ThreatAssessor.js';
import { defaultModelRegistry } from '../models/index.js';

/**
 * Main ML prediction facade
 *
 * Provides a unified interface for all ML predictions.
 * Models are loaded lazily on first use unless preloaded.
 *
 * @example
 * ```typescript
 * const predictor = new MLPredictor();
 *
 * // Predictions load models lazily
 * const outcome = await predictor.predictFightOutcome(session, 'PlayerName');
 * console.log(`Win probability: ${outcome.winProbability * 100}%`);
 *
 * // Or preload all models
 * await predictor.loadModels();
 * ```
 */
export class MLPredictor {
  private config: MLConfig;
  private fightOutcomePredictor: FightOutcomePredictor;
  private playstyleClassifier: PlaystyleClassifier;
  private performancePredictor: PerformancePredictor;
  private threatAssessor: ThreatAssessor;
  private modelsLoaded = false;

  constructor(config: Partial<MLConfig> = {}) {
    this.config = { ...DEFAULT_ML_CONFIG, ...config };

    // Initialize predictors with config
    this.fightOutcomePredictor = new FightOutcomePredictor(this.config);
    this.playstyleClassifier = new PlaystyleClassifier(this.config);
    this.performancePredictor = new PerformancePredictor(this.config);
    this.threatAssessor = new ThreatAssessor(this.config);
  }

  /**
   * Check if ML features are enabled
   */
  get isEnabled(): boolean {
    return this.config.enabled;
  }

  /**
   * Check if models are loaded
   */
  get isLoaded(): boolean {
    return this.modelsLoaded;
  }

  /**
   * Get the current configuration
   */
  getConfig(): MLConfig {
    return { ...this.config };
  }

  /**
   * Preload all ML models
   *
   * Call this at startup to avoid latency on first prediction.
   * If lazyLoad is true (default), models load on first use instead.
   */
  async loadModels(): Promise<void> {
    if (!this.config.enabled) {
      return;
    }

    if (this.modelsLoaded) {
      return;
    }

    // Load all predictors in parallel
    await Promise.all([
      this.fightOutcomePredictor.load(),
      this.playstyleClassifier.load(),
      this.performancePredictor.load(),
      this.threatAssessor.load(),
    ]);

    this.modelsLoaded = true;
  }

  /**
   * Unload all models and free resources
   */
  unloadModels(): void {
    this.fightOutcomePredictor.unload();
    this.playstyleClassifier.unload();
    this.performancePredictor.unload();
    this.threatAssessor.unload();
    this.modelsLoaded = false;
  }

  /**
   * Get metadata for all available models
   */
  getAvailableModels(): ModelMetadata[] {
    return defaultModelRegistry.toMetadata();
  }

  /**
   * Predict fight outcome (win/loss probability)
   *
   * @param session - Combat session to analyze
   * @param playerName - Name of the player to predict for
   * @returns Fight outcome prediction with confidence and contributing factors
   *
   * @example
   * ```typescript
   * const prediction = await predictor.predictFightOutcome(session, 'Legolas');
   * if (prediction.winProbability > 0.7) {
   *   console.log('Looking good!');
   * }
   * ```
   */
  async predictFightOutcome(
    session: CombatSession,
    playerName: string
  ): Promise<FightOutcomePrediction> {
    if (!this.config.enabled) {
      return this.getDisabledOutcomePrediction();
    }

    // Check minimum events threshold
    if (session.events.length < this.config.minEventsThreshold) {
      return this.getInsufficientDataOutcomePrediction(session.events.length);
    }

    return this.fightOutcomePredictor.predict(session, playerName);
  }

  /**
   * Classify player's playstyle
   *
   * @param session - Combat session to analyze
   * @param playerName - Name of the player to classify
   * @returns Playstyle classification with style scores and traits
   *
   * @example
   * ```typescript
   * const style = await predictor.classifyPlaystyle(session, 'Gimli');
   * console.log(`Primary style: ${style.primaryStyle}`);
   * ```
   */
  async classifyPlaystyle(
    session: CombatSession,
    playerName: string
  ): Promise<PlaystyleClassification> {
    if (!this.config.enabled) {
      return this.getDisabledPlaystyleClassification();
    }

    // Check minimum events threshold
    if (session.events.length < this.config.minEventsThreshold) {
      return this.getInsufficientDataPlaystyleClassification(session.events.length);
    }

    return this.playstyleClassifier.classify(session, playerName);
  }

  /**
   * Predict expected performance based on history
   *
   * @param sessions - Historical combat sessions
   * @param playerName - Name of the player
   * @returns Performance prediction with DPS/HPS estimates
   *
   * @example
   * ```typescript
   * const performance = await predictor.predictPerformance(historySessions, 'Aragorn');
   * console.log(`Expected DPS: ${performance.predictedDps}`);
   * ```
   */
  async predictPerformance(
    sessions: CombatSession[],
    playerName: string
  ): Promise<PerformancePrediction> {
    if (!this.config.enabled) {
      return this.getDisabledPerformancePrediction();
    }

    // Need at least some historical sessions
    if (sessions.length === 0) {
      return this.getInsufficientDataPerformancePrediction(0);
    }

    return this.performancePredictor.predict(sessions, playerName);
  }

  /**
   * Assess threats from all enemies in a session
   *
   * @param session - Combat session to analyze
   * @param selfEntity - The player entity to assess threats for
   * @returns Array of threat assessments sorted by threat level (highest first)
   *
   * @example
   * ```typescript
   * const threats = await predictor.assessThreats(session, playerEntity);
   * const primaryThreat = threats[0];
   * console.log(`Focus: ${primaryThreat.entity.name} (${primaryThreat.threatCategory})`);
   * ```
   */
  async assessThreats(session: CombatSession, selfEntity: Entity): Promise<ThreatAssessment[]> {
    if (!this.config.enabled) {
      return [];
    }

    // Check minimum events threshold
    if (session.events.length < this.config.minEventsThreshold) {
      return [];
    }

    return this.threatAssessor.assessAll(session, selfEntity);
  }

  /**
   * Assess threat for a single target entity
   *
   * @param session - Combat session
   * @param selfEntity - The player
   * @param targetEntity - The entity to assess
   * @returns Threat assessment for the target
   */
  async assessSingleThreat(
    session: CombatSession,
    selfEntity: Entity,
    targetEntity: Entity
  ): Promise<ThreatAssessment> {
    if (!this.config.enabled) {
      return this.getDisabledThreatAssessment(targetEntity);
    }

    return this.threatAssessor.assessSingle(session, selfEntity, targetEntity);
  }

  // ============================================================================
  // Private Helpers - Disabled/Insufficient Data Responses
  // ============================================================================

  private getDisabledOutcomePrediction(): FightOutcomePrediction {
    return {
      winProbability: 0.5,
      lossProbability: 0.5,
      confidence: 0,
      factors: [],
      timestamp: new Date(),
      eventsAnalyzed: 0,
      isHeuristic: true,
    };
  }

  private getInsufficientDataOutcomePrediction(eventCount: number): FightOutcomePrediction {
    return {
      winProbability: 0.5,
      lossProbability: 0.5,
      confidence: 0,
      factors: [
        {
          name: 'Insufficient Data',
          value: eventCount,
          impact: 'NEUTRAL',
          contribution: 0,
          description: `Need at least ${this.config.minEventsThreshold} events for prediction`,
        },
      ],
      timestamp: new Date(),
      eventsAnalyzed: eventCount,
      isHeuristic: true,
    };
  }

  private getDisabledPlaystyleClassification(): PlaystyleClassification {
    return {
      primaryStyle: 'BALANCED',
      styleScores: {
        AGGRESSIVE: 0.25,
        DEFENSIVE: 0.25,
        BALANCED: 0.25,
        OPPORTUNISTIC: 0.25,
      },
      confidence: 0,
      traits: [],
      eventsAnalyzed: 0,
      isHeuristic: true,
    };
  }

  private getInsufficientDataPlaystyleClassification(eventCount: number): PlaystyleClassification {
    return {
      primaryStyle: 'BALANCED',
      styleScores: {
        AGGRESSIVE: 0.25,
        DEFENSIVE: 0.25,
        BALANCED: 0.25,
        OPPORTUNISTIC: 0.25,
      },
      confidence: 0,
      traits: [],
      eventsAnalyzed: eventCount,
      isHeuristic: true,
    };
  }

  private getDisabledPerformancePrediction(): PerformancePrediction {
    return {
      predictedDps: 0,
      dpsRange: { low: 0, high: 0 },
      predictedHps: 0,
      hpsRange: { low: 0, high: 0 },
      predictedPerformance: 0,
      performanceRange: { low: 0, high: 0 },
      confidence: 0,
      sessionsAnalyzed: 0,
      isHeuristic: true,
    };
  }

  private getInsufficientDataPerformancePrediction(sessionCount: number): PerformancePrediction {
    return {
      predictedDps: 0,
      dpsRange: { low: 0, high: 0 },
      predictedHps: 0,
      hpsRange: { low: 0, high: 0 },
      predictedPerformance: 0,
      performanceRange: { low: 0, high: 0 },
      confidence: 0,
      sessionsAnalyzed: sessionCount,
      isHeuristic: true,
    };
  }

  private getDisabledThreatAssessment(entity: Entity): ThreatAssessment {
    return {
      entity,
      threatLevel: 0,
      threatCategory: 'LOW',
      factors: [],
      recommendations: [],
      isHeuristic: true,
    };
  }
}
