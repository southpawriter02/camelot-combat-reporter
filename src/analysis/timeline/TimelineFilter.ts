import type { TimelineEntry, TimelineFilterConfig } from './types.js';
import { DEFAULT_TIMELINE_FILTER } from './types.js';

/**
 * Handles filtering of timeline entries based on configuration
 */
export class TimelineFilter {
  private config: TimelineFilterConfig;

  constructor(config: Partial<TimelineFilterConfig> = {}) {
    this.config = { ...DEFAULT_TIMELINE_FILTER, ...config };
  }

  /**
   * Apply filters to timeline entries
   * @param entries Array of timeline entries to filter
   * @returns Filtered array of entries
   */
  filter(entries: TimelineEntry[]): TimelineEntry[] {
    return entries.filter((entry) => this.matchesFilter(entry));
  }

  /**
   * Check if a single entry matches the current filter configuration
   * All filters use AND logic - entry must pass all active filters
   * @param entry The timeline entry to check
   * @returns true if entry matches all active filters
   */
  matchesFilter(entry: TimelineEntry): boolean {
    // Event type filter
    if (!this.matchesEventTypeFilter(entry)) {
      return false;
    }

    // Marker category filter
    if (!this.matchesMarkerCategoryFilter(entry)) {
      return false;
    }

    // Entity name filter
    if (!this.matchesEntityFilter(entry)) {
      return false;
    }

    // Time range filter
    if (!this.matchesTimeRangeFilter(entry)) {
      return false;
    }

    // Minimum value filter
    if (!this.matchesMinValueFilter(entry)) {
      return false;
    }

    // Critical only filter
    if (!this.matchesCriticalOnlyFilter(entry)) {
      return false;
    }

    return true;
  }

  /**
   * Check event type filter
   */
  private matchesEventTypeFilter(entry: TimelineEntry): boolean {
    const { eventTypes } = this.config;

    // Empty array means all types
    if (!eventTypes || eventTypes.length === 0) {
      return true;
    }

    return eventTypes.includes(entry.eventType);
  }

  /**
   * Check marker category filter
   */
  private matchesMarkerCategoryFilter(entry: TimelineEntry): boolean {
    const { markerCategories } = this.config;

    // Empty array means all categories
    if (!markerCategories || markerCategories.length === 0) {
      return true;
    }

    return markerCategories.includes(entry.markerCategory);
  }

  /**
   * Check entity name filter
   */
  private matchesEntityFilter(entry: TimelineEntry): boolean {
    const { entityName, includeAsSource, includeAsTarget } = this.config;

    // No entity filter means all entities
    if (!entityName) {
      return true;
    }

    const matchesSource = includeAsSource !== false && entry.source?.name === entityName;
    const matchesTarget = includeAsTarget !== false && entry.target?.name === entityName;

    return matchesSource || matchesTarget;
  }

  /**
   * Check time range filter
   */
  private matchesTimeRangeFilter(entry: TimelineEntry): boolean {
    const { startTimeMs, endTimeMs } = this.config;

    // Check start time
    if (startTimeMs !== undefined && entry.relativeTimeMs < startTimeMs) {
      return false;
    }

    // Check end time
    if (endTimeMs !== undefined && entry.relativeTimeMs > endTimeMs) {
      return false;
    }

    return true;
  }

  /**
   * Check minimum value filter
   */
  private matchesMinValueFilter(entry: TimelineEntry): boolean {
    const { minValue } = this.config;

    // No minimum means all values
    if (minValue === undefined) {
      return true;
    }

    // Events without a primary value don't match min value filter
    if (entry.primaryValue === undefined) {
      return false;
    }

    return entry.primaryValue >= minValue;
  }

  /**
   * Check critical only filter
   */
  private matchesCriticalOnlyFilter(entry: TimelineEntry): boolean {
    const { criticalOnly } = this.config;

    // Not filtering by critical
    if (!criticalOnly) {
      return true;
    }

    return entry.details.isCritical === true;
  }

  /**
   * Update filter configuration
   * @param config Partial configuration to merge with existing
   */
  setConfig(config: Partial<TimelineFilterConfig>): void {
    this.config = { ...this.config, ...config };
  }

  /**
   * Get current filter configuration
   * @returns Copy of current configuration
   */
  getConfig(): TimelineFilterConfig {
    return { ...this.config };
  }

  /**
   * Reset to default filter (show all)
   */
  reset(): void {
    this.config = { ...DEFAULT_TIMELINE_FILTER };
  }
}
