using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using CamelotCombatReporter.Core.Models;
using CamelotCombatReporter.Core.Parsing;
using CamelotCombatReporter.Core.RealmAbilities;
using CamelotCombatReporter.Core.RealmAbilities.Models;
using CamelotCombatReporter.Gui.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;

namespace CamelotCombatReporter.Gui.RealmAbilities.ViewModels;

/// <summary>
/// ViewModel for the Realm Abilities view.
/// </summary>
public partial class RealmAbilityViewModel : ViewModelBase
{
    private readonly IRealmAbilityService _raService;

    #region Statistics Properties

    [ObservableProperty]
    private int _totalActivations;

    [ObservableProperty]
    private int _uniqueAbilitiesUsed;

    [ObservableProperty]
    private string _mostUsedAbility = "N/A";

    [ObservableProperty]
    private string _highestDamageAbility = "N/A";

    [ObservableProperty]
    private string _overallCooldownEfficiency = "0%";

    [ObservableProperty]
    private string _sessionDuration = "0:00:00";

    [ObservableProperty]
    private bool _hasData;

    #endregion

    #region Activations Collection

    [ObservableProperty]
    private ObservableCollection<RealmAbilityActivationViewModel> _activations = new();

    [ObservableProperty]
    private RealmAbilityActivationViewModel? _selectedActivation;

    #endregion

    #region Usage Stats Collection

    [ObservableProperty]
    private ObservableCollection<RealmAbilityUsageStatsViewModel> _usageStats = new();

    #endregion

    #region Chart Properties

    [ObservableProperty]
    private ISeries[] _typeDistributionSeries = Array.Empty<ISeries>();

    [ObservableProperty]
    private ISeries[] _timelineSeries = Array.Empty<ISeries>();

    [ObservableProperty]
    private Axis[] _timelineXAxes = Array.Empty<Axis>();

    [ObservableProperty]
    private Axis[] _timelineYAxes = Array.Empty<Axis>();

    #endregion

    #region Cooldown States

    [ObservableProperty]
    private ObservableCollection<CooldownStateViewModel> _cooldownStates = new();

    #endregion

    #region Filter Properties

    [ObservableProperty]
    private string _selectedType = "All";

    public string[] TypeOptions { get; } = new[]
    {
        "All", "Damage", "Crowd Control", "Defensive", "Healing", "Utility", "Passive"
    };

    [ObservableProperty]
    private string _selectedRealm = "All";

    public string[] RealmOptions { get; } = new[]
    {
        "All", "Albion", "Midgard", "Hibernia", "Universal"
    };

    #endregion

    public RealmAbilityViewModel()
    {
        var database = new RealmAbilityDatabase();
        _raService = new RealmAbilityService(database);
    }

    public RealmAbilityViewModel(IRealmAbilityService raService)
    {
        _raService = raService;
    }

    /// <summary>
    /// Analyzes realm abilities from a log file.
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
    /// Analyzes realm abilities from a log file path.
    /// </summary>
    public async Task AnalyzeLogFile(string filePath)
    {
        await Task.Run(() =>
        {
            var parser = new LogParser(filePath);
            var events = parser.Parse().ToList();

            var activations = _raService.ExtractActivations(events);

            // Calculate session duration from events
            TimeSpan duration = TimeSpan.Zero;
            if (events.Count > 1)
            {
                var first = events.First().Timestamp;
                var last = events.Last().Timestamp;
                duration = last.ToTimeSpan() - first.ToTimeSpan();
                if (duration < TimeSpan.Zero)
                    duration = TimeSpan.FromHours(24) + duration; // Handle day rollover
            }

            var statistics = _raService.CalculateStatistics(activations, duration);

            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                UpdateUI(activations, statistics);
            });
        });
    }

    /// <summary>
    /// Analyzes realm abilities from existing events.
    /// </summary>
    public void AnalyzeEvents(System.Collections.Generic.IEnumerable<LogEvent> events)
    {
        var eventsList = events.ToList();
        var activations = _raService.ExtractActivations(eventsList);

        TimeSpan duration = TimeSpan.Zero;
        if (eventsList.Count > 1)
        {
            var first = eventsList.First().Timestamp;
            var last = eventsList.Last().Timestamp;
            duration = last.ToTimeSpan() - first.ToTimeSpan();
            if (duration < TimeSpan.Zero)
                duration = TimeSpan.FromHours(24) + duration;
        }

        var statistics = _raService.CalculateStatistics(activations, duration);
        UpdateUI(activations, statistics);
    }

    private void UpdateUI(
        System.Collections.Generic.IReadOnlyList<RealmAbilityActivation> activations,
        RealmAbilitySessionStats statistics)
    {
        // Clear existing data
        Activations.Clear();
        UsageStats.Clear();
        CooldownStates.Clear();

        // Update statistics
        TotalActivations = statistics.TotalActivations;
        UniqueAbilitiesUsed = statistics.TotalRAsUsed;
        SessionDuration = statistics.SessionDuration.ToString(@"h\:mm\:ss");
        OverallCooldownEfficiency = $"{statistics.OverallCooldownEfficiency:F0}%";
        HasData = activations.Count > 0;

        // Update most used ability
        if (statistics.MostUsedAbility != null)
        {
            MostUsedAbility = $"{statistics.MostUsedAbility.Ability.Name} ({statistics.MostUsedAbility.TotalActivations}x)";
        }
        else
        {
            MostUsedAbility = "N/A";
        }

        // Update highest damage ability
        if (statistics.HighestDamageAbility != null)
        {
            HighestDamageAbility = $"{statistics.HighestDamageAbility.Ability.Name} ({statistics.HighestDamageAbility.TotalDamage:N0} dmg)";
        }
        else
        {
            HighestDamageAbility = "N/A";
        }

        // Add activations
        foreach (var activation in activations.OrderByDescending(a => a.Timestamp))
        {
            Activations.Add(new RealmAbilityActivationViewModel(activation));
        }

        // Add usage stats
        foreach (var stat in statistics.PerAbilityStats.OrderByDescending(s => s.TotalActivations))
        {
            UsageStats.Add(new RealmAbilityUsageStatsViewModel(stat));
        }

        // Update cooldown states
        if (activations.Any())
        {
            var currentTime = activations.Max(a => a.Timestamp);
            var cooldowns = _raService.GetCooldownStates(activations, currentTime);
            foreach (var cd in cooldowns.OrderBy(c => c.IsReady ? 0 : 1).ThenBy(c => c.AbilityName))
            {
                CooldownStates.Add(new CooldownStateViewModel(cd));
            }
        }

        // Update charts
        UpdateTypeDistributionChart(statistics);
        UpdateTimelineChart(activations);
    }

    private void UpdateTypeDistributionChart(RealmAbilitySessionStats statistics)
    {
        if (!statistics.UsageByType.Any())
        {
            TypeDistributionSeries = Array.Empty<ISeries>();
            return;
        }

        var colors = new Dictionary<RealmAbilityType, SKColor>
        {
            { RealmAbilityType.Damage, new SKColor(244, 67, 54) },      // Red
            { RealmAbilityType.CrowdControl, new SKColor(156, 39, 176) }, // Purple
            { RealmAbilityType.Defensive, new SKColor(33, 150, 243) },   // Blue
            { RealmAbilityType.Healing, new SKColor(76, 175, 80) },     // Green
            { RealmAbilityType.Utility, new SKColor(255, 152, 0) },      // Orange
            { RealmAbilityType.Passive, new SKColor(158, 158, 158) }     // Gray
        };

        var series = statistics.UsageByType
            .Where(kvp => kvp.Value > 0)
            .Select(kvp => new PieSeries<int>
            {
                Values = new[] { kvp.Value },
                Name = kvp.Key.ToString(),
                Fill = colors.TryGetValue(kvp.Key, out var color)
                    ? new SolidColorPaint(color)
                    : new SolidColorPaint(SKColors.Gray)
            })
            .Cast<ISeries>()
            .ToArray();

        TypeDistributionSeries = series;
    }

    private void UpdateTimelineChart(System.Collections.Generic.IReadOnlyList<RealmAbilityActivation> activations)
    {
        if (!activations.Any())
        {
            TimelineSeries = Array.Empty<ISeries>();
            return;
        }

        var timeline = _raService.BuildTimeline(activations);

        // Group by minute for cleaner visualization
        var byMinute = timeline
            .GroupBy(t => t.Timestamp.Hour * 60 + t.Timestamp.Minute)
            .OrderBy(g => g.Key)
            .Select(g => new { Minute = g.Key, Count = g.Count() })
            .ToList();

        if (!byMinute.Any())
        {
            TimelineSeries = Array.Empty<ISeries>();
            return;
        }

        TimelineSeries = new ISeries[]
        {
            new ColumnSeries<int>
            {
                Values = byMinute.Select(b => b.Count).ToArray(),
                Name = "RA Activations",
                Fill = new SolidColorPaint(new SKColor(156, 39, 176))
            }
        };

        var startMinute = byMinute.First().Minute;
        TimelineXAxes = new Axis[]
        {
            new Axis
            {
                Name = "Time (minutes from start)",
                Labels = byMinute.Select(b => $"+{b.Minute - startMinute}m").ToArray()
            }
        };

        TimelineYAxes = new Axis[]
        {
            new Axis
            {
                Name = "Activations",
                MinLimit = 0
            }
        };
    }
}

/// <summary>
/// ViewModel for a single realm ability activation.
/// </summary>
public class RealmAbilityActivationViewModel
{
    public RealmAbilityActivationViewModel(RealmAbilityActivation activation)
    {
        Activation = activation;
        Timestamp = activation.Timestamp.ToString("HH:mm:ss");
        AbilityName = activation.Ability.Name;
        Type = activation.Ability.Type.ToString();
        TypeColor = RealmAbilityTimelineEntry.GetColorForType(activation.Ability.Type);
        Level = activation.Level;
        SourceName = activation.SourceName;

        // Summarize associated events
        var totalDamage = activation.AssociatedEvents
            .OfType<DamageEvent>()
            .Sum(d => d.DamageAmount);
        var totalHealing = activation.AssociatedEvents
            .OfType<HealingEvent>()
            .Sum(h => h.HealingAmount);

        if (totalDamage > 0)
            EffectSummary = $"{totalDamage:N0} damage";
        else if (totalHealing > 0)
            EffectSummary = $"{totalHealing:N0} healing";
        else
            EffectSummary = "Activated";
    }

    public RealmAbilityActivation Activation { get; }
    public string Timestamp { get; }
    public string AbilityName { get; }
    public string Type { get; }
    public string TypeColor { get; }
    public int Level { get; }
    public string SourceName { get; }
    public string EffectSummary { get; }
}

/// <summary>
/// ViewModel for realm ability usage statistics.
/// </summary>
public class RealmAbilityUsageStatsViewModel
{
    public RealmAbilityUsageStatsViewModel(RealmAbilityUsageStats stats)
    {
        Stats = stats;
        AbilityName = stats.Ability.Name;
        Type = stats.Ability.Type.ToString();
        TypeColor = RealmAbilityTimelineEntry.GetColorForType(stats.Ability.Type);
        TotalActivations = stats.TotalActivations;
        TotalDamage = stats.TotalDamage > 0 ? $"{stats.TotalDamage:N0}" : "-";
        TotalHealing = stats.TotalHealing > 0 ? $"{stats.TotalHealing:N0}" : "-";
        AverageEffectiveness = $"{stats.AverageEffectiveness:F1}";
        CooldownEfficiency = $"{stats.CooldownEfficiency:F0}%";
    }

    public RealmAbilityUsageStats Stats { get; }
    public string AbilityName { get; }
    public string Type { get; }
    public string TypeColor { get; }
    public int TotalActivations { get; }
    public string TotalDamage { get; }
    public string TotalHealing { get; }
    public string AverageEffectiveness { get; }
    public string CooldownEfficiency { get; }
}

/// <summary>
/// ViewModel for cooldown state display.
/// </summary>
public class CooldownStateViewModel
{
    public CooldownStateViewModel(CooldownState state)
    {
        State = state;
        AbilityName = state.AbilityName;
        IsReady = state.IsReady;
        StatusText = state.IsReady ? "Ready" : $"On CD until {state.CooldownEnds:HH:mm:ss}";
        StatusColor = state.IsReady ? "#4CAF50" : "#F44336";
    }

    public CooldownState State { get; }
    public string AbilityName { get; }
    public bool IsReady { get; }
    public string StatusText { get; }
    public string StatusColor { get; }
}
