using System.Threading.Tasks;
using Avalonia.Controls;
using CamelotCombatReporter.Core.Models;
using CamelotCombatReporter.Gui.CrossRealm.ViewModels;

namespace CamelotCombatReporter.Gui.CrossRealm.Views;

public partial class CharacterConfigDialog : Window
{
    private CharacterInfo? _result;

    public CharacterConfigDialog()
    {
        InitializeComponent();
    }

    public CharacterConfigDialog(CharacterInfo? existingCharacter) : this()
    {
        var viewModel = new CharacterConfigViewModel(existingCharacter);
        DataContext = viewModel;

        viewModel.Saved += (_, character) =>
        {
            _result = character;
            Close(_result);
        };

        viewModel.Cancelled += (_, _) =>
        {
            Close(null);
        };
    }

    /// <summary>
    /// Shows the dialog and returns the configured character info, or null if cancelled.
    /// </summary>
    public static async Task<CharacterInfo?> ShowDialogAsync(Window parent, CharacterInfo? existingCharacter = null)
    {
        var dialog = new CharacterConfigDialog(existingCharacter);
        return await dialog.ShowDialog<CharacterInfo?>(parent);
    }
}
