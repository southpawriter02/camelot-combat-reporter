/**
 * API module exports
 */

// Main server
export { ApiServer, SSEManager } from './ApiServer.js';

// Types
export type {
  // Configuration
  ApiServerConfig,
  AuthConfig,
  RateLimitConfig,
  CorsConfig,
  SSEConfig,
  OpenApiConfig,
  OpenApiServer,
  ApiKeyConfig,
  ApiPermission,
  // Request/Response
  ApiRequest,
  ApiResponse,
  ApiErrorBody,
  ApiErrorResponse,
  ApiSuccessResponse,
  // Query parameters
  PaginationParams,
  EventFilterParams,
  SessionFilterParams,
  PlayerStatsFilterParams,
  // Route types
  RouteDefinition,
  RouteHandler,
  Middleware,
  NextFunction,
  HttpMethod,
  // OpenAPI types
  OpenApiOperation,
  OpenApiParameter,
  OpenApiRequestBody,
  OpenApiResponseDef,
  OpenApiSchema,
  // Server state
  ServerState,
  ServerStatus,
  // SSE types
  SSEConnection,
  SSEEventData,
  // Events
  ApiServerEvents,
} from './types.js';

// Default configs
export {
  DEFAULT_API_SERVER_CONFIG,
  DEFAULT_AUTH_CONFIG,
  DEFAULT_RATE_LIMIT_CONFIG,
  DEFAULT_CORS_CONFIG,
  DEFAULT_SSE_CONFIG,
  DEFAULT_OPENAPI_CONFIG,
} from './types.js';

// Errors
export {
  ApiError,
  BadRequestError,
  AuthenticationError,
  ForbiddenError,
  NotFoundError,
  MethodNotAllowedError,
  ConflictError,
  ValidationError,
  RateLimitError,
  InternalError,
  ServiceUnavailableError,
  isApiError,
  wrapError,
} from './errors.js';

// Middleware
export {
  createAuthMiddleware,
  requirePermission,
  hasPermission,
} from './middleware/authentication.js';
export { createRateLimitMiddleware } from './middleware/rateLimit.js';
export { createCorsMiddleware } from './middleware/cors.js';
export {
  createErrorHandler,
  handleError,
  withErrorHandling,
} from './middleware/errorHandler.js';

// OpenAPI
export { generateOpenApiSpec, getSchemas } from './openapi/index.js';

// Routes (for custom route registration)
export { createMLRoutes } from './routes/ml.js';
