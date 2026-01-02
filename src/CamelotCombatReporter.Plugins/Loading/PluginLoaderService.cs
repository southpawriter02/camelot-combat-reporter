using System.Reflection;
using CamelotCombatReporter.Plugins.Abstractions;
using CamelotCombatReporter.Plugins.Manifest;
using CamelotCombatReporter.Plugins.Permissions;
using CamelotCombatReporter.Plugins.Registry;
using CamelotCombatReporter.Plugins.Sandbox;
using CamelotCombatReporter.Plugins.Security;

namespace CamelotCombatReporter.Plugins.Loading;

/// <summary>
/// Service for loading, managing, and unloading plugins.
/// </summary>
public sealed class PluginLoaderService : IAsyncDisposable
{
    private readonly string _pluginsDirectory;
    private readonly PluginRegistry _registry;
    private readonly ManifestReader _manifestReader;
    private readonly PluginVerificationService _verificationService;
    private readonly PermissionManager _permissionManager;
    private readonly ISecurityAuditLogger _auditLogger;
    private readonly Version _applicationVersion;
    private readonly SemaphoreSlim _loadLock = new(1, 1);
    private readonly bool _requireSignedPlugins;

    public PluginLoaderService(
        string pluginsDirectory,
        ISecurityAuditLogger auditLogger,
        IPermissionPromptService? permissionPromptService = null,
        bool requireSignedPlugins = false)
    {
        _pluginsDirectory = pluginsDirectory;
        _auditLogger = auditLogger;
        _requireSignedPlugins = requireSignedPlugins;
        _applicationVersion = Assembly.GetEntryAssembly()?.GetName().Version ?? new Version(1, 0, 0);

        _registry = new PluginRegistry();
        _manifestReader = new ManifestReader();
        _verificationService = new PluginVerificationService(auditLogger);
        _permissionManager = new PermissionManager(pluginsDirectory, permissionPromptService);

        // Ensure directories exist
        Directory.CreateDirectory(Path.Combine(pluginsDirectory, "installed"));
        Directory.CreateDirectory(Path.Combine(pluginsDirectory, "disabled"));
    }

    /// <summary>
    /// Gets the plugin registry.
    /// </summary>
    public IPluginRegistry Registry => _registry;

    /// <summary>
    /// Discovers all plugins in the plugins directory.
    /// </summary>
    public async Task<IReadOnlyCollection<PluginManifest>> DiscoverPluginsAsync(CancellationToken ct = default)
    {
        var manifests = new List<PluginManifest>();
        var installedDir = Path.Combine(_pluginsDirectory, "installed");

        if (!Directory.Exists(installedDir))
        {
            return manifests.AsReadOnly();
        }

        foreach (var pluginDir in Directory.GetDirectories(installedDir))
        {
            var manifestPath = Path.Combine(pluginDir, "plugin.json");
            if (!File.Exists(manifestPath)) continue;

            var result = await _manifestReader.ReadFromFileAsync(manifestPath, ct);
            if (result.IsSuccess && result.Manifest != null)
            {
                manifests.Add(result.Manifest);
            }
        }

        return manifests.AsReadOnly();
    }

    /// <summary>
    /// Loads a plugin from a directory.
    /// </summary>
    public async Task<PluginLoadResult> LoadPluginAsync(
        string pluginDirectory,
        CancellationToken ct = default)
    {
        await _loadLock.WaitAsync(ct);
        try
        {
            // Step 1: Read and validate manifest
            var manifestPath = Path.Combine(pluginDirectory, "plugin.json");
            var manifestResult = await _manifestReader.ReadFromFileAsync(manifestPath, ct);

            if (!manifestResult.IsSuccess || manifestResult.Manifest == null)
            {
                return PluginLoadResult.ManifestError(manifestResult.Error ?? "Failed to read manifest");
            }

            var manifest = manifestResult.Manifest;

            // Step 2: Check if already loaded
            if (_registry.IsLoaded(manifest.Id))
            {
                return PluginLoadResult.Failure($"Plugin '{manifest.Id}' is already loaded", PluginLoadErrorType.AlreadyLoaded);
            }

            _auditLogger.LogPluginLifecycle(manifest.Id, PluginLifecycleEvent.Loading);

            // Step 3: Verify signature
            var verificationResult = await _verificationService.VerifyAsync(pluginDirectory, manifest, ct);

            if (!verificationResult.IsSuccess && _requireSignedPlugins)
            {
                return PluginLoadResult.SecurityError($"Signature verification failed: {verificationResult.Error}");
            }

            var trustLevel = verificationResult.TrustLevel;

            // Step 4: Check compatibility
            var compatResult = CheckCompatibility(manifest);
            if (!compatResult.IsCompatible)
            {
                return PluginLoadResult.CompatibilityError(compatResult.Error!);
            }

            // Step 5: Check dependencies
            var depResult = CheckDependencies(manifest);
            if (!depResult.Success)
            {
                return PluginLoadResult.DependencyError(depResult.Error!);
            }

            // Step 6: Request permissions
            var permResult = await _permissionManager.RequestPermissionsAsync(
                manifest.Id,
                manifest.Permissions,
                trustLevel,
                ct);

            // Step 7: Create isolated load context
            var assemblyPath = Path.Combine(pluginDirectory, manifest.EntryPoint.Assembly);
            var loadContext = new PluginLoadContext(assemblyPath, manifest.Id);

            // Step 8: Load assembly
            Assembly assembly;
            try
            {
                assembly = loadContext.LoadFromAssemblyPath(assemblyPath);
            }
            catch (Exception ex)
            {
                loadContext.Unload();
                return PluginLoadResult.LoadError($"Failed to load assembly: {ex.Message}");
            }

            // Step 9: Find and create plugin instance
            var pluginType = assembly.GetType(manifest.EntryPoint.TypeName);
            if (pluginType == null)
            {
                loadContext.Unload();
                return PluginLoadResult.Failure(
                    $"Type '{manifest.EntryPoint.TypeName}' not found in assembly",
                    PluginLoadErrorType.TypeNotFound);
            }

            if (!typeof(IPlugin).IsAssignableFrom(pluginType))
            {
                loadContext.Unload();
                return PluginLoadResult.Failure(
                    $"Type '{manifest.EntryPoint.TypeName}' does not implement IPlugin",
                    PluginLoadErrorType.TypeNotFound);
            }

            IPlugin plugin;
            try
            {
                plugin = (IPlugin)Activator.CreateInstance(pluginType)!;
            }
            catch (Exception ex)
            {
                loadContext.Unload();
                return PluginLoadResult.InitializationError($"Failed to create plugin instance: {ex.Message}");
            }

            // Step 10: Create sandboxed context
            var context = new SandboxedPluginContext(
                manifest.Id,
                pluginDirectory,
                permResult.Granted,
                manifest.Resources,
                _auditLogger,
                _applicationVersion);

            // Step 11: Initialize plugin
            try
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                cts.CancelAfter(TimeSpan.FromSeconds(30));

                await plugin.OnLoadAsync(context, cts.Token);

                _auditLogger.LogPluginLifecycle(manifest.Id, PluginLifecycleEvent.Loaded);

                await plugin.InitializeAsync(context, cts.Token);

                _auditLogger.LogPluginLifecycle(manifest.Id, PluginLifecycleEvent.Initialized);
            }
            catch (Exception ex)
            {
                plugin.Dispose();
                loadContext.Unload();
                return PluginLoadResult.InitializationError($"Plugin initialization failed: {ex.Message}");
            }

            // Step 12: Create and register loaded plugin
            var loadedPlugin = new LoadedPlugin(
                manifest,
                plugin,
                loadContext,
                context,
                pluginDirectory,
                permResult.Granted,
                trustLevel);

            _registry.Register(loadedPlugin);

            return PluginLoadResult.Success(loadedPlugin);
        }
        finally
        {
            _loadLock.Release();
        }
    }

    /// <summary>
    /// Enables a loaded plugin.
    /// </summary>
    public async Task<bool> EnablePluginAsync(string pluginId, CancellationToken ct = default)
    {
        var plugin = _registry.GetPlugin(pluginId);
        if (plugin == null || plugin.IsEnabled) return false;

        try
        {
            _auditLogger.LogPluginLifecycle(pluginId, PluginLifecycleEvent.Enabling);

            await plugin.Instance.OnEnableAsync(ct);
            _registry.SetEnabled(pluginId, true);

            _auditLogger.LogPluginLifecycle(pluginId, PluginLifecycleEvent.Enabled);
            return true;
        }
        catch (Exception ex)
        {
            _auditLogger.LogPluginLifecycle(pluginId, PluginLifecycleEvent.Error, ex.Message);
            return false;
        }
    }

    /// <summary>
    /// Disables a loaded plugin.
    /// </summary>
    public async Task<bool> DisablePluginAsync(string pluginId, CancellationToken ct = default)
    {
        var plugin = _registry.GetPlugin(pluginId);
        if (plugin == null || !plugin.IsEnabled) return false;

        try
        {
            _auditLogger.LogPluginLifecycle(pluginId, PluginLifecycleEvent.Disabling);

            await plugin.Instance.OnDisableAsync(ct);
            _registry.SetEnabled(pluginId, false);

            _auditLogger.LogPluginLifecycle(pluginId, PluginLifecycleEvent.Disabled);
            return true;
        }
        catch (Exception ex)
        {
            _auditLogger.LogPluginLifecycle(pluginId, PluginLifecycleEvent.Error, ex.Message);
            return false;
        }
    }

    /// <summary>
    /// Unloads a plugin.
    /// </summary>
    public async Task<bool> UnloadPluginAsync(string pluginId, CancellationToken ct = default)
    {
        var plugin = _registry.GetPlugin(pluginId);
        if (plugin == null) return false;

        try
        {
            _auditLogger.LogPluginLifecycle(pluginId, PluginLifecycleEvent.Unloading);

            if (plugin.IsEnabled)
            {
                await DisablePluginAsync(pluginId, ct);
            }

            _registry.Unregister(pluginId);
            await plugin.DisposeAsync();

            _auditLogger.LogPluginLifecycle(pluginId, PluginLifecycleEvent.Unloaded);
            return true;
        }
        catch (Exception ex)
        {
            _auditLogger.LogPluginLifecycle(pluginId, PluginLifecycleEvent.Error, ex.Message);
            return false;
        }
    }

    /// <summary>
    /// Loads all discovered plugins.
    /// </summary>
    public async Task<IReadOnlyCollection<PluginLoadResult>> LoadAllPluginsAsync(CancellationToken ct = default)
    {
        var results = new List<PluginLoadResult>();
        var installedDir = Path.Combine(_pluginsDirectory, "installed");

        if (!Directory.Exists(installedDir))
        {
            return results.AsReadOnly();
        }

        foreach (var pluginDir in Directory.GetDirectories(installedDir))
        {
            var result = await LoadPluginAsync(pluginDir, ct);
            results.Add(result);
        }

        return results.AsReadOnly();
    }

    /// <summary>
    /// Installs a plugin from a source directory.
    /// </summary>
    public async Task<PluginLoadResult> InstallPluginAsync(
        string sourceDirectory,
        CancellationToken ct = default)
    {
        // Read manifest to get plugin ID
        var manifestPath = Path.Combine(sourceDirectory, "plugin.json");
        var manifestResult = await _manifestReader.ReadFromFileAsync(manifestPath, ct);

        if (!manifestResult.IsSuccess || manifestResult.Manifest == null)
        {
            return PluginLoadResult.ManifestError(manifestResult.Error ?? "Invalid manifest");
        }

        var manifest = manifestResult.Manifest;
        var targetDir = Path.Combine(_pluginsDirectory, "installed", manifest.Id);

        // Check if already installed
        if (Directory.Exists(targetDir))
        {
            // Unload existing plugin first
            if (_registry.IsLoaded(manifest.Id))
            {
                await UnloadPluginAsync(manifest.Id, ct);
            }

            Directory.Delete(targetDir, recursive: true);
        }

        // Copy plugin files
        CopyDirectory(sourceDirectory, targetDir);

        // Load the installed plugin
        return await LoadPluginAsync(targetDir, ct);
    }

    private CompatibilityCheckResult CheckCompatibility(PluginManifest manifest)
    {
        if (manifest.Compatibility.MinAppVersion != null)
        {
            if (Version.TryParse(manifest.Compatibility.MinAppVersion, out var minVersion))
            {
                if (_applicationVersion < minVersion)
                {
                    return CompatibilityCheckResult.Incompatible(
                        $"Plugin requires app version {minVersion} or higher (current: {_applicationVersion})");
                }
            }
        }

        if (manifest.Compatibility.MaxAppVersion != null)
        {
            if (Version.TryParse(manifest.Compatibility.MaxAppVersion, out var maxVersion))
            {
                if (_applicationVersion > maxVersion)
                {
                    return CompatibilityCheckResult.Incompatible(
                        $"Plugin only supports app versions up to {maxVersion} (current: {_applicationVersion})");
                }
            }
        }

        return CompatibilityCheckResult.Compatible();
    }

    private DependencyCheckResult CheckDependencies(PluginManifest manifest)
    {
        foreach (var dep in manifest.Dependencies.Where(d => !d.Optional))
        {
            if (!_registry.IsLoaded(dep.Id))
            {
                return DependencyCheckResult.Failed($"Required dependency '{dep.Id}' is not loaded");
            }

            if (dep.MinVersion != null)
            {
                var loadedPlugin = _registry.GetPlugin(dep.Id);
                if (loadedPlugin != null &&
                    Version.TryParse(dep.MinVersion, out var minVersion) &&
                    Version.TryParse(loadedPlugin.Manifest.Version, out var loadedVersion))
                {
                    if (loadedVersion < minVersion)
                    {
                        return DependencyCheckResult.Failed(
                            $"Dependency '{dep.Id}' version {loadedVersion} is lower than required {minVersion}");
                    }
                }
            }
        }

        return DependencyCheckResult.Succeeded();
    }

    private static void CopyDirectory(string sourceDir, string targetDir)
    {
        Directory.CreateDirectory(targetDir);

        foreach (var file in Directory.GetFiles(sourceDir))
        {
            var targetFile = Path.Combine(targetDir, Path.GetFileName(file));
            File.Copy(file, targetFile, overwrite: true);
        }

        foreach (var subDir in Directory.GetDirectories(sourceDir))
        {
            var targetSubDir = Path.Combine(targetDir, Path.GetFileName(subDir));
            CopyDirectory(subDir, targetSubDir);
        }
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var plugin in _registry.GetAllPlugins())
        {
            await UnloadPluginAsync(plugin.Manifest.Id);
        }
    }

    private record CompatibilityCheckResult(bool IsCompatible, string? Error)
    {
        public static CompatibilityCheckResult Compatible() => new(true, null);
        public static CompatibilityCheckResult Incompatible(string error) => new(false, error);
    }

    private record DependencyCheckResult(bool Success, string? Error)
    {
        public static DependencyCheckResult Succeeded() => new(true, null);
        public static DependencyCheckResult Failed(string error) => new(false, error);
    }
}
