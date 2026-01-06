using System.Net.Http.Json;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using CamelotCombatReporter.Core.Updates.Models;

namespace CamelotCombatReporter.Core.Updates;

/// <summary>
/// Implementation of the update service for checking, downloading, and installing updates.
/// </summary>
public class UpdateService : IUpdateService
{
    private const string DefaultReleaseFeedUrl = "https://raw.githubusercontent.com/southpawriter02/camelot-combat-reporter/main/releases/latest.json";
    private const string BetaReleaseFeedUrl = "https://raw.githubusercontent.com/southpawriter02/camelot-combat-reporter/main/releases/beta.json";
    private const string DevReleaseFeedUrl = "https://raw.githubusercontent.com/southpawriter02/camelot-combat-reporter/main/releases/dev.json";

    private readonly HttpClient _httpClient;
    private readonly string _downloadDirectory;
    private readonly string _backupDirectory;
    private readonly string _installDirectory;

    /// <inheritdoc />
    public Version CurrentVersion { get; }

    /// <inheritdoc />
    public UpdateChannel Channel { get; set; } = UpdateChannel.Stable;

    /// <inheritdoc />
    public bool AutoCheckEnabled { get; set; } = true;

    /// <inheritdoc />
    public bool CanRollback => Directory.Exists(_backupDirectory) &&
                                Directory.GetFiles(_backupDirectory).Length > 0;

    /// <inheritdoc />
    public Version? RollbackVersion { get; private set; }

    /// <inheritdoc />
    public event EventHandler<UpdateCheckResult>? UpdateCheckCompleted;

    /// <summary>
    /// Creates a new instance of the update service.
    /// </summary>
    /// <param name="httpClient">Optional HTTP client for testing.</param>
    public UpdateService(HttpClient? httpClient = null)
    {
        _httpClient = httpClient ?? new HttpClient();
        _httpClient.Timeout = TimeSpan.FromMinutes(5);

        // Get current version from assembly
        var assembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
        var version = assembly.GetName().Version ?? new Version(1, 0, 0);
        CurrentVersion = version;

        // Set up directories
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var appFolder = Path.Combine(appDataPath, "CamelotCombatReporter");
        _downloadDirectory = Path.Combine(appFolder, "Downloads");
        _backupDirectory = Path.Combine(appFolder, "Backup");
        _installDirectory = AppContext.BaseDirectory;

        // Ensure directories exist
        Directory.CreateDirectory(_downloadDirectory);

        // Load rollback version if backup exists
        LoadRollbackVersion();
    }

    /// <inheritdoc />
    public async Task<UpdateCheckResult> CheckForUpdatesAsync(
        UpdateChannel? channel = null,
        CancellationToken cancellationToken = default)
    {
        var targetChannel = channel ?? Channel;

        try
        {
            var feedUrl = GetReleaseFeedUrl(targetChannel);
            var updateInfo = await _httpClient.GetFromJsonAsync<UpdateInfo>(feedUrl, cancellationToken);

            if (updateInfo == null)
            {
                var result = UpdateCheckResult.Error(CurrentVersion, "Failed to parse update information");
                UpdateCheckCompleted?.Invoke(this, result);
                return result;
            }

            // Compare versions
            var isNewer = updateInfo.ParsedVersion > CurrentVersion;

            // Check minimum version requirement
            if (isNewer && updateInfo.MinimumVersion != null)
            {
                if (Version.TryParse(updateInfo.MinimumVersion, out var minVersion) && CurrentVersion < minVersion)
                {
                    var result = UpdateCheckResult.Error(CurrentVersion,
                        $"Your version is too old to upgrade directly. Please download version {updateInfo.MinimumVersion} first.");
                    UpdateCheckCompleted?.Invoke(this, result);
                    return result;
                }
            }

            var checkResult = isNewer
                ? UpdateCheckResult.UpdateAvailable(CurrentVersion, updateInfo)
                : UpdateCheckResult.NoUpdateAvailable(CurrentVersion);

            UpdateCheckCompleted?.Invoke(this, checkResult);
            return checkResult;
        }
        catch (HttpRequestException ex)
        {
            var result = UpdateCheckResult.Error(CurrentVersion, $"Network error: {ex.Message}");
            UpdateCheckCompleted?.Invoke(this, result);
            return result;
        }
        catch (JsonException ex)
        {
            var result = UpdateCheckResult.Error(CurrentVersion, $"Invalid update data: {ex.Message}");
            UpdateCheckCompleted?.Invoke(this, result);
            return result;
        }
        catch (Exception ex)
        {
            var result = UpdateCheckResult.Error(CurrentVersion, $"Update check failed: {ex.Message}");
            UpdateCheckCompleted?.Invoke(this, result);
            return result;
        }
    }

    /// <inheritdoc />
    public async Task<string> DownloadUpdateAsync(
        UpdateInfo updateInfo,
        IProgress<DownloadProgress>? progress = null,
        string? preferredFormat = null,
        CancellationToken cancellationToken = default)
    {
        var downloadUrl = updateInfo.GetDownloadUrlForCurrentPlatform(preferredFormat)
            ?? throw new InvalidOperationException("No download available for current platform");

        var fileName = Path.GetFileName(new Uri(downloadUrl).LocalPath);
        var filePath = Path.Combine(_downloadDirectory, fileName);

        // Delete existing file if present
        if (File.Exists(filePath))
            File.Delete(filePath);

        using var response = await _httpClient.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        var totalBytes = response.Content.Headers.ContentLength;
        var startTime = DateTime.UtcNow;
        var lastProgressTime = startTime;
        var lastBytes = 0L;

        await using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        await using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);

        var buffer = new byte[8192];
        var bytesDownloaded = 0L;
        int bytesRead;

        while ((bytesRead = await contentStream.ReadAsync(buffer, cancellationToken)) > 0)
        {
            await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
            bytesDownloaded += bytesRead;

            // Calculate speed and report progress
            var now = DateTime.UtcNow;
            var elapsed = (now - lastProgressTime).TotalSeconds;

            if (elapsed >= 0.1) // Update at most 10 times per second
            {
                var bytesPerSecond = (bytesDownloaded - lastBytes) / elapsed;
                double? percentage = totalBytes.HasValue ? (double)bytesDownloaded / totalBytes.Value * 100 : null;

                progress?.Report(new DownloadProgress(bytesDownloaded, totalBytes, percentage, bytesPerSecond));

                lastProgressTime = now;
                lastBytes = bytesDownloaded;
            }
        }

        // Final progress report
        var totalElapsed = (DateTime.UtcNow - startTime).TotalSeconds;
        var averageSpeed = totalElapsed > 0 ? bytesDownloaded / totalElapsed : 0;
        progress?.Report(new DownloadProgress(bytesDownloaded, totalBytes, 100, averageSpeed));

        return filePath;
    }

    /// <inheritdoc />
    public async Task<bool> VerifyDownloadAsync(
        string filePath,
        string expectedChecksum,
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(filePath))
            return false;

        // Parse expected checksum (format: "sha256:abc123...")
        var parts = expectedChecksum.Split(':', 2);
        if (parts.Length != 2 || !parts[0].Equals("sha256", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Invalid checksum format. Expected 'sha256:...'", nameof(expectedChecksum));

        var expectedHash = parts[1].ToLowerInvariant();

        await using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 8192, true);
        var hashBytes = await SHA256.HashDataAsync(fileStream, cancellationToken);
        var actualHash = Convert.ToHexString(hashBytes).ToLowerInvariant();

        return actualHash == expectedHash;
    }

    /// <inheritdoc />
    public async Task InstallUpdateAsync(
        string installerPath,
        bool restartAfter = true,
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(installerPath))
            throw new FileNotFoundException("Installer not found", installerPath);

        // Create backup before installation
        await CreateBackupAsync(cancellationToken);

        var extension = Path.GetExtension(installerPath).ToLowerInvariant();

        if (OperatingSystem.IsWindows())
        {
            await InstallWindowsAsync(installerPath, restartAfter, cancellationToken);
        }
        else if (OperatingSystem.IsMacOS())
        {
            await InstallMacOSAsync(installerPath, restartAfter, cancellationToken);
        }
        else if (OperatingSystem.IsLinux())
        {
            await InstallLinuxAsync(installerPath, extension, restartAfter, cancellationToken);
        }
        else
        {
            throw new PlatformNotSupportedException("Update installation not supported on this platform");
        }
    }

    /// <inheritdoc />
    public async Task<bool> CreateBackupAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Clean previous backup
            if (Directory.Exists(_backupDirectory))
                Directory.Delete(_backupDirectory, true);

            Directory.CreateDirectory(_backupDirectory);

            // Save version info
            var versionFile = Path.Combine(_backupDirectory, "version.txt");
            await File.WriteAllTextAsync(versionFile, CurrentVersion.ToString(), cancellationToken);

            // Copy critical files for rollback
            var filesToBackup = new[] { "*.exe", "*.dll", "*.json", "*.config" };
            foreach (var pattern in filesToBackup)
            {
                foreach (var file in Directory.GetFiles(_installDirectory, pattern))
                {
                    var destFile = Path.Combine(_backupDirectory, Path.GetFileName(file));
                    File.Copy(file, destFile, true);
                }
            }

            LoadRollbackVersion();
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <inheritdoc />
    public Task<bool> RollbackAsync(CancellationToken cancellationToken = default)
    {
        if (!CanRollback)
            return Task.FromResult(false);

        try
        {
            // Copy backup files back to install directory
            foreach (var file in Directory.GetFiles(_backupDirectory))
            {
                var fileName = Path.GetFileName(file);
                if (fileName == "version.txt")
                    continue;

                var destFile = Path.Combine(_installDirectory, fileName);
                File.Copy(file, destFile, true);
            }

            // Delete backup after successful rollback
            Directory.Delete(_backupDirectory, true);
            RollbackVersion = null;

            return Task.FromResult(true);
        }
        catch
        {
            return Task.FromResult(false);
        }
    }

    private string GetReleaseFeedUrl(UpdateChannel channel) => channel switch
    {
        UpdateChannel.Beta => BetaReleaseFeedUrl,
        UpdateChannel.Dev => DevReleaseFeedUrl,
        _ => DefaultReleaseFeedUrl
    };

    private void LoadRollbackVersion()
    {
        var versionFile = Path.Combine(_backupDirectory, "version.txt");
        if (File.Exists(versionFile))
        {
            var versionText = File.ReadAllText(versionFile).Trim();
            if (Version.TryParse(versionText, out var version))
            {
                RollbackVersion = version;
            }
        }
    }

    private async Task InstallWindowsAsync(string installerPath, bool restartAfter, CancellationToken cancellationToken)
    {
        var extension = Path.GetExtension(installerPath).ToLowerInvariant();

        if (extension == ".msi")
        {
            // Install MSI silently
            var args = $"/i \"{installerPath}\" /qn /norestart";
            await RunProcessAsync("msiexec", args, cancellationToken);
        }
        else if (extension == ".zip")
        {
            // Extract ZIP to install directory
            System.IO.Compression.ZipFile.ExtractToDirectory(installerPath, _installDirectory, true);
        }

        if (restartAfter)
        {
            RestartApplication();
        }
    }

    private async Task InstallMacOSAsync(string installerPath, bool restartAfter, CancellationToken cancellationToken)
    {
        var extension = Path.GetExtension(installerPath).ToLowerInvariant();

        if (extension == ".dmg")
        {
            // Mount DMG
            var mountPoint = "/Volumes/CamelotCombatReporter";
            await RunProcessAsync("hdiutil", $"attach \"{installerPath}\" -nobrowse -mountpoint \"{mountPoint}\"", cancellationToken);

            try
            {
                // Copy app to Applications
                var appPath = Path.Combine(mountPoint, "Camelot Combat Reporter.app");
                var destPath = "/Applications/Camelot Combat Reporter.app";

                if (Directory.Exists(destPath))
                    Directory.Delete(destPath, true);

                await RunProcessAsync("cp", $"-R \"{appPath}\" \"{destPath}\"", cancellationToken);
            }
            finally
            {
                // Unmount DMG
                await RunProcessAsync("hdiutil", $"detach \"{mountPoint}\"", cancellationToken);
            }
        }

        if (restartAfter)
        {
            RestartApplication();
        }
    }

    private async Task InstallLinuxAsync(string installerPath, string extension, bool restartAfter, CancellationToken cancellationToken)
    {
        switch (extension)
        {
            case ".appimage":
                // Replace current AppImage
                var currentExe = Environment.ProcessPath;
                if (currentExe != null && currentExe.EndsWith(".AppImage", StringComparison.OrdinalIgnoreCase))
                {
                    File.Copy(installerPath, currentExe, true);
                    // Make executable
                    await RunProcessAsync("chmod", $"+x \"{currentExe}\"", cancellationToken);
                }
                break;

            case ".deb":
                // Install with pkexec (graphical sudo)
                await RunProcessAsync("pkexec", $"dpkg -i \"{installerPath}\"", cancellationToken);
                break;

            case ".rpm":
                // Install with pkexec
                await RunProcessAsync("pkexec", $"rpm -U \"{installerPath}\"", cancellationToken);
                break;
        }

        if (restartAfter)
        {
            RestartApplication();
        }
    }

    private static async Task RunProcessAsync(string fileName, string arguments, CancellationToken cancellationToken)
    {
        using var process = new System.Diagnostics.Process
        {
            StartInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            }
        };

        process.Start();
        await process.WaitForExitAsync(cancellationToken);

        if (process.ExitCode != 0)
        {
            var error = await process.StandardError.ReadToEndAsync(cancellationToken);
            throw new InvalidOperationException($"Process '{fileName}' failed with exit code {process.ExitCode}: {error}");
        }
    }

    private static void RestartApplication()
    {
        var exePath = Environment.ProcessPath;
        if (exePath != null)
        {
            System.Diagnostics.Process.Start(exePath);
            Environment.Exit(0);
        }
    }
}
