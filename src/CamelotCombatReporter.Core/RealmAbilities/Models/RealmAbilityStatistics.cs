using CamelotCombatReporter.Core.Models;

namespace CamelotCombatReporter.Core.RealmAbilities.Models;

/// <summary>
/// Represents a single activation of a realm ability.
/// </summary>
/// <param name="Id">Unique identifier for this activation.</param>
/// <param name="Timestamp">When the ability was activated.</param>
/// <param name="Ability">The realm ability that was activated.</param>
/// <param name="Level">The level of the ability at activation (if known).</param>
/// <param name="SourceName">The name of the entity that activated it.</param>
/// <param name="CooldownEnds">Estimated time when cooldown ends.</param>
/// <param name="AssociatedEvents">Events associated with this activation (damage, healing).</param>
public record RealmAbilityActivation(
    Guid Id,
    TimeOnly Timestamp,
    RealmAbility Ability,
    int Level,
    string SourceName,
    TimeOnly CooldownEnds,
    IReadOnlyList<LogEvent> AssociatedEvents
);

/// <summary>
/// Usage statistics for a single realm ability.
/// </summary>
/// <param name="Ability">The realm ability.</param>
/// <param name="TotalActivations">Total number of times activated.</param>
/// <param name="TotalDamage">Total damage dealt by this ability.</param>
/// <param name="TotalHealing">Total healing done by this ability.</param>
/// <param name="TotalTargetsAffected">Total unique targets affected.</param>
/// <param name="AverageEffectiveness">Average effect per activation.</param>
/// <param name="CooldownEfficiency">Percentage of time ability was used when available (0-100).</param>
/// <param name="Activations">List of individual activations.</param>
public record RealmAbilityUsageStats(
    RealmAbility Ability,
    int TotalActivations,
    int TotalDamage,
    int TotalHealing,
    int TotalTargetsAffected,
    double AverageEffectiveness,
    double CooldownEfficiency,
    IReadOnlyList<RealmAbilityActivation> Activations
);

/// <summary>
/// Session-level statistics for all realm ability usage.
/// </summary>
/// <param name="TotalActivations">Total activations across all abilities.</param>
/// <param name="TotalRAsUsed">Number of unique RAs used.</param>
/// <param name="MostUsedAbility">The most frequently activated ability.</param>
/// <param name="HighestDamageAbility">The ability that dealt the most total damage.</param>
/// <param name="OverallCooldownEfficiency">Average cooldown efficiency across all abilities.</param>
/// <param name="UsageByType">Count of activations grouped by ability type.</param>
/// <param name="PerAbilityStats">Statistics for each ability used.</param>
/// <param name="SessionDuration">Total duration of the session.</param>
public record RealmAbilitySessionStats(
    int TotalActivations,
    int TotalRAsUsed,
    RealmAbilityUsageStats? MostUsedAbility,
    RealmAbilityUsageStats? HighestDamageAbility,
    double OverallCooldownEfficiency,
    IReadOnlyDictionary<RealmAbilityType, int> UsageByType,
    IReadOnlyList<RealmAbilityUsageStats> PerAbilityStats,
    TimeSpan SessionDuration
);

/// <summary>
/// Represents the current cooldown state of an ability.
/// </summary>
/// <param name="AbilityId">The ability identifier.</param>
/// <param name="AbilityName">The ability display name.</param>
/// <param name="LastUsed">When the ability was last used.</param>
/// <param name="CooldownEnds">When the cooldown will end.</param>
/// <param name="IsReady">Whether the ability is currently available.</param>
public record CooldownState(
    string AbilityId,
    string AbilityName,
    TimeOnly LastUsed,
    TimeOnly CooldownEnds,
    bool IsReady
);

/// <summary>
/// Timeline entry for visualizing realm ability usage.
/// </summary>
/// <param name="Timestamp">When the event occurred.</param>
/// <param name="AbilityName">Name of the ability.</param>
/// <param name="Type">Type of the ability.</param>
/// <param name="EffectValue">Effect value (damage, healing) if applicable.</param>
/// <param name="WasOnOptimalCooldown">Whether the ability was used optimally.</param>
/// <param name="DisplayColor">Color for UI visualization.</param>
public record RealmAbilityTimelineEntry(
    TimeOnly Timestamp,
    string AbilityName,
    RealmAbilityType Type,
    int? EffectValue,
    bool WasOnOptimalCooldown,
    string DisplayColor
)
{
    /// <summary>
    /// Gets a suggested display color based on ability type.
    /// </summary>
    public static string GetColorForType(RealmAbilityType type) => type switch
    {
        RealmAbilityType.Damage => "#FF4444",      // Red
        RealmAbilityType.CrowdControl => "#9944FF", // Purple
        RealmAbilityType.Defensive => "#4499FF",   // Blue
        RealmAbilityType.Healing => "#44FF44",     // Green
        RealmAbilityType.Utility => "#FFAA44",     // Orange
        RealmAbilityType.Passive => "#888888",     // Gray
        _ => "#FFFFFF"                              // White
    };
}
