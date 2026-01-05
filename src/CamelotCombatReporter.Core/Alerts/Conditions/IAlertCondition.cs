using CamelotCombatReporter.Core.Alerts.Models;
using CamelotCombatReporter.Core.Models;

namespace CamelotCombatReporter.Core.Alerts.Conditions;

/// <summary>
/// Interface for alert conditions that evaluate combat state to determine if an alert should trigger.
/// </summary>
public interface IAlertCondition
{
    /// <summary>
    /// Unique type identifier for this condition (e.g., "HealthBelow", "DamageInWindow").
    /// </summary>
    string ConditionType { get; }

    /// <summary>
    /// Human-readable description of this condition's configuration.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Evaluates whether this condition is met given the current combat state.
    /// </summary>
    /// <param name="state">Current combat state to evaluate against.</param>
    /// <param name="currentEvent">The event currently being processed, if any.</param>
    /// <returns>
    /// A tuple containing:
    /// - IsMet: Whether the condition is satisfied
    /// - Reason: Human-readable explanation of why the condition was met (or empty if not met)
    /// - Data: Key-value data from the evaluation for logging/display
    /// </returns>
    (bool IsMet, string Reason, Dictionary<string, object> Data) Evaluate(
        CombatState state,
        LogEvent? currentEvent = null);
}
