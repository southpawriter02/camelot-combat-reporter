/**
 * API Middleware Tests
 */
import {
  createAuthMiddleware,
  requirePermission,
  hasPermission,
} from '../../../src/api/middleware/authentication.js';
import { createRateLimitMiddleware } from '../../../src/api/middleware/rateLimit.js';
import { createCorsMiddleware } from '../../../src/api/middleware/cors.js';
import { createErrorHandler, handleError, createErrorResponse } from '../../../src/api/middleware/errorHandler.js';
import {
  ApiError,
  BadRequestError,
  AuthenticationError,
  ForbiddenError,
  NotFoundError,
  RateLimitError,
} from '../../../src/api/errors.js';
import type { AuthConfig, RateLimitConfig, CorsConfig, ApiKeyConfig, ApiPermission } from '../../../src/api/types.js';

// Mock request factory - uses any type for testing flexibility
function createMockRequest(overrides: Record<string, unknown> = {}): any {
  return {
    method: 'GET',
    url: '/test',
    path: '/test',
    query: {},
    params: {},
    headers: {},
    body: null,
    apiKey: null,
    ...overrides,
  };
}

// Mock response factory - uses any type for testing flexibility
function createMockResponse(): any {
  return {
    data: null as unknown,
    statusCode: 200,
    headers: {} as Record<string, string>,
    ended: false,
    status(code: number) {
      this.statusCode = code;
      return this;
    },
    json(data: unknown) {
      this.data = data;
      this.ended = true;
    },
    send(data: unknown) {
      this.data = data;
      this.ended = true;
    },
    setHeader(name: string, value: string) {
      this.headers[name] = value;
    },
    paginate() {},
    error(opts: { statusCode: number; code: string; message: string; details?: unknown }) {
      this.statusCode = opts.statusCode;
      this.data = {
        error: {
          code: opts.code,
          message: opts.message,
          ...(opts.details !== undefined && { details: opts.details }),
        },
      };
      this.headers['Retry-After'] = opts.details && (opts.details as any).retryAfter
        ? String((opts.details as any).retryAfter)
        : undefined;
      this.ended = true;
    },
  };
}

describe('Authentication Middleware', () => {
  const validKey: ApiKeyConfig = {
    key: 'test-key-123',
    name: 'Test Key',
    permissions: ['read:events', 'read:sessions'] as ApiPermission[],
    enabled: true,
  };

  const authConfig: AuthConfig = {
    enabled: true,
    headerName: 'X-API-Key',
    queryParamName: 'api_key',
    keys: [validKey],
  };

  describe('createAuthMiddleware', () => {
    it('should pass when auth is disabled', async () => {
      const middleware = createAuthMiddleware({ ...authConfig, enabled: false });
      const req = createMockRequest();
      const res = createMockResponse();
      const next = jest.fn();

      await middleware(req, res, next);

      expect(next).toHaveBeenCalled();
    });

    it('should authenticate with valid API key in header', async () => {
      const middleware = createAuthMiddleware(authConfig);
      const req = createMockRequest({
        headers: { 'x-api-key': 'test-key-123' },
      });
      const res = createMockResponse();
      const next = jest.fn();

      await middleware(req, res, next);

      expect(next).toHaveBeenCalled();
      expect(req.apiKey).toEqual(validKey);
    });

    it('should authenticate with valid API key in query param', async () => {
      const middleware = createAuthMiddleware(authConfig);
      const req = createMockRequest({
        query: { api_key: 'test-key-123' },
      });
      const res = createMockResponse();
      const next = jest.fn();

      await middleware(req, res, next);

      expect(next).toHaveBeenCalled();
      expect(req.apiKey).toEqual(validKey);
    });

    it('should reject when no API key provided', async () => {
      const middleware = createAuthMiddleware(authConfig);
      const req = createMockRequest();
      const res = createMockResponse();
      const next = jest.fn();

      await expect(middleware(req, res, next)).rejects.toThrow(AuthenticationError);
      expect(next).not.toHaveBeenCalled();
    });

    it('should reject invalid API key', async () => {
      const middleware = createAuthMiddleware(authConfig);
      const req = createMockRequest({
        headers: { 'x-api-key': 'invalid-key' },
      });
      const res = createMockResponse();
      const next = jest.fn();

      await expect(middleware(req, res, next)).rejects.toThrow(AuthenticationError);
      expect(next).not.toHaveBeenCalled();
    });

    it('should reject disabled API key', async () => {
      const configWithDisabledKey: AuthConfig = {
        ...authConfig,
        keys: [{ ...validKey, enabled: false }],
      };
      const middleware = createAuthMiddleware(configWithDisabledKey);
      const req = createMockRequest({
        headers: { 'x-api-key': 'test-key-123' },
      });
      const res = createMockResponse();
      const next = jest.fn();

      await expect(middleware(req, res, next)).rejects.toThrow(AuthenticationError);
    });

    it('should reject expired API key', async () => {
      const configWithExpiredKey: AuthConfig = {
        ...authConfig,
        keys: [{ ...validKey, expiresAt: new Date('2020-01-01') }],
      };
      const middleware = createAuthMiddleware(configWithExpiredKey);
      const req = createMockRequest({
        headers: { 'x-api-key': 'test-key-123' },
      });
      const res = createMockResponse();
      const next = jest.fn();

      await expect(middleware(req, res, next)).rejects.toThrow(AuthenticationError);
    });
  });

  describe('requirePermission', () => {
    it('should pass when key has required permission', async () => {
      const middleware = requirePermission('read:events');
      const req = createMockRequest();
      req.apiKey = validKey;
      const res = createMockResponse();
      const next = jest.fn();

      await middleware(req, res, next);

      expect(next).toHaveBeenCalled();
    });

    it('should reject when key lacks required permission', async () => {
      const middleware = requirePermission('admin');
      const req = createMockRequest();
      req.apiKey = validKey;
      const res = createMockResponse();
      const next = jest.fn();

      // The middleware throws when key lacks permission
      let thrown = false;
      try {
        await middleware(req, res, next);
      } catch (e) {
        thrown = true;
        expect(e).toBeInstanceOf(ForbiddenError);
      }
      expect(thrown).toBe(true);
    });

    it('should allow access when no API key present (auth responsibility of authMiddleware)', async () => {
      const middleware = requirePermission('read:events');
      const req = createMockRequest();
      req.apiKey = null;  // Explicitly null
      const res = createMockResponse();
      const next = jest.fn();

      // When no apiKey, permission check is skipped (authMiddleware handles auth requirement)
      await middleware(req, res, next);
      expect(next).toHaveBeenCalled();
    });

    it('should pass admin for any permission', async () => {
      const middleware = requirePermission('read:events');
      const req = createMockRequest();
      req.apiKey = { ...validKey, permissions: ['admin'] as ApiPermission[] };
      const res = createMockResponse();
      const next = jest.fn();

      await middleware(req, res, next);

      expect(next).toHaveBeenCalled();
    });
  });

  describe('hasPermission', () => {
    it('should return true when key has permission', () => {
      expect(hasPermission(validKey, 'read:events')).toBe(true);
    });

    it('should return false when key lacks permission', () => {
      expect(hasPermission(validKey, 'admin')).toBe(false);
    });

    it('should return true for admin key', () => {
      const adminKey: ApiKeyConfig = { ...validKey, permissions: ['admin'] as ApiPermission[] };
      expect(hasPermission(adminKey, 'read:events')).toBe(true);
    });

    it('should return false for undefined key', () => {
      expect(hasPermission(undefined, 'read:events')).toBe(false);
    });
  });
});

describe('Rate Limiting Middleware', () => {
  const rateLimitConfig: RateLimitConfig = {
    enabled: true,
    windowMs: 60000,
    maxRequests: 5,
  };

  beforeEach(() => {
    // Reset rate limit state between tests
    jest.useFakeTimers();
  });

  afterEach(() => {
    jest.useRealTimers();
  });

  it('should pass when rate limit not exceeded', async () => {
    const middleware = createRateLimitMiddleware(rateLimitConfig);
    const req = createMockRequest();
    const res = createMockResponse();
    const next = jest.fn();

    await middleware(req, res, next);

    expect(next).toHaveBeenCalled();
  });

  it('should pass when rate limiting is disabled', async () => {
    const middleware = createRateLimitMiddleware({ ...rateLimitConfig, enabled: false });
    const req = createMockRequest();
    const res = createMockResponse();
    const next = jest.fn();

    await middleware(req, res, next);

    expect(next).toHaveBeenCalled();
  });

  it('should add rate limit headers when enabled', async () => {
    const middleware = createRateLimitMiddleware(rateLimitConfig);
    const req = createMockRequest();
    const res = createMockResponse();
    const next = jest.fn();

    await middleware(req, res, next);

    expect(res.headers['X-RateLimit-Limit']).toBe('5');
    expect(res.headers['X-RateLimit-Remaining']).toBeDefined();
    expect(res.headers['X-RateLimit-Reset']).toBeDefined();
  });

  it('should reject when rate limit exceeded', async () => {
    const middleware = createRateLimitMiddleware(rateLimitConfig);
    const req = createMockRequest();
    const res = createMockResponse();
    const next = jest.fn();

    // Make 5 requests (should all pass)
    for (let i = 0; i < 5; i++) {
      await middleware(req, res, next);
    }

    // 6th request should be rate limited
    await expect(middleware(req, res, jest.fn())).rejects.toThrow(RateLimitError);
  });

  it('should reset after window expires', async () => {
    const middleware = createRateLimitMiddleware(rateLimitConfig);
    const req = createMockRequest();
    const res = createMockResponse();
    const next = jest.fn();

    // Exhaust rate limit
    for (let i = 0; i < 5; i++) {
      await middleware(req, res, next);
    }

    // Advance time past the window
    jest.advanceTimersByTime(61000);

    // Should work again
    await middleware(req, res, next);
    expect(next).toHaveBeenCalled();
  });
});

describe('CORS Middleware', () => {
  const corsConfig: CorsConfig = {
    enabled: true,
    origins: ['http://localhost:3000', 'https://example.com'],
    methods: ['GET', 'POST', 'PUT', 'DELETE'],
    allowedHeaders: ['Content-Type', 'X-API-Key'],
    exposedHeaders: [],
    credentials: true,
    maxAge: 86400,
  };

  it('should add CORS headers for allowed origin', async () => {
    const middleware = createCorsMiddleware(corsConfig);
    const req = createMockRequest({
      headers: { origin: 'http://localhost:3000' },
    });
    const res = createMockResponse();
    const next = jest.fn();

    await middleware(req, res, next);

    // Non-preflight requests only get the origin and credentials headers
    expect(res.headers['Access-Control-Allow-Origin']).toBe('http://localhost:3000');
    expect(res.headers['Access-Control-Allow-Credentials']).toBe('true');
    expect(next).toHaveBeenCalled();
  });

  it('should add full CORS headers on preflight OPTIONS request', async () => {
    const middleware = createCorsMiddleware(corsConfig);
    const req = createMockRequest({
      method: 'OPTIONS',
      headers: { origin: 'http://localhost:3000' },
    });
    // For OPTIONS we need writeHead and end
    const res = {
      ...createMockResponse(),
      writeHead: jest.fn(),
      end: jest.fn(),
    };
    const next = jest.fn();

    await middleware(req, res, next);

    expect(res.headers['Access-Control-Allow-Origin']).toBe('http://localhost:3000');
    expect(res.headers['Access-Control-Allow-Methods']).toBe('GET, POST, PUT, DELETE');
    expect(res.headers['Access-Control-Allow-Headers']).toBe('Content-Type, X-API-Key');
    expect(res.writeHead).toHaveBeenCalledWith(204);
    expect(res.end).toHaveBeenCalled();
    // next is NOT called for preflight
    expect(next).not.toHaveBeenCalled();
  });

  it('should not add origin header for disallowed origin', async () => {
    const middleware = createCorsMiddleware(corsConfig);
    const req = createMockRequest({
      headers: { origin: 'http://malicious.com' },
    });
    const res = createMockResponse();
    const next = jest.fn();

    await middleware(req, res, next);

    expect(res.headers['Access-Control-Allow-Origin']).toBeUndefined();
    expect(next).toHaveBeenCalled();
  });

  it('should allow all origins when wildcard is used without credentials', async () => {
    // With wildcard AND no credentials, we get '*'
    const wildcardConfig: CorsConfig = { ...corsConfig, origins: ['*'], credentials: false };
    const middleware = createCorsMiddleware(wildcardConfig);
    const req = createMockRequest({
      headers: { origin: 'http://any-origin.com' },
    });
    const res = createMockResponse();
    const next = jest.fn();

    await middleware(req, res, next);

    expect(res.headers['Access-Control-Allow-Origin']).toBe('*');
    expect(next).toHaveBeenCalled();
  });

  it('should reflect origin when wildcard is used with credentials', async () => {
    // With wildcard AND credentials, we reflect the origin
    const wildcardConfig: CorsConfig = { ...corsConfig, origins: ['*'], credentials: true };
    const middleware = createCorsMiddleware(wildcardConfig);
    const req = createMockRequest({
      headers: { origin: 'http://any-origin.com' },
    });
    const res = createMockResponse();
    const next = jest.fn();

    await middleware(req, res, next);

    // With credentials, we reflect the origin, not '*'
    expect(res.headers['Access-Control-Allow-Origin']).toBe('http://any-origin.com');
    expect(next).toHaveBeenCalled();
  });

  it('should pass when CORS is disabled', async () => {
    const middleware = createCorsMiddleware({ ...corsConfig, enabled: false });
    const req = createMockRequest();
    const res = createMockResponse();
    const next = jest.fn();

    await middleware(req, res, next);

    expect(res.headers['Access-Control-Allow-Origin']).toBeUndefined();
    expect(next).toHaveBeenCalled();
  });
});

describe('Error Handler', () => {
  describe('handleError', () => {
    it('should handle ApiError correctly', () => {
      const error = new BadRequestError('Invalid input');
      const res = createMockResponse();

      handleError(error, res);

      expect(res.statusCode).toBe(400);
      expect(res.data).toEqual({
        error: {
          code: 'BAD_REQUEST',
          message: 'Invalid input',
        },
      });
    });

    it('should handle NotFoundError correctly', () => {
      const error = new NotFoundError('Session', 'abc123');
      const res = createMockResponse();

      handleError(error, res);

      expect(res.statusCode).toBe(404);
      expect(res.data).toEqual({
        error: {
          code: 'NOT_FOUND',
          message: "Session with id 'abc123' not found",
        },
      });
    });

    it('should handle RateLimitError correctly', () => {
      const error = new RateLimitError(60);
      const res = createMockResponse();

      handleError(error, res);

      expect(res.statusCode).toBe(429);
      expect(res.headers['Retry-After']).toBe('60');
    });

    it('should handle unknown errors as 500', () => {
      const error = new Error('Something went wrong');
      const res = createMockResponse();

      handleError(error, res);

      expect(res.statusCode).toBe(500);
      expect(res.data).toEqual({
        error: {
          code: 'INTERNAL_ERROR',
          message: 'Something went wrong',
        },
      });
    });
  });

  describe('createErrorResponse', () => {
    it('should create error response object from ApiError', () => {
      const error = new BadRequestError('Test message');
      const response = createErrorResponse(error);

      expect(response).toEqual({
        statusCode: 400,
        code: 'BAD_REQUEST',
        message: 'Test message',
        details: undefined,
      });
    });

    it('should include details when provided', () => {
      const error = new ApiError('Test message', 400, 'TEST_ERROR', { field: 'value' });
      const response = createErrorResponse(error);

      expect(response).toEqual({
        statusCode: 400,
        code: 'TEST_ERROR',
        message: 'Test message',
        details: { field: 'value' },
      });
    });
  });

  describe('createErrorHandler', () => {
    it('should return error handling middleware', () => {
      const errorHandler = createErrorHandler();
      expect(typeof errorHandler).toBe('function');
    });
  });
});
