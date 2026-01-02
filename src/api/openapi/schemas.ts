/**
 * OpenAPI JSON Schema definitions
 */
import type { OpenApiSchema } from '../types.js';

/**
 * Common schema definitions for reuse
 */
export const schemas: Record<string, OpenApiSchema> = {
  // Core types
  Entity: {
    type: 'object',
    properties: {
      id: { type: 'string', description: 'Unique entity identifier' },
      name: { type: 'string', description: 'Entity name' },
      entityType: {
        type: 'string',
        enum: ['SELF', 'PLAYER', 'NPC', 'PET', 'UNKNOWN'],
        description: 'Type of entity',
      },
      realm: {
        type: 'string',
        enum: ['ALBION', 'MIDGARD', 'HIBERNIA', 'NEUTRAL'],
        description: 'Entity realm',
      },
      isPlayer: { type: 'boolean', description: 'Whether this is a player' },
      isSelf: { type: 'boolean', description: 'Whether this is the self player' },
    },
    required: ['name', 'entityType'],
  },

  CombatEvent: {
    type: 'object',
    properties: {
      id: { type: 'string', description: 'Unique event identifier' },
      eventType: {
        type: 'string',
        enum: [
          'DAMAGE_DEALT',
          'DAMAGE_RECEIVED',
          'HEALING_DONE',
          'HEALING_RECEIVED',
          'DEATH',
          'CROWD_CONTROL',
          'UNKNOWN',
        ],
        description: 'Type of combat event',
      },
      timestamp: { type: 'string', format: 'date-time', description: 'Event timestamp' },
      rawTimestamp: { type: 'string', description: 'Original timestamp string from log' },
      rawLine: { type: 'string', description: 'Original log line' },
      lineNumber: { type: 'number', description: 'Line number in log file' },
      source: { $ref: '#/components/schemas/Entity' },
      target: { $ref: '#/components/schemas/Entity' },
    },
    required: ['eventType', 'timestamp'],
  },

  DamageEvent: {
    allOf: [
      { $ref: '#/components/schemas/CombatEvent' },
      {
        type: 'object',
        properties: {
          amount: { type: 'number', description: 'Total damage amount' },
          absorbedAmount: { type: 'number', description: 'Damage absorbed' },
          effectiveAmount: { type: 'number', description: 'Effective damage dealt' },
          damageType: {
            type: 'string',
            enum: ['CRUSH', 'SLASH', 'THRUST', 'HEAT', 'COLD', 'MATTER', 'BODY', 'SPIRIT', 'ENERGY'],
          },
          actionType: {
            type: 'string',
            enum: ['MELEE', 'SPELL', 'STYLE', 'PROC', 'DOT', 'UNKNOWN'],
          },
          actionName: { type: 'string', description: 'Name of spell/style/ability' },
          isCritical: { type: 'boolean', description: 'Whether this was a critical hit' },
          isBlocked: { type: 'boolean', description: 'Whether the attack was blocked' },
          isParried: { type: 'boolean', description: 'Whether the attack was parried' },
          isEvaded: { type: 'boolean', description: 'Whether the attack was evaded' },
        },
        required: ['amount', 'damageType', 'actionType'],
      },
    ],
  },

  HealingEvent: {
    allOf: [
      { $ref: '#/components/schemas/CombatEvent' },
      {
        type: 'object',
        properties: {
          amount: { type: 'number', description: 'Total healing amount' },
          effectiveAmount: { type: 'number', description: 'Effective healing (minus overheal)' },
          overhealAmount: { type: 'number', description: 'Overhealing amount' },
          actionType: {
            type: 'string',
            enum: ['SPELL', 'PROC', 'HOT', 'POTION', 'UNKNOWN'],
          },
          actionName: { type: 'string', description: 'Name of healing spell' },
          isCritical: { type: 'boolean', description: 'Whether this was a critical heal' },
        },
        required: ['amount', 'actionType'],
      },
    ],
  },

  CombatSession: {
    type: 'object',
    properties: {
      id: { type: 'string', description: 'Unique session identifier' },
      startTime: { type: 'string', format: 'date-time', description: 'Session start time' },
      endTime: { type: 'string', format: 'date-time', description: 'Session end time' },
      durationMs: { type: 'number', description: 'Session duration in milliseconds' },
      summary: { $ref: '#/components/schemas/SessionSummary' },
    },
    required: ['id', 'startTime', 'endTime', 'durationMs'],
  },

  SessionSummary: {
    type: 'object',
    properties: {
      totalDamageDealt: { type: 'number' },
      totalDamageTaken: { type: 'number' },
      totalHealingDone: { type: 'number' },
      totalHealingReceived: { type: 'number' },
      deathCount: { type: 'number' },
      ccEventCount: { type: 'number' },
    },
  },

  SessionParticipant: {
    type: 'object',
    properties: {
      entity: { $ref: '#/components/schemas/Entity' },
      role: {
        type: 'string',
        enum: ['DAMAGE_DEALER', 'HEALER', 'TANK', 'HYBRID', 'UNKNOWN'],
      },
      firstSeen: { type: 'string', format: 'date-time' },
      lastSeen: { type: 'string', format: 'date-time' },
      eventCount: { type: 'number' },
    },
    required: ['entity', 'role'],
  },

  PlayerSessionStats: {
    type: 'object',
    properties: {
      playerName: { type: 'string' },
      sessionId: { type: 'string' },
      sessionStart: { type: 'string', format: 'date-time' },
      sessionEnd: { type: 'string', format: 'date-time' },
      durationMs: { type: 'number' },
      role: { type: 'string' },
      kills: { type: 'number' },
      deaths: { type: 'number' },
      assists: { type: 'number' },
      kdr: { type: 'number', description: 'Kill/Death ratio' },
      damageDealt: { type: 'number' },
      damageTaken: { type: 'number' },
      dps: { type: 'number', description: 'Damage per second' },
      peakDps: { type: 'number', description: 'Peak DPS' },
      healingDone: { type: 'number' },
      healingReceived: { type: 'number' },
      hps: { type: 'number', description: 'Healing per second' },
      overhealRate: { type: 'number' },
      critRate: { type: 'number' },
      performanceScore: { type: 'number', description: 'Performance score (0-100)' },
      performanceRating: {
        type: 'string',
        enum: ['EXCELLENT', 'GOOD', 'AVERAGE', 'BELOW_AVERAGE', 'POOR'],
      },
    },
  },

  PlayerAggregateStats: {
    type: 'object',
    properties: {
      playerName: { type: 'string' },
      totalSessions: { type: 'number' },
      totalCombatTimeMs: { type: 'number' },
      totalKills: { type: 'number' },
      totalDeaths: { type: 'number' },
      totalAssists: { type: 'number' },
      overallKDR: { type: 'number' },
      totalDamageDealt: { type: 'number' },
      totalDamageTaken: { type: 'number' },
      totalHealingDone: { type: 'number' },
      totalHealingReceived: { type: 'number' },
      avgDPS: { type: 'number' },
      avgHPS: { type: 'number' },
      avgPerformanceScore: { type: 'number' },
      performanceVariance: { type: 'number' },
      consistencyRating: {
        type: 'string',
        enum: ['VERY_CONSISTENT', 'CONSISTENT', 'VARIABLE', 'INCONSISTENT'],
      },
    },
  },

  // Response wrappers
  ApiSuccessResponse: {
    type: 'object',
    properties: {
      data: { description: 'Response data' },
      meta: {
        type: 'object',
        properties: {
          total: { type: 'number', description: 'Total count' },
          limit: { type: 'number', description: 'Pagination limit' },
          offset: { type: 'number', description: 'Pagination offset' },
          hasMore: { type: 'boolean', description: 'Whether more results exist' },
        },
      },
    },
    required: ['data'],
  },

  ApiErrorResponse: {
    type: 'object',
    properties: {
      error: {
        type: 'object',
        properties: {
          code: { type: 'string', description: 'Error code' },
          message: { type: 'string', description: 'Error message' },
          details: { description: 'Additional error details' },
        },
        required: ['code', 'message'],
      },
    },
    required: ['error'],
  },

  HealthStatus: {
    type: 'object',
    properties: {
      status: { type: 'string', enum: ['healthy', 'unhealthy'] },
      timestamp: { type: 'string', format: 'date-time' },
      uptime: { type: 'number', description: 'Uptime in milliseconds' },
      database: {
        type: 'object',
        properties: {
          connected: { type: 'boolean' },
          backend: { type: 'string', enum: ['sqlite', 'postgresql'] },
        },
      },
    },
  },
};

/**
 * Get all schema definitions for OpenAPI spec
 */
export function getSchemas(): Record<string, OpenApiSchema> {
  return schemas;
}
