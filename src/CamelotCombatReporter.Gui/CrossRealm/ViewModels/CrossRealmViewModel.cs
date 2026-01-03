using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using CamelotCombatReporter.Core.CrossRealm;
using CamelotCombatReporter.Core.Models;
using CamelotCombatReporter.Gui.CrossRealm.Views;
using CamelotCombatReporter.Gui.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CamelotCombatReporter.Gui.CrossRealm.ViewModels;

/// <summary>
/// ViewModel for the cross-realm statistics view.
/// </summary>
public partial class CrossRealmViewModel : ViewModelBase
{
    private readonly ICrossRealmStatisticsService _statisticsService;
    private readonly CrossRealmExporter _exporter;

    #region Character Properties

    [ObservableProperty]
    private CharacterInfo _character = CharacterInfo.Default;

    [ObservableProperty]
    private bool _isCharacterConfigured;

    [ObservableProperty]
    private string _characterDisplayText = "No character configured";

    #endregion

    #region Current Session Properties

    [ObservableProperty]
    private ExtendedCombatStatistics? _currentSession;

    [ObservableProperty]
    private bool _hasCurrentSession;

    [ObservableProperty]
    private string _currentSessionSummary = "";

    #endregion

    #region Statistics Properties

    [ObservableProperty]
    private ObservableCollection<RealmStatistics> _realmStatistics = new();

    [ObservableProperty]
    private ObservableCollection<ClassStatistics> _classStatistics = new();

    [ObservableProperty]
    private ObservableCollection<LeaderboardEntry> _dpsLeaderboard = new();

    [ObservableProperty]
    private ObservableCollection<LeaderboardEntry> _hpsLeaderboard = new();

    [ObservableProperty]
    private ObservableCollection<CombatSessionSummary> _recentSessions = new();

    [ObservableProperty]
    private int _totalSessionCount;

    [ObservableProperty]
    private bool _hasStatistics;

    #endregion

    #region UI State Properties

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _statusMessage = "";

    [ObservableProperty]
    private Realm _selectedRealmFilter = Realm.Unknown;

    [ObservableProperty]
    private ObservableCollection<Realm> _availableRealms = new()
    {
        Realm.Unknown, // "All" option
        Realm.Albion,
        Realm.Midgard,
        Realm.Hibernia
    };

    #endregion

    public CrossRealmViewModel() : this(new CrossRealmStatisticsService())
    {
    }

    public CrossRealmViewModel(ICrossRealmStatisticsService statisticsService)
    {
        _statisticsService = statisticsService;
        _exporter = new CrossRealmExporter(statisticsService);
    }

    /// <summary>
    /// Initializes the view model and loads data.
    /// </summary>
    public async Task InitializeAsync()
    {
        await RefreshStatisticsAsync();
    }

    partial void OnCharacterChanged(CharacterInfo value)
    {
        IsCharacterConfigured = value.IsConfigured;
        CharacterDisplayText = value.IsConfigured
            ? $"{value.Name} - {value.Class.GetDisplayName()} ({value.Realm})"
            : "No character configured";
    }

    partial void OnSelectedRealmFilterChanged(Realm value)
    {
        _ = RefreshStatisticsAsync();
    }

    #region Commands

    [RelayCommand]
    private async Task ConfigureCharacter()
    {
        var mainWindow = GetMainWindow();
        if (mainWindow == null) return;

        var result = await CharacterConfigDialog.ShowDialogAsync(mainWindow, Character);
        if (result != null)
        {
            Character = result;
            StatusMessage = "Character configuration saved";
        }
    }

    [RelayCommand]
    private async Task SaveCurrentSession()
    {
        if (CurrentSession == null)
        {
            StatusMessage = "No session to save";
            return;
        }

        try
        {
            IsLoading = true;
            StatusMessage = "Saving session...";

            await _statisticsService.SaveSessionAsync(CurrentSession);

            StatusMessage = "Session saved successfully";
            await RefreshStatisticsAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error saving session: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task RefreshStatistics()
    {
        await RefreshStatisticsAsync();
    }

    [RelayCommand]
    private async Task ExportJson()
    {
        var mainWindow = GetMainWindow();
        if (mainWindow == null) return;

        try
        {
            var file = await mainWindow.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Export Cross-Realm Statistics to JSON",
                DefaultExtension = "json",
                SuggestedFileName = $"cross-realm-stats-{DateTime.Now:yyyyMMdd}",
                FileTypeChoices = new[] { new FilePickerFileType("JSON Files") { Patterns = new[] { "*.json" } } }
            });

            if (file != null)
            {
                IsLoading = true;
                StatusMessage = "Exporting to JSON...";

                var options = new ExportOptions(
                    RealmFilter: SelectedRealmFilter == Realm.Unknown ? null : SelectedRealmFilter,
                    AggregateOnly: false,
                    IncludeCharacterNames: true);

                await using var stream = await file.OpenWriteAsync();
                await _exporter.ExportToJsonAsync(stream, options);

                StatusMessage = "Export completed successfully";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Export failed: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task ExportCsv()
    {
        var mainWindow = GetMainWindow();
        if (mainWindow == null) return;

        try
        {
            var file = await mainWindow.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Export Cross-Realm Statistics to CSV",
                DefaultExtension = "csv",
                SuggestedFileName = $"cross-realm-stats-{DateTime.Now:yyyyMMdd}",
                FileTypeChoices = new[] { new FilePickerFileType("CSV Files") { Patterns = new[] { "*.csv" } } }
            });

            if (file != null)
            {
                IsLoading = true;
                StatusMessage = "Exporting to CSV...";

                var options = new ExportOptions(
                    RealmFilter: SelectedRealmFilter == Realm.Unknown ? null : SelectedRealmFilter,
                    AggregateOnly: true);

                await using var stream = await file.OpenWriteAsync();
                await _exporter.ExportToCsvAsync(stream, options);

                StatusMessage = "Export completed successfully";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Export failed: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task RebuildIndex()
    {
        try
        {
            IsLoading = true;
            StatusMessage = "Rebuilding session index...";

            await _statisticsService.RebuildIndexAsync();

            StatusMessage = "Index rebuilt successfully";
            await RefreshStatisticsAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error rebuilding index: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Sets the current session from analyzed combat data.
    /// Call this after analyzing a log to enable saving cross-realm statistics.
    /// </summary>
    public void SetCurrentSession(CombatStatistics baseStats, int damageTaken, int healingDone, int healingReceived,
        int kills, int deaths, int assists, DateTime sessionStart, DateTime sessionEnd, string? logFileName)
    {
        if (!IsCharacterConfigured)
        {
            CurrentSession = null;
            HasCurrentSession = false;
            CurrentSessionSummary = "Configure your character to track cross-realm statistics";
            return;
        }

        CurrentSession = ExtendedCombatStatistics.FromBaseStats(
            baseStats,
            Character,
            damageTaken,
            healingDone,
            healingReceived,
            kills,
            deaths,
            assists,
            sessionStart,
            sessionEnd,
            logFileName);

        HasCurrentSession = true;
        CurrentSessionSummary = $"DPS: {baseStats.Dps:F1} | Duration: {baseStats.DurationMinutes:F1}m | K/D: {kills}/{deaths}";
    }

    /// <summary>
    /// Clears the current session.
    /// </summary>
    public void ClearCurrentSession()
    {
        CurrentSession = null;
        HasCurrentSession = false;
        CurrentSessionSummary = "";
    }

    #endregion

    #region Private Methods

    private async Task RefreshStatisticsAsync()
    {
        try
        {
            IsLoading = true;
            StatusMessage = "Loading statistics...";

            // Get realm filter
            Realm? realmFilter = SelectedRealmFilter == Realm.Unknown ? null : SelectedRealmFilter;

            // Load realm statistics
            var realmStats = await _statisticsService.GetAllRealmStatisticsAsync();
            RealmStatistics.Clear();
            foreach (var stat in realmStats)
            {
                if (!realmFilter.HasValue || stat.Realm == realmFilter.Value)
                {
                    if (stat.SessionCount > 0)
                    {
                        RealmStatistics.Add(stat);
                    }
                }
            }

            // Load class statistics for selected realm (or all if no filter)
            ClassStatistics.Clear();
            if (realmFilter.HasValue)
            {
                var classStats = await _statisticsService.GetClassStatisticsForRealmAsync(realmFilter.Value);
                foreach (var stat in classStats)
                {
                    if (stat.SessionCount > 0)
                    {
                        ClassStatistics.Add(stat);
                    }
                }
            }

            // Load leaderboards
            var dpsLeaders = await _statisticsService.GetLocalLeaderboardAsync(
                LeaderboardMetrics.Dps, realmFilter, null, 5);
            DpsLeaderboard.Clear();
            foreach (var entry in dpsLeaders)
            {
                DpsLeaderboard.Add(entry);
            }

            var hpsLeaders = await _statisticsService.GetLocalLeaderboardAsync(
                LeaderboardMetrics.Hps, realmFilter, null, 5);
            HpsLeaderboard.Clear();
            foreach (var entry in hpsLeaders)
            {
                HpsLeaderboard.Add(entry);
            }

            // Load recent sessions
            var sessions = await _statisticsService.GetSessionsAsync(realmFilter, null, null, 10);
            RecentSessions.Clear();
            foreach (var session in sessions)
            {
                RecentSessions.Add(session);
            }

            // Get total count
            TotalSessionCount = await _statisticsService.GetSessionCountAsync();
            HasStatistics = TotalSessionCount > 0;

            StatusMessage = HasStatistics
                ? $"Loaded {TotalSessionCount} sessions"
                : "No sessions saved yet";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading statistics: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private static Window? GetMainWindow()
    {
        return Avalonia.Application.Current?.ApplicationLifetime is
            Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
            ? desktop.MainWindow
            : null;
    }

    #endregion
}
