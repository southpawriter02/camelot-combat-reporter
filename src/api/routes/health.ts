/**
 * Health check routes
 */
import type { RouteDefinition } from '../types.js';
import type { ApiServer } from '../ApiServer.js';

/**
 * Create health check routes
 */
export function createHealthRoutes(server: ApiServer): RouteDefinition[] {
  return [
    {
      method: 'GET',
      path: '/health',
      handler: (_req, res) => {
        const status = server.getStatus();
        const database = server.getDatabase();

        res.json({
          status: status.state === 'running' ? 'healthy' : 'unhealthy',
          timestamp: new Date().toISOString(),
          uptime: status.uptime,
          database: {
            connected: database.isConnected(),
            backend: database.backend,
          },
          sse: {
            connections: status.sseConnections,
          },
        });
      },
      openapi: {
        summary: 'Health check',
        description: 'Returns the health status of the API server',
        tags: ['Health'],
        responses: {
          '200': {
            description: 'Health status',
            content: {
              'application/json': {
                schema: {
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
                    sse: {
                      type: 'object',
                      properties: {
                        connections: { type: 'number' },
                      },
                    },
                  },
                },
              },
            },
          },
        },
      },
    },
    {
      method: 'GET',
      path: '/health/ready',
      handler: (_req, res) => {
        const status = server.getStatus();
        const database = server.getDatabase();

        const isReady = status.state === 'running' && database.isConnected();

        if (isReady) {
          res.json({
            ready: true,
            timestamp: new Date().toISOString(),
          });
        } else {
          res.json(
            {
              ready: false,
              timestamp: new Date().toISOString(),
              reason: !database.isConnected()
                ? 'Database not connected'
                : 'Server not running',
            },
            503
          );
        }
      },
      openapi: {
        summary: 'Readiness check',
        description: 'Returns whether the server is ready to accept requests',
        tags: ['Health'],
        responses: {
          '200': {
            description: 'Server is ready',
            content: {
              'application/json': {
                schema: {
                  type: 'object',
                  properties: {
                    ready: { type: 'boolean' },
                    timestamp: { type: 'string', format: 'date-time' },
                  },
                },
              },
            },
          },
          '503': {
            description: 'Server is not ready',
            content: {
              'application/json': {
                schema: {
                  type: 'object',
                  properties: {
                    ready: { type: 'boolean' },
                    timestamp: { type: 'string', format: 'date-time' },
                    reason: { type: 'string' },
                  },
                },
              },
            },
          },
        },
      },
    },
  ];
}
