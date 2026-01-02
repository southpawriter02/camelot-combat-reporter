import { v4 as uuidv4 } from 'uuid';
import type { PatternHandler } from './index.js';
import {
  type CombatEvent,
  type DamageEvent,
  EventType,
  DamageType,
  ActionType,
  createEntity,
  createSelfEntity,
  createDamageEvent,
} from '../../types/index.js';

// Damage patterns

// "You hit the goblin for 150 damage!"
const PLAYER_HIT_TARGET = /^You hit (?:the )?(.+?) for (\d+) damage!$/i;

// "The goblin hits you for 75 (-25) damage!"
const TARGET_HIT_PLAYER = /^(?:The )?(.+?) hits you for (\d+)(?: \(-(\d+)\))? damage!$/i;

// "You attack the goblin with your sword and hit for 100 damage!"
const PLAYER_ATTACK_WITH_WEAPON =
  /^You attack (?:the )?(.+?) with your (.+?) and hit for (\d+) damage!$/i;

// "You perform Side Stun and hit the troll for 180 damage!"
const PLAYER_STYLE_ATTACK = /^You perform (.+?) and hit (?:the )?(.+?) for (\d+) damage!$/i;

// "Your Greater Fireball hits the goblin for 300 damage!" (no damage type)
const PLAYER_SPELL_DAMAGE = /^Your (.+?) hits (?:the )?(.+?) for (\d+) damage!$/i;

// "Your Greater Fireball hits the goblin for 300 heat damage!"
const PLAYER_SPELL_DAMAGE_WITH_TYPE =
  /^Your (.+?) hits (?:the )?(.+?) for (\d+) (\w+) damage!$/i;

// "You critically hit the goblin for 250 damage!"
const PLAYER_CRITICAL_HIT = /^You critically hit (?:the )?(.+?) for (\d+) damage!$/i;

// "The goblin critically hits you for 150 damage!"
const TARGET_CRITICAL_HIT = /^(?:The )?(.+?) critically hits you for (\d+) damage!$/i;

// Map of damage type strings to enum values
const DAMAGE_TYPE_MAP: Record<string, DamageType> = {
  crush: DamageType.CRUSH,
  crushing: DamageType.CRUSH,
  slash: DamageType.SLASH,
  slashing: DamageType.SLASH,
  thrust: DamageType.THRUST,
  thrusting: DamageType.THRUST,
  heat: DamageType.HEAT,
  cold: DamageType.COLD,
  matter: DamageType.MATTER,
  body: DamageType.BODY,
  spirit: DamageType.SPIRIT,
  energy: DamageType.ENERGY,
};

/**
 * Parse damage type from string
 */
function parseDamageType(typeStr: string): DamageType {
  return DAMAGE_TYPE_MAP[typeStr.toLowerCase()] ?? DamageType.UNKNOWN;
}

/**
 * Pattern handler for damage events
 */
export class DamagePatternHandler implements PatternHandler {
  name = 'DamagePatternHandler';
  priority = 10;

  private patterns = [
    { regex: PLAYER_CRITICAL_HIT, parser: this.parsePlayerCriticalHit.bind(this) },
    { regex: TARGET_CRITICAL_HIT, parser: this.parseTargetCriticalHit.bind(this) },
    { regex: PLAYER_STYLE_ATTACK, parser: this.parsePlayerStyleAttack.bind(this) },
    { regex: PLAYER_SPELL_DAMAGE_WITH_TYPE, parser: this.parsePlayerSpellDamageWithType.bind(this) },
    { regex: PLAYER_SPELL_DAMAGE, parser: this.parsePlayerSpellDamage.bind(this) },
    { regex: PLAYER_ATTACK_WITH_WEAPON, parser: this.parsePlayerAttackWithWeapon.bind(this) },
    { regex: PLAYER_HIT_TARGET, parser: this.parsePlayerHitTarget.bind(this) },
    { regex: TARGET_HIT_PLAYER, parser: this.parseTargetHitPlayer.bind(this) },
  ];

  canHandle(message: string): boolean {
    return this.patterns.some(({ regex }) => regex.test(message));
  }

  parse(
    message: string,
    timestamp: Date,
    rawTimestamp: string,
    rawLine: string,
    lineNumber: number
  ): CombatEvent | null {
    for (const { regex, parser } of this.patterns) {
      const match = regex.exec(message);
      if (match) {
        return parser(match, timestamp, rawTimestamp, rawLine, lineNumber);
      }
    }
    return null;
  }

  private createBaseDamageEvent(
    timestamp: Date,
    rawTimestamp: string,
    rawLine: string,
    lineNumber: number
  ): Omit<DamageEvent, 'eventType' | 'source' | 'target' | 'amount'> {
    return {
      id: uuidv4(),
      timestamp,
      rawTimestamp,
      rawLine,
      lineNumber,
      absorbedAmount: 0,
      effectiveAmount: 0,
      damageType: DamageType.UNKNOWN,
      actionType: ActionType.UNKNOWN,
      isCritical: false,
      isBlocked: false,
      isParried: false,
      isEvaded: false,
    };
  }

  // "You hit the goblin for 150 damage!"
  private parsePlayerHitTarget(
    match: RegExpExecArray,
    timestamp: Date,
    rawTimestamp: string,
    rawLine: string,
    lineNumber: number
  ): DamageEvent {
    const [, targetName, amountStr] = match;
    return createDamageEvent({
      ...this.createBaseDamageEvent(timestamp, rawTimestamp, rawLine, lineNumber),
      eventType: EventType.DAMAGE_DEALT,
      source: createSelfEntity(),
      target: createEntity(targetName!),
      amount: parseInt(amountStr!, 10),
      actionType: ActionType.MELEE,
    });
  }

  // "The goblin hits you for 75 (-25) damage!"
  private parseTargetHitPlayer(
    match: RegExpExecArray,
    timestamp: Date,
    rawTimestamp: string,
    rawLine: string,
    lineNumber: number
  ): DamageEvent {
    const [, sourceName, amountStr, absorbedStr] = match;
    const amount = parseInt(amountStr!, 10);
    const absorbedAmount = absorbedStr ? parseInt(absorbedStr, 10) : 0;

    return createDamageEvent({
      ...this.createBaseDamageEvent(timestamp, rawTimestamp, rawLine, lineNumber),
      eventType: EventType.DAMAGE_RECEIVED,
      source: createEntity(sourceName!),
      target: createSelfEntity(),
      amount,
      absorbedAmount,
      actionType: ActionType.MELEE,
    });
  }

  // "You attack the goblin with your sword and hit for 100 damage!"
  private parsePlayerAttackWithWeapon(
    match: RegExpExecArray,
    timestamp: Date,
    rawTimestamp: string,
    rawLine: string,
    lineNumber: number
  ): DamageEvent {
    const [, targetName, weaponName, amountStr] = match;
    return createDamageEvent({
      ...this.createBaseDamageEvent(timestamp, rawTimestamp, rawLine, lineNumber),
      eventType: EventType.DAMAGE_DEALT,
      source: createSelfEntity(),
      target: createEntity(targetName!),
      amount: parseInt(amountStr!, 10),
      actionType: ActionType.MELEE,
      weaponName: weaponName!,
    });
  }

  // "You perform Side Stun and hit the troll for 180 damage!"
  private parsePlayerStyleAttack(
    match: RegExpExecArray,
    timestamp: Date,
    rawTimestamp: string,
    rawLine: string,
    lineNumber: number
  ): DamageEvent {
    const [, styleName, targetName, amountStr] = match;
    return createDamageEvent({
      ...this.createBaseDamageEvent(timestamp, rawTimestamp, rawLine, lineNumber),
      eventType: EventType.DAMAGE_DEALT,
      source: createSelfEntity(),
      target: createEntity(targetName!),
      amount: parseInt(amountStr!, 10),
      actionType: ActionType.STYLE,
      actionName: styleName!,
    });
  }

  // "Your Greater Fireball hits the goblin for 300 damage!"
  private parsePlayerSpellDamage(
    match: RegExpExecArray,
    timestamp: Date,
    rawTimestamp: string,
    rawLine: string,
    lineNumber: number
  ): DamageEvent {
    const [, spellName, targetName, amountStr] = match;
    return createDamageEvent({
      ...this.createBaseDamageEvent(timestamp, rawTimestamp, rawLine, lineNumber),
      eventType: EventType.DAMAGE_DEALT,
      source: createSelfEntity(),
      target: createEntity(targetName!),
      amount: parseInt(amountStr!, 10),
      actionType: ActionType.SPELL,
      actionName: spellName!,
    });
  }

  // "Your Greater Fireball hits the goblin for 300 heat damage!"
  private parsePlayerSpellDamageWithType(
    match: RegExpExecArray,
    timestamp: Date,
    rawTimestamp: string,
    rawLine: string,
    lineNumber: number
  ): DamageEvent {
    const [, spellName, targetName, amountStr, damageTypeStr] = match;
    return createDamageEvent({
      ...this.createBaseDamageEvent(timestamp, rawTimestamp, rawLine, lineNumber),
      eventType: EventType.DAMAGE_DEALT,
      source: createSelfEntity(),
      target: createEntity(targetName!),
      amount: parseInt(amountStr!, 10),
      actionType: ActionType.SPELL,
      actionName: spellName!,
      damageType: parseDamageType(damageTypeStr!),
    });
  }

  // "You critically hit the goblin for 250 damage!"
  private parsePlayerCriticalHit(
    match: RegExpExecArray,
    timestamp: Date,
    rawTimestamp: string,
    rawLine: string,
    lineNumber: number
  ): DamageEvent {
    const [, targetName, amountStr] = match;
    return createDamageEvent({
      ...this.createBaseDamageEvent(timestamp, rawTimestamp, rawLine, lineNumber),
      eventType: EventType.DAMAGE_DEALT,
      source: createSelfEntity(),
      target: createEntity(targetName!),
      amount: parseInt(amountStr!, 10),
      actionType: ActionType.MELEE,
      isCritical: true,
    });
  }

  // "The goblin critically hits you for 150 damage!"
  private parseTargetCriticalHit(
    match: RegExpExecArray,
    timestamp: Date,
    rawTimestamp: string,
    rawLine: string,
    lineNumber: number
  ): DamageEvent {
    const [, sourceName, amountStr] = match;
    return createDamageEvent({
      ...this.createBaseDamageEvent(timestamp, rawTimestamp, rawLine, lineNumber),
      eventType: EventType.DAMAGE_RECEIVED,
      source: createEntity(sourceName!),
      target: createSelfEntity(),
      amount: parseInt(amountStr!, 10),
      actionType: ActionType.MELEE,
      isCritical: true,
    });
  }
}
