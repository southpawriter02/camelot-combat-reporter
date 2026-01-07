using CamelotCombatReporter.Core.CharacterBuilding.Models;
using CamelotCombatReporter.Core.CrossRealm;
using CamelotCombatReporter.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace CamelotCombatReporter.Core.CharacterBuilding.Services;

/// <summary>
/// Analyzes combat sessions to calculate aggregated performance metrics.
/// </summary>
/// <remarks>
/// <para>
/// This service aggregates data from multiple combat sessions to produce
/// comprehensive performance metrics for a character build. Key metrics include:
/// </para>
/// <list type="bullet">
///   <item><description>DPS (damage per second) - average and peak</description></item>
///   <item><description>HPS (healing per second) - for healer classes</description></item>
///   <item><description>K/D ratio - kills to deaths ratio</description></item>
///   <item><description>Combat time - total and per-session averages</description></item>
/// </list>
/// <para>
/// Metrics can be filtered by date range and optionally by specific build ID.
/// </para>
/// </remarks>
public class PerformanceAnalysisService : IPerformanceAnalysisService
{
    private readonly ICharacterProfileService _profileService;
    private readonly ICrossRealmStatisticsService _sessionService;
    private readonly ILogger<PerformanceAnalysisService> _logger;

    /// <summary>
    /// Initializes a new instance of the PerformanceAnalysisService.
    /// </summary>
    /// <param name="profileService">Service for accessing character profiles.</param>
    /// <param name="sessionService">Service for accessing combat session data.</param>
    /// <param name="logger">Optional logger for diagnostics.</param>
    public PerformanceAnalysisService(
        ICharacterProfileService profileService,
        ICrossRealmStatisticsService sessionService,
        ILogger<PerformanceAnalysisService>? logger = null)
    {
        _profileService = profileService;
        _sessionService = sessionService;
        _logger = logger ?? NullLogger<PerformanceAnalysisService>.Instance;
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Iterates through all provided sessions to accumulate totals for
    /// damage, healing, kills, deaths, and combat time. Then calculates
    /// per-second rates and ratios from these totals.
    /// </remarks>
    public Task<BuildPerformanceMetrics> CalculateMetricsAsync(
        IEnumerable<ExtendedCombatStatistics> sessions)
    {
        var sessionList = sessions.ToList();
        
        if (sessionList.Count == 0)
        {
            _logger.LogDebug("No sessions provided - returning empty metrics");
            return Task.FromResult(BuildPerformanceMetrics.Empty);
        }

        _logger.LogDebug("Calculating metrics from {Count} sessions", sessionList.Count);

        // Aggregate totals across all sessions
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
            // Accumulate damage and healing totals
            totalDamageDealt += session.TotalDamageDealt;
            totalDamageTaken += session.TotalDamageTaken;
            totalHealingDone += session.TotalHealingDone;
            
            // Accumulate kill/death statistics
            totalKills += session.KillCount;
            totalDeaths += session.DeathCount;
            totalAssists += session.AssistCount;
            
            // Track combat duration for rate calculations
            totalCombatSeconds += session.Duration.TotalSeconds;
            
            // Track peak DPS across all sessions
            var sessionDps = session.BaseStats.Dps;
            if (sessionDps > peakDps)
            {
                peakDps = sessionDps;
            }
        }

        // Calculate per-second rates (guard against division by zero)
        var avgDps = totalCombatSeconds > 0 ? totalDamageDealt / totalCombatSeconds : 0;
        var avgHps = totalCombatSeconds > 0 ? totalHealingDone / totalCombatSeconds : 0;
        var avgDamageTakenPerSecond = totalCombatSeconds > 0 ? totalDamageTaken / totalCombatSeconds : 0;
        
        // Calculate K/D ratio (if no deaths, K/D equals total kills)
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

        _logger.LogInformation(
            "Metrics calculated: {Sessions} sessions, DPS: {DPS:F1}, K/D: {KD:F2}, Combat: {Time}",
            sessionList.Count, avgDps, kdRatio, metrics.TotalCombatTime);

        return Task.FromResult(metrics);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Retrieves attached session IDs from the profile service, then fetches
    /// each session from the statistics service. Applies date filtering if
    /// specified before calculating metrics.
    /// </remarks>
    public async Task<BuildPerformanceMetrics> CalculateMetricsForProfileAsync(
        Guid profileId,
        DateRange? dateRange = null,
        Guid? buildId = null)
    {
        _logger.LogDebug("Calculating metrics for profile {ProfileId}", profileId);
        
        var sessionIds = await _profileService.GetAttachedSessionIdsAsync(profileId);
        var sessions = new List<ExtendedCombatStatistics>();
        var skippedByDate = 0;

        foreach (var sessionId in sessionIds)
        {
            // Fetch session data from the statistics service
            var session = await _sessionService.GetSessionAsync(sessionId);
            if (session == null)
            {
                _logger.LogWarning("Session {SessionId} not found", sessionId);
                continue;
            }

            // Apply date range filter if specified
            if (dateRange != null && !dateRange.Contains(session.SessionStartUtc))
            {
                skippedByDate++;
                continue;
            }

            sessions.Add(session);
        }

        if (skippedByDate > 0)
        {
            _logger.LogDebug("Filtered out {Count} sessions by date range", skippedByDate);
        }

        return await CalculateMetricsAsync(sessions);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Currently returns top damage sources from cached metrics.
    /// Full damage source tracking would require parsing individual
    /// combat events, which is not currently stored in ExtendedCombatStatistics.
    /// </remarks>
    public async Task<IReadOnlyList<DamageBreakdown>> GetTopDamageSourcesAsync(
        Guid profileId,
        int topN = 10,
        DateRange? dateRange = null)
    {
        _logger.LogDebug("Getting top {Top} damage sources for profile {ProfileId}", topN, profileId);
        
        var metrics = await CalculateMetricsForProfileAsync(profileId, dateRange);
        return metrics.TopDamageSources.Values.Take(topN).ToList();
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Calculates fresh metrics from all attached sessions and updates
    /// the active build with the new performance data. Creates a new
    /// build version (builds are immutable).
    /// </remarks>
    public async Task UpdateBuildMetricsAsync(Guid profileId)
    {
        var profile = await _profileService.GetProfileAsync(profileId);
        if (profile?.ActiveBuild == null)
        {
            _logger.LogWarning("Cannot update metrics - profile {ProfileId} has no active build", profileId);
            return;
        }

        _logger.LogInformation("Updating build metrics for profile '{Name}'", profile.Name);
        
        var metrics = await CalculateMetricsForProfileAsync(profileId);
        
        var updatedBuild = profile.ActiveBuild with { PerformanceMetrics = metrics };
        await _profileService.UpdateBuildAsync(profileId, updatedBuild);
        
        _logger.LogInformation(
            "Updated build '{Build}' with {Sessions} sessions of performance data",
            updatedBuild.Name, metrics.SessionCount);
    }
}

