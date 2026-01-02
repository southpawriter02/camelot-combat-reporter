using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using CamelotCombatReporter.Gui.ViewModels;
using CamelotCombatReporter.Plugins.Loading;
using CamelotCombatReporter.Plugins.Registry;
using CamelotCombatReporter.Plugins.Security;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CamelotCombatReporter.Gui.Plugins.ViewModels;

/// <summary>
/// ViewModel for the Plugin Manager window.
/// </summary>
public partial class PluginManagerViewModel : ViewModelBase
{
    private readonly PluginLoaderService _loaderService;
    private readonly bool _ownsLoaderService;

    [ObservableProperty]
    private ObservableCollection<PluginItemViewModel> _plugins = new();

    [ObservableProperty]
    private PluginItemViewModel? _selectedPlugin;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _searchFilter = "";

    [ObservableProperty]
    private string _statusMessage = "";

    public int TotalPlugins => Plugins.Count;
    public int EnabledPlugins => Plugins.Count(p => p.IsEnabled);

    public PluginManagerViewModel(string pluginsDirectory)
    {
        var auditLogger = new SecurityAuditLogger(pluginsDirectory);
        _loaderService = new PluginLoaderService(pluginsDirectory, auditLogger);
        _ownsLoaderService = true;
        _ = InitializeAsync();
    }

    public PluginManagerViewModel(PluginLoaderService loaderService)
    {
        _loaderService = loaderService;
        _ownsLoaderService = false;
        LoadPlugins();
    }

    private async Task InitializeAsync()
    {
        IsLoading = true;
        StatusMessage = "Loading plugins...";

        try
        {
            await _loaderService.LoadAllPluginsAsync();
            LoadPlugins();
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void LoadPlugins()
    {
        Plugins.Clear();

        foreach (var plugin in _loaderService.Registry.GetAllPlugins())
        {
            Plugins.Add(new PluginItemViewModel(plugin));
        }

        OnPropertyChanged(nameof(TotalPlugins));
        OnPropertyChanged(nameof(EnabledPlugins));
        UpdateStatusMessage();
    }

    [RelayCommand]
    private async Task InstallPlugin()
    {
        var topLevel = GetMainWindow();
        if (topLevel == null) return;

        var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Select Plugin Folder",
            AllowMultiple = false
        });

        if (folders.Count > 0)
        {
            IsLoading = true;
            StatusMessage = "Installing plugin...";

            try
            {
                var result = await _loaderService.InstallPluginAsync(folders[0].Path.LocalPath);

                if (result.IsSuccess)
                {
                    LoadPlugins();
                    StatusMessage = $"Plugin '{result.Plugin?.Manifest.Name}' installed successfully.";
                }
                else
                {
                    StatusMessage = $"Failed to install plugin: {result.Error}";
                }
            }
            finally
            {
                IsLoading = false;
            }
        }
    }

    [RelayCommand]
    private async Task ToggleEnabled()
    {
        if (SelectedPlugin == null) return;

        IsLoading = true;

        try
        {
            if (SelectedPlugin.IsEnabled)
            {
                await _loaderService.DisablePluginAsync(SelectedPlugin.Id);
                SelectedPlugin.UpdateEnabled(false);
                StatusMessage = $"Plugin '{SelectedPlugin.Name}' disabled.";
            }
            else
            {
                await _loaderService.EnablePluginAsync(SelectedPlugin.Id);
                SelectedPlugin.UpdateEnabled(true);
                StatusMessage = $"Plugin '{SelectedPlugin.Name}' enabled.";
            }

            OnPropertyChanged(nameof(EnabledPlugins));
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task UninstallPlugin()
    {
        if (SelectedPlugin == null) return;

        IsLoading = true;
        var pluginName = SelectedPlugin.Name;

        try
        {
            await _loaderService.UnloadPluginAsync(SelectedPlugin.Id);
            LoadPlugins();
            SelectedPlugin = null;
            StatusMessage = $"Plugin '{pluginName}' uninstalled.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void Refresh()
    {
        LoadPlugins();
        StatusMessage = "Plugin list refreshed.";
    }

    private void UpdateStatusMessage()
    {
        StatusMessage = $"{TotalPlugins} plugins installed, {EnabledPlugins} enabled";
    }

    private static Avalonia.Controls.Window? GetMainWindow()
    {
        return Avalonia.Application.Current?.ApplicationLifetime is
            Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
            ? desktop.MainWindow
            : null;
    }
}
