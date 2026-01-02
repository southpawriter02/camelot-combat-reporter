/**
 * CORS middleware
 */
import type { Middleware, CorsConfig } from '../types.js';

/**
 * Create CORS middleware
 */
export function createCorsMiddleware(config: CorsConfig): Middleware {
  return async (req, res, next) => {
    if (!config.enabled) {
      next();
      return;
    }

    const origin = req.headers.origin;

    // Check if origin is allowed
    if (origin) {
      const isAllowed = isOriginAllowed(origin, config.origins);

      if (isAllowed) {
        // Set allowed origin
        if (config.origins.includes('*') && !config.credentials) {
          res.setHeader('Access-Control-Allow-Origin', '*');
        } else {
          res.setHeader('Access-Control-Allow-Origin', origin);
        }

        // Set credentials if enabled
        if (config.credentials) {
          res.setHeader('Access-Control-Allow-Credentials', 'true');
        }
      }
    }

    // Handle preflight
    if (req.method === 'OPTIONS') {
      // Allow methods
      res.setHeader('Access-Control-Allow-Methods', config.methods.join(', '));

      // Allow headers
      res.setHeader('Access-Control-Allow-Headers', config.allowedHeaders.join(', '));

      // Max age
      res.setHeader('Access-Control-Max-Age', config.maxAge.toString());

      // Respond to preflight
      res.writeHead(204);
      res.end();
      return;
    }

    // Expose headers for non-preflight requests
    if (config.exposedHeaders.length > 0) {
      res.setHeader('Access-Control-Expose-Headers', config.exposedHeaders.join(', '));
    }

    // Vary header for caching
    res.setHeader('Vary', 'Origin');

    next();
  };
}

/**
 * Check if origin is in allowed list
 */
function isOriginAllowed(origin: string, allowedOrigins: string[]): boolean {
  // Wildcard allows all
  if (allowedOrigins.includes('*')) {
    return true;
  }

  // Check exact match
  if (allowedOrigins.includes(origin)) {
    return true;
  }

  // Check pattern matches (e.g., *.example.com)
  for (const allowed of allowedOrigins) {
    if (allowed.startsWith('*.')) {
      const domain = allowed.slice(2);
      if (origin.endsWith(domain) || origin.endsWith('.' + domain)) {
        return true;
      }
    }
  }

  return false;
}
