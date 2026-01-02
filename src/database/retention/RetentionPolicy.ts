/**
 * RetentionPolicy - Policy definition and evaluation
 */
import type {
  RetentionConfig,
  TableRetentionPolicy,
  RetentionTable,
  RetentionAction,
} from '../types.js';
import { DEFAULT_RETENTION_CONFIG } from '../types.js';

/**
 * Validates a retention policy configuration
 */
export function validateRetentionPolicy(policy: TableRetentionPolicy): string[] {
  const errors: string[] = [];

  if (!policy.table) {
    errors.push('Policy must have a table name');
  }

  if (policy.maxAgeDays !== undefined && policy.maxAgeDays <= 0) {
    errors.push('maxAgeDays must be positive');
  }

  if (policy.maxCount !== undefined && policy.maxCount <= 0) {
    errors.push('maxCount must be positive');
  }

  if (!policy.maxAgeDays && !policy.maxCount) {
    errors.push('Policy must have either maxAgeDays or maxCount');
  }

  if (policy.action === 'archive' && !policy.archivePath) {
    errors.push('Archive action requires archivePath');
  }

  return errors;
}

/**
 * Validates full retention configuration
 */
export function validateRetentionConfig(config: RetentionConfig): string[] {
  const errors: string[] = [];

  if (config.scheduleMs <= 0) {
    errors.push('scheduleMs must be positive');
  }

  if (config.maxConcurrency <= 0) {
    errors.push('maxConcurrency must be positive');
  }

  for (const policy of config.policies) {
    const policyErrors = validateRetentionPolicy(policy);
    errors.push(...policyErrors.map((e) => `Policy ${policy.table}: ${e}`));
  }

  return errors;
}

/**
 * Get the cutoff date for a time-based policy
 */
export function getCutoffDate(maxAgeDays: number): Date {
  const now = new Date();
  return new Date(now.getTime() - maxAgeDays * 24 * 60 * 60 * 1000);
}

/**
 * Sort policies by priority (lower first)
 */
export function sortPoliciesByPriority(
  policies: TableRetentionPolicy[]
): TableRetentionPolicy[] {
  return [...policies].sort((a, b) => (a.priority ?? 10) - (b.priority ?? 10));
}

/**
 * Filter enabled policies
 */
export function getEnabledPolicies(
  policies: TableRetentionPolicy[]
): TableRetentionPolicy[] {
  return policies.filter((p) => p.enabled);
}

/**
 * Create a default policy for a table
 */
export function createDefaultPolicy(
  table: RetentionTable,
  options: {
    maxAgeDays?: number;
    maxCount?: number;
    action?: RetentionAction;
    archivePath?: string;
    priority?: number;
  } = {}
): TableRetentionPolicy {
  return {
    table,
    enabled: true,
    maxAgeDays: options.maxAgeDays,
    maxCount: options.maxCount,
    action: options.action ?? 'delete',
    archivePath: options.archivePath,
    priority: options.priority ?? 10,
  };
}

/**
 * Merge user config with defaults
 */
export function mergeRetentionConfig(
  userConfig: Partial<RetentionConfig>
): RetentionConfig {
  return {
    ...DEFAULT_RETENTION_CONFIG,
    ...userConfig,
    policies: userConfig.policies ?? DEFAULT_RETENTION_CONFIG.policies,
  };
}

/**
 * Get policy for a specific table
 */
export function getPolicyForTable(
  config: RetentionConfig,
  table: RetentionTable
): TableRetentionPolicy | undefined {
  return config.policies.find((p) => p.table === table);
}

/**
 * Check if retention should run based on last run time
 */
export function shouldRunRetention(
  lastRunTime: Date | null,
  scheduleMs: number
): boolean {
  if (!lastRunTime) {
    return true;
  }

  const elapsed = Date.now() - lastRunTime.getTime();
  return elapsed >= scheduleMs;
}
