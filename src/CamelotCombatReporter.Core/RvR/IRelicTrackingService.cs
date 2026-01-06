using CamelotCombatReporter.Core.Models;
using CamelotCombatReporter.Core.RvR.Models;

namespace CamelotCombatReporter.Core.RvR;

/// <summary>
/// Interface for tracking and analyzing relic raids.
/// </summary>
public interface IRelicTrackingService
{
    /// <summary>
    /// Gets or sets the time gap threshold for considering events as separate raid sessions.
    /// </summary>
    TimeSpan SessionGapThreshold { get; set; }

    /// <summary>
    /// Extracts all relic-related events from a collection of log events.
    /// </summary>
    /// <param name="events">The log events to search.</param>
    /// <returns>A list of relic events ordered by timestamp.</returns>
    IReadOnlyList<RelicEvent> ExtractRelicEvents(IEnumerable<LogEvent> events);

    /// <summary>
    /// Resolves relic events into distinct raid sessions.
    /// </summary>
    /// <param name="events">All log events.</param>
    /// <param name="playerName">The player's name for contribution tracking.</param>
    /// <returns>A list of relic raid sessions.</returns>
    IReadOnlyList<RelicRaidSession> ResolveSessions(IEnumerable<LogEvent> events, string playerName = "You");

    /// <summary>
    /// Gets the current status of all relics based on log events.
    /// </summary>
    /// <param name="events">The log events to analyze.</param>
    /// <returns>A dictionary of relic names to their current status.</returns>
    IReadOnlyDictionary<string, RelicStatus> GetRelicStatuses(IEnumerable<LogEvent> events);

    /// <summary>
    /// Calculates player contribution for a set of relic-related events.
    /// </summary>
    /// <param name="events">The events to analyze.</param>
    /// <param name="playerName">The player's name.</param>
    /// <returns>The player's contribution to the relic raid.</returns>
    RelicContribution CalculateContribution(IEnumerable<LogEvent> events, string playerName = "You");

    /// <summary>
    /// Calculates aggregate statistics across multiple relic raid sessions.
    /// </summary>
    /// <param name="sessions">The sessions to analyze.</param>
    /// <returns>Aggregate statistics for all sessions.</returns>
    RelicRaidStatistics CalculateStatistics(IEnumerable<RelicRaidSession> sessions);

    /// <summary>
    /// Gets information about relic carriers from log events.
    /// </summary>
    /// <param name="events">The log events to analyze.</param>
    /// <returns>A dictionary of carrier names to their carry statistics.</returns>
    IReadOnlyDictionary<string, CarrierStatistics> GetCarrierStatistics(IEnumerable<LogEvent> events);
}

/// <summary>
/// Statistics for a relic carrier.
/// </summary>
/// <param name="CarrierName">The name of the carrier.</param>
/// <param name="RelicsCarried">Number of relics carried.</param>
/// <param name="SuccessfulDeliveries">Number of successful deliveries.</param>
/// <param name="DropsFromDeath">Number of times the carrier dropped the relic due to death.</param>
/// <param name="TotalCarryTime">Total time spent carrying relics.</param>
public record CarrierStatistics(
    string CarrierName,
    int RelicsCarried,
    int SuccessfulDeliveries,
    int DropsFromDeath,
    TimeSpan TotalCarryTime
);
