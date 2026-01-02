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
  type AnalysisConfigWithML,
  type PartialAnalysisConfigWithML,
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

// Database Integration
export {
  // Adapters
  DatabaseAdapter,
  SQLiteAdapter,
  PostgreSQLAdapter,
  // Query interfaces
  type BaseQuery,
  type EventQuery,
  type SessionQuery,
  type PlayerSessionStatsQuery,
  type StatsQuery,
  type AggregationQuery,
  // Types
  type DatabaseBackend,
  type SQLiteConnectionConfig,
  type PostgreSQLConnectionConfig,
  type DatabaseConnectionConfig,
  type DatabaseConfig,
  type Transaction,
  type PaginationOptions,
  type SortOptions,
  type RetentionAction,
  type RetentionTable,
  type TableRetentionPolicy,
  type ArchiveFormat,
  type RetentionConfig,
  type RetentionCleanupResult,
  type PersistenceConfig,
  type TimeBucket,
  type TimeBucketResult,
  type EntityAggregateResult,
  type DatabaseAdapterEvents,
  type PersistenceAdapterEvents,
  type RetentionSchedulerEvents,
  // Default configs
  DEFAULT_SQLITE_CONFIG,
  DEFAULT_POSTGRESQL_CONFIG,
  DEFAULT_DATABASE_CONFIG,
  DEFAULT_RETENTION_CONFIG,
  DEFAULT_PERSISTENCE_CONFIG,
  // Errors
  DatabaseError,
  ConnectionError,
  QueryError,
  MigrationError,
  TransactionError,
  NotConnectedError,
  NotFoundError,
  ConstraintError,
  RetentionError,
  ArchivalError,
  isDatabaseError,
  wrapError,
  // Retention
  RetentionScheduler,
  ArchivalManager,
  type ArchiveResult,
  type ArchiveMetadata,
  // Persistence
  BatchWriter,
  type BatchWriterConfig,
  DEFAULT_BATCH_WRITER_CONFIG,
  TransactionManager,
  type TransactionState,
  type ManagedTransaction,
  PersistenceAdapter,
} from './database/index.js';

// REST API
export {
  // Server
  ApiServer,
  SSEManager,
  // Types
  type ApiServerConfig,
  type AuthConfig,
  type RateLimitConfig,
  type CorsConfig,
  type SSEConfig,
  type OpenApiConfig,
  type OpenApiServer,
  type ApiKeyConfig,
  type ApiPermission,
  type ApiRequest,
  type ApiResponse,
  type ApiErrorBody,
  type ApiErrorResponse,
  type ApiSuccessResponse,
  type PaginationParams,
  type EventFilterParams,
  type SessionFilterParams,
  type PlayerStatsFilterParams,
  type RouteDefinition,
  type RouteHandler,
  type Middleware as ApiMiddleware,
  type NextFunction,
  type HttpMethod,
  type OpenApiOperation,
  type OpenApiParameter,
  type OpenApiRequestBody,
  type OpenApiResponseDef,
  type OpenApiSchema,
  type ServerState,
  type ServerStatus,
  type SSEConnection,
  type SSEEventData,
  type ApiServerEvents,
  // Default configs
  DEFAULT_API_SERVER_CONFIG,
  DEFAULT_AUTH_CONFIG,
  DEFAULT_RATE_LIMIT_CONFIG,
  DEFAULT_CORS_CONFIG,
  DEFAULT_SSE_CONFIG,
  DEFAULT_OPENAPI_CONFIG,
  // Errors
  ApiError,
  BadRequestError,
  AuthenticationError,
  ForbiddenError,
  NotFoundError as ApiNotFoundError,
  MethodNotAllowedError,
  ConflictError,
  ValidationError,
  RateLimitError,
  InternalError,
  ServiceUnavailableError,
  isApiError,
  wrapError as wrapApiError,
  // Middleware
  createAuthMiddleware,
  requirePermission,
  hasPermission,
  createRateLimitMiddleware,
  createCorsMiddleware,
  createErrorHandler,
  handleError,
  withErrorHandling,
  // OpenAPI
  generateOpenApiSpec,
  getSchemas,
} from './api/index.js';

// Machine Learning
export {
  // Main Predictor
  MLPredictor,
  // Configuration
  DEFAULT_ML_CONFIG,
  MODEL_NAMES,
  MODEL_VERSIONS,
  // Types - Configuration
  type MLConfig,
  type PartialMLConfig,
  type ModelMetadata,
  // Types - Features
  type FeatureVector,
  type NormalizationStats,
  type NormalizationConfig,
  type CombatStateFeatures,
  type BehaviorFeatures,
  type PlayerHistoryFeatures,
  // Types - Prediction Inputs
  type FightOutcomePredictionInput,
  type PlaystyleClassificationInput,
  type PerformancePredictionInput,
  type ThreatAssessmentInput,
  // Types - Prediction Outputs
  type PredictionFactor,
  type FightOutcomePrediction,
  type PlaystyleType,
  type PlaystyleTrait,
  type PlaystyleClassification,
  type PerformancePrediction,
  type ThreatCategory,
  type ThreatFactor,
  type ThreatAssessment,
  // Types - Training Data
  type FightOutcomeTrainingExample,
  type PlaystyleTrainingExample,
  type TrainingDataset,
  type TrainingDatasetMetadata,
  // Types - Events
  type MLPredictionEvent,
  // Feature Extraction
  FeatureExtractor,
  DEFAULT_FEATURE_EXTRACTOR_CONFIG,
  type FeatureExtractorConfig,
  Normalizer,
  type NormalizationMethod,
  CombatFeaturesExtractor,
  BehaviorFeaturesExtractor,
  // Model Management
  ModelLoader,
  type LoadedModel,
  type ModelLoadOptions,
  ModelRegistry,
  defaultModelRegistry,
  type ModelRegistryEntry,
  // Individual Predictors
  FightOutcomePredictor,
  PlaystyleClassifier,
  PerformancePredictor,
  ThreatAssessor,
  // Training Data Export
  TrainingDataExporter,
  resolveOutcomeByDeaths,
  resolveOutcomeByDamageRatio,
  type TrainingExportConfig,
  type TrainingDataStats,
  type OutcomeLabel,
} from './ml/index.js';
