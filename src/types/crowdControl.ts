import { CrowdControlEffect, EventType } from './enums.js';
import { BaseEvent, Entity } from './events.js';

/**
 * Event representing a crowd control effect
 */
export interface CrowdControlEvent extends BaseEvent {
  eventType: EventType.CROWD_CONTROL;
  /** Who applied the CC (may be unknown) */
  source?: Entity;
  /** Who is affected by the CC */
  target: Entity;
  /** Type of CC effect */
  effect: CrowdControlEffect;
  /** Duration in seconds */
  duration: number;
  /** True if the target resisted the effect */
  isResisted: boolean;
}

/**
 * Helper to create a crowd control event with default values
 */
export function createCrowdControlEvent(
  base: Omit<CrowdControlEvent, 'isResisted'> & Partial<Pick<CrowdControlEvent, 'isResisted'>>
): CrowdControlEvent {
  return {
    ...base,
    isResisted: base.isResisted ?? false,
  };
}
