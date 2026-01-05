using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CamelotCombatReporter.Core.Comparison;
using CamelotCombatReporter.Core.Comparison.Models;
using CamelotCombatReporter.Gui.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.SkiaSharpView.Painting.Effects;
using SkiaSharp;

namespace CamelotCombatReporter.Gui.Comparison.ViewModels;

/// <summary>
/// ViewModel for the Session Comparison view.
/// </summary>
public partial class SessionComparisonViewModel : ViewModelBase
{
    private readonly ISessionComparisonService _comparisonService;
    private readonly ITrendAnalysisService _trendService;
    private readonly IGoalTracker _goalTracker;
    private readonly IPersonalBestTracker _pbTracker;

    #region Observable Properties

    [ObservableProperty]
    private ObservableCollection<SessionSummaryViewModel> _availableSessions = new();

    [ObservableProperty]
    private SessionSummaryViewModel? _baseSession;

    [ObservableProperty]
    private SessionSummaryViewModel? _compareSession;

    [ObservableProperty]
    private ObservableCollection<MetricDeltaViewModel> _deltas = new();

    [ObservableProperty]
    private string _comparisonSummary = "Select two sessions to compare";

    [ObservableProperty]
    private bool _hasComparison;

    [ObservableProperty]
    private ObservableCollection<GoalViewModel> _activeGoals = new();

    [ObservableProperty]
    private ObservableCollection<PersonalBestViewModel> _recentPbs = new();

    [ObservableProperty]
    private string _selectedTrendMetric = "DPS";

    public string[] TrendMetricOptions { get; } = new[]
    {
        "DPS", "HPS", "K/D Ratio", "Total Damage", "Total Healing"
    };

    [ObservableProperty]
    private ISeries[] _trendChartSeries = Array.Empty<ISeries>();

    [ObservableProperty]
    private Axis[] _trendChartXAxes = Array.Empty<Axis>();

    [ObservableProperty]
    private Axis[] _trendChartYAxes = Array.Empty<Axis>();

    [ObservableProperty]
    private string _trendInterpretation = "";

    [ObservableProperty]
    private string _statusMessage = "Ready";

    #endregion

    public SessionComparisonViewModel()
    {
        var storagePath = GetDefaultStoragePath();
        _comparisonService = new SessionComparisonService(null!);
        _trendService = new TrendAnalysisService();
        _goalTracker = new GoalTracker(System.IO.Path.Combine(storagePath, "goals.json"));
        _pbTracker = new PersonalBestTracker(System.IO.Path.Combine(storagePath, "personal-bests.json"));

        InitializeAsync();
    }

    public SessionComparisonViewModel(
        ISessionComparisonService comparisonService,
        ITrendAnalysisService trendService,
        IGoalTracker goalTracker,
        IPersonalBestTracker pbTracker)
    {
        _comparisonService = comparisonService;
        _trendService = trendService;
        _goalTracker = goalTracker;
        _pbTracker = pbTracker;

        InitializeAsync();
    }

    private async void InitializeAsync()
    {
        await LoadGoalsAsync();
        await LoadPersonalBestsAsync();
    }

    #region Commands

    /// <summary>
    /// Loads available sessions from storage.
    /// </summary>
    [RelayCommand]
    private void LoadSessions()
    {
        try
        {
            StatusMessage = "Loading sessions...";
            var sessions = _comparisonService.LoadSessionHistory(20);

            AvailableSessions.Clear();
            foreach (var session in sessions.OrderByDescending(s => s.SessionDate))
            {
                AvailableSessions.Add(new SessionSummaryViewModel(session));
            }

            StatusMessage = $"Loaded {sessions.Count} sessions";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading sessions: {ex.Message}";
        }
    }

    /// <summary>
    /// Compares the selected sessions.
    /// </summary>
    [RelayCommand]
    private void CompareSelectedSessions()
    {
        if (BaseSession == null || CompareSession == null)
        {
            ComparisonSummary = "Please select both a base and comparison session";
            return;
        }

        if (BaseSession.SessionId == CompareSession.SessionId)
        {
            ComparisonSummary = "Please select two different sessions";
            return;
        }

        try
        {
            StatusMessage = "Comparing sessions...";
            var comparison = _comparisonService.Compare(BaseSession.Summary, CompareSession.Summary);

            Deltas.Clear();
            foreach (var delta in comparison.Deltas.OrderByDescending(d => Math.Abs(d.PercentChange)))
            {
                Deltas.Add(new MetricDeltaViewModel(delta));
            }

            ComparisonSummary = comparison.ComparisonSummary;
            HasComparison = true;

            StatusMessage = "Comparison complete";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Comparison error: {ex.Message}";
        }
    }

    /// <summary>
    /// Updates the trend chart for the selected metric.
    /// </summary>
    [RelayCommand]
    private void UpdateTrendChart()
    {
        if (AvailableSessions.Count < 2)
        {
            TrendInterpretation = "Need at least 2 sessions for trend analysis";
            return;
        }

        try
        {
            var metricName = SelectedTrendMetric;
            var dataPoints = AvailableSessions
                .OrderBy(s => s.SessionDate)
                .Select(s => new TrendDataPoint(
                    s.SessionDate,
                    GetMetricValue(s.Summary, metricName),
                    s.SessionDate.ToString("MM/dd")))
                .ToList();

            var analysis = _trendService.AnalyzeTrend(metricName, dataPoints);

            // Build chart
            var values = dataPoints.Select(p => p.Value).ToArray();
            var rollingAvg = _trendService.CalculateRollingAverage(values, 3).ToArray();

            TrendChartSeries = new ISeries[]
            {
                new LineSeries<double>
                {
                    Values = values,
                    Name = metricName,
                    Fill = null,
                    Stroke = new SolidColorPaint(new SKColor(33, 150, 243)) { StrokeThickness = 2 },
                    GeometrySize = 8
                },
                new LineSeries<double>
                {
                    Values = rollingAvg,
                    Name = "3-Session Avg",
                    Fill = null,
                    Stroke = new SolidColorPaint(new SKColor(255, 152, 0)) { StrokeThickness = 2, PathEffect = new DashEffect(new float[] { 5, 5 }) },
                    GeometrySize = 0
                }
            };

            TrendChartXAxes = new Axis[]
            {
                new Axis
                {
                    Labels = dataPoints.Select(p => p.Label ?? "").ToArray(),
                    LabelsRotation = 45
                }
            };

            TrendChartYAxes = new Axis[]
            {
                new Axis { Name = metricName }
            };

            TrendInterpretation = analysis.Interpretation;
        }
        catch (Exception ex)
        {
            TrendInterpretation = $"Error: {ex.Message}";
        }
    }

    /// <summary>
    /// Creates a new performance goal.
    /// </summary>
    [RelayCommand]
    private async Task CreateGoal(string goalName)
    {
        if (string.IsNullOrWhiteSpace(goalName))
            return;

        try
        {
            var goal = _goalTracker.CreateGoal(
                goalName,
                GoalType.DamagePerSecond,
                100.0);

            ActiveGoals.Add(new GoalViewModel(goal));
            await _goalTracker.SaveAsync();
            StatusMessage = "Goal created";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error creating goal: {ex.Message}";
        }
    }

    /// <summary>
    /// Deletes a goal.
    /// </summary>
    [RelayCommand]
    private async Task DeleteGoal(GoalViewModel? goal)
    {
        if (goal == null)
            return;

        _goalTracker.DeleteGoal(goal.Id);
        ActiveGoals.Remove(goal);
        await _goalTracker.SaveAsync();
        StatusMessage = "Goal deleted";
    }

    /// <summary>
    /// Saves current data.
    /// </summary>
    [RelayCommand]
    private async Task SaveData()
    {
        try
        {
            await _goalTracker.SaveAsync();
            await _pbTracker.SaveAsync();
            StatusMessage = "Data saved";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Save error: {ex.Message}";
        }
    }

    #endregion

    #region Private Methods

    private async Task LoadGoalsAsync()
    {
        try
        {
            await _goalTracker.LoadAsync();
            ActiveGoals.Clear();
            foreach (var goal in _goalTracker.ActiveGoals)
            {
                ActiveGoals.Add(new GoalViewModel(goal));
            }
        }
        catch
        {
            // Start with empty goals
        }
    }

    private async Task LoadPersonalBestsAsync()
    {
        try
        {
            await _pbTracker.LoadAsync();
            RecentPbs.Clear();
            foreach (var pb in _pbTracker.GetRecentBests(10))
            {
                RecentPbs.Add(new PersonalBestViewModel(pb));
            }
        }
        catch
        {
            // Start with empty PBs
        }
    }

    private static double GetMetricValue(SessionSummary session, string metricName)
    {
        return metricName switch
        {
            "DPS" => session.DamagePerSecond,
            "HPS" => session.HealingPerSecond,
            "K/D Ratio" => session.KillDeathRatio,
            "Total Damage" => session.TotalDamageDealt,
            "Total Healing" => session.TotalHealingDone,
            _ => 0
        };
    }

    private static string GetDefaultStoragePath()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return System.IO.Path.Combine(appData, "CamelotCombatReporter");
    }

    #endregion
}

/// <summary>
/// ViewModel for displaying a session summary.
/// </summary>
public class SessionSummaryViewModel
{
    public SessionSummaryViewModel(SessionSummary summary)
    {
        Summary = summary;
        SessionId = summary.SessionId;
        SessionDate = summary.SessionDate;
        FormattedDate = summary.SessionDate.ToString("yyyy-MM-dd HH:mm");
        Duration = summary.Duration.ToString(@"h\:mm\:ss");
        DpsDisplay = $"{summary.DamagePerSecond:F1}";
        KdDisplay = summary.Deaths > 0
            ? $"{summary.Kills}/{summary.Deaths} ({summary.KillDeathRatio:F2})"
            : $"{summary.Kills}/0";
    }

    public SessionSummary Summary { get; }
    public Guid SessionId { get; }
    public DateTime SessionDate { get; }
    public string FormattedDate { get; }
    public string Duration { get; }
    public string DpsDisplay { get; }
    public string KdDisplay { get; }
}

/// <summary>
/// ViewModel for displaying a metric delta.
/// </summary>
public class MetricDeltaViewModel
{
    public MetricDeltaViewModel(MetricDelta delta)
    {
        MetricName = delta.MetricName;
        Category = delta.Category;
        BaseValue = delta.FormattedBase;
        CompareValue = delta.FormattedCompare;
        ChangeDisplay = delta.FormattedChange;
        Direction = delta.Direction;
        IsSignificant = delta.IsSignificant;

        DirectionIcon = delta.Direction switch
        {
            ChangeDirection.Improved => "Improved",
            ChangeDirection.Declined => "Declined",
            ChangeDirection.NewMetric => "New",
            _ => "-"
        };

        DirectionColor = delta.Direction switch
        {
            ChangeDirection.Improved => "#4CAF50",
            ChangeDirection.Declined => "#F44336",
            ChangeDirection.NewMetric => "#2196F3",
            _ => "#9E9E9E"
        };
    }

    public string MetricName { get; }
    public string Category { get; }
    public string BaseValue { get; }
    public string CompareValue { get; }
    public string ChangeDisplay { get; }
    public ChangeDirection Direction { get; }
    public bool IsSignificant { get; }
    public string DirectionIcon { get; }
    public string DirectionColor { get; }
}

/// <summary>
/// ViewModel for displaying a goal.
/// </summary>
public class GoalViewModel
{
    public GoalViewModel(PerformanceGoal goal)
    {
        Id = goal.Id;
        Name = goal.Name;
        Type = goal.Type.ToString();
        TargetValue = goal.TargetValue;
        CurrentValue = goal.CurrentValue ?? 0;
        Status = goal.Status.ToString();
        ProgressPercent = goal.CurrentValue.HasValue && goal.TargetValue > 0
            ? Math.Min(100, (goal.CurrentValue.Value / goal.TargetValue) * 100)
            : 0;
        ProgressDisplay = $"{ProgressPercent:F0}%";

        StatusColor = goal.Status switch
        {
            GoalStatus.Achieved => "#4CAF50",
            GoalStatus.InProgress => "#2196F3",
            GoalStatus.Failed or GoalStatus.Expired => "#F44336",
            _ => "#9E9E9E"
        };
    }

    public Guid Id { get; }
    public string Name { get; }
    public string Type { get; }
    public double TargetValue { get; }
    public double CurrentValue { get; }
    public string Status { get; }
    public double ProgressPercent { get; }
    public string ProgressDisplay { get; }
    public string StatusColor { get; }
}

/// <summary>
/// ViewModel for displaying a personal best.
/// </summary>
public class PersonalBestViewModel
{
    public PersonalBestViewModel(PersonalBest pb)
    {
        MetricName = pb.MetricName;
        Value = $"{pb.Value:N0}";
        AchievedAt = pb.AchievedAt.ToString("yyyy-MM-dd HH:mm");
        ImprovementDisplay = pb.ImprovementPercent.HasValue
            ? $"+{pb.ImprovementPercent.Value:F1}%"
            : "First!";
    }

    public string MetricName { get; }
    public string Value { get; }
    public string AchievedAt { get; }
    public string ImprovementDisplay { get; }
}
