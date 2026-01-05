using Avalonia.Controls;
using CamelotCombatReporter.Gui.Comparison.ViewModels;

namespace CamelotCombatReporter.Gui.Comparison.Views;

/// <summary>
/// Code-behind for the SessionComparisonView.
/// </summary>
public partial class SessionComparisonView : UserControl
{
    public SessionComparisonView()
    {
        InitializeComponent();
        DataContext = new SessionComparisonViewModel();
    }

    public SessionComparisonView(SessionComparisonViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
