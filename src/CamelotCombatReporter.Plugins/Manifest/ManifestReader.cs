using System.Text.Json;
using System.Text.RegularExpressions;

namespace CamelotCombatReporter.Plugins.Manifest;

/// <summary>
/// Reads and validates plugin manifests.
/// </summary>
public sealed partial class ManifestReader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    [GeneratedRegex(@"^[a-z][a-z0-9-]*[a-z0-9]$")]
    private static partial Regex PluginIdPattern();

    [GeneratedRegex(@"^\d+\.\d+\.\d+(-[a-zA-Z0-9]+)?$")]
    private static partial Regex VersionPattern();

    /// <summary>
    /// Reads a manifest from a file path.
    /// </summary>
    public async Task<ManifestReadResult> ReadFromFileAsync(string filePath, CancellationToken ct = default)
    {
        if (!File.Exists(filePath))
        {
            return ManifestReadResult.Failed($"Manifest file not found: {filePath}");
        }

        try
        {
            var json = await File.ReadAllTextAsync(filePath, ct);
            return ReadFromJson(json);
        }
        catch (Exception ex)
        {
            return ManifestReadResult.Failed($"Failed to read manifest file: {ex.Message}");
        }
    }

    /// <summary>
    /// Reads a manifest from JSON string.
    /// </summary>
    public ManifestReadResult ReadFromJson(string json)
    {
        try
        {
            var manifest = JsonSerializer.Deserialize<PluginManifest>(json, JsonOptions);
            if (manifest == null)
            {
                return ManifestReadResult.Failed("Failed to deserialize manifest: result was null");
            }

            var validationResult = Validate(manifest);
            if (!validationResult.IsValid)
            {
                return ManifestReadResult.Failed($"Invalid manifest: {string.Join(", ", validationResult.Errors)}");
            }

            return ManifestReadResult.Success(manifest);
        }
        catch (JsonException ex)
        {
            return ManifestReadResult.Failed($"Invalid JSON: {ex.Message}");
        }
    }

    /// <summary>
    /// Validates a manifest.
    /// </summary>
    public ManifestValidationResult Validate(PluginManifest manifest)
    {
        var errors = new List<string>();

        // Validate ID
        if (string.IsNullOrWhiteSpace(manifest.Id))
        {
            errors.Add("Plugin ID is required");
        }
        else if (manifest.Id.Length < 3 || manifest.Id.Length > 64)
        {
            errors.Add("Plugin ID must be between 3 and 64 characters");
        }
        else if (!PluginIdPattern().IsMatch(manifest.Id))
        {
            errors.Add("Plugin ID must be lowercase alphanumeric with hyphens (e.g., 'my-plugin')");
        }

        // Validate name
        if (string.IsNullOrWhiteSpace(manifest.Name))
        {
            errors.Add("Plugin name is required");
        }
        else if (manifest.Name.Length > 128)
        {
            errors.Add("Plugin name must be 128 characters or less");
        }

        // Validate version
        if (string.IsNullOrWhiteSpace(manifest.Version))
        {
            errors.Add("Plugin version is required");
        }
        else if (!VersionPattern().IsMatch(manifest.Version))
        {
            errors.Add("Plugin version must follow semantic versioning (e.g., '1.0.0' or '2.1.0-beta')");
        }

        // Validate entry point
        if (string.IsNullOrWhiteSpace(manifest.EntryPoint.Assembly))
        {
            errors.Add("Entry point assembly is required");
        }
        else if (!manifest.EntryPoint.Assembly.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
        {
            errors.Add("Entry point assembly must be a .dll file");
        }

        if (string.IsNullOrWhiteSpace(manifest.EntryPoint.TypeName))
        {
            errors.Add("Entry point type name is required");
        }

        // Validate compatibility versions if specified
        if (manifest.Compatibility.MinAppVersion != null &&
            !VersionPattern().IsMatch(manifest.Compatibility.MinAppVersion))
        {
            errors.Add("Invalid minAppVersion format");
        }

        if (manifest.Compatibility.MaxAppVersion != null &&
            !VersionPattern().IsMatch(manifest.Compatibility.MaxAppVersion))
        {
            errors.Add("Invalid maxAppVersion format");
        }

        // Validate resource limits
        if (manifest.Resources.MaxMemoryMb < 16 || manifest.Resources.MaxMemoryMb > 512)
        {
            errors.Add("maxMemoryMb must be between 16 and 512");
        }

        if (manifest.Resources.MaxCpuTimeSeconds < 1 || manifest.Resources.MaxCpuTimeSeconds > 300)
        {
            errors.Add("maxCpuTimeSeconds must be between 1 and 300");
        }

        // Validate dependencies
        foreach (var dep in manifest.Dependencies)
        {
            if (string.IsNullOrWhiteSpace(dep.Id))
            {
                errors.Add("Dependency ID is required");
            }
            else if (!PluginIdPattern().IsMatch(dep.Id))
            {
                errors.Add($"Invalid dependency ID format: {dep.Id}");
            }

            if (dep.MinVersion != null && !VersionPattern().IsMatch(dep.MinVersion))
            {
                errors.Add($"Invalid dependency minVersion for {dep.Id}");
            }
        }

        return new ManifestValidationResult(errors.Count == 0, errors);
    }
}

/// <summary>
/// Result of reading a manifest.
/// </summary>
public sealed record ManifestReadResult
{
    public bool IsSuccess { get; init; }
    public PluginManifest? Manifest { get; init; }
    public string? Error { get; init; }

    private ManifestReadResult() { }

    public static ManifestReadResult Success(PluginManifest manifest) =>
        new() { IsSuccess = true, Manifest = manifest };

    public static ManifestReadResult Failed(string error) =>
        new() { IsSuccess = false, Error = error };
}

/// <summary>
/// Result of manifest validation.
/// </summary>
/// <param name="IsValid">Whether the manifest is valid.</param>
/// <param name="Errors">List of validation errors.</param>
public sealed record ManifestValidationResult(bool IsValid, IReadOnlyList<string> Errors);
