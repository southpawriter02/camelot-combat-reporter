using Avalonia.Controls;
using CamelotCombatReporter.Gui.Alerts.ViewModels;

namespace CamelotCombatReporter.Gui.Alerts.Views;

/// <summary>
/// Code-behind for the AlertsView.
/// </summary>
public partial class AlertsView : UserControl
{
    public AlertsView()
    {
        InitializeComponent();
        DataContext = new AlertsViewModel();
    }

    public AlertsView(AlertsViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
