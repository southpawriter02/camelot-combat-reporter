import { v4 as uuidv4 } from 'uuid';
import type { PatternHandler } from './index.js';
import {
  type CombatEvent,
  type HealingEvent,
  EventType,
  createEntity,
  createSelfEntity,
  createHealingEvent,
} from '../../types/index.js';

// Healing patterns

// "You cast Minor Heal on yourself for 200 hit points."
const PLAYER_HEAL_SELF = /^You cast (.+?) on yourself for (\d+) hit points\.$/i;

// "You cast Major Heal on Playername for 500 hit points."
const PLAYER_HEAL_TARGET = /^You cast (.+?) on (.+?) for (\d+) hit points\.$/i;

// "Playername heals you for 300 hit points."
const TARGET_HEALS_PLAYER = /^(.+?) heals you for (\d+) hit points\.$/i;

// "Your group heal heals the group for 400 hit points." (less common pattern)
const PLAYER_GROUP_HEAL = /^Your (.+?) heals (?:the )?group for (\d+) hit points\.$/i;

// "You are healed for 150 hit points." (passive heal)
const PLAYER_HEALED_PASSIVE = /^You are healed for (\d+) hit points\.$/i;

/**
 * Pattern handler for healing events
 */
export class HealingPatternHandler implements PatternHandler {
  name = 'HealingPatternHandler';
  priority = 20;

  private patterns = [
    { regex: PLAYER_HEAL_SELF, parser: this.parsePlayerHealSelf.bind(this) },
    { regex: PLAYER_HEAL_TARGET, parser: this.parsePlayerHealTarget.bind(this) },
    { regex: TARGET_HEALS_PLAYER, parser: this.parseTargetHealsPlayer.bind(this) },
    { regex: PLAYER_GROUP_HEAL, parser: this.parsePlayerGroupHeal.bind(this) },
    { regex: PLAYER_HEALED_PASSIVE, parser: this.parsePlayerHealedPassive.bind(this) },
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

  private createBaseHealingEvent(
    timestamp: Date,
    rawTimestamp: string,
    rawLine: string,
    lineNumber: number
  ): Omit<HealingEvent, 'eventType' | 'source' | 'target' | 'amount' | 'spellName'> {
    return {
      id: uuidv4(),
      timestamp,
      rawTimestamp,
      rawLine,
      lineNumber,
      overheal: 0,
      effectiveAmount: 0,
      isCritical: false,
    };
  }

  // "You cast Minor Heal on yourself for 200 hit points."
  private parsePlayerHealSelf(
    match: RegExpExecArray,
    timestamp: Date,
    rawTimestamp: string,
    rawLine: string,
    lineNumber: number
  ): HealingEvent {
    const [, spellName, amountStr] = match;
    const amount = parseInt(amountStr!, 10);
    return createHealingEvent({
      ...this.createBaseHealingEvent(timestamp, rawTimestamp, rawLine, lineNumber),
      eventType: EventType.HEALING_DONE,
      source: createSelfEntity(),
      target: createSelfEntity(),
      amount,
      spellName: spellName!,
    });
  }

  // "You cast Major Heal on Playername for 500 hit points."
  private parsePlayerHealTarget(
    match: RegExpExecArray,
    timestamp: Date,
    rawTimestamp: string,
    rawLine: string,
    lineNumber: number
  ): HealingEvent {
    const [, spellName, targetName, amountStr] = match;
    const amount = parseInt(amountStr!, 10);
    return createHealingEvent({
      ...this.createBaseHealingEvent(timestamp, rawTimestamp, rawLine, lineNumber),
      eventType: EventType.HEALING_DONE,
      source: createSelfEntity(),
      target: createEntity(targetName!),
      amount,
      spellName: spellName!,
    });
  }

  // "Playername heals you for 300 hit points."
  private parseTargetHealsPlayer(
    match: RegExpExecArray,
    timestamp: Date,
    rawTimestamp: string,
    rawLine: string,
    lineNumber: number
  ): HealingEvent {
    const [, sourceName, amountStr] = match;
    const amount = parseInt(amountStr!, 10);
    return createHealingEvent({
      ...this.createBaseHealingEvent(timestamp, rawTimestamp, rawLine, lineNumber),
      eventType: EventType.HEALING_RECEIVED,
      source: createEntity(sourceName!),
      target: createSelfEntity(),
      amount,
      spellName: 'Unknown',
    });
  }

  // "Your group heal heals the group for 400 hit points."
  private parsePlayerGroupHeal(
    match: RegExpExecArray,
    timestamp: Date,
    rawTimestamp: string,
    rawLine: string,
    lineNumber: number
  ): HealingEvent {
    const [, spellName, amountStr] = match;
    const amount = parseInt(amountStr!, 10);
    return createHealingEvent({
      ...this.createBaseHealingEvent(timestamp, rawTimestamp, rawLine, lineNumber),
      eventType: EventType.HEALING_DONE,
      source: createSelfEntity(),
      target: createEntity('Group'),
      amount,
      spellName: spellName!,
    });
  }

  // "You are healed for 150 hit points."
  private parsePlayerHealedPassive(
    match: RegExpExecArray,
    timestamp: Date,
    rawTimestamp: string,
    rawLine: string,
    lineNumber: number
  ): HealingEvent {
    const [, amountStr] = match;
    const amount = parseInt(amountStr!, 10);
    return createHealingEvent({
      ...this.createBaseHealingEvent(timestamp, rawTimestamp, rawLine, lineNumber),
      eventType: EventType.HEALING_RECEIVED,
      source: createEntity('Unknown'),
      target: createSelfEntity(),
      amount,
      spellName: 'Unknown',
    });
  }
}
