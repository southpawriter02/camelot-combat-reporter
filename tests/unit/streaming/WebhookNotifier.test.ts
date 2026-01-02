import { WebhookNotifier } from '../../../src/streaming/WebhookNotifier';
import type { WebhookPayload, WebhookDeliveryResult, DeadLetterEntry } from '../../../src/streaming/types';

// Mock fetch
const originalFetch = global.fetch;

describe('WebhookNotifier', () => {
  let notifier: WebhookNotifier;
  let mockFetch: jest.Mock;

  beforeEach(() => {
    notifier = new WebhookNotifier();

    // Mock fetch
    mockFetch = jest.fn();
    global.fetch = mockFetch;
  });

  afterEach(() => {
    notifier.destroy();
    global.fetch = originalFetch;
  });

  const createPayload = (eventType: string = 'test'): WebhookPayload => ({
    eventType,
    timestamp: new Date().toISOString(),
    data: {} as any,
    metadata: {
      filename: 'test.log',
    },
  });

  const mockSuccessResponse = () => {
    mockFetch.mockResolvedValue({
      ok: true,
      status: 200,
      statusText: 'OK',
      text: () => Promise.resolve('{"success":true}'),
    });
  };

  const mockErrorResponse = (status: number = 500) => {
    mockFetch.mockResolvedValue({
      ok: false,
      status,
      statusText: 'Internal Server Error',
      text: () => Promise.resolve('Error'),
    });
  };

  describe('webhook management', () => {
    it('should add webhook and return ID', () => {
      const id = notifier.addWebhook({ url: 'https://example.com/hook' });
      expect(id).toBeDefined();
      expect(typeof id).toBe('string');
    });

    it('should use provided ID if given', () => {
      const id = notifier.addWebhook({
        url: 'https://example.com/hook',
        id: 'custom-id',
      });
      expect(id).toBe('custom-id');
    });

    it('should get webhooks', () => {
      notifier.addWebhook({ url: 'https://example.com/hook1' });
      notifier.addWebhook({ url: 'https://example.com/hook2' });

      const webhooks = notifier.getWebhooks();
      expect(webhooks).toHaveLength(2);
    });

    it('should get webhook by ID', () => {
      const id = notifier.addWebhook({ url: 'https://example.com/hook' });
      const webhook = notifier.getWebhook(id);
      expect(webhook?.url).toBe('https://example.com/hook');
    });

    it('should remove webhook', () => {
      const id = notifier.addWebhook({ url: 'https://example.com/hook' });
      const removed = notifier.removeWebhook(id);

      expect(removed).toBe(true);
      expect(notifier.getWebhooks()).toHaveLength(0);
    });

    it('should return false when removing non-existent webhook', () => {
      const removed = notifier.removeWebhook('nonexistent');
      expect(removed).toBe(false);
    });
  });

  describe('notify', () => {
    it('should send payload to registered webhooks', async () => {
      mockSuccessResponse();
      notifier.addWebhook({ url: 'https://example.com/hook' });

      const payload = createPayload();
      const results = await notifier.notify(payload);

      expect(results).toHaveLength(1);
      expect(results[0]?.success).toBe(true);
      expect(mockFetch).toHaveBeenCalledWith(
        'https://example.com/hook',
        expect.objectContaining({
          method: 'POST',
          headers: expect.objectContaining({
            'Content-Type': 'application/json',
          }),
        })
      );
    });

    it('should filter by event type', async () => {
      mockSuccessResponse();
      notifier.addWebhook({
        url: 'https://example.com/hook',
        events: ['session:end'],
      });

      const payload = createPayload('event:damage');
      const results = await notifier.notify(payload);

      expect(results).toHaveLength(0); // Filtered out
      expect(mockFetch).not.toHaveBeenCalled();
    });

    it('should send to webhook with matching event filter', async () => {
      mockSuccessResponse();
      notifier.addWebhook({
        url: 'https://example.com/hook',
        events: ['session:end', 'event:death'],
      });

      const payload = createPayload('session:end');
      const results = await notifier.notify(payload);

      expect(results).toHaveLength(1);
      expect(mockFetch).toHaveBeenCalled();
    });

    it('should send to all webhooks with empty event filter', async () => {
      mockSuccessResponse();
      notifier.addWebhook({
        url: 'https://example.com/hook',
        events: [],
      });

      const payload = createPayload('any:event');
      const results = await notifier.notify(payload);

      expect(results).toHaveLength(1);
    });

    it('should include custom headers', async () => {
      mockSuccessResponse();
      notifier.addWebhook({
        url: 'https://example.com/hook',
        headers: { Authorization: 'Bearer token123' },
      });

      await notifier.notify(createPayload());

      expect(mockFetch).toHaveBeenCalledWith(
        expect.any(String),
        expect.objectContaining({
          headers: expect.objectContaining({
            Authorization: 'Bearer token123',
          }),
        })
      );
    });
  });

  describe('retry logic', () => {
    it('should retry on failure', async () => {
      mockFetch
        .mockResolvedValueOnce({
          ok: false,
          status: 500,
          statusText: 'Error',
          text: () => Promise.resolve(''),
        })
        .mockResolvedValueOnce({
          ok: true,
          status: 200,
          statusText: 'OK',
          text: () => Promise.resolve(''),
        });

      notifier.addWebhook({
        url: 'https://example.com/hook',
        maxRetries: 3,
        retryDelayMs: 10,
      });

      const results = await notifier.notify(createPayload());

      expect(results[0]?.success).toBe(true);
      expect(mockFetch).toHaveBeenCalledTimes(2);
    });

    it('should emit retry event', async () => {
      mockFetch.mockResolvedValue({
        ok: false,
        status: 500,
        statusText: 'Error',
        text: () => Promise.resolve(''),
      });

      const retryEvents: { id: string; attempt: number }[] = [];
      notifier.on('retry', (id, attempt) => retryEvents.push({ id, attempt }));

      notifier.addWebhook({
        url: 'https://example.com/hook',
        maxRetries: 2,
        retryDelayMs: 5,
      });

      await notifier.notify(createPayload());

      expect(retryEvents.length).toBe(2); // 2 retries
    });

    it('should add to dead letter queue after all retries fail', async () => {
      mockErrorResponse();

      notifier.addWebhook({
        url: 'https://example.com/hook',
        maxRetries: 1,
        retryDelayMs: 5,
      });

      await notifier.notify(createPayload());

      const dlq = notifier.getDeadLetterQueue();
      expect(dlq).toHaveLength(1);
      expect(dlq[0]?.webhook.url).toBe('https://example.com/hook');
    });

    it('should emit failed event on final failure', async () => {
      mockErrorResponse();

      const failedEvents: { result: WebhookDeliveryResult; entry: DeadLetterEntry }[] = [];
      notifier.on('failed', (result, entry) => failedEvents.push({ result, entry }));

      notifier.addWebhook({
        url: 'https://example.com/hook',
        maxRetries: 0,
        retryDelayMs: 5,
      });

      await notifier.notify(createPayload());

      expect(failedEvents).toHaveLength(1);
      expect(failedEvents[0]?.result.success).toBe(false);
    });
  });

  describe('dead letter queue', () => {
    it('should clear dead letter queue', async () => {
      mockErrorResponse();

      notifier.addWebhook({
        url: 'https://example.com/hook',
        maxRetries: 0,
      });

      await notifier.notify(createPayload());

      const cleared = notifier.clearDeadLetterQueue();
      expect(cleared).toHaveLength(1);
      expect(notifier.getDeadLetterQueue()).toHaveLength(0);
    });

    it('should retry dead letter entry', async () => {
      mockFetch
        .mockResolvedValueOnce({
          ok: false,
          status: 500,
          statusText: 'Error',
          text: () => Promise.resolve(''),
        })
        .mockResolvedValueOnce({
          ok: true,
          status: 200,
          statusText: 'OK',
          text: () => Promise.resolve(''),
        });

      notifier.addWebhook({
        url: 'https://example.com/hook',
        maxRetries: 0,
        retryDelayMs: 5,
      });

      await notifier.notify(createPayload());
      expect(notifier.getDeadLetterQueue()).toHaveLength(1);

      const result = await notifier.retryDeadLetter(0);
      expect(result?.success).toBe(true);
      expect(notifier.getDeadLetterQueue()).toHaveLength(0);
    });

    it('should return null for invalid dead letter index', async () => {
      const result = await notifier.retryDeadLetter(999);
      expect(result).toBeNull();
    });
  });

  describe('statistics', () => {
    it('should track delivered count', async () => {
      mockSuccessResponse();
      notifier.addWebhook({ url: 'https://example.com/hook' });

      await notifier.notify(createPayload());
      await notifier.notify(createPayload());

      const stats = notifier.getStats();
      expect(stats.delivered).toBe(2);
    });

    it('should track failed count', async () => {
      mockErrorResponse();
      notifier.addWebhook({
        url: 'https://example.com/hook',
        maxRetries: 0,
      });

      await notifier.notify(createPayload());

      const stats = notifier.getStats();
      expect(stats.failed).toBe(1);
    });

    it('should track retries count', async () => {
      mockErrorResponse();
      notifier.addWebhook({
        url: 'https://example.com/hook',
        maxRetries: 2,
        retryDelayMs: 5,
      });

      await notifier.notify(createPayload());

      const stats = notifier.getStats();
      expect(stats.retries).toBe(2);
    });

    it('should reset statistics', async () => {
      mockSuccessResponse();
      notifier.addWebhook({ url: 'https://example.com/hook' });
      await notifier.notify(createPayload());

      notifier.resetStats();

      const stats = notifier.getStats();
      expect(stats.delivered).toBe(0);
    });
  });

  describe('timeout handling', () => {
    it('should timeout slow requests', async () => {
      // Mock fetch that respects AbortController signal
      mockFetch.mockImplementation((_url: string, options?: { signal?: AbortSignal }) => {
        return new Promise((resolve, reject) => {
          const timeoutId = setTimeout(() => {
            resolve({
              ok: true,
              status: 200,
              text: () => Promise.resolve(''),
            });
          }, 100);

          if (options?.signal) {
            options.signal.addEventListener('abort', () => {
              clearTimeout(timeoutId);
              const abortError = new Error('The operation was aborted');
              abortError.name = 'AbortError';
              reject(abortError);
            });
          }
        });
      });

      notifier.addWebhook({
        url: 'https://example.com/hook',
        timeoutMs: 10,
        maxRetries: 0,
      });

      const results = await notifier.notify(createPayload());
      expect(results[0]?.success).toBe(false);
      expect(results[0]?.error).toMatch(/timed out|abort/i);
    });
  });

  describe('cleanup', () => {
    it('should clean up on destroy', () => {
      notifier.addWebhook({ url: 'https://example.com/hook' });
      notifier.destroy();

      expect(notifier.getWebhooks()).toHaveLength(0);
      expect(notifier.getDeadLetterQueue()).toHaveLength(0);
    });
  });
});
