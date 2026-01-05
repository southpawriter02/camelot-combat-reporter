using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using CamelotCombatReporter.Core.CrowdControlAnalysis;
using CamelotCombatReporter.Core.CrowdControlAnalysis.Models;
using CamelotCombatReporter.Core.Models;
using CamelotCombatReporter.Core.Parsing;
using CamelotCombatReporter.Gui.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;

namespace CamelotCombatReporter.Gui.CrowdControlAnalysis.ViewModels;

/// <summary>
/// ViewModel for the Crowd Control Analysis view.
/// </summary>
public partial class CCAnalysisViewModel : ViewModelBase
{
    private readonly ICCAnalysisService _analysisService;

    #region Statistics Properties

    [ObservableProperty]
    private int _totalCcApplied;

    [ObservableProperty]
    private int _totalCcResisted;

    [ObservableProperty]
    private string _ccUptime = "0%";

    [ObservableProperty]
    private string _averageDuration = "0.0s";

    [ObservableProperty]
    private string _drEfficiency = "0%";

    [ObservableProperty]
    private int _killsWithinCc;

    [ObservableProperty]
    private string _totalDamageDuringCc = "0";

    [ObservableProperty]
    private bool _hasData;

    #endregion

    #region Timeline Collection

    [ObservableProperty]
    private ObservableCollection<CCTimelineEntryViewModel> _timelineEntries = new();

    [ObservableProperty]
    private CCTimelineEntryViewModel? _selectedEntry;

    #endregion

    #region Chain Collection

    [ObservableProperty]
    private ObservableCollection<CCChainViewModel> _chains = new();

    [ObservableProperty]
    private CCChainViewModel? _selectedChain;

    #endregion

    #region Chart Properties

    [ObservableProperty]
    private ISeries[] _ccTypeSeries = Array.Empty<ISeries>();

    [ObservableProperty]
    private ISeries[] _drLevelSeries = Array.Empty<ISeries>();

    #endregion

    #region Filter Properties

    [ObservableProperty]
    private string _selectedCcType = "All";

    public string[] CcTypeOptions { get; } = new[]
    {
        "All", "Mez", "Stun", "Root", "Snare", "Silence", "Disarm"
    };

    [ObservableProperty]
    private string _selectedTarget = "All";

    [ObservableProperty]
    private ObservableCollection<string> _targetOptions = new() { "All" };

    #endregion

    public CCAnalysisViewModel()
    {
        _analysisService = new CCAnalysisService();
    }

    public CCAnalysisViewModel(ICCAnalysisService analysisService)
    {
        _analysisService = analysisService;
    }

    /// <summary>
    /// Analyzes CC from a log file.
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
    /// Analyzes CC from a log file path.
    /// </summary>
    public async Task AnalyzeLogFile(string filePath)
    {
        await Task.Run(() =>
        {
            var parser = new LogParser(filePath);
            var events = parser.Parse().ToList();

            // Get combat duration from first to last event
            var combatDuration = TimeSpan.Zero;
            if (events.Count >= 2)
            {
                var firstTime = events.First().Timestamp;
                var lastTime = events.Last().Timestamp;
                combatDuration = lastTime - firstTime;
            }

            var applications = _analysisService.ExtractCCApplications(events);
            var chains = _analysisService.DetectChains(applications);
            var statistics = _analysisService.CalculateStatistics(events, combatDuration);
            var timeline = _analysisService.BuildTimeline(events);

            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                UpdateUI(applications, chains, statistics, timeline);
            });
        });
    }

    /// <summary>
    /// Analyzes CC from existing events.
    /// </summary>
    public void AnalyzeEvents(System.Collections.Generic.IEnumerable<LogEvent> events, TimeSpan combatDuration)
    {
        var eventsList = events.ToList();
        var applications = _analysisService.ExtractCCApplications(eventsList);
        var chains = _analysisService.DetectChains(applications);
        var statistics = _analysisService.CalculateStatistics(eventsList, combatDuration);
        var timeline = _analysisService.BuildTimeline(eventsList);
        UpdateUI(applications, chains, statistics, timeline);
    }

    private void UpdateUI(
        System.Collections.Generic.IReadOnlyList<CCApplication> applications,
        System.Collections.Generic.IReadOnlyList<CCChain> chains,
        CCStatistics statistics,
        System.Collections.Generic.IReadOnlyList<CCTimelineEntry> timeline)
    {
        // Clear existing data
        TimelineEntries.Clear();
        Chains.Clear();
        TargetOptions.Clear();
        TargetOptions.Add("All");

        // Update statistics
        TotalCcApplied = statistics.TotalCCApplied;
        TotalCcResisted = statistics.TotalCCResisted;
        CcUptime = $"{statistics.CCUptimePercent:F1}%";
        AverageDuration = $"{statistics.AverageDuration.TotalSeconds:F1}s";
        DrEfficiency = $"{statistics.DREfficiencyPercent:F0}%";
        KillsWithinCc = statistics.KillsWithinCCWindow;
        TotalDamageDuringCc = statistics.TotalDamageDuringCC.ToString("N0");
        HasData = applications.Count > 0;

        // Build unique targets list
        var targets = applications.Select(a => a.TargetName).Distinct().OrderBy(t => t);
        foreach (var target in targets)
        {
            TargetOptions.Add(target);
        }

        // Add timeline entries
        foreach (var entry in timeline)
        {
            TimelineEntries.Add(new CCTimelineEntryViewModel(entry));
        }

        // Add chains
        foreach (var chain in chains.OrderByDescending(c => c.ChainLength))
        {
            Chains.Add(new CCChainViewModel(chain));
        }

        // Update charts
        UpdateCCTypeChart(statistics);
        UpdateDRLevelChart(applications);
    }

    partial void OnSelectedCcTypeChanged(string value)
    {
        FilterTimeline();
    }

    partial void OnSelectedTargetChanged(string value)
    {
        FilterTimeline();
    }

    private void FilterTimeline()
    {
        // Re-filter timeline based on selections
        // For now, we'll just highlight matching entries
    }

    private void UpdateCCTypeChart(CCStatistics statistics)
    {
        if (!statistics.CCByType.Any())
        {
            CcTypeSeries = Array.Empty<ISeries>();
            return;
        }

        var colors = new Dictionary<CCType, SKColor>
        {
            { CCType.Mez, new SKColor(156, 39, 176) },      // Purple
            { CCType.Stun, new SKColor(244, 67, 54) },      // Red
            { CCType.Root, new SKColor(139, 195, 74) },     // Green
            { CCType.Snare, new SKColor(33, 150, 243) },    // Blue
            { CCType.Silence, new SKColor(255, 152, 0) },   // Orange
            { CCType.Disarm, new SKColor(121, 85, 72) }     // Brown
        };

        var series = new List<ISeries>();
        foreach (var kvp in statistics.CCByType.OrderByDescending(x => x.Value))
        {
            var color = colors.TryGetValue(kvp.Key, out var c) ? c : new SKColor(158, 158, 158);
            series.Add(new PieSeries<int>
            {
                Values = new[] { kvp.Value },
                Name = kvp.Key.ToString(),
                Fill = new SolidColorPaint(color)
            });
        }

        CcTypeSeries = series.ToArray();
    }

    private void UpdateDRLevelChart(System.Collections.Generic.IReadOnlyList<CCApplication> applications)
    {
        if (!applications.Any())
        {
            DrLevelSeries = Array.Empty<ISeries>();
            return;
        }

        var byDR = applications.GroupBy(a => a.DRAtApplication)
            .ToDictionary(g => g.Key, g => g.Count());

        var colors = new Dictionary<DRLevel, SKColor>
        {
            { DRLevel.Full, new SKColor(76, 175, 80) },     // Green
            { DRLevel.Reduced, new SKColor(255, 235, 59) }, // Yellow
            { DRLevel.Minimal, new SKColor(255, 152, 0) },  // Orange
            { DRLevel.Immune, new SKColor(244, 67, 54) }    // Red
        };

        var series = new List<ISeries>();
        foreach (var level in new[] { DRLevel.Full, DRLevel.Reduced, DRLevel.Minimal, DRLevel.Immune })
        {
            if (byDR.TryGetValue(level, out var count))
            {
                var color = colors[level];
                series.Add(new PieSeries<int>
                {
                    Values = new[] { count },
                    Name = level.ToString(),
                    Fill = new SolidColorPaint(color)
                });
            }
        }

        DrLevelSeries = series.ToArray();
    }
}

/// <summary>
/// ViewModel for a CC timeline entry.
/// </summary>
public class CCTimelineEntryViewModel
{
    private readonly CCTimelineEntry _entry;

    public CCTimelineEntryViewModel(CCTimelineEntry entry)
    {
        _entry = entry;
    }

    public string Timestamp => _entry.Timestamp.ToString("HH:mm:ss");
    public string CcType => _entry.CrowdControlType.ToString();
    public string TargetName => _entry.TargetName;
    public string SourceName => _entry.SourceName ?? "Unknown";
    public string EventType => _entry.EventType.ToString();
    public string DrLevel => _entry.DRLevel.ToString();
    public string Duration => _entry.Duration?.TotalSeconds.ToString("F1") + "s" ?? "-";
    public string DisplayColor => _entry.DisplayColor;

    public string DrLevelDisplay => _entry.DRLevel switch
    {
        DRLevel.Full => "100%",
        DRLevel.Reduced => "50%",
        DRLevel.Minimal => "25%",
        DRLevel.Immune => "0%",
        _ => "?"
    };
}

/// <summary>
/// ViewModel for a CC chain.
/// </summary>
public class CCChainViewModel
{
    private readonly CCChain _chain;

    public CCChainViewModel(CCChain chain)
    {
        _chain = chain;
    }

    public CCChain Chain => _chain;

    public string TargetName => _chain.TargetName;
    public string StartTime => _chain.StartTime.ToString("HH:mm:ss");
    public string EndTime => _chain.EndTime.ToString("HH:mm:ss");
    public int ChainLength => _chain.ChainLength;
    public string TotalDuration => $"{_chain.TotalDuration.TotalSeconds:F1}s";
    public string GapTime => $"{_chain.GapTime.TotalSeconds:F1}s";
    public string Efficiency => $"{_chain.EfficiencyPercent:F0}%";

    public string EfficiencyColor => _chain.EfficiencyPercent switch
    {
        >= 90 => "#4CAF50",  // Green
        >= 70 => "#FFEB3B",  // Yellow
        >= 50 => "#FF9800",  // Orange
        _ => "#F44336"       // Red
    };
}
