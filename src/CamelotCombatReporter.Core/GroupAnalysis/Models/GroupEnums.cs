namespace CamelotCombatReporter.Core.GroupAnalysis.Models;

/// <summary>
/// Defines the role a character fills in a group composition.
/// More granular than ClassArchetype for detailed group analysis.
/// </summary>
public enum GroupRole
{
    Unknown = 0,

    /// <summary>
    /// Front-line damage absorber with high survivability.
    /// </summary>
    Tank = 1,

    /// <summary>
    /// Primary healing and group sustain.
    /// </summary>
    Healer = 2,

    /// <summary>
    /// Mez, stun, root specialists for enemy control.
    /// </summary>
    CrowdControl = 3,

    /// <summary>
    /// Melee damage dealers (assassins, berserkers, etc).
    /// </summary>
    MeleeDps = 4,

    /// <summary>
    /// Ranged magical damage dealers.
    /// </summary>
    CasterDps = 5,

    /// <summary>
    /// Utility classes providing buffs, speed, and group enhancement.
    /// </summary>
    Support = 6,

    /// <summary>
    /// Classes that can fulfill multiple roles effectively.
    /// </summary>
    Hybrid = 7
}

/// <summary>
/// Categorizes groups by size following DAoC conventions.
/// </summary>
public enum GroupSizeCategory
{
    /// <summary>
    /// Solo player (1 person).
    /// </summary>
    Solo = 1,

    /// <summary>
    /// Small-man group (2-4 players).
    /// </summary>
    SmallMan = 2,

    /// <summary>
    /// Full 8-man group (5-8 players).
    /// </summary>
    EightMan = 3,

    /// <summary>
    /// Battlegroup/zerg (9+ players).
    /// </summary>
    Battlegroup = 4
}

/// <summary>
/// Indicates how a group member was detected.
/// </summary>
public enum GroupMemberSource
{
    /// <summary>
    /// Detected from combat event patterns (healing, damage, buffs).
    /// </summary>
    Inferred = 1,

    /// <summary>
    /// Manually added by the user.
    /// </summary>
    Manual = 2,

    /// <summary>
    /// Both inferred and manually confirmed.
    /// </summary>
    Both = 3
}

/// <summary>
/// Types of composition recommendations.
/// </summary>
public enum RecommendationType
{
    /// <summary>
    /// Suggests adding a specific role to the group.
    /// </summary>
    AddRole = 1,

    /// <summary>
    /// Suggests reducing over-representation of a role.
    /// </summary>
    ReduceRole = 2,

    /// <summary>
    /// Suggests rebalancing roles for better coverage.
    /// </summary>
    RebalanceRoles = 3,

    /// <summary>
    /// Suggests matching a known effective template.
    /// </summary>
    TemplateMatch = 4,

    /// <summary>
    /// Suggests class combinations that work well together.
    /// </summary>
    SynergyImprovement = 5
}

/// <summary>
/// Priority level for recommendations.
/// </summary>
public enum RecommendationPriority
{
    /// <summary>
    /// Nice to have improvement.
    /// </summary>
    Low = 1,

    /// <summary>
    /// Moderate improvement opportunity.
    /// </summary>
    Medium = 2,

    /// <summary>
    /// Important composition gap.
    /// </summary>
    High = 3,

    /// <summary>
    /// Critical missing role (no healer, etc).
    /// </summary>
    Critical = 4
}

/// <summary>
/// Extension methods for GroupRole enum.
/// </summary>
public static class GroupRoleExtensions
{
    /// <summary>
    /// Gets a display-friendly name for the role.
    /// </summary>
    public static string GetDisplayName(this GroupRole role) => role switch
    {
        GroupRole.Tank => "Tank",
        GroupRole.Healer => "Healer",
        GroupRole.CrowdControl => "Crowd Control",
        GroupRole.MeleeDps => "Melee DPS",
        GroupRole.CasterDps => "Caster DPS",
        GroupRole.Support => "Support",
        GroupRole.Hybrid => "Hybrid",
        _ => "Unknown"
    };

    /// <summary>
    /// Gets a short abbreviation for the role.
    /// </summary>
    public static string GetAbbreviation(this GroupRole role) => role switch
    {
        GroupRole.Tank => "TNK",
        GroupRole.Healer => "HLR",
        GroupRole.CrowdControl => "CC",
        GroupRole.MeleeDps => "MDPS",
        GroupRole.CasterDps => "CDPS",
        GroupRole.Support => "SUP",
        GroupRole.Hybrid => "HYB",
        _ => "UNK"
    };

    /// <summary>
    /// Gets a color code (hex) for UI representation.
    /// </summary>
    public static string GetColorHex(this GroupRole role) => role switch
    {
        GroupRole.Tank => "#4CAF50",      // Green
        GroupRole.Healer => "#2196F3",     // Blue
        GroupRole.CrowdControl => "#9C27B0", // Purple
        GroupRole.MeleeDps => "#F44336",   // Red
        GroupRole.CasterDps => "#FF9800",  // Orange
        GroupRole.Support => "#00BCD4",    // Cyan
        GroupRole.Hybrid => "#795548",     // Brown
        _ => "#9E9E9E"                     // Grey
    };
}

/// <summary>
/// Extension methods for GroupSizeCategory enum.
/// </summary>
public static class GroupSizeCategoryExtensions
{
    /// <summary>
    /// Gets the size category based on member count.
    /// </summary>
    public static GroupSizeCategory FromMemberCount(int count) => count switch
    {
        1 => GroupSizeCategory.Solo,
        >= 2 and <= 4 => GroupSizeCategory.SmallMan,
        >= 5 and <= 8 => GroupSizeCategory.EightMan,
        _ => GroupSizeCategory.Battlegroup
    };

    /// <summary>
    /// Gets a display-friendly name for the category.
    /// </summary>
    public static string GetDisplayName(this GroupSizeCategory category) => category switch
    {
        GroupSizeCategory.Solo => "Solo",
        GroupSizeCategory.SmallMan => "Small-Man",
        GroupSizeCategory.EightMan => "8-Man",
        GroupSizeCategory.Battlegroup => "Battlegroup",
        _ => "Unknown"
    };

    /// <summary>
    /// Gets the size range for the category.
    /// </summary>
    public static (int Min, int Max) GetSizeRange(this GroupSizeCategory category) => category switch
    {
        GroupSizeCategory.Solo => (1, 1),
        GroupSizeCategory.SmallMan => (2, 4),
        GroupSizeCategory.EightMan => (5, 8),
        GroupSizeCategory.Battlegroup => (9, int.MaxValue),
        _ => (0, 0)
    };
}
