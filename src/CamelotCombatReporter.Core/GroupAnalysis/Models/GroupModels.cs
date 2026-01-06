using CamelotCombatReporter.Core.Models;

namespace CamelotCombatReporter.Core.GroupAnalysis.Models;

/// <summary>
/// Represents a member of a group detected from combat logs.
/// </summary>
public record GroupMember(
    /// <summary>
    /// The character name.
    /// </summary>
    string Name,

    /// <summary>
    /// The character's class if known.
    /// </summary>
    CharacterClass? Class,

    /// <summary>
    /// The character's realm if known.
    /// </summary>
    Realm? Realm,

    /// <summary>
    /// The primary role this character fills.
    /// </summary>
    GroupRole PrimaryRole,

    /// <summary>
    /// Optional secondary role capability.
    /// </summary>
    GroupRole? SecondaryRole,

    /// <summary>
    /// How this member was detected.
    /// </summary>
    GroupMemberSource Source,

    /// <summary>
    /// First time this member was seen in combat.
    /// </summary>
    TimeOnly FirstSeen,

    /// <summary>
    /// Last time this member was seen in combat.
    /// </summary>
    TimeOnly? LastSeen,

    /// <summary>
    /// True if this is the player character ("You").
    /// </summary>
    bool IsPlayer
)
{
    /// <summary>
    /// Gets a display string for the member.
    /// </summary>
    public string DisplayString => Class.HasValue
        ? $"{Name} ({Class.Value.GetDisplayName()})"
        : Name;
}

/// <summary>
/// Represents the composition of a group at a point in time.
/// </summary>
public record GroupComposition(
    /// <summary>
    /// Unique identifier for this composition.
    /// </summary>
    Guid Id,

    /// <summary>
    /// List of group members.
    /// </summary>
    IReadOnlyList<GroupMember> Members,

    /// <summary>
    /// Size category of the group.
    /// </summary>
    GroupSizeCategory SizeCategory,

    /// <summary>
    /// Best matching template, if any.
    /// </summary>
    GroupTemplate? MatchedTemplate,

    /// <summary>
    /// Balance score from 0-100.
    /// </summary>
    double BalanceScore,

    /// <summary>
    /// When this composition was formed.
    /// </summary>
    TimeOnly FormationTime,

    /// <summary>
    /// When this composition disbanded, if applicable.
    /// </summary>
    TimeOnly? DisbandTime
)
{
    /// <summary>
    /// Gets the member count.
    /// </summary>
    public int MemberCount => Members.Count;

    /// <summary>
    /// Gets members grouped by primary role.
    /// </summary>
    public IReadOnlyDictionary<GroupRole, IReadOnlyList<GroupMember>> MembersByRole =>
        Members
            .GroupBy(m => m.PrimaryRole)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<GroupMember>)g.ToList());
}

/// <summary>
/// Defines requirements for a role in a group template.
/// </summary>
public record RoleRequirement(
    /// <summary>
    /// Minimum number of this role required.
    /// </summary>
    int MinCount,

    /// <summary>
    /// Maximum number of this role recommended.
    /// </summary>
    int MaxCount,

    /// <summary>
    /// Whether this role is essential for the template.
    /// </summary>
    bool IsRequired
);

/// <summary>
/// Represents a known effective group template.
/// </summary>
public record GroupTemplate(
    /// <summary>
    /// Template name (e.g., "8-Man RvR").
    /// </summary>
    string Name,

    /// <summary>
    /// Description of the template's purpose.
    /// </summary>
    string Description,

    /// <summary>
    /// Role requirements for this template.
    /// </summary>
    IReadOnlyDictionary<GroupRole, RoleRequirement> RoleRequirements,

    /// <summary>
    /// Minimum group size for this template.
    /// </summary>
    int MinSize,

    /// <summary>
    /// Maximum group size for this template.
    /// </summary>
    int MaxSize
)
{
    /// <summary>
    /// Calculates how well a composition matches this template.
    /// Returns a score from 0-100.
    /// </summary>
    public double CalculateMatchScore(GroupComposition composition)
    {
        if (composition.MemberCount < MinSize || composition.MemberCount > MaxSize)
            return 0;

        var roleCounts = composition.Members
            .GroupBy(m => m.PrimaryRole)
            .ToDictionary(g => g.Key, g => g.Count());

        double totalScore = 0;
        int requirementCount = 0;

        foreach (var (role, requirement) in RoleRequirements)
        {
            var actualCount = roleCounts.GetValueOrDefault(role, 0);
            requirementCount++;

            if (requirement.IsRequired && actualCount < requirement.MinCount)
            {
                // Missing required role is heavily penalized
                totalScore += 0;
            }
            else if (actualCount >= requirement.MinCount && actualCount <= requirement.MaxCount)
            {
                // Perfect match
                totalScore += 100;
            }
            else if (actualCount < requirement.MinCount)
            {
                // Partial match - some but not enough
                totalScore += (actualCount / (double)requirement.MinCount) * 70;
            }
            else
            {
                // Over the max - slight penalty
                totalScore += 80;
            }
        }

        return requirementCount > 0 ? totalScore / requirementCount : 0;
    }
}

/// <summary>
/// Performance metrics for a group composition.
/// </summary>
public record GroupPerformanceMetrics(
    /// <summary>
    /// The composition this relates to.
    /// </summary>
    Guid CompositionId,

    /// <summary>
    /// Total damage per second across all members.
    /// </summary>
    double TotalDps,

    /// <summary>
    /// Total healing per second across all members.
    /// </summary>
    double TotalHps,

    /// <summary>
    /// Total kills by the group.
    /// </summary>
    int TotalKills,

    /// <summary>
    /// Total deaths in the group.
    /// </summary>
    int TotalDeaths,

    /// <summary>
    /// Kill/Death ratio.
    /// </summary>
    double KillDeathRatio,

    /// <summary>
    /// Average DPS per member.
    /// </summary>
    double AverageMemberDps,

    /// <summary>
    /// Average HPS per member.
    /// </summary>
    double AverageMemberHps,

    /// <summary>
    /// Total time spent in combat.
    /// </summary>
    TimeSpan CombatDuration,

    /// <summary>
    /// Individual contribution breakdown.
    /// </summary>
    IReadOnlyDictionary<string, MemberContribution> MemberContributions
);

/// <summary>
/// Individual member's contribution to group performance.
/// </summary>
public record MemberContribution(
    /// <summary>
    /// The member's name.
    /// </summary>
    string MemberName,

    /// <summary>
    /// The member's primary role.
    /// </summary>
    GroupRole Role,

    /// <summary>
    /// Total damage dealt by this member.
    /// </summary>
    int DamageDealt,

    /// <summary>
    /// Total healing done by this member.
    /// </summary>
    int HealingDone,

    /// <summary>
    /// Kills attributed to this member.
    /// </summary>
    int Kills,

    /// <summary>
    /// Deaths of this member.
    /// </summary>
    int Deaths,

    /// <summary>
    /// Percentage of group DPS from this member.
    /// </summary>
    double DpsContributionPercent,

    /// <summary>
    /// Percentage of group HPS from this member.
    /// </summary>
    double HpsContributionPercent
);

/// <summary>
/// Analysis of how well a role is covered in the composition.
/// </summary>
public record RoleCoverage(
    /// <summary>
    /// The role being analyzed.
    /// </summary>
    GroupRole Role,

    /// <summary>
    /// Number of members filling this role.
    /// </summary>
    int MemberCount,

    /// <summary>
    /// Whether the role is adequately covered.
    /// </summary>
    bool IsCovered,

    /// <summary>
    /// Whether the role is over-represented.
    /// </summary>
    bool IsOverRepresented,

    /// <summary>
    /// Names of members filling this role.
    /// </summary>
    IReadOnlyList<string> MemberNames
)
{
    /// <summary>
    /// Gets a status indicator for UI.
    /// </summary>
    public string StatusIndicator => IsOverRepresented ? "+" : (IsCovered ? "✓" : "!");
}

/// <summary>
/// A recommendation for improving group composition.
/// </summary>
public record CompositionRecommendation(
    /// <summary>
    /// Type of recommendation.
    /// </summary>
    RecommendationType Type,

    /// <summary>
    /// Target role for the recommendation, if applicable.
    /// </summary>
    GroupRole? TargetRole,

    /// <summary>
    /// Human-readable message.
    /// </summary>
    string Message,

    /// <summary>
    /// Priority of this recommendation.
    /// </summary>
    RecommendationPriority Priority
)
{
    /// <summary>
    /// Gets a color code for the priority.
    /// </summary>
    public string PriorityColor => Priority switch
    {
        RecommendationPriority.Low => "#4CAF50",
        RecommendationPriority.Medium => "#FF9800",
        RecommendationPriority.High => "#F44336",
        RecommendationPriority.Critical => "#9C27B0",
        _ => "#9E9E9E"
    };
}

/// <summary>
/// Summary of group analysis results.
/// </summary>
public record GroupAnalysisSummary(
    /// <summary>
    /// The analyzed composition.
    /// </summary>
    GroupComposition Composition,

    /// <summary>
    /// Performance metrics.
    /// </summary>
    GroupPerformanceMetrics Metrics,

    /// <summary>
    /// Role coverage analysis.
    /// </summary>
    IReadOnlyList<RoleCoverage> RoleCoverage,

    /// <summary>
    /// Recommendations for improvement.
    /// </summary>
    IReadOnlyList<CompositionRecommendation> Recommendations
);
