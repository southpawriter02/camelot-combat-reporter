using System;
using System.Collections.ObjectModel;
using System.Linq;
using CamelotCombatReporter.Core.DeathAnalysis.Models;
using CamelotCombatReporter.Gui.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;

namespace CamelotCombatReporter.Gui.DeathAnalysis.ViewModels;

/// <summary>
/// ViewModel for displaying a single death report.
/// </summary>
public partial class DeathReportViewModel : ViewModelBase
{
    private readonly DeathReport _report;

    public DeathReportViewModel()
    {
        // Design-time constructor
        _report = null!;
    }

    public DeathReportViewModel(DeathReport report)
    {
        _report = report;
        InitializeChartData();
        InitializeDamageSources();
    }

    /// <summary>
    /// Gets the underlying report.
    /// </summary>
    public DeathReport Report => _report;

    /// <summary>
    /// Gets the death timestamp.
    /// </summary>
    public string Timestamp => _report.DeathEvent.Timestamp.ToString("HH:mm:ss");

    /// <summary>
    /// Gets the death category.
    /// </summary>
    public string Category => FormatCategory(_report.Category);

    /// <summary>
    /// Gets the time to death formatted.
    /// </summary>
    public string TimeToDeath => $"{_report.TimeToDeath.TotalSeconds:F1}s";

    /// <summary>
    /// Gets the total damage taken.
    /// </summary>
    public string TotalDamage => _report.TotalDamageTaken.ToString("N0");

    /// <summary>
    /// Gets the total healing received.
    /// </summary>
    public string TotalHealing => _report.TotalHealingReceived.ToString("N0");

    /// <summary>
    /// Gets the number of attackers.
    /// </summary>
    public int AttackerCount => _report.AttackerCount;

    /// <summary>
    /// Gets whether the player was CC'd.
    /// </summary>
    public bool WasCrowdControlled => _report.WasCrowdControlled;

    /// <summary>
    /// Gets the killing blow attacker name.
    /// </summary>
    public string KillerName => _report.KillingBlow?.AttackerName ?? "Unknown";

    /// <summary>
    /// Gets the killing blow ability.
    /// </summary>
    public string KillingAbility => _report.KillingBlow?.AbilityName ?? "Unknown";

    /// <summary>
    /// Gets the killing blow damage.
    /// </summary>
    public int KillingDamage => _report.KillingBlow?.DamageAmount ?? 0;

    /// <summary>
    /// Gets the color for the category badge.
    /// </summary>
    public string CategoryColor => GetCategoryColor(_report.Category);

    /// <summary>
    /// Gets the number of recommendations.
    /// </summary>
    public int RecommendationCount => _report.Recommendations.Count;

    /// <summary>
    /// Gets the alias property for CC status used in views.
    /// </summary>
    public bool WasUnderCC => WasCrowdControlled;

    /// <summary>
    /// Gets the killer class.
    /// </summary>
    public string KillerClass => _report.KillingBlow?.AttackerClass?.ToString() ?? "Unknown";

    /// <summary>
    /// Gets the killing blow ability name.
    /// </summary>
    public string KillingBlowAbility => _report.KillingBlow?.AbilityName ?? "Unknown";

    #region Chart Properties

    /// <summary>
    /// Chart series for damage timeline.
    /// </summary>
    [ObservableProperty]
    private ISeries[] _damageTimelineSeries = Array.Empty<ISeries>();

    /// <summary>
    /// X-axis configuration for timeline chart.
    /// </summary>
    [ObservableProperty]
    private Axis[] _timelineXAxes = Array.Empty<Axis>();

    #endregion

    #region Damage Sources Collection

    /// <summary>
    /// Collection of damage sources for the DataGrid.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<DamageSourceViewModel> _damageSources = new();

    #endregion

    #region Commands

    /// <summary>
    /// Command to close the dialog.
    /// </summary>
    [RelayCommand]
    private void Close()
    {
        // This will be handled by the view
    }

    #endregion

    #region Initialization

    private void InitializeChartData()
    {
        if (_report?.DamageTimeline == null || !_report.DamageTimeline.Any())
            return;

        var values = _report.DamageTimeline
            .OrderBy(b => b.SecondOffset)
            .Select(b => (double)b.TotalDamage)
            .ToArray();

        DamageTimelineSeries = new ISeries[]
        {
            new ColumnSeries<double>
            {
                Values = values,
                Name = "Damage",
                Fill = new SolidColorPaint(new SKColor(244, 67, 54))
            }
        };

        TimelineXAxes = new Axis[]
        {
            new Axis
            {
                Name = "Seconds Before Death",
                Labels = _report.DamageTimeline
                    .OrderBy(b => b.SecondOffset)
                    .Select(b => b.SecondOffset.ToString())
                    .ToArray()
            }
        };
    }

    private void InitializeDamageSources()
    {
        if (_report?.DamageSources == null)
            return;

        var totalDamage = _report.TotalDamageTaken;
        foreach (var source in _report.DamageSources.OrderByDescending(s => s.TotalDamage))
        {
            var percent = totalDamage > 0
                ? (double)source.TotalDamage / totalDamage * 100
                : 0;

            DamageSources.Add(new DamageSourceViewModel(source, percent));
        }
    }

    #endregion

    private static string FormatCategory(DeathCategory category)
    {
        return category switch
        {
            DeathCategory.BurstAlphaStrike => "Alpha Strike",
            DeathCategory.BurstCoordinated => "Coordinated",
            DeathCategory.BurstCCChain => "CC Chain",
            DeathCategory.AttritionHealingDeficit => "Healing Deficit",
            DeathCategory.AttritionResourceExhaustion => "Resources",
            DeathCategory.AttritionPositional => "Positioning",
            DeathCategory.ExecutionLowHealth => "Execution",
            DeathCategory.ExecutionDoT => "DoT",
            DeathCategory.Environmental => "Environmental",
            _ => "Unknown"
        };
    }

    private static string GetCategoryColor(DeathCategory category)
    {
        return category switch
        {
            // Burst deaths - Red
            DeathCategory.BurstAlphaStrike or
            DeathCategory.BurstCoordinated or
            DeathCategory.BurstCCChain => "#F44336",

            // Attrition deaths - Orange
            DeathCategory.AttritionHealingDeficit or
            DeathCategory.AttritionResourceExhaustion or
            DeathCategory.AttritionPositional => "#FF9800",

            // Execution deaths - Yellow
            DeathCategory.ExecutionLowHealth or
            DeathCategory.ExecutionDoT => "#FFEB3B",

            // Environmental - Gray
            DeathCategory.Environmental => "#9E9E9E",

            // Unknown - Gray
            _ => "#757575"
        };
    }
}

/// <summary>
/// ViewModel for displaying a damage source in the DataGrid.
/// </summary>
public class DamageSourceViewModel
{
    private readonly DamageSource _source;
    private readonly double _percent;

    public DamageSourceViewModel(DamageSource source, double percent)
    {
        _source = source;
        _percent = percent;
    }

    public string SourceName => _source.AttackerName;
    public string SourceClass => _source.AttackerClass?.ToString() ?? "Unknown";
    public string TotalDamage => _source.TotalDamage.ToString("N0");
    public string DamagePercent => $"{_percent:F1}%";
    public int HitCount => _source.Events.Count;
}
