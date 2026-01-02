import { EventType } from './enums.js';
import { BaseEvent, Entity } from './events.js';

/**
 * Event representing healing done or received
 */
export interface HealingEvent extends BaseEvent {
  eventType: EventType.HEALING_DONE | EventType.HEALING_RECEIVED;
  /** Who did the healing */
  source: Entity;
  /** Who received the healing */
  target: Entity;
  /** Heal amount */
  amount: number;
  /** Amount over max HP (overheal) */
  overheal: number;
  /** Actual HP restored (amount - overheal) */
  effectiveAmount: number;
  /** Name of healing spell */
  spellName: string;
  /** True if this was a critical heal */
  isCritical: boolean;
}

/**
 * Helper to create a healing event with default values
 */
export function createHealingEvent(
  base: Omit<HealingEvent, 'overheal' | 'effectiveAmount' | 'isCritical'> &
    Partial<Pick<HealingEvent, 'overheal' | 'isCritical'>>
): HealingEvent {
  const overheal = base.overheal ?? 0;
  return {
    ...base,
    overheal,
    effectiveAmount: base.amount - overheal,
    isCritical: base.isCritical ?? false,
  };
}
