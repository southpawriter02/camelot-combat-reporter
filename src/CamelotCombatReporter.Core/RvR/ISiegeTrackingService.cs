using CamelotCombatReporter.Core.Models;
using CamelotCombatReporter.Core.RvR.Models;

namespace CamelotCombatReporter.Core.RvR;

/// <summary>
/// Service interface for tracking and analyzing keep sieges.
/// </summary>
public interface ISiegeTrackingService
{
    /// <summary>
    /// The time gap (in seconds) between events to consider as a new siege session.
    /// </summary>
    TimeSpan SessionGapThreshold { get; set; }

    /// <summary>
    /// Extracts siege-related events from a collection of log events.
    /// </summary>
    /// <param name="events">The log events to filter.</param>
    /// <returns>A list of siege events.</returns>
    IReadOnlyList<SiegeEvent> ExtractSiegeEvents(IEnumerable<LogEvent> events);

    /// <summary>
    /// Resolves continuous siege sessions from events.
    /// </summary>
    /// <param name="events">The log events to analyze.</param>
    /// <param name="playerName">The player name (for contribution tracking).</param>
    /// <returns>A list of detected siege sessions.</returns>
    IReadOnlyList<SiegeSession> ResolveSessions(IEnumerable<LogEvent> events, string playerName = "You");

    /// <summary>
    /// Detects the current siege phase based on events in a session.
    /// </summary>
    /// <param name="session">The siege session to analyze.</param>
    /// <returns>The detected siege phase.</returns>
    SiegePhase DetectPhase(SiegeSession session);

    /// <summary>
    /// Calculates player contribution metrics for a siege session.
    /// </summary>
    /// <param name="events">Events from the siege.</param>
    /// <param name="playerName">The player name.</param>
    /// <returns>The calculated contribution metrics.</returns>
    SiegeContribution CalculateContribution(IEnumerable<LogEvent> events, string playerName = "You");

    /// <summary>
    /// Calculates aggregate siege statistics across multiple sessions.
    /// </summary>
    /// <param name="sessions">The siege sessions to aggregate.</param>
    /// <returns>Aggregate statistics.</returns>
    SiegeStatistics CalculateStatistics(IEnumerable<SiegeSession> sessions);

    /// <summary>
    /// Builds a timeline of events for visualization.
    /// </summary>
    /// <param name="session">The siege session.</param>
    /// <returns>A list of timeline entries.</returns>
    IReadOnlyList<SiegeTimelineEntry> BuildTimeline(SiegeSession session);
}
