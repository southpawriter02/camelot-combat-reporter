import type { ParticipantRole } from '../types/session.js';
import type { ParticipantMetrics } from '../types/metrics.js';
import type {
  PerformanceRating,
  PerformanceThresholds,
  PlayerStatsConfig,
} from './types.js';
import { DEFAULT_PLAYER_STATS_CONFIG } from './types.js';

/**
 * Input metrics for calculating performance score
 */
export interface PerformanceInput {
  /** Player's DPS */
  dps: number;
  /** Player's HPS */
  hps: number;
  /** Player's kill/death ratio */
  kdr: number;
  /** Player's role in the fight */
  role: ParticipantRole;
  /** Whether the player survived the fight */
  survived: boolean;
}

/**
 * Calculates performance scores and ratings for players
 */
export class PerformanceScorer {
  private thresholds: PerformanceThresholds;

  constructor(config: Partial<PlayerStatsConfig> = {}) {
    this.thresholds = {
      ...DEFAULT_PLAYER_STATS_CONFIG.performanceThresholds,
      ...config.performanceThresholds,
    };
  }

  /**
   * Calculate performance score relative to other participants
   * @param input Player's performance metrics
   * @param allParticipants All participants' metrics for comparison
   * @returns Performance score (0-100)
   */
  calculateScore(
    input: PerformanceInput,
    allParticipants: ParticipantMetrics[]
  ): number {
    // Calculate average DPS across all participants
    const avgDPS = this.calculateAverageDPS(allParticipants);
    const avgHPS = this.calculateAverageHPS(allParticipants);

    // DPS contribution (0-40 points)
    // Score based on how player's DPS compares to average
    const dpsScore = this.calculateDPSScore(input.dps, avgDPS, input.role);

    // HPS contribution for healers (0-30 points for healers, 0 for others)
    const hpsScore = this.calculateHPSScore(input.hps, avgHPS, input.role);

    // KDR contribution (0-30 points)
    const kdrScore = this.calculateKDRScore(input.kdr);

    // Survival bonus (0-10 points)
    const survivalScore = input.survived ? 10 : 0;

    // Role-specific adjustment
    const roleBonus = this.calculateRoleBonus(input);

    // Calculate total score, capped at 100
    const totalScore = Math.min(
      100,
      dpsScore + hpsScore + kdrScore + survivalScore + roleBonus
    );

    return Math.max(0, Math.round(totalScore));
  }

  /**
   * Calculate DPS-based score component
   */
  private calculateDPSScore(
    playerDPS: number,
    avgDPS: number,
    role: ParticipantRole
  ): number {
    if (avgDPS === 0) return 20; // Default middle score if no comparison

    const dpsRatio = playerDPS / avgDPS;

    // Healers get reduced DPS weight (max 15 points)
    // Everyone else gets full DPS weight (max 40 points)
    const maxPoints = role === 'HEALER' ? 15 : 40;

    // 100% of average = 50% of max points
    // 200% of average = 100% of max points
    return Math.min(maxPoints, dpsRatio * (maxPoints / 2));
  }

  /**
   * Calculate HPS-based score component (for healers)
   */
  private calculateHPSScore(
    playerHPS: number,
    avgHPS: number,
    role: ParticipantRole
  ): number {
    // Only healers get HPS score
    if (role !== 'HEALER') return 0;

    if (avgHPS === 0) return 15; // Default if no comparison

    const hpsRatio = playerHPS / avgHPS;
    const maxPoints = 30;

    // 100% of average = 50% of max points
    // 200% of average = 100% of max points
    return Math.min(maxPoints, hpsRatio * (maxPoints / 2));
  }

  /**
   * Calculate KDR-based score component
   */
  private calculateKDRScore(kdr: number): number {
    const maxPoints = 30;

    // KDR scoring:
    // 0.5 KDR = 10 points
    // 1.0 KDR = 15 points
    // 2.0 KDR = 25 points
    // 3.0+ KDR = 30 points (max)
    if (kdr >= 3) return maxPoints;
    if (kdr >= 2) return 25;
    if (kdr >= 1) return 15 + (kdr - 1) * 10;
    return Math.max(0, kdr * 20);
  }

  /**
   * Calculate role-specific bonus
   */
  private calculateRoleBonus(input: PerformanceInput): number {
    switch (input.role) {
      case 'TANK':
        // Tanks get bonus for surviving while taking damage
        return input.survived ? 5 : 0;
      case 'HEALER':
        // Healers get bonus for keeping others alive (already reflected in HPS)
        return 0;
      case 'HYBRID':
        // Hybrids get small bonus for versatility
        return 3;
      default:
        return 0;
    }
  }

  /**
   * Calculate average DPS across all participants
   */
  private calculateAverageDPS(participants: ParticipantMetrics[]): number {
    if (participants.length === 0) return 0;

    const totalDPS = participants.reduce((sum, p) => sum + p.damage.dps, 0);
    return totalDPS / participants.length;
  }

  /**
   * Calculate average HPS across all participants
   */
  private calculateAverageHPS(participants: ParticipantMetrics[]): number {
    if (participants.length === 0) return 0;

    const totalHPS = participants.reduce((sum, p) => sum + p.healing.hps, 0);
    return totalHPS / participants.length;
  }

  /**
   * Convert a performance score to a rating
   * @param score Performance score (0-100)
   * @returns Performance rating category
   */
  ratePerformance(score: number): PerformanceRating {
    if (score >= this.thresholds.excellent) return 'EXCELLENT';
    if (score >= this.thresholds.good) return 'GOOD';
    if (score >= this.thresholds.average) return 'AVERAGE';
    if (score >= this.thresholds.belowAverage) return 'BELOW_AVERAGE';
    return 'POOR';
  }

  /**
   * Get the current thresholds
   */
  getThresholds(): PerformanceThresholds {
    return { ...this.thresholds };
  }
}
