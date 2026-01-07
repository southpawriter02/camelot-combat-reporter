using System;
using CamelotCombatReporter.Core.CharacterBuilding.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CamelotCombatReporter.Gui.CharacterBuilding.ViewModels;

/// <summary>
/// ViewModel for the Export Profile dialog.
/// </summary>
public partial class ExportProfileViewModel : ObservableObject
{
    private readonly CharacterProfile _profile;

    [ObservableProperty]
    private bool _includeBuildHistory = true;

    [ObservableProperty]
    private bool _includeSessionReferences;

    [ObservableProperty]
    private bool _anonymizeCharacterName;

    [ObservableProperty]
    private bool _includePerformanceMetrics;

    [ObservableProperty]
    private string _customExportName = "";

    [ObservableProperty]
    private bool _useCustomName;

    public string ProfileName => _profile.Name;
    public string ProfileClass => _profile.Class.ToString();
    public string ProfileRealm => _profile.Realm.ToString();
    public int BuildCount => _profile.BuildHistory.Count;
    public int SessionCount => _profile.AttachedSessionIds.Count;

    public string DialogTitle => $"Export Profile: {_profile.Name}";

    /// <summary>
    /// The export options, or null if cancelled.
    /// </summary>
    public ProfileExportOptions? Result { get; private set; }

    /// <summary>
    /// Event raised when the dialog should close.
    /// </summary>
    public event Action<bool>? RequestClose;

    public ExportProfileViewModel() : this(new CharacterProfile 
    { 
        Name = "Test Profile",
        Realm = Core.Models.Realm.Albion,
        Class = Core.Models.CharacterClass.Armsman
    })
    {
        // Design-time constructor
    }

    public ExportProfileViewModel(CharacterProfile profile)
    {
        _profile = profile;
        _customExportName = profile.Name;
    }

    partial void OnAnonymizeCharacterNameChanged(bool value)
    {
        // If anonymizing, suggest using custom name
        if (value && !UseCustomName)
        {
            UseCustomName = true;
            CustomExportName = "Anonymous";
        }
    }

    [RelayCommand]
    private void Export()
    {
        Result = new ProfileExportOptions
        {
            IncludeBuildHistory = IncludeBuildHistory,
            IncludeSessionReferences = IncludeSessionReferences,
            AnonymizeCharacterName = AnonymizeCharacterName,
            IncludePerformanceMetrics = IncludePerformanceMetrics,
            CustomExportName = UseCustomName ? CustomExportName : null
        };
        RequestClose?.Invoke(true);
    }

    [RelayCommand]
    private void Cancel()
    {
        Result = null;
        RequestClose?.Invoke(false);
    }

    [RelayCommand]
    private void ResetDefaults()
    {
        IncludeBuildHistory = true;
        IncludeSessionReferences = false;
        AnonymizeCharacterName = false;
        IncludePerformanceMetrics = false;
        UseCustomName = false;
        CustomExportName = _profile.Name;
    }
}
