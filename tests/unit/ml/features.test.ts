/**
 * Tests for ML feature extraction pipeline
 */
import { FeatureExtractor } from '../../../src/ml/features';
import { Normalizer } from '../../../src/ml/features/Normalizer';
import { CombatFeaturesExtractor } from '../../../src/ml/features/CombatFeatures';
import { BehaviorFeaturesExtractor } from '../../../src/ml/features/BehaviorFeatures';
import { EntityType, EventType, ActionType, DamageType } from '../../../src/types';
import type { Entity, CombatEvent, DamageEvent, DeathEvent } from '../../../src/types';
import type { CombatSession, SessionParticipant, SessionSummary } from '../../../src/analysis/types';

describe('Normalizer', () => {
  describe('normalizeValue', () => {
    it('should normalize values between 0 and 1 with minmax', () => {
      const normalizer = new Normalizer('minmax', {
        testFeature: { min: 0, max: 100, mean: 50, std: 25 },
      });
      expect(normalizer.normalizeValue('testFeature', 50)).toBeCloseTo(0.5);
      expect(normalizer.normalizeValue('testFeature', 0)).toBeCloseTo(0);
      expect(normalizer.normalizeValue('testFeature', 100)).toBeCloseTo(1);
    });

    it('should clamp values outside range', () => {
      const normalizer = new Normalizer('minmax', {
        testFeature: { min: 0, max: 100, mean: 50, std: 25 },
      });
      expect(normalizer.normalizeValue('testFeature', -10)).toBe(0);
      expect(normalizer.normalizeValue('testFeature', 150)).toBe(1);
    });

    it('should handle constant feature (zero range)', () => {
      const normalizer = new Normalizer('minmax', {
        testFeature: { min: 50, max: 50, mean: 50, std: 0 },
      });
      expect(normalizer.normalizeValue('testFeature', 50)).toBe(0.5);
    });
  });

  describe('zscore normalization', () => {
    it('should return 0 for mean value', () => {
      const normalizer = new Normalizer('zscore', {
        testFeature: { min: 0, max: 100, mean: 50, std: 10 },
      });
      expect(normalizer.normalizeValue('testFeature', 50)).toBe(0);
    });

    it('should return positive z-score for above-mean values', () => {
      const normalizer = new Normalizer('zscore', {
        testFeature: { min: 0, max: 100, mean: 50, std: 10 },
      });
      expect(normalizer.normalizeValue('testFeature', 60)).toBe(1);
    });

    it('should return negative z-score for below-mean values', () => {
      const normalizer = new Normalizer('zscore', {
        testFeature: { min: 0, max: 100, mean: 50, std: 10 },
      });
      expect(normalizer.normalizeValue('testFeature', 40)).toBe(-1);
    });

    it('should handle zero std deviation', () => {
      const normalizer = new Normalizer('zscore', {
        testFeature: { min: 50, max: 50, mean: 50, std: 0 },
      });
      expect(normalizer.normalizeValue('testFeature', 50)).toBe(0);
    });
  });

  describe('normalize', () => {
    it('should normalize a feature object to vector', () => {
      const normalizer = new Normalizer('minmax', {
        feature1: { min: 0, max: 100, mean: 50, std: 25 },
        feature2: { min: 0, max: 200, mean: 100, std: 50 },
      });

      const result = normalizer.normalize(
        { feature1: 50, feature2: 100 },
        ['feature1', 'feature2']
      );

      expect(result.values[0]).toBeCloseTo(0.5);
      expect(result.values[1]).toBeCloseTo(0.5);
      expect(result.featureNames).toEqual(['feature1', 'feature2']);
    });
  });

  describe('computeStats', () => {
    it('should compute statistics from data', () => {
      const data = [
        { value: 10 },
        { value: 20 },
        { value: 30 },
      ];

      const stats = Normalizer.computeStats(data, ['value']);

      expect(stats.value?.min).toBe(10);
      expect(stats.value?.max).toBe(30);
      expect(stats.value?.mean).toBe(20);
    });
  });
});

describe('CombatFeaturesExtractor', () => {
  const createEntity = (name: string, isSelf = false): Entity => ({
    name,
    entityType: isSelf ? EntityType.SELF : EntityType.PLAYER,
    isPlayer: true,
    isSelf,
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

  describe('extract', () => {
    it('should extract features from combat events', () => {
      const extractor = new CombatFeaturesExtractor();
      const selfEntity = createEntity('You', true);
      const opponent = createEntity('Enemy');
      const baseTime = new Date('2024-01-01T12:00:00Z');

      const events: CombatEvent[] = [
        createDamageEvent(selfEntity, opponent, 100, baseTime, '1'),
        createDamageEvent(opponent, selfEntity, 50, new Date(baseTime.getTime() + 1000), '2'),
        createDamageEvent(selfEntity, opponent, 150, new Date(baseTime.getTime() + 2000), '3'),
      ];

      const features = extractor.extract(events, selfEntity, 3000);

      expect(features.selfDamageDealt).toBe(250);
      expect(features.selfDamageTaken).toBe(50);
      expect(features.selfDamageRatio).toBeGreaterThan(0.5);
      expect(features.opponentCount).toBe(1);
      expect(features.eventCount).toBe(3);
    });

    it('should handle empty events array', () => {
      const extractor = new CombatFeaturesExtractor();
      const selfEntity = createEntity('You', true);

      const features = extractor.extract([], selfEntity, 0);

      expect(features.selfDamageDealt).toBe(0);
      expect(features.selfDamageTaken).toBe(0);
      expect(features.selfDamageRatio).toBe(0.5);
      expect(features.eventCount).toBe(0);
    });

    it('should count kills and deaths correctly', () => {
      const extractor = new CombatFeaturesExtractor();
      const selfEntity = createEntity('You', true);
      const opponent = createEntity('Enemy');
      const baseTime = new Date('2024-01-01T12:00:00Z');

      const events: CombatEvent[] = [
        createDeathEvent(opponent, selfEntity, baseTime, '1'),
        createDeathEvent(selfEntity, opponent, new Date(baseTime.getTime() + 5000), '2'),
      ];

      const features = extractor.extract(events, selfEntity, 6000);

      expect(features.selfKills).toBe(1);
      expect(features.selfDeaths).toBe(1);
    });
  });
});

describe('BehaviorFeaturesExtractor', () => {
  const createEntity = (name: string, isSelf = false): Entity => ({
    name,
    entityType: isSelf ? EntityType.SELF : EntityType.PLAYER,
    isPlayer: true,
    isSelf,
  });

  const createDamageEvent = (
    source: Entity,
    target: Entity,
    amount: number,
    actionType: ActionType,
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
    actionType,
    isCritical: false,
    isBlocked: false,
    isParried: false,
    isEvaded: false,
  });

  describe('extract', () => {
    it('should calculate action type ratios correctly', () => {
      const extractor = new BehaviorFeaturesExtractor();
      const selfEntity = createEntity('You', true);
      const opponent = createEntity('Enemy');
      const baseTime = new Date('2024-01-01T12:00:00Z');

      const events: CombatEvent[] = [
        createDamageEvent(selfEntity, opponent, 100, ActionType.MELEE, baseTime, '1'),
        createDamageEvent(selfEntity, opponent, 100, ActionType.MELEE, new Date(baseTime.getTime() + 1000), '2'),
        createDamageEvent(selfEntity, opponent, 100, ActionType.SPELL, new Date(baseTime.getTime() + 2000), '3'),
        createDamageEvent(selfEntity, opponent, 100, ActionType.STYLE, new Date(baseTime.getTime() + 3000), '4'),
      ];

      const features = extractor.extract(events, selfEntity, 4000);

      expect(features.meleeRatio).toBe(0.5); // 2 out of 4
      expect(features.spellRatio).toBe(0.25); // 1 out of 4
      expect(features.styleRatio).toBe(0.25); // 1 out of 4
    });

    it('should handle empty events array', () => {
      const extractor = new BehaviorFeaturesExtractor();
      const selfEntity = createEntity('You', true);

      const features = extractor.extract([], selfEntity, 0);

      expect(features.meleeRatio).toBe(0);
      expect(features.spellRatio).toBe(0);
      expect(features.styleRatio).toBe(0);
      // With no events: damageRatio=20 + switch=0 + burst=0 + heal=20 = 40
      expect(features.aggressionScore).toBe(40);
    });

    it('should track unique targets attacked', () => {
      const extractor = new BehaviorFeaturesExtractor();
      const selfEntity = createEntity('You', true);
      const opponent1 = createEntity('Enemy1');
      const opponent2 = createEntity('Enemy2');
      const baseTime = new Date('2024-01-01T12:00:00Z');

      const events: CombatEvent[] = [
        createDamageEvent(selfEntity, opponent1, 100, ActionType.MELEE, baseTime, '1'),
        createDamageEvent(selfEntity, opponent2, 100, ActionType.MELEE, new Date(baseTime.getTime() + 1000), '2'),
        createDamageEvent(selfEntity, opponent1, 100, ActionType.MELEE, new Date(baseTime.getTime() + 2000), '3'),
      ];

      const features = extractor.extract(events, selfEntity, 3000);

      expect(features.uniqueTargetsAttacked).toBe(2);
    });
  });
});

describe('FeatureExtractor', () => {
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

  const createSession = (events: CombatEvent[], durationMs: number): CombatSession => ({
    id: 'test-session',
    startTime: new Date('2024-01-01T12:00:00Z'),
    endTime: new Date('2024-01-01T12:01:00Z'),
    durationMs,
    events,
    participants: [
      createParticipant(createEntity('You', true)),
      createParticipant(createEntity('Enemy')),
    ],
    summary: createSummary(),
  });

  describe('extractFromSession', () => {
    it('should extract combined features from a session', () => {
      const extractor = new FeatureExtractor();
      const session = createSession([], 60000);

      const features = extractor.extractFromSession(session, 'You');

      expect(features).not.toBeNull();
      expect(features?.combatState).toBeDefined();
      expect(features?.behavior).toBeDefined();
    });

    it('should return null for non-existent player', () => {
      const extractor = new FeatureExtractor();
      const session = createSession([], 60000);

      const features = extractor.extractFromSession(session, 'NonExistentPlayer');

      expect(features).toBeNull();
    });
  });

  describe('extractCombatState', () => {
    it('should extract combat state features directly', () => {
      const extractor = new FeatureExtractor();
      const selfEntity = createEntity('You', true);

      const features = extractor.extractCombatState([], selfEntity, 0);

      expect(features.selfDamageDealt).toBe(0);
      expect(features.eventCount).toBe(0);
    });
  });

  describe('extractBehavior', () => {
    it('should extract behavior features directly', () => {
      const extractor = new FeatureExtractor();
      const selfEntity = createEntity('You', true);

      const features = extractor.extractBehavior([], selfEntity, 0);

      expect(features.meleeRatio).toBe(0);
      // With no events: damageRatio=20 + switch=0 + burst=0 + heal=20 = 40
      expect(features.aggressionScore).toBe(40);
    });
  });
});
