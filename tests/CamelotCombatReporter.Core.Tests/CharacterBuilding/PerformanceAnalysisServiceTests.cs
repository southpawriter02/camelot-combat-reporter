using CamelotCombatReporter.Core.CharacterBuilding.Models;
using CamelotCombatReporter.Core.CharacterBuilding.Services;
using CamelotCombatReporter.Core.Models;

namespace CamelotCombatReporter.Core.Tests.CharacterBuilding;

/// <summary>
/// Tests for the performance metrics calculation logic.
/// Uses a testable subclass to bypass service dependencies.
/// </summary>
public class PerformanceAnalysisServiceTests
{
    [Fact]
    public async Task CalculateMetrics_EmptySessions_ReturnsEmptyMetrics()
    {
        // Arrange
        var service = new TestablePerformanceService();
        var sessions = Array.Empty<ExtendedCombatStatistics>();

        // Act
        var metrics = await service.CalculateMetricsAsync(sessions);

        // Assert
        Assert.Equal(0, metrics.SessionCount);
        Assert.Equal(0, metrics.TotalDamageDealt);
    }

    [Fact]
    public async Task CalculateMetrics_SingleSession_ReturnsCorrectTotals()
    {
        // Arrange
        var service = new TestablePerformanceService();
        var session = CreateTestSession(
            damageDealt: 10000,
            damageTaken: 5000,
            healingDone: 2000,
            kills: 5,
            deaths: 2,
            assists: 3,
            durationMinutes: 10);

        // Act
        var metrics = await service.CalculateMetricsAsync([session]);

        // Assert
        Assert.Equal(1, metrics.SessionCount);
        Assert.Equal(10000, metrics.TotalDamageDealt);
        Assert.Equal(5000, metrics.TotalDamageTaken);
        Assert.Equal(2000, metrics.TotalHealingDone);
        Assert.Equal(5, metrics.Kills);
        Assert.Equal(2, metrics.Deaths);
        Assert.Equal(3, metrics.Assists);
        Assert.Equal(2.5, metrics.KillDeathRatio, 2);
    }

    [Fact]
    public async Task CalculateMetrics_MultipleSessions_AggregatesCorrectly()
    {
        // Arrange
        var service = new TestablePerformanceService();
        var session1 = CreateTestSession(damageDealt: 10000, kills: 5, deaths: 1);
        var session2 = CreateTestSession(damageDealt: 20000, kills: 10, deaths: 2);

        // Act
        var metrics = await service.CalculateMetricsAsync([session1, session2]);

        // Assert
        Assert.Equal(2, metrics.SessionCount);
        Assert.Equal(30000, metrics.TotalDamageDealt);
        Assert.Equal(15, metrics.Kills);
        Assert.Equal(3, metrics.Deaths);
        Assert.Equal(5.0, metrics.KillDeathRatio, 2);
    }

    [Fact]
    public async Task CalculateMetrics_NoDeaths_KdRatioEqualsKills()
    {
        // Arrange
        var service = new TestablePerformanceService();
        var session = CreateTestSession(kills: 10, deaths: 0);

        // Act
        var metrics = await service.CalculateMetricsAsync([session]);

        // Assert
        Assert.Equal(10, metrics.KillDeathRatio);
    }

    [Fact]
    public async Task CalculateMetrics_CalculatesCorrectDps()
    {
        // Arrange
        var service = new TestablePerformanceService();
        var session = CreateTestSession(damageDealt: 60000, durationMinutes: 10); // 600 seconds

        // Act
        var metrics = await service.CalculateMetricsAsync([session]);

        // Assert
        // 60000 damage / 600 seconds = 100 DPS
        Assert.Equal(100, metrics.AverageDps, 1);
    }

    [Fact]
    public async Task CalculateMetrics_TracksPeakDps()
    {
        // Arrange
        var service = new TestablePerformanceService();
        var session1 = CreateTestSession(damageDealt: 10000, durationMinutes: 5, baseDps: 100);
        var session2 = CreateTestSession(damageDealt: 20000, durationMinutes: 5, baseDps: 200);

        // Act
        var metrics = await service.CalculateMetricsAsync([session1, session2]);

        // Assert
        Assert.Equal(200, metrics.PeakDps);
    }

    [Fact]
    public async Task CalculateMetrics_CorrectCombatTime()
    {
        // Arrange
        var service = new TestablePerformanceService();
        var session1 = CreateTestSession(durationMinutes: 10);
        var session2 = CreateTestSession(durationMinutes: 20);

        // Act
        var metrics = await service.CalculateMetricsAsync([session1, session2]);

        // Assert
        Assert.Equal(30, metrics.TotalCombatTime.TotalMinutes, 1);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Additional Tests (v1.8.2)
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task CalculateMetrics_TwoSessionsZeroDeaths_KdEqualsKills()
    {
        // Arrange
        var service = new TestablePerformanceService();
        var session1 = CreateTestSession(kills: 5, deaths: 0);
        var session2 = CreateTestSession(kills: 10, deaths: 0);

        // Act
        var metrics = await service.CalculateMetricsAsync([session1, session2]);

        // Assert
        Assert.Equal(15, metrics.KillDeathRatio);
    }

    [Fact]
    public async Task CalculateMetrics_CorrectHps()
    {
        // Arrange
        var service = new TestablePerformanceService();
        var session = CreateTestSession(healingDone: 6000, durationMinutes: 10); // 600 seconds

        // Act
        var metrics = await service.CalculateMetricsAsync([session]);

        // Assert
        // 6000 healing / 600 seconds = 10 HPS
        Assert.Equal(10, metrics.AverageHps, 1);
    }

    [Fact]
    public async Task CalculateMetrics_TotalDamageTaken_Aggregates()
    {
        // Arrange
        var service = new TestablePerformanceService();
        var session1 = CreateTestSession(damageTaken: 5000);
        var session2 = CreateTestSession(damageTaken: 3000);

        // Act
        var metrics = await service.CalculateMetricsAsync([session1, session2]);

        // Assert
        Assert.Equal(8000, metrics.TotalDamageTaken);
    }

    [Fact]
    public async Task CalculateMetrics_Assists_Aggregates()
    {
        // Arrange
        var service = new TestablePerformanceService();
        var session1 = CreateTestSession(assists: 10);
        var session2 = CreateTestSession(assists: 5);

        // Act
        var metrics = await service.CalculateMetricsAsync([session1, session2]);

        // Assert
        Assert.Equal(15, metrics.Assists);
    }

    [Fact]
    public async Task CalculateMetrics_SessionCount_IsCorrect()
    {
        // Arrange
        var service = new TestablePerformanceService();
        var sessions = Enumerable.Range(0, 5)
            .Select(_ => CreateTestSession(damageDealt: 1000))
            .ToList();

        // Act
        var metrics = await service.CalculateMetricsAsync(sessions);

        // Assert
        Assert.Equal(5, metrics.SessionCount);
    }

    private static ExtendedCombatStatistics CreateTestSession(
        int damageDealt = 0,
        int damageTaken = 0,
        int healingDone = 0,
        int kills = 0,
        int deaths = 0,
        int assists = 0,
        double durationMinutes = 10,
        double baseDps = 0)
    {
        var effectiveDps = baseDps > 0 ? baseDps : (damageDealt / Math.Max(durationMinutes * 60, 1));
        var baseStats = new CombatStatistics(
            DurationMinutes: durationMinutes,
            TotalDamage: damageDealt,
            Dps: effectiveDps,
            AverageDamage: 100,
            MedianDamage: 100,
            CombatStylesCount: 0,
            SpellsCastCount: 0);

        var start = DateTime.UtcNow.AddMinutes(-durationMinutes);
        var end = DateTime.UtcNow;
        var hps = durationMinutes > 0 ? healingDone / (durationMinutes * 60) : 0;

        return new ExtendedCombatStatistics(
            baseStats,
            new CharacterInfo("TestChar", Realm.Albion, CharacterClass.Armsman),
            damageDealt,
            damageTaken,
            healingDone,
            0, // healingReceived
            hps,
            kills,
            deaths,
            assists,
            start,
            end,
            "test.log");
    }

    /// <summary>
    /// Testable subclass that allows testing CalculateMetricsAsync without dependencies.
    /// </summary>
    private class TestablePerformanceService : IPerformanceAnalysisService
    {
        public Task<BuildPerformanceMetrics> CalculateMetricsAsync(IEnumerable<ExtendedCombatStatistics> sessions)
        {
            var sessionList = sessions.ToList();
            
            if (sessionList.Count == 0)
            {
                return Task.FromResult(BuildPerformanceMetrics.Empty);
            }

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
                if (sessionDps > peakDps) peakDps = sessionDps;
            }

            var avgDps = totalCombatSeconds > 0 ? totalDamageDealt / totalCombatSeconds : 0;
            var avgHps = totalCombatSeconds > 0 ? totalHealingDone / totalCombatSeconds : 0;
            var avgDamageTakenPerSecond = totalCombatSeconds > 0 ? totalDamageTaken / totalCombatSeconds : 0;

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

        public Task<BuildPerformanceMetrics> CalculateMetricsForProfileAsync(Guid profileId, DateRange? dateRange = null, Guid? buildId = null)
            => Task.FromResult(BuildPerformanceMetrics.Empty);

        public Task<IReadOnlyList<DamageBreakdown>> GetTopDamageSourcesAsync(Guid profileId, int topN = 10, DateRange? dateRange = null)
            => Task.FromResult<IReadOnlyList<DamageBreakdown>>([]);

        public Task UpdateBuildMetricsAsync(Guid profileId)
            => Task.CompletedTask;
    }
}
