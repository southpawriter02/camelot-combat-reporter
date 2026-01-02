using System.Reflection;
using System.Runtime.Loader;
using CamelotCombatReporter.Plugins.Manifest;

namespace CamelotCombatReporter.Plugins.Loading;

/// <summary>
/// Custom AssemblyLoadContext for plugin isolation.
/// Provides assembly isolation and blocks dangerous assemblies.
/// </summary>
public sealed class PluginLoadContext : AssemblyLoadContext
{
    private readonly AssemblyDependencyResolver _resolver;
    private readonly HashSet<string> _sharedAssemblies;
    private readonly string _pluginId;

    /// <summary>
    /// Assemblies that are blocked from being loaded by plugins.
    /// </summary>
    private static readonly HashSet<string> BlockedAssemblies = new(StringComparer.OrdinalIgnoreCase)
    {
        "System.Reflection.Emit",
        "System.Reflection.Emit.Lightweight",
        "System.Reflection.Emit.ILGeneration",
        "System.Runtime.Loader",
        "System.Diagnostics.Process",
        "Microsoft.CSharp", // Prevents dynamic compilation
        "System.CodeDom",
        "System.Runtime.CompilerServices.Unsafe" // Could be used for unsafe operations
    };

    /// <summary>
    /// Assemblies that are shared with the host application.
    /// </summary>
    private static readonly HashSet<string> DefaultSharedAssemblies = new(StringComparer.OrdinalIgnoreCase)
    {
        // Core shared assemblies
        "CamelotCombatReporter.Core",
        "CamelotCombatReporter.Plugins",

        // System assemblies
        "System.Runtime",
        "System.Collections",
        "System.Linq",
        "System.Text.Json",
        "System.Threading",
        "System.Threading.Tasks",
        "System.IO",
        "System.Net.Http",
        "System.ComponentModel",
        "System.ObjectModel",
        "netstandard",
        "System.Private.CoreLib",

        // Avalonia (for UI plugins)
        "Avalonia",
        "Avalonia.Base",
        "Avalonia.Controls",
        "Avalonia.Desktop",
        "Avalonia.Dialogs",
        "Avalonia.Input",
        "Avalonia.Interactivity",
        "Avalonia.Layout",
        "Avalonia.Logging",
        "Avalonia.Media",
        "Avalonia.Remote.Protocol",
        "Avalonia.Styling",
        "Avalonia.Themes.Fluent",
        "Avalonia.Visuals",

        // MVVM
        "CommunityToolkit.Mvvm",

        // LiveCharts
        "LiveChartsCore",
        "LiveChartsCore.SkiaSharpView",
        "LiveChartsCore.SkiaSharpView.Avalonia"
    };

    /// <summary>
    /// Creates a new plugin load context.
    /// </summary>
    /// <param name="pluginPath">Path to the plugin assembly.</param>
    /// <param name="pluginId">Unique plugin identifier for logging.</param>
    /// <param name="additionalSharedAssemblies">Additional assemblies to share with host.</param>
    public PluginLoadContext(
        string pluginPath,
        string pluginId,
        IEnumerable<string>? additionalSharedAssemblies = null)
        : base(name: $"Plugin_{pluginId}", isCollectible: true)
    {
        _resolver = new AssemblyDependencyResolver(pluginPath);
        _pluginId = pluginId;
        _sharedAssemblies = new HashSet<string>(DefaultSharedAssemblies, StringComparer.OrdinalIgnoreCase);

        if (additionalSharedAssemblies != null)
        {
            foreach (var assembly in additionalSharedAssemblies)
            {
                _sharedAssemblies.Add(assembly);
            }
        }
    }

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        var name = assemblyName.Name;
        if (name == null)
        {
            return null;
        }

        // Block dangerous assemblies
        if (IsBlockedAssembly(name))
        {
            throw new PluginSecurityException(
                _pluginId,
                $"Plugin '{_pluginId}' attempted to load blocked assembly: {name}");
        }

        // Share assemblies with host to avoid version conflicts
        if (_sharedAssemblies.Contains(name) || IsSystemAssembly(name))
        {
            return null; // Fall back to default context
        }

        // Resolve plugin-specific assemblies
        var assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
        if (assemblyPath != null)
        {
            return LoadFromAssemblyPath(assemblyPath);
        }

        return null;
    }

    protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
    {
        // Block all native DLL loading by plugins for security
        throw new PluginSecurityException(
            _pluginId,
            $"Plugin '{_pluginId}' attempted to load native library: {unmanagedDllName}. " +
            "Native library loading is not permitted for plugins.");
    }

    private static bool IsBlockedAssembly(string name)
    {
        return BlockedAssemblies.Contains(name) ||
               name.StartsWith("System.Reflection.Emit", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsSystemAssembly(string name)
    {
        return name.StartsWith("System.", StringComparison.OrdinalIgnoreCase) ||
               name.StartsWith("Microsoft.", StringComparison.OrdinalIgnoreCase) ||
               name.StartsWith("netstandard", StringComparison.OrdinalIgnoreCase);
    }
}

/// <summary>
/// Exception thrown when a plugin violates security restrictions.
/// </summary>
public sealed class PluginSecurityException : Exception
{
    /// <summary>
    /// ID of the plugin that caused the violation.
    /// </summary>
    public string PluginId { get; }

    public PluginSecurityException(string pluginId, string message)
        : base(message)
    {
        PluginId = pluginId;
    }

    public PluginSecurityException(string pluginId, string message, Exception innerException)
        : base(message, innerException)
    {
        PluginId = pluginId;
    }
}

/// <summary>
/// Exception thrown when a plugin exceeds its resource limits.
/// </summary>
public sealed class PluginResourceLimitException : Exception
{
    public string PluginId { get; }
    public ResourceLimitType LimitType { get; }
    public long CurrentValue { get; }
    public long LimitValue { get; }

    public PluginResourceLimitException(
        string pluginId,
        ResourceLimitType limitType,
        long currentValue,
        long limitValue)
        : base($"Plugin '{pluginId}' exceeded {limitType} limit: {currentValue} > {limitValue}")
    {
        PluginId = pluginId;
        LimitType = limitType;
        CurrentValue = currentValue;
        LimitValue = limitValue;
    }
}

/// <summary>
/// Types of resource limits.
/// </summary>
public enum ResourceLimitType
{
    Memory,
    CpuTime,
    FileHandles,
    NetworkConnections
}
