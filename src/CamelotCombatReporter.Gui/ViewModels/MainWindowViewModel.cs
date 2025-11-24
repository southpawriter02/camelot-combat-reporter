using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using CamelotCombatReporter.Core.Models;
using CamelotCombatReporter.Core.Parsing;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CamelotCombatReporter.Gui.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _selectedLogFile = "No file selected";

    [ObservableProperty]
    private string _combatantName = "You";

    [ObservableProperty]
    private bool _hasAnalyzedData = false;

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

    [RelayCommand]
    private async Task SelectLogFile()
    {
        var topLevel = Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
            ? desktop.MainWindow
            : null;

        if (topLevel == null) return;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select Combat Log File",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("Log Files")
                {
                    Patterns = new[] { "*.log", "*.txt" }
                },
                new FilePickerFileType("All Files")
                {
                    Patterns = new[] { "*" }
                }
            }
        });

        if (files.Count > 0)
        {
            SelectedLogFile = files[0].Path.LocalPath;
            await AnalyzeLog();
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

            // Analysis
            var firstEventTime = events.First().Timestamp;
            var lastEventTime = events.Last().Timestamp;
            var duration = lastEventTime - firstEventTime;

            var combatStyleCount = events.OfType<CombatStyleEvent>().Count();
            var spellCastCount = events.OfType<SpellCastEvent>().Count();

            var damageDealtEvents = events.OfType<DamageEvent>().Where(e => e.Source == CombatantName).ToList();
            var totalDamageDealt = damageDealtEvents.Sum(e => e.DamageAmount);
            var damageDealtAmounts = damageDealtEvents.Select(e => e.DamageAmount).OrderBy(d => d).ToList();

            double damageMedian = 0;
            if (damageDealtAmounts.Count > 0)
            {
                var mid = damageDealtAmounts.Count / 2;
                damageMedian = (damageDealtAmounts.Count % 2 != 0)
                    ? damageDealtAmounts[mid]
                    : (damageDealtAmounts[mid - 1] + damageDealtAmounts[mid]) / 2.0;
            }

            var damageAverage = damageDealtEvents.Count > 0 ? totalDamageDealt / (double)damageDealtEvents.Count : 0;
            var dps = duration.TotalSeconds > 0 ? totalDamageDealt / duration.TotalSeconds : 0;

            // Update properties
            LogDuration = duration.TotalMinutes.ToString("F2");
            TotalDamageDealt = totalDamageDealt;
            DamagePerSecond = dps.ToString("F2");
            AverageDamage = damageAverage.ToString("F2");
            MedianDamage = damageMedian.ToString("F2");
            CombatStylesUsed = combatStyleCount;
            SpellsCast = spellCastCount;
            HasAnalyzedData = true;
        });
    }
}
