using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using System.IO;
using CamelotCombatReporter.Core.Exporting;
using CamelotCombatReporter.Core.Models;
using CamelotCombatReporter.Core.Parsing;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;

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

    private List<LogEvent>? _analyzedEvents;
    private CombatStatistics? _currentStatistics;

    [ObservableProperty]
    private ISeries[] _series = Array.Empty<ISeries>();

    [ObservableProperty]
    private Axis[] _xAxes = Array.Empty<Axis>();

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

            _currentStatistics = new CombatStatistics(
                duration.TotalMinutes,
                totalDamageDealt,
                dps,
                damageAverage,
                damageMedian,
                combatStyleCount,
                spellCastCount
            );
            _analyzedEvents = events;

            // Generate Chart Data
            var chartSeries = Array.Empty<ISeries>();
            var chartXAxes = Array.Empty<Axis>();

            if (damageDealtEvents.Any())
            {
                var intervalSeconds = 5;
                var totalDurationSeconds = (int)duration.TotalSeconds;
                var bucketCount = (totalDurationSeconds / intervalSeconds) + 1;

                var points = new List<double>();
                var labels = new List<string>();

                for (int i = 0; i < bucketCount; i++)
                {
                    var intervalStart = firstEventTime.Add(TimeSpan.FromSeconds(i * intervalSeconds));
                    var intervalEnd = intervalStart.Add(TimeSpan.FromSeconds(intervalSeconds));

                    var damageInInterval = damageDealtEvents
                        .Where(e => e.Timestamp >= intervalStart && e.Timestamp < intervalEnd)
                        .Sum(e => e.DamageAmount);

                    points.Add(damageInInterval);
                    labels.Add($"{(i * intervalSeconds)}s");
                }

                chartSeries = new ISeries[]
                {
                    new LineSeries<double>
                    {
                        Values = points,
                        Name = "Damage Output (5s intervals)",
                        Fill = null,
                        GeometrySize = 5,
                        LineSmoothness = 0.5
                    }
                };

                chartXAxes = new Axis[]
                {
                    new Axis
                    {
                        Name = "Time",
                        Labels = labels,
                        MinStep = 1, // Ensure integer steps if using index
                        ForceStepToMin = false
                    }
                };
            }

            // Update properties
            LogDuration = duration.TotalMinutes.ToString("F2");
            TotalDamageDealt = totalDamageDealt;
            DamagePerSecond = dps.ToString("F2");
            AverageDamage = damageAverage.ToString("F2");
            MedianDamage = damageMedian.ToString("F2");
            CombatStylesUsed = combatStyleCount;
            SpellsCast = spellCastCount;

            Series = chartSeries;
            XAxes = chartXAxes;

            HasAnalyzedData = true;
        });
    }

    [RelayCommand]
    private async Task ExportCsv()
    {
        if (_analyzedEvents == null || _currentStatistics == null) return;

        var topLevel = Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
            ? desktop.MainWindow
            : null;

        if (topLevel == null) return;

        var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Export Analysis to CSV",
            DefaultExtension = "csv",
            FileTypeChoices = new[]
            {
                new FilePickerFileType("CSV Files") { Patterns = new[] { "*.csv" } }
            }
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
}
