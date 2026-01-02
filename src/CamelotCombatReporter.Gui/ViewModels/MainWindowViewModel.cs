using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using CamelotCombatReporter.Core.Exporting;
using CamelotCombatReporter.Core.Models;
using CamelotCombatReporter.Core.Parsing;
using CamelotCombatReporter.Gui.Plugins.ViewModels;
using CamelotCombatReporter.Gui.Plugins.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;

namespace CamelotCombatReporter.Gui.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    #region File Selection Properties

    [ObservableProperty]
    private string _selectedLogFile = "No file selected";

    [ObservableProperty]
    private string _combatantName = "You";

    [ObservableProperty]
    private bool _hasAnalyzedData = false;

    #endregion

    #region Event Type Toggles

    [ObservableProperty]
    private bool _showDamageDealt = true;

    [ObservableProperty]
    private bool _showDamageTaken = true;

    [ObservableProperty]
    private bool _showHealingDone = true;

    [ObservableProperty]
    private bool _showHealingReceived = true;

    [ObservableProperty]
    private bool _showCombatStyles = true;

    [ObservableProperty]
    private bool _showSpells = true;

    #endregion

    #region Damage Type Filter

    [ObservableProperty]
    private ObservableCollection<string> _availableDamageTypes = new() { "All" };

    [ObservableProperty]
    private string _selectedDamageType = "All";

    #endregion

    #region Target Filter

    [ObservableProperty]
    private ObservableCollection<string> _availableTargets = new() { "All" };

    [ObservableProperty]
    private string _selectedTarget = "All";

    #endregion

    #region Time Range Filter

    [ObservableProperty]
    private TimeSpan _timeRangeStart = TimeSpan.Zero;

    [ObservableProperty]
    private TimeSpan _timeRangeEnd = TimeSpan.FromHours(24);

    [ObservableProperty]
    private TimeSpan _logStartTime = TimeSpan.Zero;

    [ObservableProperty]
    private TimeSpan _logEndTime = TimeSpan.FromHours(24);

    [ObservableProperty]
    private string _timeRangeStartText = "00:00:00";

    [ObservableProperty]
    private string _timeRangeEndText = "23:59:59";

    #endregion

    #region Statistics Visibility Toggles

    [ObservableProperty]
    private bool _showDurationStat = true;

    [ObservableProperty]
    private bool _showTotalDamageStat = true;

    [ObservableProperty]
    private bool _showDpsStat = true;

    [ObservableProperty]
    private bool _showAverageDamageStat = true;

    [ObservableProperty]
    private bool _showMedianDamageStat = true;

    [ObservableProperty]
    private bool _showCombatStylesStat = true;

    [ObservableProperty]
    private bool _showSpellsCastStat = true;

    [ObservableProperty]
    private bool _showHealingStats = true;

    [ObservableProperty]
    private bool _showDamageTakenStats = true;

    #endregion

    #region Damage Statistics

    [ObservableProperty]
    private string _logDuration = "0.00";

    [ObservableProperty]
    private int _totalDamageDealt = 0;

    [ObservableProperty]
    private string _damagePerSecond = "0.00";

    [ObservableProperty]
    private string _averageDamage = "0.00";

    [ObservableProperty]
    private string _medianDamage = "0.00";

    [ObservableProperty]
    private int _combatStylesUsed = 0;

    [ObservableProperty]
    private int _spellsCast = 0;

    #endregion

    #region Healing Statistics

    [ObservableProperty]
    private int _totalHealingDone = 0;

    [ObservableProperty]
    private string _healingPerSecond = "0.00";

    [ObservableProperty]
    private string _averageHealing = "0.00";

    [ObservableProperty]
    private string _medianHealing = "0.00";

    [ObservableProperty]
    private int _totalHealingReceived = 0;

    [ObservableProperty]
    private string _healingReceivedPerSecond = "0.00";

    #endregion

    #region Damage Taken Statistics

    [ObservableProperty]
    private int _totalDamageTaken = 0;

    [ObservableProperty]
    private string _damageTakenPerSecond = "0.00";

    [ObservableProperty]
    private string _averageDamageTaken = "0.00";

    [ObservableProperty]
    private string _medianDamageTaken = "0.00";

    #endregion

    #region Chart Options

    [ObservableProperty]
    private ObservableCollection<string> _chartTypes = new() { "Line", "Bar", "Area" };

    [ObservableProperty]
    private string _selectedChartType = "Line";

    [ObservableProperty]
    private ObservableCollection<string> _chartIntervals = new() { "1s", "5s", "10s", "30s", "1m" };

    [ObservableProperty]
    private string _selectedChartInterval = "5s";

    [ObservableProperty]
    private bool _showDamageDealtOnChart = true;

    [ObservableProperty]
    private bool _showDamageTakenOnChart = false;

    [ObservableProperty]
    private bool _showHealingOnChart = false;

    [ObservableProperty]
    private bool _showDpsTrendLine = false;

    #endregion

    #region Chart Data

    [ObservableProperty]
    private ISeries[] _series = Array.Empty<ISeries>();

    [ObservableProperty]
    private Axis[] _xAxes = Array.Empty<Axis>();

    [ObservableProperty]
    private Axis[] _yAxes = new Axis[] { new Axis { Name = "Amount" } };

    #endregion

    #region Pie Chart Data (Damage By Target, Damage Type Distribution)

    [ObservableProperty]
    private ISeries[] _damageByTargetSeries = Array.Empty<ISeries>();

    [ObservableProperty]
    private ISeries[] _damageTypeSeries = Array.Empty<ISeries>();

    #endregion

    #region Detailed Lists

    [ObservableProperty]
    private ObservableCollection<CombatStyleDetail> _combatStyleDetails = new();

    [ObservableProperty]
    private ObservableCollection<SpellCastDetail> _spellCastDetails = new();

    #endregion

    #region Event Table

    [ObservableProperty]
    private ObservableCollection<EventTableRow> _eventTableRows = new();

    [ObservableProperty]
    private string _eventTableFilter = "";

    #endregion

    #region Quick Stats Summary

    [ObservableProperty]
    private string _quickStatsSummary = "";

    #endregion

    #region Comparison Mode

    [ObservableProperty]
    private bool _isComparisonMode = false;

    [ObservableProperty]
    private string _comparisonLogFile = "No file selected";

    [ObservableProperty]
    private string _comparisonSummary = "";

    #endregion

    #region Private Fields

    private List<LogEvent>? _analyzedEvents;
    private List<LogEvent>? _comparisonEvents;
    private CombatStatistics? _currentStatistics;
    private TimeOnly _firstEventTime;
    private TimeOnly _lastEventTime;

    private static readonly string PreferencesPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "CamelotCombatReporter",
        "preferences.json");

    #endregion

    public MainWindowViewModel()
    {
        LoadPreferences();
    }

    #region Commands

    [RelayCommand]
    private async Task SelectLogFile()
    {
        var topLevel = GetMainWindow();
        if (topLevel == null) return;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select Combat Log File",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("Log Files") { Patterns = new[] { "*.log", "*.txt" } },
                new FilePickerFileType("All Files") { Patterns = new[] { "*" } }
            }
        });

        if (files.Count > 0)
        {
            SelectedLogFile = files[0].Path.LocalPath;
            SavePreferences();
        }
    }

    [RelayCommand]
    private async Task SelectComparisonLogFile()
    {
        var topLevel = GetMainWindow();
        if (topLevel == null) return;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select Comparison Log File",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("Log Files") { Patterns = new[] { "*.log", "*.txt" } },
                new FilePickerFileType("All Files") { Patterns = new[] { "*" } }
            }
        });

        if (files.Count > 0)
        {
            ComparisonLogFile = files[0].Path.LocalPath;
        }
    }

    [RelayCommand]
    private async Task AnalyzeLog()
    {
        if (string.IsNullOrEmpty(SelectedLogFile) || SelectedLogFile == "No file selected")
            return;

        await Task.Run(() =>
        {
            var logParser = new LogParser(SelectedLogFile);
            var events = logParser.Parse().ToList();

            if (events.Count == 0)
            {
                HasAnalyzedData = false;
                return;
            }

            _analyzedEvents = events;
            _firstEventTime = events.First().Timestamp;
            _lastEventTime = events.Last().Timestamp;

            // Set time range to full log duration
            LogStartTime = _firstEventTime.ToTimeSpan();
            LogEndTime = _lastEventTime.ToTimeSpan();
            TimeRangeStart = LogStartTime;
            TimeRangeEnd = LogEndTime;
            TimeRangeStartText = _firstEventTime.ToString("HH:mm:ss");
            TimeRangeEndText = _lastEventTime.ToString("HH:mm:ss");

            // Populate filters
            PopulateFilters(events);

            // Parse comparison file if in comparison mode
            if (IsComparisonMode && !string.IsNullOrEmpty(ComparisonLogFile) && ComparisonLogFile != "No file selected")
            {
                var compParser = new LogParser(ComparisonLogFile);
                _comparisonEvents = compParser.Parse().ToList();
            }

            // Analyze and update UI
            RefreshAnalysis();

            HasAnalyzedData = true;
        });

        SavePreferences();
    }

    [RelayCommand]
    private void ApplyFilters()
    {
        if (_analyzedEvents == null) return;

        // Parse time range
        if (TimeSpan.TryParse(TimeRangeStartText, out var start))
            TimeRangeStart = start;
        if (TimeSpan.TryParse(TimeRangeEndText, out var end))
            TimeRangeEnd = end;

        RefreshAnalysis();
    }

    [RelayCommand]
    private void ResetFilters()
    {
        SelectedDamageType = "All";
        SelectedTarget = "All";
        TimeRangeStart = LogStartTime;
        TimeRangeEnd = LogEndTime;
        TimeRangeStartText = TimeOnly.FromTimeSpan(LogStartTime).ToString("HH:mm:ss");
        TimeRangeEndText = TimeOnly.FromTimeSpan(LogEndTime).ToString("HH:mm:ss");
        ShowDamageDealt = true;
        ShowDamageTaken = true;
        ShowHealingDone = true;
        ShowHealingReceived = true;
        ShowCombatStyles = true;
        ShowSpells = true;

        RefreshAnalysis();
    }

    [RelayCommand]
    private void SetTimeRangePreset(string preset)
    {
        var duration = LogEndTime - LogStartTime;
        switch (preset)
        {
            case "first5m":
                TimeRangeStart = LogStartTime;
                TimeRangeEnd = LogStartTime + TimeSpan.FromMinutes(Math.Min(5, duration.TotalMinutes));
                break;
            case "last5m":
                TimeRangeStart = LogEndTime - TimeSpan.FromMinutes(Math.Min(5, duration.TotalMinutes));
                TimeRangeEnd = LogEndTime;
                break;
            case "first10m":
                TimeRangeStart = LogStartTime;
                TimeRangeEnd = LogStartTime + TimeSpan.FromMinutes(Math.Min(10, duration.TotalMinutes));
                break;
            case "last10m":
                TimeRangeStart = LogEndTime - TimeSpan.FromMinutes(Math.Min(10, duration.TotalMinutes));
                TimeRangeEnd = LogEndTime;
                break;
            case "all":
                TimeRangeStart = LogStartTime;
                TimeRangeEnd = LogEndTime;
                break;
        }
        TimeRangeStartText = TimeOnly.FromTimeSpan(TimeRangeStart).ToString("HH:mm:ss");
        TimeRangeEndText = TimeOnly.FromTimeSpan(TimeRangeEnd).ToString("HH:mm:ss");
        RefreshAnalysis();
    }

    [RelayCommand]
    private async Task ExportCsv()
    {
        if (_analyzedEvents == null || _currentStatistics == null) return;

        var topLevel = GetMainWindow();
        if (topLevel == null) return;

        var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Export Analysis to CSV",
            DefaultExtension = "csv",
            FileTypeChoices = new[] { new FilePickerFileType("CSV Files") { Patterns = new[] { "*.csv" } } }
        });

        if (file != null)
        {
            var exporter = new CsvExporter();
            var content = exporter.GenerateCsv(_currentStatistics, _analyzedEvents);
            await using var stream = await file.OpenWriteAsync();
            using var writer = new StreamWriter(stream);
            await writer.WriteAsync(content);
        }
    }

    [RelayCommand]
    private async Task ExportJson()
    {
        if (_analyzedEvents == null || _currentStatistics == null) return;

        var topLevel = GetMainWindow();
        if (topLevel == null) return;

        var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Export Analysis to JSON",
            DefaultExtension = "json",
            FileTypeChoices = new[] { new FilePickerFileType("JSON Files") { Patterns = new[] { "*.json" } } }
        });

        if (file != null)
        {
            var exportData = new
            {
                Statistics = _currentStatistics,
                Filters = new
                {
                    CombatantName,
                    SelectedDamageType,
                    SelectedTarget,
                    TimeRangeStart = TimeRangeStartText,
                    TimeRangeEnd = TimeRangeEndText
                },
                DamageByTarget = _eventTableRows
                    .Where(e => e.Type == "Damage" && e.Source == CombatantName)
                    .GroupBy(e => e.Target)
                    .Select(g => new { Target = g.Key, TotalDamage = g.Sum(e => int.TryParse(e.Amount, out var amt) ? amt : 0) }),
                DamageTypeBreakdown = _eventTableRows
                    .Where(e => e.Type == "Damage" && e.Source == CombatantName)
                    .GroupBy(e => e.Details)
                    .Select(g => new { DamageType = g.Key, TotalDamage = g.Sum(e => int.TryParse(e.Amount, out var amt) ? amt : 0) }),
                CombatStyles = CombatStyleDetails.ToList(),
                Spells = SpellCastDetails.ToList(),
                Events = _eventTableRows.ToList()
            };

            var json = JsonSerializer.Serialize(exportData, new JsonSerializerOptions { WriteIndented = true });
            await using var stream = await file.OpenWriteAsync();
            using var writer = new StreamWriter(stream);
            await writer.WriteAsync(json);
        }
    }

    [RelayCommand]
    private void UpdateChart()
    {
        if (_analyzedEvents == null) return;
        GenerateCharts();
    }

    [RelayCommand]
    private async Task ShowPluginManager()
    {
        var mainWindow = GetMainWindow();
        if (mainWindow == null) return;

        var pluginsDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "CamelotCombatReporter",
            "plugins");

        var viewModel = new PluginManagerViewModel(pluginsDirectory);
        var window = new PluginManagerWindow
        {
            DataContext = viewModel
        };

        await window.ShowDialog(mainWindow);
    }

    #endregion

    #region Private Methods

    private static Avalonia.Controls.Window? GetMainWindow()
    {
        return Avalonia.Application.Current?.ApplicationLifetime is
            Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
            ? desktop.MainWindow
            : null;
    }

    private void PopulateFilters(List<LogEvent> events)
    {
        // Populate damage types
        var damageTypes = events.OfType<DamageEvent>()
            .Select(e => e.DamageType)
            .Distinct()
            .OrderBy(t => t)
            .ToList();

        AvailableDamageTypes = new ObservableCollection<string>(new[] { "All" }.Concat(damageTypes));

        // Populate targets
        var targets = events.OfType<DamageEvent>()
            .SelectMany(e => new[] { e.Source, e.Target })
            .Concat(events.OfType<HealingEvent>().SelectMany(e => new[] { e.Source, e.Target }))
            .Concat(events.OfType<CombatStyleEvent>().Select(e => e.Target))
            .Concat(events.OfType<SpellCastEvent>().Select(e => e.Target))
            .Where(t => !string.IsNullOrEmpty(t))
            .Distinct()
            .OrderBy(t => t)
            .ToList();

        AvailableTargets = new ObservableCollection<string>(new[] { "All" }.Concat(targets));
    }

    private void RefreshAnalysis()
    {
        if (_analyzedEvents == null) return;

        var startTime = TimeOnly.FromTimeSpan(TimeRangeStart);
        var endTime = TimeOnly.FromTimeSpan(TimeRangeEnd);

        // Filter events by time range
        var filteredEvents = _analyzedEvents
            .Where(e => e.Timestamp >= startTime && e.Timestamp <= endTime)
            .ToList();

        // Calculate duration
        var duration = filteredEvents.Any()
            ? filteredEvents.Last().Timestamp - filteredEvents.First().Timestamp
            : TimeSpan.Zero;

        // Filter and calculate damage dealt
        var damageDealtEvents = filteredEvents.OfType<DamageEvent>()
            .Where(e => e.Source == CombatantName)
            .Where(e => SelectedDamageType == "All" || e.DamageType == SelectedDamageType)
            .Where(e => SelectedTarget == "All" || e.Target == SelectedTarget)
            .ToList();

        // Filter and calculate damage taken
        var damageTakenEvents = filteredEvents.OfType<DamageEvent>()
            .Where(e => e.Target == CombatantName)
            .Where(e => SelectedDamageType == "All" || e.DamageType == SelectedDamageType)
            .Where(e => SelectedTarget == "All" || e.Source == SelectedTarget)
            .ToList();

        // Filter and calculate healing done
        var healingDoneEvents = filteredEvents.OfType<HealingEvent>()
            .Where(e => e.Source == CombatantName)
            .Where(e => SelectedTarget == "All" || e.Target == SelectedTarget)
            .ToList();

        // Filter and calculate healing received
        var healingReceivedEvents = filteredEvents.OfType<HealingEvent>()
            .Where(e => e.Target == CombatantName)
            .Where(e => SelectedTarget == "All" || e.Source == SelectedTarget)
            .ToList();

        // Combat styles
        var combatStyleEvents = filteredEvents.OfType<CombatStyleEvent>()
            .Where(e => SelectedTarget == "All" || e.Target == SelectedTarget)
            .ToList();

        // Spells
        var spellEvents = filteredEvents.OfType<SpellCastEvent>()
            .Where(e => SelectedTarget == "All" || e.Target == SelectedTarget)
            .ToList();

        // Calculate statistics
        CalculateDamageStatistics(damageDealtEvents, duration);
        CalculateDamageTakenStatistics(damageTakenEvents, duration);
        CalculateHealingStatistics(healingDoneEvents, healingReceivedEvents, duration);
        CalculateCombatStyleDetails(combatStyleEvents);
        CalculateSpellDetails(spellEvents);

        // Update statistics record
        _currentStatistics = new CombatStatistics(
            duration.TotalMinutes,
            TotalDamageDealt,
            double.TryParse(DamagePerSecond, out var dps) ? dps : 0,
            double.TryParse(AverageDamage, out var avg) ? avg : 0,
            double.TryParse(MedianDamage, out var med) ? med : 0,
            CombatStylesUsed,
            SpellsCast
        );

        // Generate charts
        GenerateCharts();

        // Generate pie charts
        GeneratePieCharts(damageDealtEvents);

        // Populate event table
        PopulateEventTable(filteredEvents);

        // Generate quick stats summary
        GenerateQuickStatsSummary();

        // Generate comparison if applicable
        if (IsComparisonMode && _comparisonEvents != null)
        {
            GenerateComparisonSummary();
        }
    }

    private void CalculateDamageStatistics(List<DamageEvent> events, TimeSpan duration)
    {
        var totalDamage = events.Sum(e => e.DamageAmount);
        var amounts = events.Select(e => e.DamageAmount).OrderBy(d => d).ToList();

        double median = 0;
        if (amounts.Count > 0)
        {
            var mid = amounts.Count / 2;
            median = amounts.Count % 2 != 0 ? amounts[mid] : (amounts[mid - 1] + amounts[mid]) / 2.0;
        }

        var average = events.Count > 0 ? totalDamage / (double)events.Count : 0;
        var dps = duration.TotalSeconds > 0 ? totalDamage / duration.TotalSeconds : 0;

        LogDuration = duration.TotalMinutes.ToString("F2");
        TotalDamageDealt = totalDamage;
        DamagePerSecond = dps.ToString("F2");
        AverageDamage = average.ToString("F2");
        MedianDamage = median.ToString("F2");
    }

    private void CalculateDamageTakenStatistics(List<DamageEvent> events, TimeSpan duration)
    {
        var totalDamage = events.Sum(e => e.DamageAmount);
        var amounts = events.Select(e => e.DamageAmount).OrderBy(d => d).ToList();

        double median = 0;
        if (amounts.Count > 0)
        {
            var mid = amounts.Count / 2;
            median = amounts.Count % 2 != 0 ? amounts[mid] : (amounts[mid - 1] + amounts[mid]) / 2.0;
        }

        var average = events.Count > 0 ? totalDamage / (double)events.Count : 0;
        var dtps = duration.TotalSeconds > 0 ? totalDamage / duration.TotalSeconds : 0;

        TotalDamageTaken = totalDamage;
        DamageTakenPerSecond = dtps.ToString("F2");
        AverageDamageTaken = average.ToString("F2");
        MedianDamageTaken = median.ToString("F2");
    }

    private void CalculateHealingStatistics(List<HealingEvent> healingDone, List<HealingEvent> healingReceived, TimeSpan duration)
    {
        // Healing done
        var totalHealing = healingDone.Sum(e => e.HealingAmount);
        var amounts = healingDone.Select(e => e.HealingAmount).OrderBy(d => d).ToList();

        double median = 0;
        if (amounts.Count > 0)
        {
            var mid = amounts.Count / 2;
            median = amounts.Count % 2 != 0 ? amounts[mid] : (amounts[mid - 1] + amounts[mid]) / 2.0;
        }

        var average = healingDone.Count > 0 ? totalHealing / (double)healingDone.Count : 0;
        var hps = duration.TotalSeconds > 0 ? totalHealing / duration.TotalSeconds : 0;

        TotalHealingDone = totalHealing;
        HealingPerSecond = hps.ToString("F2");
        AverageHealing = average.ToString("F2");
        MedianHealing = median.ToString("F2");

        // Healing received
        var totalReceived = healingReceived.Sum(e => e.HealingAmount);
        var hrps = duration.TotalSeconds > 0 ? totalReceived / duration.TotalSeconds : 0;

        TotalHealingReceived = totalReceived;
        HealingReceivedPerSecond = hrps.ToString("F2");
    }

    private void CalculateCombatStyleDetails(List<CombatStyleEvent> events)
    {
        var details = events
            .GroupBy(e => e.StyleName)
            .Select(g => new CombatStyleDetail { StyleName = g.Key, Count = g.Count() })
            .OrderByDescending(d => d.Count)
            .ToList();

        CombatStyleDetails = new ObservableCollection<CombatStyleDetail>(details);
        CombatStylesUsed = details.Count;
    }

    private void CalculateSpellDetails(List<SpellCastEvent> events)
    {
        var details = events
            .GroupBy(e => e.SpellName)
            .Select(g => new SpellCastDetail { SpellName = g.Key, Count = g.Count() })
            .OrderByDescending(d => d.Count)
            .ToList();

        SpellCastDetails = new ObservableCollection<SpellCastDetail>(details);
        SpellsCast = details.Count;
    }

    private void GenerateCharts()
    {
        if (_analyzedEvents == null) return;

        var startTime = TimeOnly.FromTimeSpan(TimeRangeStart);
        var endTime = TimeOnly.FromTimeSpan(TimeRangeEnd);

        var filteredEvents = _analyzedEvents
            .Where(e => e.Timestamp >= startTime && e.Timestamp <= endTime)
            .ToList();

        if (!filteredEvents.Any()) return;

        var intervalSeconds = SelectedChartInterval switch
        {
            "1s" => 1,
            "5s" => 5,
            "10s" => 10,
            "30s" => 30,
            "1m" => 60,
            _ => 5
        };

        var firstTime = filteredEvents.First().Timestamp;
        var lastTime = filteredEvents.Last().Timestamp;
        var duration = lastTime - firstTime;
        var bucketCount = Math.Max(1, (int)(duration.TotalSeconds / intervalSeconds) + 1);

        var seriesList = new List<ISeries>();
        var labels = new List<string>();

        // Generate labels
        for (int i = 0; i < bucketCount; i++)
        {
            var seconds = i * intervalSeconds;
            labels.Add(seconds >= 60 ? $"{seconds / 60}m{seconds % 60}s" : $"{seconds}s");
        }

        // Damage Dealt series
        if (ShowDamageDealtOnChart)
        {
            var damageDealtEvents = filteredEvents.OfType<DamageEvent>()
                .Where(e => e.Source == CombatantName)
                .Where(e => SelectedDamageType == "All" || e.DamageType == SelectedDamageType)
                .Where(e => SelectedTarget == "All" || e.Target == SelectedTarget)
                .ToList();

            var damagePoints = GenerateBucketData(damageDealtEvents, firstTime, intervalSeconds, bucketCount, e => e.DamageAmount);
            seriesList.Add(CreateSeries("Damage Dealt", damagePoints, SKColors.Orange));
        }

        // Damage Taken series
        if (ShowDamageTakenOnChart)
        {
            var damageTakenEvents = filteredEvents.OfType<DamageEvent>()
                .Where(e => e.Target == CombatantName)
                .Where(e => SelectedDamageType == "All" || e.DamageType == SelectedDamageType)
                .ToList();

            var takenPoints = GenerateBucketData(damageTakenEvents, firstTime, intervalSeconds, bucketCount, e => e.DamageAmount);
            seriesList.Add(CreateSeries("Damage Taken", takenPoints, SKColors.Red));
        }

        // Healing series
        if (ShowHealingOnChart)
        {
            var healingEvents = filteredEvents.OfType<HealingEvent>()
                .Where(e => e.Source == CombatantName || e.Target == CombatantName)
                .ToList();

            var healingPoints = GenerateBucketData(healingEvents, firstTime, intervalSeconds, bucketCount, e => e.HealingAmount);
            seriesList.Add(CreateSeries("Healing", healingPoints, SKColors.Green));
        }

        // DPS Trend line (moving average)
        if (ShowDpsTrendLine && ShowDamageDealtOnChart)
        {
            var damageDealtEvents = filteredEvents.OfType<DamageEvent>()
                .Where(e => e.Source == CombatantName)
                .Where(e => SelectedDamageType == "All" || e.DamageType == SelectedDamageType)
                .Where(e => SelectedTarget == "All" || e.Target == SelectedTarget)
                .ToList();

            var damagePoints = GenerateBucketData(damageDealtEvents, firstTime, intervalSeconds, bucketCount, e => e.DamageAmount);
            var trendPoints = CalculateMovingAverage(damagePoints, 3);

            seriesList.Add(new LineSeries<double>
            {
                Values = trendPoints,
                Name = "DPS Trend",
                Fill = null,
                GeometrySize = 0,
                LineSmoothness = 0.8,
                Stroke = new SolidColorPaint(SKColors.Purple) { StrokeThickness = 3 },
                GeometryStroke = null
            });
        }

        Series = seriesList.ToArray();
        XAxes = new Axis[]
        {
            new Axis { Name = "Time", Labels = labels, MinStep = 1, ForceStepToMin = false }
        };
    }

    private List<double> GenerateBucketData<T>(List<T> events, TimeOnly firstTime, int intervalSeconds, int bucketCount, Func<T, int> amountSelector)
        where T : LogEvent
    {
        var points = new List<double>();
        for (int i = 0; i < bucketCount; i++)
        {
            var intervalStart = firstTime.Add(TimeSpan.FromSeconds(i * intervalSeconds));
            var intervalEnd = intervalStart.Add(TimeSpan.FromSeconds(intervalSeconds));

            var total = events
                .Where(e => e.Timestamp >= intervalStart && e.Timestamp < intervalEnd)
                .Sum(amountSelector);

            points.Add(total);
        }
        return points;
    }

    private ISeries CreateSeries(string name, List<double> values, SKColor color)
    {
        return SelectedChartType switch
        {
            "Bar" => new ColumnSeries<double>
            {
                Values = values,
                Name = name,
                Fill = new SolidColorPaint(color)
            },
            "Area" => new LineSeries<double>
            {
                Values = values,
                Name = name,
                Fill = new SolidColorPaint(color.WithAlpha(100)),
                GeometrySize = 5,
                LineSmoothness = 0.5,
                Stroke = new SolidColorPaint(color) { StrokeThickness = 2 }
            },
            _ => new LineSeries<double>
            {
                Values = values,
                Name = name,
                Fill = null,
                GeometrySize = 5,
                LineSmoothness = 0.5,
                Stroke = new SolidColorPaint(color) { StrokeThickness = 2 },
                GeometryFill = new SolidColorPaint(color)
            }
        };
    }

    private static List<double> CalculateMovingAverage(List<double> data, int window)
    {
        var result = new List<double>();
        for (int i = 0; i < data.Count; i++)
        {
            var start = Math.Max(0, i - window + 1);
            var count = i - start + 1;
            var avg = data.Skip(start).Take(count).Average();
            result.Add(avg);
        }
        return result;
    }

    private void GeneratePieCharts(List<DamageEvent> damageEvents)
    {
        // Damage by target pie chart
        var byTarget = damageEvents
            .GroupBy(e => e.Target)
            .Select(g => new { Target = g.Key, Total = g.Sum(e => e.DamageAmount) })
            .OrderByDescending(x => x.Total)
            .Take(8) // Limit to top 8 for readability
            .ToList();

        DamageByTargetSeries = byTarget
            .Select(x => new PieSeries<int> { Values = new[] { x.Total }, Name = x.Target } as ISeries)
            .ToArray();

        // Damage type distribution pie chart
        var byType = damageEvents
            .GroupBy(e => e.DamageType)
            .Select(g => new { Type = g.Key, Total = g.Sum(e => e.DamageAmount) })
            .OrderByDescending(x => x.Total)
            .ToList();

        DamageTypeSeries = byType
            .Select(x => new PieSeries<int> { Values = new[] { x.Total }, Name = x.Type } as ISeries)
            .ToArray();
    }

    private void PopulateEventTable(List<LogEvent> events)
    {
        var rows = new List<EventTableRow>();

        foreach (var ev in events)
        {
            var row = ev switch
            {
                DamageEvent de when (de.Source == CombatantName && ShowDamageDealt) || (de.Target == CombatantName && ShowDamageTaken) =>
                    new EventTableRow
                    {
                        Timestamp = de.Timestamp.ToString("HH:mm:ss"),
                        Type = "Damage",
                        Source = de.Source,
                        Target = de.Target,
                        Amount = de.DamageAmount.ToString(),
                        Details = de.DamageType
                    },
                HealingEvent he when (he.Source == CombatantName && ShowHealingDone) || (he.Target == CombatantName && ShowHealingReceived) =>
                    new EventTableRow
                    {
                        Timestamp = he.Timestamp.ToString("HH:mm:ss"),
                        Type = "Healing",
                        Source = he.Source,
                        Target = he.Target,
                        Amount = he.HealingAmount.ToString(),
                        Details = ""
                    },
                CombatStyleEvent cse when ShowCombatStyles =>
                    new EventTableRow
                    {
                        Timestamp = cse.Timestamp.ToString("HH:mm:ss"),
                        Type = "Style",
                        Source = cse.Source,
                        Target = cse.Target,
                        Amount = "",
                        Details = cse.StyleName
                    },
                SpellCastEvent sce when ShowSpells =>
                    new EventTableRow
                    {
                        Timestamp = sce.Timestamp.ToString("HH:mm:ss"),
                        Type = "Spell",
                        Source = sce.Source,
                        Target = sce.Target,
                        Amount = "",
                        Details = sce.SpellName
                    },
                _ => null
            };

            if (row != null)
            {
                // Apply text filter if set
                if (string.IsNullOrEmpty(EventTableFilter) ||
                    row.Source.Contains(EventTableFilter, StringComparison.OrdinalIgnoreCase) ||
                    row.Target.Contains(EventTableFilter, StringComparison.OrdinalIgnoreCase) ||
                    row.Details.Contains(EventTableFilter, StringComparison.OrdinalIgnoreCase))
                {
                    rows.Add(row);
                }
            }
        }

        EventTableRows = new ObservableCollection<EventTableRow>(rows);
    }

    private void GenerateQuickStatsSummary()
    {
        var parts = new List<string>();

        if (TotalDamageDealt > 0)
            parts.Add($"DMG: {TotalDamageDealt:N0} ({DamagePerSecond} DPS)");
        if (TotalDamageTaken > 0)
            parts.Add($"TAKEN: {TotalDamageTaken:N0}");
        if (TotalHealingDone > 0)
            parts.Add($"HEAL: {TotalHealingDone:N0} ({HealingPerSecond} HPS)");
        if (CombatStylesUsed > 0)
            parts.Add($"Styles: {CombatStylesUsed}");
        if (SpellsCast > 0)
            parts.Add($"Spells: {SpellsCast}");

        QuickStatsSummary = string.Join(" | ", parts);
    }

    private void GenerateComparisonSummary()
    {
        if (_comparisonEvents == null || !_comparisonEvents.Any())
        {
            ComparisonSummary = "No comparison data available.";
            return;
        }

        var compDamageEvents = _comparisonEvents.OfType<DamageEvent>()
            .Where(e => e.Source == CombatantName)
            .ToList();

        var compDuration = _comparisonEvents.Last().Timestamp - _comparisonEvents.First().Timestamp;
        var compTotalDamage = compDamageEvents.Sum(e => e.DamageAmount);
        var compDps = compDuration.TotalSeconds > 0 ? compTotalDamage / compDuration.TotalSeconds : 0;

        var dmgDiff = TotalDamageDealt - compTotalDamage;
        var dpsDiff = (double.TryParse(DamagePerSecond, out var currentDps) ? currentDps : 0) - compDps;

        var dmgSign = dmgDiff >= 0 ? "+" : "";
        var dpsSign = dpsDiff >= 0 ? "+" : "";

        ComparisonSummary = $"Comparison: Damage {dmgSign}{dmgDiff:N0} ({dpsSign}{dpsDiff:F2} DPS)";
    }

    #endregion

    #region Preferences

    private void LoadPreferences()
    {
        try
        {
            if (File.Exists(PreferencesPath))
            {
                var json = File.ReadAllText(PreferencesPath);
                var prefs = JsonSerializer.Deserialize<UserPreferences>(json);
                if (prefs != null)
                {
                    CombatantName = prefs.CombatantName ?? "You";
                    SelectedChartType = prefs.ChartType ?? "Line";
                    SelectedChartInterval = prefs.ChartInterval ?? "5s";
                    if (!string.IsNullOrEmpty(prefs.LastLogFilePath) && File.Exists(prefs.LastLogFilePath))
                    {
                        SelectedLogFile = prefs.LastLogFilePath;
                    }
                }
            }
        }
        catch
        {
            // Ignore preferences loading errors
        }
    }

    private void SavePreferences()
    {
        try
        {
            var prefs = new UserPreferences
            {
                CombatantName = CombatantName,
                ChartType = SelectedChartType,
                ChartInterval = SelectedChartInterval,
                LastLogFilePath = SelectedLogFile != "No file selected" ? SelectedLogFile : null
            };

            var directory = Path.GetDirectoryName(PreferencesPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonSerializer.Serialize(prefs, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(PreferencesPath, json);
        }
        catch
        {
            // Ignore preferences saving errors
        }
    }

    #endregion
}

#region Helper Classes

public class CombatStyleDetail
{
    public string StyleName { get; set; } = "";
    public int Count { get; set; }
}

public class SpellCastDetail
{
    public string SpellName { get; set; } = "";
    public int Count { get; set; }
}

public class EventTableRow
{
    public string Timestamp { get; set; } = "";
    public string Type { get; set; } = "";
    public string Source { get; set; } = "";
    public string Target { get; set; } = "";
    public string Amount { get; set; } = "";
    public string Details { get; set; } = "";
}

public class UserPreferences
{
    public string? CombatantName { get; set; }
    public string? ChartType { get; set; }
    public string? ChartInterval { get; set; }
    public string? LastLogFilePath { get; set; }
}

#endregion
