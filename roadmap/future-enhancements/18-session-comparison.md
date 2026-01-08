# 18. Session Comparison and Trend Analysis

## Status: ✅ Complete (v1.3.0)

**Implementation Complete:**
- ✅ Log parsing infrastructure
- ✅ Cross-realm session storage
- ✅ Historical data aggregation

---

## Description

Compare combat sessions across time to identify trends, measure improvement, and understand performance variations. Visualize progress over days, weeks, and months with detailed breakdowns and actionable insights.

## Functionality

### Core Features

* **Session Comparison:**
  * Side-by-side session analysis
  * Difference highlighting
  * Percentage change calculations
  * Statistical significance testing

* **Trend Analysis:**
  * Performance over time graphs
  * Rolling averages
  * Peak performance identification
  * Regression detection

* **Benchmarking:**
  * Personal best tracking
  * Class averages comparison
  * Goal setting and progress
  * Percentile rankings

### Comparison Views

| View | Purpose | Metrics |
|------|---------|---------|
| **Session vs. Session** | Compare two specific sessions | All stats side-by-side |
| **Session vs. Average** | Compare to personal average | Deviation from norm |
| **Today vs. Yesterday** | Quick daily comparison | Key metrics delta |
| **Week over Week** | Weekly trend analysis | Aggregated changes |
| **Class Benchmark** | Compare to class data | Percentile ranking |

### Time Periods

* **Single Session:** One combat log analysis
* **Daily:** All sessions from one day
* **Weekly:** 7-day rolling window
* **Monthly:** 30-day aggregation
* **All-Time:** Complete history
* **Custom Range:** User-defined period

### Comparison Metrics

* **Core Combat:**
  * DPS (average, peak, consistency)
  * HPS (if applicable)
  * Damage dealt/taken ratio
  * Kill/death ratio

* **Efficiency:**
  * Uptime percentage
  * Ability usage rate
  * Resource efficiency
  * Combat style usage

* **Survival:**
  * Average time to death
  * Deaths per hour
  * Damage taken per minute
  * Defensive cooldown usage

### Trend Visualizations

* **Line Charts:**
  * DPS over time
  * K/D ratio progression
  * Session duration trends
  * Performance consistency

* **Bar Charts:**
  * Daily/weekly comparisons
  * Metric breakdowns
  * Goal progress
  * Class comparisons

* **Heat Maps:**
  * Performance by time of day
  * Day of week patterns
  * Peak performance windows
  * Consistency visualization

### Progress Tracking

* **Goals System:**
  * Set target metrics
  * Track progress percentage
  * Milestone notifications
  * Goal history

* **Personal Bests:**
  * Automatic PB detection
  * Category-based records
  * Session context (date, character)
  * PB streaks and trends

* **Improvement Insights:**
  * Identify improving metrics
  * Highlight declining areas
  * Suggest focus areas
  * Celebrate achievements

### Reporting

* **Summary Reports:**
  * Weekly progress summary
  * Monthly recap
  * Yearly review
  * Custom period reports

* **Export Options:**
  * PDF report generation
  * CSV data export
  * Shareable web links
  * Image exports for social

## Requirements

* **Data Storage:** Persistent session history
* **Statistics Engine:** Trend calculations
* **Charting Library:** Visualization components
* **UI:** Comparison and trend views

## Limitations

* Requires sufficient session history
* Comparisons affected by log quality
* External factors not captured (group, enemy)
* Statistical significance needs sample size

## Dependencies

* **01-log-parsing.md:** Core event parsing
* **03-cross-realm-analysis.md:** Session storage
* **04-timeline-view.md:** Chart components

## Implementation Phases

### Phase 1: Session Comparison
- [ ] Create comparison data model
- [ ] Implement side-by-side view
- [ ] Calculate metric differences
- [ ] Add change highlighting

### Phase 2: Trend Analysis
- [ ] Build historical data aggregation
- [ ] Implement rolling averages
- [ ] Create trend line calculations
- [ ] Add regression detection

### Phase 3: Visualization
- [ ] Design trend chart components
- [ ] Implement heat maps
- [ ] Create comparison tables
- [ ] Add interactive features

### Phase 4: Goals and Insights
- [ ] Build goal tracking system
- [ ] Implement PB detection
- [ ] Create insight generation
- [ ] Add notification integration

### Phase 5: Reporting
- [ ] Design report templates
- [ ] Implement PDF generation
- [ ] Add export functionality
- [ ] Create sharing features

## Technical Notes

### Data Structures

```csharp
public record SessionSummary(
    Guid Id,
    DateTime StartTime,
    DateTime EndTime,
    CharacterInfo Character,
    CombatStatistics Statistics,
    ExtendedMetrics Extended
);

public record ExtendedMetrics(
    int TotalKills,
    int TotalDeaths,
    int TotalHealing,
    int TotalDamageTaken,
    TimeSpan CombatUptime,
    Dictionary<string, int> AbilityUsage
);

public record SessionComparison(
    SessionSummary Session1,
    SessionSummary Session2,
    MetricDelta DpsDelta,
    MetricDelta HpsDelta,
    MetricDelta KdDelta,
    IReadOnlyList<MetricChange> AllChanges
);

public record MetricDelta(
    double Value1,
    double Value2,
    double AbsoluteDelta,
    double PercentChange,
    ChangeDirection Direction
);

public enum ChangeDirection
{
    Improved,
    Declined,
    Unchanged
}

public record TrendData(
    string MetricName,
    IReadOnlyList<DataPoint> DataPoints,
    TrendStatistics Statistics
);

public record DataPoint(
    DateTime Timestamp,
    double Value,
    string? Label
);

public record TrendStatistics(
    double Mean,
    double Median,
    double StandardDeviation,
    double Min,
    double Max,
    double Slope,        // Trend direction
    double RSquared      // Trend strength
);
```

### Session Comparison Service

```csharp
public class SessionComparisonService
{
    public SessionComparison Compare(SessionSummary s1, SessionSummary s2)
    {
        var changes = new List<MetricChange>();

        // DPS comparison
        var dpsDelta = CalculateDelta(s1.Statistics.Dps, s2.Statistics.Dps);
        changes.Add(new MetricChange("DPS", dpsDelta));

        // K/D comparison
        var kd1 = s1.Extended.TotalDeaths > 0
            ? (double)s1.Extended.TotalKills / s1.Extended.TotalDeaths
            : s1.Extended.TotalKills;
        var kd2 = s2.Extended.TotalDeaths > 0
            ? (double)s2.Extended.TotalKills / s2.Extended.TotalDeaths
            : s2.Extended.TotalKills;
        var kdDelta = CalculateDelta(kd1, kd2);
        changes.Add(new MetricChange("K/D Ratio", kdDelta));

        // HPS comparison (if healing data available)
        var hps1 = CalculateHps(s1);
        var hps2 = CalculateHps(s2);
        var hpsDelta = CalculateDelta(hps1, hps2);
        if (hps1 > 0 || hps2 > 0)
            changes.Add(new MetricChange("HPS", hpsDelta));

        // Additional metrics
        changes.Add(new MetricChange("Total Damage",
            CalculateDelta(s1.Statistics.TotalDamage, s2.Statistics.TotalDamage)));
        changes.Add(new MetricChange("Combat Duration",
            CalculateDelta(s1.Statistics.DurationMinutes, s2.Statistics.DurationMinutes)));

        return new SessionComparison(s1, s2, dpsDelta, hpsDelta, kdDelta, changes);
    }

    private MetricDelta CalculateDelta(double v1, double v2)
    {
        var absolute = v2 - v1;
        var percent = v1 != 0 ? (v2 - v1) / v1 * 100 : (v2 > 0 ? 100 : 0);

        var direction = absolute switch
        {
            > 0.01 => ChangeDirection.Improved,
            < -0.01 => ChangeDirection.Declined,
            _ => ChangeDirection.Unchanged
        };

        return new MetricDelta(v1, v2, absolute, percent, direction);
    }
}
```

### Trend Analysis Service

```csharp
public class TrendAnalysisService
{
    public TrendData AnalyzeTrend(
        string metricName,
        IEnumerable<SessionSummary> sessions,
        Func<SessionSummary, double> metricSelector)
    {
        var dataPoints = sessions
            .OrderBy(s => s.StartTime)
            .Select(s => new DataPoint(s.StartTime, metricSelector(s), null))
            .ToList();

        if (dataPoints.Count < 2)
        {
            return new TrendData(metricName, dataPoints, CalculateBasicStats(dataPoints));
        }

        var stats = CalculateTrendStatistics(dataPoints);
        return new TrendData(metricName, dataPoints, stats);
    }

    private TrendStatistics CalculateTrendStatistics(List<DataPoint> points)
    {
        var values = points.Select(p => p.Value).ToList();

        // Basic statistics
        var mean = values.Average();
        var median = CalculateMedian(values);
        var stdDev = CalculateStandardDeviation(values, mean);
        var min = values.Min();
        var max = values.Max();

        // Linear regression for trend
        var (slope, rSquared) = CalculateLinearRegression(points);

        return new TrendStatistics(mean, median, stdDev, min, max, slope, rSquared);
    }

    private (double Slope, double RSquared) CalculateLinearRegression(
        List<DataPoint> points)
    {
        var n = points.Count;
        var xValues = Enumerable.Range(0, n).Select(i => (double)i).ToList();
        var yValues = points.Select(p => p.Value).ToList();

        var xMean = xValues.Average();
        var yMean = yValues.Average();

        var numerator = 0.0;
        var denominator = 0.0;
        var ssTotal = 0.0;
        var ssResidual = 0.0;

        for (int i = 0; i < n; i++)
        {
            numerator += (xValues[i] - xMean) * (yValues[i] - yMean);
            denominator += Math.Pow(xValues[i] - xMean, 2);
        }

        var slope = denominator != 0 ? numerator / denominator : 0;
        var intercept = yMean - slope * xMean;

        for (int i = 0; i < n; i++)
        {
            var predicted = slope * xValues[i] + intercept;
            ssTotal += Math.Pow(yValues[i] - yMean, 2);
            ssResidual += Math.Pow(yValues[i] - predicted, 2);
        }

        var rSquared = ssTotal != 0 ? 1 - (ssResidual / ssTotal) : 0;

        return (slope, rSquared);
    }

    public IReadOnlyList<DataPoint> CalculateRollingAverage(
        IReadOnlyList<DataPoint> points,
        int windowSize)
    {
        var result = new List<DataPoint>();

        for (int i = 0; i < points.Count; i++)
        {
            var windowStart = Math.Max(0, i - windowSize + 1);
            var windowPoints = points.Skip(windowStart).Take(i - windowStart + 1);
            var average = windowPoints.Average(p => p.Value);

            result.Add(new DataPoint(points[i].Timestamp, average, $"{windowSize}-period avg"));
        }

        return result;
    }
}
```

### Goal Tracking

```csharp
public record PerformanceGoal(
    Guid Id,
    string Name,
    string MetricName,
    double TargetValue,
    GoalType Type,
    DateTime? Deadline,
    DateTime CreatedAt,
    double? AchievedValue,
    DateTime? AchievedAt
);

public enum GoalType
{
    Minimum,     // Achieve at least this value
    Maximum,     // Stay below this value
    Average      // Maintain this average
}

public class GoalTracker
{
    private readonly List<PerformanceGoal> _goals = new();

    public void AddGoal(PerformanceGoal goal) => _goals.Add(goal);

    public IReadOnlyList<GoalProgress> GetProgress(
        IEnumerable<SessionSummary> recentSessions)
    {
        return _goals
            .Where(g => g.AchievedAt == null)
            .Select(g => CalculateProgress(g, recentSessions))
            .ToList();
    }

    private GoalProgress CalculateProgress(
        PerformanceGoal goal,
        IEnumerable<SessionSummary> sessions)
    {
        var metricValues = sessions
            .Select(s => GetMetricValue(s, goal.MetricName))
            .Where(v => v.HasValue)
            .Select(v => v!.Value)
            .ToList();

        if (!metricValues.Any())
        {
            return new GoalProgress(goal, 0, 0, null, false);
        }

        var currentValue = goal.Type switch
        {
            GoalType.Minimum => metricValues.Max(),
            GoalType.Maximum => metricValues.Min(),
            GoalType.Average => metricValues.Average(),
            _ => metricValues.Last()
        };

        var progressPercent = goal.Type switch
        {
            GoalType.Minimum => Math.Min(100, currentValue / goal.TargetValue * 100),
            GoalType.Maximum => Math.Min(100, goal.TargetValue / currentValue * 100),
            _ => Math.Min(100, currentValue / goal.TargetValue * 100)
        };

        var isAchieved = goal.Type switch
        {
            GoalType.Minimum => currentValue >= goal.TargetValue,
            GoalType.Maximum => currentValue <= goal.TargetValue,
            _ => currentValue >= goal.TargetValue * 0.95
        };

        var trendData = new TrendAnalysisService()
            .AnalyzeTrend(goal.MetricName, sessions,
                s => GetMetricValue(s, goal.MetricName) ?? 0);

        return new GoalProgress(
            goal,
            progressPercent,
            currentValue,
            trendData.Statistics.Slope,
            isAchieved
        );
    }

    private double? GetMetricValue(SessionSummary session, string metricName)
    {
        return metricName switch
        {
            "DPS" => session.Statistics.Dps,
            "K/D" => session.Extended.TotalDeaths > 0
                ? (double)session.Extended.TotalKills / session.Extended.TotalDeaths
                : session.Extended.TotalKills,
            "Total Damage" => session.Statistics.TotalDamage,
            _ => null
        };
    }
}

public record GoalProgress(
    PerformanceGoal Goal,
    double ProgressPercent,
    double CurrentValue,
    double? TrendSlope,
    bool IsAchieved
);
```

### Personal Best Tracking

```csharp
public class PersonalBestTracker
{
    private readonly Dictionary<string, PersonalBest> _records = new();

    public PersonalBest? CheckAndUpdate(
        string category,
        double value,
        SessionSummary session)
    {
        if (!_records.TryGetValue(category, out var current) ||
            value > current.Value)
        {
            var newPb = new PersonalBest(
                category,
                value,
                session.StartTime,
                session.Character,
                current?.Value
            );

            _records[category] = newPb;
            return newPb;
        }

        return null;
    }

    public IReadOnlyDictionary<string, PersonalBest> GetAllRecords() => _records;
}

public record PersonalBest(
    string Category,
    double Value,
    DateTime AchievedAt,
    CharacterInfo Character,
    double? PreviousValue
)
{
    public double? Improvement => PreviousValue.HasValue
        ? Value - PreviousValue.Value
        : null;
}
```

### Comparison View Model

```csharp
public partial class SessionComparisonViewModel : ViewModelBase
{
    private readonly SessionComparisonService _comparisonService;
    private readonly TrendAnalysisService _trendService;

    [ObservableProperty] private SessionSummary? _session1;
    [ObservableProperty] private SessionSummary? _session2;
    [ObservableProperty] private SessionComparison? _comparison;
    [ObservableProperty] private TrendData? _dpsTrend;
    [ObservableProperty] private ObservableCollection<GoalProgress> _goals = new();

    public ObservableCollection<SessionSummary> AvailableSessions { get; } = new();

    partial void OnSession1Changed(SessionSummary? value)
    {
        if (value != null && Session2 != null)
            UpdateComparison();
    }

    partial void OnSession2Changed(SessionSummary? value)
    {
        if (value != null && Session1 != null)
            UpdateComparison();
    }

    private void UpdateComparison()
    {
        if (Session1 == null || Session2 == null) return;
        Comparison = _comparisonService.Compare(Session1, Session2);
    }

    [RelayCommand]
    private void LoadTrends(string period)
    {
        var sessions = period switch
        {
            "week" => GetSessionsFromLastDays(7),
            "month" => GetSessionsFromLastDays(30),
            "all" => AvailableSessions.ToList(),
            _ => GetSessionsFromLastDays(7)
        };

        DpsTrend = _trendService.AnalyzeTrend(
            "DPS",
            sessions,
            s => s.Statistics.Dps
        );
    }
}
```
