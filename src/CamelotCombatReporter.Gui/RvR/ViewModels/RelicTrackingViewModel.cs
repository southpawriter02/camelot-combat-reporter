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

public partial class RelicTrackingViewModel : ViewModelBase
{
    private readonly IRelicTrackingService _relicService;

    #region Observable Properties

    [ObservableProperty]
    private bool _hasData;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _statusMessage = "Load a combat log to analyze relic events";

    // Statistics
    [ObservableProperty]
    private int _totalRaids;

    [ObservableProperty]
    private int _successfulRaids;

    [ObservableProperty]
    private int _failedRaids;

    [ObservableProperty]
    private int _timesAsCarrier;

    [ObservableProperty]
    private int _successfulDeliveries;

    [ObservableProperty]
    private int _escortKills;

    [ObservableProperty]
    private string _contributionScore = "0";

    [ObservableProperty]
    private string _averageDuration = "00:00";

    // Relic Statuses
    [ObservableProperty]
    private ObservableCollection<RelicStatusViewModel> _relicStatuses = new();

    // Raid Sessions
    [ObservableProperty]
    private ObservableCollection<RelicRaidViewModel> _raidSessions = new();

    // Carrier Statistics
    [ObservableProperty]
    private ObservableCollection<CarrierStatsViewModel> _carrierStats = new();

    // Charts
    [ObservableProperty]
    private ISeries[] _outcomesSeries = Array.Empty<ISeries>();

    [ObservableProperty]
    private ISeries[] _raidsByRelicSeries = Array.Empty<ISeries>();

    [ObservableProperty]
    private Axis[] _raidsByRelicXAxes = Array.Empty<Axis>();

    [ObservableProperty]
    private Axis[] _raidsByRelicYAxes = new Axis[]
    {
        new Axis { Name = "Raids", MinLimit = 0 }
    };

    #endregion

    public RelicTrackingViewModel()
    {
        _relicService = new RelicTrackingService();
    }

    public RelicTrackingViewModel(IRelicTrackingService relicService)
    {
        _relicService = relicService;
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
        StatusMessage = "Analyzing combat log for relic events...";

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

                var sessions = _relicService.ResolveSessions(events);
                var statistics = _relicService.CalculateStatistics(sessions);
                var statuses = _relicService.GetRelicStatuses(events);
                var carrierStats = _relicService.GetCarrierStatistics(events);

                Dispatcher.UIThread.Post(() =>
                {
                    UpdateUI(sessions, statistics, statuses, carrierStats);
                    HasData = sessions.Count > 0;
                    IsLoading = false;
                    StatusMessage = sessions.Count > 0
                        ? $"Found {sessions.Count} relic raids"
                        : "No relic events found in log";
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
        var sessions = _relicService.ResolveSessions(eventList);
        var statistics = _relicService.CalculateStatistics(sessions);
        var statuses = _relicService.GetRelicStatuses(eventList);
        var carrierStats = _relicService.GetCarrierStatistics(eventList);
        UpdateUI(sessions, statistics, statuses, carrierStats);
        HasData = sessions.Count > 0;
    }

    private void UpdateUI(
        IReadOnlyList<RelicRaidSession> sessions,
        RelicRaidStatistics statistics,
        IReadOnlyDictionary<string, RelicStatus> statuses,
        IReadOnlyDictionary<string, CarrierStatistics> carrierStats)
    {
        // Update statistics
        TotalRaids = statistics.TotalRaidsParticipated;
        SuccessfulRaids = statistics.SuccessfulRaids;
        FailedRaids = statistics.FailedRaids;
        TimesAsCarrier = statistics.TimesAsCarrier;
        SuccessfulDeliveries = statistics.SuccessfulDeliveries;
        EscortKills = statistics.TotalEscortKills;
        ContributionScore = statistics.TotalContributionScore.ToString("F0");
        AverageDuration = statistics.AverageRaidDuration.ToString(@"mm\:ss");

        // Update relic statuses
        RelicStatuses.Clear();
        foreach (var relic in RelicDatabase.Relics)
        {
            var status = statuses.GetValueOrDefault(relic.Name, RelicStatus.Home);
            RelicStatuses.Add(new RelicStatusViewModel(relic, status));
        }

        // Update raid sessions
        RaidSessions.Clear();
        foreach (var session in sessions.OrderByDescending(s => s.StartTime))
        {
            RaidSessions.Add(new RelicRaidViewModel(session));
        }

        // Update carrier stats
        CarrierStats.Clear();
        foreach (var carrier in carrierStats.Values.OrderByDescending(c => c.RelicsCarried))
        {
            CarrierStats.Add(new CarrierStatsViewModel(carrier));
        }

        // Update charts
        GenerateOutcomesChart(statistics);
        GenerateRaidsByRelicChart(statistics);
    }

    private void GenerateOutcomesChart(RelicRaidStatistics statistics)
    {
        var outcomes = new List<(string Name, int Count, string Color)>
        {
            ("Successful", statistics.SuccessfulRaids, "#4CAF50"),
            ("Failed", statistics.FailedRaids, "#F44336"),
        };

        OutcomesSeries = outcomes
            .Where(o => o.Count > 0)
            .Select(o => new PieSeries<int>
            {
                Values = new[] { o.Count },
                Name = o.Name,
                Fill = new SolidColorPaint(SKColor.Parse(o.Color))
            } as ISeries).ToArray();
    }

    private void GenerateRaidsByRelicChart(RelicRaidStatistics statistics)
    {
        if (statistics.RaidsByRelic.Count == 0)
        {
            RaidsByRelicSeries = Array.Empty<ISeries>();
            RaidsByRelicXAxes = Array.Empty<Axis>();
            return;
        }

        var relics = statistics.RaidsByRelic
            .OrderByDescending(kvp => kvp.Value)
            .ToList();

        var counts = relics.Select(r => r.Value).ToArray();
        var labels = relics.Select(r =>
            r.Key.Length > 15 ? r.Key[..15] : r.Key).ToArray();

        RaidsByRelicSeries = new ISeries[]
        {
            new ColumnSeries<int>
            {
                Values = counts,
                Name = "Raids",
                Fill = new SolidColorPaint(new SKColor(156, 39, 176))
            }
        };

        RaidsByRelicXAxes = new Axis[]
        {
            new Axis
            {
                Labels = labels,
                LabelsRotation = 45
            }
        };
    }

    [RelayCommand]
    private void Reset()
    {
        HasData = false;
        TotalRaids = 0;
        SuccessfulRaids = 0;
        FailedRaids = 0;
        TimesAsCarrier = 0;
        SuccessfulDeliveries = 0;
        EscortKills = 0;
        ContributionScore = "0";
        AverageDuration = "00:00";
        RelicStatuses.Clear();
        RaidSessions.Clear();
        CarrierStats.Clear();
        OutcomesSeries = Array.Empty<ISeries>();
        RaidsByRelicSeries = Array.Empty<ISeries>();
        StatusMessage = "Load a combat log to analyze relic events";
    }
}

public class RelicStatusViewModel
{
    public string Name { get; }
    public string Type { get; }
    public string HomeRealm { get; }
    public string Status { get; }
    public string StatusColor { get; }
    public string RealmColor { get; }

    public RelicStatusViewModel(RelicInfo relic, RelicStatus status)
    {
        Name = relic.Name;
        Type = relic.Type == RelicType.Strength ? "Strength" : "Power";
        HomeRealm = relic.HomeRealm.GetDisplayName();
        Status = status switch
        {
            RelicStatus.Home => "Home",
            RelicStatus.Captured => "Captured",
            RelicStatus.InTransit => "In Transit",
            _ => "Unknown"
        };

        StatusColor = status switch
        {
            RelicStatus.Home => "#4CAF50",
            RelicStatus.Captured => "#F44336",
            RelicStatus.InTransit => "#FF9800",
            _ => "#9E9E9E"
        };

        RealmColor = relic.HomeRealm switch
        {
            Realm.Albion => "#E53935",
            Realm.Midgard => "#1E88E5",
            Realm.Hibernia => "#43A047",
            _ => "#9E9E9E"
        };
    }
}

public class RelicRaidViewModel
{
    public string RelicName { get; }
    public string RelicType { get; }
    public string StartTime { get; }
    public string Duration { get; }
    public string Outcome { get; }
    public string OutcomeColor { get; }
    public string Carriers { get; }
    public string Role { get; }

    public RelicRaidViewModel(RelicRaidSession session)
    {
        RelicName = session.RelicName;
        RelicType = session.RelicType == CamelotCombatReporter.Core.RvR.Models.RelicType.Strength ? "Strength" : "Power";
        StartTime = session.StartTime.ToString("HH:mm:ss");
        Duration = session.Duration.ToString(@"mm\:ss");
        Role = session.PlayerWasCarrier ? "Carrier" : "Escort";
        Carriers = string.Join(", ", session.Carriers.Take(3));

        Outcome = session.Outcome switch
        {
            RelicRaidOutcome.Captured => "Captured",
            RelicRaidOutcome.Returned => "Returned",
            RelicRaidOutcome.CarrierKilled => "Failed",
            _ => "Unknown"
        };

        OutcomeColor = session.Outcome switch
        {
            RelicRaidOutcome.Captured => "#4CAF50",
            RelicRaidOutcome.Returned => "#2196F3",
            RelicRaidOutcome.CarrierKilled => "#F44336",
            _ => "#9E9E9E"
        };
    }
}

public class CarrierStatsViewModel
{
    public string Name { get; }
    public int RelicsCarried { get; }
    public int Delivered { get; }
    public int Dropped { get; }
    public string TotalCarryTime { get; }
    public string SuccessRate { get; }

    public CarrierStatsViewModel(CarrierStatistics stats)
    {
        Name = stats.CarrierName;
        RelicsCarried = stats.RelicsCarried;
        Delivered = stats.SuccessfulDeliveries;
        Dropped = stats.DropsFromDeath;
        TotalCarryTime = stats.TotalCarryTime.ToString(@"mm\:ss");
        SuccessRate = stats.RelicsCarried > 0
            ? $"{(double)stats.SuccessfulDeliveries / stats.RelicsCarried * 100:F0}%"
            : "N/A";
    }
}
