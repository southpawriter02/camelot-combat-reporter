import type { DeathEvent, DamageEvent } from '../../types/index.js';
import { EventType } from '../../types/index.js';
import type { CombatSession, ParticipantMetrics } from '../types/index.js';
import { DamageCalculator } from '../metrics/DamageCalculator.js';
import { HealingCalculator } from '../metrics/HealingCalculator.js';
import type {
  PlayerSessionStats,
  PlayerAggregateStats,
  PlayerStatsConfig,
  PerformanceRating,
} from './types.js';
import { DEFAULT_PLAYER_STATS_CONFIG } from './types.js';
import { PerformanceScorer, type PerformanceInput } from './PerformanceScorer.js';
import { TrendCalculator } from './TrendCalculator.js';

/**
 * Calculates player statistics from combat sessions
 */
export class PlayerStatisticsCalculator {
  private config: PlayerStatsConfig;
  private damageCalculator: DamageCalculator;
  private healingCalculator: HealingCalculator;
  private performanceScorer: PerformanceScorer;
  private trendCalculator: TrendCalculator;

  constructor(config: Partial<PlayerStatsConfig> = {}) {
    this.config = {
      ...DEFAULT_PLAYER_STATS_CONFIG,
      ...config,
      performanceThresholds: {
        ...DEFAULT_PLAYER_STATS_CONFIG.performanceThresholds,
        ...config.performanceThresholds,
      },
      consistencyThresholds: {
        ...DEFAULT_PLAYER_STATS_CONFIG.consistencyThresholds,
        ...config.consistencyThresholds,
      },
    };

    this.damageCalculator = new DamageCalculator();
    this.healingCalculator = new HealingCalculator();
    this.performanceScorer = new PerformanceScorer(this.config);
    this.trendCalculator = new TrendCalculator(this.config);
  }

  /**
   * Calculate statistics for a player in a single session
   * @param session The combat session
   * @param playerName Name of the player to analyze
   * @returns Player session stats, or null if player not in session
   */
  calculateForSession(
    session: CombatSession,
    playerName: string
  ): PlayerSessionStats | null {
    // Find the participant
    const participant = session.participants.find(
      (p) => p.entity.name === playerName
    );

    if (!participant) {
      return null;
    }

    // Get damage and healing metrics
    const damageMetrics = this.damageCalculator.calculateForEntity(
      session.events,
      participant.entity,
      session.durationMs
    );

    const healingMetrics = this.healingCalculator.calculateForEntity(
      session.events,
      participant.entity,
      session.durationMs
    );

    // Count kills, deaths, assists
    const kills = this.countKills(session, playerName);
    const deaths = this.countDeaths(session, playerName);
    const assists = this.countAssists(session, playerName);
    const kdr = kills / Math.max(deaths, 1);

    // Calculate if player survived (didn't die in session)
    const survived = deaths === 0;

    // Get all participant metrics for comparison
    const allParticipantMetrics = this.getAllParticipantMetrics(session);

    // Calculate performance score
    const performanceInput: PerformanceInput = {
      dps: damageMetrics.dps,
      hps: healingMetrics.hps,
      kdr,
      role: participant.role,
      survived,
    };

    const performanceScore = this.performanceScorer.calculateScore(
      performanceInput,
      allParticipantMetrics
    );

    const performanceRating = this.performanceScorer.ratePerformance(performanceScore);

    return {
      playerName,
      sessionId: session.id,
      sessionStart: session.startTime,
      sessionEnd: session.endTime,
      durationMs: session.durationMs,
      role: participant.role,

      kills,
      deaths,
      assists,
      kdr,

      damageDealt: damageMetrics.totalDealt,
      damageTaken: damageMetrics.totalTaken,
      dps: damageMetrics.dps,
      peakDps: damageMetrics.peakDps,

      healingDone: healingMetrics.totalDone,
      healingReceived: healingMetrics.totalReceived,
      hps: healingMetrics.hps,
      overhealRate: healingMetrics.overhealRate,

      critRate: damageMetrics.critStats.critRate,

      performanceScore,
      performanceRating,
    };
  }

  /**
   * Calculate aggregate statistics across multiple sessions
   * @param sessions Array of combat sessions
   * @param playerName Name of the player to analyze
   * @returns Aggregate stats, or null if player not in any session
   */
  calculateAggregate(
    sessions: CombatSession[],
    playerName: string
  ): PlayerAggregateStats | null {
    // Calculate stats for each session where the player participated
    const sessionStats: PlayerSessionStats[] = [];

    for (const session of sessions) {
      const stats = this.calculateForSession(session, playerName);
      if (stats) {
        sessionStats.push(stats);
      }
    }

    if (sessionStats.length === 0) {
      return null;
    }

    // Aggregate totals
    const totalSessions = sessionStats.length;
    const totalCombatTimeMs = sessionStats.reduce((sum, s) => sum + s.durationMs, 0);

    const totalKills = sessionStats.reduce((sum, s) => sum + s.kills, 0);
    const totalDeaths = sessionStats.reduce((sum, s) => sum + s.deaths, 0);
    const totalAssists = sessionStats.reduce((sum, s) => sum + s.assists, 0);
    const overallKDR = totalKills / Math.max(totalDeaths, 1);

    const totalDamageDealt = sessionStats.reduce((sum, s) => sum + s.damageDealt, 0);
    const totalDamageTaken = sessionStats.reduce((sum, s) => sum + s.damageTaken, 0);
    const totalHealingDone = sessionStats.reduce((sum, s) => sum + s.healingDone, 0);
    const totalHealingReceived = sessionStats.reduce(
      (sum, s) => sum + s.healingReceived,
      0
    );

    // Calculate averages
    const avgDPS = sessionStats.reduce((sum, s) => sum + s.dps, 0) / totalSessions;
    const avgHPS = sessionStats.reduce((sum, s) => sum + s.hps, 0) / totalSessions;
    const avgKillsPerSession = totalKills / totalSessions;
    const avgDeathsPerSession = totalDeaths / totalSessions;

    // Performance distribution
    const avgPerformanceScore =
      sessionStats.reduce((sum, s) => sum + s.performanceScore, 0) / totalSessions;

    const performanceDistribution = this.calculatePerformanceDistribution(sessionStats);

    // Find best and worst fights
    const sortedByPerformance = [...sessionStats].sort(
      (a, b) => b.performanceScore - a.performanceScore
    );
    const bestFight = sortedByPerformance[0]!;
    const worstFight = sortedByPerformance[sortedByPerformance.length - 1]!;

    // Calculate trends
    const trends = this.trendCalculator.calculateTrends(sessionStats);

    // Calculate consistency
    const performanceScores = sessionStats.map((s) => s.performanceScore);
    const performanceVariance = this.trendCalculator.calculateVariance(performanceScores);
    const consistencyRating = this.trendCalculator.rateConsistency(performanceVariance);

    return {
      playerName,
      totalSessions,
      totalCombatTimeMs,

      totalKills,
      totalDeaths,
      totalAssists,
      overallKDR,

      totalDamageDealt,
      totalDamageTaken,
      totalHealingDone,
      totalHealingReceived,

      avgDPS,
      avgHPS,
      avgKillsPerSession,
      avgDeathsPerSession,

      avgPerformanceScore,
      performanceDistribution,

      bestFight,
      worstFight,

      dpsOverTime: trends.dpsOverTime,
      kdrOverTime: trends.kdrOverTime,
      performanceOverTime: trends.performanceOverTime,

      performanceVariance,
      consistencyRating,
    };
  }

  /**
   * Calculate aggregate stats for all unique players across sessions
   * @param sessions Array of combat sessions
   * @returns Map of player name to aggregate stats
   */
  calculateAllPlayers(
    sessions: CombatSession[]
  ): Map<string, PlayerAggregateStats> {
    // Collect all unique player names
    const playerNames = new Set<string>();

    for (const session of sessions) {
      for (const participant of session.participants) {
        // Only include actual players, not NPCs
        if (participant.entity.isPlayer) {
          playerNames.add(participant.entity.name);
        }
      }
    }

    // Calculate aggregate stats for each player
    const result = new Map<string, PlayerAggregateStats>();

    for (const playerName of playerNames) {
      const stats = this.calculateAggregate(sessions, playerName);
      if (stats) {
        result.set(playerName, stats);
      }
    }

    return result;
  }

  /**
   * Count kills for a player in a session
   */
  private countKills(session: CombatSession, playerName: string): number {
    return session.events.filter(
      (e): e is DeathEvent =>
        e.eventType === EventType.DEATH && e.killer?.name === playerName
    ).length;
  }

  /**
   * Count deaths for a player in a session
   */
  private countDeaths(session: CombatSession, playerName: string): number {
    return session.events.filter(
      (e): e is DeathEvent =>
        e.eventType === EventType.DEATH && e.target.name === playerName
    ).length;
  }

  /**
   * Count assists for a player in a session
   * An assist is when you dealt damage to a target that died, but you weren't the killer
   */
  private countAssists(session: CombatSession, playerName: string): number {
    // Get all death events
    const deathEvents = session.events.filter(
      (e): e is DeathEvent => e.eventType === EventType.DEATH
    );

    // Get all damage events from this player
    const playerDamageEvents = session.events.filter(
      (e): e is DamageEvent =>
        e.eventType === EventType.DAMAGE_DEALT && e.source.name === playerName
    );

    // Count deaths where player dealt damage but wasn't the killer
    let assists = 0;

    for (const death of deathEvents) {
      // Skip if player was the killer (not an assist)
      if (death.killer?.name === playerName) continue;

      // Check if player dealt any damage to this target
      const dealtDamage = playerDamageEvents.some(
        (d) => d.target.name === death.target.name
      );

      if (dealtDamage) {
        assists++;
      }
    }

    return assists;
  }

  /**
   * Get all participant metrics for a session
   */
  private getAllParticipantMetrics(session: CombatSession): ParticipantMetrics[] {
    const metrics: ParticipantMetrics[] = [];

    for (const participant of session.participants) {
      const damage = this.damageCalculator.calculateForEntity(
        session.events,
        participant.entity,
        session.durationMs
      );

      const healing = this.healingCalculator.calculateForEntity(
        session.events,
        participant.entity,
        session.durationMs
      );

      metrics.push({
        entity: participant.entity,
        role: participant.role,
        damage,
        healing,
      });
    }

    return metrics;
  }

  /**
   * Calculate the distribution of performance ratings across sessions
   */
  private calculatePerformanceDistribution(
    sessionStats: PlayerSessionStats[]
  ): Record<PerformanceRating, number> {
    const distribution: Record<PerformanceRating, number> = {
      EXCELLENT: 0,
      GOOD: 0,
      AVERAGE: 0,
      BELOW_AVERAGE: 0,
      POOR: 0,
    };

    for (const stats of sessionStats) {
      distribution[stats.performanceRating]++;
    }

    return distribution;
  }

  /**
   * Get the current configuration
   */
  getConfig(): PlayerStatsConfig {
    return { ...this.config };
  }
}
