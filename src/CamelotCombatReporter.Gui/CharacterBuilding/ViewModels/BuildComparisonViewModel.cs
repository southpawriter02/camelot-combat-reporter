using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Media;
using CamelotCombatReporter.Core.CharacterBuilding.Models;
using CamelotCombatReporter.Core.CharacterBuilding.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CamelotCombatReporter.Gui.CharacterBuilding.ViewModels;

/// <summary>
/// ViewModel for build comparison functionality.
/// </summary>
public partial class BuildComparisonViewModel : ObservableObject
{
    private readonly IBuildComparisonService _comparisonService;

    [ObservableProperty]
    private CharacterBuild? _selectedBuildA;

    [ObservableProperty]
    private CharacterBuild? _selectedBuildB;

    [ObservableProperty]
    private BuildComparisonResult? _comparisonResult;

    public ObservableCollection<CharacterBuild> AvailableBuilds { get; } = [];
    public ObservableCollection<SpecDeltaViewModel> SpecDeltas { get; } = [];
    public ObservableCollection<RADeltaViewModel> RADeltas { get; } = [];

    public bool HasComparison => ComparisonResult != null;
    public bool HasRADeltas => RADeltas.Count > 0;
    public bool HasPerformanceDeltas => ComparisonResult?.PerformanceDeltas != null;

    public string SummaryText => ComparisonResult?.AreIdentical == true
        ? "Builds are identical"
        : $"Comparing {SelectedBuildA?.Name} → {SelectedBuildB?.Name}";

    public string SpecPointsDeltaDisplay => FormatDelta(ComparisonResult?.TotalSpecPointsDelta ?? 0);
    public string RAPointsDeltaDisplay => FormatDelta(ComparisonResult?.TotalRAPointsDelta ?? 0);
    
    public IBrush SpecPointsDeltaColor => GetDeltaColor(ComparisonResult?.TotalSpecPointsDelta ?? 0);
    public IBrush RAPointsDeltaColor => GetDeltaColor(ComparisonResult?.TotalRAPointsDelta ?? 0);

    public string DpsDeltaDisplay => FormatDelta(ComparisonResult?.PerformanceDeltas?.DpsDelta ?? 0, "F1");
    public string HpsDeltaDisplay => FormatDelta(ComparisonResult?.PerformanceDeltas?.HpsDelta ?? 0, "F1");
    public string KdDeltaDisplay => FormatDelta(ComparisonResult?.PerformanceDeltas?.KdRatioDelta ?? 0, "F2");

    public BuildComparisonViewModel(IBuildComparisonService comparisonService)
    {
        _comparisonService = comparisonService;
    }

    public void LoadBuilds(IEnumerable<CharacterBuild> builds)
    {
        AvailableBuilds.Clear();
        foreach (var build in builds)
        {
            AvailableBuilds.Add(build);
        }
    }

    partial void OnSelectedBuildAChanged(CharacterBuild? value) => UpdateComparison();
    partial void OnSelectedBuildBChanged(CharacterBuild? value) => UpdateComparison();

    private void UpdateComparison()
    {
        SpecDeltas.Clear();
        RADeltas.Clear();

        if (SelectedBuildA == null || SelectedBuildB == null)
        {
            ComparisonResult = null;
            NotifyComparisonChanged();
            return;
        }

        ComparisonResult = _comparisonService.CompareBuilds(SelectedBuildA, SelectedBuildB);

        // Populate spec deltas (only show ones with changes)
        foreach (var delta in ComparisonResult.SpecDeltas.Where(d => d.Delta != 0))
        {
            SpecDeltas.Add(new SpecDeltaViewModel(delta));
        }

        // Populate RA deltas
        foreach (var delta in ComparisonResult.RealmAbilityDeltas)
        {
            RADeltas.Add(new RADeltaViewModel(delta));
        }

        NotifyComparisonChanged();
    }

    private void NotifyComparisonChanged()
    {
        OnPropertyChanged(nameof(HasComparison));
        OnPropertyChanged(nameof(HasRADeltas));
        OnPropertyChanged(nameof(HasPerformanceDeltas));
        OnPropertyChanged(nameof(SummaryText));
        OnPropertyChanged(nameof(SpecPointsDeltaDisplay));
        OnPropertyChanged(nameof(RAPointsDeltaDisplay));
        OnPropertyChanged(nameof(SpecPointsDeltaColor));
        OnPropertyChanged(nameof(RAPointsDeltaColor));
        OnPropertyChanged(nameof(DpsDeltaDisplay));
        OnPropertyChanged(nameof(HpsDeltaDisplay));
        OnPropertyChanged(nameof(KdDeltaDisplay));
    }

    private static string FormatDelta(int value) =>
        value > 0 ? $"+{value}" : value.ToString();

    private static string FormatDelta(double value, string format) =>
        value > 0 ? $"+{value.ToString(format)}" : value.ToString(format);

    private static IBrush GetDeltaColor(int value) => value switch
    {
        > 0 => Brushes.LimeGreen,
        < 0 => Brushes.Crimson,
        _ => Brushes.White
    };
}

/// <summary>
/// ViewModel wrapper for SpecDelta with display properties.
/// </summary>
public class SpecDeltaViewModel(SpecDelta delta)
{
    public string SpecName => delta.SpecName;
    public int ValueA => delta.ValueA;
    public int ValueB => delta.ValueB;
    public int Delta => delta.Delta;
    public string DeltaDisplay => Delta > 0 ? $"+{Delta}" : Delta.ToString();
    public IBrush DeltaColor => Delta > 0 ? Brushes.LimeGreen : Brushes.Crimson;
}

/// <summary>
/// ViewModel wrapper for RealmAbilityDelta with display properties.
/// </summary>
public class RADeltaViewModel(RealmAbilityDelta delta)
{
    public string AbilityName => delta.AbilityName;
    public RealmAbilityChangeType ChangeType => delta.ChangeType;
    
    public string ChangeTypeIcon => ChangeType switch
    {
        RealmAbilityChangeType.Added => "➕",
        RealmAbilityChangeType.Removed => "➖",
        RealmAbilityChangeType.RankChanged => "↔",
        _ => "?"
    };

    public string RankDisplay => ChangeType switch
    {
        RealmAbilityChangeType.Added => $"Rank {delta.RankB}",
        RealmAbilityChangeType.Removed => $"Rank {delta.RankA}",
        RealmAbilityChangeType.RankChanged => $"Rank {delta.RankA} → {delta.RankB}",
        _ => ""
    };

    public string PointsDisplay
    {
        get
        {
            var pointsDelta = delta.PointsB - delta.PointsA;
            return pointsDelta > 0 ? $"+{pointsDelta} pts" : $"{pointsDelta} pts";
        }
    }

    public IBrush PointsDeltaColor
    {
        get
        {
            var pointsDelta = delta.PointsB - delta.PointsA;
            return pointsDelta > 0 ? Brushes.Orange : Brushes.LimeGreen;
        }
    }
}
