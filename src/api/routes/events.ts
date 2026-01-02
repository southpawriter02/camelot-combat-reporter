/**
 * Event query routes
 */
import type { RouteDefinition, Middleware, EventFilterParams } from '../types.js';
import type { DatabaseAdapter } from '../../database/adapters/DatabaseAdapter.js';
import { EventType } from '../../types/index.js';
import { NotFoundError, BadRequestError } from '../errors.js';

/**
 * Create event routes
 */
export function createEventRoutes(
  database: DatabaseAdapter,
  authMiddleware: Middleware
): RouteDefinition[] {
  return [
    // GET /events - List events with filters
    {
      method: 'GET',
      path: '/events',
      middleware: [authMiddleware],
      handler: async (req, res) => {
        const params = parseEventFilters(req.query);
        let query = database.events();

        // Apply filters
        if (params.type) {
          const types = Array.isArray(params.type) ? params.type : [params.type];
          const eventTypes = types
            .map((t) => EventType[t as keyof typeof EventType])
            .filter((t) => t !== undefined);
          if (eventTypes.length > 0) {
            query = query.byType(...eventTypes);
          }
        }

        if (params.startTime || params.endTime) {
          const start = params.startTime ? new Date(params.startTime) : undefined;
          const end = params.endTime ? new Date(params.endTime) : undefined;
          if (start) {
            query = query.inTimeRange(start, end);
          }
        }

        if (params.sessionId) {
          query = query.inSession(params.sessionId);
        }

        if (params.source) {
          query = query.fromEntity(params.source);
        }

        if (params.target) {
          query = query.toEntity(params.target);
        }

        if (params.minAmount !== undefined) {
          query = query.withMinAmount(params.minAmount);
        }

        if (params.criticalOnly) {
          query = query.criticalOnly();
        }

        // Sorting
        if (params.orderBy) {
          query = query.orderBy(params.orderBy, params.orderDir || 'desc');
        }

        // Get total count before pagination
        const total = await query.count();

        // Pagination
        const limit = params.limit ?? 50;
        const offset = params.offset ?? 0;
        query = query.paginate({ limit, offset });

        const events = await query.execute();

        res.paginate(total, limit, offset);
        res.json({
          data: events,
          meta: {
            total,
            limit,
            offset,
            hasMore: offset + events.length < total,
          },
        });
      },
      openapi: {
        summary: 'List events',
        description: 'Retrieve combat events with optional filters',
        tags: ['Events'],
        parameters: [
          { name: 'type', in: 'query', description: 'Filter by event type', schema: { type: 'string' } },
          { name: 'startTime', in: 'query', description: 'Filter by start time (ISO date)', schema: { type: 'string', format: 'date-time' } },
          { name: 'endTime', in: 'query', description: 'Filter by end time (ISO date)', schema: { type: 'string', format: 'date-time' } },
          { name: 'sessionId', in: 'query', description: 'Filter by session ID', schema: { type: 'string' } },
          { name: 'source', in: 'query', description: 'Filter by source entity name', schema: { type: 'string' } },
          { name: 'target', in: 'query', description: 'Filter by target entity name', schema: { type: 'string' } },
          { name: 'minAmount', in: 'query', description: 'Minimum damage/healing amount', schema: { type: 'number' } },
          { name: 'criticalOnly', in: 'query', description: 'Only critical hits/heals', schema: { type: 'boolean' } },
          { name: 'limit', in: 'query', description: 'Number of results (default: 50)', schema: { type: 'number' } },
          { name: 'offset', in: 'query', description: 'Offset for pagination', schema: { type: 'number' } },
          { name: 'orderBy', in: 'query', description: 'Field to sort by', schema: { type: 'string' } },
          { name: 'orderDir', in: 'query', description: 'Sort direction', schema: { type: 'string', enum: ['asc', 'desc'] } },
        ],
        responses: {
          '200': { description: 'List of events' },
          '400': { description: 'Invalid parameters' },
          '401': { description: 'Authentication required' },
        },
      },
    },

    // GET /events/types - List available event types
    {
      method: 'GET',
      path: '/events/types',
      middleware: [authMiddleware],
      handler: (_req, res) => {
        const types = Object.keys(EventType).filter(
          (key) => isNaN(Number(key))
        );
        res.json({
          data: types,
        });
      },
      openapi: {
        summary: 'List event types',
        description: 'Get all available event types',
        tags: ['Events'],
        responses: {
          '200': { description: 'List of event types' },
        },
      },
    },

    // GET /events/:id - Get single event
    {
      method: 'GET',
      path: '/events/:id',
      middleware: [authMiddleware],
      handler: async (req, res) => {
        const id = req.params.id;
        if (!id) {
          throw new BadRequestError('Event ID is required');
        }
        const event = await database.getEventById(id);

        if (!event) {
          throw new NotFoundError('Event', id);
        }

        res.json({ data: event });
      },
      openapi: {
        summary: 'Get event by ID',
        description: 'Retrieve a single event by its ID',
        tags: ['Events'],
        parameters: [
          { name: 'id', in: 'path', required: true, description: 'Event ID', schema: { type: 'string' } },
        ],
        responses: {
          '200': { description: 'Event details' },
          '404': { description: 'Event not found' },
        },
      },
    },
  ];
}

/**
 * Parse event filter parameters from query
 */
function parseEventFilters(
  query: Record<string, string | string[] | undefined>
): EventFilterParams {
  return {
    type: query.type as string | string[] | undefined,
    startTime: query.startTime as string | undefined,
    endTime: query.endTime as string | undefined,
    sessionId: query.sessionId as string | undefined,
    source: query.source as string | undefined,
    target: query.target as string | undefined,
    minAmount: query.minAmount ? parseInt(query.minAmount as string, 10) : undefined,
    actionType: query.actionType as string | string[] | undefined,
    damageType: query.damageType as string | string[] | undefined,
    criticalOnly: query.criticalOnly === 'true',
    limit: query.limit ? parseInt(query.limit as string, 10) : undefined,
    offset: query.offset ? parseInt(query.offset as string, 10) : undefined,
    orderBy: query.orderBy as string | undefined,
    orderDir: query.orderDir as 'asc' | 'desc' | undefined,
  };
}
