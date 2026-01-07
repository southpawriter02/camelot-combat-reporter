using System;
using System.Collections.ObjectModel;
using System.Linq;
using CamelotCombatReporter.Core.CharacterBuilding.Models;
using CamelotCombatReporter.Core.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CamelotCombatReporter.Gui.CharacterBuilding.ViewModels;

/// <summary>
/// ViewModel for the profile editor dialog.
/// </summary>
public partial class ProfileEditorViewModel : ObservableObject
{
    private readonly CharacterProfile? _existingProfile;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private Realm _selectedRealm = Realm.Albion;

    [ObservableProperty]
    private CharacterClass _selectedClass = CharacterClass.Armsman;

    [ObservableProperty]
    private int _level = 50;

    [ObservableProperty]
    private string _serverName = string.Empty;

    [ObservableProperty]
    private string _validationMessage = string.Empty;

    public ProfileEditorViewModel() : this(null)
    {
    }

    public ProfileEditorViewModel(CharacterProfile? existingProfile)
    {
        _existingProfile = existingProfile;

        if (existingProfile != null)
        {
            Name = existingProfile.Name;
            SelectedRealm = existingProfile.Realm;
            SelectedClass = existingProfile.Class;
            Level = existingProfile.Level;
            ServerName = existingProfile.ServerName ?? string.Empty;
        }

        // Initialize available classes for default realm
        UpdateAvailableClasses();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Properties
    // ─────────────────────────────────────────────────────────────────────────

    public string DialogTitle => _existingProfile == null ? "Create Character Profile" : "Edit Character Profile";
    public bool IsEditing => _existingProfile != null;
    public bool CanSave => !string.IsNullOrWhiteSpace(Name) && SelectedClass != CharacterClass.Unknown;
    public bool HasValidationError => !string.IsNullOrEmpty(ValidationMessage);

    public ObservableCollection<Realm> AvailableRealms { get; } =
    [
        Realm.Albion,
        Realm.Midgard,
        Realm.Hibernia
    ];

    [ObservableProperty]
    private ObservableCollection<CharacterClass> _availableClasses = [];

    // ─────────────────────────────────────────────────────────────────────────
    // Events
    // ─────────────────────────────────────────────────────────────────────────

    public event EventHandler<CharacterProfile?>? CloseRequested;

    // ─────────────────────────────────────────────────────────────────────────
    // Property Changed Handlers
    // ─────────────────────────────────────────────────────────────────────────

    partial void OnSelectedRealmChanged(Realm value)
    {
        UpdateAvailableClasses();
    }

    partial void OnNameChanged(string value)
    {
        OnPropertyChanged(nameof(CanSave));
        ValidationMessage = string.Empty;
    }

    partial void OnSelectedClassChanged(CharacterClass value)
    {
        OnPropertyChanged(nameof(CanSave));
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Commands
    // ─────────────────────────────────────────────────────────────────────────

    [RelayCommand]
    private void Save()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            ValidationMessage = "Character name is required.";
            return;
        }

        if (SelectedClass == CharacterClass.Unknown)
        {
            ValidationMessage = "Please select a class.";
            return;
        }

        var profile = _existingProfile == null
            ? new CharacterProfile
            {
                Name = Name.Trim(),
                Realm = SelectedRealm,
                Class = SelectedClass,
                Level = Level,
                ServerName = string.IsNullOrWhiteSpace(ServerName) ? null : ServerName.Trim()
            }
            : _existingProfile with
            {
                Name = Name.Trim(),
                Realm = SelectedRealm,
                Class = SelectedClass,
                Level = Level,
                ServerName = string.IsNullOrWhiteSpace(ServerName) ? null : ServerName.Trim()
            };

        CloseRequested?.Invoke(this, profile);
    }

    [RelayCommand]
    private void Cancel()
    {
        CloseRequested?.Invoke(this, null);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Private Methods
    // ─────────────────────────────────────────────────────────────────────────

    private void UpdateAvailableClasses()
    {
        var classes = SelectedRealm.GetClasses().ToList();
        AvailableClasses = new ObservableCollection<CharacterClass>(classes);

        // Select first class if current selection is not valid for the realm
        if (!classes.Contains(SelectedClass))
        {
            SelectedClass = classes.FirstOrDefault();
        }
    }
}
