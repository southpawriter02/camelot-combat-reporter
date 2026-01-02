import type { CombatEvent } from '../../types/index.js';

/**
 * Interface for pattern handlers that match and parse log lines
 */
export interface PatternHandler {
  /** Name of the pattern handler for debugging */
  name: string;
  /** Priority (lower = higher priority) */
  priority: number;
  /** Check if this handler can parse the given message */
  canHandle(message: string): boolean;
  /** Parse the message and return a combat event */
  parse(
    message: string,
    timestamp: Date,
    rawTimestamp: string,
    rawLine: string,
    lineNumber: number
  ): CombatEvent | null;
}

/**
 * Registry that manages all pattern handlers
 */
export class PatternRegistry {
  private handlers: PatternHandler[] = [];

  /**
   * Register a new pattern handler
   */
  register(handler: PatternHandler): void {
    this.handlers.push(handler);
    // Keep handlers sorted by priority
    this.handlers.sort((a, b) => a.priority - b.priority);
  }

  /**
   * Find a handler that can parse the given message
   */
  findHandler(message: string): PatternHandler | undefined {
    return this.handlers.find((handler) => handler.canHandle(message));
  }

  /**
   * Get all registered handlers
   */
  getHandlers(): readonly PatternHandler[] {
    return this.handlers;
  }
}

// Export pattern handlers
export { DamagePatternHandler } from './damage.js';
export { HealingPatternHandler } from './healing.js';
export { CrowdControlPatternHandler } from './crowdControl.js';
