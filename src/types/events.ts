import { EntityType, EventType, Realm } from './enums.js';

/**
 * Represents any actor in combat (player, NPC, pet, etc.)
 */
export interface Entity {
  /** Display name of the entity */
  name: string;
  /** Type of entity */
  entityType: EntityType;
  /** Realm of the entity (if known) */
  realm?: Realm;
  /** Convenience flag - true if this is a player */
  isPlayer: boolean;
  /** Convenience flag - true if this is the log owner ("you") */
  isSelf: boolean;
}

/**
 * Base interface for all combat events
 */
export interface BaseEvent {
  /** Unique event ID */
  id: string;
  /** Parsed timestamp */
  timestamp: Date;
  /** Original timestamp string "[HH:MM:SS]" */
  rawTimestamp: string;
  /** Original log line for reference */
  rawLine: string;
  /** Line number in source file */
  lineNumber: number;
  /** Discriminator for event type */
  eventType: EventType;
}

/**
 * Event representing an unknown/unparseable log line
 */
export interface UnknownEvent extends BaseEvent {
  eventType: EventType.UNKNOWN;
}

/**
 * Event representing a death
 */
export interface DeathEvent extends BaseEvent {
  eventType: EventType.DEATH;
  /** Who died */
  target: Entity;
  /** Who killed them (if known) */
  killer?: Entity;
}

/**
 * Helper function to create the "self" entity
 */
export function createSelfEntity(): Entity {
  return {
    name: 'You',
    entityType: EntityType.SELF,
    isPlayer: true,
    isSelf: true,
  };
}

/**
 * Helper function to create an entity from a name
 */
export function createEntity(name: string, isSelf = false): Entity {
  if (isSelf || name.toLowerCase() === 'you' || name.toLowerCase() === 'yourself') {
    return createSelfEntity();
  }

  // Determine entity type based on name patterns
  // Names starting with "The " or lowercase are typically NPCs
  const isNPC = name.startsWith('the ') || name.charAt(0) === name.charAt(0).toLowerCase();

  return {
    name: name.replace(/^[Tt]he /, ''),
    entityType: isNPC ? EntityType.NPC : EntityType.PLAYER,
    isPlayer: !isNPC,
    isSelf: false,
  };
}
