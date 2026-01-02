/**
 * WebhookNotifier - HTTP webhook delivery with retry and dead-letter queue
 *
 * Sends event payloads to configured webhook URLs with:
 * - Exponential backoff retry
 * - Event filtering by type
 * - Dead-letter queue for failed deliveries
 */
import { EventEmitter } from 'events';
import { v4 as uuidv4 } from 'uuid';
import type {
  WebhookConfig,
  WebhookPayload,
  WebhookDeliveryResult,
  DeadLetterEntry,
} from './types.js';
import { DEFAULT_WEBHOOK_CONFIG } from './types.js';

/**
 * Events emitted by WebhookNotifier
 */
export interface WebhookNotifierEvents {
  delivered: (result: WebhookDeliveryResult) => void;
  failed: (result: WebhookDeliveryResult, entry: DeadLetterEntry) => void;
  retry: (webhookId: string, attempt: number, error: string) => void;
}

/**
 * WebhookNotifier manages webhook delivery with retry
 */
export class WebhookNotifier extends EventEmitter {
  private webhooks: Map<string, WebhookConfig> = new Map();
  private deadLetterQueue: DeadLetterEntry[] = [];
  private stats = {
    delivered: 0,
    failed: 0,
    retries: 0,
  };

  constructor() {
    super();
  }

  /**
   * Add a webhook configuration
   * @returns The webhook ID
   */
  addWebhook(config: Partial<WebhookConfig> & { url: string }): string {
    const id = config.id || uuidv4();
    const fullConfig: WebhookConfig = {
      ...DEFAULT_WEBHOOK_CONFIG,
      ...config,
      id,
    } as WebhookConfig;

    this.webhooks.set(id, fullConfig);
    return id;
  }

  /**
   * Remove a webhook by ID
   */
  removeWebhook(id: string): boolean {
    return this.webhooks.delete(id);
  }

  /**
   * Get all registered webhooks
   */
  getWebhooks(): WebhookConfig[] {
    return Array.from(this.webhooks.values());
  }

  /**
   * Get a specific webhook by ID
   */
  getWebhook(id: string): WebhookConfig | undefined {
    return this.webhooks.get(id);
  }

  /**
   * Send a payload to all matching webhooks
   */
  async notify(payload: WebhookPayload): Promise<WebhookDeliveryResult[]> {
    const results: WebhookDeliveryResult[] = [];

    for (const webhook of this.webhooks.values()) {
      // Check if webhook is subscribed to this event type
      if (
        webhook.events.length > 0 &&
        !webhook.events.includes(payload.eventType)
      ) {
        continue;
      }

      const result = await this.deliverWithRetry(webhook, payload);
      results.push(result);
    }

    return results;
  }

  /**
   * Deliver payload to a single webhook with retry
   */
  private async deliverWithRetry(
    webhook: WebhookConfig,
    payload: WebhookPayload
  ): Promise<WebhookDeliveryResult> {
    let lastError = '';
    let attempt = 0;
    let delay = webhook.retryDelayMs;

    while (attempt <= webhook.maxRetries) {
      attempt++;

      try {
        const result = await this.deliver(webhook, payload);

        if (result.success) {
          this.stats.delivered++;
          this.emit('delivered', result);
          return result;
        }

        lastError = result.error || 'Unknown error';
      } catch (error) {
        lastError = (error as Error).message;
      }

      // Don't retry after final attempt
      if (attempt <= webhook.maxRetries) {
        this.stats.retries++;
        this.emit('retry', webhook.id!, attempt, lastError);

        // Wait before retry
        await this.sleep(delay);
        delay *= webhook.retryMultiplier;
      }
    }

    // All retries exhausted - add to dead letter queue
    this.stats.failed++;
    const deadLetterEntry: DeadLetterEntry = {
      webhook,
      payload,
      error: lastError,
      failedAt: new Date(),
      attempts: attempt,
    };
    this.deadLetterQueue.push(deadLetterEntry);

    const failedResult: WebhookDeliveryResult = {
      webhookId: webhook.id!,
      success: false,
      error: lastError,
      attempts: attempt,
    };

    this.emit('failed', failedResult, deadLetterEntry);
    return failedResult;
  }

  /**
   * Deliver a single webhook request
   */
  private async deliver(
    webhook: WebhookConfig,
    payload: WebhookPayload
  ): Promise<WebhookDeliveryResult> {
    const controller = new AbortController();
    const timeoutId = setTimeout(() => controller.abort(), webhook.timeoutMs);

    try {
      const response = await fetch(webhook.url, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          ...webhook.headers,
        },
        body: JSON.stringify(payload),
        signal: controller.signal,
      });

      clearTimeout(timeoutId);

      let responseBody: string | undefined;
      try {
        responseBody = await response.text();
      } catch {
        // Ignore response body read errors
      }

      if (response.ok) {
        return {
          webhookId: webhook.id!,
          success: true,
          statusCode: response.status,
          attempts: 1,
          responseBody,
        };
      }

      return {
        webhookId: webhook.id!,
        success: false,
        statusCode: response.status,
        error: `HTTP ${response.status}: ${response.statusText}`,
        attempts: 1,
        responseBody,
      };
    } catch (error) {
      clearTimeout(timeoutId);

      const errorMessage =
        error instanceof Error
          ? error.name === 'AbortError'
            ? 'Request timed out'
            : error.message
          : 'Unknown error';

      return {
        webhookId: webhook.id!,
        success: false,
        error: errorMessage,
        attempts: 1,
      };
    }
  }

  /**
   * Get the dead letter queue
   */
  getDeadLetterQueue(): DeadLetterEntry[] {
    return [...this.deadLetterQueue];
  }

  /**
   * Clear the dead letter queue
   */
  clearDeadLetterQueue(): DeadLetterEntry[] {
    const cleared = this.deadLetterQueue;
    this.deadLetterQueue = [];
    return cleared;
  }

  /**
   * Retry a dead letter entry
   */
  async retryDeadLetter(index: number): Promise<WebhookDeliveryResult | null> {
    if (index < 0 || index >= this.deadLetterQueue.length) {
      return null;
    }

    const entry = this.deadLetterQueue[index]!;
    this.deadLetterQueue.splice(index, 1);

    return this.deliverWithRetry(entry.webhook, entry.payload);
  }

  /**
   * Get delivery statistics
   */
  getStats(): typeof this.stats {
    return { ...this.stats };
  }

  /**
   * Reset statistics
   */
  resetStats(): void {
    this.stats = {
      delivered: 0,
      failed: 0,
      retries: 0,
    };
  }

  /**
   * Helper to sleep for a duration
   */
  private sleep(ms: number): Promise<void> {
    return new Promise((resolve) => setTimeout(resolve, ms));
  }

  /**
   * Clean up all webhooks
   */
  destroy(): void {
    this.webhooks.clear();
    this.deadLetterQueue = [];
    this.removeAllListeners();
  }

  /**
   * Type-safe event emitter methods
   */
  override on<K extends keyof WebhookNotifierEvents>(
    event: K,
    listener: WebhookNotifierEvents[K]
  ): this {
    return super.on(event, listener);
  }

  override once<K extends keyof WebhookNotifierEvents>(
    event: K,
    listener: WebhookNotifierEvents[K]
  ): this {
    return super.once(event, listener);
  }

  override emit<K extends keyof WebhookNotifierEvents>(
    event: K,
    ...args: Parameters<WebhookNotifierEvents[K]>
  ): boolean {
    return super.emit(event, ...args);
  }

  override off<K extends keyof WebhookNotifierEvents>(
    event: K,
    listener: WebhookNotifierEvents[K]
  ): this {
    return super.off(event, listener);
  }
}
