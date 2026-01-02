/**
 * API Error Tests
 */
import {
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
} from '../../../src/api/errors.js';

describe('API Errors', () => {
  describe('ApiError', () => {
    it('should create error with correct properties', () => {
      const error = new ApiError('Test message', 400, 'BAD_REQUEST', { field: 'value' });

      expect(error).toBeInstanceOf(Error);
      expect(error).toBeInstanceOf(ApiError);
      expect(error.statusCode).toBe(400);
      expect(error.code).toBe('BAD_REQUEST');
      expect(error.message).toBe('Test message');
      expect(error.details).toEqual({ field: 'value' });
      expect(error.name).toBe('ApiError');
    });

    it('should work without details', () => {
      const error = new ApiError('Test', 500, 'ERROR');

      expect(error.details).toBeUndefined();
    });

    it('should convert to JSON response format', () => {
      const error = new ApiError('Test message', 400, 'BAD_REQUEST', { field: 'value' });
      const json = error.toJSON();

      expect(json).toEqual({
        error: {
          code: 'BAD_REQUEST',
          message: 'Test message',
          details: { field: 'value' },
        },
      });
    });

    it('should omit details in JSON when undefined', () => {
      const error = new ApiError('Test', 500, 'ERROR');
      const json = error.toJSON();

      expect(json).toEqual({
        error: {
          code: 'ERROR',
          message: 'Test',
        },
      });
    });
  });

  describe('BadRequestError', () => {
    it('should create 400 error', () => {
      const error = new BadRequestError('Invalid input');

      expect(error.statusCode).toBe(400);
      expect(error.code).toBe('BAD_REQUEST');
      expect(error.message).toBe('Invalid input');
    });

    it('should accept optional details', () => {
      const error = new BadRequestError('Invalid input', { field: 'email' });

      expect(error.details).toEqual({ field: 'email' });
    });
  });

  describe('AuthenticationError', () => {
    it('should create 401 error', () => {
      const error = new AuthenticationError('Invalid token');

      expect(error.statusCode).toBe(401);
      expect(error.code).toBe('UNAUTHORIZED');
      expect(error.message).toBe('Invalid token');
    });

    it('should use default message', () => {
      const error = new AuthenticationError();

      expect(error.message).toBe('Authentication required');
    });
  });

  describe('ForbiddenError', () => {
    it('should create 403 error', () => {
      const error = new ForbiddenError('Access denied');

      expect(error.statusCode).toBe(403);
      expect(error.code).toBe('FORBIDDEN');
      expect(error.message).toBe('Access denied');
    });

    it('should use default message', () => {
      const error = new ForbiddenError();

      expect(error.message).toBe('Access denied');
    });
  });

  describe('NotFoundError', () => {
    it('should create 404 error with resource type and id', () => {
      const error = new NotFoundError('Session', 'abc123');

      expect(error.statusCode).toBe(404);
      expect(error.code).toBe('NOT_FOUND');
      expect(error.message).toBe("Session with id 'abc123' not found");
    });

    it('should create 404 error with just resource type', () => {
      const error = new NotFoundError('Resource');

      expect(error.message).toBe('Resource not found');
    });
  });

  describe('MethodNotAllowedError', () => {
    it('should create 405 error', () => {
      const error = new MethodNotAllowedError('POST', '/api/v1/events');

      expect(error.statusCode).toBe(405);
      expect(error.code).toBe('METHOD_NOT_ALLOWED');
      expect(error.message).toBe('Method POST not allowed for /api/v1/events');
    });
  });

  describe('ConflictError', () => {
    it('should create 409 error', () => {
      const error = new ConflictError('Resource already exists');

      expect(error.statusCode).toBe(409);
      expect(error.code).toBe('CONFLICT');
      expect(error.message).toBe('Resource already exists');
    });

    it('should accept optional details', () => {
      const error = new ConflictError('Resource exists', { id: 'abc123' });

      expect(error.details).toEqual({ id: 'abc123' });
    });
  });

  describe('ValidationError', () => {
    it('should create 422 error', () => {
      const error = new ValidationError('Validation failed');

      expect(error.statusCode).toBe(422);
      expect(error.code).toBe('VALIDATION_ERROR');
      expect(error.message).toBe('Validation failed');
    });

    it('should accept optional details', () => {
      const validationErrors = [
        { field: 'email', message: 'Invalid email format' },
        { field: 'name', message: 'Name is required' },
      ];
      const error = new ValidationError('Validation failed', { errors: validationErrors });

      expect(error.details).toEqual({ errors: validationErrors });
    });
  });

  describe('RateLimitError', () => {
    it('should create 429 error', () => {
      const error = new RateLimitError(60);

      expect(error.statusCode).toBe(429);
      expect(error.code).toBe('RATE_LIMITED');
      expect(error.message).toBe('Rate limit exceeded');
      expect(error.retryAfter).toBe(60);
      expect(error.details).toEqual({ retryAfter: 60 });
    });
  });

  describe('InternalError', () => {
    it('should create 500 error', () => {
      const error = new InternalError('Database error');

      expect(error.statusCode).toBe(500);
      expect(error.code).toBe('INTERNAL_ERROR');
      expect(error.message).toBe('Database error');
    });

    it('should use default message', () => {
      const error = new InternalError();

      expect(error.message).toBe('Internal server error');
    });
  });

  describe('ServiceUnavailableError', () => {
    it('should create 503 error', () => {
      const error = new ServiceUnavailableError('Maintenance');

      expect(error.statusCode).toBe(503);
      expect(error.code).toBe('SERVICE_UNAVAILABLE');
      expect(error.message).toBe('Maintenance');
    });

    it('should use default message', () => {
      const error = new ServiceUnavailableError();

      expect(error.message).toBe('Service temporarily unavailable');
    });
  });

  describe('isApiError', () => {
    it('should return true for ApiError instances', () => {
      expect(isApiError(new ApiError('Test', 400, 'TEST'))).toBe(true);
      expect(isApiError(new BadRequestError('Test'))).toBe(true);
      expect(isApiError(new NotFoundError('Test'))).toBe(true);
    });

    it('should return false for non-ApiError', () => {
      expect(isApiError(new Error('Test'))).toBe(false);
      expect(isApiError(null)).toBe(false);
      expect(isApiError(undefined)).toBe(false);
      expect(isApiError('error')).toBe(false);
      expect(isApiError({ statusCode: 400 })).toBe(false);
    });
  });

  describe('wrapError', () => {
    it('should return ApiError as-is', () => {
      const original = new BadRequestError('Test');
      const wrapped = wrapError(original);

      expect(wrapped).toBe(original);
    });

    it('should wrap regular Error as InternalError', () => {
      const original = new Error('Something failed');
      const wrapped = wrapError(original);

      expect(wrapped).toBeInstanceOf(InternalError);
      expect(wrapped.message).toBe('Something failed');
    });

    it('should wrap unknown values as InternalError', () => {
      const wrapped = wrapError('string error');

      expect(wrapped).toBeInstanceOf(InternalError);
      expect(wrapped.message).toBe('string error');
    });

    it('should handle null/undefined', () => {
      expect(wrapError(null)).toBeInstanceOf(InternalError);
      expect(wrapError(undefined)).toBeInstanceOf(InternalError);
    });
  });
});
