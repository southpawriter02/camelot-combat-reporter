/**
 * Streaming module for real-time log monitoring
 *
 * @module streaming
 *
 * @example
 * ```typescript
 * import { RealTimeMonitor } from 'camelot-combat-reporter';
 *
 * const monitor = new RealTimeMonitor();
 *
 * // Subscribe to events
 * monitor.on('event:death', (event) => {
 *   console.log(`${event.target.name} died!`);
 * });
 *
 * monitor.on('session:end', (update) => {
 *   console.log(`Session ended. Duration: ${update.session.durationMs}ms`);
 * });
 *
 * // Add webhook for Discord bot
 * monitor.addWebhook({
 *   url: 'https://discord-bot.example.com/notify',
 *   events: ['session:end', 'event:death'],
 *   headers: { 'Authorization': 'Bearer <token>' },
 * });
 *
 * // Start monitoring
 * await monitor.start('/path/to/chat.log');
 *
 * // Later: save position and stop
 * const position = monitor.getPosition();
 * await monitor.stop();
 * ```
 */

// Types
export type {
  // File watching
  FileChangeEvent,
  FileWatcherConfig,

  // Tailing
  TailLine,
  TailPosition,
  LogTailerConfig,

  // Events
  StreamingEventType,

  // Session detection
  SessionState,
  SessionParticipantState,
  ActiveSession,
  SessionDetectorConfig,
  SessionUpdate,

  // Webhooks
  WebhookConfig,
  WebhookPayload,
  WebhookDeliveryResult,
  DeadLetterEntry,

  // Monitor
  MonitorState,
  MonitorStatus,
  MonitorStats,
  MonitorStartOptions,
  RealTimeMonitorConfig,
} from './types.js';

// Default configs
export {
  DEFAULT_FILE_WATCHER_CONFIG,
  DEFAULT_LOG_TAILER_CONFIG,
  DEFAULT_SESSION_DETECTOR_CONFIG,
  DEFAULT_WEBHOOK_CONFIG,
  DEFAULT_MONITOR_CONFIG,
} from './types.js';

// Components
export { FileWatcher, type FileWatcherEvents } from './FileWatcher.js';
export { LogTailer } from './LogTailer.js';
export { StreamingParser, type StreamingParserEvents } from './StreamingParser.js';
export {
  StreamingSessionDetector,
  type SessionDetectorEvents,
} from './StreamingSessionDetector.js';
export {
  WebhookNotifier,
  type WebhookNotifierEvents,
} from './WebhookNotifier.js';

// Main orchestrator
export {
  RealTimeMonitor,
  type RealTimeMonitorEvents,
} from './RealTimeMonitor.js';
