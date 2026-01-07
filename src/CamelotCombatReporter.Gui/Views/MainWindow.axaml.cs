using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Styling;
using CamelotCombatReporter.Core.Logging;
using CamelotCombatReporter.Gui.CrossRealm.ViewModels;
using CamelotCombatReporter.Gui.Plugins.Views;
using CamelotCombatReporter.Gui.Services;
using CamelotCombatReporter.Gui.Settings.ViewModels;
using CamelotCombatReporter.Gui.Settings.Views;
using CamelotCombatReporter.Gui.ViewModels;
using Microsoft.Extensions.Logging;
using System.Linq;

namespace CamelotCombatReporter.Gui.Views;

public partial class MainWindow : Window
{
    private readonly CrossRealmViewModel _crossRealmViewModel;
    private readonly ILogger<MainWindow> _logger;

    public MainWindow()
    {
        InitializeComponent();
        _logger = App.CreateLogger<MainWindow>();

        // Initialize CrossRealmView with its ViewModel
        _crossRealmViewModel = new CrossRealmViewModel();
        CrossRealmView.DataContext = _crossRealmViewModel;

        // Initialize async data when loaded with proper error handling
        Loaded += async (_, _) =>
        {
            try
            {
                await _crossRealmViewModel.InitializeAsync();
            }
            catch (Exception ex)
            {
                _logger.LogUnexpectedError("CrossRealmViewModel initialization", ex);
            }
        };
    }

    private void OnDrop(object? sender, DragEventArgs e)
    {
#pragma warning disable CS0618 // Keep using Data for now; DataTransfer.GetFiles() requires IDataObject
        var files = e.Data.GetFiles();
#pragma warning restore CS0618
        if (files != null)
        {
            var file = files.FirstOrDefault();
            if (file != null)
            {
                var path = file.Path.LocalPath;
                if (path.EndsWith(".log") || path.EndsWith(".txt"))
                {
                    if (DataContext is MainWindowViewModel vm)
                    {
                        vm.SelectedLogFile = path;
                    }
                }
            }
        }
    }

    private void ToggleTheme(object? sender, RoutedEventArgs e)
    {
        var themeService = App.ThemeService;
        if (themeService != null)
        {
            // Cycle through themes: System -> Light -> Dark -> System
            var newTheme = themeService.CurrentTheme switch
            {
                ThemeMode.System => ThemeMode.Light,
                ThemeMode.Light => ThemeMode.Dark,
                ThemeMode.Dark => ThemeMode.System,
                _ => ThemeMode.System
            };
            themeService.ApplyTheme(newTheme);
            themeService.SavePreference();
        }
    }

    private void OnExitClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    private async void OnAboutClick(object? sender, RoutedEventArgs e)
    {
        var dialog = new Window
        {
            Title = "About Camelot Combat Reporter",
            Width = 450,
            Height = 280,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false,
            Content = new StackPanel
            {
                Margin = new Avalonia.Thickness(30),
                Spacing = 12,
                Children =
                {
                    new TextBlock
                    {
                        Text = "Camelot Combat Reporter",
                        FontSize = 24,
                        FontWeight = Avalonia.Media.FontWeight.Bold,
                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
                    },
                    new TextBlock
                    {
                        Text = "Version 1.7.0",
                        FontSize = 14,
                        Foreground = Avalonia.Media.Brushes.Gray,
                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
                    },
                    new Separator { Margin = new Avalonia.Thickness(0, 5) },
                    new TextBlock
                    {
                        Text = "A comprehensive combat log analyzer for Dark Age of Camelot",
                        TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                        TextAlignment = Avalonia.Media.TextAlignment.Center
                    },
                    new TextBlock
                    {
                        Text = "Features: DPS Analysis, Death Analysis, Group Composition,\nRealm Abilities, Buff Tracking, Alerts, and more.",
                        TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                        TextAlignment = Avalonia.Media.TextAlignment.Center,
                        FontSize = 12,
                        Foreground = Avalonia.Media.Brushes.Gray
                    },
                    new TextBlock
                    {
                        Text = "Built with .NET 9.0 and Avalonia UI",
                        FontSize = 11,
                        Foreground = Avalonia.Media.Brushes.DimGray,
                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                        Margin = new Avalonia.Thickness(0, 10, 0, 0)
                    }
                }
            }
        };

        await dialog.ShowDialog(this);
    }

    private async void OnSettingsClick(object? sender, RoutedEventArgs e)
    {
        var settingsWindow = new SettingsWindow
        {
            DataContext = new SettingsWindowViewModel()
        };
        await settingsWindow.ShowDialog(this);
    }

    private async void OnKeyboardShortcutsClick(object? sender, RoutedEventArgs e)
    {
        var shortcutsWindow = new KeyboardShortcutsWindow();
        await shortcutsWindow.ShowDialog(this);
    }
}
