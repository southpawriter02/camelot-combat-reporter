/**
 * Model Registry
 *
 * Tracks available models and their metadata.
 * Provides discovery and versioning support for ML models.
 */

import type { ModelMetadata } from '../types.js';
import { MODEL_NAMES, MODEL_VERSIONS } from '../config.js';

/**
 * Registry entry for a model
 */
export interface ModelRegistryEntry {
  /** Model name/identifier */
  name: string;
  /** Available versions */
  versions: string[];
  /** Latest version */
  latestVersion: string;
  /** Model description */
  description: string;
  /** Input feature count */
  inputSize: number;
  /** Output size */
  outputSize: number;
  /** Whether a trained model is available (vs placeholder) */
  isAvailable: boolean;
}

/**
 * Registry of available ML models
 */
export class ModelRegistry {
  private entries = new Map<string, ModelRegistryEntry>();

  constructor() {
    this.registerDefaultModels();
  }

  /**
   * Register the default models
   */
  private registerDefaultModels(): void {
    // Fight Outcome Predictor
    this.register({
      name: MODEL_NAMES.FIGHT_OUTCOME,
      versions: [MODEL_VERSIONS[MODEL_NAMES.FIGHT_OUTCOME]],
      latestVersion: MODEL_VERSIONS[MODEL_NAMES.FIGHT_OUTCOME],
      description: 'Predicts win/loss probability based on current fight state',
      inputSize: 32,
      outputSize: 2,
      isAvailable: false, // Placeholder until trained
    });

    // Playstyle Classifier
    this.register({
      name: MODEL_NAMES.PLAYSTYLE,
      versions: [MODEL_VERSIONS[MODEL_NAMES.PLAYSTYLE]],
      latestVersion: MODEL_VERSIONS[MODEL_NAMES.PLAYSTYLE],
      description: 'Classifies player behavior as aggressive, defensive, balanced, or opportunistic',
      inputSize: 24,
      outputSize: 4,
      isAvailable: false,
    });

    // Performance Predictor
    this.register({
      name: MODEL_NAMES.PERFORMANCE,
      versions: [MODEL_VERSIONS[MODEL_NAMES.PERFORMANCE]],
      latestVersion: MODEL_VERSIONS[MODEL_NAMES.PERFORMANCE],
      description: 'Predicts expected DPS, HPS, and performance score',
      inputSize: 24,
      outputSize: 3,
      isAvailable: false,
    });

    // Threat Assessor
    this.register({
      name: MODEL_NAMES.THREAT,
      versions: [MODEL_VERSIONS[MODEL_NAMES.THREAT]],
      latestVersion: MODEL_VERSIONS[MODEL_NAMES.THREAT],
      description: 'Assesses threat level of enemies based on their behavior',
      inputSize: 20,
      outputSize: 1,
      isAvailable: false,
    });
  }

  /**
   * Register a model in the registry
   */
  register(entry: ModelRegistryEntry): void {
    this.entries.set(entry.name, entry);
  }

  /**
   * Unregister a model
   */
  unregister(name: string): boolean {
    return this.entries.delete(name);
  }

  /**
   * Get a model entry by name
   */
  get(name: string): ModelRegistryEntry | undefined {
    return this.entries.get(name);
  }

  /**
   * Check if a model is registered
   */
  has(name: string): boolean {
    return this.entries.has(name);
  }

  /**
   * Get all registered models
   */
  getAll(): ModelRegistryEntry[] {
    return Array.from(this.entries.values());
  }

  /**
   * Get all model names
   */
  getNames(): string[] {
    return Array.from(this.entries.keys());
  }

  /**
   * Get the latest version of a model
   */
  getLatestVersion(name: string): string | undefined {
    return this.entries.get(name)?.latestVersion;
  }

  /**
   * Check if a specific version exists
   */
  hasVersion(name: string, version: string): boolean {
    const entry = this.entries.get(name);
    return entry ? entry.versions.includes(version) : false;
  }

  /**
   * Add a new version to a model
   */
  addVersion(name: string, version: string, makeLatest = true): boolean {
    const entry = this.entries.get(name);
    if (!entry) return false;

    if (!entry.versions.includes(version)) {
      entry.versions.push(version);
    }

    if (makeLatest) {
      entry.latestVersion = version;
    }

    return true;
  }

  /**
   * Mark a model as available (trained model exists)
   */
  markAvailable(name: string, isAvailable = true): boolean {
    const entry = this.entries.get(name);
    if (!entry) return false;

    entry.isAvailable = isAvailable;
    return true;
  }

  /**
   * Get available models (with trained weights)
   */
  getAvailable(): ModelRegistryEntry[] {
    return this.getAll().filter((e) => e.isAvailable);
  }

  /**
   * Get placeholder models (using heuristics)
   */
  getPlaceholders(): ModelRegistryEntry[] {
    return this.getAll().filter((e) => !e.isAvailable);
  }

  /**
   * Convert registry entries to ModelMetadata format
   */
  toMetadata(): ModelMetadata[] {
    return this.getAll().map((entry) => ({
      name: entry.name,
      version: entry.latestVersion,
      inputShape: [entry.inputSize],
      outputShape: [entry.outputSize],
      featureNames: [],
      trainedOn: new Date().toISOString(),
      isPlaceholder: !entry.isAvailable,
    }));
  }
}

/**
 * Default model registry instance
 */
export const defaultModelRegistry = new ModelRegistry();
