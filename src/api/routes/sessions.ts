/**
 * Session query routes
 */
import type { RouteDefinition, Middleware, SessionFilterParams } from '../types.js';
import type { DatabaseAdapter } from '../../database/adapters/DatabaseAdapter.js';
import { NotFoundError, BadRequestError } from '../errors.js';

/**
 * Create session routes
 */
export function createSessionRoutes(
  database: DatabaseAdapter,
  authMiddleware: Middleware
): RouteDefinition[] {
  return [
    // GET /sessions - List sessions with filters
    {
      method: 'GET',
      path: '/sessions',
      middleware: [authMiddleware],
      handler: async (req, res) => {
        const params = parseSessionFilters(req.query);
        let query = database.sessions();

        // Apply filters
        if (params.startTime || params.endTime) {
          const start = params.startTime ? new Date(params.startTime) : undefined;
          const end = params.endTime ? new Date(params.endTime) : undefined;
          if (start) {
            query = query.inTimeRange(start, end);
          }
        }

        if (params.minDuration !== undefined) {
          query = query.minDuration(params.minDuration);
        }

        if (params.maxDuration !== undefined) {
          query = query.maxDuration(params.maxDuration);
        }

        if (params.participant) {
          query = query.withParticipant(params.participant);
        }

        if (params.minParticipants !== undefined) {
          query = query.minParticipants(params.minParticipants);
        }

        if (params.includeSummary) {
          query = query.withSummary();
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
        summary: 'List sessions',
        description: 'Retrieve combat sessions with optional filters',
        tags: ['Sessions'],
        parameters: [
          { name: 'startTime', in: 'query', description: 'Filter by start time', schema: { type: 'string', format: 'date-time' } },
          { name: 'endTime', in: 'query', description: 'Filter by end time', schema: { type: 'string', format: 'date-time' } },
          { name: 'minDuration', in: 'query', description: 'Minimum duration (ms)', schema: { type: 'number' } },
          { name: 'maxDuration', in: 'query', description: 'Maximum duration (ms)', schema: { type: 'number' } },
          { name: 'participant', in: 'query', description: 'Filter by participant name', schema: { type: 'string' } },
          { name: 'minParticipants', in: 'query', description: 'Minimum participant count', schema: { type: 'number' } },
          { name: 'includeSummary', in: 'query', description: 'Include session summary', schema: { type: 'boolean' } },
          { name: 'limit', in: 'query', description: 'Number of results', schema: { type: 'number' } },
          { name: 'offset', in: 'query', description: 'Offset for pagination', schema: { type: 'number' } },
        ],
        responses: {
          '200': { description: 'List of sessions' },
          '401': { description: 'Authentication required' },
        },
      },
    },

    // GET /sessions/:id - Get single session
    {
      method: 'GET',
      path: '/sessions/:id',
      middleware: [authMiddleware],
      handler: async (req, res) => {
        const id = req.params.id;
        if (!id) {
          throw new BadRequestError('Session ID is required');
        }
        const includeEvents = req.query.includeEvents === 'true';

        const session = await database.getSessionById(id);

        if (!session) {
          throw new NotFoundError('Session', id);
        }

        // Optionally load events
        let events;
        if (includeEvents) {
          events = await database.events().inSession(id).execute();
        }

        res.json({
          data: {
            ...session,
            ...(events && { events }),
          },
        });
      },
      openapi: {
        summary: 'Get session by ID',
        description: 'Retrieve a single session by its ID',
        tags: ['Sessions'],
        parameters: [
          { name: 'id', in: 'path', required: true, description: 'Session ID', schema: { type: 'string' } },
          { name: 'includeEvents', in: 'query', description: 'Include events', schema: { type: 'boolean' } },
          { name: 'includeSummary', in: 'query', description: 'Include summary', schema: { type: 'boolean' } },
        ],
        responses: {
          '200': { description: 'Session details' },
          '404': { description: 'Session not found' },
        },
      },
    },

    // GET /sessions/:id/events - Get events in session
    {
      method: 'GET',
      path: '/sessions/:id/events',
      middleware: [authMiddleware],
      handler: async (req, res) => {
        const id = req.params.id;
        if (!id) {
          throw new BadRequestError('Session ID is required');
        }

        // Verify session exists
        const session = await database.getSessionById(id);
        if (!session) {
          throw new NotFoundError('Session', id);
        }

        let query = database.events().inSession(id);

        // Optional type filter
        const type = req.query.type as string | undefined;
        if (type) {
          const EventType = await import('../../types/index.js').then(m => m.EventType);
          const eventType = EventType[type as keyof typeof EventType];
          if (eventType !== undefined) {
            query = query.byType(eventType);
          }
        }

        // Pagination
        const limit = req.query.limit ? parseInt(req.query.limit as string, 10) : 100;
        const offset = req.query.offset ? parseInt(req.query.offset as string, 10) : 0;

        const total = await query.count();
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
        summary: 'Get session events',
        description: 'Retrieve all events in a session',
        tags: ['Sessions'],
        parameters: [
          { name: 'id', in: 'path', required: true, description: 'Session ID', schema: { type: 'string' } },
          { name: 'type', in: 'query', description: 'Filter by event type', schema: { type: 'string' } },
          { name: 'limit', in: 'query', description: 'Number of results', schema: { type: 'number' } },
          { name: 'offset', in: 'query', description: 'Offset for pagination', schema: { type: 'number' } },
        ],
        responses: {
          '200': { description: 'Session events' },
          '404': { description: 'Session not found' },
        },
      },
    },

    // GET /sessions/:id/participants - Get session participants
    {
      method: 'GET',
      path: '/sessions/:id/participants',
      middleware: [authMiddleware],
      handler: async (req, res) => {
        const id = req.params.id;
        if (!id) {
          throw new BadRequestError('Session ID is required');
        }

        // Verify session exists
        const session = await database.getSessionById(id);
        if (!session) {
          throw new NotFoundError('Session', id);
        }

        const participants = await database.getParticipantsBySession(id);

        res.json({
          data: participants,
        });
      },
      openapi: {
        summary: 'Get session participants',
        description: 'Retrieve all participants in a session',
        tags: ['Sessions'],
        parameters: [
          { name: 'id', in: 'path', required: true, description: 'Session ID', schema: { type: 'string' } },
        ],
        responses: {
          '200': { description: 'Session participants' },
          '404': { description: 'Session not found' },
        },
      },
    },

    // GET /sessions/:id/summary - Get session summary
    {
      method: 'GET',
      path: '/sessions/:id/summary',
      middleware: [authMiddleware],
      handler: async (req, res) => {
        const id = req.params.id;
        if (!id) {
          throw new BadRequestError('Session ID is required');
        }

        const session = await database.getSessionById(id);
        if (!session) {
          throw new NotFoundError('Session', id);
        }

        res.json({
          data: {
            id: session.id,
            startTime: session.startTime,
            endTime: session.endTime,
            durationMs: session.durationMs,
            summary: session.summary,
          },
        });
      },
      openapi: {
        summary: 'Get session summary',
        description: 'Retrieve session summary statistics',
        tags: ['Sessions'],
        parameters: [
          { name: 'id', in: 'path', required: true, description: 'Session ID', schema: { type: 'string' } },
        ],
        responses: {
          '200': { description: 'Session summary' },
          '404': { description: 'Session not found' },
        },
      },
    },
  ];
}

/**
 * Parse session filter parameters from query
 */
function parseSessionFilters(
  query: Record<string, string | string[] | undefined>
): SessionFilterParams {
  return {
    startTime: query.startTime as string | undefined,
    endTime: query.endTime as string | undefined,
    minDuration: query.minDuration ? parseInt(query.minDuration as string, 10) : undefined,
    maxDuration: query.maxDuration ? parseInt(query.maxDuration as string, 10) : undefined,
    participant: query.participant as string | undefined,
    minParticipants: query.minParticipants
      ? parseInt(query.minParticipants as string, 10)
      : undefined,
    includeEvents: query.includeEvents === 'true',
    includeSummary: query.includeSummary === 'true',
    limit: query.limit ? parseInt(query.limit as string, 10) : undefined,
    offset: query.offset ? parseInt(query.offset as string, 10) : undefined,
    orderBy: query.orderBy as string | undefined,
    orderDir: query.orderDir as 'asc' | 'desc' | undefined,
  };
}
