/**
 * Streaming module types for real-time log monitoring
 */
import type { CombatEvent, Entity } from '../types/index.js';
import type {
  CombatSession,
  SessionParticipant,
  SessionSummary,
  SessionUpdate,
} from '../analysis/types/session.js';

// Re-export SessionUpdate for convenience
export type { SessionUpdate };

/**
 * File change event emitted by FileWatcher
 */
export interface FileChangeEvent {
  /** Type of change detected */
  type: 'change' | 'rename' | 'rotate';
  /** Name of the file */
  filename: string;
  /** Current file size in bytes */
  currentSize: number;
  /** Current inode number (for rotation detection) */
  currentInode: number;
  /** Previous file size (if known) */
  previousSize?: number;
  /** Previous inode (if known) */
  previousInode?: number;
}

/**
 * Configuration for FileWatcher
 */
export interface FileWatcherConfig {
  /** Debounce delay for rapid changes (ms) */
  debounceMs: number;
  /** Use polling instead of fs.watch */
  usePolling: boolean;
  /** Polling interval if usePolling is true (ms) */
  pollIntervalMs: number;
}

/**
 * A line read from the log file
 */
export interface TailLine {
  /** The line content (without newline) */
  content: string;
  /** Line number (1-based) */
  lineNumber: number;
  /** Byte offset where this line starts */
  byteOffset: number;
}

/**
 * Position in a file for resuming tailing
 */
export interface TailPosition {
  /** Byte offset in the file */
  byteOffset: number;
  /** Line number (1-based) */
  lineNumber: number;
}

/**
 * Configuration for LogTailer
 */
export interface LogTailerConfig {
  /** Size of read buffer in bytes */
  bufferSize: number;
  /** Starting position (default: end of file) */
  startPosition?: TailPosition;
  /** Start from beginning of file instead of end */
  fromBeginning: boolean;
}

/**
 * Event hierarchy for StreamingParser emissions
 */
export type StreamingEventType =
  | 'line'
  | 'event'
  | 'event:damage'
  | 'event:damage:dealt'
  | 'event:damage:received'
  | 'event:healing'
  | 'event:healing:done'
  | 'event:healing:received'
  | 'event:death'
  | 'event:cc'
  | 'event:unknown'
  | 'session:start'
  | 'session:update'
  | 'session:end'
  | 'file:change'
  | 'file:rotate'
  | 'error';

/**
 * State of the session detection state machine
 */
export type SessionState = 'idle' | 'active' | 'ending';

/**
 * Internal state for a participant during session building
 */
export interface SessionParticipantState {
  entity: Entity;
  firstSeen: Date;
  lastSeen: Date;
  eventCount: number;
  damageDealt: number;
  healingDone: number;
  deathCount: number;
}

/**
 * Active session being built by StreamingSessionDetector
 */
export interface ActiveSession {
  /** Session ID */
  id: string;
  /** Current state */
  state: SessionState;
  /** When session started */
  startTime: Date;
  /** Last event timestamp */
  lastEventTime: Date;
  /** Events in this session */
  events: CombatEvent[];
  /** Participant state tracking */
  participantMap: Map<string, SessionParticipantState>;
}

/**
 * Configuration for StreamingSessionDetector
 */
export interface SessionDetectorConfig {
  /** Inactivity timeout to end session (ms) */
  inactivityTimeoutMs: number;
  /** Minimum events to consider a valid session */
  minEventsForSession: number;
  /** Minimum duration to consider a valid session (ms) */
  minDurationMs: number;
}

/**
 * Configuration for a webhook endpoint
 */
export interface WebhookConfig {
  /** Webhook URL to POST to */
  url: string;
  /** Maximum retry attempts */
  maxRetries: number;
  /** Initial retry delay (ms) */
  retryDelayMs: number;
  /** Multiplier for exponential backoff */
  retryMultiplier: number;
  /** Request timeout (ms) */
  timeoutMs: number;
  /** Additional headers to send */
  headers: Record<string, string>;
  /** Event types to send (empty = all) */
  events: string[];
  /** Unique identifier for this webhook */
  id?: string;
}

/**
 * Payload sent to webhooks
 */
export interface WebhookPayload {
  /** Type of event */
  eventType: string;
  /** ISO timestamp */
  timestamp: string;
  /** Event data (CombatEvent or SessionUpdate) */
  data: CombatEvent | SessionUpdate | TailLine;
  /** Additional context */
  metadata: {
    /** Source filename */
    filename: string;
    /** Line number if applicable */
    lineNumber?: number;
    /** Session ID if applicable */
    sessionId?: string;
  };
}

/**
 * Result of a webhook delivery attempt
 */
export interface WebhookDeliveryResult {
  /** Webhook ID */
  webhookId: string;
  /** Whether delivery succeeded */
  success: boolean;
  /** HTTP status code if applicable */
  statusCode?: number;
  /** Error message if failed */
  error?: string;
  /** Number of attempts made */
  attempts: number;
  /** Response body if available */
  responseBody?: string;
}

/**
 * Failed webhook delivery for dead-letter queue
 */
export interface DeadLetterEntry {
  /** Original webhook config */
  webhook: WebhookConfig;
  /** Payload that failed */
  payload: WebhookPayload;
  /** Final error */
  error: string;
  /** Timestamp of final failure */
  failedAt: Date;
  /** Total attempts made */
  attempts: number;
}

/**
 * State of the RealTimeMonitor
 */
export type MonitorState =
  | 'stopped'
  | 'starting'
  | 'running'
  | 'paused'
  | 'stopping'
  | 'error';

/**
 * Configuration for RealTimeMonitor
 */
export interface RealTimeMonitorConfig {
  /** FileWatcher configuration */
  fileWatcher: Partial<FileWatcherConfig>;
  /** LogTailer configuration */
  logTailer: Partial<LogTailerConfig>;
  /** SessionDetector configuration */
  sessionDetector: Partial<SessionDetectorConfig>;
  /** Whether to follow log rotation */
  followRotation: boolean;
}

/**
 * Statistics from RealTimeMonitor
 */
export interface MonitorStats {
  /** Total lines processed */
  linesProcessed: number;
  /** Total events parsed */
  eventsEmitted: number;
  /** Events by type */
  eventsByType: Record<string, number>;
  /** Sessions detected */
  sessionsDetected: number;
  /** Webhooks delivered successfully */
  webhooksDelivered: number;
  /** Webhooks failed */
  webhooksFailed: number;
  /** Monitor start time */
  startedAt?: Date;
  /** Total runtime in ms */
  runtimeMs: number;
}

/**
 * Status information from RealTimeMonitor
 */
export interface MonitorStatus {
  /** Current monitor state */
  state: MonitorState;
  /** File being monitored */
  filename?: string;
  /** Current position in file */
  position: TailPosition;
  /** Active session info if any */
  activeSession?: {
    id: string;
    eventCount: number;
    durationMs: number;
  };
  /** Statistics */
  stats: MonitorStats;
  /** Last error if in error state */
  lastError?: string;
}

/**
 * Options for starting the monitor
 */
export interface MonitorStartOptions {
  /** Position to resume from */
  resumeFrom?: TailPosition;
  /** Start from beginning of file */
  fromBeginning?: boolean;
}

/**
 * Default configurations
 */
export const DEFAULT_FILE_WATCHER_CONFIG: FileWatcherConfig = {
  debounceMs: 100,
  usePolling: false,
  pollIntervalMs: 1000,
};

export const DEFAULT_LOG_TAILER_CONFIG: LogTailerConfig = {
  bufferSize: 64 * 1024, // 64KB
  fromBeginning: false,
};

export const DEFAULT_SESSION_DETECTOR_CONFIG: SessionDetectorConfig = {
  inactivityTimeoutMs: 30000, // 30 seconds
  minEventsForSession: 3,
  minDurationMs: 1000, // 1 second
};

export const DEFAULT_WEBHOOK_CONFIG: Partial<WebhookConfig> = {
  maxRetries: 3,
  retryDelayMs: 1000,
  retryMultiplier: 2,
  timeoutMs: 5000,
  headers: {},
  events: [],
};

export const DEFAULT_MONITOR_CONFIG: RealTimeMonitorConfig = {
  fileWatcher: DEFAULT_FILE_WATCHER_CONFIG,
  logTailer: DEFAULT_LOG_TAILER_CONFIG,
  sessionDetector: DEFAULT_SESSION_DETECTOR_CONFIG,
  followRotation: true,
};
