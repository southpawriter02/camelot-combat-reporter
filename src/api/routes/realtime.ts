/**
 * Real-time SSE streaming routes
 */
import type { RouteDefinition, Middleware } from '../types.js';
import type { SSEManager } from '../ApiServer.js';
import type { ApiServer } from '../ApiServer.js';
import { ServiceUnavailableError } from '../errors.js';

/**
 * Create real-time routes
 */
export function createRealtimeRoutes(
  sseManager: SSEManager,
  server: ApiServer,
  authMiddleware: Middleware
): RouteDefinition[] {
  return [
    // GET /realtime/events - SSE stream of combat events
    {
      method: 'GET',
      path: '/realtime/events',
      middleware: [authMiddleware],
      handler: (req, res) => {
        const monitor = server.getMonitor();
        if (!monitor) {
          throw new ServiceUnavailableError('Real-time monitor not attached');
        }

        // Parse event type filter
        const typesParam = req.query.types as string | undefined;
        const eventTypes = typesParam ? typesParam.split(',').map((t) => t.trim()) : [];

        // Add SSE connection
        const connectionId = sseManager.addConnection(req.clientId, res, eventTypes);

        // Log connection
        server.emit('sse:connect', req.clientId, eventTypes);

        // Handle disconnect
        res.on('close', () => {
          server.emit('sse:disconnect', req.clientId);
        });
      },
      openapi: {
        summary: 'Stream combat events',
        description: 'Server-Sent Events stream of real-time combat events',
        tags: ['Real-time'],
        parameters: [
          {
            name: 'types',
            in: 'query',
            description: 'Comma-separated list of event types to filter',
            schema: { type: 'string' },
          },
        ],
        responses: {
          '200': {
            description: 'SSE stream',
            content: {
              'text/event-stream': {
                schema: { type: 'string' },
              },
            },
          },
          '503': { description: 'Monitor not attached' },
        },
      },
    },

    // GET /realtime/sessions - SSE stream of session updates
    {
      method: 'GET',
      path: '/realtime/sessions',
      middleware: [authMiddleware],
      handler: (req, res) => {
        const monitor = server.getMonitor();
        if (!monitor) {
          throw new ServiceUnavailableError('Real-time monitor not attached');
        }

        // Filter to session events only
        const eventTypes = ['session_start', 'session_update', 'session_end'];

        // Add SSE connection
        const connectionId = sseManager.addConnection(req.clientId, res, eventTypes);

        // Log connection
        server.emit('sse:connect', req.clientId, eventTypes);

        // Handle disconnect
        res.on('close', () => {
          server.emit('sse:disconnect', req.clientId);
        });
      },
      openapi: {
        summary: 'Stream session updates',
        description: 'Server-Sent Events stream of session start/update/end events',
        tags: ['Real-time'],
        responses: {
          '200': {
            description: 'SSE stream',
            content: {
              'text/event-stream': {
                schema: { type: 'string' },
              },
            },
          },
          '503': { description: 'Monitor not attached' },
        },
      },
    },

    // GET /realtime/status - Get monitor status
    {
      method: 'GET',
      path: '/realtime/status',
      middleware: [authMiddleware],
      handler: (req, res) => {
        const monitor = server.getMonitor();

        if (!monitor) {
          res.json({
            data: {
              attached: false,
              status: null,
            },
          });
          return;
        }

        const status = monitor.getStatus();

        res.json({
          data: {
            attached: true,
            status: {
              state: status.state,
              filename: status.filename,
              position: status.position,
              activeSession: status.activeSession,
              stats: {
                linesProcessed: status.stats.linesProcessed,
                eventsEmitted: status.stats.eventsEmitted,
                sessionsDetected: status.stats.sessionsDetected,
                runtimeMs: status.stats.runtimeMs,
              },
            },
          },
        });
      },
      openapi: {
        summary: 'Get monitor status',
        description: 'Get the status of the real-time monitor',
        tags: ['Real-time'],
        responses: {
          '200': { description: 'Monitor status' },
        },
      },
    },
  ];
}
