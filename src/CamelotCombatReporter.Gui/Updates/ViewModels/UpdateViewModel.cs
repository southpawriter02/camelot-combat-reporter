using System;
using System.Threading;
using System.Threading.Tasks;
using CamelotCombatReporter.Core.Updates;
using CamelotCombatReporter.Core.Updates.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CamelotCombatReporter.Gui.Updates.ViewModels;

/// <summary>
/// View model for the update dialog.
/// </summary>
public partial class UpdateViewModel : ObservableObject
{
    private readonly IUpdateService _updateService;
    private CancellationTokenSource? _downloadCancellation;

    [ObservableProperty]
    private bool _isChecking;

    [ObservableProperty]
    private bool _isDownloading;

    [ObservableProperty]
    private bool _isUpdateAvailable;

    [ObservableProperty]
    private bool _hasError;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private string _currentVersion = string.Empty;

    [ObservableProperty]
    private string _newVersion = string.Empty;

    [ObservableProperty]
    private string _releaseDate = string.Empty;

    [ObservableProperty]
    private string _releaseNotesUrl = string.Empty;

    [ObservableProperty]
    private bool _isRequired;

    [ObservableProperty]
    private double _downloadProgress;

    [ObservableProperty]
    private string _downloadStatus = string.Empty;

    [ObservableProperty]
    private string _downloadSpeed = string.Empty;

    [ObservableProperty]
    private string _downloadedSize = string.Empty;

    [ObservableProperty]
    private string _totalSize = string.Empty;

    [ObservableProperty]
    private string _timeRemaining = string.Empty;

    [ObservableProperty]
    private bool _canRollback;

    [ObservableProperty]
    private string _rollbackVersion = string.Empty;

    private UpdateInfo? _updateInfo;

    /// <summary>
    /// Event raised when the dialog should close.
    /// </summary>
    public event EventHandler<bool>? CloseRequested;

    /// <summary>
    /// Creates a new instance of the update view model.
    /// </summary>
    public UpdateViewModel() : this(new UpdateService())
    {
    }

    /// <summary>
    /// Creates a new instance of the update view model with a custom update service.
    /// </summary>
    /// <param name="updateService">The update service to use.</param>
    public UpdateViewModel(IUpdateService updateService)
    {
        _updateService = updateService;
        CurrentVersion = _updateService.CurrentVersion.ToString(3);
        CanRollback = _updateService.CanRollback;
        RollbackVersion = _updateService.RollbackVersion?.ToString(3) ?? string.Empty;

        _updateService.UpdateCheckCompleted += OnUpdateCheckCompleted;
    }

    /// <summary>
    /// Checks for updates.
    /// </summary>
    [RelayCommand]
    private async Task CheckForUpdatesAsync()
    {
        IsChecking = true;
        HasError = false;
        ErrorMessage = string.Empty;
        IsUpdateAvailable = false;

        try
        {
            var result = await _updateService.CheckForUpdatesAsync();

            if (!result.WasSuccessful)
            {
                HasError = true;
                ErrorMessage = result.ErrorMessage ?? "Unknown error occurred";
            }
            else if (result.IsUpdateAvailable && result.UpdateInfo != null)
            {
                _updateInfo = result.UpdateInfo;
                IsUpdateAvailable = true;
                NewVersion = result.UpdateInfo.Version;
                ReleaseDate = result.UpdateInfo.ParsedReleaseDate?.ToString("MMMM d, yyyy") ?? result.UpdateInfo.ReleaseDate;
                ReleaseNotesUrl = result.UpdateInfo.ReleaseNotesUrl;
                IsRequired = result.UpdateInfo.IsRequired;
            }
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsChecking = false;
        }
    }

    /// <summary>
    /// Downloads and installs the update.
    /// </summary>
    [RelayCommand]
    private async Task DownloadAndInstallAsync()
    {
        if (_updateInfo == null)
            return;

        IsDownloading = true;
        DownloadProgress = 0;
        DownloadStatus = "Preparing download...";
        _downloadCancellation = new CancellationTokenSource();

        try
        {
            var progress = new Progress<DownloadProgress>(p =>
            {
                DownloadProgress = p.Percentage ?? 0;
                DownloadSpeed = p.SpeedFormatted;
                DownloadedSize = p.BytesDownloadedFormatted;
                TotalSize = p.TotalBytesFormatted ?? "Unknown";
                TimeRemaining = p.EstimatedTimeRemainingFormatted ?? "Calculating...";
                DownloadStatus = $"Downloading... {p.Percentage:F1}%";
            });

            DownloadStatus = "Downloading update...";
            var installerPath = await _updateService.DownloadUpdateAsync(
                _updateInfo,
                progress,
                cancellationToken: _downloadCancellation.Token);

            // Verify download
            DownloadStatus = "Verifying download...";
            var checksum = _updateInfo.GetChecksumForCurrentPlatform();
            if (checksum != null)
            {
                var isValid = await _updateService.VerifyDownloadAsync(installerPath, checksum, _downloadCancellation.Token);
                if (!isValid)
                {
                    HasError = true;
                    ErrorMessage = "Download verification failed. The file may be corrupted.";
                    return;
                }
            }

            // Install update
            DownloadStatus = "Installing update...";
            await _updateService.InstallUpdateAsync(installerPath, restartAfter: true, _downloadCancellation.Token);
        }
        catch (OperationCanceledException)
        {
            DownloadStatus = "Download cancelled";
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Download failed: {ex.Message}";
        }
        finally
        {
            IsDownloading = false;
            _downloadCancellation?.Dispose();
            _downloadCancellation = null;
        }
    }

    /// <summary>
    /// Cancels the current download.
    /// </summary>
    [RelayCommand]
    private void CancelDownload()
    {
        _downloadCancellation?.Cancel();
    }

    /// <summary>
    /// Reminds the user later about the update.
    /// </summary>
    [RelayCommand]
    private void RemindLater()
    {
        CloseRequested?.Invoke(this, false);
    }

    /// <summary>
    /// Skips this version.
    /// </summary>
    [RelayCommand]
    private void SkipVersion()
    {
        // TODO: Store skipped version in settings
        CloseRequested?.Invoke(this, false);
    }

    /// <summary>
    /// Rolls back to the previous version.
    /// </summary>
    [RelayCommand]
    private async Task RollbackAsync()
    {
        if (!CanRollback)
            return;

        try
        {
            var success = await _updateService.RollbackAsync();
            if (success)
            {
                DownloadStatus = "Rollback successful. Restarting...";
                // Restart application
                var exePath = Environment.ProcessPath;
                if (exePath != null)
                {
                    System.Diagnostics.Process.Start(exePath);
                    Environment.Exit(0);
                }
            }
            else
            {
                HasError = true;
                ErrorMessage = "Rollback failed";
            }
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Rollback failed: {ex.Message}";
        }
    }

    /// <summary>
    /// Opens the release notes URL.
    /// </summary>
    [RelayCommand]
    private void OpenReleaseNotes()
    {
        if (!string.IsNullOrEmpty(ReleaseNotesUrl))
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = ReleaseNotesUrl,
                    UseShellExecute = true
                });
            }
            catch
            {
                // Ignore errors opening URL
            }
        }
    }

    private void OnUpdateCheckCompleted(object? sender, UpdateCheckResult result)
    {
        // Handle background update check completion
    }
}
