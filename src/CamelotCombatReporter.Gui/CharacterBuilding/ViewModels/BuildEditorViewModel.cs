using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CamelotCombatReporter.Core.CharacterBuilding.Models;
using CamelotCombatReporter.Core.CharacterBuilding.Services;
using CamelotCombatReporter.Core.CharacterBuilding.Templates;
using CamelotCombatReporter.Core.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CamelotCombatReporter.Gui.CharacterBuilding.ViewModels;

/// <summary>
/// ViewModel for the Build Editor dialog.
/// </summary>
public partial class BuildEditorViewModel : ObservableObject
{
    private readonly ISpecializationTemplateService _specService;
    private readonly CharacterProfile _profile;
    private readonly CharacterBuild? _existingBuild;
    private readonly Action<CharacterBuild?, bool> _onClose;

    [ObservableProperty]
    private string _buildName = string.Empty;

    [ObservableProperty]
    private int _realmRank = 1;

    [ObservableProperty]
    private int _realmRankLevel = 0;

    [ObservableProperty]
    private long _realmPoints;

    [ObservableProperty]
    private string? _notes;

    [ObservableProperty]
    private string _validationMessage = string.Empty;

    [ObservableProperty]
    private bool _hasValidationWarning;

    public ObservableCollection<SpecLineViewModel> SpecLineViewModels { get; } = [];
    public ObservableCollection<RealmAbilityViewModel> SelectedRealmAbilities { get; } = [];

    public string DialogTitle => IsEditing ? $"Edit Build: {_existingBuild?.Name}" : "Create New Build";
    public string ProfileDisplayName => $"{_profile.Name} - {_profile.Class} ({_profile.Realm})";
    public bool IsEditing => _existingBuild != null;
    public string RealmRankDisplay => $"RR{RealmRank}L{RealmRankLevel}";

    public int AllocatedSpecPoints => SpecLineViewModels.Sum(s => CalculateSpecPointCost(s.Level));
    public int MaxSpecPoints => _specService.GetMaxSpecPoints(_profile.Level);
    public int SpecPointsRemaining => MaxSpecPoints - AllocatedSpecPoints;

    public int AllocatedRAPoints => SelectedRealmAbilities.Sum(ra => ra.PointCost);
    public int MaxRAPoints => RealmAbilityCatalog.GetMaxRealmAbilityPoints(RealmRank, RealmRankLevel);

    public BuildEditorViewModel(
        CharacterProfile profile,
        CharacterBuild? existingBuild,
        ISpecializationTemplateService specService,
        Action<CharacterBuild?, bool> onClose)
    {
        _profile = profile;
        _existingBuild = existingBuild;
        _specService = specService;
        _onClose = onClose;

        InitializeFromProfile();

        if (existingBuild != null)
        {
            LoadExistingBuild(existingBuild);
        }
        else
        {
            BuildName = $"New Build - {DateTime.Now:yyyy-MM-dd}";
        }
    }

    private void InitializeFromProfile()
    {
        var template = _specService.GetTemplateForClass(_profile.Class);

        foreach (var specLine in template.SpecLines)
        {
            var vm = new SpecLineViewModel(specLine);
            vm.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(SpecLineViewModel.Level))
                {
                    OnPropertyChanged(nameof(AllocatedSpecPoints));
                    OnPropertyChanged(nameof(SpecPointsRemaining));
                    ValidatePoints();
                }
            };
            SpecLineViewModels.Add(vm);
        }
    }

    private void LoadExistingBuild(CharacterBuild build)
    {
        BuildName = build.Name;
        RealmRank = build.RealmRank;
        RealmRankLevel = build.RealmRankLevel;
        RealmPoints = build.RealmPoints;
        Notes = build.Notes;

        // Load spec allocations
        foreach (var specVm in SpecLineViewModels)
        {
            if (build.SpecLines.TryGetValue(specVm.Name, out var level))
            {
                specVm.Level = level;
            }
        }

        // Load realm abilities
        foreach (var ra in build.RealmAbilities)
        {
            var def = RealmAbilityCatalog.GetAllAbilities()
                .FirstOrDefault(a => a.Name == ra.AbilityName);
            if (def != null)
            {
                SelectedRealmAbilities.Add(new RealmAbilityViewModel(def, ra.Rank));
            }
        }
    }

    partial void OnRealmRankChanged(int value)
    {
        OnPropertyChanged(nameof(RealmRankDisplay));
        OnPropertyChanged(nameof(MaxRAPoints));
        ValidatePoints();
    }

    partial void OnRealmRankLevelChanged(int value)
    {
        OnPropertyChanged(nameof(RealmRankDisplay));
        OnPropertyChanged(nameof(MaxRAPoints));
        ValidatePoints();
    }

    private void ValidatePoints()
    {
        var warnings = new List<string>();

        if (AllocatedSpecPoints > MaxSpecPoints)
        {
            warnings.Add($"Spec points exceeded by {AllocatedSpecPoints - MaxSpecPoints}");
        }

        if (AllocatedRAPoints > MaxRAPoints)
        {
            warnings.Add($"RA points exceeded by {AllocatedRAPoints - MaxRAPoints}");
        }

        ValidationMessage = string.Join(" | ", warnings);
        HasValidationWarning = warnings.Count > 0;
    }

    private static int CalculateSpecPointCost(int level)
    {
        // Standard formula: sum of 1 to level = n(n+1)/2
        return level * (level + 1) / 2;
    }

    [RelayCommand]
    private void AddRealmAbility()
    {
        // Get available abilities (not already selected)
        var selectedNames = SelectedRealmAbilities.Select(ra => ra.AbilityName).ToHashSet();
        var available = RealmAbilityCatalog.GetAllAbilities()
            .Where(a => !selectedNames.Contains(a.Name))
            .OrderBy(a => a.Category)
            .ThenBy(a => a.Name)
            .ToList();

        if (available.Count == 0) return;

        // Add the first available ability (in real app, would show picker dialog)
        var first = available.First();
        var vm = new RealmAbilityViewModel(first, 1);
        vm.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(RealmAbilityViewModel.PointCost))
            {
                OnPropertyChanged(nameof(AllocatedRAPoints));
                ValidatePoints();
            }
        };
        SelectedRealmAbilities.Add(vm);
        OnPropertyChanged(nameof(AllocatedRAPoints));
        ValidatePoints();
    }

    [RelayCommand]
    private void RemoveRealmAbility(RealmAbilityViewModel? ra)
    {
        if (ra != null)
        {
            SelectedRealmAbilities.Remove(ra);
            OnPropertyChanged(nameof(AllocatedRAPoints));
            ValidatePoints();
        }
    }

    [RelayCommand]
    private void Save()
    {
        var build = CreateBuild();
        _onClose(build, false); // false = not a new build (update existing if editing)
    }

    [RelayCommand]
    private void SaveAsNew()
    {
        var build = CreateBuild();
        _onClose(build, true); // true = create as new build
    }

    [RelayCommand]
    private void Cancel()
    {
        _onClose(null, false);
    }

    private CharacterBuild CreateBuild()
    {
        var specLines = SpecLineViewModels
            .Where(s => s.Level > 1)
            .ToDictionary(s => s.Name, s => s.Level);

        var realmAbilities = SelectedRealmAbilities
            .Select(ra => new RealmAbilitySelection
            {
                AbilityName = ra.AbilityName,
                Rank = ra.Rank,
                PointCost = ra.PointCost,
                Category = ra.Category
            })
            .ToList();

        return new CharacterBuild
        {
            Id = IsEditing ? _existingBuild!.Id : Guid.NewGuid(),
            Name = string.IsNullOrWhiteSpace(BuildName) ? "Unnamed Build" : BuildName,
            RealmRank = RealmRank,
            RealmRankLevel = RealmRankLevel,
            RealmPoints = RealmPoints,
            SpecLines = specLines,
            RealmAbilities = realmAbilities,
            Notes = Notes,
            CreatedUtc = IsEditing ? _existingBuild!.CreatedUtc : DateTime.UtcNow
        };
    }
}

/// <summary>
/// ViewModel for a single spec line in the editor.
/// </summary>
public partial class SpecLineViewModel : ObservableObject
{
    private readonly SpecLine _specLine;

    [ObservableProperty]
    private int _level = 1;

    public string Name => _specLine.Name;
    public int MaxLevel => _specLine.MaxLevel;
    public string? Description => _specLine.Description;
    public SpecLineType Type => _specLine.Type;

    public SpecLineViewModel(SpecLine specLine)
    {
        _specLine = specLine;
    }
}

/// <summary>
/// ViewModel for a selected realm ability in the editor.
/// </summary>
public partial class RealmAbilityViewModel : ObservableObject
{
    private readonly RealmAbilityDefinition _definition;

    [ObservableProperty]
    private int _rank = 1;

    public string AbilityName => _definition.Name;
    public RealmAbilityCategory Category => _definition.Category;
    public int MaxRank => _definition.MaxRank;
    public int PointCost => RealmAbilityCatalog.GetPointCost(AbilityName, Rank);

    public RealmAbilityViewModel(RealmAbilityDefinition definition, int initialRank = 1)
    {
        _definition = definition;
        _rank = Math.Min(initialRank, MaxRank);
    }

    partial void OnRankChanged(int value)
    {
        OnPropertyChanged(nameof(PointCost));
    }
}
