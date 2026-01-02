// Enums
export {
  EventType,
  DamageType,
  ActionType,
  CrowdControlEffect,
  EntityType,
  Realm,
  LogType,
} from './enums.js';

// Events
export {
  Entity,
  BaseEvent,
  UnknownEvent,
  DeathEvent,
  createSelfEntity,
  createEntity,
} from './events.js';

// Damage
export { DamageEvent, createDamageEvent } from './damage.js';

// Healing
export { HealingEvent, createHealingEvent } from './healing.js';

// Crowd Control
export { CrowdControlEvent, createCrowdControlEvent } from './crowdControl.js';

// Union type for all combat events
import type { UnknownEvent, DeathEvent } from './events.js';
import type { DamageEvent } from './damage.js';
import type { HealingEvent } from './healing.js';
import type { CrowdControlEvent } from './crowdControl.js';

export type CombatEvent =
  | DamageEvent
  | HealingEvent
  | CrowdControlEvent
  | DeathEvent
  | UnknownEvent;

/**
 * Result from parsing a single line
 */
export interface ParseResult {
  success: boolean;
  event?: CombatEvent;
  error?: Error;
}

/**
 * Result from parsing an entire file
 */
export interface ParsedLog {
  filename: string;
  filePath: string;
  parseStartTime: Date;
  parseEndTime: Date;
  totalLines: number;
  parsedLines: number;
  errorLines: number;
  events: CombatEvent[];
  errors: Error[];
  metadata: LogMetadata;
}

/**
 * Metadata about a parsed log
 */
export interface LogMetadata {
  firstEventTime?: Date;
  lastEventTime?: Date;
  logDuration?: number;
  uniqueEntities: string[];
  eventTypeCounts: Record<string, number>;
}

/**
 * Configuration for the parser
 */
export interface ParserConfig {
  /** Continue parsing on errors (default: true) */
  continueOnError: boolean;
  /** Maximum errors before stopping (0 = unlimited) */
  maxErrors: number;
  /** Include unknown events in results (default: true) */
  includeUnknownEvents: boolean;
  /** Custom error handler */
  onError?: (error: Error) => void;
}

/**
 * Information about a log file
 */
export interface LogFileInfo {
  path: string;
  name: string;
  size: number;
  lastModified: Date;
}
