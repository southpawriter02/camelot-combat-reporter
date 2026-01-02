using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Styling;
using CamelotCombatReporter.Gui.Plugins.Views;
using CamelotCombatReporter.Gui.ViewModels;
using System.Linq;

namespace CamelotCombatReporter.Gui.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void OnDrop(object? sender, DragEventArgs e)
    {
        var files = e.Data.GetFiles();
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
        var app = Avalonia.Application.Current;
        if (app != null)
        {
            var theme = app.RequestedThemeVariant;
            app.RequestedThemeVariant = (theme == ThemeVariant.Dark)
                ? ThemeVariant.Light
                : ThemeVariant.Dark;
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
            Width = 400,
            Height = 200,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Content = new StackPanel
            {
                Margin = new Avalonia.Thickness(20),
                Spacing = 10,
                Children =
                {
                    new TextBlock
                    {
                        Text = "Camelot Combat Reporter",
                        FontSize = 20,
                        FontWeight = Avalonia.Media.FontWeight.Bold,
                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
                    },
                    new TextBlock
                    {
                        Text = "Version 1.0.0",
                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
                    },
                    new TextBlock
                    {
                        Text = "A combat log analyzer for Dark Age of Camelot",
                        TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
                    }
                }
            }
        };

        await dialog.ShowDialog(this);
    }
}
