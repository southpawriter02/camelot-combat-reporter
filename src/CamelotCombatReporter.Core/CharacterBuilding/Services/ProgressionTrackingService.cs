using CamelotCombatReporter.Core.CharacterBuilding.Models;

namespace CamelotCombatReporter.Core.CharacterBuilding.Services;

/// <summary>
/// Tracks realm rank progression and calculates milestone statistics.
/// </summary>
public class ProgressionTrackingService : IProgressionTrackingService
{
    private readonly ICharacterProfileService _profileService;

    // RP thresholds for each realm rank (1-14)
    private static readonly long[] RankThresholds =
    [
        0,          // RR1
        25_000,     // RR2
        125_000,    // RR3
        350_000,    // RR4
        750_000,    // RR5
        1_375_000,  // RR6
        2_275_000,  // RR7
        3_500_000,  // RR8
        5_100_000,  // RR9
        7_125_000,  // RR10
        9_625_000,  // RR11
        12_650_000, // RR12
        16_250_000, // RR13
        20_475_000  // RR14
    ];

    public ProgressionTrackingService(ICharacterProfileService profileService)
    {
        _profileService = profileService;
    }

    public async Task RecordMilestoneAsync(
        Guid profileId, 
        RankMilestone milestone, 
        CancellationToken ct = default)
    {
        var profile = await _profileService.GetProfileAsync(profileId, ct);
        if (profile == null) return;

        var milestones = (profile.RankProgression?.Milestones ?? []).ToList();
        
        // Check if this rank is already recorded
        if (milestones.Exists(m => m.RealmRank == milestone.RealmRank))
            return;

        milestones.Add(milestone);
        milestones = milestones.OrderBy(m => m.RealmRank).ToList();

        var updatedProfile = profile with
        {
            RankProgression = new RealmRankProgression { Milestones = milestones }
        };

        await _profileService.UpdateProfileAsync(updatedProfile, ct);
    }

    public async Task<RealmRankProgression> GetProgressionAsync(
        Guid profileId, 
        CancellationToken ct = default)
    {
        var profile = await _profileService.GetProfileAsync(profileId, ct);
        return profile?.RankProgression ?? new RealmRankProgression();
    }

    public async Task<RankMilestone?> GetCurrentMilestoneAsync(
        Guid profileId, 
        CancellationToken ct = default)
    {
        var progression = await GetProgressionAsync(profileId, ct);
        return progression.Milestones.LastOrDefault();
    }

    public ProgressionSummary CalculateProgressionSummary(RealmRankProgression progression)
    {
        var milestones = progression.Milestones.ToList();
        
        if (milestones.Count == 0)
        {
            return new ProgressionSummary();
        }

        var current = milestones.Last();
        var daysBetween = CalculateAverageDaysBetweenRanks(milestones);
        var rpPerSession = CalculateAverageRpPerSession(milestones, current.RealmPoints);
        var dpsTrend = CalculateTrend(milestones, m => m.AverageDps);
        var kdTrend = CalculateTrend(milestones, m => m.KillDeathRatio);

        TimeSpan? timeSinceLastRank = null;
        TimeSpan? estTimeToNext = null;

        if (milestones.Count >= 1)
        {
            timeSinceLastRank = DateTime.UtcNow - current.AchievedUtc;
            
            if (current.RealmRank < 14 && rpPerSession > 0)
            {
                var rpToNext = RankThresholds[current.RealmRank] - current.RealmPoints;
                if (rpToNext > 0 && current.SessionCount > 0)
                {
                    var sessionsToNext = rpToNext / rpPerSession;
                    // Assuming ~1 hour per session average
                    estTimeToNext = TimeSpan.FromHours(sessionsToNext);
                }
            }
        }

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

    public async Task<RankMilestone?> CheckAndRecordRankUpAsync(
        Guid profileId,
        int newRealmRank,
        long newTotalRealmPoints,
        BuildPerformanceMetrics? currentMetrics = null,
        CancellationToken ct = default)
    {
        var current = await GetCurrentMilestoneAsync(profileId, ct);
        
        // Check if this is a new rank
        if (current != null && current.RealmRank >= newRealmRank)
            return null;

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

    private static double CalculateAverageRpPerSession(List<RankMilestone> milestones, long totalRp)
    {
        var totalSessions = milestones.Sum(m => m.SessionCount);
        return totalSessions > 0 ? (double)totalRp / totalSessions : 0;
    }

    private static double CalculateTrend(List<RankMilestone> milestones, Func<RankMilestone, double> selector)
    {
        if (milestones.Count < 2) return 0;

        // Simple linear trend: compare first half average to second half average
        var mid = milestones.Count / 2;
        var firstHalf = milestones.Take(mid).Average(selector);
        var secondHalf = milestones.Skip(mid).Average(selector);

        return secondHalf - firstHalf;
    }

    /// <summary>
    /// Gets the RP required to reach a specific realm rank.
    /// </summary>
    public static long GetRpForRank(int realmRank)
    {
        if (realmRank < 1 || realmRank > 14) return 0;
        return RankThresholds[realmRank - 1];
    }

    /// <summary>
    /// Gets the realm rank for a given RP total.
    /// </summary>
    public static int GetRankForRp(long realmPoints)
    {
        for (int i = RankThresholds.Length - 1; i >= 0; i--)
        {
            if (realmPoints >= RankThresholds[i])
                return i + 1;
        }
        return 1;
    }
}
