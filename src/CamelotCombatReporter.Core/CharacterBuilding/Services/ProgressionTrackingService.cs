using CamelotCombatReporter.Core.CharacterBuilding.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace CamelotCombatReporter.Core.CharacterBuilding.Services;

/// <summary>
/// Tracks realm rank progression and calculates milestone statistics.
/// </summary>
/// <remarks>
/// <para>
/// This service manages the recording and analysis of realm rank progression
/// milestones. Key features include:
/// </para>
/// <list type="bullet">
///   <item><description>Recording milestones when rank-ups occur</description></item>
///   <item><description>Calculating progression summary statistics</description></item>
///   <item><description>Detecting rank-ups and auto-recording milestones</description></item>
///   <item><description>Estimating time to next rank based on session RP rates</description></item>
/// </list>
/// <para>
/// Realm ranks in DAoC range from RR1 to RR14, with increasing RP requirements
/// at each tier. The service provides static utilities for RP-to-rank conversions.
/// </para>
/// </remarks>
public class ProgressionTrackingService : IProgressionTrackingService
{
    private readonly ICharacterProfileService _profileService;
    private readonly ILogger<ProgressionTrackingService> _logger;

    // ─────────────────────────────────────────────────────────────────────────
    // Realm Rank RP Thresholds
    // ─────────────────────────────────────────────────────────────────────────
    // These are the cumulative RP required to reach each realm rank (1-14).
    // Source: Dark Age of Camelot realm rank progression system.
    private static readonly long[] RankThresholds =
    [
        0,          // RR1  - Starting rank
        25_000,     // RR2  - First major milestone
        125_000,    // RR3  
        350_000,    // RR4  
        750_000,    // RR5  - Significant PvP experience
        1_375_000,  // RR6  
        2_275_000,  // RR7  
        3_500_000,  // RR8  
        5_100_000,  // RR9  
        7_125_000,  // RR10 - Veteran rank
        9_625_000,  // RR11 
        12_650_000, // RR12 
        16_250_000, // RR13 
        20_475_000  // RR14 - Maximum rank (legendary)
    ];

    /// <summary>
    /// Initializes a new instance of the ProgressionTrackingService.
    /// </summary>
    /// <param name="profileService">Service for accessing character profiles.</param>
    /// <param name="logger">Optional logger for diagnostics.</param>
    public ProgressionTrackingService(
        ICharacterProfileService profileService,
        ILogger<ProgressionTrackingService>? logger = null)
    {
        _profileService = profileService;
        _logger = logger ?? NullLogger<ProgressionTrackingService>.Instance;
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Milestones are stored in rank order. Duplicate ranks are ignored
    /// to prevent overwriting historical performance data.
    /// </remarks>
    public async Task RecordMilestoneAsync(
        Guid profileId, 
        RankMilestone milestone, 
        CancellationToken ct = default)
    {
        var profile = await _profileService.GetProfileAsync(profileId, ct);
        if (profile == null)
        {
            _logger.LogWarning("Cannot record milestone - profile {ProfileId} not found", profileId);
            return;
        }

        var milestones = (profile.RankProgression?.Milestones ?? []).ToList();
        
        // Prevent duplicate rank entries
        if (milestones.Exists(m => m.RealmRank == milestone.RealmRank))
        {
            _logger.LogDebug("Milestone for RR{Rank} already exists, skipping", milestone.RealmRank);
            return;
        }

        milestones.Add(milestone);
        milestones = milestones.OrderBy(m => m.RealmRank).ToList();

        var updatedProfile = profile with
        {
            RankProgression = new RealmRankProgression { Milestones = milestones }
        };

        await _profileService.UpdateProfileAsync(updatedProfile, ct);
        
        _logger.LogInformation(
            "Recorded milestone: RR{Rank} ({RP:N0} RP) for profile '{Name}'",
            milestone.RealmRank, milestone.RealmPoints, profile.Name);
    }

    /// <inheritdoc/>
    public async Task<RealmRankProgression> GetProgressionAsync(
        Guid profileId, 
        CancellationToken ct = default)
    {
        var profile = await _profileService.GetProfileAsync(profileId, ct);
        return profile?.RankProgression ?? new RealmRankProgression();
    }

    /// <inheritdoc/>
    public async Task<RankMilestone?> GetCurrentMilestoneAsync(
        Guid profileId, 
        CancellationToken ct = default)
    {
        var progression = await GetProgressionAsync(profileId, ct);
        return progression.Milestones.LastOrDefault();
    }

    /// <inheritdoc/>
    /// <remarks>
    /// The summary includes:
    /// <list type="bullet">
    ///   <item><description>Average days between rank-ups</description></item>
    ///   <item><description>Average RP earned per session</description></item>
    ///   <item><description>DPS and K/D trends (positive = improving)</description></item>
    ///   <item><description>Estimated time to next rank based on current RP rate</description></item>
    /// </list>
    /// Trends are calculated by comparing first-half to second-half averages
    /// of recorded milestones.
    /// </remarks>
    public ProgressionSummary CalculateProgressionSummary(RealmRankProgression progression)
    {
        var milestones = progression.Milestones.ToList();
        
        if (milestones.Count == 0)
        {
            _logger.LogDebug("No milestones recorded - returning empty summary");
            return new ProgressionSummary();
        }

        var current = milestones.Last();
        
        // Calculate progression statistics
        var daysBetween = CalculateAverageDaysBetweenRanks(milestones);
        var rpPerSession = CalculateAverageRpPerSession(milestones, current.RealmPoints);
        var dpsTrend = CalculateTrend(milestones, m => m.AverageDps);
        var kdTrend = CalculateTrend(milestones, m => m.KillDeathRatio);

        TimeSpan? timeSinceLastRank = null;
        TimeSpan? estTimeToNext = null;

        if (milestones.Count >= 1)
        {
            timeSinceLastRank = DateTime.UtcNow - current.AchievedUtc;
            
            // Estimate time to next rank (only if not max rank and have RP/session data)
            if (current.RealmRank < 14 && rpPerSession > 0)
            {
                var rpToNext = RankThresholds[current.RealmRank] - current.RealmPoints;
                if (rpToNext > 0 && current.SessionCount > 0)
                {
                    var sessionsToNext = rpToNext / rpPerSession;
                    // Assumption: ~1 hour average per combat session
                    estTimeToNext = TimeSpan.FromHours(sessionsToNext);
                }
            }
        }

        _logger.LogDebug(
            "Progression summary: RR{Rank}, {Milestones} milestones, {Days:F1} avg days/rank",
            current.RealmRank, milestones.Count, daysBetween);

        return new ProgressionSummary
        {
            CurrentRank = current.RealmRank,
            TotalRealmPoints = current.RealmPoints,
            MilestoneCount = milestones.Count,
            AverageDaysBetweenRanks = daysBetween,
            AverageRpPerSession = rpPerSession,
            DpsTrend = dpsTrend,
            KdTrend = kdTrend,
            TimeSinceLastRankUp = timeSinceLastRank,
            EstimatedTimeToNextRank = estTimeToNext
        };
    }

    /// <inheritdoc/>
    /// <remarks>
    /// This is typically called after processing combat sessions to detect
    /// if the player has ranked up. If the new rank is higher than the
    /// current milestone, a new milestone is automatically recorded.
    /// </remarks>
    public async Task<RankMilestone?> CheckAndRecordRankUpAsync(
        Guid profileId,
        int newRealmRank,
        long newTotalRealmPoints,
        BuildPerformanceMetrics? currentMetrics = null,
        CancellationToken ct = default)
    {
        var current = await GetCurrentMilestoneAsync(profileId, ct);
        
        // Only record if this is a new, higher rank
        if (current != null && current.RealmRank >= newRealmRank)
        {
            return null;
        }

        _logger.LogInformation(
            "Rank up detected: RR{OldRank} → RR{NewRank}",
            current?.RealmRank ?? 0, newRealmRank);

        var milestone = new RankMilestone
        {
            RealmRank = newRealmRank,
            RealmPoints = newTotalRealmPoints,
            AchievedUtc = DateTime.UtcNow,
            AverageDps = currentMetrics?.AverageDps ?? 0,
            AverageHps = currentMetrics?.AverageHps ?? 0,
            KillDeathRatio = currentMetrics?.KillDeathRatio ?? 0,
            SessionCount = currentMetrics?.SessionCount ?? 0
        };

        await RecordMilestoneAsync(profileId, milestone, ct);
        return milestone;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Statistical Calculations
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Calculates the average number of days between rank-up milestones.
    /// </summary>
    private static double CalculateAverageDaysBetweenRanks(List<RankMilestone> milestones)
    {
        if (milestones.Count < 2) return 0;

        var totalDays = 0.0;
        for (int i = 1; i < milestones.Count; i++)
        {
            totalDays += (milestones[i].AchievedUtc - milestones[i - 1].AchievedUtc).TotalDays;
        }

        return totalDays / (milestones.Count - 1);
    }

    /// <summary>
    /// Calculates average RP earned per session across all milestones.
    /// </summary>
    private static double CalculateAverageRpPerSession(List<RankMilestone> milestones, long totalRp)
    {
        var totalSessions = milestones.Sum(m => m.SessionCount);
        return totalSessions > 0 ? (double)totalRp / totalSessions : 0;
    }

    /// <summary>
    /// Calculates a simple trend by comparing first-half to second-half averages.
    /// </summary>
    /// <param name="milestones">List of milestones with the metric.</param>
    /// <param name="selector">Function to extract the metric value.</param>
    /// <returns>Positive value = improving, negative = declining, zero = stable.</returns>
    private static double CalculateTrend(List<RankMilestone> milestones, Func<RankMilestone, double> selector)
    {
        if (milestones.Count < 2) return 0;

        // Split milestones into halves and compare averages
        var mid = milestones.Count / 2;
        var firstHalf = milestones.Take(mid).Average(selector);
        var secondHalf = milestones.Skip(mid).Average(selector);

        return secondHalf - firstHalf;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Static Utilities
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Gets the RP required to reach a specific realm rank.
    /// </summary>
    /// <param name="realmRank">Target realm rank (1-14).</param>
    /// <returns>Cumulative RP required, or 0 if rank is out of range.</returns>
    public static long GetRpForRank(int realmRank)
    {
        if (realmRank < 1 || realmRank > 14) return 0;
        return RankThresholds[realmRank - 1];
    }

    /// <summary>
    /// Gets the realm rank for a given RP total.
    /// </summary>
    /// <param name="realmPoints">Total accumulated realm points.</param>
    /// <returns>Current realm rank (1-14).</returns>
    public static int GetRankForRp(long realmPoints)
    {
        // Iterate from highest rank down to find the first threshold met
        for (int i = RankThresholds.Length - 1; i >= 0; i--)
        {
            if (realmPoints >= RankThresholds[i])
                return i + 1;
        }
        return 1;
    }
}

