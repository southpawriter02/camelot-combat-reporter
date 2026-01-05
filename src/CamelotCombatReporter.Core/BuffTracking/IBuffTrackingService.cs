using CamelotCombatReporter.Core.BuffTracking.Models;
using CamelotCombatReporter.Core.Models;

namespace CamelotCombatReporter.Core.BuffTracking;

/// <summary>
/// Service for tracking and analyzing buff/debuff usage.
/// </summary>
public interface IBuffTrackingService
{
    /// <summary>
    /// Gets the underlying state tracker.
    /// </summary>
    BuffStateTracker StateTracker { get; }

    /// <summary>
    /// Gets or sets the list of buff IDs to track for uptime (expected buffs).
    /// </summary>
    IReadOnlyList<string> ExpectedBuffIds { get; set; }

    /// <summary>
    /// Extracts buff events from log events.
    /// </summary>
    /// <param name="events">Log events to analyze.</param>
    /// <returns>Extracted buff events.</returns>
    IReadOnlyList<BuffEvent> ExtractBuffEvents(IEnumerable<LogEvent> events);

    /// <summary>
    /// Builds a timeline of buff events.
    /// </summary>
    /// <param name="events">Log events to analyze.</param>
    /// <returns>Timeline entries for visualization.</returns>
    IReadOnlyList<BuffTimelineEntry> BuildTimeline(IEnumerable<LogEvent> events);

    /// <summary>
    /// Calculates buff statistics.
    /// </summary>
    /// <param name="events">Log events to analyze.</param>
    /// <param name="combatDuration">Total combat duration.</param>
    /// <param name="targetName">Optional target filter.</param>
    /// <returns>Buff statistics.</returns>
    BuffStatistics CalculateStatistics(
        IEnumerable<LogEvent> events,
        TimeSpan combatDuration,
        string? targetName = null);

    /// <summary>
    /// Calculates uptime for specific buffs.
    /// </summary>
    /// <param name="events">Log events to analyze.</param>
    /// <param name="buffIds">Buff IDs to calculate uptime for.</param>
    /// <param name="combatDuration">Total combat duration.</param>
    /// <param name="targetName">Optional target filter.</param>
    /// <returns>Uptime stats per buff.</returns>
    IReadOnlyList<BuffUptimeStats> CalculateUptimes(
        IEnumerable<LogEvent> events,
        IEnumerable<string> buffIds,
        TimeSpan combatDuration,
        string? targetName = null);

    /// <summary>
    /// Detects critical gaps in expected buff uptime.
    /// </summary>
    /// <param name="events">Log events to analyze.</param>
    /// <param name="gapThreshold">Minimum gap duration to consider critical.</param>
    /// <returns>List of critical gaps.</returns>
    IReadOnlyList<BuffGap> DetectCriticalGaps(
        IEnumerable<LogEvent> events,
        TimeSpan gapThreshold);

    /// <summary>
    /// Gets active buffs at a specific timestamp.
    /// </summary>
    /// <param name="events">Log events to analyze.</param>
    /// <param name="timestamp">Timestamp to check.</param>
    /// <param name="targetName">Optional target filter.</param>
    /// <returns>Active buffs at the timestamp.</returns>
    IReadOnlyList<ActiveBuff> GetActiveBuffsAt(
        IEnumerable<LogEvent> events,
        TimeOnly timestamp,
        string? targetName = null);

    /// <summary>
    /// Gets missing expected buffs at a specific timestamp.
    /// </summary>
    /// <param name="events">Log events to analyze.</param>
    /// <param name="timestamp">Timestamp to check.</param>
    /// <param name="targetName">Target to check.</param>
    /// <returns>List of expected but missing buff IDs.</returns>
    IReadOnlyList<string> GetMissingExpectedBuffs(
        IEnumerable<LogEvent> events,
        TimeOnly timestamp,
        string targetName);

    /// <summary>
    /// Resets the service state.
    /// </summary>
    void Reset();
}
