using CamelotCombatReporter.Core.Alerts.Models;
using CamelotCombatReporter.Core.Models;

namespace CamelotCombatReporter.Core.Alerts.Conditions;

/// <summary>
/// Condition that triggers when targeting an enemy of a specific class.
/// </summary>
public class EnemyClassCondition : IAlertCondition
{
    /// <summary>
    /// List of class names to watch for.
    /// </summary>
    public IReadOnlyList<string> TargetClasses { get; }

    /// <inheritdoc />
    public string ConditionType => "EnemyClass";

    /// <inheritdoc />
    public string Description => $"Enemy class: {string.Join(", ", TargetClasses)}";

    /// <summary>
    /// Creates a new enemy class condition.
    /// </summary>
    /// <param name="targetClasses">List of class names to watch for.</param>
    public EnemyClassCondition(IEnumerable<string> targetClasses)
    {
        var classes = targetClasses?.ToList() ?? throw new ArgumentNullException(nameof(targetClasses));

        if (classes.Count == 0)
            throw new ArgumentException("At least one target class must be specified", nameof(targetClasses));

        TargetClasses = classes;
    }

    /// <inheritdoc />
    public (bool IsMet, string Reason, Dictionary<string, object> Data) Evaluate(
        CombatState state,
        LogEvent? currentEvent = null)
    {
        var data = new Dictionary<string, object>
        {
            ["CurrentTarget"] = state.CurrentTargetClass ?? "Unknown",
            ["WatchedClasses"] = TargetClasses
        };

        if (!string.IsNullOrEmpty(state.CurrentTargetClass))
        {
            var matchingClass = TargetClasses.FirstOrDefault(
                c => c.Equals(state.CurrentTargetClass, StringComparison.OrdinalIgnoreCase));

            if (matchingClass != null)
            {
                data["MatchedClass"] = matchingClass;
                return (true, $"Targeting {state.CurrentTargetClass}", data);
            }
        }

        return (false, string.Empty, data);
    }
}
