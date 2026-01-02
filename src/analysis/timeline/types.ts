import type { CombatEvent, Entity, EventType } from '../../types/index.js';

/**
 * Visual category for timeline markers
 * Used for color-coding or grouping in UI
 */
export type TimelineMarkerCategory =
  | 'DAMAGE_OUTGOING'
  | 'DAMAGE_INCOMING'
  | 'HEALING_OUTGOING'
  | 'HEALING_INCOMING'
  | 'CROWD_CONTROL'
  | 'DEATH';

/**
 * Type-specific details for timeline entries
 */
export interface TimelineEntryDetails {
  /** For damage: action name (spell/style) */
  actionName?: string;
  /** For damage: damage type (CRUSH, HEAT, etc.) */
  damageType?: string;
  /** For damage/healing: was critical hit */
  isCritical?: boolean;
  /** For damage: amount absorbed */
  absorbedAmount?: number;
  /** For healing: spell name */
  spellName?: string;
  /** For healing: overheal amount */
  overheal?: number;
  /** For CC: effect type (STUN, MESMERIZE, etc.) */
  ccEffect?: string;
  /** For CC: was resisted */
  wasResisted?: boolean;
  /** For death: killer entity */
  killer?: Entity;
}

/**
 * A single entry in the event timeline
 */
export interface TimelineEntry {
  /** Unique identifier for this entry */
  id: string;
  /** Original combat event reference */
  event: CombatEvent;
  /** Absolute timestamp (Date object) */
  timestamp: Date;
  /** Formatted absolute timestamp (HH:MM:SS) */
  formattedTimestamp: string;
  /** Milliseconds relative to session start */
  relativeTimeMs: number;
  /** Formatted relative time (+MM:SS) */
  formattedRelativeTime: string;
  /** Event type category */
  eventType: EventType;
  /** Visual marker category for UI grouping */
  markerCategory: TimelineMarkerCategory;
  /** Human-readable description (who did what to whom) */
  description: string;
  /** Source entity (if applicable) */
  source?: Entity;
  /** Target entity (if applicable) */
  target?: Entity;
  /** Primary numerical value (damage amount, heal amount, CC duration) */
  primaryValue?: number;
  /** Unit for primary value ("damage", "healing", "seconds") */
  primaryValueUnit?: string;
  /** Type-specific details */
  details: TimelineEntryDetails;
}

/**
 * Filter configuration for timeline
 */
export interface TimelineFilterConfig {
  /** Event types to include (empty = all) */
  eventTypes?: EventType[];
  /** Entity name to filter by (source OR target matches) */
  entityName?: string;
  /** Include events where entity is source (default: true) */
  includeAsSource?: boolean;
  /** Include events where entity is target (default: true) */
  includeAsTarget?: boolean;
  /** Start time offset from session start (ms) */
  startTimeMs?: number;
  /** End time offset from session start (ms) */
  endTimeMs?: number;
  /** Minimum value threshold (e.g., damage > 100) */
  minValue?: number;
  /** Include only critical hits/heals */
  criticalOnly?: boolean;
  /** Marker categories to include (empty = all) */
  markerCategories?: TimelineMarkerCategory[];
}

/**
 * Default filter configuration (shows all events)
 */
export const DEFAULT_TIMELINE_FILTER: TimelineFilterConfig = {
  eventTypes: [],
  entityName: undefined,
  includeAsSource: true,
  includeAsTarget: true,
  startTimeMs: undefined,
  endTimeMs: undefined,
  minValue: undefined,
  criticalOnly: false,
  markerCategories: [],
};

/**
 * Statistics about the visible timeline entries
 */
export interface TimelineStats {
  /** Total number of entries after filtering */
  totalEntries: number;
  /** Breakdown by event type */
  entriesByType: Partial<Record<EventType, number>>;
  /** Breakdown by marker category */
  entriesByCategory: Partial<Record<TimelineMarkerCategory, number>>;
  /** Total damage shown */
  totalDamage: number;
  /** Total healing shown */
  totalHealing: number;
  /** Number of deaths shown */
  deathCount: number;
  /** Number of CC events shown */
  ccCount: number;
}

/**
 * Visible time range information
 */
export interface TimelineVisibleRange {
  /** Start time in ms from session start */
  startMs: number;
  /** End time in ms from session start */
  endMs: number;
  /** Duration of visible range in ms */
  durationMs: number;
}

/**
 * Complete timeline view result
 */
export interface TimelineView {
  /** Session ID this timeline is for */
  sessionId: string;
  /** Session start time */
  sessionStart: Date;
  /** Session end time */
  sessionEnd: Date;
  /** Session duration in milliseconds */
  sessionDurationMs: number;
  /** Filtered timeline entries (chronological order) */
  entries: TimelineEntry[];
  /** Statistics about visible entries */
  stats: TimelineStats;
  /** The filter configuration that was applied */
  appliedFilter: TimelineFilterConfig;
  /** Time range of visible entries */
  visibleTimeRange: TimelineVisibleRange;
}

/**
 * Configuration for timeline generation
 */
export interface TimelineConfig {
  /** Default filter to apply */
  defaultFilter: TimelineFilterConfig;
  /** Maximum entries to return (0 = unlimited) */
  maxEntries: number;
}

/**
 * Default timeline configuration
 */
export const DEFAULT_TIMELINE_CONFIG: TimelineConfig = {
  defaultFilter: DEFAULT_TIMELINE_FILTER,
  maxEntries: 0,
};
