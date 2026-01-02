/**
 * Integration tests for ML module
 */
import { CombatAnalyzer } from '../../src/analysis/CombatAnalyzer';
import { MLPredictor } from '../../src/ml/predictors/MLPredictor';
import { TrainingDataExporter, resolveOutcomeByDeaths } from '../../src/ml/training/TrainingDataExporter';
import { EntityType, EventType, ActionType, DamageType } from '../../src/types';
import type { Entity, CombatEvent, DamageEvent, DeathEvent } from '../../src/types';
import type { CombatSession, SessionParticipant, SessionSummary } from '../../src/analysis/types';

// Helper functions
const createEntity = (name: string, isSelf = false): Entity => ({
  name,
  entityType: isSelf ? EntityType.SELF : EntityType.PLAYER,
  isPlayer: true,
  isSelf,
});

const createParticipant = (entity: Entity): SessionParticipant => ({
  entity,
  role: 'DAMAGE_DEALER',
  firstSeen: new Date('2024-01-01T12:00:00Z'),
  lastSeen: new Date('2024-01-01T12:01:00Z'),
  eventCount: 0,
});

const createSummary = (events: CombatEvent[]): SessionSummary => ({
  totalDamageDealt: events.reduce((sum, e) => {
    if (e.eventType === EventType.DAMAGE_DEALT) {
      return sum + (e as DamageEvent).effectiveAmount;
    }
    return sum;
  }, 0),
  totalDamageTaken: 0,
  totalHealingDone: 0,
  totalHealingReceived: 0,
  deathCount: events.filter((e) => e.eventType === EventType.DEATH).length,
  ccEventCount: 0,
  keyEvents: [],
});

const createDamageEvent = (
  source: Entity,
  target: Entity,
  amount: number,
  timestamp: Date,
  id: string,
  actionType: ActionType = ActionType.MELEE
): DamageEvent => ({
  id,
  timestamp,
  rawTimestamp: '[00:00:00]',
  rawLine: '',
  lineNumber: 1,
  eventType: EventType.DAMAGE_DEALT,
  source,
  target,
  amount,
  absorbedAmount: 0,
  effectiveAmount: amount,
  damageType: DamageType.SLASH,
  actionType,
  isCritical: false,
  isBlocked: false,
  isParried: false,
  isEvaded: false,
});

const createDeathEvent = (target: Entity, killer: Entity, timestamp: Date, id: string): DeathEvent => ({
  id,
  timestamp,
  rawTimestamp: '[00:00:00]',
  rawLine: '',
  lineNumber: 1,
  eventType: EventType.DEATH,
  target,
  killer,
});

const createSession = (
  events: CombatEvent[],
  durationMs = 60000,
  participants?: Entity[]
): CombatSession => {
  const self = createEntity('You', true);
  const enemy = createEntity('Enemy');
  const allParticipants = participants ?? [self, enemy];

  return {
    id: 'test-session-' + Math.random().toString(36).slice(2, 9),
    startTime: new Date('2024-01-01T12:00:00Z'),
    endTime: new Date(new Date('2024-01-01T12:00:00Z').getTime() + durationMs),
    durationMs,
    events,
    participants: allParticipants.map((entity) => createParticipant(entity)),
    summary: createSummary(events),
  };
};

describe('ML Integration', () => {
  describe('CombatAnalyzer with ML', () => {
    it('should create analyzer with ML enabled', () => {
      const analyzer = new CombatAnalyzer({ ml: { enabled: true } });
      expect(analyzer.isMLEnabled()).toBe(true);
    });

    it('should create analyzer with ML disabled', () => {
      const analyzer = new CombatAnalyzer({ ml: { enabled: false } });
      expect(analyzer.isMLEnabled()).toBe(false);
    });

    it('should get ML predictor from analyzer', () => {
      const analyzer = new CombatAnalyzer({ ml: { enabled: true } });
      const predictor = analyzer.getMLPredictor();
      expect(predictor).toBeInstanceOf(MLPredictor);
    });

    it('should return predictor with isEnabled=false when disabled', () => {
      const analyzer = new CombatAnalyzer({ ml: { enabled: false } });
      const predictor = analyzer.getMLPredictor();
      // getMLPredictor always returns a predictor instance, check isEnabled property
      expect(predictor).toBeInstanceOf(MLPredictor);
      expect(predictor.isEnabled).toBe(false);
    });

    it('should predict fight outcome through analyzer', async () => {
      const analyzer = new CombatAnalyzer({ ml: { enabled: true, lazyLoad: true } });
      const self = createEntity('You', true);
      const enemy = createEntity('Enemy');
      const baseTime = new Date('2024-01-01T12:00:00Z');

      const events: CombatEvent[] = Array.from({ length: 20 }, (_, i) =>
        createDamageEvent(self, enemy, 100, new Date(baseTime.getTime() + i * 1000), String(i))
      );

      const session = createSession(events, 20000);
      const prediction = await analyzer.predictFightOutcome(session, 'You');

      expect(prediction).not.toBeNull();
      expect(prediction?.winProbability).toBeGreaterThanOrEqual(0);
      expect(prediction?.winProbability).toBeLessThanOrEqual(1);
    });

    it('should classify playstyle through analyzer', async () => {
      const analyzer = new CombatAnalyzer({ ml: { enabled: true, lazyLoad: true } });
      const self = createEntity('You', true);
      const enemy = createEntity('Enemy');
      const baseTime = new Date('2024-01-01T12:00:00Z');

      const events: CombatEvent[] = Array.from({ length: 20 }, (_, i) =>
        createDamageEvent(self, enemy, 100, new Date(baseTime.getTime() + i * 1000), String(i))
      );

      const session = createSession(events, 20000);
      const classification = await analyzer.classifyPlaystyle(session, 'You');

      expect(classification).not.toBeNull();
      expect(['AGGRESSIVE', 'DEFENSIVE', 'BALANCED', 'OPPORTUNISTIC']).toContain(
        classification?.primaryStyle
      );
    });

    it('should assess threats through analyzer', async () => {
      const analyzer = new CombatAnalyzer({ ml: { enabled: true, lazyLoad: true } });
      const self = createEntity('You', true);
      const enemy = createEntity('Enemy');
      const baseTime = new Date('2024-01-01T12:00:00Z');

      const events: CombatEvent[] = Array.from({ length: 10 }, (_, i) =>
        createDamageEvent(enemy, self, 100, new Date(baseTime.getTime() + i * 1000), String(i))
      );

      const session = createSession(events, 10000, [self, enemy]);
      const threats = await analyzer.assessThreats(session, self);

      expect(threats).toBeInstanceOf(Array);
    });

    it('should return null for predictions when ML disabled', async () => {
      const analyzer = new CombatAnalyzer({ ml: { enabled: false } });
      const session = createSession([], 60000);

      const prediction = await analyzer.predictFightOutcome(session, 'You');
      expect(prediction).toBeNull();

      const classification = await analyzer.classifyPlaystyle(session, 'You');
      expect(classification).toBeNull();

      const performance = await analyzer.predictPerformance([session], 'You');
      expect(performance).toBeNull();

      const self = createEntity('You', true);
      const threats = await analyzer.assessThreats(session, self);
      expect(threats).toEqual([]);
    });
  });

  describe('Training Data Export', () => {
    it('should export outcome training data', () => {
      const exporter = new TrainingDataExporter();
      const self = createEntity('You', true);
      const enemy = createEntity('Enemy');
      const baseTime = new Date('2024-01-01T12:00:00Z');

      const events: CombatEvent[] = Array.from({ length: 20 }, (_, i) =>
        createDamageEvent(self, enemy, 100, new Date(baseTime.getTime() + i * 1000), String(i))
      );

      const session = createSession(events, 20000);
      exporter.addOutcomeExample(session, 'You', 'WIN');

      const dataset = exporter.exportOutcomeDataset();

      expect(dataset.examples.length).toBeGreaterThan(0);
      expect(dataset.metadata.exampleCount).toBeGreaterThan(0);
      expect(dataset.metadata.labelDistribution.WIN).toBeGreaterThan(0);
      expect(dataset.metadata.source).toBe('camelot-combat-reporter');
    });

    it('should export playstyle training data', () => {
      const exporter = new TrainingDataExporter();
      const self = createEntity('You', true);
      const enemy = createEntity('Enemy');
      const baseTime = new Date('2024-01-01T12:00:00Z');

      const events: CombatEvent[] = Array.from({ length: 20 }, (_, i) =>
        createDamageEvent(self, enemy, 100, new Date(baseTime.getTime() + i * 1000), String(i))
      );

      const session = createSession(events, 20000);
      exporter.addPlaystyleExample(session, 'You', 'AGGRESSIVE');

      const dataset = exporter.exportPlaystyleDataset();

      expect(dataset.examples.length).toBe(1);
      expect(dataset.examples[0]?.label).toBe('AGGRESSIVE');
      expect(dataset.metadata.labelDistribution.AGGRESSIVE).toBe(1);
    });

    it('should auto-label outcome by deaths', () => {
      const exporter = new TrainingDataExporter();
      const self = createEntity('You', true);
      const enemy = createEntity('Enemy');
      const baseTime = new Date('2024-01-01T12:00:00Z');

      const winEvents: CombatEvent[] = [
        createDeathEvent(enemy, self, baseTime, '1'),
        createDeathEvent(enemy, self, new Date(baseTime.getTime() + 5000), '2'),
      ];

      const lossEvents: CombatEvent[] = [
        createDeathEvent(self, enemy, baseTime, '1'),
      ];

      const winSession = createSession(winEvents, 10000);
      const lossSession = createSession(lossEvents, 10000);

      exporter.addSessionsWithAutoOutcome([winSession, lossSession], 'You', resolveOutcomeByDeaths);

      const dataset = exporter.exportOutcomeDataset();
      expect(dataset.metadata.labelDistribution.WIN).toBeGreaterThan(0);
      expect(dataset.metadata.labelDistribution.LOSS).toBeGreaterThan(0);
    });

    it('should track example counts correctly', () => {
      const exporter = new TrainingDataExporter();
      const self = createEntity('You', true);
      const enemy = createEntity('Enemy');
      const baseTime = new Date('2024-01-01T12:00:00Z');

      // Create session with events (empty sessions don't add examples)
      const events: CombatEvent[] = Array.from({ length: 5 }, (_, i) =>
        createDamageEvent(self, enemy, 100, new Date(baseTime.getTime() + i * 1000), String(i))
      );
      const session = createSession(events, 10000);

      expect(exporter.getExampleCounts().outcome).toBe(0);
      expect(exporter.getExampleCounts().playstyle).toBe(0);

      exporter.addPlaystyleExample(session, 'You', 'BALANCED');
      expect(exporter.getExampleCounts().playstyle).toBe(1);

      exporter.clear();
      expect(exporter.getExampleCounts().outcome).toBe(0);
      expect(exporter.getExampleCounts().playstyle).toBe(0);
    });

    it('should export training stats', () => {
      const exporter = new TrainingDataExporter({ featureVersion: '2.0.0' });
      const self = createEntity('You', true);
      const enemy = createEntity('Enemy');
      const baseTime = new Date('2024-01-01T12:00:00Z');

      const events: CombatEvent[] = Array.from({ length: 20 }, (_, i) =>
        createDamageEvent(self, enemy, 100, new Date(baseTime.getTime() + i * 1000), String(i))
      );

      const session = createSession(events, 20000);
      exporter.addOutcomeExample(session, 'You', 'WIN');
      exporter.addPlaystyleExample(session, 'You', 'AGGRESSIVE');

      const stats = exporter.exportStats();

      expect(stats.featureVersion).toBe('2.0.0');
      expect(stats.outcome.uniqueSessions).toBe(1);
      expect(stats.playstyle.uniqueSessions).toBe(1);
    });
  });

  describe('End-to-end ML Workflow', () => {
    it('should complete full prediction workflow', async () => {
      // Create analyzer with ML
      const analyzer = new CombatAnalyzer({ ml: { enabled: true, lazyLoad: true } });
      const self = createEntity('You', true);
      const enemy1 = createEntity('Enemy1');
      const enemy2 = createEntity('Enemy2');
      const baseTime = new Date('2024-01-01T12:00:00Z');

      // Create a combat session with multiple opponents
      const events: CombatEvent[] = [
        // Player deals damage to enemy1
        ...Array.from({ length: 15 }, (_, i) =>
          createDamageEvent(
            self,
            enemy1,
            100 + i * 10,
            new Date(baseTime.getTime() + i * 1000),
            `p1-${i}`,
            i % 3 === 0 ? ActionType.SPELL : ActionType.MELEE
          )
        ),
        // Enemy1 deals damage to player
        ...Array.from({ length: 8 }, (_, i) =>
          createDamageEvent(
            enemy1,
            self,
            50,
            new Date(baseTime.getTime() + i * 2000),
            `e1-${i}`
          )
        ),
        // Enemy2 deals damage to player
        ...Array.from({ length: 5 }, (_, i) =>
          createDamageEvent(
            enemy2,
            self,
            30,
            new Date(baseTime.getTime() + i * 3000),
            `e2-${i}`
          )
        ),
        // Player kills enemy1
        createDeathEvent(enemy1, self, new Date(baseTime.getTime() + 20000), 'death1'),
      ];

      const session = createSession(events, 25000, [self, enemy1, enemy2]);

      // Predict fight outcome
      const outcome = await analyzer.predictFightOutcome(session, 'You');
      expect(outcome).not.toBeNull();
      expect(outcome?.winProbability).toBeGreaterThan(0.5); // Should favor player with kill

      // Classify playstyle
      const playstyle = await analyzer.classifyPlaystyle(session, 'You');
      expect(playstyle).not.toBeNull();
      expect(playstyle?.primaryStyle).toBeDefined();

      // Assess threats
      const threats = await analyzer.assessThreats(session, self);
      expect(threats.length).toBe(2);

      // Enemy1 should have higher threat (more damage dealt before dying)
      const enemy1Threat = threats.find((t) => t.entity.name === 'Enemy1');
      const enemy2Threat = threats.find((t) => t.entity.name === 'Enemy2');
      expect(enemy1Threat?.threatLevel).toBeGreaterThan(enemy2Threat?.threatLevel ?? 0);

      // Export training data
      const exporter = new TrainingDataExporter();
      exporter.addOutcomeExample(session, 'You', 'WIN');
      exporter.addPlaystyleExample(session, 'You', playstyle?.primaryStyle ?? 'BALANCED');

      const outcomeDataset = exporter.exportOutcomeDataset();
      const playstyleDataset = exporter.exportPlaystyleDataset();

      expect(outcomeDataset.examples.length).toBeGreaterThan(0);
      expect(playstyleDataset.examples.length).toBe(1);
    });

    it('should handle multiple sessions for performance prediction', async () => {
      const analyzer = new CombatAnalyzer({ ml: { enabled: true, lazyLoad: true } });
      const self = createEntity('You', true);
      const enemy = createEntity('Enemy');
      const baseTime = new Date('2024-01-01T12:00:00Z');

      // Create multiple sessions with varying performance
      const sessions = [100, 120, 110, 130, 90].map((dps, sessionIndex) => {
        const events: CombatEvent[] = Array.from({ length: 20 }, (_, i) =>
          createDamageEvent(
            self,
            enemy,
            dps * 3, // 3 seconds per event
            new Date(baseTime.getTime() + sessionIndex * 100000 + i * 3000),
            `${sessionIndex}-${i}`
          )
        );
        return createSession(events, 60000);
      });

      const performance = await analyzer.predictPerformance(sessions, 'You');

      expect(performance).not.toBeNull();
      expect(performance?.predictedDps).toBeGreaterThan(0);
      expect(performance?.dpsRange.low).toBeLessThanOrEqual(performance?.predictedDps ?? 0);
      expect(performance?.dpsRange.high).toBeGreaterThanOrEqual(performance?.predictedDps ?? 0);
    });
  });
});
