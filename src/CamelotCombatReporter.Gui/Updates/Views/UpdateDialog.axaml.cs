using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using CamelotCombatReporter.Gui.Updates.ViewModels;

namespace CamelotCombatReporter.Gui.Updates.Views;

/// <summary>
/// Code-behind for the update dialog.
/// </summary>
public partial class UpdateDialog : Window
{
    /// <summary>
    /// Creates a new instance of the update dialog.
    /// </summary>
    public UpdateDialog()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Creates a new instance of the update dialog with a view model.
    /// </summary>
    /// <param name="viewModel">The view model to use.</param>
    public UpdateDialog(UpdateViewModel viewModel) : this()
    {
        DataContext = viewModel;
        viewModel.CloseRequested += OnCloseRequested;
    }

    private void OnCloseRequested(object? sender, bool result)
    {
        Close(result);
    }

    private void OnCloseClick(object? sender, RoutedEventArgs e)
    {
        Close(false);
    }

    /// <summary>
    /// Shows the update dialog and checks for updates automatically.
    /// </summary>
    /// <param name="owner">The owner window.</param>
    /// <param name="autoCheck">Whether to automatically check for updates.</param>
    /// <returns>True if an update was installed.</returns>
    public static async Task<bool> ShowDialogAsync(Window owner, bool autoCheck = true)
    {
        var viewModel = new UpdateViewModel();
        var dialog = new UpdateDialog(viewModel);

        if (autoCheck)
        {
            // Start checking for updates when dialog opens
            _ = viewModel.CheckForUpdatesCommand.ExecuteAsync(null);
        }

        var result = await dialog.ShowDialog<bool?>(owner);
        return result == true;
    }
}
