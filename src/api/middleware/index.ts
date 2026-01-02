/**
 * Middleware exports
 */
export { createAuthMiddleware, requirePermission, hasPermission } from './authentication.js';
export { createRateLimitMiddleware } from './rateLimit.js';
export { createCorsMiddleware } from './cors.js';
export {
  createErrorHandler,
  handleError,
  withErrorHandling,
  createErrorResponse,
} from './errorHandler.js';
