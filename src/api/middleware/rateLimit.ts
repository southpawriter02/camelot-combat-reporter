/**
 * Rate limiting middleware
 */
import type { Middleware, RateLimitConfig, ApiRequest } from '../types.js';
import { RateLimitError } from '../errors.js';

/**
 * Rate limit entry for a client
 */
interface RateLimitEntry {
  /** Request count in current window */
  count: number;
  /** Window start timestamp */
  windowStart: number;
}

/**
 * Create rate limiting middleware
 */
export function createRateLimitMiddleware(config: RateLimitConfig): Middleware {
  const clients: Map<string, RateLimitEntry> = new Map();

  // Cleanup old entries periodically
  const cleanupInterval = setInterval(() => {
    const now = Date.now();
    for (const [clientId, entry] of clients.entries()) {
      if (now - entry.windowStart > config.windowMs * 2) {
        clients.delete(clientId);
      }
    }
  }, config.windowMs);

  // Prevent interval from keeping process alive
  cleanupInterval.unref?.();

  return async (req, res, next) => {
    if (!config.enabled) {
      next();
      return;
    }

    const clientId = getClientId(req);
    const now = Date.now();

    // Get or create entry
    let entry = clients.get(clientId);
    if (!entry || now - entry.windowStart > config.windowMs) {
      // New window
      entry = { count: 0, windowStart: now };
      clients.set(clientId, entry);
    }

    // Check limit (use per-key limit if available)
    const limit = req.apiKey?.rateLimit ?? config.maxRequests;

    if (entry.count >= limit) {
      const resetTime = entry.windowStart + config.windowMs;
      const retryAfter = Math.ceil((resetTime - now) / 1000);

      // Set rate limit headers
      res.setHeader('X-RateLimit-Limit', limit.toString());
      res.setHeader('X-RateLimit-Remaining', '0');
      res.setHeader('X-RateLimit-Reset', Math.ceil(resetTime / 1000).toString());
      res.setHeader('Retry-After', retryAfter.toString());

      throw new RateLimitError(retryAfter);
    }

    // Increment counter
    entry.count++;

    // Set rate limit headers
    const remaining = Math.max(0, limit - entry.count);
    const resetTime = entry.windowStart + config.windowMs;

    res.setHeader('X-RateLimit-Limit', limit.toString());
    res.setHeader('X-RateLimit-Remaining', remaining.toString());
    res.setHeader('X-RateLimit-Reset', Math.ceil(resetTime / 1000).toString());

    next();
  };
}

/**
 * Get client identifier for rate limiting
 */
function getClientId(req: ApiRequest): string {
  // Use API key if available for more accurate tracking
  if (req.apiKey) {
    return `key:${req.apiKey.key}`;
  }
  return `ip:${req.clientId}`;
}
