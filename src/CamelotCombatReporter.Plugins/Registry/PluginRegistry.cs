using System.Collections.Concurrent;
using CamelotCombatReporter.Plugins.Abstractions;
using CamelotCombatReporter.Plugins.Loading;
using CamelotCombatReporter.Plugins.Manifest;

namespace CamelotCombatReporter.Plugins.Registry;

/// <summary>
/// Registry for tracking loaded plugins.
/// </summary>
public sealed class PluginRegistry : IPluginRegistry
{
    private readonly ConcurrentDictionary<string, LoadedPlugin> _plugins = new();

    /// <summary>
    /// Event raised when a plugin is loaded.
    /// </summary>
    public event EventHandler<PluginLoadedEventArgs>? PluginLoaded;

    /// <summary>
    /// Event raised when a plugin is unloaded.
    /// </summary>
    public event EventHandler<PluginUnloadedEventArgs>? PluginUnloaded;

    /// <summary>
    /// Event raised when a plugin's enabled state changes.
    /// </summary>
    public event EventHandler<PluginStateChangedEventArgs>? PluginStateChanged;

    /// <summary>
    /// Gets all loaded plugins.
    /// </summary>
    public IReadOnlyCollection<LoadedPlugin> GetAllPlugins()
    {
        return _plugins.Values.ToList().AsReadOnly();
    }

    /// <summary>
    /// Gets all enabled plugins.
    /// </summary>
    public IReadOnlyCollection<LoadedPlugin> GetEnabledPlugins()
    {
        return _plugins.Values.Where(p => p.IsEnabled).ToList().AsReadOnly();
    }

    /// <summary>
    /// Gets a plugin by ID.
    /// </summary>
    public LoadedPlugin? GetPlugin(string pluginId)
    {
        return _plugins.TryGetValue(pluginId, out var plugin) ? plugin : null;
    }

    /// <summary>
    /// Checks if a plugin is loaded.
    /// </summary>
    public bool IsLoaded(string pluginId)
    {
        return _plugins.ContainsKey(pluginId);
    }

    /// <summary>
    /// Gets all plugins of a specific type.
    /// </summary>
    public IReadOnlyCollection<T> GetPlugins<T>() where T : IPlugin
    {
        return _plugins.Values
            .Where(p => p.IsEnabled && p.Instance is T)
            .Select(p => (T)p.Instance)
            .ToList()
            .AsReadOnly();
    }

    /// <summary>
    /// Gets all plugins of a specific plugin type.
    /// </summary>
    public IReadOnlyCollection<LoadedPlugin> GetPluginsByType(PluginType type)
    {
        return _plugins.Values
            .Where(p => p.Manifest.Type == type)
            .ToList()
            .AsReadOnly();
    }

    /// <summary>
    /// Registers a loaded plugin.
    /// </summary>
    internal bool Register(LoadedPlugin plugin)
    {
        if (_plugins.TryAdd(plugin.Manifest.Id, plugin))
        {
            PluginLoaded?.Invoke(this, new PluginLoadedEventArgs(plugin));
            return true;
        }
        return false;
    }

    /// <summary>
    /// Unregisters a plugin.
    /// </summary>
    internal bool Unregister(string pluginId)
    {
        if (_plugins.TryRemove(pluginId, out var plugin))
        {
            PluginUnloaded?.Invoke(this, new PluginUnloadedEventArgs(pluginId, plugin.Manifest.Name));
            return true;
        }
        return false;
    }

    /// <summary>
    /// Updates plugin enabled state.
    /// </summary>
    internal void SetEnabled(string pluginId, bool enabled)
    {
        if (_plugins.TryGetValue(pluginId, out var plugin))
        {
            var previousState = plugin.IsEnabled;
            plugin.IsEnabled = enabled;

            if (previousState != enabled)
            {
                PluginStateChanged?.Invoke(this, new PluginStateChangedEventArgs(
                    pluginId,
                    enabled ? PluginState.Enabled : PluginState.Disabled));
            }
        }
    }
}

/// <summary>
/// Interface for plugin registry.
/// </summary>
public interface IPluginRegistry
{
    IReadOnlyCollection<LoadedPlugin> GetAllPlugins();
    IReadOnlyCollection<LoadedPlugin> GetEnabledPlugins();
    LoadedPlugin? GetPlugin(string pluginId);
    bool IsLoaded(string pluginId);
    IReadOnlyCollection<T> GetPlugins<T>() where T : IPlugin;
    IReadOnlyCollection<LoadedPlugin> GetPluginsByType(PluginType type);

    event EventHandler<PluginLoadedEventArgs>? PluginLoaded;
    event EventHandler<PluginUnloadedEventArgs>? PluginUnloaded;
    event EventHandler<PluginStateChangedEventArgs>? PluginStateChanged;
}

/// <summary>
/// Event args for plugin loaded events.
/// </summary>
public sealed class PluginLoadedEventArgs : EventArgs
{
    public LoadedPlugin Plugin { get; }
    public string PluginId => Plugin.Manifest.Id;
    public string PluginName => Plugin.Manifest.Name;
    public PluginTrustLevel TrustLevel => Plugin.TrustLevel;

    public PluginLoadedEventArgs(LoadedPlugin plugin)
    {
        Plugin = plugin;
    }
}

/// <summary>
/// Event args for plugin unloaded events.
/// </summary>
public sealed class PluginUnloadedEventArgs : EventArgs
{
    public string PluginId { get; }
    public string PluginName { get; }

    public PluginUnloadedEventArgs(string pluginId, string pluginName)
    {
        PluginId = pluginId;
        PluginName = pluginName;
    }
}

/// <summary>
/// Event args for plugin state changed events.
/// </summary>
public sealed class PluginStateChangedEventArgs : EventArgs
{
    public string PluginId { get; }
    public PluginState NewState { get; }

    public PluginStateChangedEventArgs(string pluginId, PluginState newState)
    {
        PluginId = pluginId;
        NewState = newState;
    }
}
