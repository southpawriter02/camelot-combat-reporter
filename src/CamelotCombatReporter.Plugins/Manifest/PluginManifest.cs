using System.Text.Json.Serialization;
using CamelotCombatReporter.Plugins.Abstractions;
using CamelotCombatReporter.Plugins.Permissions;

namespace CamelotCombatReporter.Plugins.Manifest;

/// <summary>
/// Represents a plugin manifest (plugin.json).
/// </summary>
public sealed class PluginManifest
{
    /// <summary>
    /// Unique plugin identifier (lowercase, alphanumeric with hyphens).
    /// </summary>
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    /// <summary>
    /// Human-readable plugin name.
    /// </summary>
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    /// <summary>
    /// Semantic version string (e.g., "1.0.0", "2.1.0-beta").
    /// </summary>
    [JsonPropertyName("version")]
    public required string Version { get; init; }

    /// <summary>
    /// Plugin author or organization.
    /// </summary>
    [JsonPropertyName("author")]
    public string Author { get; init; } = "Unknown";

    /// <summary>
    /// Brief description of the plugin.
    /// </summary>
    [JsonPropertyName("description")]
    public string Description { get; init; } = "";

    /// <summary>
    /// Plugin website or repository URL.
    /// </summary>
    [JsonPropertyName("website")]
    public string? Website { get; init; }

    /// <summary>
    /// SPDX license identifier (e.g., MIT, Apache-2.0).
    /// </summary>
    [JsonPropertyName("license")]
    public string? License { get; init; }

    /// <summary>
    /// Primary plugin type.
    /// </summary>
    [JsonPropertyName("type")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public required PluginType Type { get; init; }

    /// <summary>
    /// Entry point configuration.
    /// </summary>
    [JsonPropertyName("entryPoint")]
    public required EntryPointConfig EntryPoint { get; init; }

    /// <summary>
    /// Compatibility requirements.
    /// </summary>
    [JsonPropertyName("compatibility")]
    public CompatibilityConfig Compatibility { get; init; } = new();

    /// <summary>
    /// Requested permissions.
    /// </summary>
    [JsonPropertyName("permissions")]
    public List<PermissionRequestConfig> Permissions { get; init; } = new();

    /// <summary>
    /// Plugin dependencies.
    /// </summary>
    [JsonPropertyName("dependencies")]
    public List<DependencyConfig> Dependencies { get; init; } = new();

    /// <summary>
    /// Resource limits.
    /// </summary>
    [JsonPropertyName("resources")]
    public ResourceLimitsConfig Resources { get; init; } = new();

    /// <summary>
    /// Code signing information.
    /// </summary>
    [JsonPropertyName("signing")]
    public SigningConfig? Signing { get; init; }

    /// <summary>
    /// Plugin-specific configuration.
    /// </summary>
    [JsonPropertyName("configuration")]
    [JsonExtensionData]
    public Dictionary<string, object>? Configuration { get; init; }
}

/// <summary>
/// Entry point configuration specifying the assembly and type to load.
/// </summary>
public sealed class EntryPointConfig
{
    /// <summary>
    /// Assembly file name (e.g., "MyPlugin.dll").
    /// </summary>
    [JsonPropertyName("assembly")]
    public required string Assembly { get; init; }

    /// <summary>
    /// Fully qualified type name implementing IPlugin.
    /// </summary>
    [JsonPropertyName("typeName")]
    public required string TypeName { get; init; }
}

/// <summary>
/// Compatibility requirements configuration.
/// </summary>
public sealed class CompatibilityConfig
{
    /// <summary>
    /// Minimum required application version.
    /// </summary>
    [JsonPropertyName("minAppVersion")]
    public string? MinAppVersion { get; init; }

    /// <summary>
    /// Maximum supported application version.
    /// </summary>
    [JsonPropertyName("maxAppVersion")]
    public string? MaxAppVersion { get; init; }

    /// <summary>
    /// Target .NET framework.
    /// </summary>
    [JsonPropertyName("targetFramework")]
    public string TargetFramework { get; init; } = "net9.0";
}

/// <summary>
/// Permission request configuration.
/// </summary>
public sealed class PermissionRequestConfig
{
    /// <summary>
    /// Permission type being requested.
    /// </summary>
    [JsonPropertyName("type")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public required PluginPermission Type { get; init; }

    /// <summary>
    /// Optional scope restriction (e.g., file paths, domains).
    /// </summary>
    [JsonPropertyName("scope")]
    public string? Scope { get; init; }

    /// <summary>
    /// User-facing explanation for why permission is needed.
    /// </summary>
    [JsonPropertyName("reason")]
    public string Reason { get; init; } = "";
}

/// <summary>
/// Plugin dependency configuration.
/// </summary>
public sealed class DependencyConfig
{
    /// <summary>
    /// Dependency plugin ID.
    /// </summary>
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    /// <summary>
    /// Minimum required version.
    /// </summary>
    [JsonPropertyName("minVersion")]
    public string? MinVersion { get; init; }

    /// <summary>
    /// Whether this dependency is optional.
    /// </summary>
    [JsonPropertyName("optional")]
    public bool Optional { get; init; }
}

/// <summary>
/// Resource limits configuration.
/// </summary>
public sealed class ResourceLimitsConfig
{
    /// <summary>
    /// Maximum memory allocation in MB.
    /// </summary>
    [JsonPropertyName("maxMemoryMb")]
    public int MaxMemoryMb { get; init; } = 64;

    /// <summary>
    /// Maximum CPU time per operation in seconds.
    /// </summary>
    [JsonPropertyName("maxCpuTimeSeconds")]
    public int MaxCpuTimeSeconds { get; init; } = 30;

    /// <summary>
    /// Whether background tasks are allowed.
    /// </summary>
    [JsonPropertyName("allowBackgroundTasks")]
    public bool AllowBackgroundTasks { get; init; }
}

/// <summary>
/// Code signing configuration.
/// </summary>
public sealed class SigningConfig
{
    /// <summary>
    /// Certificate thumbprint for verification.
    /// </summary>
    [JsonPropertyName("thumbprint")]
    public string? Thumbprint { get; init; }

    /// <summary>
    /// Publisher common name.
    /// </summary>
    [JsonPropertyName("trustedPublisher")]
    public string? TrustedPublisher { get; init; }
}

/// <summary>
/// Trust level determined after signature verification.
/// </summary>
public enum PluginTrustLevel
{
    /// <summary>Plugin is not trusted; user must approve permissions.</summary>
    Untrusted,

    /// <summary>User has manually marked this plugin as trusted.</summary>
    UserTrusted,

    /// <summary>Plugin is signed by a known developer.</summary>
    SignedTrusted,

    /// <summary>Plugin is signed by the official key.</summary>
    OfficialTrusted
}
