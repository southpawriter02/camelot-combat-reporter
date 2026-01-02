using CamelotCombatReporter.Plugins.Abstractions;
using CamelotCombatReporter.Plugins.Manifest;
using CamelotCombatReporter.Plugins.Permissions;
using CamelotCombatReporter.Plugins.Security;

namespace CamelotCombatReporter.Plugins.Sandbox;

/// <summary>
/// Provides a sandboxed execution context for plugins.
/// All services are accessed through permission-enforcing proxies.
/// </summary>
public sealed class SandboxedPluginContext : IPluginContext
{
    private readonly string _pluginId;
    private readonly string _pluginDataDirectory;
    private readonly HashSet<PluginPermission> _grantedPermissions;
    private readonly ResourceLimitsConfig _resourceLimits;
    private readonly ISecurityAuditLogger _auditLogger;
    private readonly PluginLogger _pluginLogger;
    private readonly Dictionary<Type, object> _services = new();

    public string PluginDataDirectory => _pluginDataDirectory;
    public IPluginLogger Logger => _pluginLogger;
    public IReadOnlyCollection<PluginPermission> GrantedPermissions => _grantedPermissions;
    public Version ApplicationVersion { get; }

    public SandboxedPluginContext(
        string pluginId,
        string pluginDirectory,
        IEnumerable<PluginPermission> grantedPermissions,
        ResourceLimitsConfig resourceLimits,
        ISecurityAuditLogger auditLogger,
        Version applicationVersion)
    {
        _pluginId = pluginId;
        _pluginDataDirectory = Path.Combine(pluginDirectory, "data");
        _grantedPermissions = new HashSet<PluginPermission>(grantedPermissions);
        _resourceLimits = resourceLimits;
        _auditLogger = auditLogger;
        _pluginLogger = new PluginLogger(pluginId, auditLogger);
        ApplicationVersion = applicationVersion;

        // Ensure data directory exists
        Directory.CreateDirectory(_pluginDataDirectory);

        // Initialize available services based on permissions
        InitializeServices();
    }

    public bool HasPermission(PluginPermission permission)
    {
        return _grantedPermissions.Contains(permission) ||
               _grantedPermissions.Any(p => (p & permission) == permission);
    }

    public T? GetService<T>() where T : class
    {
        if (_services.TryGetValue(typeof(T), out var service))
        {
            return service as T;
        }
        return null;
    }

    /// <summary>
    /// Registers a service for plugins to access.
    /// </summary>
    internal void RegisterService<T>(T service) where T : class
    {
        _services[typeof(T)] = service;
    }

    private void InitializeServices()
    {
        // File system access
        if (HasPermission(PluginPermission.FileRead) || HasPermission(PluginPermission.FileWrite))
        {
            var fileSystemProxy = new FileSystemProxy(
                _pluginId,
                _pluginDataDirectory,
                HasPermission(PluginPermission.FileRead),
                HasPermission(PluginPermission.FileWrite),
                _auditLogger);
            _services[typeof(IFileSystemAccess)] = fileSystemProxy;
        }

        // Network access
        if (HasPermission(PluginPermission.NetworkAccess))
        {
            var networkProxy = new NetworkProxy(_pluginId, _auditLogger);
            _services[typeof(INetworkAccess)] = networkProxy;
        }
    }
}

/// <summary>
/// Logger implementation for plugins.
/// </summary>
public sealed class PluginLogger : IPluginLogger
{
    private readonly string _pluginId;
    private readonly ISecurityAuditLogger _auditLogger;

    public PluginLogger(string pluginId, ISecurityAuditLogger auditLogger)
    {
        _pluginId = pluginId;
        _auditLogger = auditLogger;
    }

    public void Debug(string message)
    {
        _auditLogger.LogPluginMessage(_pluginId, LogLevel.Debug, message);
    }

    public void Info(string message)
    {
        _auditLogger.LogPluginMessage(_pluginId, LogLevel.Info, message);
    }

    public void Warning(string message)
    {
        _auditLogger.LogPluginMessage(_pluginId, LogLevel.Warning, message);
    }

    public void Error(string message, Exception? exception = null)
    {
        _auditLogger.LogPluginMessage(_pluginId, LogLevel.Error, message, exception);
    }
}

/// <summary>
/// Log levels for plugin logging.
/// </summary>
public enum LogLevel
{
    Debug,
    Info,
    Warning,
    Error
}
