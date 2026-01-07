using System;
using System.Collections.ObjectModel;
using Avalonia.Media;
using CamelotCombatReporter.Core.CharacterBuilding.Models;
using CamelotCombatReporter.Core.CharacterBuilding.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CamelotCombatReporter.Gui.CharacterBuilding.ViewModels;

/// <summary>
/// ViewModel for the progression chart view.
/// </summary>
public partial class ProgressionChartViewModel : ObservableObject
{
    [ObservableProperty]
    private ProgressionSummary _summary = new();

    public ObservableCollection<MilestoneViewModel> Milestones { get; } = [];

    public bool HasMilestones => Milestones.Count > 0;
    public bool HasNextRankEstimate => Summary.EstimatedTimeToNextRank.HasValue;

    public string CurrentRankDisplay => $"RR{Summary.CurrentRank}";
    public string TotalRpDisplay => FormatNumber(Summary.TotalRealmPoints);
    public int MilestoneCount => Summary.MilestoneCount;
    public string AvgRpPerSessionDisplay => FormatNumber((long)Summary.AverageRpPerSession);
    public string AvgDaysBetweenRanksDisplay => Summary.AverageDaysBetweenRanks > 0 
        ? $"{Summary.AverageDaysBetweenRanks:F1} days" 
        : "N/A";

    public string DpsTrendDisplay => FormatTrend(Summary.DpsTrend);
    public string KdTrendDisplay => FormatTrend(Summary.KdTrend);
    public IBrush DpsTrendColor => GetTrendColor(Summary.DpsTrend);
    public IBrush KdTrendColor => GetTrendColor(Summary.KdTrend);

    public string EstimatedTimeToNextDisplay
    {
        get
        {
            if (!Summary.EstimatedTimeToNextRank.HasValue) return "";
            var est = Summary.EstimatedTimeToNextRank.Value;
            if (est.TotalDays >= 1) return $"Est. {est.TotalDays:F0} days to next rank";
            if (est.TotalHours >= 1) return $"Est. {est.TotalHours:F0} hours to next rank";
            return "Almost there!";
        }
    }

    public void LoadProgression(RealmRankProgression progression, ProgressionSummary summary)
    {
        Summary = summary;
        Milestones.Clear();

        foreach (var milestone in progression.Milestones)
        {
            Milestones.Add(new MilestoneViewModel(milestone));
        }

        NotifyAllChanged();
    }

    private void NotifyAllChanged()
    {
        OnPropertyChanged(nameof(HasMilestones));
        OnPropertyChanged(nameof(HasNextRankEstimate));
        OnPropertyChanged(nameof(CurrentRankDisplay));
        OnPropertyChanged(nameof(TotalRpDisplay));
        OnPropertyChanged(nameof(MilestoneCount));
        OnPropertyChanged(nameof(AvgRpPerSessionDisplay));
        OnPropertyChanged(nameof(AvgDaysBetweenRanksDisplay));
        OnPropertyChanged(nameof(DpsTrendDisplay));
        OnPropertyChanged(nameof(KdTrendDisplay));
        OnPropertyChanged(nameof(DpsTrendColor));
        OnPropertyChanged(nameof(KdTrendColor));
        OnPropertyChanged(nameof(EstimatedTimeToNextDisplay));
    }

    private static string FormatNumber(long value) => value switch
    {
        >= 1_000_000 => $"{value / 1_000_000.0:F2}M",
        >= 1_000 => $"{value / 1_000.0:F1}K",
        _ => value.ToString("N0")
    };

    private static string FormatTrend(double value) => value switch
    {
        > 0 => $"↑ +{value:F1}",
        < 0 => $"↓ {value:F1}",
        _ => "→ Stable"
    };

    private static IBrush GetTrendColor(double value) => value switch
    {
        > 0 => Brushes.LimeGreen,
        < 0 => Brushes.Crimson,
        _ => Brushes.White
    };
}

/// <summary>
/// ViewModel for individual milestone display.
/// </summary>
public class MilestoneViewModel(RankMilestone milestone)
{
    public string RankDisplay => $"RR{milestone.RealmRank}";
    public string DateDisplay => milestone.AchievedUtc.ToString("MMM d, yyyy");
    public string RpDisplay => $"{milestone.RealmPoints:N0} RP";
    public string StatsDisplay => $"DPS: {milestone.AverageDps:F0} | K/D: {milestone.KillDeathRatio:F2}";
    public string SessionsDisplay => $"{milestone.SessionCount} sessions";
}
