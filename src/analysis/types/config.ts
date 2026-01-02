/**
 * Configuration for combat session detection
 */
export interface CombatSessionConfig {
  /** Inactivity timeout in milliseconds to end a combat session (default: 30000 = 30s) */
  inactivityTimeoutMs: number;
  /** Minimum events required to consider a valid combat session (default: 3) */
  minEventsForSession: number;
  /** Whether to merge nearby sessions (default: false) */
  mergeNearbySessions: boolean;
  /** Time window for merging sessions in milliseconds (default: 10000 = 10s) */
  mergeWindowMs: number;
}

/**
 * Configuration for metrics calculation
 */
export interface MetricsConfig {
  /** Time window for rolling DPS/HPS calculations in milliseconds (default: 5000 = 5s) */
  rollingWindowMs: number;
  /** Whether to include overkill/overheal in calculations (default: false) */
  includeOverage: boolean;
  /** Threshold for "big hit" key event detection (default: 500) */
  bigHitThreshold: number;
  /** Threshold for "big heal" key event detection (default: 500) */
  bigHealThreshold: number;
}

/**
 * Full analysis configuration
 */
export interface AnalysisConfig {
  session: CombatSessionConfig;
  metrics: MetricsConfig;
}

/**
 * Partial analysis configuration for constructor overrides
 */
export interface PartialAnalysisConfig {
  session?: Partial<CombatSessionConfig>;
  metrics?: Partial<MetricsConfig>;
}

/**
 * Default session configuration
 */
export const DEFAULT_SESSION_CONFIG: CombatSessionConfig = {
  inactivityTimeoutMs: 30000,
  minEventsForSession: 3,
  mergeNearbySessions: false,
  mergeWindowMs: 10000,
};

/**
 * Default metrics configuration
 */
export const DEFAULT_METRICS_CONFIG: MetricsConfig = {
  rollingWindowMs: 5000,
  includeOverage: false,
  bigHitThreshold: 500,
  bigHealThreshold: 500,
};

/**
 * Default full configuration
 */
export const DEFAULT_ANALYSIS_CONFIG: AnalysisConfig = {
  session: DEFAULT_SESSION_CONFIG,
  metrics: DEFAULT_METRICS_CONFIG,
};
