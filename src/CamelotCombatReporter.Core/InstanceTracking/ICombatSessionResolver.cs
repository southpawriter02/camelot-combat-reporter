using CamelotCombatReporter.Core.Models;

namespace CamelotCombatReporter.Core.InstanceTracking;

/// <summary>
/// Service for resolving combat events into distinct combat sessions.
/// Sessions are bounded by combat mode changes, rest periods, or significant time gaps.
/// </summary>
public interface ICombatSessionResolver
{
    /// <summary>
    /// Processes a collection of log events and groups them into combat sessions.
    /// </summary>
    /// <param name="events">The log events to process (should be in chronological order).</param>
    /// <param name="playerName">The player's name for context.</param>
    /// <returns>All combat sessions in chronological order.</returns>
    IReadOnlyList<CombatSession> ResolveSessions(
        IReadOnlyList<LogEvent> events,
        string? playerName = null);

    /// <summary>
    /// Gets aggregated statistics across all sessions.
    /// </summary>
    SessionStatistics GetSessionStatistics(
        IReadOnlyList<LogEvent> events,
        string? playerName = null);

    /// <summary>
    /// Time gap after which a new session is created (default: 60 seconds).
    /// </summary>
    TimeSpan SessionTimeoutThreshold { get; set; }

    /// <summary>
    /// Whether to start a new session when the player rests (sits down).
    /// Default: true.
    /// </summary>
    bool SplitOnRest { get; set; }

    /// <summary>
    /// Whether to start a new session on combat mode enter events.
    /// Default: true.
    /// </summary>
    bool SplitOnCombatModeEnter { get; set; }
}
