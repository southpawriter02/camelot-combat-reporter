using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using CamelotCombatReporter.Core.Caching;
using CamelotCombatReporter.Core.Exporting;
using CamelotCombatReporter.Core.InstanceTracking;
using CamelotCombatReporter.Core.Logging;
using CamelotCombatReporter.Core.Models;
using CamelotCombatReporter.Core.Parsing;
using CamelotCombatReporter.Gui.Plugins.ViewModels;
using CamelotCombatReporter.Gui.Plugins.Views;
using CamelotCombatReporter.Gui.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using Microsoft.Extensions.Logging;
using SkiaSharp;

namespace CamelotCombatReporter.Gui.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly ILogger<MainWindowViewModel> _logger;
    #region File Selection Properties

    [ObservableProperty]
    private string _selectedLogFile = "No file selected";

    [ObservableProperty]
    private string _combatantName = "You";

    [ObservableProperty]
    private bool _hasAnalyzedData = false;

    [ObservableProperty]
    private string _statusMessage = "";

    [ObservableProperty]
    private bool _hasStatusMessage = false;

    /// <summary>
    /// Indicates whether a log file is currently being analyzed.
    /// </summary>
    [ObservableProperty]
    private bool _isAnalyzing = false;

    /// <summary>
    /// The loading progress message displayed during analysis.
    /// </summary>
    [ObservableProperty]
    private string _loadingMessage = "Analyzing log file...";

    /// <summary>
    /// The current analysis progress (0-100).
    /// </summary>
    [ObservableProperty]
    private double _analysisProgress = 0;

    /// <summary>
    /// Current theme mode value for reactive binding.
    /// </summary>
    [ObservableProperty]
    private string _currentThemeMode = "System";

    /// <summary>
    /// Gets the event count status text for the status bar.
    /// </summary>
    public string EventCountStatus => _analyzedEvents != null
        ? $"Events: {_analyzedEvents.Count:N0}"
        : "No data";

    /// <summary>
    /// Gets the current theme status text for the status bar.
    /// </summary>
    public string ThemeStatus => CurrentThemeMode;

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

    #region Instance Tracking

    [ObservableProperty]
    private bool _showInstanceBreakdown = false;

    [ObservableProperty]
    private ObservableCollection<TargetTypeStatisticsViewModel> _targetStatistics = new();

    [ObservableProperty]
    private ObservableCollection<CombatEncounterViewModel> _allEncounters = new();

    #endregion

    #region Session Tracking

    [ObservableProperty]
    private ObservableCollection<CombatSessionViewModel> _combatSessions = new();

    [ObservableProperty]
    private ObservableCollection<string> _availableSessions = new() { "All Sessions" };

    [ObservableProperty]
    private string _selectedSession = "All Sessions";

    [ObservableProperty]
    private int _totalSessionCount = 0;

    [ObservableProperty]
    private string _sessionStatsSummary = "";

    #endregion

    #region Private Fields

    private List<LogEvent>? _analyzedEvents;
    private List<LogEvent>? _comparisonEvents;
    private CombatStatistics? _currentStatistics;
    private TimeOnly _firstEventTime;
    private TimeOnly _lastEventTime;
    private readonly ICombatInstanceResolver _instanceResolver;
    private readonly ICombatSessionResolver _sessionResolver;
    private readonly IStatisticsCacheService _cacheService;
    private IReadOnlyList<CombatSession>? _resolvedSessions;
    private CancellationTokenSource? _analysisCancellationSource;

    private static readonly string PreferencesPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "CamelotCombatReporter",
        "preferences.json");

    private static readonly string CachePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "CamelotCombatReporter",
        "cache");

    #endregion

    public MainWindowViewModel()
    {
        _logger = App.CreateLogger<MainWindowViewModel>();
        _instanceResolver = new CombatInstanceResolver();
        _sessionResolver = new CombatSessionResolver();
        _cacheService = new StatisticsCacheService(CachePath, App.CreateLogger<StatisticsCacheService>());

        // Initialize theme mode from current theme service
        CurrentThemeMode = App.ThemeService?.CurrentTheme.ToString() ?? "System";

        LoadPreferences();

        _logger.LogInformation("MainWindowViewModel initialized successfully");
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

        // Cancel any previous analysis
        _analysisCancellationSource?.Cancel();
        _analysisCancellationSource = new CancellationTokenSource();
        var cancellationToken = _analysisCancellationSource.Token;

        _logger.LogAnalyzingFile(SelectedLogFile);
        StatusMessage = "";
        HasStatusMessage = false;

        // Show loading overlay
        IsAnalyzing = true;
        AnalysisProgress = 0;
        LoadingMessage = "Initializing analysis...";

        try
        {
            var fileName = Path.GetFileName(SelectedLogFile);
            var noCombatEvents = false;

            // Create progress reporter for async parsing
            var progress = new Progress<ParseProgress>(p =>
            {
                Dispatcher.UIThread.Post(() =>
                {
                    AnalysisProgress = p.PercentComplete;
                    LoadingMessage = p.LinesProcessed > 0
                        ? $"Processing log... ({p.LinesProcessed:N0} lines, {p.EventsFound:N0} events)"
                        : "Reading file...";
                });
            });

            // Check cache first
            var cachedEvents = await _cacheService.GetCachedStatisticsAsync<List<LogEvent>>(SelectedLogFile, "ParsedEvents");
            List<LogEvent> events;

            if (cachedEvents != null)
            {
                _logger.LogDebug("Using cached events for {FilePath}", SelectedLogFile);
                events = cachedEvents;
                AnalysisProgress = 100;
                LoadingMessage = "Loading from cache...";
            }
            else
            {
                // Use async parser with progress reporting
                var logParser = new LogParser(SelectedLogFile);
                events = await logParser.ParseAsync(progress, cancellationToken);

                // Cache the parsed events
                if (events.Count > 0)
                {
                    await _cacheService.CacheStatisticsAsync(SelectedLogFile, "ParsedEvents", events);
                    _logger.LogDebug("Cached {EventCount} events for {FilePath}", events.Count, SelectedLogFile);
                }
            }

            cancellationToken.ThrowIfCancellationRequested();

            LoadingMessage = "Analyzing combat data...";

            if (events.Count == 0)
            {
                HasAnalyzedData = false;
                noCombatEvents = true;
            }
            else
            {
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
                    LoadingMessage = "Parsing comparison file...";
                    var compParser = new LogParser(ComparisonLogFile);
                    _comparisonEvents = await compParser.ParseAsync(null, cancellationToken);
                }

                LoadingMessage = "Generating statistics...";

                // Analyze and update UI
                RefreshAnalysis();

                HasAnalyzedData = true;
                OnPropertyChanged(nameof(EventCountStatus));

                _logger.LogAnalysisCompleted(events.Count, LogDuration, DamagePerSecond);
            }

            if (noCombatEvents)
            {
                StatusMessage = $"No combat events found in '{fileName}'. The log may contain only chat, spells, or other non-combat activity. Combat Reporter looks for damage, healing, loot, and death events.";
                HasStatusMessage = true;
                _logger.LogInformation("No combat events found in log file: {FilePath}", SelectedLogFile);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Analysis cancelled for: {FilePath}", SelectedLogFile);
            StatusMessage = "Analysis was cancelled.";
            HasStatusMessage = true;
        }
        catch (Exception ex)
        {
            _logger.LogAnalysisError(SelectedLogFile, ex);
            HasAnalyzedData = false;
            StatusMessage = $"Error analyzing log file: {ex.Message}";
            HasStatusMessage = true;
        }
        finally
        {
            // Hide loading overlay
            IsAnalyzing = false;
            AnalysisProgress = 0;
            LoadingMessage = "Analyzing log file...";
        }

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

    [RelayCommand]
    private async Task ShowKeyboardShortcuts()
    {
        var mainWindow = GetMainWindow();
        if (mainWindow == null) return;

        var window = new Views.KeyboardShortcutsWindow();
        await window.ShowDialog(mainWindow);
    }

    /// <summary>
    /// Toggles between light and dark themes (cycles: System -> Light -> Dark -> System).
    /// </summary>
    [RelayCommand]
    private void ToggleTheme()
    {
        if (App.ThemeService == null) return;

        var nextTheme = App.ThemeService.CurrentTheme switch
        {
            ThemeMode.System => ThemeMode.Light,
            ThemeMode.Light => ThemeMode.Dark,
            ThemeMode.Dark => ThemeMode.System,
            _ => ThemeMode.System
        };

        App.ThemeService.SetTheme(nextTheme);
        CurrentThemeMode = nextTheme.ToString();
        OnPropertyChanged(nameof(ThemeStatus));

        _logger.LogInformation("Theme toggled to {Theme}", nextTheme);
    }

    /// <summary>
    /// Sets a specific theme mode.
    /// </summary>
    /// <param name="themeMode">The theme mode string: "System", "Light", or "Dark".</param>
    [RelayCommand]
    private void SetTheme(string themeMode)
    {
        if (App.ThemeService == null) return;

        var theme = themeMode switch
        {
            "Light" => ThemeMode.Light,
            "Dark" => ThemeMode.Dark,
            _ => ThemeMode.System
        };

        App.ThemeService.SetTheme(theme);
        CurrentThemeMode = theme.ToString();
        OnPropertyChanged(nameof(ThemeStatus));

        _logger.LogInformation("Theme set to {Theme}", theme);
    }

    /// <summary>
    /// Cancels the current analysis operation if one is in progress.
    /// </summary>
    [RelayCommand]
    private void CancelAnalysis()
    {
        if (_analysisCancellationSource != null && !_analysisCancellationSource.IsCancellationRequested)
        {
            _analysisCancellationSource.Cancel();
            _logger.LogInformation("Analysis cancellation requested");
        }
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

        // Resolve combat instances for per-target tracking
        ResolveInstanceTracking(filteredEvents);

        // Resolve combat sessions
        ResolveCombatSessions(filteredEvents);

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

    private void ResolveInstanceTracking(List<LogEvent> filteredEvents)
    {
        var targetStats = _instanceResolver.ResolveInstances(filteredEvents, CombatantName);
        var allEncounters = _instanceResolver.GetAllEncounters(filteredEvents, CombatantName);

        // Map to view models
        TargetStatistics = new ObservableCollection<TargetTypeStatisticsViewModel>(
            targetStats.Select(ts => new TargetTypeStatisticsViewModel(ts)));

        AllEncounters = new ObservableCollection<CombatEncounterViewModel>(
            allEncounters.Select(e => new CombatEncounterViewModel(e)));
    }

    private void ResolveCombatSessions(List<LogEvent> filteredEvents)
    {
        _resolvedSessions = _sessionResolver.ResolveSessions(filteredEvents, CombatantName);
        var stats = _sessionResolver.GetSessionStatistics(filteredEvents, CombatantName);

        // Map to view models
        CombatSessions = new ObservableCollection<CombatSessionViewModel>(
            _resolvedSessions.Select(s => new CombatSessionViewModel(s)));

        TotalSessionCount = _resolvedSessions.Count;

        // Populate session filter dropdown
        var sessionOptions = new List<string> { "All Sessions" };
        sessionOptions.AddRange(_resolvedSessions.Select(s => $"Session {s.SessionNumber} ({s.StartTime:HH:mm:ss})"));
        AvailableSessions = new ObservableCollection<string>(sessionOptions);

        // Generate session stats summary
        if (_resolvedSessions.Count > 0)
        {
            SessionStatsSummary = $"{stats.TotalSessions} sessions, {stats.TotalKills} kills, {stats.TotalDamageDealt:N0} total dmg, {stats.AverageDps:F1} avg DPS";
        }
        else
        {
            SessionStatsSummary = "No combat sessions detected";
        }
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
        _logger.LogLoadingPreferences(PreferencesPath);
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
        catch (Exception ex)
        {
            _logger.LogPreferencesLoadFailed(ex);
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
        catch (Exception ex)
        {
            _logger.LogPreferencesSaveFailed(ex);
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

/// <summary>
/// View model for target type statistics with expandable encounter list.
/// </summary>
public partial class TargetTypeStatisticsViewModel : ViewModelBase
{
    private readonly TargetTypeStatistics _model;

    [ObservableProperty]
    private bool _isExpanded = false;

    public TargetTypeStatisticsViewModel(TargetTypeStatistics model)
    {
        _model = model;
        Encounters = new ObservableCollection<CombatEncounterViewModel>(
            model.Encounters.Select(e => new CombatEncounterViewModel(e)));
    }

    public string TargetName => _model.TargetName;
    public int TotalKills => _model.TotalKills;
    public int TotalEncounters => _model.TotalEncounters;
    public int TotalDamageDealt => _model.TotalDamageDealt;
    public int TotalDamageTaken => _model.TotalDamageTaken;
    public string AverageDamagePerKill => _model.AverageDamagePerKill.ToString("F0");
    public string AverageTimeToKill => _model.AverageTimeToKill.ToString("F1") + "s";
    public string AverageDps => _model.AverageDps.ToString("F1");
    public string FastestKill => _model.FastestKill?.ToString("F1") + "s" ?? "N/A";
    public string HighestDps => _model.HighestDps?.ToString("F1") ?? "N/A";

    public ObservableCollection<CombatEncounterViewModel> Encounters { get; }

    public string Summary => $"{TotalKills} kills, {TotalDamageDealt:N0} dmg, {AverageDps} avg DPS";
}

/// <summary>
/// View model for individual combat encounters.
/// </summary>
public class CombatEncounterViewModel
{
    private readonly CombatEncounter _model;

    public CombatEncounterViewModel(CombatEncounter model)
    {
        _model = model;
    }

    public string DisplayName => _model.Instance.DisplayName;
    public int InstanceNumber => _model.Instance.InstanceNumber;
    public string StartTime => _model.StartTime.ToString("HH:mm:ss");
    public string EndTime => _model.EndTime.ToString("HH:mm:ss");
    public string Duration => _model.Duration.TotalSeconds.ToString("F1") + "s";
    public int DamageDealt => _model.TotalDamageDealt;
    public int DamageTaken => _model.TotalDamageTaken;
    public int HealingDone => _model.TotalHealingDone;
    public string Dps => _model.Dps.ToString("F1");
    public string EndReason => _model.EndReason.ToString();
    public bool WasKilled => _model.WasKilled;
    public int EventCount => _model.Events.Count;

    // For timeline markers - expose raw TimeOnly for chart integration
    public TimeOnly EndTimeValue => _model.EndTime;
}

/// <summary>
/// View model for combat sessions containing multiple encounters.
/// </summary>
public partial class CombatSessionViewModel : ViewModelBase
{
    private readonly CombatSession _model;

    [ObservableProperty]
    private bool _isExpanded = false;

    public CombatSessionViewModel(CombatSession model)
    {
        _model = model;
        Encounters = new ObservableCollection<CombatEncounterViewModel>(
            model.Encounters.Select(e => new CombatEncounterViewModel(e)));
    }

    public int SessionNumber => _model.SessionNumber;
    public string StartTime => _model.StartTime.ToString("HH:mm:ss");
    public string EndTime => _model.EndTime.ToString("HH:mm:ss");
    public string Duration => FormatDuration(_model.Duration);
    public int TotalKills => _model.TotalKills;
    public int TotalEncounters => _model.Encounters.Count;
    public int TotalDamageDealt => _model.TotalDamageDealt;
    public int TotalDamageTaken => _model.TotalDamageTaken;
    public int TotalHealingDone => _model.TotalHealingDone;
    public string Dps => _model.Dps.ToString("F1");
    public int UniqueTargetCount => _model.UniqueTargetCount;
    public string EndReason => FormatEndReason(_model.EndReason);
    public int EventCount => _model.Events.Count;

    public ObservableCollection<CombatEncounterViewModel> Encounters { get; }

    public string Summary => $"{TotalKills} kills, {TotalDamageDealt:N0} dmg, {Dps} DPS";

    public string DisplayName => $"Session {SessionNumber}";

    private static string FormatDuration(TimeSpan duration)
    {
        if (duration.TotalMinutes >= 1)
            return $"{duration.TotalMinutes:F1}m";
        return $"{duration.TotalSeconds:F0}s";
    }

    private static string FormatEndReason(SessionEndReason reason) => reason switch
    {
        SessionEndReason.Timeout => "Timeout",
        SessionEndReason.Rest => "Rested",
        SessionEndReason.LogBoundary => "Log Closed",
        SessionEndReason.CombatModeExit => "Combat Ended",
        SessionEndReason.EndOfLog => "End of Log",
        SessionEndReason.InProgress => "In Progress",
        _ => reason.ToString()
    };
}

#endregion
