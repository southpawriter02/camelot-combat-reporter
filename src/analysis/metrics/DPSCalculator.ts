import type { DamageEvent, HealingEvent } from '../../types/index.js';

/**
 * Point in a DPS/HPS timeline
 */
export interface TimelinePoint {
  timestamp: Date;
  value: number;
  cumulativeAmount: number;
}

/**
 * Result of peak calculation
 */
export interface PeakResult {
  value: number;
  windowStart: Date;
  windowEnd: Date;
}

/**
 * Calculates rolling DPS/HPS using sliding window algorithm
 */
export class DPSCalculator {
  private windowMs: number;

  constructor(windowMs: number = 5000) {
    this.windowMs = windowMs;
  }

  /**
   * Calculate average DPS over entire duration
   */
  calculateAverageDPS(events: DamageEvent[], durationMs: number): number {
    if (events.length === 0 || durationMs <= 0) {
      return 0;
    }

    const totalDamage = events.reduce((sum, e) => sum + e.effectiveAmount, 0);
    const durationSeconds = durationMs / 1000;

    return totalDamage / Math.max(durationSeconds, 1);
  }

  /**
   * Calculate peak DPS using sliding window
   */
  calculatePeakDPS(events: DamageEvent[]): PeakResult {
    return this.calculatePeak(
      events.map((e) => ({ timestamp: e.timestamp, amount: e.effectiveAmount }))
    );
  }

  /**
   * Calculate average HPS over entire duration
   */
  calculateAverageHPS(events: HealingEvent[], durationMs: number): number {
    if (events.length === 0 || durationMs <= 0) {
      return 0;
    }

    const totalHealing = events.reduce((sum, e) => sum + e.effectiveAmount, 0);
    const durationSeconds = durationMs / 1000;

    return totalHealing / Math.max(durationSeconds, 1);
  }

  /**
   * Calculate peak HPS using sliding window
   */
  calculatePeakHPS(events: HealingEvent[]): PeakResult {
    return this.calculatePeak(
      events.map((e) => ({ timestamp: e.timestamp, amount: e.effectiveAmount }))
    );
  }

  /**
   * Generic peak calculation using sliding window two-pointer technique
   */
  private calculatePeak(
    items: Array<{ timestamp: Date; amount: number }>
  ): PeakResult {
    if (items.length === 0) {
      const now = new Date();
      return { value: 0, windowStart: now, windowEnd: now };
    }

    // Sort by timestamp
    const sorted = [...items].sort(
      (a, b) => a.timestamp.getTime() - b.timestamp.getTime()
    );

    let maxValue = 0;
    let maxStart = sorted[0]!.timestamp;
    let maxEnd = sorted[0]!.timestamp;

    let windowAmount = 0;
    let left = 0;

    for (let right = 0; right < sorted.length; right++) {
      windowAmount += sorted[right]!.amount;

      // Shrink window from left if too large
      while (
        sorted[right]!.timestamp.getTime() - sorted[left]!.timestamp.getTime() >
        this.windowMs
      ) {
        windowAmount -= sorted[left]!.amount;
        left++;
      }

      // Calculate rate for current window
      const windowDuration =
        (sorted[right]!.timestamp.getTime() - sorted[left]!.timestamp.getTime()) / 1000;

      // Use minimum of 1 second to avoid division issues
      const rate = windowAmount / Math.max(windowDuration, 1);

      if (rate > maxValue) {
        maxValue = rate;
        maxStart = sorted[left]!.timestamp;
        maxEnd = sorted[right]!.timestamp;
      }
    }

    return { value: maxValue, windowStart: maxStart, windowEnd: maxEnd };
  }

  /**
   * Calculate DPS at regular intervals (for graphing)
   */
  calculateDPSTimeline(events: DamageEvent[], intervalMs: number = 1000): TimelinePoint[] {
    return this.calculateTimeline(
      events.map((e) => ({ timestamp: e.timestamp, amount: e.effectiveAmount })),
      intervalMs
    );
  }

  /**
   * Calculate HPS at regular intervals (for graphing)
   */
  calculateHPSTimeline(events: HealingEvent[], intervalMs: number = 1000): TimelinePoint[] {
    return this.calculateTimeline(
      events.map((e) => ({ timestamp: e.timestamp, amount: e.effectiveAmount })),
      intervalMs
    );
  }

  /**
   * Generic timeline calculation
   */
  private calculateTimeline(
    items: Array<{ timestamp: Date; amount: number }>,
    intervalMs: number
  ): TimelinePoint[] {
    if (items.length === 0) {
      return [];
    }

    const sorted = [...items].sort(
      (a, b) => a.timestamp.getTime() - b.timestamp.getTime()
    );

    const startTime = sorted[0]!.timestamp.getTime();
    const endTime = sorted[sorted.length - 1]!.timestamp.getTime();
    const timeline: TimelinePoint[] = [];

    let cumulativeAmount = 0;
    let itemIndex = 0;

    for (let time = startTime; time <= endTime; time += intervalMs) {
      // Add all items up to this point
      while (
        itemIndex < sorted.length &&
        sorted[itemIndex]!.timestamp.getTime() <= time
      ) {
        cumulativeAmount += sorted[itemIndex]!.amount;
        itemIndex++;
      }

      // Calculate DPS for the window ending at this time
      const windowStart = Math.max(startTime, time - this.windowMs);
      let windowAmount = 0;

      for (const item of sorted) {
        const itemTime = item.timestamp.getTime();
        if (itemTime >= windowStart && itemTime <= time) {
          windowAmount += item.amount;
        }
      }

      const windowDuration = Math.min(time - startTime, this.windowMs) / 1000;
      const value = windowDuration > 0 ? windowAmount / windowDuration : 0;

      timeline.push({
        timestamp: new Date(time),
        value,
        cumulativeAmount,
      });
    }

    return timeline;
  }

  /**
   * Update the window size
   */
  setWindowMs(windowMs: number): void {
    this.windowMs = windowMs;
  }

  /**
   * Get the current window size
   */
  getWindowMs(): number {
    return this.windowMs;
  }
}
