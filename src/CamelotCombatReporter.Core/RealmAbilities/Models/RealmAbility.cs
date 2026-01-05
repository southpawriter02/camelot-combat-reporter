namespace CamelotCombatReporter.Core.RealmAbilities.Models;

/// <summary>
/// Represents a realm ability definition with all metadata.
/// </summary>
/// <param name="Id">Unique identifier for the ability.</param>
/// <param name="Name">Display name of the ability.</param>
/// <param name="InternalName">Internal name used for log parsing.</param>
/// <param name="RealmAvailability">Which realms can use this ability.</param>
/// <param name="Type">Classification of the ability type.</param>
/// <param name="MaxLevel">Maximum trainable level.</param>
/// <param name="RealmPointCosts">Realm point cost per level (array index = level - 1).</param>
/// <param name="BaseCooldown">Base cooldown duration (null for passives).</param>
/// <param name="Prerequisites">List of prerequisite ability IDs.</param>
/// <param name="Description">General description of the ability.</param>
/// <param name="EffectDescriptions">Level-specific effect descriptions.</param>
/// <param name="IntroducedIn">Game era when this ability was introduced.</param>
/// <param name="IsTimer">Whether the ability has a timer/cooldown (false for passives).</param>
/// <param name="SharedCooldownGroup">Group name for abilities that share cooldowns.</param>
public record RealmAbility(
    string Id,
    string Name,
    string InternalName,
    RealmAvailability RealmAvailability,
    RealmAbilityType Type,
    int MaxLevel,
    int[] RealmPointCosts,
    TimeSpan? BaseCooldown,
    IReadOnlyList<string> Prerequisites,
    string Description,
    IReadOnlyDictionary<int, string> EffectDescriptions,
    GameEra IntroducedIn,
    bool IsTimer = true,
    string? SharedCooldownGroup = null
)
{
    /// <summary>
    /// Calculates the total realm point cost to train this ability to the specified level.
    /// </summary>
    /// <param name="level">Target level (1 to MaxLevel).</param>
    /// <returns>Total realm points required.</returns>
    public int GetTotalCostForLevel(int level)
    {
        if (level < 1 || level > MaxLevel)
            return 0;

        int total = 0;
        for (int i = 0; i < level && i < RealmPointCosts.Length; i++)
        {
            total += RealmPointCosts[i];
        }
        return total;
    }

    /// <summary>
    /// Gets the cost to upgrade from current level to next level.
    /// </summary>
    /// <param name="currentLevel">Current level (0 for not trained).</param>
    /// <returns>Realm points required for next level, or 0 if already maxed.</returns>
    public int GetNextLevelCost(int currentLevel)
    {
        if (currentLevel >= MaxLevel || currentLevel < 0)
            return 0;

        return currentLevel < RealmPointCosts.Length ? RealmPointCosts[currentLevel] : 0;
    }

    /// <summary>
    /// Whether this is a passive ability (no activation).
    /// </summary>
    public bool IsPassive => Type == RealmAbilityType.Passive || !IsTimer;

    /// <summary>
    /// Gets the effect description for a specific level.
    /// </summary>
    /// <param name="level">Level to get description for.</param>
    /// <returns>Effect description or general description if level-specific not available.</returns>
    public string GetEffectDescription(int level)
    {
        if (EffectDescriptions.TryGetValue(level, out var desc))
            return desc;
        return Description;
    }
}
