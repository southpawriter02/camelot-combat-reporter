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

public partial class SiegeTrackingViewModel : ViewModelBase
{
    private readonly ISiegeTrackingService _siegeService;

    #region Observable Properties

    [ObservableProperty]
    private bool _hasData;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _statusMessage = "Load a combat log to analyze siege events";

    // Statistics
    [ObservableProperty]
    private int _totalSieges;

    [ObservableProperty]
    private int _attackVictories;

    [ObservableProperty]
    private int _defenseVictories;

    [ObservableProperty]
    private int _structureDamage;

    [ObservableProperty]
    private int _playerKills;

    [ObservableProperty]
    private int _deaths;

    [ObservableProperty]
    private int _guardKills;

    [ObservableProperty]
    private string _contributionScore = "0";

    [ObservableProperty]
    private string _averageDuration = "00:00";

    // Sessions list
    [ObservableProperty]
    private ObservableCollection<SiegeSessionViewModel> _siegeSessions = new();

    [ObservableProperty]
    private SiegeSessionViewModel? _selectedSession;

    // Timeline for selected session
    [ObservableProperty]
    private ObservableCollection<SiegeTimelineEntryViewModel> _timelineEntries = new();

    // Charts
    [ObservableProperty]
    private ISeries[] _outcomesSeries = Array.Empty<ISeries>();

    [ObservableProperty]
    private ISeries[] _siegesByKeepSeries = Array.Empty<ISeries>();

    [ObservableProperty]
    private Axis[] _siegesByKeepXAxes = Array.Empty<Axis>();

    [ObservableProperty]
    private Axis[] _siegesByKeepYAxes = new Axis[]
    {
        new Axis { Name = "Sieges", MinLimit = 0 }
    };

    #endregion

    public SiegeTrackingViewModel()
    {
        _siegeService = new SiegeTrackingService();
    }

    public SiegeTrackingViewModel(ISiegeTrackingService siegeService)
    {
        _siegeService = siegeService;
    }

    partial void OnSelectedSessionChanged(SiegeSessionViewModel? value)
    {
        UpdateTimeline(value);
    }

    private void UpdateTimeline(SiegeSessionViewModel? session)
    {
        TimelineEntries.Clear();
        if (session == null)
            return;

        var timeline = _siegeService.BuildTimeline(session.Session);
        foreach (var entry in timeline)
        {
            TimelineEntries.Add(new SiegeTimelineEntryViewModel(entry));
        }
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
        StatusMessage = "Analyzing combat log for siege events...";

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

                var sessions = _siegeService.ResolveSessions(events);
                var statistics = _siegeService.CalculateStatistics(sessions);

                Dispatcher.UIThread.Post(() =>
                {
                    UpdateUI(sessions, statistics);
                    HasData = sessions.Count > 0;
                    IsLoading = false;
                    StatusMessage = sessions.Count > 0
                        ? $"Found {sessions.Count} siege sessions"
                        : "No siege events found in log";
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
        var sessions = _siegeService.ResolveSessions(eventList);
        var statistics = _siegeService.CalculateStatistics(sessions);
        UpdateUI(sessions, statistics);
        HasData = sessions.Count > 0;
    }

    private void UpdateUI(IReadOnlyList<SiegeSession> sessions, SiegeStatistics statistics)
    {
        // Update statistics
        TotalSieges = statistics.TotalSiegesParticipated;
        AttackVictories = statistics.AttackVictories;
        DefenseVictories = statistics.DefenseVictories;
        StructureDamage = statistics.TotalStructureDamage;
        PlayerKills = statistics.TotalPlayerKills;
        Deaths = statistics.TotalDeaths;
        GuardKills = statistics.TotalGuardKills;
        ContributionScore = statistics.TotalContributionScore.ToString("F0");
        AverageDuration = statistics.AverageSiegeDuration.ToString(@"mm\:ss");

        // Update sessions list
        SiegeSessions.Clear();
        foreach (var session in sessions.OrderByDescending(s => s.StartTime))
        {
            SiegeSessions.Add(new SiegeSessionViewModel(session));
        }

        // Update charts
        GenerateOutcomesChart(statistics);
        GenerateSiegesByKeepChart(statistics);
    }

    private void GenerateOutcomesChart(SiegeStatistics statistics)
    {
        var outcomes = new List<(string Name, int Count, string Color)>
        {
            ("Attack Victories", statistics.AttackVictories, "#4CAF50"),
            ("Defense Victories", statistics.DefenseVictories, "#2196F3"),
            ("Other", statistics.TotalSiegesParticipated - statistics.AttackVictories - statistics.DefenseVictories, "#9E9E9E")
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

    private void GenerateSiegesByKeepChart(SiegeStatistics statistics)
    {
        if (statistics.SiegesByKeep.Count == 0)
        {
            SiegesByKeepSeries = Array.Empty<ISeries>();
            SiegesByKeepXAxes = Array.Empty<Axis>();
            return;
        }

        var topKeeps = statistics.SiegesByKeep
            .OrderByDescending(kvp => kvp.Value)
            .Take(8)
            .ToList();

        var counts = topKeeps.Select(k => k.Value).ToArray();
        var labels = topKeeps.Select(k =>
            k.Key.Length > 12 ? k.Key[..12] : k.Key).ToArray();

        SiegesByKeepSeries = new ISeries[]
        {
            new ColumnSeries<int>
            {
                Values = counts,
                Name = "Sieges",
                Fill = new SolidColorPaint(new SKColor(33, 150, 243))
            }
        };

        SiegesByKeepXAxes = new Axis[]
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
        TotalSieges = 0;
        AttackVictories = 0;
        DefenseVictories = 0;
        StructureDamage = 0;
        PlayerKills = 0;
        Deaths = 0;
        GuardKills = 0;
        ContributionScore = "0";
        AverageDuration = "00:00";
        SiegeSessions.Clear();
        TimelineEntries.Clear();
        OutcomesSeries = Array.Empty<ISeries>();
        SiegesByKeepSeries = Array.Empty<ISeries>();
        StatusMessage = "Load a combat log to analyze siege events";
    }
}

public class SiegeSessionViewModel
{
    public SiegeSession Session { get; }
    public string KeepName { get; }
    public string KeepType { get; }
    public string StartTime { get; }
    public string Duration { get; }
    public string Outcome { get; }
    public string OutcomeColor { get; }
    public string Phase { get; }
    public string Role { get; }
    public string ContributionScore { get; }

    public SiegeSessionViewModel(SiegeSession session)
    {
        Session = session;
        KeepName = session.KeepName;
        KeepType = session.KeepType.GetDisplayName();
        StartTime = session.StartTime.ToString("HH:mm:ss");
        Duration = session.Duration.ToString(@"mm\:ss");
        Outcome = session.Outcome.GetDisplayName();
        Phase = session.FinalPhase.GetDisplayName();
        Role = session.PlayerWasAttacker ? "Attacker" : "Defender";
        ContributionScore = session.PlayerContribution.ContributionScore.ToString("F0");

        OutcomeColor = session.Outcome switch
        {
            SiegeOutcome.AttackSuccess when session.PlayerWasAttacker => "#4CAF50",
            SiegeOutcome.DefenseSuccess when !session.PlayerWasAttacker => "#4CAF50",
            SiegeOutcome.AttackSuccess => "#F44336",
            SiegeOutcome.DefenseSuccess => "#F44336",
            _ => "#9E9E9E"
        };
    }
}

public class SiegeTimelineEntryViewModel
{
    public string Timestamp { get; }
    public string EventType { get; }
    public string Description { get; }
    public string Phase { get; }
    public string PhaseColor { get; }
    public bool IsPlayerAction { get; }
    public string Icon { get; }

    public SiegeTimelineEntryViewModel(SiegeTimelineEntry entry)
    {
        Timestamp = entry.Timestamp.ToString("HH:mm:ss");
        EventType = entry.EventType;
        Description = entry.Description;
        Phase = entry.Phase.GetDisplayName();
        IsPlayerAction = entry.IsPlayerAction;

        PhaseColor = entry.Phase switch
        {
            SiegePhase.Approach => "#9E9E9E",
            SiegePhase.OuterSiege => "#FF9800",
            SiegePhase.InnerSiege => "#F44336",
            SiegePhase.LordFight => "#9C27B0",
            SiegePhase.Capture => "#4CAF50",
            _ => "#9E9E9E"
        };

        Icon = entry.EventType switch
        {
            "Door Destroyed" => "X",
            "Door Damage" => "D",
            "Lord Killed" => "L",
            "Guard Killed" => "G",
            "Keep Captured" => "C",
            "Siege Deployed" => "S",
            "Siege Destroyed" => "-",
            _ => "?"
        };
    }
}
