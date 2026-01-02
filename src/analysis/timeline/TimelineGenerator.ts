import type { CombatEvent, EventType } from '../../types/index.js';
import type { CombatSession } from '../types/index.js';
import type {
  TimelineEntry,
  TimelineView,
  TimelineStats,
  TimelineFilterConfig,
  TimelineConfig,
  TimelineMarkerCategory,
  TimelineVisibleRange,
} from './types.js';
import { DEFAULT_TIMELINE_CONFIG, DEFAULT_TIMELINE_FILTER } from './types.js';
import { TimelineEventFormatter } from './TimelineEventFormatter.js';
import { TimelineFilter } from './TimelineFilter.js';

/**
 * Main orchestrator class for generating timeline views
 */
export class TimelineGenerator {
  private config: TimelineConfig;
  private formatter: TimelineEventFormatter;
  private filter: TimelineFilter;

  constructor(config: Partial<TimelineConfig> = {}) {
    this.config = {
      ...DEFAULT_TIMELINE_CONFIG,
      ...config,
      defaultFilter: {
        ...DEFAULT_TIMELINE_CONFIG.defaultFilter,
        ...config.defaultFilter,
      },
    };
    this.formatter = new TimelineEventFormatter();
    this.filter = new TimelineFilter(this.config.defaultFilter);
  }

  /**
   * Generate a complete timeline view for a session
   * @param session The combat session to generate timeline for
   * @param filterConfig Optional filter configuration to apply
   * @returns Complete timeline view with entries, stats, and time range
   */
  generate(
    session: CombatSession,
    filterConfig?: Partial<TimelineFilterConfig>
  ): TimelineView {
    // Merge provided filter with defaults
    const appliedFilter: TimelineFilterConfig = {
      ...DEFAULT_TIMELINE_FILTER,
      ...filterConfig,
    };
    this.filter.setConfig(appliedFilter);

    // Format all events to timeline entries
    let entries = session.events
      .map((event) => this.formatter.formatEvent(event, session.startTime))
      .sort((a, b) => a.relativeTimeMs - b.relativeTimeMs);

    // Apply filters
    entries = this.filter.filter(entries);

    // Apply max entries limit if configured
    if (this.config.maxEntries > 0 && entries.length > this.config.maxEntries) {
      entries = entries.slice(0, this.config.maxEntries);
    }

    // Calculate statistics
    const stats = this.calculateStats(entries);

    // Calculate visible time range
    const visibleTimeRange = this.calculateVisibleTimeRange(
      entries,
      session.durationMs
    );

    return {
      sessionId: session.id,
      sessionStart: session.startTime,
      sessionEnd: session.endTime,
      sessionDurationMs: session.durationMs,
      entries,
      stats,
      appliedFilter,
      visibleTimeRange,
    };
  }

  /**
   * Create a single timeline entry from an event
   * Useful for creating entries outside of full timeline generation
   * @param event The combat event to convert
   * @param sessionStartTime Start time of the session for relative timing
   * @returns A rich timeline entry representation
   */
  createEntry(event: CombatEvent, sessionStartTime: Date): TimelineEntry {
    return this.formatter.formatEvent(event, sessionStartTime);
  }

  /**
   * Calculate statistics for a set of entries
   */
  private calculateStats(entries: TimelineEntry[]): TimelineStats {
    const entriesByType: Partial<Record<EventType, number>> = {};
    const entriesByCategory: Partial<Record<TimelineMarkerCategory, number>> = {};

    let totalDamage = 0;
    let totalHealing = 0;
    let deathCount = 0;
    let ccCount = 0;

    for (const entry of entries) {
      // Count by event type
      entriesByType[entry.eventType] = (entriesByType[entry.eventType] ?? 0) + 1;

      // Count by marker category
      entriesByCategory[entry.markerCategory] =
        (entriesByCategory[entry.markerCategory] ?? 0) + 1;

      // Accumulate values
      if (entry.primaryValueUnit === 'damage' && entry.primaryValue) {
        totalDamage += entry.primaryValue;
      }
      if (entry.primaryValueUnit === 'healing' && entry.primaryValue) {
        totalHealing += entry.primaryValue;
      }
      if (entry.markerCategory === 'DEATH') {
        deathCount++;
      }
      if (entry.markerCategory === 'CROWD_CONTROL') {
        ccCount++;
      }
    }

    return {
      totalEntries: entries.length,
      entriesByType,
      entriesByCategory,
      totalDamage,
      totalHealing,
      deathCount,
      ccCount,
    };
  }

  /**
   * Calculate the visible time range from entries
   */
  private calculateVisibleTimeRange(
    entries: TimelineEntry[],
    sessionDurationMs: number
  ): TimelineVisibleRange {
    if (entries.length === 0) {
      return {
        startMs: 0,
        endMs: sessionDurationMs,
        durationMs: sessionDurationMs,
      };
    }

    const startMs = entries[0]!.relativeTimeMs;
    const endMs = entries[entries.length - 1]!.relativeTimeMs;

    return {
      startMs,
      endMs,
      durationMs: endMs - startMs,
    };
  }

  /**
   * Get current configuration
   * @returns Copy of current configuration
   */
  getConfig(): TimelineConfig {
    return { ...this.config };
  }
}
