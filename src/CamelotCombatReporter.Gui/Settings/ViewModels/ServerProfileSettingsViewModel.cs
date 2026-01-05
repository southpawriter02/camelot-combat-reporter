using System;
using System.Collections.ObjectModel;
using System.Linq;
using CamelotCombatReporter.Core.ServerProfiles;
using CamelotCombatReporter.Gui.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CamelotCombatReporter.Gui.Settings.ViewModels;

/// <summary>
/// ViewModel for Server Profile settings.
/// </summary>
public partial class ServerProfileSettingsViewModel : ViewModelBase
{
    private readonly ServerProfileService _profileService;

    [ObservableProperty]
    private ObservableCollection<ServerProfileViewModel> _profiles = new();

    [ObservableProperty]
    private ServerProfileViewModel? _selectedProfile;

    [ObservableProperty]
    private string _currentProfileName = "Live";

    [ObservableProperty]
    private bool _hasChanges;

    public ServerProfileSettingsViewModel()
    {
        _profileService = new ServerProfileService();
        LoadProfiles();
    }

    private void LoadProfiles()
    {
        Profiles.Clear();
        foreach (var profile in _profileService.AllProfiles)
        {
            Profiles.Add(new ServerProfileViewModel(profile));
        }

        var current = _profileService.ActiveProfile;
        if (current != null)
        {
            CurrentProfileName = current.Name;
            SelectedProfile = Profiles.FirstOrDefault(p => p.Id == current.Id);
        }
    }

    partial void OnSelectedProfileChanged(ServerProfileViewModel? value)
    {
        if (value != null)
        {
            HasChanges = value.Name != CurrentProfileName;
        }
    }

    [RelayCommand]
    private void ApplyProfile()
    {
        if (SelectedProfile != null)
        {
            _profileService.SetActiveProfile(SelectedProfile.Id);
            CurrentProfileName = SelectedProfile.Name;
            HasChanges = false;
        }
    }

    public void Save()
    {
        ApplyProfile();
    }

    public void ResetToDefaults()
    {
        _profileService.SetActiveProfile("live");
        LoadProfiles();
        HasChanges = false;
    }
}

/// <summary>
/// ViewModel for a server profile.
/// </summary>
public class ServerProfileViewModel
{
    private readonly ServerProfile _profile;

    public ServerProfileViewModel(ServerProfile profile)
    {
        _profile = profile;
    }

    public string Id => _profile.Id;
    public string Name => _profile.Name;
    public string Description => GetDescription();
    public bool IsBuiltIn => _profile.IsBuiltIn;
    public int ClassCount => _profile.AvailableClasses.Count;
    public bool HasMasterLevels => _profile.HasMasterLevels;
    public bool HasArtifacts => _profile.HasArtifacts;
    public bool HasChampionLevels => _profile.HasChampionLevels;
    public bool HasMaulers => _profile.HasMaulers;

    public string Features
    {
        get
        {
            var features = new System.Collections.Generic.List<string>();
            if (HasMasterLevels) features.Add("MLs");
            if (HasArtifacts) features.Add("Artifacts");
            if (HasChampionLevels) features.Add("CLs");
            if (HasMaulers) features.Add("Maulers");
            return features.Count > 0 ? string.Join(", ", features) : "Base classes only";
        }
    }

    private string GetDescription()
    {
        return _profile.BaseType switch
        {
            ServerType.Classic => "Original DAoC classes (pre-SI)",
            ServerType.ShroudedIsles => "Includes SI expansion classes",
            ServerType.TrialsOfAtlantis => "Includes Master Levels and Artifacts",
            ServerType.NewFrontiers => "Includes NF and Mauler class",
            ServerType.Live => "All current retail features",
            ServerType.Custom => "Custom configuration",
            _ => "Unknown server type"
        };
    }
}
