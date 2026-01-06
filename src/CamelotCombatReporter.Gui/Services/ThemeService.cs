using System;
using System.IO;
using System.Text.Json;
using Avalonia;
using Avalonia.Styling;
using Microsoft.Extensions.Logging;

namespace CamelotCombatReporter.Gui.Services;

/// <summary>
/// Service for managing application theming with persistent preferences.
/// </summary>
public class ThemeService : IThemeService
{
    private static readonly string SettingsDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "CamelotCombatReporter");

    private static readonly string SettingsFilePath = Path.Combine(SettingsDirectory, "theme-settings.json");

    private readonly ILogger<ThemeService> _logger;
    private ThemeMode _currentTheme = ThemeMode.System;

    /// <inheritdoc />
    public ThemeMode CurrentTheme
    {
        get => _currentTheme;
        set
        {
            if (_currentTheme != value)
            {
                ApplyTheme(value);
            }
        }
    }

    /// <inheritdoc />
    public bool IsDarkTheme
    {
        get
        {
            return _currentTheme switch
            {
                ThemeMode.Dark => true,
                ThemeMode.Light => false,
                ThemeMode.System => GetSystemThemeIsDark(),
                _ => false
            };
        }
    }

    /// <inheritdoc />
    public ThemeMode[] AvailableThemes => [ThemeMode.System, ThemeMode.Light, ThemeMode.Dark];

    /// <inheritdoc />
    public event EventHandler<ThemeChangedEventArgs>? ThemeChanged;

    public ThemeService()
    {
        _logger = App.CreateLogger<ThemeService>();
    }

    /// <inheritdoc />
    public void ApplyTheme(ThemeMode theme)
    {
        var previousTheme = _currentTheme;
        _currentTheme = theme;

        var app = Application.Current;
        if (app is null)
        {
            _logger.LogWarning("Cannot apply theme: Application.Current is null");
            return;
        }

        app.RequestedThemeVariant = theme switch
        {
            ThemeMode.Light => ThemeVariant.Light,
            ThemeMode.Dark => ThemeVariant.Dark,
            ThemeMode.System => ThemeVariant.Default,
            _ => ThemeVariant.Default
        };

        _logger.LogInformation("Applied theme: {Theme} (IsDark: {IsDark})", theme, IsDarkTheme);

        ThemeChanged?.Invoke(this, new ThemeChangedEventArgs(previousTheme, theme, IsDarkTheme));
    }

    /// <inheritdoc />
    public void SavePreference()
    {
        try
        {
            EnsureSettingsDirectoryExists();

            var settings = new ThemeSettings { Theme = _currentTheme.ToString() };
            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingsFilePath, json);

            _logger.LogDebug("Saved theme preference: {Theme}", _currentTheme);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save theme preference");
        }
    }

    /// <inheritdoc />
    public void LoadAndApplyPreference()
    {
        try
        {
            if (!File.Exists(SettingsFilePath))
            {
                _logger.LogDebug("No theme settings file found, using default (System)");
                ApplyTheme(ThemeMode.System);
                return;
            }

            var json = File.ReadAllText(SettingsFilePath);
            var settings = JsonSerializer.Deserialize<ThemeSettings>(json);

            if (settings is not null && Enum.TryParse<ThemeMode>(settings.Theme, out var theme))
            {
                ApplyTheme(theme);
                _logger.LogDebug("Loaded theme preference: {Theme}", theme);
            }
            else
            {
                _logger.LogWarning("Invalid theme settings, using default (System)");
                ApplyTheme(ThemeMode.System);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load theme preference, using default");
            ApplyTheme(ThemeMode.System);
        }
    }

    private static bool GetSystemThemeIsDark()
    {
        var app = Application.Current;
        if (app?.ActualThemeVariant is not null)
        {
            return app.ActualThemeVariant == ThemeVariant.Dark;
        }

        // Default to light if we can't determine
        return false;
    }

    private static void EnsureSettingsDirectoryExists()
    {
        if (!Directory.Exists(SettingsDirectory))
        {
            Directory.CreateDirectory(SettingsDirectory);
        }
    }

    private sealed class ThemeSettings
    {
        public string Theme { get; set; } = ThemeMode.System.ToString();
    }
}
