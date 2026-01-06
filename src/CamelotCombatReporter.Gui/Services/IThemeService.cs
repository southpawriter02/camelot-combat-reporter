using System;

namespace CamelotCombatReporter.Gui.Services;

/// <summary>
/// Theme mode options for the application.
/// </summary>
public enum ThemeMode
{
    /// <summary>
    /// Follows the system theme setting.
    /// </summary>
    System,

    /// <summary>
    /// Light theme with light backgrounds and dark text.
    /// </summary>
    Light,

    /// <summary>
    /// Dark theme with dark backgrounds and light text.
    /// </summary>
    Dark
}

/// <summary>
/// Service interface for managing application theming.
/// </summary>
public interface IThemeService
{
    /// <summary>
    /// Gets or sets the current theme mode.
    /// </summary>
    ThemeMode CurrentTheme { get; set; }

    /// <summary>
    /// Gets whether the current effective theme is dark.
    /// This accounts for System theme mode resolving to the actual system preference.
    /// </summary>
    bool IsDarkTheme { get; }

    /// <summary>
    /// Gets the available theme modes.
    /// </summary>
    ThemeMode[] AvailableThemes { get; }

    /// <summary>
    /// Applies the specified theme mode to the application.
    /// </summary>
    /// <param name="theme">The theme mode to apply.</param>
    void ApplyTheme(ThemeMode theme);

    /// <summary>
    /// Saves the current theme preference to persistent storage.
    /// </summary>
    void SavePreference();

    /// <summary>
    /// Loads the theme preference from persistent storage and applies it.
    /// </summary>
    void LoadAndApplyPreference();

    /// <summary>
    /// Raised when the theme changes.
    /// </summary>
    event EventHandler<ThemeChangedEventArgs>? ThemeChanged;
}

/// <summary>
/// Event arguments for theme change events.
/// </summary>
public class ThemeChangedEventArgs : EventArgs
{
    /// <summary>
    /// Gets the previous theme mode.
    /// </summary>
    public ThemeMode PreviousTheme { get; }

    /// <summary>
    /// Gets the new theme mode.
    /// </summary>
    public ThemeMode NewTheme { get; }

    /// <summary>
    /// Gets whether the effective theme is now dark.
    /// </summary>
    public bool IsDarkTheme { get; }

    public ThemeChangedEventArgs(ThemeMode previousTheme, ThemeMode newTheme, bool isDarkTheme)
    {
        PreviousTheme = previousTheme;
        NewTheme = newTheme;
        IsDarkTheme = isDarkTheme;
    }
}
