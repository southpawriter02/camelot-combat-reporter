/**
 * Machine Learning API routes
 *
 * Endpoints for ML predictions (fight outcome, playstyle, performance, threats).
 * All inference runs locally - no cloud APIs required.
 */
import type { RouteDefinition, Middleware } from '../types.js';
import type { DatabaseAdapter } from '../../database/index.js';
import type { Entity } from '../../types/index.js';
import type { CombatSession } from '../../analysis/types/index.js';
import { MLPredictor } from '../../ml/index.js';
import { BadRequestError, NotFoundError } from '../errors.js';

/**
 * Create ML routes
 *
 * @param database - Database adapter for retrieving sessions
 * @param authMiddleware - Authentication middleware
 * @param mlPredictor - Optional pre-configured ML predictor
 */
export function createMLRoutes(
  database: DatabaseAdapter,
  authMiddleware: Middleware,
  mlPredictor?: MLPredictor
): RouteDefinition[] {
  // Use provided predictor or create a new one
  const predictor = mlPredictor || new MLPredictor();

  return [
    // GET /ml/models - List available models
    {
      method: 'GET',
      path: '/ml/models',
      middleware: [authMiddleware],
      handler: async (_req, res) => {
        const models = predictor.getAvailableModels();
        const config = predictor.getConfig();

        res.json({
          data: {
            enabled: config.enabled,
            loaded: predictor.isLoaded,
            models,
          },
        });
      },
      openapi: {
        summary: 'List ML models',
        description: 'Get information about available ML models',
        tags: ['ML'],
        responses: {
          '200': { description: 'Available models' },
        },
      },
    },

    // POST /ml/load - Preload all models
    {
      method: 'POST',
      path: '/ml/load',
      middleware: [authMiddleware],
      handler: async (_req, res) => {
        await predictor.loadModels();

        res.json({
          data: {
            loaded: true,
            models: predictor.getAvailableModels(),
          },
        });
      },
      openapi: {
        summary: 'Preload models',
        description: 'Preload all ML models into memory',
        tags: ['ML'],
        responses: {
          '200': { description: 'Models loaded' },
        },
      },
    },

    // POST /ml/unload - Unload all models
    {
      method: 'POST',
      path: '/ml/unload',
      middleware: [authMiddleware],
      handler: async (_req, res) => {
        predictor.unloadModels();

        res.json({
          data: {
            loaded: false,
          },
        });
      },
      openapi: {
        summary: 'Unload models',
        description: 'Unload ML models from memory',
        tags: ['ML'],
        responses: {
          '200': { description: 'Models unloaded' },
        },
      },
    },

    // POST /ml/predict/outcome - Fight outcome prediction
    {
      method: 'POST',
      path: '/ml/predict/outcome',
      middleware: [authMiddleware],
      handler: async (req, res) => {
        const { sessionId, playerName } = req.body as {
          sessionId?: string;
          playerName?: string;
        };

        if (!sessionId) {
          throw new BadRequestError('sessionId is required');
        }
        if (!playerName) {
          throw new BadRequestError('playerName is required');
        }

        // Retrieve session from database
        const session = await getSessionFromDatabase(database, sessionId);

        const prediction = await predictor.predictFightOutcome(session, playerName);

        res.json({
          data: {
            sessionId,
            playerName,
            prediction: {
              winProbability: prediction.winProbability,
              lossProbability: prediction.lossProbability,
              confidence: prediction.confidence,
              factors: prediction.factors,
              isHeuristic: prediction.isHeuristic,
              eventsAnalyzed: prediction.eventsAnalyzed,
              timestamp: prediction.timestamp.toISOString(),
            },
          },
        });
      },
      openapi: {
        summary: 'Predict fight outcome',
        description: 'Predict win/loss probability for a player in a session',
        tags: ['ML'],
        requestBody: {
          required: true,
          content: {
            'application/json': {
              schema: {
                type: 'object',
                properties: {
                  sessionId: { type: 'string', description: 'Combat session ID' },
                  playerName: { type: 'string', description: 'Player name' },
                },
                required: ['sessionId', 'playerName'],
              },
            },
          },
        },
        responses: {
          '200': { description: 'Fight outcome prediction' },
          '400': { description: 'Missing required parameters' },
          '404': { description: 'Session not found' },
        },
      },
    },

    // POST /ml/classify/playstyle - Playstyle classification
    {
      method: 'POST',
      path: '/ml/classify/playstyle',
      middleware: [authMiddleware],
      handler: async (req, res) => {
        const { sessionId, playerName } = req.body as {
          sessionId?: string;
          playerName?: string;
        };

        if (!sessionId) {
          throw new BadRequestError('sessionId is required');
        }
        if (!playerName) {
          throw new BadRequestError('playerName is required');
        }

        const session = await getSessionFromDatabase(database, sessionId);

        const classification = await predictor.classifyPlaystyle(session, playerName);

        res.json({
          data: {
            sessionId,
            playerName,
            classification: {
              primaryStyle: classification.primaryStyle,
              styleScores: classification.styleScores,
              confidence: classification.confidence,
              traits: classification.traits,
              isHeuristic: classification.isHeuristic,
              eventsAnalyzed: classification.eventsAnalyzed,
            },
          },
        });
      },
      openapi: {
        summary: 'Classify playstyle',
        description: 'Classify player playstyle (AGGRESSIVE, DEFENSIVE, BALANCED, OPPORTUNISTIC)',
        tags: ['ML'],
        requestBody: {
          required: true,
          content: {
            'application/json': {
              schema: {
                type: 'object',
                properties: {
                  sessionId: { type: 'string', description: 'Combat session ID' },
                  playerName: { type: 'string', description: 'Player name' },
                },
                required: ['sessionId', 'playerName'],
              },
            },
          },
        },
        responses: {
          '200': { description: 'Playstyle classification' },
          '400': { description: 'Missing required parameters' },
          '404': { description: 'Session not found' },
        },
      },
    },

    // POST /ml/predict/performance - Performance prediction
    {
      method: 'POST',
      path: '/ml/predict/performance',
      middleware: [authMiddleware],
      handler: async (req, res) => {
        const { sessionIds, playerName } = req.body as {
          sessionIds?: string[];
          playerName?: string;
        };

        if (!sessionIds || sessionIds.length === 0) {
          throw new BadRequestError('sessionIds array is required');
        }
        if (!playerName) {
          throw new BadRequestError('playerName is required');
        }

        // Retrieve all sessions
        const sessions: CombatSession[] = [];
        for (const sessionId of sessionIds) {
          const session = await getSessionFromDatabase(database, sessionId);
          sessions.push(session);
        }

        const prediction = await predictor.predictPerformance(sessions, playerName);

        res.json({
          data: {
            playerName,
            sessionCount: sessions.length,
            prediction: {
              predictedDps: prediction.predictedDps,
              dpsRange: prediction.dpsRange,
              predictedHps: prediction.predictedHps,
              hpsRange: prediction.hpsRange,
              predictedPerformance: prediction.predictedPerformance,
              performanceRange: prediction.performanceRange,
              confidence: prediction.confidence,
              isHeuristic: prediction.isHeuristic,
              sessionsAnalyzed: prediction.sessionsAnalyzed,
            },
          },
        });
      },
      openapi: {
        summary: 'Predict performance',
        description: 'Predict expected DPS/HPS based on historical sessions',
        tags: ['ML'],
        requestBody: {
          required: true,
          content: {
            'application/json': {
              schema: {
                type: 'object',
                properties: {
                  sessionIds: {
                    type: 'array',
                    items: { type: 'string' },
                    description: 'Historical session IDs',
                  },
                  playerName: { type: 'string', description: 'Player name' },
                },
                required: ['sessionIds', 'playerName'],
              },
            },
          },
        },
        responses: {
          '200': { description: 'Performance prediction' },
          '400': { description: 'Missing required parameters' },
          '404': { description: 'Session not found' },
        },
      },
    },

    // POST /ml/assess/threats - Threat assessment
    {
      method: 'POST',
      path: '/ml/assess/threats',
      middleware: [authMiddleware],
      handler: async (req, res) => {
        const { sessionId, selfEntity } = req.body as {
          sessionId?: string;
          selfEntity?: Entity;
        };

        if (!sessionId) {
          throw new BadRequestError('sessionId is required');
        }
        if (!selfEntity || !selfEntity.name) {
          throw new BadRequestError('selfEntity with name is required');
        }

        const session = await getSessionFromDatabase(database, sessionId);

        const threats = await predictor.assessThreats(session, selfEntity);

        res.json({
          data: {
            sessionId,
            selfEntity: selfEntity.name,
            threats: threats.map((threat) => ({
              entity: {
                name: threat.entity.name,
                entityType: threat.entity.entityType,
                realm: threat.entity.realm,
              },
              threatLevel: threat.threatLevel,
              threatCategory: threat.threatCategory,
              factors: threat.factors,
              recommendations: threat.recommendations,
              isHeuristic: threat.isHeuristic,
            })),
          },
        });
      },
      openapi: {
        summary: 'Assess threats',
        description: 'Rank enemies by threat level',
        tags: ['ML'],
        requestBody: {
          required: true,
          content: {
            'application/json': {
              schema: {
                type: 'object',
                properties: {
                  sessionId: { type: 'string', description: 'Combat session ID' },
                  selfEntity: {
                    type: 'object',
                    properties: {
                      name: { type: 'string' },
                      type: { type: 'string' },
                      realm: { type: 'string' },
                    },
                    required: ['name'],
                    description: 'Player entity to assess threats for',
                  },
                },
                required: ['sessionId', 'selfEntity'],
              },
            },
          },
        },
        responses: {
          '200': { description: 'Threat assessments' },
          '400': { description: 'Missing required parameters' },
          '404': { description: 'Session not found' },
        },
      },
    },

    // POST /ml/assess/single-threat - Single target threat assessment
    {
      method: 'POST',
      path: '/ml/assess/single-threat',
      middleware: [authMiddleware],
      handler: async (req, res) => {
        const { sessionId, selfEntity, targetEntity } = req.body as {
          sessionId?: string;
          selfEntity?: Entity;
          targetEntity?: Entity;
        };

        if (!sessionId) {
          throw new BadRequestError('sessionId is required');
        }
        if (!selfEntity || !selfEntity.name) {
          throw new BadRequestError('selfEntity with name is required');
        }
        if (!targetEntity || !targetEntity.name) {
          throw new BadRequestError('targetEntity with name is required');
        }

        const session = await getSessionFromDatabase(database, sessionId);

        const threat = await predictor.assessSingleThreat(session, selfEntity, targetEntity);

        res.json({
          data: {
            sessionId,
            selfEntity: selfEntity.name,
            target: {
              entity: {
                name: threat.entity.name,
                entityType: threat.entity.entityType,
                realm: threat.entity.realm,
              },
              threatLevel: threat.threatLevel,
              threatCategory: threat.threatCategory,
              factors: threat.factors,
              recommendations: threat.recommendations,
              isHeuristic: threat.isHeuristic,
            },
          },
        });
      },
      openapi: {
        summary: 'Assess single threat',
        description: 'Assess threat level of a specific target',
        tags: ['ML'],
        requestBody: {
          required: true,
          content: {
            'application/json': {
              schema: {
                type: 'object',
                properties: {
                  sessionId: { type: 'string', description: 'Combat session ID' },
                  selfEntity: {
                    type: 'object',
                    properties: { name: { type: 'string' } },
                    required: ['name'],
                  },
                  targetEntity: {
                    type: 'object',
                    properties: { name: { type: 'string' } },
                    required: ['name'],
                  },
                },
                required: ['sessionId', 'selfEntity', 'targetEntity'],
              },
            },
          },
        },
        responses: {
          '200': { description: 'Single threat assessment' },
          '400': { description: 'Missing required parameters' },
          '404': { description: 'Session not found' },
        },
      },
    },
  ];
}

/**
 * Helper to retrieve session from database
 */
async function getSessionFromDatabase(
  database: DatabaseAdapter,
  sessionId: string
): Promise<CombatSession> {
  // Get sessions matching this ID (should be 0 or 1)
  const sessions = await database
    .sessions()
    .withEvents()
    .withParticipants()
    .withSummary()
    .paginate({ limit: 1, offset: 0 })
    .execute();

  // Find the matching session by ID
  const sessionData = sessions.find((s) => s.id === sessionId);

  if (!sessionData) {
    throw new NotFoundError(`Session not found: ${sessionId}`);
  }

  return sessionData;
}
