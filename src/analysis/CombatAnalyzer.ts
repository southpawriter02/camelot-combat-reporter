import type { CombatEvent, ParsedLog } from '../types/index.js';
import type {
  AnalysisConfig,
  PartialAnalysisConfig,
  CombatSession,
  AnalysisResult,
  FightSummary,
  DamageMetrics,
  HealingMetrics,
  ParticipantMetrics,
} from './types/index.js';
import { DEFAULT_ANALYSIS_CONFIG } from './types/index.js';
import { SessionDetector } from './session/SessionDetector.js';
import { DamageCalculator } from './metrics/DamageCalculator.js';
import { HealingCalculator } from './metrics/HealingCalculator.js';
import { FightSummarizer } from './summary/FightSummarizer.js';
import {
  PlayerStatisticsCalculator,
  type PlayerSessionStats,
  type PlayerAggregateStats,
} from './player-stats/index.js';
import {
  TimelineGenerator,
  type TimelineView,
  type TimelineEntry,
  type TimelineFilterConfig,
} from './timeline/index.js';

/**
 * Main facade class for combat analysis
 *
 * Provides a unified API for:
 * - Detecting combat sessions from event streams
 * - Calculating damage and healing metrics
 * - Generating fight summaries with damage/healing meters
 *
 * @example
 * ```typescript
 * const analyzer = new CombatAnalyzer();
 * const result = analyzer.analyze(parsedLog);
 *
 * for (const session of result.sessions) {
 *   const summary = analyzer.getSummary(session);
 *   console.log(`Fight: ${summary.durationFormatted}`);
 *   for (const entry of summary.damageMeter) {
 *     console.log(`  ${entry.rank}. ${entry.entity.name}: ${entry.dps.toFixed(1)} DPS`);
 *   }
 * }
 * ```
 */
export class CombatAnalyzer {
  private config: AnalysisConfig;
  private sessionDetector: SessionDetector;
  private damageCalculator: DamageCalculator;
  private healingCalculator: HealingCalculator;
  private fightSummarizer: FightSummarizer;
  private playerStatisticsCalculator: PlayerStatisticsCalculator;
  private timelineGenerator: TimelineGenerator;

  constructor(config: PartialAnalysisConfig = {}) {
    this.config = {
      ...DEFAULT_ANALYSIS_CONFIG,
      ...config,
      session: { ...DEFAULT_ANALYSIS_CONFIG.session, ...config.session },
      metrics: { ...DEFAULT_ANALYSIS_CONFIG.metrics, ...config.metrics },
    };

    this.sessionDetector = new SessionDetector(this.config.session);
    this.damageCalculator = new DamageCalculator(this.config.metrics);
    this.healingCalculator = new HealingCalculator(this.config.metrics);
    this.fightSummarizer = new FightSummarizer({
      metrics: this.config.metrics,
    });
    this.playerStatisticsCalculator = new PlayerStatisticsCalculator();
    this.timelineGenerator = new TimelineGenerator();
  }

  /**
   * Analyze a parsed log and detect all combat sessions
   */
  analyze(parsedLog: ParsedLog): AnalysisResult {
    return this.analyzeEvents(parsedLog.events);
  }

  /**
   * Analyze raw events and detect all combat sessions
   */
  analyzeEvents(events: CombatEvent[]): AnalysisResult {
    if (events.length === 0) {
      const now = new Date();
      return {
        sessions: [],
        totalEvents: 0,
        sessionEvents: 0,
        downtimeEvents: 0,
        timeSpan: {
          start: now,
          end: now,
          durationMs: 0,
        },
      };
    }

    // Detect sessions
    const sessions = this.sessionDetector.detect(events);

    // Calculate event counts
    const sessionEvents = sessions.reduce((sum, s) => sum + s.events.length, 0);

    // Get time span
    const sortedEvents = [...events].sort(
      (a, b) => a.timestamp.getTime() - b.timestamp.getTime()
    );
    const start = sortedEvents[0]!.timestamp;
    const end = sortedEvents[sortedEvents.length - 1]!.timestamp;

    return {
      sessions,
      totalEvents: events.length,
      sessionEvents,
      downtimeEvents: events.length - sessionEvents,
      timeSpan: {
        start,
        end,
        durationMs: end.getTime() - start.getTime(),
      },
    };
  }

  /**
   * Detect combat sessions from events without full analysis
   */
  detectSessions(events: CombatEvent[]): CombatSession[] {
    return this.sessionDetector.detect(events);
  }

  /**
   * Get a complete fight summary for a session
   */
  getSummary(session: CombatSession): FightSummary {
    return this.fightSummarizer.summarize(session);
  }

  /**
   * Get damage metrics for a specific entity in a session
   */
  getEntityDamageMetrics(session: CombatSession, entityName: string): DamageMetrics | null {
    const participant = session.participants.find(
      (p) => p.entity.name === entityName
    );

    if (!participant) {
      return null;
    }

    return this.damageCalculator.calculateForEntity(
      session.events,
      participant.entity,
      session.durationMs
    );
  }

  /**
   * Get healing metrics for a specific entity in a session
   */
  getEntityHealingMetrics(
    session: CombatSession,
    entityName: string
  ): HealingMetrics | null {
    const participant = session.participants.find(
      (p) => p.entity.name === entityName
    );

    if (!participant) {
      return null;
    }

    return this.healingCalculator.calculateForEntity(
      session.events,
      participant.entity,
      session.durationMs
    );
  }

  /**
   * Get all metrics for a specific entity in a session
   */
  getEntityMetrics(
    session: CombatSession,
    entityName: string
  ): ParticipantMetrics | null {
    const participant = session.participants.find(
      (p) => p.entity.name === entityName
    );

    if (!participant) {
      return null;
    }

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

    return {
      entity: participant.entity,
      role: participant.role,
      damage,
      healing,
    };
  }

  /**
   * Create a custom fight from a subset of events
   * Useful for manual fight boundary definition
   */
  defineFight(
    events: CombatEvent[],
    startTime?: Date,
    endTime?: Date
  ): CombatSession | null {
    if (events.length === 0) {
      return null;
    }

    const sortedEvents = [...events].sort(
      (a, b) => a.timestamp.getTime() - b.timestamp.getTime()
    );

    const effectiveStart = startTime ?? sortedEvents[0]!.timestamp;
    const effectiveEnd = endTime ?? sortedEvents[sortedEvents.length - 1]!.timestamp;

    // Filter events to the specified time range
    const filteredEvents = sortedEvents.filter(
      (e) =>
        e.timestamp.getTime() >= effectiveStart.getTime() &&
        e.timestamp.getTime() <= effectiveEnd.getTime()
    );

    if (filteredEvents.length === 0) {
      return null;
    }

    // Use session detector to create the session with proper participant tracking
    const sessions = this.sessionDetector.detect(filteredEvents);

    if (sessions.length === 0) {
      return null;
    }

    // If multiple sessions were detected, merge them into one
    if (sessions.length === 1) {
      return sessions[0]!;
    }

    // Merge all detected sessions into one
    return this.mergeSessions(sessions);
  }

  /**
   * Merge multiple sessions into one
   */
  private mergeSessions(sessions: CombatSession[]): CombatSession {
    const sortedSessions = [...sessions].sort(
      (a, b) => a.startTime.getTime() - b.startTime.getTime()
    );

    const firstSession = sortedSessions[0]!;
    const lastSession = sortedSessions[sortedSessions.length - 1]!;

    // Combine all events
    const allEvents: CombatEvent[] = [];
    for (const session of sortedSessions) {
      allEvents.push(...session.events);
    }
    allEvents.sort((a, b) => a.timestamp.getTime() - b.timestamp.getTime());

    // Merge participants
    const participantMap = new Map(
      firstSession.participants.map((p) => [p.entity.name, p])
    );

    for (const session of sortedSessions.slice(1)) {
      for (const participant of session.participants) {
        const existing = participantMap.get(participant.entity.name);
        if (existing) {
          existing.lastSeen = participant.lastSeen;
          existing.eventCount += participant.eventCount;
        } else {
          participantMap.set(participant.entity.name, participant);
        }
      }
    }

    // Combine summaries
    const combinedSummary = {
      totalDamageDealt: sessions.reduce(
        (sum, s) => sum + s.summary.totalDamageDealt,
        0
      ),
      totalDamageTaken: sessions.reduce(
        (sum, s) => sum + s.summary.totalDamageTaken,
        0
      ),
      totalHealingDone: sessions.reduce(
        (sum, s) => sum + s.summary.totalHealingDone,
        0
      ),
      totalHealingReceived: sessions.reduce(
        (sum, s) => sum + s.summary.totalHealingReceived,
        0
      ),
      deathCount: sessions.reduce((sum, s) => sum + s.summary.deathCount, 0),
      ccEventCount: sessions.reduce((sum, s) => sum + s.summary.ccEventCount, 0),
      keyEvents: sessions.flatMap((s) => s.summary.keyEvents),
    };

    return {
      id: `merged-${firstSession.id}`,
      startTime: firstSession.startTime,
      endTime: lastSession.endTime,
      durationMs: lastSession.endTime.getTime() - firstSession.startTime.getTime(),
      events: allEvents,
      participants: Array.from(participantMap.values()),
      summary: combinedSummary,
    };
  }

  /**
   * Get the current configuration
   */
  getConfig(): AnalysisConfig {
    return { ...this.config };
  }

  /**
   * Get a formatted damage report for a session
   */
  getDamageReport(session: CombatSession): string {
    return this.fightSummarizer.getDamageReport(session);
  }

  /**
   * Get player statistics for a specific session
   *
   * @param session - The combat session to analyze
   * @param playerName - Name of the player to get stats for
   * @returns Player session stats or null if player not found
   */
  getPlayerSessionStats(
    session: CombatSession,
    playerName: string
  ): PlayerSessionStats | null {
    return this.playerStatisticsCalculator.calculateForSession(session, playerName);
  }

  /**
   * Get aggregate player statistics across multiple sessions
   *
   * @param sessions - Array of combat sessions to analyze
   * @param playerName - Name of the player to get stats for
   * @returns Aggregate player stats or null if player not found in any session
   */
  getPlayerAggregateStats(
    sessions: CombatSession[],
    playerName: string
  ): PlayerAggregateStats | null {
    return this.playerStatisticsCalculator.calculateAggregate(sessions, playerName);
  }

  /**
   * Get aggregate statistics for all players across multiple sessions
   *
   * @param sessions - Array of combat sessions to analyze
   * @returns Map of player names to their aggregate stats
   */
  getAllPlayerStats(sessions: CombatSession[]): Map<string, PlayerAggregateStats> {
    return this.playerStatisticsCalculator.calculateAllPlayers(sessions);
  }

  /**
   * Get a timeline view for a session with optional filtering
   *
   * @param session - The combat session to generate timeline for
   * @param filter - Optional filter configuration
   * @returns Complete timeline view with entries, stats, and time range
   *
   * @example
   * ```typescript
   * // Get all events
   * const timeline = analyzer.getTimeline(session);
   *
   * // Get only damage events above 100
   * const damageTimeline = analyzer.getTimeline(session, {
   *   eventTypes: [EventType.DAMAGE_DEALT, EventType.DAMAGE_RECEIVED],
   *   minValue: 100,
   * });
   *
   * // Get events for a specific player
   * const playerTimeline = analyzer.getTimeline(session, {
   *   entityName: 'PlayerName',
   * });
   *
   * // Get events in a time window
   * const windowTimeline = analyzer.getTimeline(session, {
   *   startTimeMs: 30000,  // 30 seconds in
   *   endTimeMs: 60000,    // 60 seconds in
   * });
   * ```
   */
  getTimeline(
    session: CombatSession,
    filter?: Partial<TimelineFilterConfig>
  ): TimelineView {
    return this.timelineGenerator.generate(session, filter);
  }

  /**
   * Create a single timeline entry from a combat event
   *
   * @param event - The combat event to convert
   * @param sessionStartTime - Start time of the session for relative timing
   * @returns A rich timeline entry representation
   */
  getTimelineEntry(event: CombatEvent, sessionStartTime: Date): TimelineEntry {
    return this.timelineGenerator.createEntry(event, sessionStartTime);
  }
}
