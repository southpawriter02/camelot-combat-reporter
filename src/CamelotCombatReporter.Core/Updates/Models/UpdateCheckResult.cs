namespace CamelotCombatReporter.Core.Updates.Models;

/// <summary>
/// Represents the result of an update check.
/// </summary>
public record UpdateCheckResult
{
    /// <summary>
    /// Gets whether an update is available.
    /// </summary>
    public required bool IsUpdateAvailable { get; init; }

    /// <summary>
    /// Gets the current application version.
    /// </summary>
    public required Version CurrentVersion { get; init; }

    /// <summary>
    /// Gets information about the available update, if any.
    /// </summary>
    public UpdateInfo? UpdateInfo { get; init; }

    /// <summary>
    /// Gets any error message from the update check.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Gets whether the update check was successful.
    /// </summary>
    public bool WasSuccessful => ErrorMessage == null;

    /// <summary>
    /// Creates a result indicating no update is available.
    /// </summary>
    public static UpdateCheckResult NoUpdateAvailable(Version currentVersion) => new()
    {
        IsUpdateAvailable = false,
        CurrentVersion = currentVersion
    };

    /// <summary>
    /// Creates a result indicating an update is available.
    /// </summary>
    public static UpdateCheckResult UpdateAvailable(Version currentVersion, UpdateInfo updateInfo) => new()
    {
        IsUpdateAvailable = true,
        CurrentVersion = currentVersion,
        UpdateInfo = updateInfo
    };

    /// <summary>
    /// Creates a result indicating an error occurred.
    /// </summary>
    public static UpdateCheckResult Error(Version currentVersion, string errorMessage) => new()
    {
        IsUpdateAvailable = false,
        CurrentVersion = currentVersion,
        ErrorMessage = errorMessage
    };
}
