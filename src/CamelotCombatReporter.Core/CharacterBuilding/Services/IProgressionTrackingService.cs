using CamelotCombatReporter.Core.CharacterBuilding.Models;

namespace CamelotCombatReporter.Core.CharacterBuilding.Services;

/// <summary>
/// Service for tracking realm rank progression and milestones.
/// </summary>
public interface IProgressionTrackingService
{
    /// <summary>
    /// Records a new rank milestone for a profile.
    /// </summary>
    Task RecordMilestoneAsync(Guid profileId, RankMilestone milestone, CancellationToken ct = default);

    /// <summary>
    /// Gets the full progression history for a profile.
    /// </summary>
    Task<RealmRankProgression> GetProgressionAsync(Guid profileId, CancellationToken ct = default);

    /// <summary>
    /// Gets the most recent milestone for a profile.
    /// </summary>
    Task<RankMilestone?> GetCurrentMilestoneAsync(Guid profileId, CancellationToken ct = default);

    /// <summary>
    /// Calculates summary statistics from progression data.
    /// </summary>
    ProgressionSummary CalculateProgressionSummary(RealmRankProgression progression);

    /// <summary>
    /// Detects if a rank up occurred and records milestone automatically.
    /// </summary>
    Task<RankMilestone?> CheckAndRecordRankUpAsync(
        Guid profileId,
        int newRealmRank,
        long newTotalRealmPoints,
        BuildPerformanceMetrics? currentMetrics = null,
        CancellationToken ct = default);
}

/// <summary>
/// Summary statistics for progression analysis.
/// </summary>
public record ProgressionSummary
{
    /// <summary>
    /// Current realm rank.
    /// </summary>
    public int CurrentRank { get; init; }

    /// <summary>
    /// Total realm points accumulated.
    /// </summary>
    public long TotalRealmPoints { get; init; }

    /// <summary>
    /// Number of recorded milestones.
    /// </summary>
    public int MilestoneCount { get; init; }

    /// <summary>
    /// Average days between rank ups.
    /// </summary>
    public double AverageDaysBetweenRanks { get; init; }

    /// <summary>
    /// Average RP per session across all milestones.
    /// </summary>
    public double AverageRpPerSession { get; init; }

    /// <summary>
    /// DPS trend (positive = improving).
    /// </summary>
    public double DpsTrend { get; init; }

    /// <summary>
    /// K/D trend (positive = improving).
    /// </summary>
    public double KdTrend { get; init; }

    /// <summary>
    /// Time since last rank up.
    /// </summary>
    public TimeSpan? TimeSinceLastRankUp { get; init; }

    /// <summary>
    /// Estimated time to next rank based on current RP rate.
    /// </summary>
    public TimeSpan? EstimatedTimeToNextRank { get; init; }
}
