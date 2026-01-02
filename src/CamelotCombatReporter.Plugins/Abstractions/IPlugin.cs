using CamelotCombatReporter.Plugins.Permissions;

namespace CamelotCombatReporter.Plugins.Abstractions;

/// <summary>
/// Base interface for all plugins in the Camelot Combat Reporter.
/// </summary>
public interface IPlugin : IDisposable
{
    /// <summary>
    /// Unique identifier for the plugin.
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Human-readable name of the plugin.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Version of the plugin following semantic versioning.
    /// </summary>
    Version Version { get; }

    /// <summary>
    /// Author or organization that created the plugin.
    /// </summary>
    string Author { get; }

    /// <summary>
    /// Brief description of what the plugin does.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// The type of plugin (Analysis, Export, UI, Parser).
    /// </summary>
    PluginType Type { get; }

    /// <summary>
    /// Current state of the plugin.
    /// </summary>
    PluginState State { get; }

    /// <summary>
    /// Permissions required by this plugin.
    /// </summary>
    IReadOnlyCollection<PluginPermission> RequiredPermissions { get; }

    /// <summary>
    /// Called when the plugin is loaded into memory.
    /// </summary>
    Task OnLoadAsync(IPluginContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Called to initialize the plugin with granted permissions.
    /// </summary>
    Task InitializeAsync(IPluginContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Called when the plugin is enabled.
    /// </summary>
    Task OnEnableAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Called when the plugin is disabled.
    /// </summary>
    Task OnDisableAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Called before the plugin is unloaded.
    /// </summary>
    Task OnUnloadAsync(CancellationToken cancellationToken = default);
}
