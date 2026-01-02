// Main analyzer
export { CombatAnalyzer } from './CombatAnalyzer.js';

// Types
export {
  // Configuration
  type CombatSessionConfig,
  type MetricsConfig,
  type AnalysisConfig,
  DEFAULT_SESSION_CONFIG,
  DEFAULT_METRICS_CONFIG,
  DEFAULT_ANALYSIS_CONFIG,
  // Session
  type ParticipantRole,
  type SessionParticipant,
  type KeyEventReason,
  type KeyEvent,
  type SessionSummary,
  type CombatSession,
  type SessionUpdate,
  // Metrics
  type CriticalStats,
  type ActionBreakdown,
  type DamageTypeBreakdown,
  type TargetBreakdown,
  type SourceBreakdown,
  type DamageMetrics,
  type SpellHealingBreakdown,
  type HealingTargetBreakdown,
  type HealingSourceBreakdown,
  type HealingMetrics,
  type ParticipantMetrics,
  // Summary
  type DamageMeterEntry,
  type HealingMeterEntry,
  type DeathTimelineEntry,
  type CCTimelineEntry,
  type FightSummary,
  type AnalysisResult,
} from './types/index.js';

// Session detection
export { SessionDetector } from './session/index.js';

// Metrics calculators
export {
  DPSCalculator,
  type TimelinePoint,
  type PeakResult,
} from './metrics/DPSCalculator.js';
export { DamageCalculator } from './metrics/DamageCalculator.js';
export { HealingCalculator } from './metrics/HealingCalculator.js';

// Summary
export {
  KeyEventDetector,
  type KeyEventConfig,
  DEFAULT_KEY_EVENT_CONFIG,
} from './summary/KeyEventDetector.js';
export {
  FightSummarizer,
  type FightSummarizerConfig,
  DEFAULT_FIGHT_SUMMARIZER_CONFIG,
} from './summary/FightSummarizer.js';

// Utilities
export {
  formatDuration,
  calculateDuration,
  getRelativeTime,
  formatTimestamp,
  formatRelativeTime,
} from './utils/timeUtils.js';

// Player Statistics
export {
  // Types
  type PerformanceRating,
  type ConsistencyRating,
  type TrendPoint,
  type PlayerSessionStats,
  type PlayerAggregateStats,
  type PlayerStatsConfig,
  type PerformanceThresholds,
  type ConsistencyThresholds,
  DEFAULT_PERFORMANCE_THRESHOLDS,
  DEFAULT_CONSISTENCY_THRESHOLDS,
  DEFAULT_PLAYER_STATS_CONFIG,
  // Classes
  PerformanceScorer,
  type PerformanceInput,
  TrendCalculator,
  type TrendData,
  PlayerStatisticsCalculator,
} from './player-stats/index.js';

// Timeline
export {
  // Types
  type TimelineMarkerCategory,
  type TimelineEntryDetails,
  type TimelineEntry,
  type TimelineFilterConfig,
  type TimelineStats,
  type TimelineVisibleRange,
  type TimelineView,
  type TimelineConfig,
  DEFAULT_TIMELINE_FILTER,
  DEFAULT_TIMELINE_CONFIG,
  // Classes
  TimelineEventFormatter,
  TimelineFilter,
  TimelineGenerator,
} from './timeline/index.js';
