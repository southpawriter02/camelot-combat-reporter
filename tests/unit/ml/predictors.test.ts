/**
 * Tests for ML predictors
 */
import { FightOutcomePredictor } from '../../../src/ml/predictors/FightOutcomePredictor';
import { PlaystyleClassifier } from '../../../src/ml/predictors/PlaystyleClassifier';
import { PerformancePredictor } from '../../../src/ml/predictors/PerformancePredictor';
import { ThreatAssessor } from '../../../src/ml/predictors/ThreatAssessor';
import { MLPredictor } from '../../../src/ml/predictors/MLPredictor';
import { EntityType, EventType, ActionType, DamageType } from '../../../src/types';
import type { Entity, CombatEvent, DamageEvent } from '../../../src/types';
import type { CombatSession, SessionParticipant, SessionSummary } from '../../../src/analysis/types';

// Helper functions for creating test data
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

const createSummary = (): SessionSummary => ({
  totalDamageDealt: 0,
  totalDamageTaken: 0,
  totalHealingDone: 0,
  totalHealingReceived: 0,
  deathCount: 0,
  ccEventCount: 0,
  keyEvents: [],
});

const createDamageEvent = (
  source: Entity,
  target: Entity,
  amount: number,
  timestamp: Date,
  id: string
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
  actionType: ActionType.MELEE,
  isCritical: false,
  isBlocked: false,
  isParried: false,
  isEvaded: false,
});

const createSession = (
  events: CombatEvent[],
  durationMs = 60000,
  participants: Entity[] = []
): CombatSession => {
  const self = createEntity('You', true);
  const enemy = createEntity('Enemy');
  const allParticipants = participants.length > 0 ? participants : [self, enemy];

  return {
    id: 'test-session',
    startTime: new Date('2024-01-01T12:00:00Z'),
    endTime: new Date(new Date('2024-01-01T12:00:00Z').getTime() + durationMs),
    durationMs,
    events,
    participants: allParticipants.map((entity) => createParticipant(entity)),
    summary: createSummary(),
  };
};

describe('FightOutcomePredictor', () => {
  describe('predict', () => {
    it('should return prediction with win/loss probabilities', async () => {
      const predictor = new FightOutcomePredictor({ lazyLoad: true });
      const self = createEntity('You', true);
      const enemy = createEntity('Enemy');
      const baseTime = new Date('2024-01-01T12:00:00Z');

      const events: CombatEvent[] = Array.from({ length: 20 }, (_, i) =>
        createDamageEvent(self, enemy, 100, new Date(baseTime.getTime() + i * 1000), String(i))
      );

      const session = createSession(events, 20000);
      const prediction = await predictor.predict(session, 'You');

      expect(prediction.winProbability).toBeGreaterThanOrEqual(0);
      expect(prediction.winProbability).toBeLessThanOrEqual(1);
      expect(prediction.lossProbability).toBeGreaterThanOrEqual(0);
      expect(prediction.lossProbability).toBeLessThanOrEqual(1);
      expect(prediction.confidence).toBeGreaterThanOrEqual(0);
      expect(prediction.factors).toBeInstanceOf(Array);
      expect(prediction.isHeuristic).toBe(true);
    });

    it('should return low confidence prediction for player not in session', async () => {
      const predictor = new FightOutcomePredictor({ lazyLoad: true });
      const session = createSession([], 60000);

      const prediction = await predictor.predict(session, 'NonExistentPlayer');

      expect(prediction.winProbability).toBe(0.5);
      expect(prediction.confidence).toBe(0);
      expect(prediction.factors[0]?.name).toBe('Insufficient Data');
    });

    it('should return low confidence prediction for insufficient events', async () => {
      const predictor = new FightOutcomePredictor({ lazyLoad: true, minEventsThreshold: 50 });
      const session = createSession([], 60000);

      const prediction = await predictor.predict(session, 'You');

      expect(prediction.confidence).toBe(0);
    });
  });

  describe('predictFromFeatures', () => {
    it('should predict from pre-extracted features', async () => {
      const predictor = new FightOutcomePredictor({ lazyLoad: true });

      const input = {
        combatState: {
          elapsedTimeMs: 60000,
          elapsedTimeRatio: 1,
          selfDamageDealt: 1000,
          selfDamageTaken: 500,
          selfDamageRatio: 0.67,
          selfDps: 16.67,
          selfDtps: 8.33,
          selfHealingDone: 0,
          selfHealingReceived: 0,
          selfNetHealth: -500,
          selfCritRate: 0.1,
          selfBlockRate: 0.05,
          selfParryRate: 0.05,
          selfEvadeRate: 0.05,
          selfCCApplied: 0,
          selfCCReceived: 0,
          selfCCDurationApplied: 0,
          selfCCDurationReceived: 0,
          selfKills: 1,
          selfDeaths: 0,
          opponentCount: 1,
          opponentTotalDamageDealt: 500,
          opponentTotalHealingDone: 0,
          opponentDeaths: 1,
          allyCount: 0,
          allyTotalDamageDealt: 0,
          allyTotalHealingDone: 0,
          allyDeaths: 0,
          eventCount: 20,
          isPvP: 1,
          uniqueDamageSources: 1,
          uniqueHealingSources: 0,
        },
      };

      const prediction = await predictor.predictFromFeatures(input, 20);

      expect(prediction.winProbability).toBeGreaterThan(0.5);
      expect(prediction.factors.length).toBeGreaterThan(0);
    });
  });
});

describe('PlaystyleClassifier', () => {
  describe('classify', () => {
    it('should classify playstyle from session', async () => {
      const classifier = new PlaystyleClassifier({ lazyLoad: true });
      const self = createEntity('You', true);
      const enemy = createEntity('Enemy');
      const baseTime = new Date('2024-01-01T12:00:00Z');

      const events: CombatEvent[] = Array.from({ length: 20 }, (_, i) =>
        createDamageEvent(self, enemy, 100, new Date(baseTime.getTime() + i * 1000), String(i))
      );

      const session = createSession(events, 20000);
      const classification = await classifier.classify(session, 'You');

      expect(['AGGRESSIVE', 'DEFENSIVE', 'BALANCED', 'OPPORTUNISTIC']).toContain(
        classification.primaryStyle
      );
      expect(classification.styleScores.AGGRESSIVE).toBeDefined();
      expect(classification.styleScores.DEFENSIVE).toBeDefined();
      expect(classification.styleScores.BALANCED).toBeDefined();
      expect(classification.styleScores.OPPORTUNISTIC).toBeDefined();
      expect(classification.confidence).toBeGreaterThanOrEqual(0);
      expect(classification.isHeuristic).toBe(true);
    });

    it('should return default classification for player not in session', async () => {
      const classifier = new PlaystyleClassifier({ lazyLoad: true });
      const session = createSession([], 60000);

      const classification = await classifier.classify(session, 'NonExistentPlayer');

      expect(classification.primaryStyle).toBe('BALANCED');
      expect(classification.confidence).toBe(0);
    });
  });

  describe('classifyFromFeatures', () => {
    it('should classify from behavior features', async () => {
      const classifier = new PlaystyleClassifier({ lazyLoad: true });

      const behavior = {
        meleeRatio: 0.8,
        spellRatio: 0.1,
        styleRatio: 0.1,
        procRatio: 0,
        targetSwitchFrequency: 2,
        targetFocusScore: 0.9,
        lowHealthTargetPreference: 0.5,
        uniqueTargetsAttacked: 2,
        burstDamageFrequency: 3,
        sustainedDamageRatio: 0.5,
        peakDps: 500,
        damageVariance: 0.2,
        defensiveAbilityUsage: 0.1,
        selfHealingRatio: 0.05,
        avgTimeBetweenHits: 2000,
        mitigationRatio: 0.1,
        avgReactionTimeMs: 500,
        ccReactionTimeMs: 1000,
        killConfirmRate: 0.8,
        assistRate: 0.3,
        aggressionScore: 80,
        survivabilityScore: 50,
        teamPlayScore: 40,
        efficiencyScore: 70,
      };

      const classification = await classifier.classifyFromFeatures(behavior, 50);

      expect(['AGGRESSIVE', 'DEFENSIVE', 'BALANCED', 'OPPORTUNISTIC']).toContain(
        classification.primaryStyle
      );
      expect(classification.traits).toBeInstanceOf(Array);
    });
  });
});

describe('PerformancePredictor', () => {
  describe('predict', () => {
    it('should predict performance from multiple sessions', async () => {
      const predictor = new PerformancePredictor({ lazyLoad: true });
      const self = createEntity('You', true);
      const enemy = createEntity('Enemy');
      const baseTime = new Date('2024-01-01T12:00:00Z');

      const createSessionWithDamage = (dps: number, index: number): CombatSession => {
        const events: CombatEvent[] = Array.from({ length: 20 }, (_, i) =>
          createDamageEvent(
            self,
            enemy,
            dps * 3, // 3 seconds per event = dps * 3 damage
            new Date(baseTime.getTime() + index * 100000 + i * 3000),
            `${index}-${i}`
          )
        );
        return createSession(events, 60000);
      };

      const sessions = [
        createSessionWithDamage(100, 0),
        createSessionWithDamage(120, 1),
        createSessionWithDamage(110, 2),
      ];

      const prediction = await predictor.predict(sessions, 'You');

      expect(prediction.predictedDps).toBeGreaterThan(0);
      expect(prediction.dpsRange.low).toBeLessThanOrEqual(prediction.predictedDps);
      expect(prediction.dpsRange.high).toBeGreaterThanOrEqual(prediction.predictedDps);
      expect(prediction.confidence).toBeGreaterThanOrEqual(0);
      expect(prediction.isHeuristic).toBe(true);
    });

    it('should return low confidence for no valid sessions', async () => {
      const predictor = new PerformancePredictor({ lazyLoad: true });

      const prediction = await predictor.predict([], 'You');

      // Default prediction returns 200 DPS as average placeholder
      expect(prediction.predictedDps).toBe(200);
      expect(prediction.confidence).toBe(0);
    });
  });
});

describe('ThreatAssessor', () => {
  describe('assessAll', () => {
    it('should assess threats from session', async () => {
      const assessor = new ThreatAssessor({ lazyLoad: true });
      const self = createEntity('You', true);
      const enemy1 = createEntity('Enemy1');
      const enemy2 = createEntity('Enemy2');
      const baseTime = new Date('2024-01-01T12:00:00Z');

      const events: CombatEvent[] = [
        ...Array.from({ length: 10 }, (_, i) =>
          createDamageEvent(enemy1, self, 100, new Date(baseTime.getTime() + i * 1000), `a${i}`)
        ),
        ...Array.from({ length: 5 }, (_, i) =>
          createDamageEvent(enemy2, self, 50, new Date(baseTime.getTime() + i * 1000), `b${i}`)
        ),
      ];

      const session = createSession(events, 15000, [self, enemy1, enemy2]);
      const threats = await assessor.assessAll(session, self);

      expect(threats.length).toBeGreaterThan(0);
      expect(threats[0]?.threatLevel).toBeGreaterThanOrEqual(0);
      expect(threats[0]?.threatLevel).toBeLessThanOrEqual(100);
      expect(['LOW', 'MEDIUM', 'HIGH', 'CRITICAL']).toContain(threats[0]?.threatCategory);
      expect(threats[0]?.isHeuristic).toBe(true);
    });

    it('should return empty array for no opponents', async () => {
      const assessor = new ThreatAssessor({ lazyLoad: true });
      const self = createEntity('You', true);
      const session = createSession([], 60000, [self]);

      const threats = await assessor.assessAll(session, self);

      expect(threats).toEqual([]);
    });

    it('should sort threats by threat level descending', async () => {
      const assessor = new ThreatAssessor({ lazyLoad: true });
      const self = createEntity('You', true);
      const enemy1 = createEntity('LowThreat');
      const enemy2 = createEntity('HighThreat');
      const baseTime = new Date('2024-01-01T12:00:00Z');

      const events: CombatEvent[] = [
        createDamageEvent(enemy1, self, 10, baseTime, '1'),
        ...Array.from({ length: 10 }, (_, i) =>
          createDamageEvent(enemy2, self, 200, new Date(baseTime.getTime() + i * 1000), `h${i}`)
        ),
      ];

      const session = createSession(events, 15000, [self, enemy1, enemy2]);
      const threats = await assessor.assessAll(session, self);

      if (threats.length >= 2) {
        expect(threats[0]?.threatLevel).toBeGreaterThanOrEqual(threats[1]?.threatLevel ?? 0);
      }
    });
  });

  describe('assessSingle', () => {
    it('should assess a single entity threat', async () => {
      const assessor = new ThreatAssessor({ lazyLoad: true });
      const self = createEntity('You', true);
      const enemy = createEntity('Enemy');
      const baseTime = new Date('2024-01-01T12:00:00Z');

      const events: CombatEvent[] = Array.from({ length: 10 }, (_, i) =>
        createDamageEvent(enemy, self, 100, new Date(baseTime.getTime() + i * 1000), String(i))
      );

      const session = createSession(events, 10000, [self, enemy]);
      const threat = await assessor.assessSingle(session, self, enemy);

      expect(threat.entity.name).toBe('Enemy');
      expect(threat.threatLevel).toBeGreaterThan(0);
      expect(threat.factors.length).toBeGreaterThan(0);
    });
  });
});

describe('MLPredictor', () => {
  describe('constructor', () => {
    it('should create with default config', () => {
      const predictor = new MLPredictor();
      expect(predictor).toBeDefined();
    });

    it('should create with custom config', () => {
      const predictor = new MLPredictor({ enabled: true, lazyLoad: false });
      expect(predictor).toBeDefined();
    });
  });

  describe('getAvailableModels', () => {
    it('should return list of available models', () => {
      const predictor = new MLPredictor();
      const models = predictor.getAvailableModels();

      expect(models).toBeInstanceOf(Array);
      expect(models.length).toBeGreaterThan(0);
      expect(models[0]).toHaveProperty('name');
      expect(models[0]).toHaveProperty('version');
    });
  });

  describe('predictFightOutcome', () => {
    it('should predict fight outcome through facade', async () => {
      const predictor = new MLPredictor({ lazyLoad: true });
      const self = createEntity('You', true);
      const enemy = createEntity('Enemy');
      const baseTime = new Date('2024-01-01T12:00:00Z');

      const events: CombatEvent[] = Array.from({ length: 20 }, (_, i) =>
        createDamageEvent(self, enemy, 100, new Date(baseTime.getTime() + i * 1000), String(i))
      );

      const session = createSession(events, 20000);
      const prediction = await predictor.predictFightOutcome(session, 'You');

      expect(prediction.winProbability).toBeDefined();
      expect(prediction.factors).toBeDefined();
    });
  });

  describe('classifyPlaystyle', () => {
    it('should classify playstyle through facade', async () => {
      const predictor = new MLPredictor({ lazyLoad: true });
      const session = createSession([], 60000);

      const classification = await predictor.classifyPlaystyle(session, 'You');

      expect(classification.primaryStyle).toBeDefined();
      expect(classification.styleScores).toBeDefined();
    });
  });

  describe('predictPerformance', () => {
    it('should predict performance through facade', async () => {
      const predictor = new MLPredictor({ lazyLoad: true });
      const sessions = [createSession([], 60000)];

      const prediction = await predictor.predictPerformance(sessions, 'You');

      expect(prediction.predictedDps).toBeDefined();
      expect(prediction.dpsRange).toBeDefined();
    });
  });

  describe('assessThreats', () => {
    it('should assess threats through facade', async () => {
      const predictor = new MLPredictor({ lazyLoad: true });
      const self = createEntity('You', true);
      const session = createSession([], 60000);

      const threats = await predictor.assessThreats(session, self);

      expect(threats).toBeInstanceOf(Array);
    });
  });
});
