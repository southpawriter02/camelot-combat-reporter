using Avalonia.Controls;
using CamelotCombatReporter.Gui.LootTracking.ViewModels;

namespace CamelotCombatReporter.Gui.LootTracking.Views;

public partial class LootTrackingView : UserControl
{
    public LootTrackingView()
    {
        InitializeComponent();
        DataContext = new LootTrackingViewModel();
    }

    protected override async void OnLoaded(Avalonia.Interactivity.RoutedEventArgs e)
    {
        base.OnLoaded(e);

        if (DataContext is LootTrackingViewModel viewModel)
        {
            await viewModel.InitializeAsync();
        }
    }
}
