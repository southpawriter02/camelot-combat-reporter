using CamelotCombatReporter.Core.Updates.Models;

namespace CamelotCombatReporter.Core.Updates;

/// <summary>
/// Service for checking, downloading, and installing application updates.
/// </summary>
public interface IUpdateService
{
    /// <summary>
    /// Gets the current application version.
    /// </summary>
    Version CurrentVersion { get; }

    /// <summary>
    /// Gets or sets the preferred update channel.
    /// </summary>
    UpdateChannel Channel { get; set; }

    /// <summary>
    /// Gets or sets whether automatic update checks are enabled.
    /// </summary>
    bool AutoCheckEnabled { get; set; }

    /// <summary>
    /// Checks for available updates.
    /// </summary>
    /// <param name="channel">Optional channel to check (defaults to current channel setting).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The update check result.</returns>
    Task<UpdateCheckResult> CheckForUpdatesAsync(
        UpdateChannel? channel = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Downloads the update installer.
    /// </summary>
    /// <param name="updateInfo">Information about the update to download.</param>
    /// <param name="progress">Progress reporter.</param>
    /// <param name="preferredFormat">Preferred package format (msi, zip, appimage, deb, rpm).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The path to the downloaded installer.</returns>
    Task<string> DownloadUpdateAsync(
        UpdateInfo updateInfo,
        IProgress<DownloadProgress>? progress = null,
        string? preferredFormat = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifies the integrity of a downloaded file.
    /// </summary>
    /// <param name="filePath">Path to the downloaded file.</param>
    /// <param name="expectedChecksum">Expected SHA256 checksum (format: "sha256:...").</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the file matches the expected checksum.</returns>
    Task<bool> VerifyDownloadAsync(
        string filePath,
        string expectedChecksum,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Installs the downloaded update.
    /// </summary>
    /// <param name="installerPath">Path to the installer.</param>
    /// <param name="restartAfter">Whether to restart the application after installation.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task InstallUpdateAsync(
        string installerPath,
        bool restartAfter = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a backup of the current installation for rollback.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if backup was created successfully.</returns>
    Task<bool> CreateBackupAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Rolls back to the previous version using the backup.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if rollback was successful.</returns>
    Task<bool> RollbackAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets whether a rollback is available.
    /// </summary>
    bool CanRollback { get; }

    /// <summary>
    /// Gets the version that would be restored by rollback, if available.
    /// </summary>
    Version? RollbackVersion { get; }

    /// <summary>
    /// Event raised when an update check completes.
    /// </summary>
    event EventHandler<UpdateCheckResult>? UpdateCheckCompleted;
}
