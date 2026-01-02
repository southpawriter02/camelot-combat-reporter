/**
 * Training Data Exporter
 *
 * Exports labeled training data from combat sessions for offline model training.
 * The exported JSON can be used with Python/TensorFlow for training actual models.
 */

import type { CombatSession } from '../../analysis/types/index.js';
import type { DamageEvent, DeathEvent } from '../../types/index.js';
import { EventType } from '../../types/index.js';
import type {
  CombatStateFeatures,
  BehaviorFeatures,
  FightOutcomeTrainingExample,
  PlaystyleTrainingExample,
  TrainingDataset,
  TrainingDatasetMetadata,
  PlaystyleType,
} from '../types.js';
import { FeatureExtractor } from '../features/index.js';

/**
 * Outcome label for training data
 */
export type OutcomeLabel = 'WIN' | 'LOSS' | 'DRAW';

/**
 * Configuration for training data export
 */
export interface TrainingExportConfig {
  /** Feature extraction time points (0-1 ratios of fight duration) */
  timePoints?: number[];
  /** Include player history features if available */
  includeHistory?: boolean;
  /** Feature extraction version for tracking */
  featureVersion?: string;
}

const DEFAULT_EXPORT_CONFIG: TrainingExportConfig = {
  timePoints: [0.25, 0.5, 0.75, 1.0],
  includeHistory: false,
  featureVersion: '1.0.0',
};

/**
 * Training data exporter for ML models
 *
 * Extracts features from combat sessions and exports them in a format
 * suitable for training ML models with Python/TensorFlow.
 *
 * @example
 * ```typescript
 * const exporter = new TrainingDataExporter();
 *
 * // Add labeled sessions
 * exporter.addOutcomeExample(session, 'PlayerName', 'WIN');
 * exporter.addPlaystyleExample(session, 'PlayerName', 'AGGRESSIVE');
 *
 * // Export to JSON
 * const outcomeData = exporter.exportOutcomeDataset();
 * const playstyleData = exporter.exportPlaystyleDataset();
 *
 * // Write to files for Python training
 * fs.writeFileSync('outcome_training.json', JSON.stringify(outcomeData, null, 2));
 * ```
 */
export class TrainingDataExporter {
  private config: TrainingExportConfig;
  private featureExtractor: FeatureExtractor;
  private outcomeExamples: FightOutcomeTrainingExample[] = [];
  private playstyleExamples: PlaystyleTrainingExample[] = [];

  constructor(config: Partial<TrainingExportConfig> = {}) {
    this.config = { ...DEFAULT_EXPORT_CONFIG, ...config };
    this.featureExtractor = new FeatureExtractor();
  }

  /**
   * Add a fight outcome training example
   *
   * @param session - Combat session
   * @param playerName - Player to extract features for
   * @param outcome - Labeled outcome (WIN, LOSS, DRAW)
   */
  addOutcomeExample(session: CombatSession, playerName: string, outcome: OutcomeLabel): void {
    const timePoints = this.config.timePoints || [1.0];

    for (const timeRatio of timePoints) {
      const features = this.extractFeaturesAtTime(session, playerName, timeRatio);

      if (features) {
        this.outcomeExamples.push({
          features,
          label: outcome,
          sessionId: session.id,
          timeRatio,
          exportedAt: new Date(),
        });
      }
    }
  }

  /**
   * Add a playstyle training example
   *
   * @param session - Combat session
   * @param playerName - Player to extract features for
   * @param playstyle - Labeled playstyle
   */
  addPlaystyleExample(
    session: CombatSession,
    playerName: string,
    playstyle: PlaystyleType
  ): void {
    const behaviorFeatures = this.extractBehaviorFeatures(session, playerName);

    if (behaviorFeatures) {
      this.playstyleExamples.push({
        features: behaviorFeatures,
        label: playstyle,
        playerName,
        sessionId: session.id,
        exportedAt: new Date(),
      });
    }
  }

  /**
   * Add multiple sessions with auto-labeled outcomes
   *
   * Automatically determines outcome based on kills/deaths.
   *
   * @param sessions - Combat sessions to add
   * @param playerName - Player name
   * @param outcomeResolver - Function to determine outcome from session
   */
  addSessionsWithAutoOutcome(
    sessions: CombatSession[],
    playerName: string,
    outcomeResolver: (session: CombatSession, playerName: string) => OutcomeLabel | null
  ): void {
    for (const session of sessions) {
      const outcome = outcomeResolver(session, playerName);
      if (outcome) {
        this.addOutcomeExample(session, playerName, outcome);
      }
    }
  }

  /**
   * Export fight outcome training dataset
   */
  exportOutcomeDataset(): TrainingDataset<FightOutcomeTrainingExample> {
    const labelDistribution: Record<string, number> = {
      WIN: 0,
      LOSS: 0,
      DRAW: 0,
    };

    const sessionIds = new Set<string>();

    for (const example of this.outcomeExamples) {
      const count = labelDistribution[example.label] ?? 0;
      labelDistribution[example.label] = count + 1;
      sessionIds.add(example.sessionId);
    }

    const metadata: TrainingDatasetMetadata = {
      exportedAt: new Date(),
      exampleCount: this.outcomeExamples.length,
      featureVersion: this.config.featureVersion || '1.0.0',
      labelDistribution,
      sessionIds: Array.from(sessionIds),
      source: 'camelot-combat-reporter',
    };

    return {
      examples: this.outcomeExamples,
      metadata,
    };
  }

  /**
   * Export playstyle training dataset
   */
  exportPlaystyleDataset(): TrainingDataset<PlaystyleTrainingExample> {
    const labelDistribution: Record<string, number> = {
      AGGRESSIVE: 0,
      DEFENSIVE: 0,
      BALANCED: 0,
      OPPORTUNISTIC: 0,
    };

    const sessionIds = new Set<string>();

    for (const example of this.playstyleExamples) {
      const count = labelDistribution[example.label] ?? 0;
      labelDistribution[example.label] = count + 1;
      sessionIds.add(example.sessionId);
    }

    const metadata: TrainingDatasetMetadata = {
      exportedAt: new Date(),
      exampleCount: this.playstyleExamples.length,
      featureVersion: this.config.featureVersion || '1.0.0',
      labelDistribution,
      sessionIds: Array.from(sessionIds),
      source: 'camelot-combat-reporter',
    };

    return {
      examples: this.playstyleExamples,
      metadata,
    };
  }

  /**
   * Clear all stored examples
   */
  clear(): void {
    this.outcomeExamples = [];
    this.playstyleExamples = [];
  }

  /**
   * Get count of stored examples
   */
  getExampleCounts(): { outcome: number; playstyle: number } {
    return {
      outcome: this.outcomeExamples.length,
      playstyle: this.playstyleExamples.length,
    };
  }

  /**
   * Export combined statistics about the training data
   */
  exportStats(): TrainingDataStats {
    const outcomeDataset = this.exportOutcomeDataset();
    const playstyleDataset = this.exportPlaystyleDataset();

    return {
      outcome: {
        totalExamples: outcomeDataset.metadata.exampleCount,
        labelDistribution: outcomeDataset.metadata.labelDistribution,
        uniqueSessions: outcomeDataset.metadata.sessionIds.length,
      },
      playstyle: {
        totalExamples: playstyleDataset.metadata.exampleCount,
        labelDistribution: playstyleDataset.metadata.labelDistribution,
        uniqueSessions: playstyleDataset.metadata.sessionIds.length,
      },
      featureVersion: this.config.featureVersion || '1.0.0',
      exportedAt: new Date(),
    };
  }

  // ============================================================================
  // Private Helpers
  // ============================================================================

  /**
   * Extract combat state features at a specific time point in the fight
   */
  private extractFeaturesAtTime(
    session: CombatSession,
    playerName: string,
    timeRatio: number
  ): CombatStateFeatures | null {
    // Filter events up to the time point
    const targetTime = session.startTime.getTime() + session.durationMs * timeRatio;
    const eventsUpToTime = session.events.filter(
      (e) => e.timestamp.getTime() <= targetTime
    );

    if (eventsUpToTime.length === 0) {
      return null;
    }

    // Create a partial session for feature extraction
    const partialSession: CombatSession = {
      ...session,
      events: eventsUpToTime,
      durationMs: session.durationMs * timeRatio,
    };

    const extracted = this.featureExtractor.extractFromSession(partialSession, playerName);
    return extracted?.combatState ?? null;
  }

  /**
   * Extract behavior features from entire session
   */
  private extractBehaviorFeatures(
    session: CombatSession,
    playerName: string
  ): BehaviorFeatures | null {
    if (session.events.length === 0) {
      return null;
    }

    const extracted = this.featureExtractor.extractFromSession(session, playerName);
    return extracted?.behavior ?? null;
  }
}

/**
 * Training data statistics
 */
export interface TrainingDataStats {
  outcome: {
    totalExamples: number;
    labelDistribution: Record<string, number>;
    uniqueSessions: number;
  };
  playstyle: {
    totalExamples: number;
    labelDistribution: Record<string, number>;
    uniqueSessions: number;
  };
  featureVersion: string;
  exportedAt: Date;
}

/**
 * Helper function to resolve outcome from session based on deaths
 *
 * @param session - Combat session
 * @param playerName - Player name to check
 * @returns Outcome label or null if undetermined
 */
export function resolveOutcomeByDeaths(
  session: CombatSession,
  playerName: string
): OutcomeLabel | null {
  let playerDeaths = 0;
  let opponentDeaths = 0;

  for (const event of session.events) {
    if (event.eventType === EventType.DEATH) {
      const deathEvent = event as DeathEvent;
      if (deathEvent.target?.name === playerName) {
        playerDeaths++;
      } else if (deathEvent.killer?.name === playerName) {
        opponentDeaths++;
      }
    }
  }

  // Simple heuristic: more opponent deaths = win
  if (opponentDeaths > playerDeaths) {
    return 'WIN';
  } else if (playerDeaths > opponentDeaths) {
    return 'LOSS';
  } else if (opponentDeaths === 0 && playerDeaths === 0) {
    return null; // Undetermined
  }

  return 'DRAW';
}

/**
 * Helper function to resolve outcome from damage ratio
 *
 * @param session - Combat session
 * @param playerName - Player name
 * @param winThreshold - Damage ratio threshold for win (default 1.5)
 * @returns Outcome label or null if undetermined
 */
export function resolveOutcomeByDamageRatio(
  session: CombatSession,
  playerName: string,
  winThreshold = 1.5
): OutcomeLabel | null {
  let damageDealt = 0;
  let damageTaken = 0;

  for (const event of session.events) {
    if (event.eventType === EventType.DAMAGE_DEALT || event.eventType === EventType.DAMAGE_RECEIVED) {
      const damageEvent = event as DamageEvent;
      const amount = damageEvent.effectiveAmount ?? damageEvent.amount ?? 0;

      if (damageEvent.source?.name === playerName) {
        damageDealt += amount;
      }
      if (damageEvent.target?.name === playerName) {
        damageTaken += amount;
      }
    }
  }

  if (damageTaken === 0 && damageDealt === 0) {
    return null;
  }

  const ratio = damageTaken > 0 ? damageDealt / damageTaken : damageDealt > 0 ? Infinity : 1;

  if (ratio >= winThreshold) {
    return 'WIN';
  } else if (ratio <= 1 / winThreshold) {
    return 'LOSS';
  }

  return 'DRAW';
}
