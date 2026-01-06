using System;
using System.Collections.ObjectModel;
using CamelotCombatReporter.Gui.Services;
using CamelotCombatReporter.Gui.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CamelotCombatReporter.Gui.Settings.ViewModels;

/// <summary>
/// ViewModel for Appearance/Theme settings.
/// </summary>
public partial class AppearanceSettingsViewModel : ViewModelBase
{
    private readonly IThemeService _themeService;

    [ObservableProperty]
    private ThemeMode _selectedTheme;

    [ObservableProperty]
    private bool _hasChanges;

    /// <summary>
    /// Gets the available theme options.
    /// </summary>
    public ObservableCollection<ThemeModeItem> ThemeOptions { get; } = new()
    {
        new ThemeModeItem(ThemeMode.System, "System", "Follow system theme setting"),
        new ThemeModeItem(ThemeMode.Light, "Light", "Light theme with light backgrounds"),
        new ThemeModeItem(ThemeMode.Dark, "Dark", "Dark theme with dark backgrounds")
    };

    public AppearanceSettingsViewModel() : this(App.ThemeService ?? new ThemeService())
    {
    }

    public AppearanceSettingsViewModel(IThemeService themeService)
    {
        _themeService = themeService;
        _selectedTheme = _themeService.CurrentTheme;
    }

    partial void OnSelectedThemeChanged(ThemeMode value)
    {
        HasChanges = true;
        // Apply immediately for preview
        _themeService.ApplyTheme(value);
    }

    [RelayCommand]
    private void SetTheme(ThemeMode theme)
    {
        SelectedTheme = theme;
    }

    public void Save()
    {
        _themeService.SavePreference();
        HasChanges = false;
    }

    public void ResetToDefaults()
    {
        SelectedTheme = ThemeMode.System;
        HasChanges = false;
    }
}

/// <summary>
/// Represents a theme mode option for display in the UI.
/// </summary>
public record ThemeModeItem(ThemeMode Mode, string DisplayName, string Description);
