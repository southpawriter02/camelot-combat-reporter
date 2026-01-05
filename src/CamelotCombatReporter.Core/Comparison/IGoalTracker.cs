using CamelotCombatReporter.Core.Comparison.Models;

namespace CamelotCombatReporter.Core.Comparison;

/// <summary>
/// Service for tracking performance goals.
/// </summary>
public interface IGoalTracker
{
    /// <summary>
    /// Gets all active goals (NotStarted or InProgress).
    /// </summary>
    IReadOnlyList<PerformanceGoal> ActiveGoals { get; }

    /// <summary>
    /// Creates a new performance goal.
    /// </summary>
    /// <param name="name">Display name for the goal.</param>
    /// <param name="type">Type of metric to track.</param>
    /// <param name="targetValue">Target value to achieve.</param>
    /// <param name="deadline">Optional deadline for the goal.</param>
    /// <param name="customMetricName">Name of custom metric if type is CustomMetric.</param>
    /// <returns>The created goal.</returns>
    PerformanceGoal CreateGoal(
        string name,
        GoalType type,
        double targetValue,
        DateTime? deadline = null,
        string? customMetricName = null);

    /// <summary>
    /// Updates progress on a goal.
    /// </summary>
    /// <param name="goalId">ID of the goal to update.</param>
    /// <param name="currentValue">Current value achieved.</param>
    /// <param name="sessionId">Optional ID of the session that recorded this progress.</param>
    void UpdateProgress(Guid goalId, double currentValue, Guid? sessionId = null);

    /// <summary>
    /// Deletes a goal.
    /// </summary>
    /// <param name="goalId">ID of the goal to delete.</param>
    void DeleteGoal(Guid goalId);

    /// <summary>
    /// Gets completed/expired/failed goals.
    /// </summary>
    IReadOnlyList<PerformanceGoal> GetGoalHistory();

    /// <summary>
    /// Loads goals from storage.
    /// </summary>
    Task LoadAsync();

    /// <summary>
    /// Saves goals to storage.
    /// </summary>
    Task SaveAsync();
}
