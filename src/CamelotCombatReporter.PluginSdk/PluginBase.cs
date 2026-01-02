using CamelotCombatReporter.Plugins.Abstractions;
using CamelotCombatReporter.Plugins.Manifest;
using CamelotCombatReporter.Plugins.Permissions;

namespace CamelotCombatReporter.PluginSdk;

/// <summary>
/// Base class for all plugins providing common functionality.
/// Plugin developers should extend this class or one of its specialized subclasses.
/// </summary>
public abstract class PluginBase : IPlugin
{
    private IPluginContext? _context;
    private bool _disposed;

    /// <summary>
    /// Unique identifier for the plugin.
    /// </summary>
    public abstract string Id { get; }

    /// <summary>
    /// Display name for the plugin.
    /// </summary>
    public abstract string Name { get; }

    /// <summary>
    /// Plugin version.
    /// </summary>
    public abstract Version Version { get; }

    /// <summary>
    /// Plugin author.
    /// </summary>
    public abstract string Author { get; }

    /// <summary>
    /// Plugin description.
    /// </summary>
    public abstract string Description { get; }

    /// <summary>
    /// Plugin type.
    /// </summary>
    public abstract PluginType Type { get; }

    /// <summary>
    /// Current plugin state.
    /// </summary>
    public PluginState State { get; protected set; } = PluginState.Unloaded;

    /// <summary>
    /// Required permissions for this plugin.
    /// Override to specify permissions needed by your plugin.
    /// </summary>
    public virtual IReadOnlyCollection<PluginPermission> RequiredPermissions { get; } =
        Array.Empty<PluginPermission>();

    /// <summary>
    /// The plugin context providing access to application services.
    /// </summary>
    protected IPluginContext Context =>
        _context ?? throw new InvalidOperationException("Plugin has not been loaded.");

    /// <summary>
    /// Called when the plugin is being loaded.
    /// </summary>
    public virtual Task OnLoadAsync(IPluginContext context, CancellationToken ct = default)
    {
        _context = context;
        State = PluginState.Loaded;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Called to initialize the plugin after loading.
    /// Override to perform initialization logic.
    /// </summary>
    public virtual Task InitializeAsync(IPluginContext context, CancellationToken ct = default)
    {
        State = PluginState.Initialized;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Called when the plugin is being enabled.
    /// Override to perform activation logic.
    /// </summary>
    public virtual Task OnEnableAsync(CancellationToken ct = default)
    {
        State = PluginState.Enabled;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Called when the plugin is being disabled.
    /// Override to perform deactivation logic.
    /// </summary>
    public virtual Task OnDisableAsync(CancellationToken ct = default)
    {
        State = PluginState.Disabled;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Called when the plugin is being unloaded.
    /// Override to perform cleanup logic.
    /// </summary>
    public virtual Task OnUnloadAsync(CancellationToken ct = default)
    {
        State = PluginState.Unloaded;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Logs a debug message.
    /// </summary>
    protected void LogDebug(string message)
    {
        Context.Logger.Debug(message);
    }

    /// <summary>
    /// Logs an information message.
    /// </summary>
    protected void LogInfo(string message)
    {
        Context.Logger.Info(message);
    }

    /// <summary>
    /// Logs a warning message.
    /// </summary>
    protected void LogWarning(string message)
    {
        Context.Logger.Warning(message);
    }

    /// <summary>
    /// Logs an error message.
    /// </summary>
    protected void LogError(string message, Exception? exception = null)
    {
        Context.Logger.Error(message, exception);
    }

    /// <summary>
    /// Disposes the plugin resources.
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Override to dispose managed resources.
    /// </summary>
    protected virtual void Dispose(bool disposing)
    {
    }
}
