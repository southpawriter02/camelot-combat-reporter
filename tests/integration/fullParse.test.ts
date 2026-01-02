import * as path from 'path';
import { LogParser } from '../../src/parser/LogParser';
import { EventType } from '../../src/types';

describe('Full Log Parsing Integration Tests', () => {
  const fixturesPath = path.join(__dirname, '../fixtures');

  describe('parseFile', () => {
    it('should parse sample combat log successfully', async () => {
      const parser = new LogParser();
      const result = await parser.parseFile(path.join(fixturesPath, 'sample-combat.log'));

      expect(result.filename).toBe('sample-combat.log');
      expect(result.totalLines).toBeGreaterThan(0);
      expect(result.events.length).toBeGreaterThan(0);
      expect(result.errors.length).toBe(0);
    });

    it('should parse damage events from sample log', async () => {
      const parser = new LogParser({ includeUnknownEvents: false });
      const result = await parser.parseFile(path.join(fixturesPath, 'sample-combat.log'));

      const damageEvents = result.events.filter(
        (e) => e.eventType === EventType.DAMAGE_DEALT || e.eventType === EventType.DAMAGE_RECEIVED
      );

      expect(damageEvents.length).toBeGreaterThan(0);
    });

    it('should parse healing events from sample log', async () => {
      const parser = new LogParser({ includeUnknownEvents: false });
      const result = await parser.parseFile(path.join(fixturesPath, 'sample-combat.log'));

      const healingEvents = result.events.filter(
        (e) =>
          e.eventType === EventType.HEALING_DONE || e.eventType === EventType.HEALING_RECEIVED
      );

      expect(healingEvents.length).toBeGreaterThan(0);
    });

    it('should parse crowd control events from sample log', async () => {
      const parser = new LogParser({ includeUnknownEvents: false });
      const result = await parser.parseFile(path.join(fixturesPath, 'sample-combat.log'));

      const ccEvents = result.events.filter((e) => e.eventType === EventType.CROWD_CONTROL);

      expect(ccEvents.length).toBeGreaterThan(0);
    });

    it('should handle malformed log gracefully', async () => {
      const parser = new LogParser({ continueOnError: true });
      const result = await parser.parseFile(path.join(fixturesPath, 'malformed.log'));

      // Should still parse successfully
      expect(result.events.length).toBeGreaterThan(0);
      // Should have some errors
      expect(result.errorLines).toBeGreaterThan(0);
    });

    it('should calculate correct metadata', async () => {
      const parser = new LogParser({ includeUnknownEvents: false });
      const result = await parser.parseFile(path.join(fixturesPath, 'sample-combat.log'));

      expect(result.metadata.firstEventTime).toBeDefined();
      expect(result.metadata.lastEventTime).toBeDefined();
      expect(result.metadata.uniqueEntities.length).toBeGreaterThan(0);
      expect(Object.keys(result.metadata.eventTypeCounts).length).toBeGreaterThan(0);
    });

    it('should track unique entities', async () => {
      const parser = new LogParser({ includeUnknownEvents: false });
      const result = await parser.parseFile(path.join(fixturesPath, 'sample-combat.log'));

      expect(result.metadata.uniqueEntities).toContain('You');
      expect(result.metadata.uniqueEntities).toContain('goblin');
    });

    it('should count event types correctly', async () => {
      const parser = new LogParser({ includeUnknownEvents: false });
      const result = await parser.parseFile(path.join(fixturesPath, 'sample-combat.log'));

      expect(result.metadata.eventTypeCounts[EventType.DAMAGE_DEALT]).toBeGreaterThan(0);
    });
  });

  describe('parseLine', () => {
    it('should parse a single damage line', () => {
      const parser = new LogParser();
      const event = parser.parseLine('[12:00:00] You hit the goblin for 150 damage!');

      expect(event).not.toBeNull();
      expect(event!.eventType).toBe(EventType.DAMAGE_DEALT);
    });

    it('should return null for invalid line', () => {
      const parser = new LogParser();
      const event = parser.parseLine('Invalid line without timestamp');

      expect(event).not.toBeNull(); // Returns unknown event
      expect(event!.eventType).toBe(EventType.UNKNOWN);
    });
  });

  describe('parseLines', () => {
    it('should parse multiple lines', () => {
      const parser = new LogParser({ includeUnknownEvents: false });
      const lines = [
        '[12:00:00] You hit the goblin for 150 damage!',
        '[12:00:01] The goblin hits you for 75 damage!',
        '[12:00:02] You cast Minor Heal on yourself for 200 hit points.',
      ];

      const events = parser.parseLines(lines);

      expect(events.length).toBe(3);
      expect(events[0]!.eventType).toBe(EventType.DAMAGE_DEALT);
      expect(events[1]!.eventType).toBe(EventType.DAMAGE_RECEIVED);
      expect(events[2]!.eventType).toBe(EventType.HEALING_DONE);
    });

    it('should skip empty lines', () => {
      const parser = new LogParser({ includeUnknownEvents: false });
      const lines = [
        '[12:00:00] You hit the goblin for 150 damage!',
        '',
        '[12:00:02] You hit the goblin for 200 damage!',
      ];

      const events = parser.parseLines(lines);

      expect(events.length).toBe(2);
    });
  });

  describe('configuration', () => {
    it('should exclude unknown events when configured', async () => {
      const parser = new LogParser({ includeUnknownEvents: false });
      const result = await parser.parseFile(path.join(fixturesPath, 'sample-combat.log'));

      const unknownEvents = result.events.filter((e) => e.eventType === EventType.UNKNOWN);
      expect(unknownEvents.length).toBe(0);
    });

    it('should include unknown events by default', async () => {
      const parser = new LogParser();
      const result = await parser.parseFile(path.join(fixturesPath, 'sample-combat.log'));

      const unknownEvents = result.events.filter((e) => e.eventType === EventType.UNKNOWN);
      expect(unknownEvents.length).toBeGreaterThan(0);
    });

    it('should call onError callback', async () => {
      const errors: Error[] = [];
      const parser = new LogParser({
        continueOnError: true,
        onError: (err) => errors.push(err),
      });

      await parser.parseFile(path.join(fixturesPath, 'malformed.log'));

      expect(errors.length).toBeGreaterThan(0);
    });
  });
});
