using CamelotCombatReporter.Core.Alerts.Models;
using CamelotCombatReporter.Core.Models;

namespace CamelotCombatReporter.Core.Alerts.Conditions;

/// <summary>
/// Condition that triggers when damage received exceeds a threshold within a time window.
/// Useful for detecting burst damage situations.
/// </summary>
public class DamageInWindowCondition : IAlertCondition
{
    /// <summary>
    /// Damage threshold that must be exceeded.
    /// </summary>
    public int DamageThreshold { get; }

    /// <summary>
    /// Time window for damage accumulation.
    /// </summary>
    public TimeSpan Window { get; }

    /// <inheritdoc />
    public string ConditionType => "DamageInWindow";

    /// <inheritdoc />
    public string Description => $">{DamageThreshold} damage in {Window.TotalSeconds}s";

    /// <summary>
    /// Creates a new damage in window condition.
    /// </summary>
    /// <param name="damageThreshold">Damage threshold that must be exceeded.</param>
    /// <param name="window">Time window for damage accumulation.</param>
    public DamageInWindowCondition(int damageThreshold, TimeSpan window)
    {
        if (damageThreshold <= 0)
            throw new ArgumentOutOfRangeException(nameof(damageThreshold), "Threshold must be positive");

        if (window <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(window), "Window must be positive");

        DamageThreshold = damageThreshold;
        Window = window;
    }

    /// <inheritdoc />
    public (bool IsMet, string Reason, Dictionary<string, object> Data) Evaluate(
        CombatState state,
        LogEvent? currentEvent = null)
    {
        var data = new Dictionary<string, object>
        {
            ["RecentDamage"] = state.RecentDamageReceived,
            ["Threshold"] = DamageThreshold,
            ["WindowSeconds"] = Window.TotalSeconds
        };

        if (state.RecentDamageReceived > DamageThreshold)
        {
            return (true,
                $"Received {state.RecentDamageReceived:N0} damage in {Window.TotalSeconds}s window (threshold: {DamageThreshold:N0})",
                data);
        }

        return (false, string.Empty, data);
    }
}
