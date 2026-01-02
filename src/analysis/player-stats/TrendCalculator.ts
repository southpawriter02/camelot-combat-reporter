import type {
  TrendPoint,
  PlayerSessionStats,
  ConsistencyRating,
  ConsistencyThresholds,
  PlayerStatsConfig,
} from './types.js';
import { DEFAULT_PLAYER_STATS_CONFIG } from './types.js';

/**
 * Trend data extracted from multiple sessions
 */
export interface TrendData {
  /** DPS over time */
  dpsOverTime: TrendPoint[];
  /** KDR over time */
  kdrOverTime: TrendPoint[];
  /** Performance score over time */
  performanceOverTime: TrendPoint[];
}

/**
 * Calculates performance trends and consistency metrics
 */
export class TrendCalculator {
  private thresholds: ConsistencyThresholds;

  constructor(config: Partial<PlayerStatsConfig> = {}) {
    this.thresholds = {
      ...DEFAULT_PLAYER_STATS_CONFIG.consistencyThresholds,
      ...config.consistencyThresholds,
    };
  }

  /**
   * Calculate trend data from session stats
   * @param sessionStats Array of player session stats sorted by time
   * @returns Trend data for graphing
   */
  calculateTrends(sessionStats: PlayerSessionStats[]): TrendData {
    // Sort by session start time
    const sorted = [...sessionStats].sort(
      (a, b) => a.sessionStart.getTime() - b.sessionStart.getTime()
    );

    return {
      dpsOverTime: sorted.map((s) => ({
        timestamp: s.sessionStart,
        sessionId: s.sessionId,
        value: s.dps,
      })),
      kdrOverTime: sorted.map((s) => ({
        timestamp: s.sessionStart,
        sessionId: s.sessionId,
        value: s.kdr,
      })),
      performanceOverTime: sorted.map((s) => ({
        timestamp: s.sessionStart,
        sessionId: s.sessionId,
        value: s.performanceScore,
      })),
    };
  }

  /**
   * Calculate variance (standard deviation squared) of values
   * @param values Array of numeric values
   * @returns Variance
   */
  calculateVariance(values: number[]): number {
    if (values.length === 0) return 0;
    if (values.length === 1) return 0;

    const mean = values.reduce((sum, v) => sum + v, 0) / values.length;
    const squaredDiffs = values.map((v) => Math.pow(v - mean, 2));
    const variance = squaredDiffs.reduce((sum, v) => sum + v, 0) / values.length;

    return variance;
  }

  /**
   * Calculate standard deviation of values
   * @param values Array of numeric values
   * @returns Standard deviation
   */
  calculateStandardDeviation(values: number[]): number {
    return Math.sqrt(this.calculateVariance(values));
  }

  /**
   * Rate consistency based on performance variance
   * @param variance Performance score variance
   * @returns Consistency rating
   */
  rateConsistency(variance: number): ConsistencyRating {
    // Use standard deviation for more intuitive thresholds
    const stdDev = Math.sqrt(variance);

    if (stdDev <= this.thresholds.veryConsistent) return 'VERY_CONSISTENT';
    if (stdDev <= this.thresholds.consistent) return 'CONSISTENT';
    if (stdDev <= this.thresholds.variable) return 'VARIABLE';
    return 'INCONSISTENT';
  }

  /**
   * Calculate moving average for smoothing trend data
   * @param points Trend points
   * @param windowSize Number of points to average
   * @returns Smoothed trend points
   */
  calculateMovingAverage(points: TrendPoint[], windowSize: number): TrendPoint[] {
    if (windowSize <= 1 || points.length <= windowSize) {
      return points;
    }

    const result: TrendPoint[] = [];

    for (let i = 0; i < points.length; i++) {
      const start = Math.max(0, i - Math.floor(windowSize / 2));
      const end = Math.min(points.length, i + Math.floor(windowSize / 2) + 1);
      const window = points.slice(start, end);

      const avgValue = window.reduce((sum, p) => sum + p.value, 0) / window.length;

      result.push({
        timestamp: points[i]!.timestamp,
        sessionId: points[i]!.sessionId,
        value: avgValue,
      });
    }

    return result;
  }

  /**
   * Detect if there's an improving or declining trend
   * @param points Trend points (must be sorted by time)
   * @returns Trend direction and strength
   */
  detectTrendDirection(points: TrendPoint[]): {
    direction: 'IMPROVING' | 'DECLINING' | 'STABLE';
    strength: number;
  } {
    if (points.length < 3) {
      return { direction: 'STABLE', strength: 0 };
    }

    // Simple linear regression
    const n = points.length;
    let sumX = 0;
    let sumY = 0;
    let sumXY = 0;
    let sumX2 = 0;

    for (let i = 0; i < n; i++) {
      sumX += i;
      sumY += points[i]!.value;
      sumXY += i * points[i]!.value;
      sumX2 += i * i;
    }

    const slope = (n * sumXY - sumX * sumY) / (n * sumX2 - sumX * sumX);
    const avgValue = sumY / n;

    // Normalize slope relative to average value
    const normalizedSlope = avgValue !== 0 ? slope / avgValue : slope;

    // Determine direction and strength
    const strength = Math.abs(normalizedSlope) * 100; // Convert to percentage

    if (normalizedSlope > 0.01) {
      return { direction: 'IMPROVING', strength: Math.min(100, strength) };
    } else if (normalizedSlope < -0.01) {
      return { direction: 'DECLINING', strength: Math.min(100, strength) };
    }

    return { direction: 'STABLE', strength: 0 };
  }

  /**
   * Get the current thresholds
   */
  getThresholds(): ConsistencyThresholds {
    return { ...this.thresholds };
  }
}
