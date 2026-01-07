using Avalonia.Controls;
using CamelotCombatReporter.Gui.CharacterBuilding.ViewModels;

namespace CamelotCombatReporter.Gui.CharacterBuilding.Views;

public partial class CharacterProfilesView : UserControl
{
    private readonly CharacterProfilesViewModel _viewModel;

    public CharacterProfilesView()
    {
        InitializeComponent();
        
        _viewModel = new CharacterProfilesViewModel();
        DataContext = _viewModel;
        
        // Initialize when view is loaded
        Loaded += async (_, _) => await _viewModel.InitializeAsync();
    }
}
