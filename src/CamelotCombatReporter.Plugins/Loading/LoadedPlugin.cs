using CamelotCombatReporter.Plugins.Abstractions;
using CamelotCombatReporter.Plugins.Manifest;
using CamelotCombatReporter.Plugins.Permissions;

namespace CamelotCombatReporter.Plugins.Loading;

/// <summary>
/// Represents a loaded plugin with its context and state.
/// </summary>
public sealed class LoadedPlugin : IDisposable, IAsyncDisposable
{
    /// <summary>
    /// The plugin manifest.
    /// </summary>
    public PluginManifest Manifest { get; }

    /// <summary>
    /// The plugin instance.
    /// </summary>
    public IPlugin Instance { get; }

    /// <summary>
    /// The isolated load context.
    /// </summary>
    public PluginLoadContext LoadContext { get; }

    /// <summary>
    /// The sandboxed plugin context.
    /// </summary>
    public IPluginContext Context { get; }

    /// <summary>
    /// Path to the plugin directory.
    /// </summary>
    public string PluginDirectory { get; }

    /// <summary>
    /// Granted permissions.
    /// </summary>
    public IReadOnlyCollection<PluginPermission> GrantedPermissions { get; }

    /// <summary>
    /// Trust level determined during loading.
    /// </summary>
    public PluginTrustLevel TrustLevel { get; }

    /// <summary>
    /// When the plugin was loaded.
    /// </summary>
    public DateTime LoadedAt { get; }

    /// <summary>
    /// Whether the plugin is enabled.
    /// </summary>
    public bool IsEnabled { get; internal set; }

    private bool _disposed;

    public LoadedPlugin(
        PluginManifest manifest,
        IPlugin instance,
        PluginLoadContext loadContext,
        IPluginContext context,
        string pluginDirectory,
        IReadOnlyCollection<PluginPermission> grantedPermissions,
        PluginTrustLevel trustLevel)
    {
        Manifest = manifest;
        Instance = instance;
        LoadContext = loadContext;
        Context = context;
        PluginDirectory = pluginDirectory;
        GrantedPermissions = grantedPermissions;
        TrustLevel = trustLevel;
        LoadedAt = DateTime.UtcNow;
        IsEnabled = false;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        try
        {
            Instance.Dispose();
        }
        catch
        {
            // Suppress disposal errors
        }

        // Unload the assembly context
        LoadContext.Unload();

        // Request garbage collection to reclaim memory
        GC.Collect();
        GC.WaitForPendingFinalizers();
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        try
        {
            await Instance.OnUnloadAsync();
            Instance.Dispose();
        }
        catch
        {
            // Suppress disposal errors
        }

        LoadContext.Unload();

        GC.Collect();
        GC.WaitForPendingFinalizers();
    }
}

/// <summary>
/// Result of a plugin load operation.
/// </summary>
public sealed record PluginLoadResult
{
    public bool IsSuccess { get; init; }
    public LoadedPlugin? Plugin { get; init; }
    public string? Error { get; init; }
    public PluginLoadErrorType? ErrorType { get; init; }

    private PluginLoadResult() { }

    public static PluginLoadResult Success(LoadedPlugin plugin) =>
        new() { IsSuccess = true, Plugin = plugin };

    public static PluginLoadResult Failure(string error, PluginLoadErrorType errorType = PluginLoadErrorType.Unknown) =>
        new() { IsSuccess = false, Error = error, ErrorType = errorType };

    public static PluginLoadResult ManifestError(string error) =>
        Failure(error, PluginLoadErrorType.ManifestInvalid);

    public static PluginLoadResult SecurityError(string error) =>
        Failure(error, PluginLoadErrorType.SecurityViolation);

    public static PluginLoadResult CompatibilityError(string error) =>
        Failure(error, PluginLoadErrorType.IncompatibleVersion);

    public static PluginLoadResult DependencyError(string error) =>
        Failure(error, PluginLoadErrorType.DependencyMissing);

    public static PluginLoadResult PermissionDenied(string error) =>
        Failure(error, PluginLoadErrorType.PermissionDenied);

    public static PluginLoadResult LoadError(string error) =>
        Failure(error, PluginLoadErrorType.AssemblyLoadFailed);

    public static PluginLoadResult InitializationError(string error) =>
        Failure(error, PluginLoadErrorType.InitializationFailed);
}

/// <summary>
/// Types of plugin load errors.
/// </summary>
public enum PluginLoadErrorType
{
    Unknown,
    ManifestNotFound,
    ManifestInvalid,
    SecurityViolation,
    SignatureInvalid,
    IncompatibleVersion,
    DependencyMissing,
    PermissionDenied,
    AssemblyLoadFailed,
    TypeNotFound,
    InitializationFailed,
    AlreadyLoaded
}
