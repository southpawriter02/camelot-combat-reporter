/**
 * Error handling middleware and utilities
 */
import type { Middleware, ApiResponse, ApiErrorBody } from '../types.js';
import { ApiError, InternalError, wrapError } from '../errors.js';

/**
 * Create error handling middleware
 *
 * This is a wrapper for route handlers that catches errors
 * and converts them to proper API error responses.
 */
export function createErrorHandler(): Middleware {
  return async (req, res, next) => {
    try {
      next();
    } catch (error) {
      handleError(error, res);
    }
  };
}

/**
 * Handle an error and send appropriate response
 */
export function handleError(error: unknown, res: ApiResponse): void {
  const apiError = wrapError(error);

  // Log internal errors
  if (apiError.statusCode >= 500) {
    console.error('Internal server error:', error);
  }

  // Send error response
  res.error({
    statusCode: apiError.statusCode,
    code: apiError.code,
    message: apiError.message,
    details: apiError.details,
  });
}

/**
 * Wrap a route handler with error handling
 */
export function withErrorHandling(
  handler: (req: any, res: ApiResponse) => Promise<void> | void
): (req: any, res: ApiResponse) => Promise<void> {
  return async (req, res) => {
    try {
      await handler(req, res);
    } catch (error) {
      handleError(error, res);
    }
  };
}

/**
 * Create a standardized error response body
 */
export function createErrorResponse(error: ApiError): ApiErrorBody {
  return {
    statusCode: error.statusCode,
    code: error.code,
    message: error.message,
    details: error.details,
  };
}
