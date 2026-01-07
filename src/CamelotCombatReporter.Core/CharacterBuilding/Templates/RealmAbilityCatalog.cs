using CamelotCombatReporter.Core.CharacterBuilding.Models;

namespace CamelotCombatReporter.Core.CharacterBuilding.Templates;

/// <summary>
/// Static catalog of all realm abilities with their costs and restrictions.
/// </summary>
public static class RealmAbilityCatalog
{
    private static readonly List<RealmAbilityDefinition> _abilities = InitializeAbilities();

    /// <summary>
    /// Gets all available realm abilities.
    /// </summary>
    public static IReadOnlyList<RealmAbilityDefinition> GetAllAbilities() => _abilities;

    /// <summary>
    /// Gets active (usable) realm abilities.
    /// </summary>
    public static IReadOnlyList<RealmAbilityDefinition> GetActiveAbilities() =>
        _abilities.Where(a => a.Category == RealmAbilityCategory.Active).ToList();

    /// <summary>
    /// Gets passive realm abilities.
    /// </summary>
    public static IReadOnlyList<RealmAbilityDefinition> GetPassiveAbilities() =>
        _abilities.Where(a => a.Category == RealmAbilityCategory.Passive).ToList();

    /// <summary>
    /// Gets mastery realm abilities.
    /// </summary>
    public static IReadOnlyList<RealmAbilityDefinition> GetMasteryAbilities() =>
        _abilities.Where(a => a.Category == RealmAbilityCategory.Mastery).ToList();

    /// <summary>
    /// Gets the point cost for a realm ability at a specific rank.
    /// </summary>
    public static int GetPointCost(string abilityName, int rank)
    {
        var ability = _abilities.FirstOrDefault(a => 
            a.Name.Equals(abilityName, StringComparison.OrdinalIgnoreCase));
        
        if (ability == null || rank < 1 || rank > ability.MaxRank)
            return 0;

        return ability.PointCosts.Length >= rank ? ability.PointCosts[rank - 1] : 0;
    }

    /// <summary>
    /// Gets the total points spent on a collection of realm abilities.
    /// </summary>
    public static int GetTotalPointsSpent(IEnumerable<RealmAbilitySelection> selections)
    {
        return selections.Sum(s => GetPointCost(s.AbilityName, s.Rank));
    }

    /// <summary>
    /// Gets the maximum realm ability points available for a realm rank.
    /// Formula varies by RR tier.
    /// </summary>
    public static int GetMaxRealmAbilityPoints(int realmRank, int realmRankLevel)
    {
        // Approximate RA point formula based on RR
        // RR1 = 0, RR2 = 2, RR3 = 5, RR4 = 9, RR5 = 14, RR6 = 20, etc.
        var basePoints = realmRank switch
        {
            1 => 0,
            2 => 2,
            3 => 5,
            4 => 9,
            5 => 14,
            6 => 20,
            7 => 27,
            8 => 35,
            9 => 44,
            10 => 54,
            11 => 65,
            12 => 77,
            13 => 90,
            14 => 104,
            _ => 0
        };

        // Add points for sub-levels (approximately 1 point per L2-L3)
        var subPoints = realmRankLevel / 3;
        return basePoints + subPoints;
    }

    private static List<RealmAbilityDefinition> InitializeAbilities()
    {
        return
        [
            // ─────────────────────────────────────────────────────────────────
            // Active Abilities
            // ─────────────────────────────────────────────────────────────────
            new RealmAbilityDefinition("Purge", RealmAbilityCategory.Active, 3, [6, 10, 14], "Removes negative effects"),
            new RealmAbilityDefinition("First Aid", RealmAbilityCategory.Active, 3, [3, 6, 10], "Self heal over time"),
            new RealmAbilityDefinition("Second Wind", RealmAbilityCategory.Active, 1, [5], "Restores endurance"),
            new RealmAbilityDefinition("Ignore Pain", RealmAbilityCategory.Active, 5, [3, 6, 10, 14, 18], "Reduces damage taken"),
            new RealmAbilityDefinition("Mystic Crystal Lore", RealmAbilityCategory.Active, 1, [5], "Reveals magic item properties"),
            new RealmAbilityDefinition("Raging Power", RealmAbilityCategory.Active, 3, [3, 6, 10], "Increases power regeneration"),
            new RealmAbilityDefinition("Soldier's Barricade", RealmAbilityCategory.Active, 5, [3, 6, 10, 14, 18], "Group damage absorption"),
            new RealmAbilityDefinition("Speed of Sound", RealmAbilityCategory.Active, 1, [10], "Sprint without endurance cost"),
            new RealmAbilityDefinition("The Empty Mind", RealmAbilityCategory.Active, 1, [14], "Increases crowd control resistance"),
            new RealmAbilityDefinition("Vanish", RealmAbilityCategory.Active, 1, [14], "Immediately enters stealth"),
            new RealmAbilityDefinition("Concentration", RealmAbilityCategory.Active, 5, [3, 6, 10, 14, 18], "Reduces spell interruption"),
            new RealmAbilityDefinition("Volcanic Pillar", RealmAbilityCategory.Active, 1, [14], "Ground-targeted fire AoE"),
            new RealmAbilityDefinition("Whirling Dervish", RealmAbilityCategory.Active, 1, [14], "360-degree attack"),
            new RealmAbilityDefinition("Desperate Bowman", RealmAbilityCategory.Active, 1, [14], "High damage bow attack"),
            new RealmAbilityDefinition("Remedy", RealmAbilityCategory.Active, 1, [10], "Removes poisons and diseases"),
            new RealmAbilityDefinition("Anger of the Gods", RealmAbilityCategory.Active, 1, [14], "Group damage buff"),
            new RealmAbilityDefinition("Juggernaut", RealmAbilityCategory.Active, 1, [14], "Increases size and damage"),
            new RealmAbilityDefinition("Viper", RealmAbilityCategory.Active, 1, [5], "Adds poison to weapon"),

            // ─────────────────────────────────────────────────────────────────
            // Passive Abilities
            // ─────────────────────────────────────────────────────────────────
            new RealmAbilityDefinition("Augmented Strength", RealmAbilityCategory.Passive, 5, [1, 2, 3, 5, 8], "Increases strength"),
            new RealmAbilityDefinition("Augmented Constitution", RealmAbilityCategory.Passive, 5, [1, 2, 3, 5, 8], "Increases constitution"),
            new RealmAbilityDefinition("Augmented Dexterity", RealmAbilityCategory.Passive, 5, [1, 2, 3, 5, 8], "Increases dexterity"),
            new RealmAbilityDefinition("Augmented Quickness", RealmAbilityCategory.Passive, 5, [1, 2, 3, 5, 8], "Increases quickness"),
            new RealmAbilityDefinition("Augmented Acuity", RealmAbilityCategory.Passive, 5, [1, 2, 3, 5, 8], "Increases casting stat"),
            new RealmAbilityDefinition("Toughness", RealmAbilityCategory.Passive, 5, [2, 4, 6, 10, 14], "Increases hit points"),
            new RealmAbilityDefinition("Long Wind", RealmAbilityCategory.Passive, 5, [1, 2, 3, 5, 8], "Increases endurance"),
            new RealmAbilityDefinition("Determination", RealmAbilityCategory.Passive, 5, [2, 4, 6, 10, 14], "Reduces crowd control duration"),
            new RealmAbilityDefinition("Avoidance of Magic", RealmAbilityCategory.Passive, 5, [2, 4, 6, 10, 14], "Increases magic resistances"),
            new RealmAbilityDefinition("Lifter", RealmAbilityCategory.Passive, 5, [1, 2, 3, 5, 8], "Increases encumbrance"),
            new RealmAbilityDefinition("Dual Threat", RealmAbilityCategory.Passive, 5, [3, 6, 10, 14, 18], "Increases dual wield speed"),
            new RealmAbilityDefinition("Dodger", RealmAbilityCategory.Passive, 5, [3, 6, 10, 14, 18], "Increases evade chance"),
            new RealmAbilityDefinition("Ethereal Bond", RealmAbilityCategory.Passive, 5, [2, 4, 6, 10, 14], "Increases power pool"),
            new RealmAbilityDefinition("Wild Power", RealmAbilityCategory.Passive, 5, [3, 6, 10, 14, 18], "Increases spell damage variance"),
            new RealmAbilityDefinition("Serenity", RealmAbilityCategory.Passive, 5, [2, 4, 6, 10, 14], "Increases power regeneration"),
            new RealmAbilityDefinition("Mastery of Pain", RealmAbilityCategory.Passive, 9, [3, 3, 3, 6, 6, 6, 10, 10, 10], "Increases melee damage"),
            new RealmAbilityDefinition("Mastery of Magery", RealmAbilityCategory.Passive, 9, [3, 3, 3, 6, 6, 6, 10, 10, 10], "Increases spell damage"),
            new RealmAbilityDefinition("Mastery of Healing", RealmAbilityCategory.Passive, 9, [3, 3, 3, 6, 6, 6, 10, 10, 10], "Increases heal effectiveness"),
            new RealmAbilityDefinition("Mastery of Archery", RealmAbilityCategory.Passive, 9, [3, 3, 3, 6, 6, 6, 10, 10, 10], "Increases ranged damage"),
            new RealmAbilityDefinition("Mastery of Stealth", RealmAbilityCategory.Passive, 9, [3, 3, 3, 6, 6, 6, 10, 10, 10], "Increases stealth effectiveness"),
            new RealmAbilityDefinition("Mastery of the Art", RealmAbilityCategory.Passive, 5, [3, 6, 10, 14, 18], "Reduces spell cast time"),
            new RealmAbilityDefinition("Mastery of Arms", RealmAbilityCategory.Passive, 5, [3, 6, 10, 14, 18], "Increases melee speed"),
            new RealmAbilityDefinition("Mastery of Focus", RealmAbilityCategory.Passive, 5, [3, 6, 10, 14, 18], "Reduces focus loss on hit"),
            new RealmAbilityDefinition("Avoid Pain", RealmAbilityCategory.Passive, 5, [3, 6, 10, 14, 18], "Reduces melee damage taken"),

            // ─────────────────────────────────────────────────────────────────
            // Mastery Abilities (class-specific lines)
            // ─────────────────────────────────────────────────────────────────
            new RealmAbilityDefinition("Mastery of Concentration", RealmAbilityCategory.Mastery, 5, [3, 6, 10, 14, 18], "Increases concentration pool"),
            new RealmAbilityDefinition("Mastery of Parrying", RealmAbilityCategory.Mastery, 5, [3, 6, 10, 14, 18], "Increases parry chance"),
            new RealmAbilityDefinition("Mastery of Blocking", RealmAbilityCategory.Mastery, 5, [3, 6, 10, 14, 18], "Increases block chance"),
            new RealmAbilityDefinition("Mastery of Water", RealmAbilityCategory.Mastery, 5, [3, 6, 10, 14, 18], "Increases underwater effectiveness"),
        ];
    }
}

/// <summary>
/// Definition of a realm ability with its cost structure.
/// </summary>
public record RealmAbilityDefinition(
    string Name,
    RealmAbilityCategory Category,
    int MaxRank,
    int[] PointCosts,
    string? Description = null
)
{
    /// <summary>
    /// Gets the total cumulative cost to max this ability.
    /// </summary>
    public int TotalCost => PointCosts.Sum();
}
