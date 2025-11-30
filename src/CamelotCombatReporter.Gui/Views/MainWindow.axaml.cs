using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Styling;
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
}
