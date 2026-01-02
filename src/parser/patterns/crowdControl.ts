import { v4 as uuidv4 } from 'uuid';
import type { PatternHandler } from './index.js';
import {
  type CombatEvent,
  type CrowdControlEvent,
  EventType,
  CrowdControlEffect,
  createEntity,
  createSelfEntity,
  createCrowdControlEvent,
} from '../../types/index.js';

// Crowd Control patterns

// "The troll is stunned for 9 seconds!"
const TARGET_STUNNED = /^(?:The )?(.+?) is stunned for (\d+) seconds?!$/i;

// "You are stunned for 5 seconds!"
const PLAYER_STUNNED = /^You are stunned for (\d+) seconds?!$/i;

// "The troll is mesmerized!"
const TARGET_MEZZED = /^(?:The )?(.+?) is mesmerized!$/i;

// "You are mesmerized!"
const PLAYER_MEZZED = /^You are mesmerized!$/i;

// "The troll is rooted for 15 seconds!"
const TARGET_ROOTED = /^(?:The )?(.+?) is rooted for (\d+) seconds?!$/i;

// "You are rooted for 10 seconds!"
const PLAYER_ROOTED = /^You are rooted for (\d+) seconds?!$/i;

// "The troll is snared!"
const TARGET_SNARED = /^(?:The )?(.+?) is snared!$/i;

// "You are snared!"
const PLAYER_SNARED = /^You are snared!$/i;

// "The troll resists the effect!"
const TARGET_RESISTS = /^(?:The )?(.+?) resists the effect!$/i;

// "You resist the effect!"
const PLAYER_RESISTS = /^You resist the effect!$/i;

/**
 * Pattern handler for crowd control events
 */
export class CrowdControlPatternHandler implements PatternHandler {
  name = 'CrowdControlPatternHandler';
  priority = 30;

  private patterns = [
    { regex: TARGET_STUNNED, parser: this.parseTargetStunned.bind(this) },
    { regex: PLAYER_STUNNED, parser: this.parsePlayerStunned.bind(this) },
    { regex: TARGET_MEZZED, parser: this.parseTargetMezzed.bind(this) },
    { regex: PLAYER_MEZZED, parser: this.parsePlayerMezzed.bind(this) },
    { regex: TARGET_ROOTED, parser: this.parseTargetRooted.bind(this) },
    { regex: PLAYER_ROOTED, parser: this.parsePlayerRooted.bind(this) },
    { regex: TARGET_SNARED, parser: this.parseTargetSnared.bind(this) },
    { regex: PLAYER_SNARED, parser: this.parsePlayerSnared.bind(this) },
    { regex: TARGET_RESISTS, parser: this.parseTargetResists.bind(this) },
    { regex: PLAYER_RESISTS, parser: this.parsePlayerResists.bind(this) },
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

  private createBaseCCEvent(
    timestamp: Date,
    rawTimestamp: string,
    rawLine: string,
    lineNumber: number
  ): Omit<CrowdControlEvent, 'eventType' | 'target' | 'effect' | 'duration'> {
    return {
      id: uuidv4(),
      timestamp,
      rawTimestamp,
      rawLine,
      lineNumber,
      isResisted: false,
    };
  }

  // "The troll is stunned for 9 seconds!"
  private parseTargetStunned(
    match: RegExpExecArray,
    timestamp: Date,
    rawTimestamp: string,
    rawLine: string,
    lineNumber: number
  ): CrowdControlEvent {
    const [, targetName, durationStr] = match;
    return createCrowdControlEvent({
      ...this.createBaseCCEvent(timestamp, rawTimestamp, rawLine, lineNumber),
      eventType: EventType.CROWD_CONTROL,
      target: createEntity(targetName!),
      effect: CrowdControlEffect.STUN,
      duration: parseInt(durationStr!, 10),
    });
  }

  // "You are stunned for 5 seconds!"
  private parsePlayerStunned(
    match: RegExpExecArray,
    timestamp: Date,
    rawTimestamp: string,
    rawLine: string,
    lineNumber: number
  ): CrowdControlEvent {
    const [, durationStr] = match;
    return createCrowdControlEvent({
      ...this.createBaseCCEvent(timestamp, rawTimestamp, rawLine, lineNumber),
      eventType: EventType.CROWD_CONTROL,
      target: createSelfEntity(),
      effect: CrowdControlEffect.STUN,
      duration: parseInt(durationStr!, 10),
    });
  }

  // "The troll is mesmerized!"
  private parseTargetMezzed(
    match: RegExpExecArray,
    timestamp: Date,
    rawTimestamp: string,
    rawLine: string,
    lineNumber: number
  ): CrowdControlEvent {
    const [, targetName] = match;
    return createCrowdControlEvent({
      ...this.createBaseCCEvent(timestamp, rawTimestamp, rawLine, lineNumber),
      eventType: EventType.CROWD_CONTROL,
      target: createEntity(targetName!),
      effect: CrowdControlEffect.MESMERIZE,
      duration: 0, // Duration unknown for mez
    });
  }

  // "You are mesmerized!"
  private parsePlayerMezzed(
    _match: RegExpExecArray,
    timestamp: Date,
    rawTimestamp: string,
    rawLine: string,
    lineNumber: number
  ): CrowdControlEvent {
    return createCrowdControlEvent({
      ...this.createBaseCCEvent(timestamp, rawTimestamp, rawLine, lineNumber),
      eventType: EventType.CROWD_CONTROL,
      target: createSelfEntity(),
      effect: CrowdControlEffect.MESMERIZE,
      duration: 0,
    });
  }

  // "The troll is rooted for 15 seconds!"
  private parseTargetRooted(
    match: RegExpExecArray,
    timestamp: Date,
    rawTimestamp: string,
    rawLine: string,
    lineNumber: number
  ): CrowdControlEvent {
    const [, targetName, durationStr] = match;
    return createCrowdControlEvent({
      ...this.createBaseCCEvent(timestamp, rawTimestamp, rawLine, lineNumber),
      eventType: EventType.CROWD_CONTROL,
      target: createEntity(targetName!),
      effect: CrowdControlEffect.ROOT,
      duration: parseInt(durationStr!, 10),
    });
  }

  // "You are rooted for 10 seconds!"
  private parsePlayerRooted(
    match: RegExpExecArray,
    timestamp: Date,
    rawTimestamp: string,
    rawLine: string,
    lineNumber: number
  ): CrowdControlEvent {
    const [, durationStr] = match;
    return createCrowdControlEvent({
      ...this.createBaseCCEvent(timestamp, rawTimestamp, rawLine, lineNumber),
      eventType: EventType.CROWD_CONTROL,
      target: createSelfEntity(),
      effect: CrowdControlEffect.ROOT,
      duration: parseInt(durationStr!, 10),
    });
  }

  // "The troll is snared!"
  private parseTargetSnared(
    match: RegExpExecArray,
    timestamp: Date,
    rawTimestamp: string,
    rawLine: string,
    lineNumber: number
  ): CrowdControlEvent {
    const [, targetName] = match;
    return createCrowdControlEvent({
      ...this.createBaseCCEvent(timestamp, rawTimestamp, rawLine, lineNumber),
      eventType: EventType.CROWD_CONTROL,
      target: createEntity(targetName!),
      effect: CrowdControlEffect.SNARE,
      duration: 0,
    });
  }

  // "You are snared!"
  private parsePlayerSnared(
    _match: RegExpExecArray,
    timestamp: Date,
    rawTimestamp: string,
    rawLine: string,
    lineNumber: number
  ): CrowdControlEvent {
    return createCrowdControlEvent({
      ...this.createBaseCCEvent(timestamp, rawTimestamp, rawLine, lineNumber),
      eventType: EventType.CROWD_CONTROL,
      target: createSelfEntity(),
      effect: CrowdControlEffect.SNARE,
      duration: 0,
    });
  }

  // "The troll resists the effect!"
  private parseTargetResists(
    match: RegExpExecArray,
    timestamp: Date,
    rawTimestamp: string,
    rawLine: string,
    lineNumber: number
  ): CrowdControlEvent {
    const [, targetName] = match;
    return createCrowdControlEvent({
      ...this.createBaseCCEvent(timestamp, rawTimestamp, rawLine, lineNumber),
      eventType: EventType.CROWD_CONTROL,
      target: createEntity(targetName!),
      effect: CrowdControlEffect.UNKNOWN,
      duration: 0,
      isResisted: true,
    });
  }

  // "You resist the effect!"
  private parsePlayerResists(
    _match: RegExpExecArray,
    timestamp: Date,
    rawTimestamp: string,
    rawLine: string,
    lineNumber: number
  ): CrowdControlEvent {
    return createCrowdControlEvent({
      ...this.createBaseCCEvent(timestamp, rawTimestamp, rawLine, lineNumber),
      eventType: EventType.CROWD_CONTROL,
      target: createSelfEntity(),
      effect: CrowdControlEffect.UNKNOWN,
      duration: 0,
      isResisted: true,
    });
  }
}
