using System.Text.Json.Serialization;

namespace CamelotCombatReporter.Core.Updates.Models;

/// <summary>
/// Represents information about an available update.
/// </summary>
public record UpdateInfo
{
    /// <summary>
    /// Gets or sets the version string (e.g., "1.6.0").
    /// </summary>
    [JsonPropertyName("version")]
    public required string Version { get; init; }

    /// <summary>
    /// Gets or sets the release date.
    /// </summary>
    [JsonPropertyName("releaseDate")]
    public required string ReleaseDate { get; init; }

    /// <summary>
    /// Gets or sets the URL to the release notes.
    /// </summary>
    [JsonPropertyName("releaseNotesUrl")]
    public required string ReleaseNotesUrl { get; init; }

    /// <summary>
    /// Gets or sets the download URLs by platform.
    /// Keys: win-x64-msi, win-x64-zip, osx-universal, linux-x64-appimage, linux-x64-deb, linux-x64-rpm
    /// </summary>
    [JsonPropertyName("downloads")]
    public required IReadOnlyDictionary<string, string> Downloads { get; init; }

    /// <summary>
    /// Gets or sets the SHA256 checksums by platform.
    /// </summary>
    [JsonPropertyName("checksums")]
    public required IReadOnlyDictionary<string, string> Checksums { get; init; }

    /// <summary>
    /// Gets or sets whether this update is required (security fix, etc.).
    /// </summary>
    [JsonPropertyName("isRequired")]
    public bool IsRequired { get; init; }

    /// <summary>
    /// Gets or sets the minimum version that can be upgraded from.
    /// Null means any version can upgrade.
    /// </summary>
    [JsonPropertyName("minimumVersion")]
    public string? MinimumVersion { get; init; }

    /// <summary>
    /// Gets the parsed version for comparison.
    /// </summary>
    [JsonIgnore]
    public Version ParsedVersion => System.Version.TryParse(Version, out var v) ? v : new Version(0, 0, 0);

    /// <summary>
    /// Gets the parsed release date.
    /// </summary>
    [JsonIgnore]
    public DateOnly? ParsedReleaseDate =>
        DateOnly.TryParse(ReleaseDate, out var date) ? date : null;

    /// <summary>
    /// Gets the download URL for the current platform.
    /// </summary>
    /// <param name="preferredFormat">Preferred package format (msi, zip, dmg, appimage, deb, rpm).</param>
    public string? GetDownloadUrlForCurrentPlatform(string? preferredFormat = null)
    {
        var platformKey = GetPlatformKey(preferredFormat);
        return platformKey != null && Downloads.TryGetValue(platformKey, out var url) ? url : null;
    }

    /// <summary>
    /// Gets the checksum for the current platform.
    /// </summary>
    /// <param name="preferredFormat">Preferred package format.</param>
    public string? GetChecksumForCurrentPlatform(string? preferredFormat = null)
    {
        var platformKey = GetPlatformKey(preferredFormat);
        return platformKey != null && Checksums.TryGetValue(platformKey, out var checksum) ? checksum : null;
    }

    private static string? GetPlatformKey(string? preferredFormat)
    {
        if (OperatingSystem.IsWindows())
        {
            return preferredFormat?.ToLowerInvariant() switch
            {
                "zip" => "win-x64-zip",
                "msi" or null => "win-x64-msi",
                _ => "win-x64-msi"
            };
        }

        if (OperatingSystem.IsMacOS())
        {
            return "osx-universal";
        }

        if (OperatingSystem.IsLinux())
        {
            return preferredFormat?.ToLowerInvariant() switch
            {
                "deb" => "linux-x64-deb",
                "rpm" => "linux-x64-rpm",
                "appimage" or null => "linux-x64-appimage",
                _ => "linux-x64-appimage"
            };
        }

        return null;
    }
}
