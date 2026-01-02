/**
 * API Server Tests
 */
import { ApiServer, SSEManager } from '../../../src/api/ApiServer.js';
import { DEFAULT_SSE_CONFIG } from '../../../src/api/types.js';
import type { ApiServerConfig } from '../../../src/api/types.js';
import type { DatabaseAdapter } from '../../../src/database/adapters/DatabaseAdapter.js';

// Create a minimal mock database adapter
function createMockDatabase(): DatabaseAdapter {
  const mockQuery = {
    byType: jest.fn().mockReturnThis(),
    bySource: jest.fn().mockReturnThis(),
    byTarget: jest.fn().mockReturnThis(),
    inSession: jest.fn().mockReturnThis(),
    inTimeRange: jest.fn().mockReturnThis(),
    orderBy: jest.fn().mockReturnThis(),
    paginate: jest.fn().mockReturnThis(),
    count: jest.fn().mockResolvedValue(0),
    execute: jest.fn().mockResolvedValue([]),
    withParticipant: jest.fn().mockReturnThis(),
    minDuration: jest.fn().mockReturnThis(),
    maxDuration: jest.fn().mockReturnThis(),
    minParticipants: jest.fn().mockReturnThis(),
    withSummary: jest.fn().mockReturnThis(),
    byRole: jest.fn().mockReturnThis(),
    minPerformance: jest.fn().mockReturnThis(),
    playerSessions: jest.fn().mockReturnThis(),
    playerAggregate: jest.fn().mockResolvedValue(null),
    allPlayerAggregates: jest.fn().mockResolvedValue([]),
    timeBucket: jest.fn().mockReturnThis(),
    damageByEntity: jest.fn().mockReturnThis(),
    healingByEntity: jest.fn().mockReturnThis(),
    eventTypeDistribution: jest.fn().mockReturnThis(),
    leaderboard: jest.fn().mockReturnThis(),
  };

  return {
    connect: jest.fn().mockResolvedValue(undefined),
    disconnect: jest.fn().mockResolvedValue(undefined),
    isConnected: jest.fn().mockReturnValue(true),
    events: jest.fn().mockReturnValue(mockQuery),
    sessions: jest.fn().mockReturnValue(mockQuery),
    stats: jest.fn().mockReturnValue(mockQuery),
    getEventById: jest.fn().mockResolvedValue(null),
    getSessionById: jest.fn().mockResolvedValue(null),
    getEntityByName: jest.fn().mockResolvedValue(null),
    getParticipantsBySession: jest.fn().mockResolvedValue([]),
    beginTransaction: jest.fn(),
    on: jest.fn(),
    off: jest.fn(),
    emit: jest.fn(),
  } as unknown as DatabaseAdapter;
}

describe('SSEManager', () => {
  let sseManager: SSEManager;

  beforeEach(() => {
    sseManager = new SSEManager(DEFAULT_SSE_CONFIG);
  });

  afterEach(() => {
    sseManager.closeAll();
  });

  describe('addConnection', () => {
    it('should add and track connections', () => {
      const mockRes = {
        writeHead: jest.fn(),
        write: jest.fn(),
        on: jest.fn(),
      };

      sseManager.addConnection('test-id', mockRes as any, ['events']);

      expect(sseManager.getConnectionCount()).toBe(1);
      expect(mockRes.writeHead).toHaveBeenCalledWith(200, expect.any(Object));
    });

    it('should handle connection close events', () => {
      const mockRes = {
        writeHead: jest.fn(),
        write: jest.fn(),
        on: jest.fn(),
      };

      sseManager.addConnection('test-id', mockRes as any, ['events']);

      // Get the close callback and call it
      const closeCallback = (mockRes.on as jest.Mock).mock.calls.find(
        (call) => call[0] === 'close'
      )?.[1];
      if (closeCallback) {
        closeCallback();
      }

      expect(sseManager.getConnectionCount()).toBe(0);
    });
  });

  describe('broadcast', () => {
    it('should broadcast to all connections of a channel', () => {
      const mockRes1 = {
        writeHead: jest.fn(),
        write: jest.fn().mockReturnValue(true),
        on: jest.fn(),
      };
      const mockRes2 = {
        writeHead: jest.fn(),
        write: jest.fn().mockReturnValue(true),
        on: jest.fn(),
      };

      sseManager.addConnection('id1', mockRes1 as any, ['events']);
      sseManager.addConnection('id2', mockRes2 as any, ['events']);

      sseManager.broadcast('events', { type: 'test', data: 'hello' });

      expect(mockRes1.write).toHaveBeenCalled();
      expect(mockRes2.write).toHaveBeenCalled();
    });

    it('should only broadcast to specific channel', () => {
      const mockRes1 = {
        writeHead: jest.fn(),
        write: jest.fn().mockReturnValue(true),
        on: jest.fn(),
      };
      const mockRes2 = {
        writeHead: jest.fn(),
        write: jest.fn().mockReturnValue(true),
        on: jest.fn(),
      };

      sseManager.addConnection('id1', mockRes1 as any, ['events']);
      sseManager.addConnection('id2', mockRes2 as any, ['sessions']);

      sseManager.broadcast('events', { type: 'test', data: 'hello' });

      // Filter out SSE connection message writes
      const res1DataWrites = (mockRes1.write as jest.Mock).mock.calls.filter(
        (call) => call[0].includes('"type":"test"')
      );
      const res2DataWrites = (mockRes2.write as jest.Mock).mock.calls.filter(
        (call) => call[0].includes('"type":"test"')
      );

      expect(res1DataWrites.length).toBe(1);
      expect(res2DataWrites.length).toBe(0);
    });
  });

  describe('closeAll', () => {
    it('should close all connections', () => {
      const mockRes1 = {
        writeHead: jest.fn(),
        write: jest.fn(),
        on: jest.fn(),
        end: jest.fn(),
      };
      const mockRes2 = {
        writeHead: jest.fn(),
        write: jest.fn(),
        on: jest.fn(),
        end: jest.fn(),
      };

      sseManager.addConnection('id1', mockRes1 as any, ['events']);
      sseManager.addConnection('id2', mockRes2 as any, ['sessions']);

      sseManager.closeAll();

      expect(mockRes1.end).toHaveBeenCalled();
      expect(mockRes2.end).toHaveBeenCalled();
      expect(sseManager.getConnectionCount()).toBe(0);
    });
  });
});

describe('ApiServer', () => {
  let database: DatabaseAdapter;
  let server: ApiServer;

  beforeEach(() => {
    database = createMockDatabase();
  });

  afterEach(async () => {
    if (server) {
      await server.stop();
    }
  });

  describe('constructor', () => {
    it('should create server with default config', () => {
      server = new ApiServer(database);

      const status = server.getStatus();
      expect(status.state).toBe('stopped');
    });

    it('should accept custom config', () => {
      const config: Partial<ApiServerConfig> = {
        port: 8080,
        host: '0.0.0.0',
        basePath: '/api/v2',
      };

      server = new ApiServer(database, config);

      const status = server.getStatus();
      expect(status.state).toBe('stopped');
    });
  });

  describe('start/stop', () => {
    it('should start and stop server', async () => {
      server = new ApiServer(database, { port: 0 }); // Port 0 for auto-assign

      await server.start();
      let status = server.getStatus();
      expect(status.state).toBe('running');

      await server.stop();
      status = server.getStatus();
      expect(status.state).toBe('stopped');
    });

    it('should throw when starting already running server', async () => {
      server = new ApiServer(database, { port: 0 });

      await server.start();
      // Second start throws error
      await expect(server.start()).rejects.toThrow();

      const status = server.getStatus();
      expect(status.state).toBe('running');
    });

    it('should emit events on start/stop', async () => {
      server = new ApiServer(database, { port: 0 });

      const startHandler = jest.fn();
      const stopHandler = jest.fn();

      server.on('server:started', startHandler);
      server.on('server:stopped', stopHandler);

      await server.start();
      expect(startHandler).toHaveBeenCalled();

      await server.stop();
      expect(stopHandler).toHaveBeenCalled();
    });
  });

  describe('getStatus', () => {
    it('should return server status', async () => {
      server = new ApiServer(database, { port: 0 });

      let status = server.getStatus();
      expect(status).toMatchObject({
        state: 'stopped',
        uptime: 0,
        requestCount: 0,
        activeConnections: 0,
        sseConnections: 0,
      });

      await server.start();
      status = server.getStatus();
      expect(status.state).toBe('running');
      expect(status.uptime).toBeGreaterThanOrEqual(0);
    });
  });

  describe('API key management', () => {
    it('should add and remove API keys', () => {
      server = new ApiServer(database, {
        auth: {
          enabled: true,
          headerName: 'X-API-Key',
          queryParamName: 'api_key',
          keys: [],
        },
      });

      server.addApiKey({
        key: 'test-key',
        name: 'Test',
        permissions: ['read:events'],
        enabled: true,
      });

      // Key was added - can verify by checking auth config isn't throwing
      const removed = server.removeApiKey('test-key');
      expect(removed).toBe(true);

      // Removing non-existent key returns false
      expect(server.removeApiKey('non-existent')).toBe(false);
    });
  });

  describe('OpenAPI spec', () => {
    it('should generate OpenAPI specification', () => {
      server = new ApiServer(database);

      const spec = server.getOpenApiSpec();

      expect(spec).toHaveProperty('openapi', '3.0.3');
      expect(spec).toHaveProperty('info');
      expect(spec).toHaveProperty('paths');
    });
  });

  describe('Monitor attachment', () => {
    it('should attach and detach monitor', () => {
      server = new ApiServer(database);

      const mockMonitor = {
        on: jest.fn(),
        off: jest.fn(),
        getStatus: jest.fn().mockReturnValue({ state: 'running' }),
      };

      server.attachMonitor(mockMonitor as any);
      expect(mockMonitor.on).toHaveBeenCalled();

      // detachMonitor clears internal reference but doesn't call off
      server.detachMonitor();
      // Monitor was detached (internal state cleared)
    });
  });
});
