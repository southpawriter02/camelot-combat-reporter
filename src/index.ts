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
