import type { ParticipantRole } from '../types/session.js';

/**
 * Performance rating based on composite score
 */
export type PerformanceRating =
  | 'EXCELLENT'
  | 'GOOD'
  | 'AVERAGE'
  | 'BELOW_AVERAGE'
  | 'POOR';

/**
 * Consistency rating based on performance variance
 */
export type ConsistencyRating =
  | 'VERY_CONSISTENT'
  | 'CONSISTENT'
  | 'VARIABLE'
  | 'INCONSISTENT';

/**
 * A single point in a time-series trend
 */
export interface TrendPoint {
  /** Timestamp of the session */
  timestamp: Date;
  /** Session ID for reference */
  sessionId: string;
  /** The metric value at this point */
  value: number;
}

/**
 * Statistics for a player in a single combat session
 */
export interface PlayerSessionStats {
  /** Player name */
  playerName: string;
  /** Session identifier */
  sessionId: string;
  /** Session start time */
  sessionStart: Date;
  /** Session end time */
  sessionEnd: Date;
  /** Duration in milliseconds */
  durationMs: number;
  /** Role determined by actions */
  role: ParticipantRole;

  // Kill/Death tracking
  /** Number of kills (death events where player is killer) */
  kills: number;
  /** Number of deaths (death events where player is target) */
  deaths: number;
  /** Number of assists (damage dealt to targets that died, but not final blow) */
  assists: number;
  /** Kill/Death ratio: kills / max(deaths, 1) */
  kdr: number;

  // Damage metrics
  /** Total damage dealt */
  damageDealt: number;
  /** Total damage taken */
  damageTaken: number;
  /** Average damage per second */
  dps: number;
  /** Peak DPS in any rolling window */
  peakDps: number;

  // Healing metrics
  /** Total healing done */
  healingDone: number;
  /** Total healing received */
  healingReceived: number;
  /** Average healing per second */
  hps: number;
  /** Overheal percentage (0-1) */
  overhealRate: number;

  // Critical stats
  /** Critical hit rate (0-1) */
  critRate: number;

  // Performance
  /** Composite performance score (0-100) */
  performanceScore: number;
  /** Performance rating category */
  performanceRating: PerformanceRating;
}

/**
 * Aggregate statistics for a player across multiple sessions
 */
export interface PlayerAggregateStats {
  /** Player name */
  playerName: string;
  /** Total number of sessions analyzed */
  totalSessions: number;
  /** Total combat time in milliseconds */
  totalCombatTimeMs: number;

  // Aggregate kill/death
  /** Total kills across all sessions */
  totalKills: number;
  /** Total deaths across all sessions */
  totalDeaths: number;
  /** Total assists across all sessions */
  totalAssists: number;
  /** Overall KDR: totalKills / max(totalDeaths, 1) */
  overallKDR: number;

  // Aggregate damage/healing
  /** Total damage dealt across all sessions */
  totalDamageDealt: number;
  /** Total damage taken across all sessions */
  totalDamageTaken: number;
  /** Total healing done across all sessions */
  totalHealingDone: number;
  /** Total healing received across all sessions */
  totalHealingReceived: number;

  // Averages per session
  /** Average DPS across sessions */
  avgDPS: number;
  /** Average HPS across sessions */
  avgHPS: number;
  /** Average kills per session */
  avgKillsPerSession: number;
  /** Average deaths per session */
  avgDeathsPerSession: number;

  // Performance distribution
  /** Average performance score across sessions */
  avgPerformanceScore: number;
  /** Count of sessions in each performance rating */
  performanceDistribution: Record<PerformanceRating, number>;

  // Best/Worst identification
  /** Best performing session */
  bestFight: PlayerSessionStats;
  /** Worst performing session */
  worstFight: PlayerSessionStats;

  // Trends (for graphing)
  /** DPS over time across sessions */
  dpsOverTime: TrendPoint[];
  /** KDR over time across sessions */
  kdrOverTime: TrendPoint[];
  /** Performance score over time */
  performanceOverTime: TrendPoint[];

  // Consistency metrics
  /** Variance in performance scores */
  performanceVariance: number;
  /** Consistency rating based on variance */
  consistencyRating: ConsistencyRating;
}

/**
 * Configuration for player statistics calculations
 */
export interface PlayerStatsConfig {
  /** Thresholds for performance ratings */
  performanceThresholds: PerformanceThresholds;
  /** Thresholds for consistency ratings */
  consistencyThresholds: ConsistencyThresholds;
}

/**
 * Thresholds for determining performance ratings
 */
export interface PerformanceThresholds {
  /** Minimum score for EXCELLENT rating (default: 80) */
  excellent: number;
  /** Minimum score for GOOD rating (default: 60) */
  good: number;
  /** Minimum score for AVERAGE rating (default: 40) */
  average: number;
  /** Minimum score for BELOW_AVERAGE rating (default: 20) */
  belowAverage: number;
  // Below belowAverage threshold = POOR
}

/**
 * Thresholds for determining consistency ratings (based on variance)
 */
export interface ConsistencyThresholds {
  /** Maximum variance for VERY_CONSISTENT (default: 5) */
  veryConsistent: number;
  /** Maximum variance for CONSISTENT (default: 15) */
  consistent: number;
  /** Maximum variance for VARIABLE (default: 25) */
  variable: number;
  // Above variable threshold = INCONSISTENT
}

/**
 * Default performance thresholds
 */
export const DEFAULT_PERFORMANCE_THRESHOLDS: PerformanceThresholds = {
  excellent: 80,
  good: 60,
  average: 40,
  belowAverage: 20,
};

/**
 * Default consistency thresholds
 */
export const DEFAULT_CONSISTENCY_THRESHOLDS: ConsistencyThresholds = {
  veryConsistent: 5,
  consistent: 15,
  variable: 25,
};

/**
 * Default player stats configuration
 */
export const DEFAULT_PLAYER_STATS_CONFIG: PlayerStatsConfig = {
  performanceThresholds: DEFAULT_PERFORMANCE_THRESHOLDS,
  consistencyThresholds: DEFAULT_CONSISTENCY_THRESHOLDS,
};
