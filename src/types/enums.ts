/**
 * Type of combat event
 */
export enum EventType {
  DAMAGE_DEALT = 'DAMAGE_DEALT',
  DAMAGE_RECEIVED = 'DAMAGE_RECEIVED',
  HEALING_DONE = 'HEALING_DONE',
  HEALING_RECEIVED = 'HEALING_RECEIVED',
  CROWD_CONTROL = 'CROWD_CONTROL',
  DEATH = 'DEATH',
  UNKNOWN = 'UNKNOWN',
}

/**
 * Type of damage (physical or magical)
 */
export enum DamageType {
  // Physical
  CRUSH = 'CRUSH',
  SLASH = 'SLASH',
  THRUST = 'THRUST',
  // Magical
  HEAT = 'HEAT',
  COLD = 'COLD',
  MATTER = 'MATTER',
  BODY = 'BODY',
  SPIRIT = 'SPIRIT',
  ENERGY = 'ENERGY',
  // Default/unknown
  UNKNOWN = 'UNKNOWN',
}

/**
 * Type of action that caused damage/healing
 */
export enum ActionType {
  MELEE = 'MELEE',
  SPELL = 'SPELL',
  STYLE = 'STYLE',
  PROC = 'PROC',
  DOT = 'DOT',
  UNKNOWN = 'UNKNOWN',
}

/**
 * Type of crowd control effect
 */
export enum CrowdControlEffect {
  STUN = 'STUN',
  MESMERIZE = 'MESMERIZE',
  ROOT = 'ROOT',
  SNARE = 'SNARE',
  SILENCE = 'SILENCE',
  DISEASE = 'DISEASE',
  POISON = 'POISON',
  UNKNOWN = 'UNKNOWN',
}

/**
 * Type of entity in combat
 */
export enum EntityType {
  SELF = 'SELF',
  PLAYER = 'PLAYER',
  NPC = 'NPC',
  PET = 'PET',
  UNKNOWN = 'UNKNOWN',
}

/**
 * DAoC Realm
 */
export enum Realm {
  ALBION = 'ALBION',
  MIDGARD = 'MIDGARD',
  HIBERNIA = 'HIBERNIA',
  NEUTRAL = 'NEUTRAL',
}

/**
 * Type of log file
 */
export enum LogType {
  CHAT = 'CHAT',
  COMBAT = 'COMBAT',
  UNKNOWN = 'UNKNOWN',
}
