using CamelotCombatReporter.Core.Comparison;
using CamelotCombatReporter.Core.Comparison.Models;

namespace CamelotCombatReporter.Core.Tests;

public class ComparisonServiceTests
{
    private readonly SessionComparisonService _service = new(null!);

    [Fact]
    public void Compare_TwoSessions_ReturnsComparison()
    {
        // Arrange
        var baseSession = CreateTestSession(dps: 100, kills: 5, deaths: 2);
        var compareSession = CreateTestSession(dps: 120, kills: 7, deaths: 1);

        // Act
        var result = _service.Compare(baseSession, compareSession);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(baseSession.SessionId, result.BaseSession.SessionId);
        Assert.Equal(compareSession.SessionId, result.CompareSession.SessionId);
        Assert.NotEmpty(result.Deltas);
    }

    [Fact]
    public void Compare_ImprovedDps_ShowsImprovement()
    {
        // Arrange
        var baseSession = CreateTestSession(dps: 100);
        var compareSession = CreateTestSession(dps: 120);

        // Act
        var result = _service.Compare(baseSession, compareSession);
        var dpsDelta = result.Deltas.FirstOrDefault(d => d.MetricName == "DPS");

        // Assert
        Assert.NotNull(dpsDelta);
        Assert.Equal(ChangeDirection.Improved, dpsDelta.Direction);
        Assert.Equal(20, dpsDelta.AbsoluteChange);
    }

    [Fact]
    public void Compare_DeclinedDps_ShowsDecline()
    {
        // Arrange
        var baseSession = CreateTestSession(dps: 120);
        var compareSession = CreateTestSession(dps: 100);

        // Act
        var result = _service.Compare(baseSession, compareSession);
        var dpsDelta = result.Deltas.FirstOrDefault(d => d.MetricName == "DPS");

        // Assert
        Assert.NotNull(dpsDelta);
        Assert.Equal(ChangeDirection.Declined, dpsDelta.Direction);
        Assert.Equal(-20, dpsDelta.AbsoluteChange);
    }

    [Fact]
    public void Compare_SameDps_ShowsUnchanged()
    {
        // Arrange
        var baseSession = CreateTestSession(dps: 100);
        var compareSession = CreateTestSession(dps: 100);

        // Act
        var result = _service.Compare(baseSession, compareSession);
        var dpsDelta = result.Deltas.FirstOrDefault(d => d.MetricName == "DPS");

        // Assert
        Assert.NotNull(dpsDelta);
        Assert.Equal(ChangeDirection.Unchanged, dpsDelta.Direction);
        Assert.Equal(0, dpsDelta.AbsoluteChange);
    }

    [Fact]
    public void CalculateDeltas_ReturnsMultipleMetrics()
    {
        // Arrange
        var baseSession = CreateTestSession(dps: 100, kills: 5, deaths: 2);
        var compareSession = CreateTestSession(dps: 120, kills: 7, deaths: 1);

        // Act
        var deltas = _service.CalculateDeltas(baseSession, compareSession);

        // Assert
        Assert.True(deltas.Count > 3);
        Assert.Contains(deltas, d => d.MetricName == "DPS");
        Assert.Contains(deltas, d => d.Category == "Combat");
        Assert.Contains(deltas, d => d.Category == "Healing");
    }

    [Fact]
    public void GenerateComparisonSummary_ReturnsNonEmptyString()
    {
        // Arrange
        var baseSession = CreateTestSession(dps: 100);
        var compareSession = CreateTestSession(dps: 120);
        var comparison = _service.Compare(baseSession, compareSession);

        // Act
        var summary = _service.GenerateComparisonSummary(comparison);

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(summary));
    }

    private static SessionSummary CreateTestSession(
        double dps = 100,
        double hps = 50,
        int kills = 5,
        int deaths = 2) => new(
            SessionId: Guid.NewGuid(),
            SessionDate: DateTime.UtcNow,
            Duration: TimeSpan.FromMinutes(30),
            TotalDamageDealt: (int)(dps * 1800),
            TotalDamageReceived: 50000,
            TotalHealingDone: (int)(hps * 1800),
            TotalHealingReceived: 30000,
            Kills: kills,
            Deaths: deaths,
            Assists: 3,
            DamagePerSecond: dps,
            HealingPerSecond: hps,
            KillDeathRatio: deaths > 0 ? (double)kills / deaths : kills,
            CustomMetrics: new Dictionary<string, double>());
}

public class TrendAnalysisServiceTests
{
    private readonly TrendAnalysisService _service = new();

    [Fact]
    public void AnalyzeTrend_ValidData_ReturnsTrendAnalysis()
    {
        // Arrange
        var dataPoints = new List<TrendDataPoint>
        {
            new(DateTime.UtcNow.AddDays(-4), 100, "Day1"),
            new(DateTime.UtcNow.AddDays(-3), 110, "Day2"),
            new(DateTime.UtcNow.AddDays(-2), 105, "Day3"),
            new(DateTime.UtcNow.AddDays(-1), 120, "Day4"),
            new(DateTime.UtcNow, 115, "Day5")
        };

        // Act
        var result = _service.AnalyzeTrend("DPS", dataPoints);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("DPS", result.MetricName);
        Assert.Equal(5, result.DataPoints.Count);
        Assert.NotNull(result.Statistics);
        Assert.False(string.IsNullOrWhiteSpace(result.Interpretation));
    }

    [Fact]
    public void CalculateLinearRegression_UptrendData_ReturnsPositiveSlope()
    {
        // Arrange
        var dataPoints = new List<TrendDataPoint>
        {
            new(DateTime.UtcNow.AddDays(-2), 100, "Day1"),
            new(DateTime.UtcNow.AddDays(-1), 110, "Day2"),
            new(DateTime.UtcNow, 120, "Day3")
        };

        // Act
        var (slope, _, _) = _service.CalculateLinearRegression(dataPoints);

        // Assert
        Assert.True(slope > 0);
    }

    [Fact]
    public void CalculateLinearRegression_DowntrendData_ReturnsNegativeSlope()
    {
        // Arrange
        var dataPoints = new List<TrendDataPoint>
        {
            new(DateTime.UtcNow.AddDays(-2), 120, "Day1"),
            new(DateTime.UtcNow.AddDays(-1), 110, "Day2"),
            new(DateTime.UtcNow, 100, "Day3")
        };

        // Act
        var (slope, _, _) = _service.CalculateLinearRegression(dataPoints);

        // Assert
        Assert.True(slope < 0);
    }

    [Fact]
    public void CalculateRollingAverage_ValidData_ReturnsSmoothedValues()
    {
        // Arrange
        var values = new double[] { 100, 110, 90, 120, 100 };

        // Act
        var result = _service.CalculateRollingAverage(values, 3).ToList();

        // Assert
        Assert.Equal(5, result.Count);
        Assert.Equal(100, result[0]); // First value same as input
        Assert.Equal(105, result[1]); // Average of first 2
    }

    [Fact]
    public void CalculateStatistics_ValidData_ReturnsCorrectStats()
    {
        // Arrange
        var values = new double[] { 100, 110, 120, 130, 140 };

        // Act
        var result = _service.CalculateStatistics(values);

        // Assert
        Assert.Equal(120, result.Mean);
        Assert.Equal(120, result.Median);
        Assert.Equal(100, result.Min);
        Assert.Equal(140, result.Max);
        Assert.True(result.StandardDeviation > 0);
    }

    [Fact]
    public void PredictNextValue_ValidStatistics_ReturnsPrediction()
    {
        // Arrange
        var dataPoints = new List<TrendDataPoint>
        {
            new(DateTime.UtcNow.AddDays(-2), 100, "Day1"),
            new(DateTime.UtcNow.AddDays(-1), 110, "Day2"),
            new(DateTime.UtcNow, 120, "Day3")
        };

        // Get stats from analyzing the trend
        var analysis = _service.AnalyzeTrend("Test", dataPoints);

        // Act
        var result = _service.PredictNextValue(analysis.Statistics);

        // Assert
        Assert.NotNull(result);
        Assert.True(result > 120); // Should predict continuation of uptrend
    }
}

public class GoalTrackerTests
{
    [Fact]
    public void CreateGoal_ValidInput_ReturnsGoal()
    {
        // Arrange
        var tracker = new GoalTracker(Path.GetTempFileName());

        // Act
        var goal = tracker.CreateGoal("Test Goal", GoalType.DamagePerSecond, 200);

        // Assert
        Assert.NotNull(goal);
        Assert.Equal("Test Goal", goal.Name);
        Assert.Equal(GoalType.DamagePerSecond, goal.Type);
        Assert.Equal(200, goal.TargetValue);
        Assert.Equal(GoalStatus.NotStarted, goal.Status);
    }

    [Fact]
    public void UpdateProgress_ValidGoal_UpdatesValue()
    {
        // Arrange
        var tracker = new GoalTracker(Path.GetTempFileName());
        var goal = tracker.CreateGoal("Test Goal", GoalType.DamagePerSecond, 200);

        // Act
        tracker.UpdateProgress(goal.Id, 150);

        // Assert
        var updatedGoal = tracker.ActiveGoals.First(g => g.Id == goal.Id);
        Assert.Equal(150, updatedGoal.CurrentValue);
        Assert.Equal(GoalStatus.InProgress, updatedGoal.Status);
    }

    [Fact]
    public void UpdateProgress_ReachesTarget_MarksAchieved()
    {
        // Arrange
        var tracker = new GoalTracker(Path.GetTempFileName());
        var goal = tracker.CreateGoal("Test Goal", GoalType.DamagePerSecond, 200);

        // Act
        tracker.UpdateProgress(goal.Id, 200);

        // Assert
        var completedGoals = tracker.GetGoalHistory();
        Assert.Contains(completedGoals, g => g.Id == goal.Id && g.Status == GoalStatus.Achieved);
    }

    [Fact]
    public void DeleteGoal_ExistingGoal_RemovesFromList()
    {
        // Arrange
        var tracker = new GoalTracker(Path.GetTempFileName());
        var goal = tracker.CreateGoal("Test Goal", GoalType.DamagePerSecond, 200);

        // Act
        tracker.DeleteGoal(goal.Id);

        // Assert
        Assert.Empty(tracker.ActiveGoals);
    }
}

public class PersonalBestTrackerTests
{
    [Fact]
    public void CheckAndUpdateBest_NewBest_ReturnsBestRecord()
    {
        // Arrange
        var tracker = new PersonalBestTracker(Path.GetTempFileName());
        var sessionId = Guid.NewGuid();

        // Act
        var result = tracker.CheckAndUpdateBest("DPS", 150, sessionId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("DPS", result.MetricName);
        Assert.Equal(150, result.Value);
        Assert.Null(result.PreviousBest);
    }

    [Fact]
    public void CheckAndUpdateBest_NotANewBest_ReturnsNull()
    {
        // Arrange
        var tracker = new PersonalBestTracker(Path.GetTempFileName());
        var sessionId = Guid.NewGuid();
        tracker.CheckAndUpdateBest("DPS", 150, sessionId);

        // Act
        var result = tracker.CheckAndUpdateBest("DPS", 140, sessionId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void CheckAndUpdateBest_BetterValue_UpdatesBest()
    {
        // Arrange
        var tracker = new PersonalBestTracker(Path.GetTempFileName());
        var sessionId = Guid.NewGuid();
        tracker.CheckAndUpdateBest("DPS", 100, sessionId);

        // Act
        var result = tracker.CheckAndUpdateBest("DPS", 120, sessionId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(120, result.Value);
        Assert.Equal(100, result.PreviousBest);
        Assert.Equal(20, result.ImprovementPercent);
    }

    [Fact]
    public void CurrentBests_ReturnsCorrectBests()
    {
        // Arrange
        var tracker = new PersonalBestTracker(Path.GetTempFileName());
        var sessionId = Guid.NewGuid();
        tracker.CheckAndUpdateBest("DPS", 150, sessionId);
        tracker.CheckAndUpdateBest("Kills", 10, sessionId);

        // Act
        var bests = tracker.CurrentBests;

        // Assert
        Assert.Equal(2, bests.Count);
        Assert.Equal(150, bests["DPS"].Value);
        Assert.Equal(10, bests["Kills"].Value);
    }

    [Fact]
    public void GetRecentBests_ReturnsOrderedByDate()
    {
        // Arrange
        var tracker = new PersonalBestTracker(Path.GetTempFileName());
        var sessionId = Guid.NewGuid();
        tracker.CheckAndUpdateBest("DPS", 100, sessionId);
        tracker.CheckAndUpdateBest("DPS", 120, sessionId);
        tracker.CheckAndUpdateBest("DPS", 150, sessionId);

        // Act
        var recentBests = tracker.GetRecentBests(3);

        // Assert
        Assert.Equal(3, recentBests.Count);
        Assert.Equal(150, recentBests[0].Value); // Most recent first
    }
}
