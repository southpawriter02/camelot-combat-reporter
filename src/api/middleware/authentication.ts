/**
 * Authentication middleware for API key validation
 */
import type { Middleware, AuthConfig, ApiKeyConfig, ApiPermission } from '../types.js';
import { AuthenticationError, ForbiddenError } from '../errors.js';

/**
 * Create authentication middleware
 */
export function createAuthMiddleware(config: AuthConfig): Middleware {
  return async (req, res, next) => {
    if (!config.enabled) {
      next();
      return;
    }

    // Extract API key from header or query param
    const headerKey = req.headers[config.headerName.toLowerCase()] as string | undefined;
    const queryKey = req.query[config.queryParamName] as string | undefined;
    const apiKey = headerKey || queryKey;

    if (!apiKey) {
      throw new AuthenticationError('API key required');
    }

    // Find matching key
    const keyConfig = config.keys.find((k) => k.key === apiKey && k.enabled);

    if (!keyConfig) {
      throw new AuthenticationError('Invalid API key');
    }

    // Check expiration
    if (keyConfig.expiresAt && keyConfig.expiresAt < new Date()) {
      throw new AuthenticationError('API key expired');
    }

    // Attach to request
    req.apiKey = keyConfig;
    next();
  };
}

/**
 * Permission check middleware factory
 */
export function requirePermission(...permissions: ApiPermission[]): Middleware {
  return (req, _res, next) => {
    const keyConfig = req.apiKey;

    // If no auth is configured, allow access
    if (!keyConfig) {
      next();
      return;
    }

    // Admin has all permissions
    if (keyConfig.permissions.includes('admin')) {
      next();
      return;
    }

    const hasPermission = permissions.some((p) => keyConfig.permissions.includes(p));

    if (!hasPermission) {
      throw new ForbiddenError(
        `Missing required permission: ${permissions.join(' or ')}`
      );
    }

    next();
  };
}

/**
 * Check if an API key has a specific permission
 */
export function hasPermission(
  apiKey: ApiKeyConfig | undefined,
  permission: ApiPermission
): boolean {
  if (!apiKey) return false;
  if (apiKey.permissions.includes('admin')) return true;
  return apiKey.permissions.includes(permission);
}
