using CamelotCombatReporter.Core.Alerts.Models;
using CamelotCombatReporter.Core.Models;
using CamelotCombatReporter.Core.RealmAbilities.Models;

namespace CamelotCombatReporter.Core.Alerts.Conditions;

/// <summary>
/// Condition that triggers when a specific ability is used.
/// Can match abilities used by self or enemies.
/// </summary>
public class AbilityUsedCondition : IAlertCondition
{
    /// <summary>
    /// Name or partial name of the ability to watch for.
    /// </summary>
    public string AbilityName { get; }

    /// <summary>
    /// If true, watches for enemy usage; if false, watches for self usage.
    /// </summary>
    public bool ByEnemy { get; }

    /// <summary>
    /// If true, performs partial match on ability name.
    /// </summary>
    public bool PartialMatch { get; }

    /// <inheritdoc />
    public string ConditionType => "AbilityUsed";

    /// <inheritdoc />
    public string Description => $"{(ByEnemy ? "Enemy" : "Self")} used {AbilityName}";

    /// <summary>
    /// Creates a new ability used condition.
    /// </summary>
    /// <param name="abilityName">Name or partial name of the ability to watch for.</param>
    /// <param name="byEnemy">If true, watches for enemy usage; if false, watches for self usage.</param>
    /// <param name="partialMatch">If true, performs partial match on ability name.</param>
    public AbilityUsedCondition(string abilityName, bool byEnemy = false, bool partialMatch = true)
    {
        if (string.IsNullOrWhiteSpace(abilityName))
            throw new ArgumentException("Ability name cannot be empty", nameof(abilityName));

        AbilityName = abilityName;
        ByEnemy = byEnemy;
        PartialMatch = partialMatch;
    }

    /// <inheritdoc />
    public (bool IsMet, string Reason, Dictionary<string, object> Data) Evaluate(
        CombatState state,
        LogEvent? currentEvent = null)
    {
        var data = new Dictionary<string, object>
        {
            ["WatchedAbility"] = AbilityName,
            ["ByEnemy"] = ByEnemy
        };

        // Check if the current event is a realm ability event
        if (currentEvent is RealmAbilityEvent raEvent)
        {
            bool isMatch = PartialMatch
                ? raEvent.AbilityName.Contains(AbilityName, StringComparison.OrdinalIgnoreCase)
                : raEvent.AbilityName.Equals(AbilityName, StringComparison.OrdinalIgnoreCase);

            bool sourceMatch = ByEnemy
                ? raEvent.SourceName != "You" && !string.IsNullOrEmpty(raEvent.SourceName)
                : raEvent.SourceName == "You";

            if (isMatch && sourceMatch)
            {
                data["MatchedAbility"] = raEvent.AbilityName;
                data["Source"] = raEvent.SourceName;
                return (true, $"{raEvent.SourceName} used {raEvent.AbilityName}", data);
            }
        }

        // Also check recent ability history
        var recentAbility = state.AbilityHistory.LastOrDefault();
        if (recentAbility != default)
        {
            bool isMatch = PartialMatch
                ? recentAbility.Ability.Contains(AbilityName, StringComparison.OrdinalIgnoreCase)
                : recentAbility.Ability.Equals(AbilityName, StringComparison.OrdinalIgnoreCase);

            if (isMatch && !ByEnemy) // History only tracks self abilities
            {
                data["MatchedAbility"] = recentAbility.Ability;
                data["Source"] = "You";
                return (true, $"You used {recentAbility.Ability}", data);
            }
        }

        return (false, string.Empty, data);
    }
}
