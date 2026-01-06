using System;
using System.Collections.ObjectModel;
using CamelotCombatReporter.Gui.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CamelotCombatReporter.Gui.Settings.ViewModels;

/// <summary>
/// ViewModel for the Settings window.
/// </summary>
public partial class SettingsWindowViewModel : ViewModelBase
{
    [ObservableProperty]
    private int _selectedTabIndex;

    [ObservableProperty]
    private ServerProfileSettingsViewModel _serverProfileSettings = new();

    [ObservableProperty]
    private ChatFilterSettingsViewModel _chatFilterSettings = new();

    [ObservableProperty]
    private PrivacySettingsViewModel _privacySettings = new();

    [ObservableProperty]
    private AppearanceSettingsViewModel _appearanceSettings = new();

    public SettingsWindowViewModel()
    {
    }

    [RelayCommand]
    private void SaveAll()
    {
        ServerProfileSettings.Save();
        ChatFilterSettings.Save();
        PrivacySettings.Save();
        AppearanceSettings.Save();
    }

    [RelayCommand]
    private void ResetToDefaults()
    {
        ServerProfileSettings.ResetToDefaults();
        ChatFilterSettings.ResetToDefaults();
        PrivacySettings.ResetToDefaults();
        AppearanceSettings.ResetToDefaults();
    }
}
