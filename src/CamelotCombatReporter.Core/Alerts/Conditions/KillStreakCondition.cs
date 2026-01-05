using CamelotCombatReporter.Core.Alerts.Models;
using CamelotCombatReporter.Core.Models;

namespace CamelotCombatReporter.Core.Alerts.Conditions;

/// <summary>
/// Condition that triggers when a kill streak reaches or exceeds a threshold.
/// </summary>
public class KillStreakCondition : IAlertCondition
{
    /// <summary>
    /// Kill streak threshold to trigger on.
    /// </summary>
    public int StreakThreshold { get; }

    /// <inheritdoc />
    public string ConditionType => "KillStreak";

    /// <inheritdoc />
    public string Description => $"Kill streak >= {StreakThreshold}";

    /// <summary>
    /// Creates a new kill streak condition.
    /// </summary>
    /// <param name="streakThreshold">Minimum kill streak to trigger.</param>
    public KillStreakCondition(int streakThreshold)
    {
        if (streakThreshold <= 0)
            throw new ArgumentOutOfRangeException(nameof(streakThreshold), "Streak threshold must be positive");

        StreakThreshold = streakThreshold;
    }

    /// <inheritdoc />
    public (bool IsMet, string Reason, Dictionary<string, object> Data) Evaluate(
        CombatState state,
        LogEvent? currentEvent = null)
    {
        var data = new Dictionary<string, object>
        {
            ["CurrentStreak"] = state.KillStreak,
            ["Threshold"] = StreakThreshold
        };

        if (state.KillStreak >= StreakThreshold)
        {
            return (true, $"Kill streak: {state.KillStreak}", data);
        }

        return (false, string.Empty, data);
    }
}
