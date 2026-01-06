using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using System.Linq;
using Avalonia.Markup.Xaml;
using CamelotCombatReporter.Gui.Services;
using CamelotCombatReporter.Gui.ViewModels;
using CamelotCombatReporter.Gui.Views;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace CamelotCombatReporter.Gui;

/// <summary>
/// Main application class for Camelot Combat Reporter GUI.
/// </summary>
public partial class App : Application
{
    /// <summary>
    /// Gets the application-wide logger factory.
    /// </summary>
    public static ILoggerFactory? LoggerFactory { get; private set; }

    /// <summary>
    /// Gets the application-wide theme service.
    /// </summary>
    public static IThemeService? ThemeService { get; private set; }

    /// <summary>
    /// Creates a logger for the specified type.
    /// Returns a NullLogger if the LoggerFactory hasn't been initialized (e.g., in tests).
    /// </summary>
    /// <typeparam name="T">The type to create a logger for.</typeparam>
    /// <returns>A logger instance.</returns>
    public static ILogger<T> CreateLogger<T>() =>
        LoggerFactory?.CreateLogger<T>() ?? NullLogger<T>.Instance;

    /// <summary>
    /// Creates a logger with the specified category name.
    /// Returns a NullLogger if the LoggerFactory hasn't been initialized (e.g., in tests).
    /// </summary>
    /// <param name="categoryName">The category name for the logger.</param>
    /// <returns>A logger instance.</returns>
    public static ILogger CreateLogger(string categoryName) =>
        LoggerFactory?.CreateLogger(categoryName) ?? NullLogger.Instance;

    public override void Initialize()
    {
        // Configure logging
        LoggerFactory = Microsoft.Extensions.Logging.LoggerFactory.Create(builder =>
        {
            builder
                .SetMinimumLevel(LogLevel.Debug)
                .AddConsole(options =>
                {
                    options.FormatterName = "simple";
                });
        });

        var logger = CreateLogger<App>();
        logger.LogInformation("Camelot Combat Reporter starting...");

        AvaloniaXamlLoader.Load(this);

        // Initialize and apply theme preferences
        ThemeService = new ThemeService();
        ThemeService.LoadAndApplyPreference();
        logger.LogInformation("Theme service initialized");
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Avoid duplicate validations from both Avalonia and the CommunityToolkit.
            // More info: https://docs.avaloniaui.net/docs/guides/development-guides/data-validation#manage-validationplugins
            DisableAvaloniaDataAnnotationValidation();
            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel(),
            };

            var logger = CreateLogger<App>();
            logger.LogInformation("Application initialized successfully");
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void DisableAvaloniaDataAnnotationValidation()
    {
        // Get an array of plugins to remove
        var dataValidationPluginsToRemove =
            BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

        // remove each entry found
        foreach (var plugin in dataValidationPluginsToRemove)
        {
            BindingPlugins.DataValidators.Remove(plugin);
        }
    }
}