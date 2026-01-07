using Avalonia.Controls;
using CamelotCombatReporter.Core.CharacterBuilding.Models;
using CamelotCombatReporter.Gui.CharacterBuilding.ViewModels;

namespace CamelotCombatReporter.Gui.CharacterBuilding.Views;

public partial class ProfileEditorDialog : Window
{
    public ProfileEditorDialog() : this(null)
    {
    }

    public ProfileEditorDialog(CharacterProfile? existingProfile)
    {
        InitializeComponent();
        
        var viewModel = new ProfileEditorViewModel(existingProfile);
        viewModel.CloseRequested += (_, result) => Close(result);
        DataContext = viewModel;
    }
}
