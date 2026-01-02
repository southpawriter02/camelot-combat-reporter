import type {
  CombatEvent,
  DamageEvent,
  HealingEvent,
  DeathEvent,
  CrowdControlEvent,
} from '../../types/index.js';
import { EventType } from '../../types/index.js';
import type { KeyEvent, KeyEventReason } from '../types/index.js';
import { getRelativeTime } from '../utils/timeUtils.js';

/**
 * Configuration for key event detection
 */
export interface KeyEventConfig {
  /** Minimum damage for a "big hit" (absolute value) */
  bigHitThreshold: number;
  /** Minimum heal for a "big heal" (absolute value) */
  bigHealThreshold: number;
  /** Include critical hits as key events */
  includeCriticalHits: boolean;
  /** Include crowd control as key events */
  includeCrowdControl: boolean;
  /** Include deaths as key events */
  includeDeaths: boolean;
}

/**
 * Default configuration for key event detection
 */
export const DEFAULT_KEY_EVENT_CONFIG: KeyEventConfig = {
  bigHitThreshold: 500,
  bigHealThreshold: 500,
  includeCriticalHits: true,
  includeCrowdControl: true,
  includeDeaths: true,
};

/**
 * Detects notable/key events from combat event streams
 */
export class KeyEventDetector {
  private config: KeyEventConfig;

  constructor(config: Partial<KeyEventConfig> = {}) {
    this.config = { ...DEFAULT_KEY_EVENT_CONFIG, ...config };
  }

  /**
   * Detect all key events from a list of combat events
   */
  detect(events: CombatEvent[], sessionStartTime: Date): KeyEvent[] {
    const keyEvents: KeyEvent[] = [];

    for (const event of events) {
      const detected = this.detectSingle(event, sessionStartTime);
      if (detected) {
        keyEvents.push(detected);
      }
    }

    // Sort by timestamp
    return keyEvents.sort(
      (a, b) => a.event.timestamp.getTime() - b.event.timestamp.getTime()
    );
  }

  /**
   * Check if a single event qualifies as a key event
   */
  detectSingle(event: CombatEvent, sessionStartTime: Date): KeyEvent | null {
    const relativeTimeMs = getRelativeTime(event.timestamp, sessionStartTime);

    // Check death events
    if (event.eventType === EventType.DEATH && this.config.includeDeaths) {
      return this.createDeathKeyEvent(event as DeathEvent, relativeTimeMs);
    }

    // Check damage events
    if (
      event.eventType === EventType.DAMAGE_DEALT ||
      event.eventType === EventType.DAMAGE_RECEIVED
    ) {
      const damageEvent = event as DamageEvent;

      // Big hit
      if (damageEvent.effectiveAmount >= this.config.bigHitThreshold) {
        return this.createBigHitKeyEvent(damageEvent, relativeTimeMs);
      }

      // Critical hit
      if (damageEvent.isCritical && this.config.includeCriticalHits) {
        return this.createCriticalHitKeyEvent(damageEvent, relativeTimeMs);
      }
    }

    // Check healing events
    if (
      (event.eventType === EventType.HEALING_DONE ||
        event.eventType === EventType.HEALING_RECEIVED) &&
      (event as HealingEvent).effectiveAmount >= this.config.bigHealThreshold
    ) {
      return this.createBigHealKeyEvent(event as HealingEvent, relativeTimeMs);
    }

    // Check crowd control events
    if (
      event.eventType === EventType.CROWD_CONTROL &&
      this.config.includeCrowdControl
    ) {
      return this.createCCKeyEvent(event as CrowdControlEvent, relativeTimeMs);
    }

    return null;
  }

  /**
   * Create a key event for a death
   */
  private createDeathKeyEvent(event: DeathEvent, relativeTimeMs: number): KeyEvent {
    const description = event.killer
      ? `${event.target.name} was killed by ${event.killer.name}`
      : `${event.target.name} died`;

    return {
      event,
      reason: 'DEATH' as KeyEventReason,
      description,
      relativeTimeMs,
    };
  }

  /**
   * Create a key event for a big hit
   */
  private createBigHitKeyEvent(event: DamageEvent, relativeTimeMs: number): KeyEvent {
    const actionName = event.actionName || 'attack';
    const description = `${event.source.name} hit ${event.target.name} for ${event.effectiveAmount} with ${actionName}`;

    return {
      event,
      reason: 'BIG_HIT' as KeyEventReason,
      description,
      relativeTimeMs,
    };
  }

  /**
   * Create a key event for a critical hit
   */
  private createCriticalHitKeyEvent(
    event: DamageEvent,
    relativeTimeMs: number
  ): KeyEvent {
    const actionName = event.actionName || 'attack';
    const description = `${event.source.name} critically hit ${event.target.name} for ${event.effectiveAmount} with ${actionName}`;

    return {
      event,
      reason: 'CRITICAL_HIT' as KeyEventReason,
      description,
      relativeTimeMs,
    };
  }

  /**
   * Create a key event for a big heal
   */
  private createBigHealKeyEvent(event: HealingEvent, relativeTimeMs: number): KeyEvent {
    const description = `${event.source.name} healed ${event.target.name} for ${event.effectiveAmount} with ${event.spellName}`;

    return {
      event,
      reason: 'BIG_HEAL' as KeyEventReason,
      description,
      relativeTimeMs,
    };
  }

  /**
   * Create a key event for crowd control
   */
  private createCCKeyEvent(
    event: CrowdControlEvent,
    relativeTimeMs: number
  ): KeyEvent {
    const sourceName = event.source?.name ?? 'Unknown';
    const effectDescription = event.isResisted
      ? `${event.target.name} resisted ${event.effect} from ${sourceName}`
      : `${sourceName} ${event.effect.toLowerCase()}ed ${event.target.name} for ${event.duration}s`;

    return {
      event,
      reason: 'CROWD_CONTROL' as KeyEventReason,
      description: effectDescription,
      relativeTimeMs,
    };
  }

  /**
   * Update configuration
   */
  setConfig(config: Partial<KeyEventConfig>): void {
    this.config = { ...this.config, ...config };
  }

  /**
   * Get current configuration
   */
  getConfig(): KeyEventConfig {
    return { ...this.config };
  }
}
