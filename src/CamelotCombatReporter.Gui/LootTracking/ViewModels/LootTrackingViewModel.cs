using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using CamelotCombatReporter.Core.LootTracking;
using CamelotCombatReporter.Core.Models;
using CamelotCombatReporter.Core.Parsing;
using CamelotCombatReporter.Gui.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;

namespace CamelotCombatReporter.Gui.LootTracking.ViewModels;

public partial class LootTrackingViewModel : ViewModelBase
{
    private readonly ILootTrackingService _lootService;

    #region Session Summary Properties

    [ObservableProperty]
    private int _sessionItemDrops;

    [ObservableProperty]
    private string _sessionCurrency = "0c";

    [ObservableProperty]
    private int _sessionUniqueMobs;

    [ObservableProperty]
    private int _sessionUniqueItems;

    [ObservableProperty]
    private bool _hasSessionData;

    #endregion

    #region Overall Stats Properties

    [ObservableProperty]
    private int _totalSessions;

    [ObservableProperty]
    private int _totalMobsTracked;

    [ObservableProperty]
    private int _totalItemsTracked;

    [ObservableProperty]
    private int _totalKills;

    [ObservableProperty]
    private string _totalCurrencyEarned = "0c";

    #endregion

    #region Mob Browser Properties

    [ObservableProperty]
    private ObservableCollection<MobLootTableViewModel> _mobs = new();

    [ObservableProperty]
    private MobLootTableViewModel? _selectedMob;

    [ObservableProperty]
    private string _mobSearchQuery = "";

    [ObservableProperty]
    private string _mobSortBy = "kills";

    #endregion

    #region Item List Properties

    [ObservableProperty]
    private ObservableCollection<ItemDropViewModel> _items = new();

    [ObservableProperty]
    private ItemDropViewModel? _selectedItem;

    #endregion

    #region Chart Properties

    [ObservableProperty]
    private ISeries[] _dropRateSeries = Array.Empty<ISeries>();

    #endregion

    #region Recent Sessions

    [ObservableProperty]
    private ObservableCollection<LootSessionViewModel> _recentSessions = new();

    #endregion

    #region Status Properties

    [ObservableProperty]
    private string _statusMessage = "Ready";

    [ObservableProperty]
    private bool _isLoading;

    #endregion

    public LootTrackingViewModel() : this(new LootTrackingService())
    {
    }

    public LootTrackingViewModel(ILootTrackingService lootService)
    {
        _lootService = lootService;
    }

    public async Task InitializeAsync()
    {
        await RefreshDataAsync();
    }

    #region Commands

    [RelayCommand]
    private async Task ImportLogFile()
    {
        var topLevel = GetMainWindow();
        if (topLevel == null) return;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select Combat Log to Import Loot Data",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("Log Files") { Patterns = new[] { "*.log", "*.txt" } },
                new FilePickerFileType("All Files") { Patterns = new[] { "*" } }
            }
        });

        if (files.Count > 0)
        {
            await ImportLogAsync(files[0].Path.LocalPath);
        }
    }

    [RelayCommand]
    private async Task RefreshData()
    {
        await RefreshDataAsync();
    }

    [RelayCommand]
    private async Task SearchMobs()
    {
        await LoadMobsAsync();
    }

    [RelayCommand]
    private async Task ExportToJson()
    {
        var topLevel = GetMainWindow();
        if (topLevel == null) return;

        var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Export Loot Data to JSON",
            DefaultExtension = "json",
            FileTypeChoices = new[] { new FilePickerFileType("JSON Files") { Patterns = new[] { "*.json" } } }
        });

        if (file != null)
        {
            await ExportDataAsync(file.Path.LocalPath, "json");
        }
    }

    [RelayCommand]
    private async Task ExportToCsv()
    {
        var topLevel = GetMainWindow();
        if (topLevel == null) return;

        var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Export Loot Data to CSV",
            DefaultExtension = "csv",
            FileTypeChoices = new[] { new FilePickerFileType("CSV Files") { Patterns = new[] { "*.csv" } } }
        });

        if (file != null)
        {
            await ExportDataAsync(file.Path.LocalPath, "csv");
        }
    }

    #endregion

    #region Private Methods

    private static Window? GetMainWindow()
    {
        return Avalonia.Application.Current?.ApplicationLifetime is
            Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
            ? desktop.MainWindow
            : null;
    }

    private async Task ImportLogAsync(string logFilePath)
    {
        try
        {
            IsLoading = true;
            StatusMessage = "Parsing log file...";

            await Task.Run(async () =>
            {
                var parser = new LogParser(logFilePath);
                var allEvents = parser.Parse().ToList();
                var lootEvents = allEvents.OfType<LootEvent>().ToList();

                if (lootEvents.Count == 0)
                {
                    StatusMessage = "No loot events found in log file.";
                    return;
                }

                StatusMessage = $"Found {lootEvents.Count} loot events. Saving session...";

                var summary = await _lootService.SaveSessionAsync(lootEvents, logFilePath);

                // Update session stats
                SessionItemDrops = summary.TotalItemDrops;
                SessionCurrency = summary.TotalCurrencyFormatted;
                SessionUniqueMobs = summary.UniqueMobsKilled;
                SessionUniqueItems = summary.UniqueItemsDropped;
                HasSessionData = true;

                StatusMessage = $"Imported {lootEvents.Count} loot events successfully!";
            });

            await RefreshDataAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error importing log: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task RefreshDataAsync()
    {
        try
        {
            IsLoading = true;
            StatusMessage = "Loading loot data...";

            // Load overall stats
            var stats = await _lootService.GetOverallStatsAsync();
            TotalSessions = stats.TotalSessions;
            TotalMobsTracked = stats.TotalMobsTracked;
            TotalItemsTracked = stats.TotalItemsTracked;
            TotalKills = stats.TotalKills;
            TotalCurrencyEarned = stats.TotalCurrencyFormatted;

            // Load mobs
            await LoadMobsAsync();

            // Load recent sessions
            var sessions = await _lootService.GetRecentSessionsAsync(10);
            RecentSessions = new ObservableCollection<LootSessionViewModel>(
                sessions.Select(s => new LootSessionViewModel(s)));

            StatusMessage = $"Loaded {TotalMobsTracked} mobs, {TotalItemsTracked} items";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading data: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task LoadMobsAsync()
    {
        var mobTables = await _lootService.SearchMobsAsync(
            string.IsNullOrWhiteSpace(MobSearchQuery) ? null : MobSearchQuery,
            MobSortBy,
            50);

        Mobs = new ObservableCollection<MobLootTableViewModel>(
            mobTables.Select(m => new MobLootTableViewModel(m)));

        if (Mobs.Any() && SelectedMob == null)
        {
            SelectedMob = Mobs.First();
        }
    }

    partial void OnSelectedMobChanged(MobLootTableViewModel? value)
    {
        if (value == null)
        {
            Items = new ObservableCollection<ItemDropViewModel>();
            DropRateSeries = Array.Empty<ISeries>();
            return;
        }

        // Update items list
        Items = new ObservableCollection<ItemDropViewModel>(
            value.Items.Select(i => new ItemDropViewModel(i)));

        // Update chart
        UpdateDropRateChart(value.Items);
    }

    private void UpdateDropRateChart(IEnumerable<ItemDropStatistic> items)
    {
        var topItems = items.OrderByDescending(i => i.DropRate).Take(10).ToList();

        if (!topItems.Any())
        {
            DropRateSeries = Array.Empty<ISeries>();
            return;
        }

        DropRateSeries = new ISeries[]
        {
            new ColumnSeries<double>
            {
                Values = topItems.Select(i => i.DropRate).ToArray(),
                Name = "Drop Rate %"
            }
        };
    }

    private async Task ExportDataAsync(string filePath, string format)
    {
        try
        {
            IsLoading = true;
            StatusMessage = $"Exporting to {format.ToUpper()}...";

            var mobTables = await _lootService.SearchMobsAsync(limit: 1000);

            if (format == "csv")
            {
                await ExportToCsvAsync(filePath, mobTables);
            }
            else
            {
                await ExportToJsonAsync(filePath, mobTables);
            }

            StatusMessage = $"Exported successfully to {Path.GetFileName(filePath)}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error exporting: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task ExportToCsvAsync(string filePath, IReadOnlyList<MobLootTable> mobTables)
    {
        using var writer = new StreamWriter(filePath);
        await writer.WriteLineAsync("MobName,ItemName,TotalDrops,TotalKills,DropRate,ConfidenceLower,ConfidenceUpper");

        foreach (var mob in mobTables)
        {
            foreach (var item in mob.Items)
            {
                await writer.WriteLineAsync(
                    $"\"{mob.MobName}\",\"{item.ItemName}\",{item.TotalDrops},{item.TotalKills}," +
                    $"{item.DropRate:F2},{item.ConfidenceLower:F2},{item.ConfidenceUpper:F2}");
            }
        }
    }

    private async Task ExportToJsonAsync(string filePath, IReadOnlyList<MobLootTable> mobTables)
    {
        var exportData = new
        {
            ExportDate = DateTime.Now,
            TotalMobs = mobTables.Count,
            TotalItems = mobTables.Sum(m => m.Items.Count),
            Mobs = mobTables.Select(m => new
            {
                m.MobName,
                m.TotalKills,
                m.FirstEncounter,
                m.LastEncounter,
                Items = m.Items.Select(i => new
                {
                    i.ItemName,
                    i.TotalDrops,
                    i.DropRate,
                    ConfidenceInterval = $"{i.ConfidenceLower:F1}% - {i.ConfidenceUpper:F1}%"
                })
            })
        };

        var json = System.Text.Json.JsonSerializer.Serialize(exportData, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true
        });

        await File.WriteAllTextAsync(filePath, json);
    }

    #endregion
}

#region Helper ViewModels

public class MobLootTableViewModel
{
    public string MobName { get; }
    public int TotalKills { get; }
    public int ItemCount { get; }
    public string CurrencyInfo { get; }
    public string LastEncounter { get; }
    public IReadOnlyList<ItemDropStatistic> Items { get; }

    public MobLootTableViewModel(MobLootTable table)
    {
        MobName = table.MobName;
        TotalKills = table.TotalKills;
        ItemCount = table.Items.Count;
        Items = table.Items;
        LastEncounter = table.LastEncounter.ToString("g");
        CurrencyInfo = table.CurrencyDrops.TotalDrops > 0
            ? $"{table.CurrencyDrops.AverageFormatted} avg"
            : "No currency";
    }
}

public class ItemDropViewModel
{
    public string ItemName { get; }
    public int TotalDrops { get; }
    public int TotalKills { get; }
    public string DropRateDisplay { get; }
    public string ConfidenceInterval { get; }

    public ItemDropViewModel(ItemDropStatistic stat)
    {
        ItemName = stat.ItemName;
        TotalDrops = stat.TotalDrops;
        TotalKills = stat.TotalKills;
        DropRateDisplay = $"{stat.DropRate:F1}%";
        ConfidenceInterval = $"({stat.ConfidenceLower:F1}% - {stat.ConfidenceUpper:F1}%)";
    }
}

public class LootSessionViewModel
{
    public string SessionDate { get; }
    public string Duration { get; }
    public int ItemDrops { get; }
    public string Currency { get; }
    public int UniqueMobs { get; }

    public LootSessionViewModel(LootSessionSummary summary)
    {
        SessionDate = summary.SessionStart.ToString("g");
        Duration = summary.Duration.TotalMinutes > 1
            ? $"{summary.Duration.TotalMinutes:F0}m"
            : $"{summary.Duration.TotalSeconds:F0}s";
        ItemDrops = summary.TotalItemDrops;
        Currency = summary.TotalCurrencyFormatted;
        UniqueMobs = summary.UniqueMobsKilled;
    }
}

#endregion
