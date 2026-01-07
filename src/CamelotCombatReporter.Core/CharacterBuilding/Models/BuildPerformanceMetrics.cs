namespace CamelotCombatReporter.Core.CharacterBuilding.Models;

/// <summary>
/// Aggregated performance metrics for a specific build.
/// Calculated from combat sessions attached to this build.
/// </summary>
public record BuildPerformanceMetrics
{
    /// <summary>
    /// Empty metrics with zero values.
    /// </summary>
    public static BuildPerformanceMetrics Empty => new();
    /// <summary>
    /// Number of combat sessions contributing to these metrics.
    /// </summary>
    public int SessionCount { get; init; }
    
    /// <summary>
    /// Total time spent in combat across all sessions.
    /// </summary>
    public TimeSpan TotalCombatTime { get; init; }

    // Damage output metrics
    public long TotalDamageDealt { get; init; }
    public double AverageDps { get; init; }
    public double PeakDps { get; init; }
    public double MedianDamagePerHit { get; init; }
    public double CriticalHitRate { get; init; }

    // Damage mitigation metrics
    public long TotalDamageTaken { get; init; }
    public double AverageDamageTakenPerSecond { get; init; }
    
    /// <summary>
    /// Combined rate of blocks, parries, and evades.
    /// </summary>
    public double AvoidanceRate { get; init; }

    // Healing metrics (for healers/hybrids)
    public long TotalHealingDone { get; init; }
    public double AverageHps { get; init; }
    public double OverhealPercent { get; init; }

    // Combat effectiveness
    public int Kills { get; init; }
    public int Deaths { get; init; }
    public int Assists { get; init; }
    
    /// <summary>
    /// Kill/Death ratio (Kills / max(Deaths, 1)).
    /// </summary>
    public double KillDeathRatio => Deaths > 0 ? (double)Kills / Deaths : Kills;

    /// <summary>
    /// Breakdown of damage by source (style/ability name).
    /// </summary>
    public IReadOnlyDictionary<string, DamageBreakdown> TopDamageSources { get; init; } =
        new Dictionary<string, DamageBreakdown>();
}

/// <summary>
/// Damage statistics for a single damage source (style or ability).
/// </summary>
/// <param name="SourceName">Name of the style or ability.</param>
/// <param name="TotalDamage">Total damage dealt by this source.</param>
/// <param name="HitCount">Number of times this source hit.</param>
/// <param name="AverageDamage">Average damage per hit.</param>
/// <param name="PercentOfTotal">Percentage of total damage from this source.</param>
public record DamageBreakdown(
    string SourceName,
    long TotalDamage,
    int HitCount,
    double AverageDamage,
    double PercentOfTotal
);

/// <summary>
/// Tracks realm rank progression over time with performance correlation.
/// </summary>
public record RealmRankProgression
{
    /// <summary>
    /// Milestones recording performance at each realm rank achieved.
    /// </summary>
    public IReadOnlyList<RankMilestone> Milestones { get; init; } = [];
}

/// <summary>
/// Performance snapshot at a specific realm rank milestone.
/// </summary>
public record RankMilestone
{
    /// <summary>
    /// Realm rank at this milestone (1-14).
    /// </summary>
    public int RealmRank { get; init; }
    
    /// <summary>
    /// Total realm points accumulated at this milestone.
    /// </summary>
    public long RealmPoints { get; init; }
    
    /// <summary>
    /// When this rank was achieved.
    /// </summary>
    public DateTime AchievedUtc { get; init; }

    // Performance metrics at this rank
    public double AverageDps { get; init; }
    public double AverageHps { get; init; }
    public double KillDeathRatio { get; init; }
    
    /// <summary>
    /// Number of sessions contributing to these averages.
    /// </summary>
    public int SessionCount { get; init; }
}
