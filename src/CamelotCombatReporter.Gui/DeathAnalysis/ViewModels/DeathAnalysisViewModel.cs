using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using CamelotCombatReporter.Core.DeathAnalysis;
using CamelotCombatReporter.Core.DeathAnalysis.Models;
using CamelotCombatReporter.Core.Models;
using CamelotCombatReporter.Core.Parsing;
using CamelotCombatReporter.Gui.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;

namespace CamelotCombatReporter.Gui.DeathAnalysis.ViewModels;

/// <summary>
/// ViewModel for the Death Analysis view.
/// </summary>
public partial class DeathAnalysisViewModel : ViewModelBase
{
    private readonly IDeathAnalysisService _analysisService;

    #region Statistics Properties

    [ObservableProperty]
    private int _totalDeaths;

    [ObservableProperty]
    private string _averageTTD = "0.0s";

    [ObservableProperty]
    private string _topKiller = "N/A";

    [ObservableProperty]
    private string _mostCommonCategory = "N/A";

    [ObservableProperty]
    private string _ccDeathPercent = "0%";

    [ObservableProperty]
    private string _averageDamage = "0";

    [ObservableProperty]
    private bool _hasData;

    #endregion

    #region Deaths Collection

    [ObservableProperty]
    private ObservableCollection<DeathReportViewModel> _deaths = new();

    [ObservableProperty]
    private DeathReportViewModel? _selectedDeath;

    #endregion

    #region Chart Properties

    [ObservableProperty]
    private ISeries[] _categoryDistributionSeries = Array.Empty<ISeries>();

    [ObservableProperty]
    private ISeries[] _damageTimelineSeries = Array.Empty<ISeries>();

    [ObservableProperty]
    private Axis[] _damageTimelineXAxes = Array.Empty<Axis>();

    #endregion

    #region Recommendations

    [ObservableProperty]
    private ObservableCollection<RecommendationViewModel> _recommendations = new();

    #endregion

    #region Filter Properties

    [ObservableProperty]
    private string _selectedCategory = "All";

    public string[] CategoryOptions { get; } = new[]
    {
        "All", "Burst", "Attrition", "Execution"
    };

    #endregion

    public DeathAnalysisViewModel()
    {
        _analysisService = new DeathAnalysisService();
    }

    public DeathAnalysisViewModel(IDeathAnalysisService analysisService)
    {
        _analysisService = analysisService;
    }

    /// <summary>
    /// Analyzes deaths from a log file.
    /// </summary>
    [RelayCommand]
    private async Task AnalyzeFromFile(Window window)
    {
        var storageProvider = window.StorageProvider;
        var files = await storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select Combat Log",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("Log Files") { Patterns = new[] { "*.log", "*.txt" } },
                new FilePickerFileType("All Files") { Patterns = new[] { "*" } }
            }
        });

        if (files.Count > 0)
        {
            var filePath = files[0].Path.LocalPath;
            await AnalyzeLogFile(filePath);
        }
    }

    /// <summary>
    /// Analyzes deaths from a log file path.
    /// </summary>
    public async Task AnalyzeLogFile(string filePath)
    {
        await Task.Run(() =>
        {
            var parser = new LogParser(filePath);
            var events = parser.Parse().ToList();

            var reports = _analysisService.AnalyzeAllDeaths(events);
            var statistics = _analysisService.GetStatistics(reports);

            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                UpdateUI(reports, statistics);
            });
        });
    }

    /// <summary>
    /// Analyzes deaths from existing events.
    /// </summary>
    public void AnalyzeEvents(System.Collections.Generic.IEnumerable<LogEvent> events)
    {
        var eventsList = events.ToList();
        var reports = _analysisService.AnalyzeAllDeaths(eventsList);
        var statistics = _analysisService.GetStatistics(reports);
        UpdateUI(reports, statistics);
    }

    private void UpdateUI(
        System.Collections.Generic.IReadOnlyList<DeathReport> reports,
        DeathStatistics statistics)
    {
        // Clear existing data
        Deaths.Clear();
        Recommendations.Clear();

        // Update statistics
        TotalDeaths = statistics.TotalDeaths;
        AverageTTD = $"{statistics.AverageTimeToDeath.TotalSeconds:F1}s";
        CcDeathPercent = $"{statistics.CCDeathPercent:F0}%";
        AverageDamage = statistics.AverageDamageTaken.ToString("N0");
        HasData = reports.Count > 0;

        // Update top killer
        if (statistics.TopKillerClasses.Any())
        {
            var topClass = statistics.TopKillerClasses.MaxBy(kvp => kvp.Value);
            TopKiller = topClass.Key.ToString();
        }
        else
        {
            TopKiller = "N/A";
        }

        // Update most common category
        if (statistics.DeathsByCategory.Any())
        {
            var topCategory = statistics.DeathsByCategory.MaxBy(kvp => kvp.Value);
            MostCommonCategory = FormatCategoryShort(topCategory.Key);
        }
        else
        {
            MostCommonCategory = "N/A";
        }

        // Add death reports
        foreach (var report in reports.OrderByDescending(r => r.DeathEvent.Timestamp))
        {
            Deaths.Add(new DeathReportViewModel(report));
        }

        // Update charts
        UpdateCategoryChart(statistics);

        // Update recommendations from first death if available
        if (reports.Any())
        {
            UpdateRecommendations(reports.First());
        }
    }

    partial void OnSelectedDeathChanged(DeathReportViewModel? value)
    {
        if (value != null)
        {
            UpdateRecommendations(value.Report);
            UpdateDamageTimeline(value.Report);
        }
    }

    private void UpdateRecommendations(DeathReport report)
    {
        Recommendations.Clear();
        foreach (var rec in report.Recommendations)
        {
            Recommendations.Add(new RecommendationViewModel(rec));
        }
    }

    private void UpdateCategoryChart(DeathStatistics statistics)
    {
        if (!statistics.DeathsByCategory.Any())
        {
            CategoryDistributionSeries = Array.Empty<ISeries>();
            return;
        }

        CategoryDistributionSeries = new ISeries[]
        {
            new PieSeries<double>
            {
                Values = statistics.DeathsByCategory.Values.Select(v => (double)v).ToArray(),
                Name = "Deaths by Category"
            }
        };
    }

    private void UpdateDamageTimeline(DeathReport report)
    {
        if (!report.DamageTimeline.Any())
        {
            DamageTimelineSeries = Array.Empty<ISeries>();
            return;
        }

        var values = report.DamageTimeline
            .OrderBy(b => b.SecondOffset)
            .Select(b => (double)b.TotalDamage)
            .ToArray();

        DamageTimelineSeries = new ISeries[]
        {
            new ColumnSeries<double>
            {
                Values = values,
                Name = "Damage per Second",
                Fill = new SolidColorPaint(new SKColor(244, 67, 54))
            }
        };

        DamageTimelineXAxes = new Axis[]
        {
            new Axis
            {
                Name = "Seconds Before Death",
                Labels = report.DamageTimeline
                    .OrderBy(b => b.SecondOffset)
                    .Select(b => b.SecondOffset.ToString())
                    .ToArray()
            }
        };
    }

    private static string FormatCategoryShort(DeathCategory category)
    {
        return category switch
        {
            DeathCategory.BurstAlphaStrike or
            DeathCategory.BurstCoordinated or
            DeathCategory.BurstCCChain => "Burst",

            DeathCategory.AttritionHealingDeficit or
            DeathCategory.AttritionResourceExhaustion or
            DeathCategory.AttritionPositional => "Attrition",

            DeathCategory.ExecutionLowHealth or
            DeathCategory.ExecutionDoT => "Execution",

            DeathCategory.Environmental => "Environmental",
            _ => "Unknown"
        };
    }
}

/// <summary>
/// ViewModel for a recommendation.
/// </summary>
public partial class RecommendationViewModel : ViewModelBase
{
    public RecommendationViewModel(Recommendation recommendation)
    {
        Title = recommendation.Title;
        Description = recommendation.Description;
        Priority = recommendation.Priority.ToString();
        PriorityColor = GetPriorityColor(recommendation.Priority);
    }

    public string Title { get; }
    public string Description { get; }
    public string Priority { get; }
    public string PriorityColor { get; }

    private static string GetPriorityColor(RecommendationPriority priority)
    {
        return priority switch
        {
            RecommendationPriority.Critical => "#F44336",
            RecommendationPriority.High => "#FF9800",
            RecommendationPriority.Medium => "#FFEB3B",
            RecommendationPriority.Low => "#4CAF50",
            _ => "#9E9E9E"
        };
    }
}
