using CamelotCombatReporter.Core.Models;
using CamelotCombatReporter.Core.RvR.Models;

namespace CamelotCombatReporter.Core.RvR;

/// <summary>
/// Interface for tracking and analyzing battleground performance.
/// </summary>
public interface IBattlegroundService
{
    /// <summary>
    /// Gets or sets the time gap threshold for considering events as separate BG sessions.
    /// </summary>
    TimeSpan SessionGapThreshold { get; set; }

    /// <summary>
    /// Extracts zone entry events from a collection of log events.
    /// </summary>
    /// <param name="events">The log events to search.</param>
    /// <returns>A list of zone entry events ordered by timestamp.</returns>
    IReadOnlyList<ZoneEntryEvent> ExtractZoneEntries(IEnumerable<LogEvent> events);

    /// <summary>
    /// Resolves log events into distinct battleground sessions.
    /// </summary>
    /// <param name="events">All log events.</param>
    /// <param name="playerName">The player's name for statistics tracking.</param>
    /// <returns>A list of battleground sessions.</returns>
    IReadOnlyList<BattlegroundSession> ResolveSessions(IEnumerable<LogEvent> events, string playerName = "You");

    /// <summary>
    /// Calculates statistics for a single battleground session.
    /// </summary>
    /// <param name="session">The session to analyze.</param>
    /// <returns>Statistics for the session.</returns>
    BattlegroundStatistics CalculateSessionStatistics(BattlegroundSession session);

    /// <summary>
    /// Calculates aggregate statistics across all battleground sessions.
    /// </summary>
    /// <param name="sessions">The sessions to analyze.</param>
    /// <returns>Aggregate statistics for all battleground types.</returns>
    AllBattlegroundStatistics CalculateAllStatistics(IEnumerable<BattlegroundSession> sessions);

    /// <summary>
    /// Gets the battleground type for a zone name.
    /// </summary>
    /// <param name="zoneName">The zone name.</param>
    /// <returns>The battleground type, or null if not a battleground.</returns>
    BattlegroundType? GetBattlegroundType(string zoneName);
}
