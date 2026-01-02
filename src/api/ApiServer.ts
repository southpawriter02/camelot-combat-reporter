/**
 * ApiServer - Embeddable REST API server for combat log data
 *
 * Provides HTTP endpoints for querying events, sessions, and player statistics
 * with optional real-time streaming via Server-Sent Events.
 */
import { EventEmitter } from 'events';
import * as http from 'http';
import { URL } from 'url';
import type { DatabaseAdapter } from '../database/adapters/DatabaseAdapter.js';
import type { RealTimeMonitor } from '../streaming/RealTimeMonitor.js';
import type {
  ApiServerConfig,
  ApiServerEvents,
  ApiRequest,
  ApiResponse,
  RouteDefinition,
  ServerState,
  ServerStatus,
  Middleware,
  ApiKeyConfig,
  HttpMethod,
  SSEConfig,
  SSEConnection,
} from './types.js';
import { DEFAULT_API_SERVER_CONFIG } from './types.js';
import { ApiError, NotFoundError, MethodNotAllowedError, wrapError } from './errors.js';
import { createAuthMiddleware, requirePermission } from './middleware/authentication.js';
import { createRateLimitMiddleware } from './middleware/rateLimit.js';
import { createCorsMiddleware } from './middleware/cors.js';
import { createErrorHandler } from './middleware/errorHandler.js';
import { createHealthRoutes } from './routes/health.js';
import { createEventRoutes } from './routes/events.js';
import { createSessionRoutes } from './routes/sessions.js';
import { createPlayerRoutes } from './routes/players.js';
import { createStatsRoutes } from './routes/stats.js';
import { createRealtimeRoutes } from './routes/realtime.js';
import { createMLRoutes } from './routes/ml.js';
import { generateOpenApiSpec } from './openapi/generator.js';
import { MLPredictor } from '../ml/index.js';

/**
 * SSE Manager for handling Server-Sent Event connections
 */
export class SSEManager {
  private connections: Map<string, SSEConnection> = new Map();
  private config: SSEConfig;
  private heartbeatIntervals: Map<string, NodeJS.Timeout> = new Map();

  constructor(config: SSEConfig) {
    this.config = config;
  }

  /**
   * Add a new SSE connection
   */
  addConnection(
    clientId: string,
    res: http.ServerResponse,
    eventTypes: string[]
  ): string {
    const connectionId = `${clientId}_${Date.now()}`;

    // Setup SSE headers
    res.writeHead(200, {
      'Content-Type': 'text/event-stream',
      'Cache-Control': 'no-cache',
      Connection: 'keep-alive',
      'X-Accel-Buffering': 'no', // Disable nginx buffering
    });

    const connection: SSEConnection = {
      id: connectionId,
      clientId,
      response: res,
      eventTypes,
      createdAt: new Date(),
      lastActivity: new Date(),
    };

    this.connections.set(connectionId, connection);

    // Setup heartbeat
    const heartbeatInterval = setInterval(() => {
      this.sendHeartbeat(connectionId);
    }, this.config.heartbeatIntervalMs);

    this.heartbeatIntervals.set(connectionId, heartbeatInterval);

    // Handle disconnect
    res.on('close', () => {
      const interval = this.heartbeatIntervals.get(connectionId);
      if (interval) {
        clearInterval(interval);
        this.heartbeatIntervals.delete(connectionId);
      }
      this.connections.delete(connectionId);
    });

    // Send initial connection event
    res.write(`event: connected\ndata: ${JSON.stringify({ connectionId })}\n\n`);

    return connectionId;
  }

  /**
   * Broadcast event to all matching connections
   */
  broadcast(eventType: string, data: unknown): void {
    const eventString = `event: ${eventType}\ndata: ${JSON.stringify(data)}\n\n`;

    for (const connection of this.connections.values()) {
      if (
        connection.eventTypes.length === 0 ||
        connection.eventTypes.includes(eventType)
      ) {
        try {
          connection.response.write(eventString);
          connection.lastActivity = new Date();
        } catch {
          // Connection may be closed, will be cleaned up on next heartbeat
        }
      }
    }
  }

  /**
   * Send heartbeat to keep connection alive
   */
  private sendHeartbeat(connectionId: string): void {
    const connection = this.connections.get(connectionId);
    if (connection) {
      try {
        connection.response.write(
          `event: heartbeat\ndata: ${JSON.stringify({ timestamp: new Date().toISOString() })}\n\n`
        );
      } catch {
        // Connection closed, clean up
        const interval = this.heartbeatIntervals.get(connectionId);
        if (interval) {
          clearInterval(interval);
          this.heartbeatIntervals.delete(connectionId);
        }
        this.connections.delete(connectionId);
      }
    }
  }

  /**
   * Close all connections
   */
  closeAll(): void {
    for (const interval of this.heartbeatIntervals.values()) {
      clearInterval(interval);
    }
    this.heartbeatIntervals.clear();

    for (const connection of this.connections.values()) {
      try {
        connection.response.end();
      } catch {
        // Ignore errors during shutdown
      }
    }
    this.connections.clear();
  }

  /**
   * Get connection count
   */
  getConnectionCount(): number {
    return this.connections.size;
  }

  /**
   * Get connections by client ID
   */
  getConnectionsByClient(clientId: string): SSEConnection[] {
    return Array.from(this.connections.values()).filter(
      (c) => c.clientId === clientId
    );
  }
}

/**
 * Route registry for pattern matching
 */
interface CompiledRoute {
  definition: RouteDefinition;
  regex: RegExp;
  paramNames: string[];
}

/**
 * ApiServer provides an embeddable REST API for combat log data
 */
export class ApiServer extends EventEmitter {
  private config: ApiServerConfig;
  private database: DatabaseAdapter;
  private monitor: RealTimeMonitor | null = null;
  private server: http.Server | null = null;
  private state: ServerState = 'stopped';
  private routes: RouteDefinition[] = [];
  private compiledRoutes: CompiledRoute[] = [];
  private globalMiddleware: Middleware[] = [];
  private sseManager: SSEManager;
  private mlPredictor: MLPredictor;

  // Stats
  private startedAt: Date | null = null;
  private requestCount: number = 0;
  private lastError: string | undefined;

  constructor(database: DatabaseAdapter, config: Partial<ApiServerConfig> = {}) {
    super();
    this.config = this.mergeConfig(DEFAULT_API_SERVER_CONFIG, config);
    this.database = database;
    this.sseManager = new SSEManager(this.config.sse);
    this.mlPredictor = new MLPredictor();

    this.setupMiddleware();
    this.setupRoutes();
  }

  /**
   * Deep merge configuration
   */
  private mergeConfig(
    defaults: ApiServerConfig,
    overrides: Partial<ApiServerConfig>
  ): ApiServerConfig {
    return {
      ...defaults,
      ...overrides,
      auth: { ...defaults.auth, ...overrides.auth },
      rateLimit: { ...defaults.rateLimit, ...overrides.rateLimit },
      cors: { ...defaults.cors, ...overrides.cors },
      sse: { ...defaults.sse, ...overrides.sse },
      openapi: { ...defaults.openapi, ...overrides.openapi },
    };
  }

  /**
   * Setup global middleware
   */
  private setupMiddleware(): void {
    // CORS must come first
    if (this.config.cors.enabled) {
      this.globalMiddleware.push(createCorsMiddleware(this.config.cors));
    }

    // Authentication
    if (this.config.auth.enabled) {
      this.globalMiddleware.push(createAuthMiddleware(this.config.auth));
    }

    // Rate limiting
    if (this.config.rateLimit.enabled) {
      this.globalMiddleware.push(createRateLimitMiddleware(this.config.rateLimit));
    }
  }

  /**
   * Setup routes
   */
  private setupRoutes(): void {
    // Health routes (no auth required)
    this.routes.push(...createHealthRoutes(this));

    // Event routes
    this.routes.push(...createEventRoutes(this.database, requirePermission('read:events')));

    // Session routes
    this.routes.push(...createSessionRoutes(this.database, requirePermission('read:sessions')));

    // Player routes
    this.routes.push(...createPlayerRoutes(this.database, requirePermission('read:players')));

    // Stats routes
    this.routes.push(...createStatsRoutes(this.database, requirePermission('read:stats')));

    // Real-time routes
    this.routes.push(
      ...createRealtimeRoutes(this.sseManager, this, requirePermission('read:realtime'))
    );

    // ML routes
    this.routes.push(
      ...createMLRoutes(this.database, requirePermission('read:ml'), this.mlPredictor)
    );

    // OpenAPI route
    if (this.config.openapi.enabled) {
      this.routes.push({
        method: 'GET',
        path: this.config.openapi.path,
        handler: (_req, res) => {
          res.json(this.getOpenApiSpec());
        },
        openapi: {
          summary: 'Get OpenAPI specification',
          tags: ['Documentation'],
          responses: {
            '200': { description: 'OpenAPI specification' },
          },
        },
      });
    }

    // Compile routes for efficient matching
    this.compileRoutes();
  }

  /**
   * Compile routes to regex patterns
   */
  private compileRoutes(): void {
    this.compiledRoutes = this.routes.map((route) => {
      const paramNames: string[] = [];
      // Convert :param to regex capture groups
      const regexStr = route.path
        .replace(/:[a-zA-Z_][a-zA-Z0-9_]*/g, (match) => {
          paramNames.push(match.slice(1));
          return '([^/]+)';
        })
        .replace(/\//g, '\\/');

      return {
        definition: route,
        regex: new RegExp(`^${regexStr}$`),
        paramNames,
      };
    });
  }

  /**
   * Attach a RealTimeMonitor for live event streaming
   */
  attachMonitor(monitor: RealTimeMonitor): void {
    this.monitor = monitor;
    this.setupMonitorEventForwarding();
  }

  /**
   * Detach the RealTimeMonitor
   */
  detachMonitor(): void {
    if (this.monitor) {
      // Remove listeners would go here if needed
      this.monitor = null;
    }
  }

  /**
   * Get the attached monitor
   */
  getMonitor(): RealTimeMonitor | null {
    return this.monitor;
  }

  /**
   * Setup event forwarding from monitor to SSE
   */
  private setupMonitorEventForwarding(): void {
    if (!this.monitor) return;

    this.monitor.on('event', (event) => {
      this.sseManager.broadcast('combat_event', {
        eventType: event.eventType,
        timestamp: new Date().toISOString(),
        data: event,
      });
    });

    this.monitor.on('session:start', (update) => {
      this.sseManager.broadcast('session_start', {
        timestamp: new Date().toISOString(),
        data: update,
      });
    });

    this.monitor.on('session:update', (update) => {
      this.sseManager.broadcast('session_update', {
        timestamp: new Date().toISOString(),
        data: update,
      });
    });

    this.monitor.on('session:end', (update) => {
      this.sseManager.broadcast('session_end', {
        timestamp: new Date().toISOString(),
        data: update,
      });
    });
  }

  /**
   * Start the API server
   */
  async start(): Promise<void> {
    if (this.state !== 'stopped') {
      throw new Error(`Cannot start: server is ${this.state}`);
    }

    this.state = 'starting';

    // Ensure database is connected
    if (!this.database.isConnected()) {
      await this.database.connect();
    }

    return new Promise((resolve, reject) => {
      this.server = http.createServer((req, res) => {
        this.handleRequest(req, res);
      });

      this.server.on('error', (error) => {
        this.state = 'error';
        this.lastError = error.message;
        this.emit('server:error', error);
        reject(error);
      });

      this.server.listen(this.config.port, this.config.host, () => {
        this.state = 'running';
        this.startedAt = new Date();
        this.emit('server:started', this.config.port);
        resolve();
      });
    });
  }

  /**
   * Stop the API server
   */
  async stop(): Promise<void> {
    if (this.state !== 'running') {
      return;
    }

    this.state = 'stopping';

    // Close all SSE connections
    this.sseManager.closeAll();

    return new Promise((resolve) => {
      this.server?.close(() => {
        this.state = 'stopped';
        this.server = null;
        this.startedAt = null;
        this.emit('server:stopped');
        resolve();
      });
    });
  }

  /**
   * Get server status
   */
  getStatus(): ServerStatus {
    return {
      state: this.state,
      uptime: this.startedAt ? Date.now() - this.startedAt.getTime() : 0,
      requestCount: this.requestCount,
      activeConnections: 0, // Would need tracking
      sseConnections: this.sseManager.getConnectionCount(),
      lastError: this.lastError,
    };
  }

  /**
   * Get server configuration
   */
  getConfig(): ApiServerConfig {
    return this.config;
  }

  /**
   * Get database adapter
   */
  getDatabase(): DatabaseAdapter {
    return this.database;
  }

  /**
   * Get ML predictor
   */
  getMLPredictor(): MLPredictor {
    return this.mlPredictor;
  }

  /**
   * Add an API key
   */
  addApiKey(keyConfig: Omit<ApiKeyConfig, 'enabled'> & { enabled?: boolean }): void {
    this.config.auth.keys.push({ ...keyConfig, enabled: keyConfig.enabled ?? true });
  }

  /**
   * Remove an API key
   */
  removeApiKey(key: string): boolean {
    const index = this.config.auth.keys.findIndex((k) => k.key === key);
    if (index >= 0) {
      this.config.auth.keys.splice(index, 1);
      return true;
    }
    return false;
  }

  /**
   * Get the OpenAPI specification
   */
  getOpenApiSpec(): object {
    return generateOpenApiSpec(this.routes, this.config.openapi);
  }

  /**
   * Handle incoming HTTP request
   */
  private handleRequest(req: http.IncomingMessage, res: http.ServerResponse): void {
    const startTime = Date.now();
    this.requestCount++;

    // Parse URL
    const baseUrl = `http://${req.headers.host || 'localhost'}`;
    const parsedUrl = new URL(req.url || '/', baseUrl);

    // Create enhanced request
    const apiReq = req as ApiRequest;
    apiReq.pathname = parsedUrl.pathname;
    apiReq.query = this.parseQuery(parsedUrl.searchParams);
    apiReq.params = {};
    apiReq.clientId = this.getClientId(req);

    // Create enhanced response
    const apiRes = res as ApiResponse;
    this.enhanceResponse(apiRes);

    // Emit request start
    this.emit('request:start', apiReq);

    // Handle request completion
    res.on('finish', () => {
      const duration = Date.now() - startTime;
      this.emit('request:end', apiReq, res.statusCode, duration);

      if (this.config.logging) {
        console.log(
          `${apiReq.method} ${apiReq.pathname} - ${res.statusCode} (${duration}ms)`
        );
      }
    });

    // Process request
    this.processRequest(apiReq, apiRes).catch((error) => {
      const apiError = wrapError(error);
      if (!res.headersSent) {
        apiRes.error({
          statusCode: apiError.statusCode,
          code: apiError.code,
          message: apiError.message,
          details: apiError.details,
        });
      }
    });
  }

  /**
   * Process request through middleware and routes
   */
  private async processRequest(req: ApiRequest, res: ApiResponse): Promise<void> {
    // Remove base path from pathname for routing
    let routePath = req.pathname;
    if (routePath.startsWith(this.config.basePath)) {
      routePath = routePath.slice(this.config.basePath.length) || '/';
    }

    // Handle OPTIONS preflight
    if (req.method === 'OPTIONS') {
      res.writeHead(204);
      res.end();
      return;
    }

    // Run global middleware
    try {
      await this.runMiddleware(this.globalMiddleware, req, res);
    } catch (error) {
      if (error instanceof ApiError) {
        res.error({
          statusCode: error.statusCode,
          code: error.code,
          message: error.message,
          details: error.details,
        });
        return;
      }
      throw error;
    }

    // Find matching route
    const match = this.findRoute(req.method as HttpMethod, routePath);

    if (!match) {
      // Check if path exists with different method
      const pathExists = this.compiledRoutes.some((r) => r.regex.test(routePath));
      if (pathExists) {
        throw new MethodNotAllowedError(req.method || 'UNKNOWN', routePath);
      }
      throw new NotFoundError('Route', routePath);
    }

    // Extract route params
    req.params = match.params;

    // Run route-specific middleware
    const routeMiddleware = match.route.definition.middleware || [];
    try {
      await this.runMiddleware(routeMiddleware, req, res);
    } catch (error) {
      if (error instanceof ApiError) {
        res.error({
          statusCode: error.statusCode,
          code: error.code,
          message: error.message,
          details: error.details,
        });
        return;
      }
      throw error;
    }

    // Run route handler
    await match.route.definition.handler(req, res);
  }

  /**
   * Run middleware chain
   */
  private async runMiddleware(
    middleware: Middleware[],
    req: ApiRequest,
    res: ApiResponse
  ): Promise<void> {
    for (const mw of middleware) {
      let nextCalled = false;
      await mw(req, res, () => {
        nextCalled = true;
      });
      if (!nextCalled) {
        // Middleware didn't call next, stop chain
        break;
      }
    }
  }

  /**
   * Find matching route
   */
  private findRoute(
    method: HttpMethod,
    path: string
  ): { route: CompiledRoute; params: Record<string, string> } | null {
    for (const route of this.compiledRoutes) {
      if (route.definition.method !== method) continue;

      const match = route.regex.exec(path);
      if (match) {
        const params: Record<string, string> = {};
        route.paramNames.forEach((name, index) => {
          const value = match[index + 1];
          params[name] = value ? decodeURIComponent(value) : '';
        });
        return { route, params };
      }
    }
    return null;
  }

  /**
   * Parse query parameters
   */
  private parseQuery(
    searchParams: URLSearchParams
  ): Record<string, string | string[] | undefined> {
    const query: Record<string, string | string[] | undefined> = {};

    searchParams.forEach((value, key) => {
      const existing = query[key];
      if (existing === undefined) {
        query[key] = value;
      } else if (Array.isArray(existing)) {
        existing.push(value);
      } else {
        query[key] = [existing, value];
      }
    });

    return query;
  }

  /**
   * Get client identifier for rate limiting
   */
  private getClientId(req: http.IncomingMessage): string {
    // Use forwarded IP if available
    const forwarded = req.headers['x-forwarded-for'];
    if (forwarded) {
      const ip = Array.isArray(forwarded) ? forwarded[0] : forwarded.split(',')[0];
      return ip?.trim() || 'unknown';
    }
    return req.socket.remoteAddress || 'unknown';
  }

  /**
   * Enhance response with helper methods
   */
  private enhanceResponse(res: ApiResponse): void {
    res.json = (data: unknown, statusCode: number = 200) => {
      res.writeHead(statusCode, { 'Content-Type': 'application/json' });
      res.end(JSON.stringify(data));
    };

    res.error = (error) => {
      res.writeHead(error.statusCode, { 'Content-Type': 'application/json' });
      res.end(
        JSON.stringify({
          error: {
            code: error.code,
            message: error.message,
            ...(error.details !== undefined && { details: error.details }),
          },
        })
      );
    };

    res.paginate = (total: number, limit: number, offset: number) => {
      res.setHeader('X-Total-Count', total.toString());
      res.setHeader('X-Limit', limit.toString());
      res.setHeader('X-Offset', offset.toString());
    };
  }
}

// Type augmentation for EventEmitter
export interface ApiServer {
  on<K extends keyof ApiServerEvents>(
    event: K,
    listener: ApiServerEvents[K]
  ): this;
  emit<K extends keyof ApiServerEvents>(
    event: K,
    ...args: Parameters<ApiServerEvents[K]>
  ): boolean;
}
