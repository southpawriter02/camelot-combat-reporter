namespace CamelotCombatReporter.Core.CharacterBuilding.Models;

/// <summary>
/// Result of comparing two character builds.
/// </summary>
public record BuildComparisonResult
{
    /// <summary>
    /// The first build being compared (baseline).
    /// </summary>
    public required CharacterBuild BuildA { get; init; }

    /// <summary>
    /// The second build being compared.
    /// </summary>
    public required CharacterBuild BuildB { get; init; }

    /// <summary>
    /// Differences in spec line allocations.
    /// </summary>
    public IReadOnlyList<SpecDelta> SpecDeltas { get; init; } = [];

    /// <summary>
    /// Differences in realm ability selections.
    /// </summary>
    public IReadOnlyList<RealmAbilityDelta> RealmAbilityDeltas { get; init; } = [];

    /// <summary>
    /// Differences in performance metrics (if both builds have metrics).
    /// </summary>
    public PerformanceDelta? PerformanceDeltas { get; init; }

    /// <summary>
    /// Total spec points difference (positive = B has more).
    /// </summary>
    public int TotalSpecPointsDelta => SpecDeltas.Sum(s => s.Delta);

    /// <summary>
    /// Total RA points difference (positive = B has more).
    /// </summary>
    public int TotalRAPointsDelta => RealmAbilityDeltas.Sum(ra => ra.PointsDelta);

    /// <summary>
    /// Whether the builds are identical in terms of specs and RAs.
    /// </summary>
    public bool AreIdentical => SpecDeltas.All(s => s.Delta == 0) && 
                                 RealmAbilityDeltas.Count == 0;
}

/// <summary>
/// Difference in a single specialization line between builds.
/// </summary>
public record SpecDelta(
    string SpecName,
    int ValueA,
    int ValueB,
    int Delta
)
{
    /// <summary>
    /// Whether this spec increased in build B.
    /// </summary>
    public bool IsIncrease => Delta > 0;

    /// <summary>
    /// Whether this spec decreased in build B.
    /// </summary>
    public bool IsDecrease => Delta < 0;
}

/// <summary>
/// Difference in a realm ability between builds.
/// </summary>
public record RealmAbilityDelta(
    string AbilityName,
    RealmAbilityChangeType ChangeType,
    int RankA,
    int RankB,
    int PointsA,
    int PointsB
)
{
    /// <summary>
    /// Points difference (positive = B costs more).
    /// </summary>
    public int PointsDelta => PointsB - PointsA;
}

/// <summary>
/// Type of change for a realm ability.
/// </summary>
public enum RealmAbilityChangeType
{
    /// <summary>Ability added in build B.</summary>
    Added,

    /// <summary>Ability removed in build B.</summary>
    Removed,

    /// <summary>Ability rank changed between builds.</summary>
    RankChanged
}

/// <summary>
/// Differences in performance metrics between builds.
/// </summary>
public record PerformanceDelta
{
    public double DpsDelta { get; init; }
    public double HpsDelta { get; init; }
    public double KdRatioDelta { get; init; }
    public int KillsDelta { get; init; }
    public int DeathsDelta { get; init; }
    
    /// <summary>
    /// Whether build B has better DPS.
    /// </summary>
    public bool IsDpsImproved => DpsDelta > 0;
    
    /// <summary>
    /// Whether build B has better K/D ratio.
    /// </summary>
    public bool IsKdImproved => KdRatioDelta > 0;
}
