/**
 * API module types for camelot-combat-reporter REST API
 */
import type { IncomingMessage, ServerResponse } from 'http';
import type { CombatEvent } from '../types/index.js';
import type { SessionUpdate } from '../analysis/types/session.js';

// ============================================================================
// API Permissions
// ============================================================================

/**
 * Available API permissions
 */
export type ApiPermission =
  | 'read:events'
  | 'read:sessions'
  | 'read:players'
  | 'read:stats'
  | 'read:realtime'
  | 'read:ml'
  | 'admin';

// ============================================================================
// API Key Configuration
// ============================================================================

/**
 * API key configuration
 */
export interface ApiKeyConfig {
  /** The API key string */
  key: string;
  /** Human-readable name for this key */
  name: string;
  /** Permissions granted to this key */
  permissions: ApiPermission[];
  /** Rate limit override (requests per minute) */
  rateLimit?: number;
  /** Key expiration date */
  expiresAt?: Date;
  /** Whether this key is enabled */
  enabled: boolean;
}

// ============================================================================
// Authentication Configuration
// ============================================================================

/**
 * Authentication configuration
 */
export interface AuthConfig {
  /** Enable authentication (default: true) */
  enabled: boolean;
  /** Header name for API key (default: 'X-API-Key') */
  headerName: string;
  /** Query param name for API key (default: 'api_key') */
  queryParamName: string;
  /** API keys */
  keys: ApiKeyConfig[];
}

/**
 * Default authentication configuration
 */
export const DEFAULT_AUTH_CONFIG: AuthConfig = {
  enabled: true,
  headerName: 'X-API-Key',
  queryParamName: 'api_key',
  keys: [],
};

// ============================================================================
// Rate Limiting Configuration
// ============================================================================

/**
 * Rate limiting configuration
 */
export interface RateLimitConfig {
  /** Enable rate limiting (default: true) */
  enabled: boolean;
  /** Requests per window */
  maxRequests: number;
  /** Window size in milliseconds (default: 60000 = 1 minute) */
  windowMs: number;
  /** Custom message when rate limited */
  message?: string;
}

/**
 * Default rate limiting configuration
 */
export const DEFAULT_RATE_LIMIT_CONFIG: RateLimitConfig = {
  enabled: true,
  maxRequests: 100,
  windowMs: 60000,
};

// ============================================================================
// CORS Configuration
// ============================================================================

/**
 * CORS configuration
 */
export interface CorsConfig {
  /** Enable CORS (default: true) */
  enabled: boolean;
  /** Allowed origins (default: ['*']) */
  origins: string[];
  /** Allowed methods */
  methods: string[];
  /** Allowed headers */
  allowedHeaders: string[];
  /** Exposed headers */
  exposedHeaders: string[];
  /** Max age for preflight cache in seconds */
  maxAge: number;
  /** Allow credentials */
  credentials: boolean;
}

/**
 * Default CORS configuration
 */
export const DEFAULT_CORS_CONFIG: CorsConfig = {
  enabled: true,
  origins: ['*'],
  methods: ['GET', 'POST', 'OPTIONS'],
  allowedHeaders: ['Content-Type', 'X-API-Key', 'Authorization'],
  exposedHeaders: ['X-RateLimit-Limit', 'X-RateLimit-Remaining', 'X-RateLimit-Reset'],
  maxAge: 86400,
  credentials: false,
};

// ============================================================================
// SSE Configuration
// ============================================================================

/**
 * Server-Sent Events configuration
 */
export interface SSEConfig {
  /** Enable SSE endpoints (default: true) */
  enabled: boolean;
  /** Heartbeat interval in ms (default: 30000) */
  heartbeatIntervalMs: number;
  /** Max connections per client (default: 5) */
  maxConnectionsPerClient: number;
  /** Connection timeout in ms (default: 0 = no timeout) */
  connectionTimeoutMs: number;
}

/**
 * Default SSE configuration
 */
export const DEFAULT_SSE_CONFIG: SSEConfig = {
  enabled: true,
  heartbeatIntervalMs: 30000,
  maxConnectionsPerClient: 5,
  connectionTimeoutMs: 0,
};

// ============================================================================
// OpenAPI Configuration
// ============================================================================

/**
 * OpenAPI server definition
 */
export interface OpenApiServer {
  /** Server URL */
  url: string;
  /** Server description */
  description?: string;
}

/**
 * OpenAPI documentation configuration
 */
export interface OpenApiConfig {
  /** Enable OpenAPI endpoint (default: true) */
  enabled: boolean;
  /** Path to serve spec (default: '/openapi.json') */
  path: string;
  /** API title */
  title: string;
  /** API version */
  version: string;
  /** API description */
  description: string;
  /** Server URLs for spec */
  servers: OpenApiServer[];
}

/**
 * Default OpenAPI configuration
 */
export const DEFAULT_OPENAPI_CONFIG: OpenApiConfig = {
  enabled: true,
  path: '/openapi.json',
  title: 'Camelot Combat Reporter API',
  version: '1.0.0',
  description: 'REST API for Dark Age of Camelot combat log data',
  servers: [{ url: 'http://localhost:3000/api/v1', description: 'Local server' }],
};

// ============================================================================
// API Server Configuration
// ============================================================================

/**
 * Full API server configuration
 */
export interface ApiServerConfig {
  /** Port to listen on (default: 3000) */
  port: number;
  /** Host to bind to (default: '127.0.0.1') */
  host: string;
  /** Base path prefix (default: '/api/v1') */
  basePath: string;
  /** Authentication config */
  auth: AuthConfig;
  /** Rate limiting config */
  rateLimit: RateLimitConfig;
  /** CORS config */
  cors: CorsConfig;
  /** SSE config */
  sse: SSEConfig;
  /** OpenAPI config */
  openapi: OpenApiConfig;
  /** Request timeout in ms (default: 30000) */
  requestTimeoutMs: number;
  /** Max request body size in bytes (default: 1MB) */
  maxBodySize: number;
  /** Enable request logging (default: false) */
  logging: boolean;
}

/**
 * Default API server configuration
 */
export const DEFAULT_API_SERVER_CONFIG: ApiServerConfig = {
  port: 3000,
  host: '127.0.0.1',
  basePath: '/api/v1',
  auth: DEFAULT_AUTH_CONFIG,
  rateLimit: DEFAULT_RATE_LIMIT_CONFIG,
  cors: DEFAULT_CORS_CONFIG,
  sse: DEFAULT_SSE_CONFIG,
  openapi: DEFAULT_OPENAPI_CONFIG,
  requestTimeoutMs: 30000,
  maxBodySize: 1024 * 1024, // 1MB
  logging: false,
};

// ============================================================================
// Request/Response Types
// ============================================================================

/**
 * Extended request with parsed data
 */
export interface ApiRequest extends IncomingMessage {
  /** Parsed URL pathname */
  pathname: string;
  /** Parsed query parameters */
  query: Record<string, string | string[] | undefined>;
  /** Route parameters (e.g., :id) */
  params: Record<string, string>;
  /** Parsed request body (for POST) */
  body?: unknown;
  /** Authenticated API key config (if auth enabled) */
  apiKey?: ApiKeyConfig;
  /** Client identifier for rate limiting */
  clientId: string;
}

/**
 * Extended response with helpers
 */
export interface ApiResponse extends ServerResponse {
  /** Send JSON response */
  json: (data: unknown, statusCode?: number) => void;
  /** Send error response */
  error: (error: ApiErrorBody) => void;
  /** Set pagination headers */
  paginate: (total: number, limit: number, offset: number) => void;
}

/**
 * API error body structure
 */
export interface ApiErrorBody {
  /** Error status code */
  statusCode: number;
  /** Error code */
  code: string;
  /** Error message */
  message: string;
  /** Additional details */
  details?: unknown;
}

/**
 * Standard API error response
 */
export interface ApiErrorResponse {
  error: {
    code: string;
    message: string;
    details?: unknown;
  };
}

/**
 * Standard API success response wrapper
 */
export interface ApiSuccessResponse<T> {
  data: T;
  meta?: {
    total?: number;
    limit?: number;
    offset?: number;
    hasMore?: boolean;
  };
}

// ============================================================================
// Query Parameters
// ============================================================================

/**
 * Pagination query parameters
 */
export interface PaginationParams {
  limit?: number;
  offset?: number;
}

/**
 * Common filter parameters for events
 */
export interface EventFilterParams extends PaginationParams {
  type?: string | string[];
  startTime?: string; // ISO date
  endTime?: string; // ISO date
  sessionId?: string;
  source?: string;
  target?: string;
  minAmount?: number;
  actionType?: string | string[];
  damageType?: string | string[];
  criticalOnly?: boolean;
  orderBy?: string;
  orderDir?: 'asc' | 'desc';
}

/**
 * Common filter parameters for sessions
 */
export interface SessionFilterParams extends PaginationParams {
  startTime?: string;
  endTime?: string;
  minDuration?: number;
  maxDuration?: number;
  participant?: string;
  minParticipants?: number;
  includeEvents?: boolean;
  includeSummary?: boolean;
  orderBy?: string;
  orderDir?: 'asc' | 'desc';
}

/**
 * Player stats filter parameters
 */
export interface PlayerStatsFilterParams extends PaginationParams {
  startTime?: string;
  endTime?: string;
  role?: string;
  minPerformance?: number;
}

// ============================================================================
// Route Handler Types
// ============================================================================

/**
 * Next function type for middleware
 */
export type NextFunction = () => void | Promise<void>;

/**
 * Route handler function
 */
export type RouteHandler = (
  req: ApiRequest,
  res: ApiResponse
) => Promise<void> | void;

/**
 * Middleware function
 */
export type Middleware = (
  req: ApiRequest,
  res: ApiResponse,
  next: NextFunction
) => Promise<void> | void;

/**
 * HTTP method types
 */
export type HttpMethod = 'GET' | 'POST' | 'PUT' | 'DELETE' | 'OPTIONS' | 'PATCH';

/**
 * Route definition
 */
export interface RouteDefinition {
  /** HTTP method */
  method: HttpMethod;
  /** URL path pattern */
  path: string;
  /** Route handler */
  handler: RouteHandler;
  /** Route-specific middleware */
  middleware?: Middleware[];
  /** Required permissions (if any) */
  permissions?: ApiPermission[];
  /** OpenAPI operation metadata */
  openapi?: OpenApiOperation;
}

// ============================================================================
// OpenAPI Types
// ============================================================================

/**
 * OpenAPI operation metadata for route
 */
export interface OpenApiOperation {
  /** Operation summary */
  summary: string;
  /** Operation description */
  description?: string;
  /** Operation tags */
  tags: string[];
  /** Operation parameters */
  parameters?: OpenApiParameter[];
  /** Request body definition */
  requestBody?: OpenApiRequestBody;
  /** Response definitions */
  responses: Record<string, OpenApiResponseDef>;
}

/**
 * OpenAPI parameter definition
 */
export interface OpenApiParameter {
  /** Parameter name */
  name: string;
  /** Parameter location */
  in: 'query' | 'path' | 'header';
  /** Parameter description */
  description?: string;
  /** Whether parameter is required */
  required?: boolean;
  /** Parameter schema */
  schema: OpenApiSchema;
}

/**
 * OpenAPI request body definition
 */
export interface OpenApiRequestBody {
  /** Description */
  description?: string;
  /** Whether required */
  required?: boolean;
  /** Content types */
  content: Record<string, { schema: OpenApiSchema }>;
}

/**
 * OpenAPI response definition
 */
export interface OpenApiResponseDef {
  /** Response description */
  description: string;
  /** Content types */
  content?: Record<string, { schema: OpenApiSchema }>;
}

/**
 * OpenAPI schema definition
 */
export interface OpenApiSchema {
  /** Schema type */
  type?: string;
  /** Schema format */
  format?: string;
  /** Array items schema */
  items?: OpenApiSchema;
  /** Object properties */
  properties?: Record<string, OpenApiSchema>;
  /** Schema reference */
  $ref?: string;
  /** Enum values */
  enum?: string[];
  /** Property description */
  description?: string;
  /** Required properties */
  required?: string[];
  /** Additional properties */
  additionalProperties?: boolean | OpenApiSchema;
  /** Composition - allOf */
  allOf?: OpenApiSchema[];
  /** Composition - oneOf */
  oneOf?: OpenApiSchema[];
  /** Composition - anyOf */
  anyOf?: OpenApiSchema[];
}

// ============================================================================
// Server State Types
// ============================================================================

/**
 * Server state
 */
export type ServerState = 'stopped' | 'starting' | 'running' | 'stopping' | 'error';

/**
 * Server status
 */
export interface ServerStatus {
  /** Current state */
  state: ServerState;
  /** Uptime in milliseconds */
  uptime: number;
  /** Total request count */
  requestCount: number;
  /** Active HTTP connections */
  activeConnections: number;
  /** Active SSE connections */
  sseConnections: number;
  /** Last error message */
  lastError?: string;
}

// ============================================================================
// SSE Types
// ============================================================================

/**
 * SSE connection info
 */
export interface SSEConnection {
  /** Connection ID */
  id: string;
  /** Client ID */
  clientId: string;
  /** Response object */
  response: ServerResponse;
  /** Subscribed event types */
  eventTypes: string[];
  /** Connection creation time */
  createdAt: Date;
  /** Last activity time */
  lastActivity: Date;
}

/**
 * SSE event data
 */
export interface SSEEventData {
  /** Event type */
  eventType: string;
  /** ISO timestamp */
  timestamp: string;
  /** Event data */
  data: CombatEvent | SessionUpdate | unknown;
  /** Metadata */
  metadata?: {
    filename?: string;
    lineNumber?: number;
    sessionId?: string;
  };
}

// ============================================================================
// Server Events
// ============================================================================

/**
 * Events emitted by ApiServer
 */
export interface ApiServerEvents {
  'server:started': (port: number) => void;
  'server:stopped': () => void;
  'server:error': (error: Error) => void;
  'request:start': (req: ApiRequest) => void;
  'request:end': (req: ApiRequest, statusCode: number, durationMs: number) => void;
  'sse:connect': (clientId: string, eventTypes: string[]) => void;
  'sse:disconnect': (clientId: string) => void;
  'auth:success': (apiKey: ApiKeyConfig, req: ApiRequest) => void;
  'auth:failure': (reason: string, req: ApiRequest) => void;
  'rateLimit:exceeded': (clientId: string, req: ApiRequest) => void;
}
