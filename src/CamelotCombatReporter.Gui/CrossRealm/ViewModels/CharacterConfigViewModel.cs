using System;
using System.Collections.ObjectModel;
using System.Linq;
using CamelotCombatReporter.Core.Models;
using CamelotCombatReporter.Gui.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CamelotCombatReporter.Gui.CrossRealm.ViewModels;

/// <summary>
/// ViewModel for the character configuration dialog.
/// </summary>
public partial class CharacterConfigViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _characterName = "";

    [ObservableProperty]
    private Realm _selectedRealm = Realm.Unknown;

    [ObservableProperty]
    private CharacterClass _selectedClass = CharacterClass.Unknown;

    [ObservableProperty]
    private int _level = 50;

    [ObservableProperty]
    private int _realmRank = 0;

    [ObservableProperty]
    private ObservableCollection<Realm> _availableRealms = new()
    {
        Realm.Albion,
        Realm.Midgard,
        Realm.Hibernia
    };

    [ObservableProperty]
    private ObservableCollection<CharacterClass> _availableClasses = new();

    [ObservableProperty]
    private bool _isValid;

    [ObservableProperty]
    private string _validationMessage = "";

    /// <summary>
    /// Event raised when the dialog should be closed with success.
    /// </summary>
    public event EventHandler<CharacterInfo>? Saved;

    /// <summary>
    /// Event raised when the dialog should be canceled.
    /// </summary>
    public event EventHandler? Cancelled;

    public CharacterConfigViewModel()
    {
        UpdateAvailableClasses();
        ValidateInput();
    }

    public CharacterConfigViewModel(CharacterInfo? existingCharacter) : this()
    {
        if (existingCharacter != null && existingCharacter.IsConfigured)
        {
            CharacterName = existingCharacter.Name;
            SelectedRealm = existingCharacter.Realm;
            Level = existingCharacter.Level;
            RealmRank = existingCharacter.RealmRank;

            // Update classes first, then set the selected class
            UpdateAvailableClasses();
            SelectedClass = existingCharacter.Class;
        }
    }

    partial void OnSelectedRealmChanged(Realm value)
    {
        UpdateAvailableClasses();
        ValidateInput();
    }

    partial void OnSelectedClassChanged(CharacterClass value)
    {
        ValidateInput();
    }

    partial void OnCharacterNameChanged(string value)
    {
        ValidateInput();
    }

    partial void OnLevelChanged(int value)
    {
        // Clamp level between 1 and 50
        if (value < 1)
            Level = 1;
        else if (value > 50)
            Level = 50;

        ValidateInput();
    }

    partial void OnRealmRankChanged(int value)
    {
        // Clamp realm rank between 0 and 14
        if (value < 0)
            RealmRank = 0;
        else if (value > 14)
            RealmRank = 14;

        ValidateInput();
    }

    private void UpdateAvailableClasses()
    {
        AvailableClasses.Clear();

        if (SelectedRealm == Realm.Unknown)
            return;

        foreach (var characterClass in SelectedRealm.GetClasses())
        {
            AvailableClasses.Add(characterClass);
        }

        // Reset selected class if it's not in the new realm
        if (!AvailableClasses.Contains(SelectedClass))
        {
            SelectedClass = AvailableClasses.FirstOrDefault();
        }
    }

    private void ValidateInput()
    {
        var errors = new System.Collections.Generic.List<string>();

        if (SelectedRealm == Realm.Unknown)
        {
            errors.Add("Please select a realm");
        }

        if (SelectedClass == CharacterClass.Unknown)
        {
            errors.Add("Please select a class");
        }

        if (Level < 1 || Level > 50)
        {
            errors.Add("Level must be between 1 and 50");
        }

        if (RealmRank < 0 || RealmRank > 14)
        {
            errors.Add("Realm rank must be between 0 and 14");
        }

        IsValid = errors.Count == 0;
        ValidationMessage = errors.Count > 0 ? string.Join("\n", errors) : "Configuration is valid";
    }

    [RelayCommand]
    private void Save()
    {
        if (!IsValid)
            return;

        var character = new CharacterInfo(
            CharacterName,
            SelectedRealm,
            SelectedClass,
            Level,
            RealmRank);

        Saved?.Invoke(this, character);
    }

    [RelayCommand]
    private void Cancel()
    {
        Cancelled?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Gets the current character info based on the form values.
    /// </summary>
    public CharacterInfo GetCharacterInfo()
    {
        return new CharacterInfo(
            CharacterName,
            SelectedRealm,
            SelectedClass,
            Level,
            RealmRank);
    }
}
