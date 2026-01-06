using CamelotCombatReporter.Core.Models;

namespace CamelotCombatReporter.Core.InstanceTracking;

/// <summary>
/// Service for resolving combat events into distinct target instances.
/// Differentiates between multiple mobs with the same name by tracking
/// death events and time gaps between encounters.
/// </summary>
public interface ICombatInstanceResolver
{
    /// <summary>
    /// Processes a collection of log events and groups them into distinct combat encounters.
    /// </summary>
    /// <param name="events">The log events to process (should be in chronological order).</param>
    /// <param name="playerName">The player's name for identifying outgoing vs incoming damage.</param>
    /// <returns>Statistics grouped by target name, with individual encounter breakdowns.</returns>
    IReadOnlyList<TargetTypeStatistics> ResolveInstances(
        IReadOnlyList<LogEvent> events,
        string? playerName = null);

    /// <summary>
    /// Gets all combat encounters from the resolved instances.
    /// </summary>
    /// <param name="events">The log events to process.</param>
    /// <param name="playerName">The player's name.</param>
    /// <returns>All encounters in chronological order.</returns>
    IReadOnlyList<CombatEncounter> GetAllEncounters(
        IReadOnlyList<LogEvent> events,
        string? playerName = null);

    /// <summary>
    /// Configuration for encounter timeout threshold.
    /// Time gap after which a new instance is created for the same target name.
    /// Default is 15 seconds.
    /// </summary>
    TimeSpan EncounterTimeoutThreshold { get; set; }

    /// <summary>
    /// Configuration for combat idle timeout.
    /// Time after which an encounter is considered ended if no death event received.
    /// Default is 30 seconds.
    /// </summary>
    TimeSpan CombatIdleTimeout { get; set; }
}
