using Avalonia.Controls;
using CamelotCombatReporter.Gui.CharacterBuilding.ViewModels;

namespace CamelotCombatReporter.Gui.CharacterBuilding.Views;

public partial class TemplateSelectionDialog : Window
{
    public TemplateSelectionDialog()
    {
        InitializeComponent();
    }

    public TemplateSelectionDialog(TemplateSelectionViewModel viewModel) : this()
    {
        DataContext = viewModel;
        viewModel.RequestClose += OnRequestClose;
    }

    private void OnRequestClose(bool result)
    {
        if (DataContext is TemplateSelectionViewModel vm)
        {
            vm.RequestClose -= OnRequestClose;
        }
        Close(result);
    }
}
