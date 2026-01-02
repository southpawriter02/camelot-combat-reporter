/**
 * OpenAPI specification generator
 */
import type { RouteDefinition, OpenApiConfig, OpenApiSchema } from '../types.js';
import { getSchemas } from './schemas.js';

/**
 * OpenAPI 3.0 specification structure
 */
interface OpenApiSpec {
  openapi: string;
  info: {
    title: string;
    version: string;
    description: string;
  };
  servers: Array<{ url: string; description?: string }>;
  paths: Record<string, Record<string, unknown>>;
  components: {
    securitySchemes: Record<string, unknown>;
    schemas: Record<string, OpenApiSchema>;
  };
  security: Array<Record<string, string[]>>;
  tags: Array<{ name: string; description: string }>;
}

/**
 * Generate OpenAPI specification from routes
 */
export function generateOpenApiSpec(
  routes: RouteDefinition[],
  config: OpenApiConfig
): OpenApiSpec {
  const spec: OpenApiSpec = {
    openapi: '3.0.3',
    info: {
      title: config.title,
      version: config.version,
      description: config.description,
    },
    servers: config.servers,
    paths: {},
    components: {
      securitySchemes: {
        ApiKeyHeader: {
          type: 'apiKey',
          in: 'header',
          name: 'X-API-Key',
          description: 'API key passed in the X-API-Key header',
        },
        ApiKeyQuery: {
          type: 'apiKey',
          in: 'query',
          name: 'api_key',
          description: 'API key passed as a query parameter',
        },
      },
      schemas: getSchemas(),
    },
    security: [{ ApiKeyHeader: [] }, { ApiKeyQuery: [] }],
    tags: [
      { name: 'Health', description: 'Health check endpoints' },
      { name: 'Events', description: 'Combat event operations' },
      { name: 'Sessions', description: 'Combat session operations' },
      { name: 'Players', description: 'Player statistics operations' },
      { name: 'Stats', description: 'Aggregation and analytics' },
      { name: 'Real-time', description: 'Real-time streaming endpoints' },
      { name: 'Documentation', description: 'API documentation' },
    ],
  };

  // Build paths from routes
  for (const route of routes) {
    if (!route.openapi) continue;

    // Convert Express-style params to OpenAPI
    const pathKey = route.path.replace(/:([a-zA-Z_][a-zA-Z0-9_]*)/g, '{$1}');
    const method = route.method.toLowerCase();

    if (!spec.paths[pathKey]) {
      spec.paths[pathKey] = {};
    }

    const operation: Record<string, unknown> = {
      summary: route.openapi.summary,
      tags: route.openapi.tags,
      responses: buildResponses(route.openapi.responses),
    };

    if (route.openapi.description) {
      operation.description = route.openapi.description;
    }

    if (route.openapi.parameters && route.openapi.parameters.length > 0) {
      operation.parameters = route.openapi.parameters;
    }

    if (route.openapi.requestBody) {
      operation.requestBody = route.openapi.requestBody;
    }

    // Add operation ID
    operation.operationId = generateOperationId(route.method, route.path);

    spec.paths[pathKey][method] = operation;
  }

  return spec;
}

/**
 * Build response definitions with common error responses
 */
function buildResponses(
  responses: Record<string, { description: string; content?: Record<string, unknown> }>
): Record<string, unknown> {
  const result: Record<string, unknown> = {};

  for (const [code, response] of Object.entries(responses)) {
    result[code] = {
      description: response.description,
      ...(response.content && { content: response.content }),
    };
  }

  // Add common error responses if not present
  if (!result['401']) {
    result['401'] = {
      description: 'Authentication required',
      content: {
        'application/json': {
          schema: { $ref: '#/components/schemas/ApiErrorResponse' },
        },
      },
    };
  }

  if (!result['403']) {
    result['403'] = {
      description: 'Permission denied',
      content: {
        'application/json': {
          schema: { $ref: '#/components/schemas/ApiErrorResponse' },
        },
      },
    };
  }

  if (!result['429']) {
    result['429'] = {
      description: 'Rate limit exceeded',
      content: {
        'application/json': {
          schema: { $ref: '#/components/schemas/ApiErrorResponse' },
        },
      },
    };
  }

  if (!result['500']) {
    result['500'] = {
      description: 'Internal server error',
      content: {
        'application/json': {
          schema: { $ref: '#/components/schemas/ApiErrorResponse' },
        },
      },
    };
  }

  return result;
}

/**
 * Generate operation ID from method and path
 */
function generateOperationId(method: string, path: string): string {
  // Convert path to camelCase operation name
  const parts = path
    .split('/')
    .filter((p) => p && !p.startsWith(':'))
    .map((p) => p.charAt(0).toUpperCase() + p.slice(1));

  // Add method prefix
  const prefix = method.toLowerCase();

  // Handle path params
  const pathParams = path.match(/:([a-zA-Z_][a-zA-Z0-9_]*)/g) || [];
  const paramSuffix = pathParams.length > 0 ? 'ById' : '';

  return prefix + parts.join('') + paramSuffix;
}
