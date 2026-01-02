/**
 * Aggregation and statistics routes
 */
import type { RouteDefinition, Middleware } from '../types.js';
import type { DatabaseAdapter, TimeBucket } from '../../database/index.js';
import { BadRequestError } from '../errors.js';

/**
 * Create stats routes
 */
export function createStatsRoutes(
  database: DatabaseAdapter,
  authMiddleware: Middleware
): RouteDefinition[] {
  return [
    // GET /stats/leaderboard - Get top performers
    {
      method: 'GET',
      path: '/stats/leaderboard',
      middleware: [authMiddleware],
      handler: async (req, res) => {
        const metric = (req.query.metric as string) || 'performance';
        const limit = req.query.limit ? parseInt(req.query.limit as string, 10) : 10;

        // Validate metric
        const validMetrics = ['damage', 'healing', 'kills', 'kdr', 'performance'];
        if (!validMetrics.includes(metric)) {
          throw new BadRequestError(
            `Invalid metric. Must be one of: ${validMetrics.join(', ')}`
          );
        }

        const topPerformers = await database
          .stats()
          .topPerformers(metric as any, limit);

        res.json({
          data: {
            metric,
            limit,
            players: topPerformers,
          },
        });
      },
      openapi: {
        summary: 'Get leaderboard',
        description: 'Retrieve top performers by metric',
        tags: ['Stats'],
        parameters: [
          {
            name: 'metric',
            in: 'query',
            description: 'Metric to rank by',
            schema: { type: 'string', enum: ['damage', 'healing', 'kills', 'kdr', 'performance'] },
          },
          { name: 'limit', in: 'query', description: 'Number of results', schema: { type: 'number' } },
        ],
        responses: {
          '200': { description: 'Leaderboard' },
          '400': { description: 'Invalid metric' },
        },
      },
    },

    // GET /stats/aggregations/time - Time-bucket aggregation
    {
      method: 'GET',
      path: '/stats/aggregations/time',
      middleware: [authMiddleware],
      handler: async (req, res) => {
        const bucket = (req.query.bucket as TimeBucket) || 'day';
        const startTime = req.query.startTime as string | undefined;
        const endTime = req.query.endTime as string | undefined;

        // Validate bucket
        const validBuckets: TimeBucket[] = ['hour', 'day', 'week', 'month'];
        if (!validBuckets.includes(bucket)) {
          throw new BadRequestError(
            `Invalid bucket. Must be one of: ${validBuckets.join(', ')}`
          );
        }

        const start = startTime
          ? new Date(startTime)
          : new Date(Date.now() - 30 * 24 * 60 * 60 * 1000); // Default: 30 days ago
        const end = endTime ? new Date(endTime) : new Date();

        const aggregation = await database
          .aggregations()
          .eventsByTimeBucket(bucket, start, end);

        res.json({
          data: {
            bucket,
            startTime: start.toISOString(),
            endTime: end.toISOString(),
            results: aggregation,
          },
        });
      },
      openapi: {
        summary: 'Time aggregation',
        description: 'Aggregate events by time bucket',
        tags: ['Stats'],
        parameters: [
          {
            name: 'bucket',
            in: 'query',
            description: 'Time bucket size',
            schema: { type: 'string', enum: ['hour', 'day', 'week', 'month'] },
          },
          { name: 'startTime', in: 'query', description: 'Start time (ISO)', schema: { type: 'string', format: 'date-time' } },
          { name: 'endTime', in: 'query', description: 'End time (ISO)', schema: { type: 'string', format: 'date-time' } },
        ],
        responses: {
          '200': { description: 'Time aggregation results' },
          '400': { description: 'Invalid bucket' },
        },
      },
    },

    // GET /stats/aggregations/damage - Damage by entity
    {
      method: 'GET',
      path: '/stats/aggregations/damage',
      middleware: [authMiddleware],
      handler: async (req, res) => {
        const sessionId = req.query.sessionId as string | undefined;

        if (!sessionId) {
          throw new BadRequestError('sessionId is required');
        }

        const aggregation = await database
          .aggregations()
          .damageByEntity(sessionId);

        res.json({
          data: {
            sessionId,
            results: aggregation,
          },
        });
      },
      openapi: {
        summary: 'Damage aggregation',
        description: 'Aggregate damage by entity in a session',
        tags: ['Stats'],
        parameters: [
          { name: 'sessionId', in: 'query', required: true, description: 'Session ID', schema: { type: 'string' } },
        ],
        responses: {
          '200': { description: 'Damage aggregation results' },
          '400': { description: 'sessionId required' },
        },
      },
    },

    // GET /stats/aggregations/healing - Healing by entity
    {
      method: 'GET',
      path: '/stats/aggregations/healing',
      middleware: [authMiddleware],
      handler: async (req, res) => {
        const sessionId = req.query.sessionId as string | undefined;

        if (!sessionId) {
          throw new BadRequestError('sessionId is required');
        }

        const aggregation = await database
          .aggregations()
          .healingByEntity(sessionId);

        res.json({
          data: {
            sessionId,
            results: aggregation,
          },
        });
      },
      openapi: {
        summary: 'Healing aggregation',
        description: 'Aggregate healing by entity in a session',
        tags: ['Stats'],
        parameters: [
          { name: 'sessionId', in: 'query', required: true, description: 'Session ID', schema: { type: 'string' } },
        ],
        responses: {
          '200': { description: 'Healing aggregation results' },
          '400': { description: 'sessionId required' },
        },
      },
    },

    // GET /stats/aggregations/events - Event type distribution
    {
      method: 'GET',
      path: '/stats/aggregations/events',
      middleware: [authMiddleware],
      handler: async (req, res) => {
        const startTime = req.query.startTime as string | undefined;
        const endTime = req.query.endTime as string | undefined;

        const start = startTime
          ? new Date(startTime)
          : new Date(Date.now() - 30 * 24 * 60 * 60 * 1000);
        const end = endTime ? new Date(endTime) : new Date();

        const distribution = await database
          .aggregations()
          .eventTypeDistribution(start, end);

        res.json({
          data: {
            startTime: start.toISOString(),
            endTime: end.toISOString(),
            distribution,
          },
        });
      },
      openapi: {
        summary: 'Event distribution',
        description: 'Get event type distribution',
        tags: ['Stats'],
        parameters: [
          { name: 'startTime', in: 'query', description: 'Start time (ISO)', schema: { type: 'string', format: 'date-time' } },
          { name: 'endTime', in: 'query', description: 'End time (ISO)', schema: { type: 'string', format: 'date-time' } },
        ],
        responses: {
          '200': { description: 'Event distribution' },
        },
      },
    },
  ];
}
