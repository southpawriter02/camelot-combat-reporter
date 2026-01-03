using System;
using Avalonia.Controls;
using CamelotCombatReporter.Core.Logging;
using CamelotCombatReporter.Gui.LootTracking.ViewModels;
using Microsoft.Extensions.Logging;

namespace CamelotCombatReporter.Gui.LootTracking.Views;

public partial class LootTrackingView : UserControl
{
    private readonly ILogger<LootTrackingView> _logger;

    public LootTrackingView()
    {
        InitializeComponent();
        _logger = App.CreateLogger<LootTrackingView>();
        DataContext = new LootTrackingViewModel();
    }

    protected override async void OnLoaded(Avalonia.Interactivity.RoutedEventArgs e)
    {
        base.OnLoaded(e);

        try
        {
            if (DataContext is LootTrackingViewModel viewModel)
            {
                await viewModel.InitializeAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogUnexpectedError("LootTrackingViewModel initialization", ex);
        }
    }
}
