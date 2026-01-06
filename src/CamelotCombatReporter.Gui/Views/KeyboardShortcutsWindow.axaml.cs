using Avalonia.Controls;
using Avalonia.Interactivity;

namespace CamelotCombatReporter.Gui.Views;

/// <summary>
/// Window displaying available keyboard shortcuts.
/// </summary>
public partial class KeyboardShortcutsWindow : Window
{
    public KeyboardShortcutsWindow()
    {
        InitializeComponent();
    }

    private void OnCloseClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
