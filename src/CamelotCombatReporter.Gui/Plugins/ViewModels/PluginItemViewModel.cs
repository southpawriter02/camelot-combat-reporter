using System;
using System.Collections.ObjectModel;
using System.Linq;
using CamelotCombatReporter.Gui.ViewModels;
using CamelotCombatReporter.Plugins.Abstractions;
using CamelotCombatReporter.Plugins.Loading;
using CamelotCombatReporter.Plugins.Manifest;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CamelotCombatReporter.Gui.Plugins.ViewModels;

/// <summary>
/// ViewModel for a single plugin item in the Plugin Manager.
/// </summary>
public partial class PluginItemViewModel : ViewModelBase
{
    private readonly LoadedPlugin _loadedPlugin;

    public string Id => _loadedPlugin.Manifest.Id;
    public string Name => _loadedPlugin.Manifest.Name;
    public string Version => _loadedPlugin.Manifest.Version;
    public string Author => _loadedPlugin.Manifest.Author;
    public string Description => _loadedPlugin.Manifest.Description;
    public PluginType Type => _loadedPlugin.Manifest.Type;
    public PluginTrustLevel TrustLevel => _loadedPlugin.TrustLevel;
    public DateTime LoadedAt => _loadedPlugin.LoadedAt;

    [ObservableProperty]
    private bool _isEnabled;

    public string TypeDisplayName => Type switch
    {
        PluginType.DataAnalysis => "Data Analysis",
        PluginType.ExportFormat => "Export Format",
        PluginType.UIComponent => "UI Component",
        PluginType.CustomParser => "Custom Parser",
        _ => "Unknown"
    };

    public string TrustLevelDisplayName => TrustLevel switch
    {
        PluginTrustLevel.OfficialTrusted => "Official",
        PluginTrustLevel.SignedTrusted => "Signed",
        PluginTrustLevel.UserTrusted => "User Trusted",
        PluginTrustLevel.Untrusted => "Untrusted",
        _ => "Unknown"
    };

    public string StatusIcon => IsEnabled ? "CheckCircle" : "PauseCircle";

    public ObservableCollection<string> Permissions { get; }

    public PluginItemViewModel(LoadedPlugin loadedPlugin)
    {
        _loadedPlugin = loadedPlugin;
        _isEnabled = loadedPlugin.IsEnabled;
        Permissions = new ObservableCollection<string>(
            loadedPlugin.GrantedPermissions.Select(p => p.ToString()));
    }

    public void UpdateEnabled(bool enabled)
    {
        IsEnabled = enabled;
        OnPropertyChanged(nameof(StatusIcon));
    }
}
