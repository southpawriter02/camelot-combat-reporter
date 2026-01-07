using CamelotCombatReporter.Core.CharacterBuilding.Models;
using CamelotCombatReporter.Core.CrossRealm;
using CamelotCombatReporter.Core.Models;

namespace CamelotCombatReporter.Core.CharacterBuilding.Services;

/// <summary>
/// Analyzes combat sessions to calculate aggregated performance metrics.
/// </summary>
public class PerformanceAnalysisService : IPerformanceAnalysisService
{
    private readonly ICharacterProfileService _profileService;
    private readonly ICrossRealmStatisticsService _sessionService;

    public PerformanceAnalysisService(
        ICharacterProfileService profileService,
        ICrossRealmStatisticsService sessionService)
    {
        _profileService = profileService;
        _sessionService = sessionService;
    }

    public Task<BuildPerformanceMetrics> CalculateMetricsAsync(
        IEnumerable<ExtendedCombatStatistics> sessions)
    {
        var sessionList = sessions.ToList();
        
        if (sessionList.Count == 0)
        {
            return Task.FromResult(BuildPerformanceMetrics.Empty);
        }

        // Aggregate totals
        long totalDamageDealt = 0;
        long totalDamageTaken = 0;
        long totalHealingDone = 0;
        int totalKills = 0;
        int totalDeaths = 0;
        int totalAssists = 0;
        double totalCombatSeconds = 0;
        double peakDps = 0;

        foreach (var session in sessionList)
        {
            totalDamageDealt += session.TotalDamageDealt;
            totalDamageTaken += session.TotalDamageTaken;
            totalHealingDone += session.TotalHealingDone;
            totalKills += session.KillCount;
            totalDeaths += session.DeathCount;
            totalAssists += session.AssistCount;
            totalCombatSeconds += session.Duration.TotalSeconds;
            
            var sessionDps = session.BaseStats.Dps;
            if (sessionDps > peakDps)
            {
                peakDps = sessionDps;
            }
        }

        // Calculate averages
        var avgDps = totalCombatSeconds > 0 ? totalDamageDealt / totalCombatSeconds : 0;
        var avgHps = totalCombatSeconds > 0 ? totalHealingDone / totalCombatSeconds : 0;
        var avgDamageTakenPerSecond = totalCombatSeconds > 0 ? totalDamageTaken / totalCombatSeconds : 0;
        var kdRatio = totalDeaths > 0 ? (double)totalKills / totalDeaths : totalKills;

        var metrics = new BuildPerformanceMetrics
        {
            SessionCount = sessionList.Count,
            TotalCombatTime = TimeSpan.FromSeconds(totalCombatSeconds),
            TotalDamageDealt = totalDamageDealt,
            AverageDps = avgDps,
            PeakDps = peakDps,
            TotalDamageTaken = totalDamageTaken,
            AverageDamageTakenPerSecond = avgDamageTakenPerSecond,
            TotalHealingDone = totalHealingDone,
            AverageHps = avgHps,
            Kills = totalKills,
            Deaths = totalDeaths,
            Assists = totalAssists
        };

        return Task.FromResult(metrics);
    }

    public async Task<BuildPerformanceMetrics> CalculateMetricsForProfileAsync(
        Guid profileId,
        DateRange? dateRange = null,
        Guid? buildId = null)
    {
        var sessionIds = await _profileService.GetAttachedSessionIdsAsync(profileId);
        var sessions = new List<ExtendedCombatStatistics>();

        foreach (var sessionId in sessionIds)
        {
            var session = await _sessionService.GetSessionAsync(sessionId);
            if (session == null) continue;

            // Apply date filter
            if (dateRange != null && !dateRange.Contains(session.SessionStartUtc))
            {
                continue;
            }

            sessions.Add(session);
        }

        return await CalculateMetricsAsync(sessions);
    }

    public async Task<IReadOnlyList<DamageBreakdown>> GetTopDamageSourcesAsync(
        Guid profileId,
        int topN = 10,
        DateRange? dateRange = null)
    {
        // Currently returns empty - damage source tracking would require
        // parsing individual combat events which isn't stored in ExtendedCombatStatistics
        var metrics = await CalculateMetricsForProfileAsync(profileId, dateRange);
        return metrics.TopDamageSources.Values.Take(topN).ToList();
    }

    public async Task UpdateBuildMetricsAsync(Guid profileId)
    {
        var profile = await _profileService.GetProfileAsync(profileId);
        if (profile?.ActiveBuild == null) return;

        var metrics = await CalculateMetricsForProfileAsync(profileId);
        
        var updatedBuild = profile.ActiveBuild with { PerformanceMetrics = metrics };
        await _profileService.UpdateBuildAsync(profileId, updatedBuild);
    }
}
