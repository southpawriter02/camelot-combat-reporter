import { TrendCalculator } from '../../../../src/analysis/player-stats/TrendCalculator';
import type { PlayerSessionStats } from '../../../../src/analysis/player-stats/types';
import type { ParticipantRole } from '../../../../src/analysis/types/index';

describe('TrendCalculator', () => {
  const calculator = new TrendCalculator();

  const createSessionStats = (
    sessionId: string,
    timestamp: Date,
    dps: number,
    kdr: number,
    performanceScore: number
  ): PlayerSessionStats => ({
    playerName: 'TestPlayer',
    sessionId,
    sessionStart: timestamp,
    sessionEnd: new Date(timestamp.getTime() + 60000),
    durationMs: 60000,
    role: 'DAMAGE_DEALER' as ParticipantRole,
    kills: Math.floor(kdr * 2),
    deaths: 2,
    assists: 1,
    kdr,
    damageDealt: dps * 60,
    damageTaken: 1000,
    dps,
    peakDps: dps * 1.5,
    healingDone: 0,
    healingReceived: 500,
    hps: 0,
    overhealRate: 0,
    critRate: 0.15,
    performanceScore,
    performanceRating: 'AVERAGE',
  });

  describe('calculateTrends', () => {
    it('should return empty trends for empty input', () => {
      const result = calculator.calculateTrends([]);

      expect(result.dpsOverTime).toHaveLength(0);
      expect(result.kdrOverTime).toHaveLength(0);
      expect(result.performanceOverTime).toHaveLength(0);
    });

    it('should return single point for single session', () => {
      const baseTime = new Date(2024, 0, 1, 12, 0, 0);
      const stats = [createSessionStats('session1', baseTime, 100, 2.0, 60)];

      const result = calculator.calculateTrends(stats);

      expect(result.dpsOverTime).toHaveLength(1);
      expect(result.dpsOverTime[0]!.value).toBe(100);
      expect(result.kdrOverTime).toHaveLength(1);
      expect(result.kdrOverTime[0]!.value).toBe(2.0);
    });

    it('should sort sessions by timestamp', () => {
      const time1 = new Date(2024, 0, 1, 12, 0, 0);
      const time2 = new Date(2024, 0, 1, 14, 0, 0);
      const time3 = new Date(2024, 0, 1, 10, 0, 0);

      const stats = [
        createSessionStats('session1', time1, 100, 2.0, 60),
        createSessionStats('session2', time2, 150, 3.0, 70),
        createSessionStats('session3', time3, 80, 1.0, 50),
      ];

      const result = calculator.calculateTrends(stats);

      expect(result.dpsOverTime[0]!.sessionId).toBe('session3');
      expect(result.dpsOverTime[1]!.sessionId).toBe('session1');
      expect(result.dpsOverTime[2]!.sessionId).toBe('session2');
    });

    it('should calculate moving averages using helper method', () => {
      const baseTime = new Date(2024, 0, 1, 12, 0, 0);
      const stats = [
        createSessionStats('s1', new Date(baseTime.getTime()), 100, 1, 50),
        createSessionStats('s2', new Date(baseTime.getTime() + 3600000), 120, 2, 60),
        createSessionStats('s3', new Date(baseTime.getTime() + 7200000), 140, 3, 70),
        createSessionStats('s4', new Date(baseTime.getTime() + 10800000), 160, 4, 80),
      ];

      const result = calculator.calculateTrends(stats);
      const movingAvg = calculator.calculateMovingAverage(result.dpsOverTime, 3);

      expect(movingAvg).toBeDefined();
      expect(movingAvg.length).toBe(4);
    });

    it('should detect trend direction using helper method', () => {
      const baseTime = new Date(2024, 0, 1, 12, 0, 0);
      const improvingStats = [
        createSessionStats('s1', new Date(baseTime.getTime()), 100, 1, 40),
        createSessionStats('s2', new Date(baseTime.getTime() + 3600000), 120, 2, 50),
        createSessionStats('s3', new Date(baseTime.getTime() + 7200000), 140, 3, 60),
        createSessionStats('s4', new Date(baseTime.getTime() + 10800000), 160, 4, 70),
      ];

      const result = calculator.calculateTrends(improvingStats);
      const trend = calculator.detectTrendDirection(result.dpsOverTime);

      expect(trend.direction).toBe('IMPROVING');
    });
  });

  describe('calculateVariance', () => {
    it('should return 0 for empty array', () => {
      expect(calculator.calculateVariance([])).toBe(0);
    });

    it('should return 0 for single value', () => {
      expect(calculator.calculateVariance([100])).toBe(0);
    });

    it('should return 0 for identical values', () => {
      expect(calculator.calculateVariance([50, 50, 50, 50])).toBe(0);
    });

    it('should calculate correct variance for different values', () => {
      // Values: 10, 20, 30, 40, 50
      // Mean: 30
      // Squared differences: 400, 100, 0, 100, 400
      // Variance: 1000 / 5 = 200
      const values = [10, 20, 30, 40, 50];
      const variance = calculator.calculateVariance(values);

      // calculateVariance returns variance (not standard deviation)
      expect(variance).toBeCloseTo(200, 1);
    });
  });

  describe('calculateStandardDeviation', () => {
    it('should calculate correct standard deviation', () => {
      // Values: 10, 20, 30, 40, 50
      // Standard deviation: sqrt(200) ≈ 14.14
      const values = [10, 20, 30, 40, 50];
      const stdDev = calculator.calculateStandardDeviation(values);

      expect(stdDev).toBeCloseTo(14.14, 1);
    });
  });

  describe('rateConsistency', () => {
    it('should rate VERY_CONSISTENT for low variance', () => {
      // rateConsistency uses sqrt(variance) internally, so we pass variance
      expect(calculator.rateConsistency(9)).toBe('VERY_CONSISTENT'); // sqrt(9) = 3
      expect(calculator.rateConsistency(25)).toBe('VERY_CONSISTENT'); // sqrt(25) = 5
    });

    it('should rate CONSISTENT for moderate variance', () => {
      expect(calculator.rateConsistency(100)).toBe('CONSISTENT'); // sqrt(100) = 10
      expect(calculator.rateConsistency(196)).toBe('CONSISTENT'); // sqrt(196) = 14
    });

    it('should rate VARIABLE for higher variance', () => {
      expect(calculator.rateConsistency(400)).toBe('VARIABLE'); // sqrt(400) = 20
      expect(calculator.rateConsistency(576)).toBe('VARIABLE'); // sqrt(576) = 24
    });

    it('should rate INCONSISTENT for high variance', () => {
      expect(calculator.rateConsistency(900)).toBe('INCONSISTENT'); // sqrt(900) = 30
      expect(calculator.rateConsistency(2500)).toBe('INCONSISTENT'); // sqrt(2500) = 50
    });
  });

  describe('custom thresholds', () => {
    it('should use custom thresholds when provided', () => {
      const customCalculator = new TrendCalculator({
        consistencyThresholds: {
          veryConsistent: 10,
          consistent: 20,
          variable: 30,
        },
      });

      // rateConsistency takes variance and uses sqrt internally
      expect(customCalculator.rateConsistency(64)).toBe('VERY_CONSISTENT'); // sqrt(64) = 8
      expect(customCalculator.rateConsistency(225)).toBe('CONSISTENT'); // sqrt(225) = 15
      expect(customCalculator.rateConsistency(625)).toBe('VARIABLE'); // sqrt(625) = 25
      expect(customCalculator.rateConsistency(1225)).toBe('INCONSISTENT'); // sqrt(1225) = 35
    });
  });
});
