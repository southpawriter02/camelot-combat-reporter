import { ActionType, DamageType, EventType } from './enums.js';
import { BaseEvent, Entity } from './events.js';

/**
 * Event representing damage dealt or received
 */
export interface DamageEvent extends BaseEvent {
  eventType: EventType.DAMAGE_DEALT | EventType.DAMAGE_RECEIVED;
  /** Who dealt the damage */
  source: Entity;
  /** Who received the damage */
  target: Entity;
  /** Raw damage amount before absorption */
  amount: number;
  /** Amount absorbed (from "(-25)" notation) */
  absorbedAmount: number;
  /** Actual damage dealt (amount - absorbedAmount) */
  effectiveAmount: number;
  /** Type of damage (physical or magical) */
  damageType: DamageType;
  /** Type of action (melee, spell, style) */
  actionType: ActionType;
  /** Name of spell/style if applicable */
  actionName?: string;
  /** True if this was a critical hit */
  isCritical: boolean;
  /** True if attack was blocked */
  isBlocked: boolean;
  /** True if attack was parried */
  isParried: boolean;
  /** True if attack was evaded */
  isEvaded: boolean;
  /** Name of weapon used if specified */
  weaponName?: string;
}

/**
 * Helper to create a damage event with default values
 */
export function createDamageEvent(
  base: Omit<
    DamageEvent,
    | 'effectiveAmount'
    | 'absorbedAmount'
    | 'isCritical'
    | 'isBlocked'
    | 'isParried'
    | 'isEvaded'
    | 'damageType'
    | 'actionType'
  > &
    Partial<
      Pick<
        DamageEvent,
        | 'absorbedAmount'
        | 'isCritical'
        | 'isBlocked'
        | 'isParried'
        | 'isEvaded'
        | 'damageType'
        | 'actionType'
      >
    >
): DamageEvent {
  const absorbedAmount = base.absorbedAmount ?? 0;
  return {
    ...base,
    absorbedAmount,
    effectiveAmount: base.amount - absorbedAmount,
    damageType: base.damageType ?? DamageType.UNKNOWN,
    actionType: base.actionType ?? ActionType.UNKNOWN,
    isCritical: base.isCritical ?? false,
    isBlocked: base.isBlocked ?? false,
    isParried: base.isParried ?? false,
    isEvaded: base.isEvaded ?? false,
  };
}
