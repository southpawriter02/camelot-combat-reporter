using System;
using CamelotCombatReporter.Core.ChatFiltering;
using CamelotCombatReporter.Gui.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CamelotCombatReporter.Gui.Settings.ViewModels;

/// <summary>
/// ViewModel for Privacy settings.
/// </summary>
public partial class PrivacySettingsViewModel : ViewModelBase
{
    [ObservableProperty]
    private bool _isEnabled;

    [ObservableProperty]
    private bool _anonymizePlayerNames;

    [ObservableProperty]
    private bool _stripPrivateMessages;

    [ObservableProperty]
    private bool _hashIdentifiers;

    [ObservableProperty]
    private bool _anonymizeGuildNames;

    [ObservableProperty]
    private bool _removeLocationInfo;

    [ObservableProperty]
    private bool _hasChanges;

    public PrivacySettingsViewModel()
    {
    }

    partial void OnIsEnabledChanged(bool value)
    {
        HasChanges = true;
    }

    partial void OnAnonymizePlayerNamesChanged(bool value)
    {
        HasChanges = true;
    }

    partial void OnStripPrivateMessagesChanged(bool value)
    {
        HasChanges = true;
    }

    partial void OnHashIdentifiersChanged(bool value)
    {
        HasChanges = true;
    }

    partial void OnAnonymizeGuildNamesChanged(bool value)
    {
        HasChanges = true;
    }

    partial void OnRemoveLocationInfoChanged(bool value)
    {
        HasChanges = true;
    }

    [RelayCommand]
    private void EnableMaxPrivacy()
    {
        IsEnabled = true;
        AnonymizePlayerNames = true;
        StripPrivateMessages = true;
        HashIdentifiers = true;
        AnonymizeGuildNames = true;
        RemoveLocationInfo = true;
        HasChanges = true;
    }

    [RelayCommand]
    private void DisableAll()
    {
        IsEnabled = false;
        AnonymizePlayerNames = false;
        StripPrivateMessages = false;
        HashIdentifiers = false;
        AnonymizeGuildNames = false;
        RemoveLocationInfo = false;
        HasChanges = true;
    }

    public void Save()
    {
        // Save settings to configuration
        HasChanges = false;
    }

    public void ResetToDefaults()
    {
        IsEnabled = false;
        AnonymizePlayerNames = false;
        StripPrivateMessages = false;
        HashIdentifiers = false;
        AnonymizeGuildNames = false;
        RemoveLocationInfo = false;
        HasChanges = false;
    }

    public PrivacySettings ToSettings()
    {
        return new PrivacySettings(
            AnonymizePlayerNames: AnonymizePlayerNames,
            StripPrivateMessages: StripPrivateMessages,
            HashIdentifiers: HashIdentifiers
        );
    }
}
