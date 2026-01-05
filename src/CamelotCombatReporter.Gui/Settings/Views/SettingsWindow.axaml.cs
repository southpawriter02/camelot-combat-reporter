using Avalonia.Controls;
using Avalonia.Interactivity;

namespace CamelotCombatReporter.Gui.Settings.Views;

public partial class SettingsWindow : Window
{
    public SettingsWindow()
    {
        InitializeComponent();
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    private void OnSaveClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
