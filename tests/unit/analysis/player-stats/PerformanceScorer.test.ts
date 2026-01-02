import { PerformanceScorer } from '../../../../src/analysis/player-stats/PerformanceScorer';
import type { ParticipantMetrics, ParticipantRole } from '../../../../src/analysis/types/index';
import { EntityType } from '../../../../src/types';
import type { Entity } from '../../../../src/types';

describe('PerformanceScorer', () => {
  const scorer = new PerformanceScorer();

  const createEntity = (name: string, isSelf = false): Entity => ({
    name,
    entityType: isSelf ? EntityType.SELF : EntityType.PLAYER,
    isPlayer: true,
    isSelf,
  });

  const createEmptyCritStats = () => ({
    totalCrits: 0,
    totalHits: 0,
    critRate: 0,
    totalCritAmount: 0,
    averageCritAmount: 0,
  });

  const createParticipantMetrics = (
    name: string,
    dps: number,
    hps = 0,
    role: ParticipantRole = 'DAMAGE_DEALER'
  ): ParticipantMetrics => ({
    entity: createEntity(name),
    role,
    damage: {
      entity: createEntity(name),
      totalDealt: dps * 60,
      effectiveDealt: dps * 60,
      totalTaken: 0,
      effectiveTaken: 0,
      dps,
      dtps: 0,
      peakDps: dps * 1.5,
      byAction: [],
      byDamageType: [],
      byTarget: [],
      bySource: [],
      critStats: createEmptyCritStats(),
    },
    healing: {
      entity: createEntity(name),
      totalDone: hps * 60,
      effectiveDone: hps * 60,
      totalOverheal: 0,
      overhealRate: 0,
      totalReceived: 0,
      effectiveReceived: 0,
      hps,
      peakHps: hps * 1.5,
      bySpell: [],
      byTarget: [],
      bySource: [],
      critStats: createEmptyCritStats(),
    },
  });

  describe('calculateScore', () => {
    it('should return 0 for zero DPS with no other contributions', () => {
      const input = {
        dps: 0,
        hps: 0,
        kdr: 0,
        survived: false,
        role: 'DAMAGE_DEALER' as ParticipantRole,
      };
      const participants = [createParticipantMetrics('Player1', 100)];

      const score = scorer.calculateScore(input, participants);
      expect(score).toBe(0);
    });

    it('should return higher score for above-average DPS', () => {
      const participants = [
        createParticipantMetrics('Player1', 100),
        createParticipantMetrics('Player2', 50),
        createParticipantMetrics('Player3', 50),
      ];

      const aboveAvgInput = {
        dps: 100,
        hps: 0,
        kdr: 1,
        survived: true,
        role: 'DAMAGE_DEALER' as ParticipantRole,
      };

      const belowAvgInput = {
        dps: 50,
        hps: 0,
        kdr: 1,
        survived: true,
        role: 'DAMAGE_DEALER' as ParticipantRole,
      };

      const aboveAvgScore = scorer.calculateScore(aboveAvgInput, participants);
      const belowAvgScore = scorer.calculateScore(belowAvgInput, participants);

      expect(aboveAvgScore).toBeGreaterThan(belowAvgScore);
    });

    it('should factor in KDR', () => {
      const participants = [createParticipantMetrics('Player1', 100)];

      const highKdrInput = {
        dps: 100,
        hps: 0,
        kdr: 5,
        survived: true,
        role: 'DAMAGE_DEALER' as ParticipantRole,
      };

      const lowKdrInput = {
        dps: 100,
        hps: 0,
        kdr: 0.5,
        survived: true,
        role: 'DAMAGE_DEALER' as ParticipantRole,
      };

      const highKdrScore = scorer.calculateScore(highKdrInput, participants);
      const lowKdrScore = scorer.calculateScore(lowKdrInput, participants);

      expect(highKdrScore).toBeGreaterThan(lowKdrScore);
    });

    it('should cap score at 100', () => {
      const participants = [createParticipantMetrics('Player1', 10)];

      const extremeInput = {
        dps: 1000,
        hps: 0,
        kdr: 100,
        survived: true,
        role: 'DAMAGE_DEALER' as ParticipantRole,
      };

      const score = scorer.calculateScore(extremeInput, participants);
      expect(score).toBeLessThanOrEqual(100);
    });

    it('should use HPS for healers instead of DPS', () => {
      const participants = [
        createParticipantMetrics('Healer1', 0, 200, 'HEALER'),
        createParticipantMetrics('Healer2', 0, 100, 'HEALER'),
      ];

      const goodHealerInput = {
        dps: 0,
        hps: 200,
        kdr: 0,
        survived: true,
        role: 'HEALER' as ParticipantRole,
      };

      const poorHealerInput = {
        dps: 0,
        hps: 50,
        kdr: 0,
        survived: true,
        role: 'HEALER' as ParticipantRole,
      };

      const goodScore = scorer.calculateScore(goodHealerInput, participants);
      const poorScore = scorer.calculateScore(poorHealerInput, participants);

      expect(goodScore).toBeGreaterThan(poorScore);
    });
  });

  describe('ratePerformance', () => {
    it('should rate EXCELLENT for scores >= 80', () => {
      expect(scorer.ratePerformance(80)).toBe('EXCELLENT');
      expect(scorer.ratePerformance(95)).toBe('EXCELLENT');
      expect(scorer.ratePerformance(100)).toBe('EXCELLENT');
    });

    it('should rate GOOD for scores >= 60 and < 80', () => {
      expect(scorer.ratePerformance(60)).toBe('GOOD');
      expect(scorer.ratePerformance(75)).toBe('GOOD');
      expect(scorer.ratePerformance(79)).toBe('GOOD');
    });

    it('should rate AVERAGE for scores >= 40 and < 60', () => {
      expect(scorer.ratePerformance(40)).toBe('AVERAGE');
      expect(scorer.ratePerformance(50)).toBe('AVERAGE');
      expect(scorer.ratePerformance(59)).toBe('AVERAGE');
    });

    it('should rate BELOW_AVERAGE for scores >= 20 and < 40', () => {
      expect(scorer.ratePerformance(20)).toBe('BELOW_AVERAGE');
      expect(scorer.ratePerformance(30)).toBe('BELOW_AVERAGE');
      expect(scorer.ratePerformance(39)).toBe('BELOW_AVERAGE');
    });

    it('should rate POOR for scores < 20', () => {
      expect(scorer.ratePerformance(0)).toBe('POOR');
      expect(scorer.ratePerformance(10)).toBe('POOR');
      expect(scorer.ratePerformance(19)).toBe('POOR');
    });
  });

  describe('custom thresholds', () => {
    it('should use custom thresholds when provided', () => {
      const customScorer = new PerformanceScorer({
        performanceThresholds: {
          excellent: 90,
          good: 70,
          average: 50,
          belowAverage: 30,
        },
      });

      expect(customScorer.ratePerformance(85)).toBe('GOOD');
      expect(customScorer.ratePerformance(90)).toBe('EXCELLENT');
    });
  });
});
