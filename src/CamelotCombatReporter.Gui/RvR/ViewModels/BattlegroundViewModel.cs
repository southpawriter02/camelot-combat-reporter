using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using CamelotCombatReporter.Core.Models;
using CamelotCombatReporter.Core.Parsing;
using CamelotCombatReporter.Core.RvR;
using CamelotCombatReporter.Core.RvR.Models;
using CamelotCombatReporter.Gui.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;

namespace CamelotCombatReporter.Gui.RvR.ViewModels;

public partial class BattlegroundViewModel : ViewModelBase
{
    private readonly IBattlegroundService _bgService;

    #region Observable Properties

    [ObservableProperty]
    private bool _hasData;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _statusMessage = "Load a combat log to analyze battleground performance";

    // Overall Statistics
    [ObservableProperty]
    private int _totalSessions;

    [ObservableProperty]
    private int _totalKills;

    [ObservableProperty]
    private int _totalDeaths;

    [ObservableProperty]
    private string _killDeathRatio = "0.00";

    [ObservableProperty]
    private string _totalDamage = "0";

    [ObservableProperty]
    private string _totalHealing = "0";

    [ObservableProperty]
    private string _totalTime = "00:00:00";

    [ObservableProperty]
    private string _estimatedRp = "0";

    [ObservableProperty]
    private string _bestBattleground = "—";

    [ObservableProperty]
    private string _mostPlayedBattleground = "—";

    // Statistics by BG Type
    [ObservableProperty]
    private ObservableCollection<BgTypeStatsViewModel> _statsByType = new();

    // Session List
    [ObservableProperty]
    private ObservableCollection<BgSessionViewModel> _sessions = new();

    // Charts
    [ObservableProperty]
    private ISeries[] _sessionsByTypeSeries = Array.Empty<ISeries>();

    [ObservableProperty]
    private ISeries[] _kdByTypeSeries = Array.Empty<ISeries>();

    [ObservableProperty]
    private Axis[] _kdByTypeXAxes = Array.Empty<Axis>();

    [ObservableProperty]
    private Axis[] _kdByTypeYAxes = new Axis[]
    {
        new Axis { Name = "K/D Ratio", MinLimit = 0 }
    };

    #endregion

    public BattlegroundViewModel()
    {
        _bgService = new BattlegroundService();
    }

    public BattlegroundViewModel(IBattlegroundService bgService)
    {
        _bgService = bgService;
    }

    [RelayCommand]
    private async Task AnalyzeFromFile(Window window)
    {
        var files = await window.StorageProvider.OpenFilePickerAsync(
            new FilePickerOpenOptions
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
            await AnalyzeLogFile(files[0].Path.LocalPath);
        }
    }

    public async Task AnalyzeLogFile(string filePath)
    {
        IsLoading = true;
        StatusMessage = "Analyzing combat log for battleground activity...";

        await Task.Run(() =>
        {
            try
            {
                var parser = new LogParser(filePath);
                var events = parser.Parse().ToList();

                if (events.Count == 0)
                {
                    Dispatcher.UIThread.Post(() =>
                    {
                        StatusMessage = "No events found in log file";
                        IsLoading = false;
                    });
                    return;
                }

                var sessions = _bgService.ResolveSessions(events);
                var statistics = _bgService.CalculateAllStatistics(sessions);

                Dispatcher.UIThread.Post(() =>
                {
                    UpdateUI(sessions, statistics);
                    HasData = sessions.Count > 0;
                    IsLoading = false;
                    StatusMessage = sessions.Count > 0
                        ? $"Found {sessions.Count} battleground sessions"
                        : "No battleground activity found in log";
                });
            }
            catch (Exception ex)
            {
                Dispatcher.UIThread.Post(() =>
                {
                    StatusMessage = $"Error: {ex.Message}";
                    IsLoading = false;
                });
            }
        });
    }

    public void AnalyzeEvents(IEnumerable<LogEvent> events)
    {
        var eventList = events.ToList();
        var sessions = _bgService.ResolveSessions(eventList);
        var statistics = _bgService.CalculateAllStatistics(sessions);
        UpdateUI(sessions, statistics);
        HasData = sessions.Count > 0;
    }

    private void UpdateUI(IReadOnlyList<BattlegroundSession> sessions, AllBattlegroundStatistics statistics)
    {
        // Update overall statistics
        TotalSessions = statistics.TotalSessions;
        TotalKills = statistics.OverallStatistics.Kills;
        TotalDeaths = statistics.OverallStatistics.Deaths;
        KillDeathRatio = statistics.OverallStatistics.KillDeathRatio.ToString("F2");
        TotalDamage = FormatNumber(statistics.OverallStatistics.DamageDealt);
        TotalHealing = FormatNumber(statistics.OverallStatistics.HealingDone);
        TotalTime = statistics.TotalTimeInBattlegrounds.ToString(@"hh\:mm\:ss");
        EstimatedRp = FormatNumber(statistics.OverallStatistics.RealmPointsEarned);

        BestBattleground = statistics.BestPerformingBattleground?.GetDisplayName() ?? "—";
        MostPlayedBattleground = statistics.MostPlayedBattleground?.GetDisplayName() ?? "—";

        // Update stats by type
        StatsByType.Clear();
        foreach (var (bgType, stats) in statistics.StatsByType.OrderByDescending(kvp => statistics.SessionCountByType.GetValueOrDefault(kvp.Key)))
        {
            var sessionCount = statistics.SessionCountByType.GetValueOrDefault(bgType);
            StatsByType.Add(new BgTypeStatsViewModel(bgType, stats, sessionCount));
        }

        // Update sessions list
        Sessions.Clear();
        foreach (var session in sessions.OrderByDescending(s => s.StartTime))
        {
            Sessions.Add(new BgSessionViewModel(session));
        }

        // Update charts
        GenerateSessionsByTypeChart(statistics);
        GenerateKdByTypeChart(statistics);
    }

    private string FormatNumber(int value)
    {
        if (value >= 1000000)
            return $"{value / 1000000.0:F1}M";
        if (value >= 1000)
            return $"{value / 1000.0:F1}K";
        return value.ToString();
    }

    private void GenerateSessionsByTypeChart(AllBattlegroundStatistics statistics)
    {
        if (statistics.SessionCountByType.Count == 0)
        {
            SessionsByTypeSeries = Array.Empty<ISeries>();
            return;
        }

        SessionsByTypeSeries = statistics.SessionCountByType
            .Where(kvp => kvp.Value > 0)
            .Select(kvp => new PieSeries<int>
            {
                Values = new[] { kvp.Value },
                Name = kvp.Key.GetDisplayName(),
                Fill = new SolidColorPaint(GetBgTypeColor(kvp.Key))
            } as ISeries).ToArray();
    }

    private void GenerateKdByTypeChart(AllBattlegroundStatistics statistics)
    {
        if (statistics.StatsByType.Count == 0)
        {
            KdByTypeSeries = Array.Empty<ISeries>();
            KdByTypeXAxes = Array.Empty<Axis>();
            return;
        }

        var types = statistics.StatsByType
            .OrderByDescending(kvp => kvp.Value.KillDeathRatio)
            .ToList();

        var kds = types.Select(t => t.Value.KillDeathRatio).ToArray();
        var labels = types.Select(t => t.Key.GetDisplayName()).ToArray();

        KdByTypeSeries = new ISeries[]
        {
            new ColumnSeries<double>
            {
                Values = kds,
                Name = "K/D Ratio",
                Fill = new SolidColorPaint(new SKColor(33, 150, 243))
            }
        };

        KdByTypeXAxes = new Axis[]
        {
            new Axis
            {
                Labels = labels,
                LabelsRotation = 30
            }
        };
    }

    private static SKColor GetBgTypeColor(BattlegroundType type) => type switch
    {
        BattlegroundType.Thidranki => new SKColor(76, 175, 80),
        BattlegroundType.Molvik => new SKColor(33, 150, 243),
        BattlegroundType.CathalValley => new SKColor(156, 39, 176),
        BattlegroundType.Killaloe => new SKColor(255, 152, 0),
        BattlegroundType.OpenRvR => new SKColor(244, 67, 54),
        _ => new SKColor(158, 158, 158)
    };

    [RelayCommand]
    private void Reset()
    {
        HasData = false;
        TotalSessions = 0;
        TotalKills = 0;
        TotalDeaths = 0;
        KillDeathRatio = "0.00";
        TotalDamage = "0";
        TotalHealing = "0";
        TotalTime = "00:00:00";
        EstimatedRp = "0";
        BestBattleground = "—";
        MostPlayedBattleground = "—";
        StatsByType.Clear();
        Sessions.Clear();
        SessionsByTypeSeries = Array.Empty<ISeries>();
        KdByTypeSeries = Array.Empty<ISeries>();
        StatusMessage = "Load a combat log to analyze battleground performance";
    }
}

public class BgTypeStatsViewModel
{
    public string Name { get; }
    public string LevelRange { get; }
    public int Sessions { get; }
    public int Kills { get; }
    public int Deaths { get; }
    public string KillDeathRatio { get; }
    public string DamageDealt { get; }
    public string HealingDone { get; }
    public string Color { get; }

    public BgTypeStatsViewModel(BattlegroundType type, BattlegroundStatistics stats, int sessionCount)
    {
        Name = type.GetDisplayName();
        var range = type.GetLevelRange();
        LevelRange = $"Lvl {range.Min}-{range.Max}";
        Sessions = sessionCount;
        Kills = stats.Kills;
        Deaths = stats.Deaths;
        KillDeathRatio = stats.KillDeathRatio.ToString("F2");
        DamageDealt = FormatNumber(stats.DamageDealt);
        HealingDone = FormatNumber(stats.HealingDone);

        Color = type switch
        {
            BattlegroundType.Thidranki => "#4CAF50",
            BattlegroundType.Molvik => "#2196F3",
            BattlegroundType.CathalValley => "#9C27B0",
            BattlegroundType.Killaloe => "#FF9800",
            BattlegroundType.OpenRvR => "#F44336",
            _ => "#9E9E9E"
        };
    }

    private static string FormatNumber(int value)
    {
        if (value >= 1000000)
            return $"{value / 1000000.0:F1}M";
        if (value >= 1000)
            return $"{value / 1000.0:F1}K";
        return value.ToString();
    }
}

public class BgSessionViewModel
{
    public string Zone { get; }
    public string BgType { get; }
    public string StartTime { get; }
    public string Duration { get; }
    public int Kills { get; }
    public int Deaths { get; }
    public string KillDeathRatio { get; }
    public string Color { get; }

    public BgSessionViewModel(BattlegroundSession session)
    {
        Zone = session.ZoneName;
        BgType = session.BattlegroundType.GetDisplayName();
        StartTime = session.StartTime.ToString("HH:mm:ss");
        Duration = session.Duration.ToString(@"mm\:ss");
        Kills = session.Statistics.Kills;
        Deaths = session.Statistics.Deaths;
        KillDeathRatio = session.Statistics.KillDeathRatio.ToString("F2");

        Color = session.BattlegroundType switch
        {
            BattlegroundType.Thidranki => "#4CAF50",
            BattlegroundType.Molvik => "#2196F3",
            BattlegroundType.CathalValley => "#9C27B0",
            BattlegroundType.Killaloe => "#FF9800",
            BattlegroundType.OpenRvR => "#F44336",
            _ => "#9E9E9E"
        };
    }
}
