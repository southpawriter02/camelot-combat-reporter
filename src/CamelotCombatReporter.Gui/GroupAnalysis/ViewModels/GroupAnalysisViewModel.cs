using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using CamelotCombatReporter.Core.GroupAnalysis;
using CamelotCombatReporter.Core.GroupAnalysis.Models;
using CamelotCombatReporter.Core.Models;
using CamelotCombatReporter.Core.Parsing;
using CamelotCombatReporter.Gui.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;

namespace CamelotCombatReporter.Gui.GroupAnalysis.ViewModels;

public partial class GroupAnalysisViewModel : ViewModelBase
{
    private readonly IGroupAnalysisService _analysisService;

    #region Observable Properties

    [ObservableProperty]
    private bool _hasData;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _statusMessage = "Load a combat log to analyze group composition";

    // Composition
    [ObservableProperty]
    private int _memberCount;

    [ObservableProperty]
    private string _sizeCategory = "—";

    [ObservableProperty]
    private double _balanceScore;

    [ObservableProperty]
    private string _matchedTemplate = "None";

    [ObservableProperty]
    private ObservableCollection<GroupMemberViewModel> _groupMembers = new();

    // Metrics
    [ObservableProperty]
    private string _totalDps = "0";

    [ObservableProperty]
    private string _totalHps = "0";

    [ObservableProperty]
    private int _totalKills;

    [ObservableProperty]
    private int _totalDeaths;

    [ObservableProperty]
    private string _killDeathRatio = "0.00";

    [ObservableProperty]
    private string _combatDuration = "00:00";

    // Role Coverage
    [ObservableProperty]
    private ObservableCollection<RoleCoverageViewModel> _roleCoverage = new();

    // Recommendations
    [ObservableProperty]
    private ObservableCollection<RecommendationViewModel> _recommendations = new();

    // Charts
    [ObservableProperty]
    private ISeries[] _roleDistributionSeries = Array.Empty<ISeries>();

    [ObservableProperty]
    private ISeries[] _contributionSeries = Array.Empty<ISeries>();

    [ObservableProperty]
    private Axis[] _contributionXAxes = Array.Empty<Axis>();

    [ObservableProperty]
    private Axis[] _contributionYAxes = new Axis[]
    {
        new Axis { Name = "Percentage (%)", MinLimit = 0, MaxLimit = 100 }
    };

    // Manual member input
    [ObservableProperty]
    private string _newMemberName = string.Empty;

    [ObservableProperty]
    private CharacterClass? _newMemberClass;

    [ObservableProperty]
    private ObservableCollection<CharacterClass> _availableClasses = new();

    #endregion

    public GroupAnalysisViewModel()
    {
        _analysisService = new GroupAnalysisService();
        InitializeAvailableClasses();
    }

    public GroupAnalysisViewModel(IGroupAnalysisService analysisService)
    {
        _analysisService = analysisService;
        InitializeAvailableClasses();
    }

    private void InitializeAvailableClasses()
    {
        foreach (var characterClass in Enum.GetValues<CharacterClass>())
        {
            if (characterClass != CharacterClass.Unknown)
            {
                AvailableClasses.Add(characterClass);
            }
        }
    }

    [RelayCommand]
    private async Task AnalyzeFromFile(Window window)
    {
        var files = await window.StorageProvider.OpenFilePickerAsync(
            new FilePickerOpenOptions
            {
                Title = "Select Combat Log",
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("Log Files") { Patterns = new[] { "*.log", "*.txt" } },
                    new FilePickerFileType("All Files") { Patterns = new[] { "*" } }
                }
            });

        if (files.Count > 0)
        {
            await AnalyzeLogFile(files[0].Path.LocalPath);
        }
    }

    public async Task AnalyzeLogFile(string filePath)
    {
        IsLoading = true;
        StatusMessage = "Analyzing combat log...";

        await Task.Run(() =>
        {
            try
            {
                var parser = new LogParser(filePath);
                var events = parser.Parse().ToList();

                if (events.Count == 0)
                {
                    Dispatcher.UIThread.Post(() =>
                    {
                        StatusMessage = "No events found in log file";
                        IsLoading = false;
                    });
                    return;
                }

                var summary = _analysisService.PerformFullAnalysis(events);

                Dispatcher.UIThread.Post(() =>
                {
                    UpdateUI(summary);
                    HasData = true;
                    IsLoading = false;
                    StatusMessage = $"Analyzed {events.Count} events";
                });
            }
            catch (Exception ex)
            {
                Dispatcher.UIThread.Post(() =>
                {
                    StatusMessage = $"Error: {ex.Message}";
                    IsLoading = false;
                });
            }
        });
    }

    public void AnalyzeEvents(IEnumerable<LogEvent> events)
    {
        var summary = _analysisService.PerformFullAnalysis(events);
        UpdateUI(summary);
        HasData = true;
    }

    private void UpdateUI(GroupAnalysisSummary summary)
    {
        // Update composition info
        MemberCount = summary.Composition.MemberCount;
        SizeCategory = summary.Composition.SizeCategory.GetDisplayName();
        BalanceScore = summary.Composition.BalanceScore;
        MatchedTemplate = summary.Composition.MatchedTemplate?.Name ?? "No match";

        // Update members list
        GroupMembers.Clear();
        foreach (var member in summary.Composition.Members)
        {
            GroupMembers.Add(new GroupMemberViewModel(member));
        }

        // Update metrics
        TotalDps = summary.Metrics.TotalDps.ToString("F1");
        TotalHps = summary.Metrics.TotalHps.ToString("F1");
        TotalKills = summary.Metrics.TotalKills;
        TotalDeaths = summary.Metrics.TotalDeaths;
        KillDeathRatio = summary.Metrics.KillDeathRatio.ToString("F2");
        CombatDuration = summary.Metrics.CombatDuration.ToString(@"mm\:ss");

        // Update role coverage
        RoleCoverage.Clear();
        foreach (var coverage in summary.RoleCoverage.Where(c => c.Role != GroupRole.Unknown))
        {
            RoleCoverage.Add(new RoleCoverageViewModel(coverage));
        }

        // Update recommendations
        Recommendations.Clear();
        foreach (var rec in summary.Recommendations)
        {
            Recommendations.Add(new RecommendationViewModel(rec));
        }

        // Update charts
        GenerateRoleDistributionChart(summary.Composition);
        GenerateContributionChart(summary.Metrics);
    }

    private void GenerateRoleDistributionChart(GroupComposition composition)
    {
        var roleGroups = composition.Members
            .Where(m => m.PrimaryRole != GroupRole.Unknown)
            .GroupBy(m => m.PrimaryRole)
            .OrderByDescending(g => g.Count())
            .ToList();

        RoleDistributionSeries = roleGroups.Select(g => new PieSeries<int>
        {
            Values = new[] { g.Count() },
            Name = g.Key.GetDisplayName(),
            Fill = new SolidColorPaint(SKColor.Parse(g.Key.GetColorHex()))
        } as ISeries).ToArray();
    }

    private void GenerateContributionChart(GroupPerformanceMetrics metrics)
    {
        if (metrics.MemberContributions.Count == 0)
        {
            ContributionSeries = Array.Empty<ISeries>();
            ContributionXAxes = Array.Empty<Axis>();
            return;
        }

        var members = metrics.MemberContributions.Values
            .OrderByDescending(c => c.DpsContributionPercent)
            .Take(8)
            .ToList();

        var dpsValues = members.Select(c => c.DpsContributionPercent).ToArray();
        var hpsValues = members.Select(c => c.HpsContributionPercent).ToArray();
        var labels = members.Select(c => c.MemberName.Length > 10 ? c.MemberName[..10] : c.MemberName).ToArray();

        ContributionSeries = new ISeries[]
        {
            new ColumnSeries<double>
            {
                Values = dpsValues,
                Name = "DPS %",
                Fill = new SolidColorPaint(new SKColor(244, 67, 54))
            },
            new ColumnSeries<double>
            {
                Values = hpsValues,
                Name = "HPS %",
                Fill = new SolidColorPaint(new SKColor(33, 150, 243))
            }
        };

        ContributionXAxes = new Axis[]
        {
            new Axis
            {
                Labels = labels,
                LabelsRotation = 45
            }
        };
    }

    [RelayCommand]
    private void AddManualMember()
    {
        if (string.IsNullOrWhiteSpace(NewMemberName))
            return;

        _analysisService.AddManualMember(NewMemberName.Trim(), NewMemberClass);

        // Add to the list immediately
        var member = new GroupMember(
            Name: NewMemberName.Trim(),
            Class: NewMemberClass,
            Realm: NewMemberClass?.GetRealm(),
            PrimaryRole: NewMemberClass.HasValue
                ? new RoleClassificationService().GetPrimaryRole(NewMemberClass.Value)
                : GroupRole.Unknown,
            SecondaryRole: NewMemberClass.HasValue
                ? new RoleClassificationService().GetSecondaryRole(NewMemberClass.Value)
                : null,
            Source: GroupMemberSource.Manual,
            FirstSeen: TimeOnly.MinValue,
            LastSeen: null,
            IsPlayer: false
        );

        GroupMembers.Add(new GroupMemberViewModel(member));

        // Clear inputs
        NewMemberName = string.Empty;
        NewMemberClass = null;

        StatusMessage = $"Added manual member: {member.Name}";
    }

    [RelayCommand]
    private void RemoveMember(GroupMemberViewModel? member)
    {
        if (member == null || member.IsPlayer)
            return;

        _analysisService.RemoveManualMember(member.Name);
        GroupMembers.Remove(member);
        StatusMessage = $"Removed member: {member.Name}";
    }

    [RelayCommand]
    private void ClearManualMembers()
    {
        _analysisService.ClearManualMembers();

        // Remove all manual members from the list
        var manualMembers = GroupMembers.Where(m => m.Source == "Manual").ToList();
        foreach (var member in manualMembers)
        {
            GroupMembers.Remove(member);
        }

        StatusMessage = "Cleared all manual members";
    }

    [RelayCommand]
    private void Reset()
    {
        _analysisService.Reset();
        HasData = false;
        MemberCount = 0;
        SizeCategory = "—";
        BalanceScore = 0;
        MatchedTemplate = "None";
        GroupMembers.Clear();
        RoleCoverage.Clear();
        Recommendations.Clear();
        RoleDistributionSeries = Array.Empty<ISeries>();
        ContributionSeries = Array.Empty<ISeries>();
        StatusMessage = "Load a combat log to analyze group composition";
    }
}

public class GroupMemberViewModel
{
    public string Name { get; }
    public string Class { get; }
    public string Realm { get; }
    public string PrimaryRole { get; }
    public string SecondaryRole { get; }
    public string Source { get; }
    public string RoleColor { get; }
    public bool IsPlayer { get; }

    public GroupMemberViewModel(GroupMember member)
    {
        Name = member.IsPlayer ? "You (Player)" : member.Name;
        Class = member.Class?.GetDisplayName() ?? "Unknown";
        Realm = member.Realm?.GetDisplayName() ?? "Unknown";
        PrimaryRole = member.PrimaryRole.GetDisplayName();
        SecondaryRole = member.SecondaryRole?.GetDisplayName() ?? "—";
        Source = member.Source.ToString();
        RoleColor = member.PrimaryRole.GetColorHex();
        IsPlayer = member.IsPlayer;
    }
}

public class RoleCoverageViewModel
{
    public string Role { get; }
    public int MemberCount { get; }
    public string Status { get; }
    public string StatusColor { get; }
    public string Members { get; }
    public string RoleColor { get; }
    public double FillPercent { get; }

    public RoleCoverageViewModel(RoleCoverage coverage)
    {
        Role = coverage.Role.GetDisplayName();
        MemberCount = coverage.MemberCount;
        Members = string.Join(", ", coverage.MemberNames);
        RoleColor = coverage.Role.GetColorHex();

        if (coverage.IsOverRepresented)
        {
            Status = "Over";
            StatusColor = "#FF9800";
        }
        else if (coverage.IsCovered)
        {
            Status = "OK";
            StatusColor = "#4CAF50";
        }
        else if (coverage.MemberCount > 0)
        {
            Status = "Low";
            StatusColor = "#FF9800";
        }
        else
        {
            Status = "None";
            StatusColor = "#F44336";
        }

        // Calculate fill percentage for visual bar (max 4 members = 100%)
        FillPercent = Math.Min(100, coverage.MemberCount * 25);
    }
}

public class RecommendationViewModel
{
    public string Message { get; }
    public string Priority { get; }
    public string PriorityColor { get; }
    public string Icon { get; }

    public RecommendationViewModel(CompositionRecommendation recommendation)
    {
        Message = recommendation.Message;
        Priority = recommendation.Priority.ToString();
        PriorityColor = recommendation.PriorityColor;

        Icon = recommendation.Type switch
        {
            RecommendationType.AddRole => "+",
            RecommendationType.ReduceRole => "-",
            RecommendationType.RebalanceRoles => "~",
            RecommendationType.TemplateMatch => "T",
            RecommendationType.SynergyImprovement => "*",
            _ => "?"
        };
    }
}
