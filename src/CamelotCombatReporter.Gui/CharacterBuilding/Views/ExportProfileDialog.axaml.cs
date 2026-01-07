using Avalonia.Controls;
using CamelotCombatReporter.Gui.CharacterBuilding.ViewModels;

namespace CamelotCombatReporter.Gui.CharacterBuilding.Views;

public partial class ExportProfileDialog : Window
{
    public ExportProfileDialog()
    {
        InitializeComponent();
    }

    public ExportProfileDialog(ExportProfileViewModel viewModel) : this()
    {
        DataContext = viewModel;
        viewModel.RequestClose += OnRequestClose;
    }

    private void OnRequestClose(bool result)
    {
        if (DataContext is ExportProfileViewModel vm)
        {
            vm.RequestClose -= OnRequestClose;
        }
        Close(result);
    }
}
