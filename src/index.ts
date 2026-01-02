/**
 * DAoC Combat Log Parser
 *
 * A TypeScript library for parsing Dark Age of Camelot combat logs.
 *
 * @example
 * ```typescript
 * import { LogParser } from 'camelot-combat-reporter';
 *
 * const parser = new LogParser();
 *
 * // Parse a file
 * const result = await parser.parseFile('./chat.log');
 * console.log(`Parsed ${result.events.length} events`);
 *
 * // Parse a single line
 * const event = parser.parseLine('[12:34:56] You hit the goblin for 150 damage!');
 * if (event && event.eventType === 'DAMAGE_DEALT') {
 *   console.log(`Dealt ${event.amount} damage to ${event.target.name}`);
 * }
 * ```
 */

// Main Parser
export { LogParser } from './parser/index.js';
export { LineParser, type LineParseResult, type LineParserOptions } from './parser/index.js';

// Pattern system (for extension)
export {
  PatternRegistry,
  DamagePatternHandler,
  HealingPatternHandler,
  CrowdControlPatternHandler,
  type PatternHandler,
} from './parser/index.js';

// File utilities
export { LogFileReader, LogFileDetector, type LineInfo, type ValidationResult } from './file/index.js';

// Types
export {
  // Enums
  EventType,
  DamageType,
  ActionType,
  CrowdControlEffect,
  EntityType,
  Realm,
  LogType,
  // Interfaces
  type Entity,
  type BaseEvent,
  type UnknownEvent,
  type DeathEvent,
  type DamageEvent,
  type HealingEvent,
  type CrowdControlEvent,
  type CombatEvent,
  type ParseResult,
  type ParsedLog,
  type LogMetadata,
  type ParserConfig,
  type LogFileInfo,
  // Helper functions
  createSelfEntity,
  createEntity,
  createDamageEvent,
  createHealingEvent,
  createCrowdControlEvent,
} from './types/index.js';

// Errors
export {
  ErrorCode,
  ParseErrorReason,
  ParserError,
  ParseLineError,
  FileError,
} from './errors/index.js';

// Utilities
export { extractTimestamp, hasValidTimestamp, type TimestampResult } from './utils/index.js';

// Combat Analysis
export {
  // Main Analyzer
  CombatAnalyzer,
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
  // Sub-components
  SessionDetector,
  DPSCalculator,
  DamageCalculator,
  HealingCalculator,
  KeyEventDetector,
  FightSummarizer,
  type TimelinePoint,
  type PeakResult,
  type KeyEventConfig,
  type FightSummarizerConfig,
  DEFAULT_KEY_EVENT_CONFIG,
  DEFAULT_FIGHT_SUMMARIZER_CONFIG,
  // Time utilities
  formatDuration,
  calculateDuration,
  getRelativeTime,
  formatTimestamp,
  formatRelativeTime,
  // Player Statistics
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
  PerformanceScorer,
  type PerformanceInput,
  TrendCalculator,
  type TrendData,
  PlayerStatisticsCalculator,
  // Timeline
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
  TimelineEventFormatter,
  TimelineFilter,
  TimelineGenerator,
} from './analysis/index.js';

// Real-time Streaming
export {
  // Main orchestrator
  RealTimeMonitor,
  type RealTimeMonitorEvents,
  // Components
  FileWatcher,
  type FileWatcherEvents,
  LogTailer,
  StreamingParser,
  type StreamingParserEvents,
  StreamingSessionDetector,
  type SessionDetectorEvents,
  WebhookNotifier,
  type WebhookNotifierEvents,
  // Types
  type FileChangeEvent,
  type FileWatcherConfig,
  type TailLine,
  type TailPosition,
  type LogTailerConfig,
  type StreamingEventType,
  type SessionState,
  type SessionParticipantState,
  type ActiveSession,
  type SessionDetectorConfig,
  type WebhookConfig,
  type WebhookPayload,
  type WebhookDeliveryResult,
  type DeadLetterEntry,
  type MonitorState,
  type MonitorStatus,
  type MonitorStats,
  type MonitorStartOptions,
  type RealTimeMonitorConfig,
  // Default configs
  DEFAULT_FILE_WATCHER_CONFIG,
  DEFAULT_LOG_TAILER_CONFIG,
  DEFAULT_SESSION_DETECTOR_CONFIG,
  DEFAULT_WEBHOOK_CONFIG,
  DEFAULT_MONITOR_CONFIG,
} from './streaming/index.js';
