using CamelotCombatReporter.Core.Models;

namespace CamelotCombatReporter.Core.DeathAnalysis.Models;

/// <summary>
/// Categories of death based on damage patterns and time-to-death.
/// </summary>
public enum DeathCategory
{
    // Burst Deaths (TTD < 3 seconds) - Rapid, overwhelming damage

    /// <summary>Single massive hit from one attacker.</summary>
    BurstAlphaStrike,
    /// <summary>Multiple attackers dealing damage simultaneously.</summary>
    BurstCoordinated,
    /// <summary>Killed while under crowd control effects.</summary>
    BurstCCChain,

    // Attrition Deaths (TTD > 10 seconds) - Slow, grinding damage

    /// <summary>Damage exceeded available healing over time.</summary>
    AttritionHealingDeficit,
    /// <summary>Ran out of defensive abilities or resources.</summary>
    AttritionResourceExhaustion,
    /// <summary>Poor positioning led to taking excessive damage.</summary>
    AttritionPositional,

    // Execution Deaths - Specific kill patterns

    /// <summary>Killed at low health by an execute-style ability.</summary>
    ExecutionLowHealth,
    /// <summary>Killed by damage over time effects.</summary>
    ExecutionDoT,
    /// <summary>Environmental damage (fall, hazard, etc.).</summary>
    Environmental,

    /// <summary>Could not determine death category.</summary>
    Unknown
}

/// <summary>
/// Type of recommendation for death prevention.
/// </summary>
public enum RecommendationType
{
    /// <summary>Use a specific ability differently.</summary>
    AbilityUsage,
    /// <summary>Improve positioning during combat.</summary>
    Positioning,
    /// <summary>Pay more attention to enemy actions.</summary>
    Awareness,
    /// <summary>Disengage from fights earlier.</summary>
    Disengagement,
    /// <summary>Counter-play against specific classes.</summary>
    ClassCounter
}

/// <summary>
/// Priority of a recommendation.
/// </summary>
public enum RecommendationPriority
{
    Low,
    Medium,
    High,
    Critical
}

/// <summary>
/// Represents the killing blow that caused a death.
/// </summary>
/// <param name="AttackerName">The name of the attacker who dealt the killing blow.</param>
/// <param name="AttackerClass">The class of the attacker, if known.</param>
/// <param name="AbilityName">The ability or attack that caused the killing blow.</param>
/// <param name="DamageAmount">The damage dealt by the killing blow.</param>
/// <param name="OverkillAmount">Damage beyond what was needed to kill (excess damage).</param>
/// <param name="DamageType">The type of damage (physical, magical, etc.).</param>
public record KillingBlow(
    string AttackerName,
    CharacterClass? AttackerClass,
    string? AbilityName,
    int DamageAmount,
    int OverkillAmount,
    string DamageType
);

/// <summary>
/// Represents damage from a single source during the pre-death window.
/// </summary>
/// <param name="AttackerName">The name of the attacker.</param>
/// <param name="AttackerClass">The class of the attacker, if known.</param>
/// <param name="TotalDamage">Total damage dealt by this source.</param>
/// <param name="PercentOfTotal">Percentage of total pre-death damage.</param>
/// <param name="Events">The individual damage events from this source.</param>
public record DamageSource(
    string AttackerName,
    CharacterClass? AttackerClass,
    int TotalDamage,
    double PercentOfTotal,
    IReadOnlyList<DamageEvent> Events
);

/// <summary>
/// Represents a bucket of damage during a specific second of the pre-death timeline.
/// </summary>
/// <param name="SecondOffset">Seconds before death (negative, e.g., -5 = 5 seconds before death).</param>
/// <param name="TotalDamage">Total damage taken during this second.</param>
/// <param name="Events">The damage events during this second.</param>
public record DamageTimelineBucket(
    int SecondOffset,
    int TotalDamage,
    IReadOnlyList<DamageEvent> Events
);

/// <summary>
/// Represents a defensive ability that could have been used but wasn't.
/// </summary>
/// <param name="AbilityName">The name of the ability.</param>
/// <param name="Description">Description of why it would have helped.</param>
/// <param name="OptimalUseTime">The ideal time to have used the ability.</param>
/// <param name="ExpectedBenefit">The expected benefit of using the ability.</param>
public record MissedOpportunity(
    string AbilityName,
    string Description,
    TimeOnly? OptimalUseTime,
    string ExpectedBenefit
);

/// <summary>
/// Represents a recommendation for preventing similar deaths.
/// </summary>
/// <param name="Type">The type of recommendation.</param>
/// <param name="Title">Short title of the recommendation.</param>
/// <param name="Description">Detailed description of the recommendation.</param>
/// <param name="Priority">How important this recommendation is.</param>
public record Recommendation(
    RecommendationType Type,
    string Title,
    string Description,
    RecommendationPriority Priority
);

/// <summary>
/// Complete death analysis report.
/// </summary>
/// <param name="Id">Unique identifier for this report.</param>
/// <param name="DeathEvent">The original death event.</param>
/// <param name="Category">The categorized death type.</param>
/// <param name="TimeToDeath">Time from first damage to death.</param>
/// <param name="KillingBlow">The killing blow information, if determinable.</param>
/// <param name="DamageSources">Breakdown of damage by source.</param>
/// <param name="DamageTimeline">Damage over time leading to death.</param>
/// <param name="MissedOpportunities">Defensive abilities that could have been used.</param>
/// <param name="Recommendations">Recommendations for preventing similar deaths.</param>
/// <param name="TotalDamageTaken">Total damage taken in the pre-death window.</param>
/// <param name="TotalHealingReceived">Total healing received in the pre-death window.</param>
/// <param name="WasCrowdControlled">Whether the player was CC'd before death.</param>
/// <param name="AttackerCount">Number of distinct attackers.</param>
public record DeathReport(
    Guid Id,
    DeathEvent DeathEvent,
    DeathCategory Category,
    TimeSpan TimeToDeath,
    KillingBlow? KillingBlow,
    IReadOnlyList<DamageSource> DamageSources,
    IReadOnlyList<DamageTimelineBucket> DamageTimeline,
    IReadOnlyList<MissedOpportunity> MissedOpportunities,
    IReadOnlyList<Recommendation> Recommendations,
    int TotalDamageTaken,
    int TotalHealingReceived,
    bool WasCrowdControlled,
    int AttackerCount
);

/// <summary>
/// Aggregated death statistics for a combat session.
/// </summary>
/// <param name="TotalDeaths">Total number of deaths analyzed.</param>
/// <param name="DeathsPerHour">Average deaths per hour.</param>
/// <param name="AverageTimeToDeath">Average time from first damage to death.</param>
/// <param name="TopKillerClasses">Most frequent killing classes.</param>
/// <param name="TopKillingAbilities">Most frequent killing abilities.</param>
/// <param name="DeathsByCategory">Deaths grouped by category.</param>
/// <param name="AverageDamageTaken">Average damage taken per death.</param>
/// <param name="CCDeathPercent">Percentage of deaths that occurred while CC'd.</param>
public record DeathStatistics(
    int TotalDeaths,
    double DeathsPerHour,
    TimeSpan AverageTimeToDeath,
    IReadOnlyDictionary<CharacterClass, int> TopKillerClasses,
    IReadOnlyDictionary<string, int> TopKillingAbilities,
    IReadOnlyDictionary<DeathCategory, int> DeathsByCategory,
    double AverageDamageTaken,
    double CCDeathPercent
);
