using CamelotCombatReporter.Core.Alerts.Models;
using CamelotCombatReporter.Core.Models;

namespace CamelotCombatReporter.Core.Alerts.Conditions;

/// <summary>
/// Condition that triggers when specific debuffs are active on the player.
/// </summary>
public class DebuffAppliedCondition : IAlertCondition
{
    /// <summary>
    /// List of debuff names to watch for.
    /// </summary>
    public IReadOnlyList<string> DebuffNames { get; }

    /// <summary>
    /// If true, triggers when any watched debuff is active.
    /// If false, triggers only when all watched debuffs are active.
    /// </summary>
    public bool MatchAny { get; }

    /// <inheritdoc />
    public string ConditionType => "DebuffApplied";

    /// <inheritdoc />
    public string Description => $"Debuff: {string.Join(", ", DebuffNames)}";

    /// <summary>
    /// Creates a new debuff applied condition.
    /// </summary>
    /// <param name="debuffNames">List of debuff names to watch for.</param>
    /// <param name="matchAny">If true, triggers when any watched debuff is active.</param>
    public DebuffAppliedCondition(IEnumerable<string> debuffNames, bool matchAny = true)
    {
        var names = debuffNames?.ToList() ?? throw new ArgumentNullException(nameof(debuffNames));

        if (names.Count == 0)
            throw new ArgumentException("At least one debuff name must be specified", nameof(debuffNames));

        DebuffNames = names;
        MatchAny = matchAny;
    }

    /// <inheritdoc />
    public (bool IsMet, string Reason, Dictionary<string, object> Data) Evaluate(
        CombatState state,
        LogEvent? currentEvent = null)
    {
        var data = new Dictionary<string, object>
        {
            ["ActiveDebuffs"] = state.ActiveDebuffs,
            ["WatchedDebuffs"] = DebuffNames
        };

        var matchingDebuffs = state.ActiveDebuffs
            .Where(d => DebuffNames.Any(n => d.Contains(n, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        if (matchingDebuffs.Count > 0)
        {
            data["MatchedDebuffs"] = matchingDebuffs;

            bool shouldTrigger = MatchAny
                ? matchingDebuffs.Count > 0
                : matchingDebuffs.Count >= DebuffNames.Count;

            if (shouldTrigger)
            {
                return (true, $"Active debuffs: {string.Join(", ", matchingDebuffs)}", data);
            }
        }

        return (false, string.Empty, data);
    }
}
