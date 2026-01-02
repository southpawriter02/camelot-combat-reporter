import type {
  CombatEvent,
  DamageEvent,
  HealingEvent,
  DeathEvent,
  CrowdControlEvent,
} from '../../types/index.js';
import { EventType } from '../../types/index.js';
import type {
  TimelineEntry,
  TimelineMarkerCategory,
  TimelineEntryDetails,
} from './types.js';
import {
  formatTimestamp,
  formatRelativeTime,
  getRelativeTime,
} from '../utils/timeUtils.js';

/**
 * Converts combat events into rich timeline entries
 */
export class TimelineEventFormatter {
  /**
   * Convert a combat event to a timeline entry
   * @param event The combat event to format
   * @param sessionStartTime Start time of the session for relative timing
   * @returns A rich timeline entry representation
   */
  formatEvent(event: CombatEvent, sessionStartTime: Date): TimelineEntry {
    const relativeTimeMs = getRelativeTime(event.timestamp, sessionStartTime);

    const baseEntry = {
      id: event.id,
      event,
      timestamp: event.timestamp,
      formattedTimestamp: formatTimestamp(event.timestamp),
      relativeTimeMs,
      formattedRelativeTime: formatRelativeTime(relativeTimeMs),
      eventType: event.eventType,
    };

    switch (event.eventType) {
      case EventType.DAMAGE_DEALT:
      case EventType.DAMAGE_RECEIVED:
        return this.formatDamageEvent(baseEntry, event as DamageEvent);
      case EventType.HEALING_DONE:
      case EventType.HEALING_RECEIVED:
        return this.formatHealingEvent(baseEntry, event as HealingEvent);
      case EventType.CROWD_CONTROL:
        return this.formatCCEvent(baseEntry, event as CrowdControlEvent);
      case EventType.DEATH:
        return this.formatDeathEvent(baseEntry, event as DeathEvent);
      default:
        return this.formatUnknownEvent(baseEntry, event);
    }
  }

  /**
   * Format a damage event
   */
  private formatDamageEvent(
    baseEntry: Partial<TimelineEntry>,
    event: DamageEvent
  ): TimelineEntry {
    const isOutgoing = event.eventType === EventType.DAMAGE_DEALT;
    const markerCategory: TimelineMarkerCategory = isOutgoing
      ? 'DAMAGE_OUTGOING'
      : 'DAMAGE_INCOMING';

    const actionDesc = event.actionName || 'attack';
    const critText = event.isCritical ? ' (CRITICAL)' : '';
    const absorbText =
      event.absorbedAmount > 0 ? ` (${event.absorbedAmount} absorbed)` : '';

    const description = `${event.source.name} hit ${event.target.name} for ${event.effectiveAmount} with ${actionDesc}${critText}${absorbText}`;

    const details: TimelineEntryDetails = {
      actionName: event.actionName,
      damageType: event.damageType,
      isCritical: event.isCritical,
    };

    if (event.absorbedAmount > 0) {
      details.absorbedAmount = event.absorbedAmount;
    }

    return {
      ...baseEntry,
      markerCategory,
      description,
      source: event.source,
      target: event.target,
      primaryValue: event.effectiveAmount,
      primaryValueUnit: 'damage',
      details,
    } as TimelineEntry;
  }

  /**
   * Format a healing event
   */
  private formatHealingEvent(
    baseEntry: Partial<TimelineEntry>,
    event: HealingEvent
  ): TimelineEntry {
    const isOutgoing = event.eventType === EventType.HEALING_DONE;
    const markerCategory: TimelineMarkerCategory = isOutgoing
      ? 'HEALING_OUTGOING'
      : 'HEALING_INCOMING';

    const overhealText =
      event.overheal > 0 ? ` (${event.overheal} overheal)` : '';
    const critText = event.isCritical ? ' (CRITICAL)' : '';

    const description = `${event.source.name} healed ${event.target.name} for ${event.effectiveAmount} with ${event.spellName}${critText}${overhealText}`;

    const details: TimelineEntryDetails = {
      spellName: event.spellName,
      isCritical: event.isCritical,
    };

    if (event.overheal > 0) {
      details.overheal = event.overheal;
    }

    return {
      ...baseEntry,
      markerCategory,
      description,
      source: event.source,
      target: event.target,
      primaryValue: event.effectiveAmount,
      primaryValueUnit: 'healing',
      details,
    } as TimelineEntry;
  }

  /**
   * Format a crowd control event
   */
  private formatCCEvent(
    baseEntry: Partial<TimelineEntry>,
    event: CrowdControlEvent
  ): TimelineEntry {
    const sourceName = event.source?.name ?? 'Unknown';
    const resistText = event.isResisted ? ' (RESISTED)' : '';

    // Convert effect to verb form for description
    const effectVerb = this.getCCVerb(event.effect);
    const description = `${sourceName} ${effectVerb} ${event.target.name} for ${event.duration}s${resistText}`;

    const details: TimelineEntryDetails = {
      ccEffect: event.effect,
      wasResisted: event.isResisted,
    };

    return {
      ...baseEntry,
      markerCategory: 'CROWD_CONTROL',
      description,
      source: event.source,
      target: event.target,
      primaryValue: event.duration,
      primaryValueUnit: 'seconds',
      details,
    } as TimelineEntry;
  }

  /**
   * Format a death event
   */
  private formatDeathEvent(
    baseEntry: Partial<TimelineEntry>,
    event: DeathEvent
  ): TimelineEntry {
    const killerText = event.killer ? ` by ${event.killer.name}` : '';
    const description = `${event.target.name} was killed${killerText}`;

    const details: TimelineEntryDetails = {};
    if (event.killer) {
      details.killer = event.killer;
    }

    return {
      ...baseEntry,
      markerCategory: 'DEATH',
      description,
      source: event.killer,
      target: event.target,
      primaryValue: undefined,
      primaryValueUnit: undefined,
      details,
    } as TimelineEntry;
  }

  /**
   * Format an unknown event type
   */
  private formatUnknownEvent(
    baseEntry: Partial<TimelineEntry>,
    event: CombatEvent
  ): TimelineEntry {
    return {
      ...baseEntry,
      markerCategory: 'DAMAGE_INCOMING', // Default fallback
      description: `Unknown event at ${baseEntry.formattedTimestamp}`,
      source: undefined,
      target: undefined,
      primaryValue: undefined,
      primaryValueUnit: undefined,
      details: {},
    } as TimelineEntry;
  }

  /**
   * Convert CC effect to verb form for descriptions
   */
  private getCCVerb(effect: string): string {
    const verbMap: Record<string, string> = {
      STUN: 'stunned',
      MESMERIZE: 'mesmerized',
      ROOT: 'rooted',
      SNARE: 'snared',
      SILENCE: 'silenced',
      DISEASE: 'diseased',
      POISON: 'poisoned',
    };

    return verbMap[effect] ?? `applied ${effect.toLowerCase()} to`;
  }
}
