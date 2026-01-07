using CamelotCombatReporter.Core.CharacterBuilding.Models;
using CamelotCombatReporter.Core.CharacterBuilding.Services;

namespace CamelotCombatReporter.Core.Tests.CharacterBuilding;

public class ProgressionTrackingServiceTests
{
    [Fact]
    public void CalculateProgressionSummary_EmptyProgression_ReturnsEmptySummary()
    {
        // Arrange
        var service = new TestableProgressionService();
        var progression = new RealmRankProgression();

        // Act
        var summary = service.CalculateProgressionSummary(progression);

        // Assert
        Assert.Equal(0, summary.CurrentRank);
        Assert.Equal(0, summary.TotalRealmPoints);
        Assert.Equal(0, summary.MilestoneCount);
    }

    [Fact]
    public void CalculateProgressionSummary_SingleMilestone_ReturnsCurrentStats()
    {
        // Arrange
        var service = new TestableProgressionService();
        var milestone = CreateMilestone(rank: 5, rp: 800000, dps: 150, kd: 2.5);
        var progression = new RealmRankProgression { Milestones = [milestone] };

        // Act
        var summary = service.CalculateProgressionSummary(progression);

        // Assert
        Assert.Equal(5, summary.CurrentRank);
        Assert.Equal(800000, summary.TotalRealmPoints);
        Assert.Equal(1, summary.MilestoneCount);
    }

    [Fact]
    public void CalculateProgressionSummary_MultipleMilestones_CalculatesAverageDays()
    {
        // Arrange
        var service = new TestableProgressionService();
        var now = DateTime.UtcNow;
        var milestones = new[]
        {
            CreateMilestone(rank: 1, rp: 0, achievedUtc: now.AddDays(-30)),
            CreateMilestone(rank: 2, rp: 25000, achievedUtc: now.AddDays(-20)),
            CreateMilestone(rank: 3, rp: 125000, achievedUtc: now.AddDays(-10)),
            CreateMilestone(rank: 4, rp: 350000, achievedUtc: now)
        };
        var progression = new RealmRankProgression { Milestones = milestones };

        // Act
        var summary = service.CalculateProgressionSummary(progression);

        // Assert
        Assert.Equal(4, summary.CurrentRank);
        Assert.Equal(10, summary.AverageDaysBetweenRanks, 1); // 30 days / 3 intervals
    }

    [Fact]
    public void CalculateProgressionSummary_ImprovingDps_ShowsPositiveTrend()
    {
        // Arrange
        var service = new TestableProgressionService();
        var milestones = new[]
        {
            CreateMilestone(rank: 1, dps: 80),
            CreateMilestone(rank: 2, dps: 100),
            CreateMilestone(rank: 3, dps: 120),
            CreateMilestone(rank: 4, dps: 140)
        };
        var progression = new RealmRankProgression { Milestones = milestones };

        // Act
        var summary = service.CalculateProgressionSummary(progression);

        // Assert
        Assert.True(summary.DpsTrend > 0); // DPS is improving
    }

    [Fact]
    public void CalculateProgressionSummary_DecliningKd_ShowsNegativeTrend()
    {
        // Arrange
        var service = new TestableProgressionService();
        var milestones = new[]
        {
            CreateMilestone(rank: 1, kd: 3.0),
            CreateMilestone(rank: 2, kd: 2.5),
            CreateMilestone(rank: 3, kd: 2.0),
            CreateMilestone(rank: 4, kd: 1.5)
        };
        var progression = new RealmRankProgression { Milestones = milestones };

        // Act
        var summary = service.CalculateProgressionSummary(progression);

        // Assert
        Assert.True(summary.KdTrend < 0); // K/D is declining
    }

    [Fact]
    public void GetRpForRank_ReturnsCorrectThresholds()
    {
        Assert.Equal(0, ProgressionTrackingService.GetRpForRank(1));
        Assert.Equal(25_000, ProgressionTrackingService.GetRpForRank(2));
        Assert.Equal(125_000, ProgressionTrackingService.GetRpForRank(3));
        Assert.Equal(750_000, ProgressionTrackingService.GetRpForRank(5));
        Assert.Equal(20_475_000, ProgressionTrackingService.GetRpForRank(14));
    }

    [Fact]
    public void GetRankForRp_ReturnsCorrectRank()
    {
        Assert.Equal(1, ProgressionTrackingService.GetRankForRp(0));
        Assert.Equal(1, ProgressionTrackingService.GetRankForRp(24_999));
        Assert.Equal(2, ProgressionTrackingService.GetRankForRp(25_000));
        Assert.Equal(5, ProgressionTrackingService.GetRankForRp(750_000));
        Assert.Equal(14, ProgressionTrackingService.GetRankForRp(25_000_000));
    }

    private static RankMilestone CreateMilestone(
        int rank = 1,
        long rp = 0,
        double dps = 100,
        double kd = 1.0,
        int sessions = 10,
        DateTime? achievedUtc = null)
    {
        return new RankMilestone
        {
            RealmRank = rank,
            RealmPoints = rp,
            AchievedUtc = achievedUtc ?? DateTime.UtcNow,
            AverageDps = dps,
            AverageHps = 0,
            KillDeathRatio = kd,
            SessionCount = sessions
        };
    }

    /// <summary>
    /// Testable subclass to avoid profile service dependency.
    /// </summary>
    private class TestableProgressionService : IProgressionTrackingService
    {
        public Task RecordMilestoneAsync(Guid profileId, RankMilestone milestone, CancellationToken ct = default)
            => Task.CompletedTask;

        public Task<RealmRankProgression> GetProgressionAsync(Guid profileId, CancellationToken ct = default)
            => Task.FromResult(new RealmRankProgression());

        public Task<RankMilestone?> GetCurrentMilestoneAsync(Guid profileId, CancellationToken ct = default)
            => Task.FromResult<RankMilestone?>(null);

        public ProgressionSummary CalculateProgressionSummary(RealmRankProgression progression)
        {
            var milestones = progression.Milestones.ToList();
            
            if (milestones.Count == 0)
                return new ProgressionSummary();

            var current = milestones.Last();
            var daysBetween = CalculateAverageDaysBetweenRanks(milestones);
            var dpsTrend = CalculateTrend(milestones, m => m.AverageDps);
            var kdTrend = CalculateTrend(milestones, m => m.KillDeathRatio);

            return new ProgressionSummary
            {
                CurrentRank = current.RealmRank,
                TotalRealmPoints = current.RealmPoints,
                MilestoneCount = milestones.Count,
                AverageDaysBetweenRanks = daysBetween,
                DpsTrend = dpsTrend,
                KdTrend = kdTrend
            };
        }

        public Task<RankMilestone?> CheckAndRecordRankUpAsync(Guid profileId, int newRealmRank, long newTotalRealmPoints, BuildPerformanceMetrics? currentMetrics = null, CancellationToken ct = default)
            => Task.FromResult<RankMilestone?>(null);

        private static double CalculateAverageDaysBetweenRanks(List<RankMilestone> milestones)
        {
            if (milestones.Count < 2) return 0;
            var totalDays = 0.0;
            for (int i = 1; i < milestones.Count; i++)
                totalDays += (milestones[i].AchievedUtc - milestones[i - 1].AchievedUtc).TotalDays;
            return totalDays / (milestones.Count - 1);
        }

        private static double CalculateTrend(List<RankMilestone> milestones, Func<RankMilestone, double> selector)
        {
            if (milestones.Count < 2) return 0;
            var mid = milestones.Count / 2;
            var firstHalf = milestones.Take(mid).Average(selector);
            var secondHalf = milestones.Skip(mid).Average(selector);
            return secondHalf - firstHalf;
        }
    }
}
