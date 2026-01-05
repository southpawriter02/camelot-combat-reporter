using CamelotCombatReporter.Core.Comparison.Models;

namespace CamelotCombatReporter.Core.Comparison;

/// <summary>
/// Event args for new personal best events.
/// </summary>
public record PersonalBestEventArgs(PersonalBest NewBest, PersonalBest? PreviousBest);

/// <summary>
/// Service for tracking personal best records.
/// </summary>
public interface IPersonalBestTracker
{
    /// <summary>
    /// Gets all current personal bests.
    /// </summary>
    IReadOnlyDictionary<string, PersonalBest> CurrentBests { get; }

    /// <summary>
    /// Checks a value against the current best and updates if it's a new record.
    /// </summary>
    /// <param name="metricName">Name of the metric.</param>
    /// <param name="value">Value to check.</param>
    /// <param name="sessionId">ID of the session where this value was achieved.</param>
    /// <returns>The new PersonalBest record if it's a new best, null otherwise.</returns>
    PersonalBest? CheckAndUpdateBest(string metricName, double value, Guid sessionId);

    /// <summary>
    /// Gets historical personal bests for a metric.
    /// </summary>
    /// <param name="metricName">Name of the metric.</param>
    /// <param name="count">Maximum number of records to return.</param>
    IReadOnlyList<PersonalBest> GetBestHistory(string metricName, int count = 10);

    /// <summary>
    /// Gets the most recent personal bests achieved.
    /// </summary>
    /// <param name="count">Maximum number of records to return.</param>
    IReadOnlyList<PersonalBest> GetRecentBests(int count = 10);

    /// <summary>
    /// Loads personal bests from storage.
    /// </summary>
    Task LoadAsync();

    /// <summary>
    /// Saves personal bests to storage.
    /// </summary>
    Task SaveAsync();

    /// <summary>
    /// Raised when a new personal best is achieved.
    /// </summary>
    event EventHandler<PersonalBestEventArgs>? NewPersonalBest;
}
