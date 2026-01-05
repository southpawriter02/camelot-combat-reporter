namespace CamelotCombatReporter.Core.Comparison.Models;

/// <summary>
/// Direction of change between two values.
/// </summary>
public enum ChangeDirection
{
    /// <summary>Value improved (better performance).</summary>
    Improved,
    /// <summary>Value declined (worse performance).</summary>
    Declined,
    /// <summary>Value unchanged (within threshold).</summary>
    Unchanged,
    /// <summary>New metric not present in baseline.</summary>
    NewMetric
}

/// <summary>
/// Represents the change in a single metric between two sessions.
/// </summary>
/// <param name="MetricName">Name of the metric.</param>
/// <param name="Category">Category of the metric (e.g., Damage, Healing, Combat).</param>
/// <param name="BaseValue">Value from the base/older session.</param>
/// <param name="CompareValue">Value from the comparison/newer session.</param>
/// <param name="AbsoluteChange">Absolute difference (CompareValue - BaseValue).</param>
/// <param name="PercentChange">Percentage change from base value.</param>
/// <param name="Direction">Whether this change represents improvement or decline.</param>
/// <param name="FormattedBase">Human-readable formatted base value.</param>
/// <param name="FormattedCompare">Human-readable formatted compare value.</param>
/// <param name="FormattedChange">Human-readable formatted change (e.g., "+15.2%").</param>
/// <param name="IsSignificant">Whether the change exceeds the significance threshold.</param>
public record MetricDelta(
    string MetricName,
    string Category,
    double BaseValue,
    double CompareValue,
    double AbsoluteChange,
    double PercentChange,
    ChangeDirection Direction,
    string FormattedBase,
    string FormattedCompare,
    string FormattedChange,
    bool IsSignificant
);

/// <summary>
/// Summary of a combat session for comparison purposes.
/// </summary>
/// <param name="SessionId">Unique identifier for the session.</param>
/// <param name="SessionDate">When the session occurred.</param>
/// <param name="Duration">Total duration of the session.</param>
/// <param name="TotalDamageDealt">Total damage dealt by the player.</param>
/// <param name="TotalDamageReceived">Total damage received by the player.</param>
/// <param name="TotalHealingDone">Total healing done by the player.</param>
/// <param name="TotalHealingReceived">Total healing received by the player.</param>
/// <param name="Kills">Number of kills.</param>
/// <param name="Deaths">Number of deaths.</param>
/// <param name="Assists">Number of assists.</param>
/// <param name="DamagePerSecond">Average damage per second.</param>
/// <param name="HealingPerSecond">Average healing per second.</param>
/// <param name="KillDeathRatio">Kill to death ratio.</param>
/// <param name="CustomMetrics">Additional custom metrics.</param>
public record SessionSummary(
    Guid SessionId,
    DateTime SessionDate,
    TimeSpan Duration,
    int TotalDamageDealt,
    int TotalDamageReceived,
    int TotalHealingDone,
    int TotalHealingReceived,
    int Kills,
    int Deaths,
    int Assists,
    double DamagePerSecond,
    double HealingPerSecond,
    double KillDeathRatio,
    IReadOnlyDictionary<string, double> CustomMetrics
);

/// <summary>
/// Complete comparison between two sessions.
/// </summary>
/// <param name="Id">Unique identifier for this comparison.</param>
/// <param name="BaseSession">The baseline/older session.</param>
/// <param name="CompareSession">The comparison/newer session.</param>
/// <param name="Deltas">All metric deltas.</param>
/// <param name="TimeBetweenSessions">Time elapsed between the two sessions.</param>
/// <param name="ComparisonSummary">Human-readable summary of the comparison.</param>
/// <param name="DeltasByCategory">Deltas grouped by category.</param>
public record SessionComparison(
    Guid Id,
    SessionSummary BaseSession,
    SessionSummary CompareSession,
    IReadOnlyList<MetricDelta> Deltas,
    TimeSpan TimeBetweenSessions,
    string ComparisonSummary,
    IReadOnlyDictionary<string, IReadOnlyList<MetricDelta>> DeltasByCategory
);
