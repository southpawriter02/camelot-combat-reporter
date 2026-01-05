namespace CamelotCombatReporter.Core.Comparison.Models;

/// <summary>
/// Type of performance goal.
/// </summary>
public enum GoalType
{
    /// <summary>Target damage per second.</summary>
    DamagePerSecond,
    /// <summary>Target healing per second.</summary>
    HealingPerSecond,
    /// <summary>Target kill/death ratio.</summary>
    KillDeathRatio,
    /// <summary>Target kills per session.</summary>
    KillsPerSession,
    /// <summary>Target deaths per session (lower is better).</summary>
    DeathsPerSession,
    /// <summary>Target buff uptime percentage.</summary>
    BuffUptime,
    /// <summary>Custom user-defined metric.</summary>
    CustomMetric
}

/// <summary>
/// Status of a performance goal.
/// </summary>
public enum GoalStatus
{
    /// <summary>Goal not yet started tracking.</summary>
    NotStarted,
    /// <summary>Goal is in progress.</summary>
    InProgress,
    /// <summary>Goal has been achieved.</summary>
    Achieved,
    /// <summary>Goal was not achieved before deadline.</summary>
    Failed,
    /// <summary>Goal deadline has passed.</summary>
    Expired
}

/// <summary>
/// A performance goal to track.
/// </summary>
/// <param name="Id">Unique identifier for the goal.</param>
/// <param name="Name">Display name for the goal.</param>
/// <param name="Type">Type of metric being tracked.</param>
/// <param name="CustomMetricName">Name of custom metric if Type is CustomMetric.</param>
/// <param name="TargetValue">Target value to achieve.</param>
/// <param name="CurrentValue">Current best value achieved.</param>
/// <param name="StartingValue">Value when the goal was created.</param>
/// <param name="CreatedAt">When the goal was created.</param>
/// <param name="Deadline">Optional deadline for the goal.</param>
/// <param name="Status">Current status of the goal.</param>
/// <param name="ProgressHistory">History of progress towards the goal.</param>
public record PerformanceGoal(
    Guid Id,
    string Name,
    GoalType Type,
    string? CustomMetricName,
    double TargetValue,
    double? CurrentValue,
    double StartingValue,
    DateTime CreatedAt,
    DateTime? Deadline,
    GoalStatus Status,
    IReadOnlyList<GoalProgress> ProgressHistory
);

/// <summary>
/// A single progress update towards a goal.
/// </summary>
/// <param name="Timestamp">When this progress was recorded.</param>
/// <param name="Value">The metric value at this point.</param>
/// <param name="PercentComplete">Percentage of goal completion (0-100).</param>
/// <param name="SessionId">ID of the session that recorded this progress.</param>
public record GoalProgress(
    DateTime Timestamp,
    double Value,
    double PercentComplete,
    Guid? SessionId
);

/// <summary>
/// A personal best record.
/// </summary>
/// <param name="Id">Unique identifier for this record.</param>
/// <param name="MetricName">Name of the metric.</param>
/// <param name="Value">The personal best value.</param>
/// <param name="AchievedAt">When this PB was achieved.</param>
/// <param name="SessionId">ID of the session where this was achieved.</param>
/// <param name="PreviousBest">The previous best value, if any.</param>
/// <param name="ImprovementPercent">Percentage improvement over previous best.</param>
public record PersonalBest(
    Guid Id,
    string MetricName,
    double Value,
    DateTime AchievedAt,
    Guid SessionId,
    double? PreviousBest,
    double? ImprovementPercent
);
