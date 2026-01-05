using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using CamelotCombatReporter.Core.BuffTracking;
using CamelotCombatReporter.Core.BuffTracking.Models;
using CamelotCombatReporter.Core.Models;
using CamelotCombatReporter.Core.Parsing;
using CamelotCombatReporter.Gui.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;

namespace CamelotCombatReporter.Gui.BuffTracking.ViewModels;

/// <summary>
/// ViewModel for the Buff Tracking view.
/// </summary>
public partial class BuffTrackingViewModel : ViewModelBase
{
    private readonly IBuffTrackingService _buffService;

    #region Statistics Properties

    [ObservableProperty]
    private int _totalBuffsApplied;

    [ObservableProperty]
    private int _totalDebuffsApplied;

    [ObservableProperty]
    private int _totalDebuffsReceived;

    [ObservableProperty]
    private string _overallBuffUptime = "0%";

    [ObservableProperty]
    private int _criticalGapsCount;

    [ObservableProperty]
    private string _sessionDuration = "0:00:00";

    [ObservableProperty]
    private bool _hasData;

    #endregion

    #region Uptime Stats Collection

    [ObservableProperty]
    private ObservableCollection<BuffUptimeViewModel> _uptimeStats = new();

    [ObservableProperty]
    private BuffUptimeViewModel? _selectedUptime;

    #endregion

    #region Timeline Collection

    [ObservableProperty]
    private ObservableCollection<BuffTimelineEntryViewModel> _timelineEntries = new();

    #endregion

    #region Gaps Collection

    [ObservableProperty]
    private ObservableCollection<BuffGapViewModel> _criticalGaps = new();

    #endregion

    #region Chart Properties

    [ObservableProperty]
    private ISeries[] _categoryDistributionSeries = Array.Empty<ISeries>();

    [ObservableProperty]
    private ISeries[] _uptimeChartSeries = Array.Empty<ISeries>();

    [ObservableProperty]
    private Axis[] _uptimeChartXAxes = Array.Empty<Axis>();

    [ObservableProperty]
    private Axis[] _uptimeChartYAxes = Array.Empty<Axis>();

    #endregion

    #region Filter Properties

    [ObservableProperty]
    private string _selectedCategory = "All";

    public string[] CategoryOptions { get; } = new[]
    {
        "All", "Stat Buffs", "Armor/Defense", "Resistance", "Damage/Speed", "Regeneration", "Debuffs", "DoT Effects"
    };

    [ObservableProperty]
    private bool _showBeneficialOnly = false;

    [ObservableProperty]
    private bool _showDetrimentalOnly = false;

    #endregion

    public BuffTrackingViewModel()
    {
        _buffService = new BuffTrackingService();
    }

    public BuffTrackingViewModel(IBuffTrackingService buffService)
    {
        _buffService = buffService;
    }

    /// <summary>
    /// Analyzes buffs from a log file.
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
    /// Analyzes buffs from a log file path.
    /// </summary>
    public async Task AnalyzeLogFile(string filePath)
    {
        await Task.Run(() =>
        {
            var parser = new LogParser(filePath);
            var events = parser.Parse().ToList();

            // Calculate session duration
            TimeSpan duration = TimeSpan.Zero;
            if (events.Count > 1)
            {
                var first = events.First().Timestamp;
                var last = events.Last().Timestamp;
                duration = last.ToTimeSpan() - first.ToTimeSpan();
                if (duration < TimeSpan.Zero)
                    duration = TimeSpan.FromHours(24) + duration;
            }

            var statistics = _buffService.CalculateStatistics(events, duration, null);
            var timeline = _buffService.BuildTimeline(events);
            var gaps = _buffService.DetectCriticalGaps(events, TimeSpan.FromSeconds(5));

            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                UpdateUI(statistics, timeline, gaps, duration);
            });
        });
    }

    /// <summary>
    /// Analyzes buffs from existing events.
    /// </summary>
    public void AnalyzeEvents(System.Collections.Generic.IEnumerable<LogEvent> events)
    {
        var eventsList = events.ToList();

        TimeSpan duration = TimeSpan.Zero;
        if (eventsList.Count > 1)
        {
            var first = eventsList.First().Timestamp;
            var last = eventsList.Last().Timestamp;
            duration = last.ToTimeSpan() - first.ToTimeSpan();
            if (duration < TimeSpan.Zero)
                duration = TimeSpan.FromHours(24) + duration;
        }

        var statistics = _buffService.CalculateStatistics(eventsList, duration, null);
        var timeline = _buffService.BuildTimeline(eventsList);
        var gaps = _buffService.DetectCriticalGaps(eventsList, TimeSpan.FromSeconds(5));
        UpdateUI(statistics, timeline, gaps, duration);
    }

    private void UpdateUI(
        BuffStatistics statistics,
        System.Collections.Generic.IReadOnlyList<BuffTimelineEntry> timeline,
        System.Collections.Generic.IReadOnlyList<BuffGap> gaps,
        TimeSpan duration)
    {
        // Clear existing data
        UptimeStats.Clear();
        TimelineEntries.Clear();
        CriticalGaps.Clear();

        // Update statistics
        TotalBuffsApplied = statistics.TotalBuffsApplied;
        TotalDebuffsApplied = statistics.TotalDebuffsApplied;
        TotalDebuffsReceived = statistics.TotalDebuffsReceived;
        OverallBuffUptime = $"{statistics.OverallBuffUptime:F1}%";
        CriticalGapsCount = statistics.CriticalGaps.Count;
        SessionDuration = duration.ToString(@"h\:mm\:ss");
        HasData = statistics.TotalBuffsApplied > 0 || statistics.TotalDebuffsReceived > 0;

        // Add uptime stats
        foreach (var (buffId, uptimeStat) in statistics.UptimeByBuff.OrderByDescending(kvp => kvp.Value.UptimePercent))
        {
            UptimeStats.Add(new BuffUptimeViewModel(uptimeStat));
        }

        // Add timeline entries (last 100)
        foreach (var entry in timeline.TakeLast(100).OrderByDescending(e => e.Timestamp))
        {
            TimelineEntries.Add(new BuffTimelineEntryViewModel(entry));
        }

        // Add critical gaps
        foreach (var gap in gaps.OrderByDescending(g => g.GapDuration))
        {
            CriticalGaps.Add(new BuffGapViewModel(gap));
        }

        // Update charts
        UpdateCategoryChart(statistics);
        UpdateUptimeChart(statistics);
    }

    private void UpdateCategoryChart(BuffStatistics statistics)
    {
        var buffCategories = statistics.BuffsByCategory
            .Where(kvp => kvp.Value > 0)
            .ToList();

        if (!buffCategories.Any())
        {
            CategoryDistributionSeries = Array.Empty<ISeries>();
            return;
        }

        var colors = new Dictionary<BuffCategory, SKColor>
        {
            { BuffCategory.StatBuff, new SKColor(76, 175, 80) },       // Green
            { BuffCategory.ArmorBuff, new SKColor(33, 150, 243) },     // Blue
            { BuffCategory.ResistanceBuff, new SKColor(156, 39, 176) },// Purple
            { BuffCategory.DamageAddBuff, new SKColor(244, 67, 54) },  // Red
            { BuffCategory.SpeedBuff, new SKColor(255, 193, 7) },      // Amber
            { BuffCategory.HasteBuff, new SKColor(255, 152, 0) },      // Orange
            { BuffCategory.RegenerationBuff, new SKColor(0, 150, 136) },// Teal
            { BuffCategory.Disease, new SKColor(139, 195, 74) },       // Light Green
            { BuffCategory.DamageOverTime, new SKColor(121, 85, 72) }  // Brown
        };

        var series = buffCategories
            .Select(kvp => new PieSeries<int>
            {
                Values = new[] { kvp.Value },
                Name = FormatCategory(kvp.Key),
                Fill = colors.TryGetValue(kvp.Key, out var color)
                    ? new SolidColorPaint(color)
                    : new SolidColorPaint(SKColors.Gray)
            })
            .Cast<ISeries>()
            .ToArray();

        CategoryDistributionSeries = series;
    }

    private void UpdateUptimeChart(BuffStatistics statistics)
    {
        var uptimeData = statistics.UptimeByBuff.Values
            .OrderByDescending(u => u.UptimePercent)
            .Take(10)
            .ToList();

        if (!uptimeData.Any())
        {
            UptimeChartSeries = Array.Empty<ISeries>();
            return;
        }

        UptimeChartSeries = new ISeries[]
        {
            new RowSeries<double>
            {
                Values = uptimeData.Select(u => u.UptimePercent).ToArray(),
                Name = "Uptime %",
                Fill = new SolidColorPaint(new SKColor(76, 175, 80))
            }
        };

        UptimeChartXAxes = new Axis[]
        {
            new Axis
            {
                Name = "Uptime %",
                MinLimit = 0,
                MaxLimit = 100
            }
        };

        UptimeChartYAxes = new Axis[]
        {
            new Axis
            {
                Labels = uptimeData.Select(u => TruncateName(u.BuffDefinition.Name, 15)).ToArray()
            }
        };
    }

    private static string FormatCategory(BuffCategory category)
    {
        return category switch
        {
            BuffCategory.StatBuff => "Stat Buffs",
            BuffCategory.ArmorBuff => "Armor",
            BuffCategory.ResistanceBuff => "Resist",
            BuffCategory.DamageAddBuff => "Damage Add",
            BuffCategory.ToHitBuff => "To-Hit",
            BuffCategory.SpeedBuff => "Speed",
            BuffCategory.HasteBuff => "Haste",
            BuffCategory.RegenerationBuff => "Regen",
            BuffCategory.ConcentrationBuff => "Concentration",
            BuffCategory.RealmAbilityBuff => "RA Buff",
            BuffCategory.StatDebuff => "Stat Debuff",
            BuffCategory.ResistDebuff => "Resist Debuff",
            BuffCategory.SpeedDebuff => "Speed Debuff",
            BuffCategory.DamageOverTime => "DoT",
            BuffCategory.Disease => "Disease",
            BuffCategory.Bleed => "Bleed",
            BuffCategory.ArmorDebuff => "AF Debuff",
            _ => category.ToString()
        };
    }

    private static string TruncateName(string name, int maxLength)
    {
        if (name.Length <= maxLength)
            return name;
        return name.Substring(0, maxLength - 3) + "...";
    }
}

/// <summary>
/// ViewModel for buff uptime display.
/// </summary>
public class BuffUptimeViewModel
{
    public BuffUptimeViewModel(BuffUptimeStats stats)
    {
        Stats = stats;
        BuffName = stats.BuffDefinition.Name;
        Category = FormatCategory(stats.BuffDefinition.Category);
        TargetName = stats.TargetName;
        UptimePercent = $"{stats.UptimePercent:F1}%";
        UptimeColor = GetUptimeColor(stats.UptimePercent);
        TotalTime = stats.TotalBuffedTime.ToString(@"m\:ss");
        ApplicationCount = stats.ApplicationCount;
        RefreshCount = stats.RefreshCount;
        GapCount = stats.Gaps.Count;
        AvgGapDuration = stats.AverageGapDuration.TotalSeconds > 0
            ? $"{stats.AverageGapDuration.TotalSeconds:F1}s"
            : "-";
        IsBeneficial = IsBeneficialCategory(stats.BuffDefinition.Category);
    }

    public BuffUptimeStats Stats { get; }
    public string BuffName { get; }
    public string Category { get; }
    public string TargetName { get; }
    public string UptimePercent { get; }
    public string UptimeColor { get; }
    public string TotalTime { get; }
    public int ApplicationCount { get; }
    public int RefreshCount { get; }
    public int GapCount { get; }
    public string AvgGapDuration { get; }
    public bool IsBeneficial { get; }

    private static string GetUptimeColor(double percent)
    {
        return percent switch
        {
            >= 90 => "#4CAF50",  // Green
            >= 70 => "#8BC34A",  // Light Green
            >= 50 => "#FFC107",  // Amber
            >= 30 => "#FF9800",  // Orange
            _ => "#F44336"       // Red
        };
    }

    private static string FormatCategory(BuffCategory category)
    {
        return category switch
        {
            BuffCategory.StatBuff => "Stat",
            BuffCategory.ArmorBuff => "Armor",
            BuffCategory.ResistanceBuff => "Resist",
            BuffCategory.DamageAddBuff => "Damage",
            BuffCategory.SpeedBuff => "Speed",
            BuffCategory.HasteBuff => "Haste",
            BuffCategory.RegenerationBuff => "Regen",
            BuffCategory.Disease => "Disease",
            BuffCategory.DamageOverTime => "DoT",
            _ => category.ToString()
        };
    }

    private static bool IsBeneficialCategory(BuffCategory category)
    {
        return category switch
        {
            BuffCategory.StatDebuff or
            BuffCategory.ResistDebuff or
            BuffCategory.SpeedDebuff or
            BuffCategory.DamageOverTime or
            BuffCategory.Disease or
            BuffCategory.Bleed or
            BuffCategory.ArmorDebuff => false,
            _ => true
        };
    }
}

/// <summary>
/// ViewModel for buff timeline entry display.
/// </summary>
public class BuffTimelineEntryViewModel
{
    public BuffTimelineEntryViewModel(BuffTimelineEntry entry)
    {
        Entry = entry;
        Timestamp = entry.Timestamp.ToString("HH:mm:ss");
        BuffName = entry.BuffName;
        EventType = entry.EventType.ToString();
        EventTypeColor = GetEventTypeColor(entry.EventType);
        TargetName = entry.TargetName;
        SourceName = entry.SourceName ?? "-";
        Duration = entry.Duration.HasValue
            ? $"{entry.Duration.Value.TotalSeconds:F0}s"
            : "-";
        IsBeneficial = entry.IsBeneficial;
        BeneficialIndicator = entry.IsBeneficial ? "+" : "-";
        BeneficialColor = entry.IsBeneficial ? "#4CAF50" : "#F44336";
    }

    public BuffTimelineEntry Entry { get; }
    public string Timestamp { get; }
    public string BuffName { get; }
    public string EventType { get; }
    public string EventTypeColor { get; }
    public string TargetName { get; }
    public string SourceName { get; }
    public string Duration { get; }
    public bool IsBeneficial { get; }
    public string BeneficialIndicator { get; }
    public string BeneficialColor { get; }

    private static string GetEventTypeColor(BuffEventType eventType)
    {
        return eventType switch
        {
            BuffEventType.Applied => "#4CAF50",    // Green
            BuffEventType.Refreshed => "#2196F3", // Blue
            BuffEventType.Expired => "#FF9800",   // Orange
            BuffEventType.Removed => "#F44336",   // Red
            BuffEventType.Resisted => "#9C27B0",  // Purple
            _ => "#607D8B"
        };
    }
}

/// <summary>
/// ViewModel for buff gap display.
/// </summary>
public class BuffGapViewModel
{
    public BuffGapViewModel(BuffGap gap)
    {
        Gap = gap;
        BuffName = gap.BuffDefinition.Name;
        TargetName = gap.TargetName;
        GapStart = gap.GapStart.ToString("HH:mm:ss");
        GapEnd = gap.GapEnd?.ToString("HH:mm:ss") ?? "Still active";
        GapDuration = $"{gap.GapDuration.TotalSeconds:F1}s";
        Context = gap.Context ?? "";
        IsCritical = gap.GapDuration.TotalSeconds > 5;
        SeverityColor = GetSeverityColor(gap.GapDuration);
    }

    public BuffGap Gap { get; }
    public string BuffName { get; }
    public string TargetName { get; }
    public string GapStart { get; }
    public string GapEnd { get; }
    public string GapDuration { get; }
    public string Context { get; }
    public bool IsCritical { get; }
    public string SeverityColor { get; }

    private static string GetSeverityColor(TimeSpan duration)
    {
        return duration.TotalSeconds switch
        {
            > 30 => "#F44336",  // Red - critical
            > 15 => "#FF9800",  // Orange - warning
            > 5 => "#FFC107",   // Amber - caution
            _ => "#4CAF50"      // Green - ok
        };
    }
}
