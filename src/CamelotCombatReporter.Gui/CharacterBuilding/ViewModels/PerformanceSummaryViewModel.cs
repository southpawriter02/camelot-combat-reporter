using Avalonia.Media;
using CamelotCombatReporter.Core.CharacterBuilding.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CamelotCombatReporter.Gui.CharacterBuilding.ViewModels;

/// <summary>
/// ViewModel for the performance summary display component.
/// </summary>
public partial class PerformanceSummaryViewModel : ObservableObject
{
    [ObservableProperty]
    private BuildPerformanceMetrics _metrics = BuildPerformanceMetrics.Empty;

    // Computed display properties
    public bool HasData => Metrics.SessionCount > 0;
    public bool HasHealingData => Metrics.TotalHealingDone > 0;
    
    public string SessionCountDisplay => Metrics.SessionCount == 1 
        ? "1 session" 
        : $"{Metrics.SessionCount} sessions";
    
    public string CombatTimeDisplay
    {
        get
        {
            var time = Metrics.TotalCombatTime;
            if (time.TotalHours >= 1)
                return $"{time.TotalHours:F1}h";
            if (time.TotalMinutes >= 1)
                return $"{time.TotalMinutes:F0}m";
            return $"{time.TotalSeconds:F0}s";
        }
    }
    
    public string AverageDpsDisplay => FormatNumber(Metrics.AverageDps);
    public string PeakDpsDisplay => FormatNumber(Metrics.PeakDps);
    public string AverageHpsDisplay => FormatNumber(Metrics.AverageHps);
    
    public int Kills => Metrics.Kills;
    public int Deaths => Metrics.Deaths;
    public int Assists => Metrics.Assists;
    
    public string KdRatioDisplay => Metrics.KillDeathRatio.ToString("F2");
    
    public IBrush KdRatioColor => Metrics.KillDeathRatio switch
    {
        >= 2.0 => Brushes.LimeGreen,
        >= 1.0 => Brushes.White,
        _ => Brushes.Crimson
    };
    
    public string TotalDamageDealtDisplay => FormatLargeNumber(Metrics.TotalDamageDealt);
    public string TotalDamageTakenDisplay => FormatLargeNumber(Metrics.TotalDamageTaken);
    public string TotalHealingDisplay => FormatLargeNumber(Metrics.TotalHealingDone);

    public PerformanceSummaryViewModel()
    {
    }

    public PerformanceSummaryViewModel(BuildPerformanceMetrics metrics)
    {
        _metrics = metrics;
    }

    public void UpdateMetrics(BuildPerformanceMetrics metrics)
    {
        Metrics = metrics;
        
        // Notify all computed properties
        OnPropertyChanged(nameof(HasData));
        OnPropertyChanged(nameof(HasHealingData));
        OnPropertyChanged(nameof(SessionCountDisplay));
        OnPropertyChanged(nameof(CombatTimeDisplay));
        OnPropertyChanged(nameof(AverageDpsDisplay));
        OnPropertyChanged(nameof(PeakDpsDisplay));
        OnPropertyChanged(nameof(AverageHpsDisplay));
        OnPropertyChanged(nameof(Kills));
        OnPropertyChanged(nameof(Deaths));
        OnPropertyChanged(nameof(Assists));
        OnPropertyChanged(nameof(KdRatioDisplay));
        OnPropertyChanged(nameof(KdRatioColor));
        OnPropertyChanged(nameof(TotalDamageDealtDisplay));
        OnPropertyChanged(nameof(TotalDamageTakenDisplay));
        OnPropertyChanged(nameof(TotalHealingDisplay));
    }

    private static string FormatNumber(double value)
    {
        return value switch
        {
            >= 1000 => $"{value / 1000:F1}k",
            >= 100 => $"{value:F0}",
            _ => $"{value:F1}"
        };
    }

    private static string FormatLargeNumber(long value)
    {
        return value switch
        {
            >= 1_000_000 => $"{value / 1_000_000.0:F2}M",
            >= 1_000 => $"{value / 1_000.0:F1}K",
            _ => value.ToString("N0")
        };
    }
}
