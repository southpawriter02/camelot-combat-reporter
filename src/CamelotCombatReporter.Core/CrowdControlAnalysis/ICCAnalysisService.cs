using CamelotCombatReporter.Core.CrowdControlAnalysis.Models;
using CamelotCombatReporter.Core.Models;

namespace CamelotCombatReporter.Core.CrowdControlAnalysis;

/// <summary>
/// Service for analyzing crowd control usage and effectiveness.
/// </summary>
public interface ICCAnalysisService
{
    /// <summary>
    /// Gets the DR tracker instance.
    /// </summary>
    DRTracker DRTracker { get; }

    /// <summary>
    /// Gets or sets the maximum gap between CC effects to be considered a chain (default: 2 seconds).
    /// </summary>
    TimeSpan ChainGapThreshold { get; set; }

    /// <summary>
    /// Extracts all CC applications from a collection of events.
    /// </summary>
    /// <param name="events">All combat events from the session.</param>
    /// <returns>List of CC applications with DR information.</returns>
    IReadOnlyList<CCApplication> ExtractCCApplications(IEnumerable<LogEvent> events);

    /// <summary>
    /// Detects CC chains from a list of CC applications.
    /// </summary>
    /// <param name="applications">CC applications to analyze.</param>
    /// <returns>Detected CC chains.</returns>
    IReadOnlyList<CCChain> DetectChains(IEnumerable<CCApplication> applications);

    /// <summary>
    /// Calculates CC statistics for a combat session.
    /// </summary>
    /// <param name="events">All combat events from the session.</param>
    /// <param name="combatDuration">The total combat duration.</param>
    /// <returns>Aggregated CC statistics.</returns>
    CCStatistics CalculateStatistics(
        IEnumerable<LogEvent> events,
        TimeSpan combatDuration);

    /// <summary>
    /// Builds a timeline of CC events for visualization.
    /// </summary>
    /// <param name="events">All combat events from the session.</param>
    /// <returns>Timeline entries for CC visualization.</returns>
    IReadOnlyList<CCTimelineEntry> BuildTimeline(IEnumerable<LogEvent> events);

    /// <summary>
    /// Resets the internal DR state.
    /// </summary>
    void Reset();
}
