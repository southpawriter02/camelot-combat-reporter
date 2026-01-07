namespace CamelotCombatReporter.Core.CharacterBuilding.Models;

/// <summary>
/// A realm ability and its trained rank.
/// </summary>
public record RealmAbilitySelection
{
    /// <summary>
    /// Name of the realm ability (e.g., "Purge", "Determination").
    /// </summary>
    public required string AbilityName { get; init; }
    
    /// <summary>
    /// Trained rank of this ability (1-5 typically).
    /// </summary>
    public int Rank { get; init; } = 1;
    
    /// <summary>
    /// Realm point cost for this ability at this rank.
    /// </summary>
    public int PointCost { get; init; }
    
    /// <summary>
    /// Category of this realm ability.
    /// </summary>
    public RealmAbilityCategory Category { get; init; }
}

/// <summary>
/// Categories of realm abilities.
/// </summary>
public enum RealmAbilityCategory
{
    /// <summary>
    /// Passive abilities that are always active.
    /// </summary>
    Passive = 0,
    
    /// <summary>
    /// Active abilities with cooldowns.
    /// </summary>
    Active = 1,
    
    /// <summary>
    /// Mastery-level abilities (typically higher cost).
    /// </summary>
    Mastery = 2
}
