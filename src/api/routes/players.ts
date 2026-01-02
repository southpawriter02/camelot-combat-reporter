/**
 * Player statistics routes
 */
import type { RouteDefinition, Middleware, PlayerStatsFilterParams } from '../types.js';
import type { DatabaseAdapter } from '../../database/adapters/DatabaseAdapter.js';
import { NotFoundError, BadRequestError } from '../errors.js';

/**
 * Create player routes
 */
export function createPlayerRoutes(
  database: DatabaseAdapter,
  authMiddleware: Middleware
): RouteDefinition[] {
  return [
    // GET /players - List all known players
    {
      method: 'GET',
      path: '/players',
      middleware: [authMiddleware],
      handler: async (req, res) => {
        const limit = req.query.limit ? parseInt(req.query.limit as string, 10) : 50;
        const offset = req.query.offset ? parseInt(req.query.offset as string, 10) : 0;

        // Get all player aggregate stats
        const allStats = await database.stats().allPlayerAggregates();

        // Paginate results
        const total = allStats.length;
        const players = allStats.slice(offset, offset + limit);

        res.paginate(total, limit, offset);
        res.json({
          data: players.map((s) => ({
            name: s.playerName,
            totalSessions: s.totalSessions,
            totalCombatTimeMs: s.totalCombatTimeMs,
            avgPerformanceScore: s.avgPerformanceScore,
            consistencyRating: s.consistencyRating,
          })),
          meta: {
            total,
            limit,
            offset,
            hasMore: offset + players.length < total,
          },
        });
      },
      openapi: {
        summary: 'List players',
        description: 'Retrieve all known players with basic stats',
        tags: ['Players'],
        parameters: [
          { name: 'limit', in: 'query', description: 'Number of results', schema: { type: 'number' } },
          { name: 'offset', in: 'query', description: 'Offset for pagination', schema: { type: 'number' } },
        ],
        responses: {
          '200': { description: 'List of players' },
          '401': { description: 'Authentication required' },
        },
      },
    },

    // GET /players/:name - Get player info
    {
      method: 'GET',
      path: '/players/:name',
      middleware: [authMiddleware],
      handler: async (req, res) => {
        const name = req.params.name;
        if (!name) {
          throw new BadRequestError('Player name is required');
        }

        // Try to get player entity
        const entity = await database.getEntityByName(name);
        if (!entity) {
          throw new NotFoundError('Player', name);
        }

        // Get aggregate stats if available
        const stats = await database.stats().playerAggregate(name);

        res.json({
          data: {
            entity,
            stats: stats || null,
          },
        });
      },
      openapi: {
        summary: 'Get player info',
        description: 'Retrieve player entity and aggregate statistics',
        tags: ['Players'],
        parameters: [
          { name: 'name', in: 'path', required: true, description: 'Player name', schema: { type: 'string' } },
        ],
        responses: {
          '200': { description: 'Player info' },
          '404': { description: 'Player not found' },
        },
      },
    },

    // GET /players/:name/sessions - Get player's sessions
    {
      method: 'GET',
      path: '/players/:name/sessions',
      middleware: [authMiddleware],
      handler: async (req, res) => {
        const name = req.params.name;
        if (!name) {
          throw new BadRequestError('Player name is required');
        }
        const params = parsePlayerStatsFilters(req.query);

        // Get sessions with this participant
        let query = database.sessions().withParticipant(name);

        if (params.startTime || params.endTime) {
          const start = params.startTime ? new Date(params.startTime) : undefined;
          const end = params.endTime ? new Date(params.endTime) : undefined;
          if (start) {
            query = query.inTimeRange(start, end);
          }
        }

        const total = await query.count();

        const limit = params.limit ?? 50;
        const offset = params.offset ?? 0;
        query = query.paginate({ limit, offset });

        const sessions = await query.execute();

        res.paginate(total, limit, offset);
        res.json({
          data: sessions,
          meta: {
            total,
            limit,
            offset,
            hasMore: offset + sessions.length < total,
          },
        });
      },
      openapi: {
        summary: 'Get player sessions',
        description: 'Retrieve sessions where the player participated',
        tags: ['Players'],
        parameters: [
          { name: 'name', in: 'path', required: true, description: 'Player name', schema: { type: 'string' } },
          { name: 'startTime', in: 'query', description: 'Filter by start time', schema: { type: 'string', format: 'date-time' } },
          { name: 'endTime', in: 'query', description: 'Filter by end time', schema: { type: 'string', format: 'date-time' } },
          { name: 'limit', in: 'query', description: 'Number of results', schema: { type: 'number' } },
          { name: 'offset', in: 'query', description: 'Offset for pagination', schema: { type: 'number' } },
        ],
        responses: {
          '200': { description: 'Player sessions' },
          '404': { description: 'Player not found' },
        },
      },
    },

    // GET /players/:name/stats - Get player aggregate stats
    {
      method: 'GET',
      path: '/players/:name/stats',
      middleware: [authMiddleware],
      handler: async (req, res) => {
        const name = req.params.name;
        if (!name) {
          throw new BadRequestError('Player name is required');
        }

        const stats = await database.stats().playerAggregate(name);

        if (!stats) {
          throw new NotFoundError('Player stats', name);
        }

        res.json({
          data: stats,
        });
      },
      openapi: {
        summary: 'Get player aggregate stats',
        description: 'Retrieve player lifetime statistics',
        tags: ['Players'],
        parameters: [
          { name: 'name', in: 'path', required: true, description: 'Player name', schema: { type: 'string' } },
        ],
        responses: {
          '200': { description: 'Player aggregate stats' },
          '404': { description: 'Player stats not found' },
        },
      },
    },

    // GET /players/:name/stats/sessions - Get player per-session stats
    {
      method: 'GET',
      path: '/players/:name/stats/sessions',
      middleware: [authMiddleware],
      handler: async (req, res) => {
        const name = req.params.name;
        if (!name) {
          throw new BadRequestError('Player name is required');
        }
        const params = parsePlayerStatsFilters(req.query);

        let query = database.stats().playerSessions(name);

        if (params.startTime || params.endTime) {
          const start = params.startTime ? new Date(params.startTime) : undefined;
          const end = params.endTime ? new Date(params.endTime) : undefined;
          if (start) {
            query = query.inTimeRange(start, end);
          }
        }

        if (params.role) {
          query = query.byRole(params.role);
        }

        if (params.minPerformance !== undefined) {
          query = query.minPerformance(params.minPerformance);
        }

        const total = await query.count();

        const limit = params.limit ?? 50;
        const offset = params.offset ?? 0;
        query = query.orderBy('session_start', 'desc');
        query = query.paginate({ limit, offset });

        const stats = await query.execute();

        res.paginate(total, limit, offset);
        res.json({
          data: stats,
          meta: {
            total,
            limit,
            offset,
            hasMore: offset + stats.length < total,
          },
        });
      },
      openapi: {
        summary: 'Get player session stats',
        description: 'Retrieve player per-session statistics',
        tags: ['Players'],
        parameters: [
          { name: 'name', in: 'path', required: true, description: 'Player name', schema: { type: 'string' } },
          { name: 'startTime', in: 'query', description: 'Filter by start time', schema: { type: 'string', format: 'date-time' } },
          { name: 'endTime', in: 'query', description: 'Filter by end time', schema: { type: 'string', format: 'date-time' } },
          { name: 'role', in: 'query', description: 'Filter by role', schema: { type: 'string' } },
          { name: 'minPerformance', in: 'query', description: 'Minimum performance score', schema: { type: 'number' } },
          { name: 'limit', in: 'query', description: 'Number of results', schema: { type: 'number' } },
          { name: 'offset', in: 'query', description: 'Offset for pagination', schema: { type: 'number' } },
        ],
        responses: {
          '200': { description: 'Player session stats' },
          '404': { description: 'Player not found' },
        },
      },
    },

    // GET /players/:name/trends - Get player trend data
    {
      method: 'GET',
      path: '/players/:name/trends',
      middleware: [authMiddleware],
      handler: async (req, res) => {
        const name = req.params.name;
        if (!name) {
          throw new BadRequestError('Player name is required');
        }
        const metric = (req.query.metric as string) || 'performance';

        const stats = await database.stats().playerAggregate(name);

        if (!stats) {
          throw new NotFoundError('Player stats', name);
        }

        // Get trend data based on metric
        let trends;
        switch (metric) {
          case 'dps':
            trends = stats.dpsOverTime || [];
            break;
          case 'kdr':
            trends = stats.kdrOverTime || [];
            break;
          case 'performance':
          default:
            trends = stats.performanceOverTime || [];
            break;
        }

        res.json({
          data: {
            playerName: name,
            metric,
            trends,
          },
        });
      },
      openapi: {
        summary: 'Get player trends',
        description: 'Retrieve player performance trends over time',
        tags: ['Players'],
        parameters: [
          { name: 'name', in: 'path', required: true, description: 'Player name', schema: { type: 'string' } },
          { name: 'metric', in: 'query', description: 'Metric to get trends for (dps, kdr, performance)', schema: { type: 'string', enum: ['dps', 'kdr', 'performance'] } },
        ],
        responses: {
          '200': { description: 'Player trends' },
          '404': { description: 'Player stats not found' },
        },
      },
    },
  ];
}

/**
 * Parse player stats filter parameters from query
 */
function parsePlayerStatsFilters(
  query: Record<string, string | string[] | undefined>
): PlayerStatsFilterParams {
  return {
    startTime: query.startTime as string | undefined,
    endTime: query.endTime as string | undefined,
    role: query.role as string | undefined,
    minPerformance: query.minPerformance
      ? parseFloat(query.minPerformance as string)
      : undefined,
    limit: query.limit ? parseInt(query.limit as string, 10) : undefined,
    offset: query.offset ? parseInt(query.offset as string, 10) : undefined,
  };
}
