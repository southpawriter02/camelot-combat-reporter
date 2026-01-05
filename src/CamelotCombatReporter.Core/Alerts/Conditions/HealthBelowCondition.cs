using CamelotCombatReporter.Core.Alerts.Models;
using CamelotCombatReporter.Core.Models;

namespace CamelotCombatReporter.Core.Alerts.Conditions;

/// <summary>
/// Condition that triggers when health drops below a specified threshold.
/// </summary>
public class HealthBelowCondition : IAlertCondition
{
    /// <summary>
    /// Health percentage threshold (0-100).
    /// </summary>
    public double ThresholdPercent { get; }

    /// <inheritdoc />
    public string ConditionType => "HealthBelow";

    /// <inheritdoc />
    public string Description => $"Health below {ThresholdPercent}%";

    /// <summary>
    /// Creates a new health below condition.
    /// </summary>
    /// <param name="thresholdPercent">Health percentage threshold (0-100).</param>
    public HealthBelowCondition(double thresholdPercent)
    {
        if (thresholdPercent < 0 || thresholdPercent > 100)
            throw new ArgumentOutOfRangeException(nameof(thresholdPercent), "Threshold must be between 0 and 100");

        ThresholdPercent = thresholdPercent;
    }

    /// <inheritdoc />
    public (bool IsMet, string Reason, Dictionary<string, object> Data) Evaluate(
        CombatState state,
        LogEvent? currentEvent = null)
    {
        var data = new Dictionary<string, object>
        {
            ["CurrentHealth"] = state.CurrentHealthPercent,
            ["Threshold"] = ThresholdPercent
        };

        if (state.CurrentHealthPercent < ThresholdPercent)
        {
            return (true, $"Health at {state.CurrentHealthPercent:F0}% (below {ThresholdPercent}%)", data);
        }

        return (false, string.Empty, data);
    }
}
