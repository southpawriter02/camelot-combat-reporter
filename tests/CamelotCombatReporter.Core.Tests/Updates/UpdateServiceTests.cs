using CamelotCombatReporter.Core.Updates;
using CamelotCombatReporter.Core.Updates.Models;
using Xunit;

namespace CamelotCombatReporter.Core.Tests.Updates;

public class UpdateServiceTests
{
    [Fact]
    public void CurrentVersion_ReturnsValidVersion()
    {
        // Arrange
        var service = new UpdateService();

        // Act
        var version = service.CurrentVersion;

        // Assert
        Assert.NotNull(version);
        Assert.True(version >= new Version(0, 0, 0));
    }

    [Fact]
    public void Channel_DefaultsToStable()
    {
        // Arrange
        var service = new UpdateService();

        // Act & Assert
        Assert.Equal(UpdateChannel.Stable, service.Channel);
    }

    [Fact]
    public void Channel_CanBeChanged()
    {
        // Arrange
        var service = new UpdateService();

        // Act
        service.Channel = UpdateChannel.Beta;

        // Assert
        Assert.Equal(UpdateChannel.Beta, service.Channel);
    }

    [Fact]
    public void AutoCheckEnabled_DefaultsToTrue()
    {
        // Arrange
        var service = new UpdateService();

        // Act & Assert
        Assert.True(service.AutoCheckEnabled);
    }

    [Fact]
    public void CanRollback_ReturnsFalseWhenNoBackup()
    {
        // Arrange
        var service = new UpdateService();

        // Act & Assert
        Assert.False(service.CanRollback);
    }

    [Fact]
    public void RollbackVersion_ReturnsNullWhenNoBackup()
    {
        // Arrange
        var service = new UpdateService();

        // Act & Assert
        Assert.Null(service.RollbackVersion);
    }
}

public class UpdateInfoTests
{
    [Fact]
    public void ParsedVersion_ReturnsValidVersion()
    {
        // Arrange
        var info = CreateTestUpdateInfo("1.6.0");

        // Act
        var version = info.ParsedVersion;

        // Assert
        Assert.Equal(new Version(1, 6, 0), version);
    }

    [Fact]
    public void ParsedVersion_ReturnsZeroForInvalidVersion()
    {
        // Arrange
        var info = CreateTestUpdateInfo("invalid");

        // Act
        var version = info.ParsedVersion;

        // Assert
        Assert.Equal(new Version(0, 0, 0), version);
    }

    [Fact]
    public void ParsedReleaseDate_ReturnsValidDate()
    {
        // Arrange
        var info = CreateTestUpdateInfo("1.6.0", "2026-01-15");

        // Act
        var date = info.ParsedReleaseDate;

        // Assert
        Assert.NotNull(date);
        Assert.Equal(new DateOnly(2026, 1, 15), date.Value);
    }

    [Fact]
    public void ParsedReleaseDate_ReturnsNullForInvalidDate()
    {
        // Arrange
        var info = CreateTestUpdateInfo("1.6.0", "invalid-date");

        // Act
        var date = info.ParsedReleaseDate;

        // Assert
        Assert.Null(date);
    }

    [Fact]
    public void GetDownloadUrlForCurrentPlatform_ReturnsCorrectUrl()
    {
        // Arrange
        var info = CreateTestUpdateInfo("1.6.0");

        // Act
        var url = info.GetDownloadUrlForCurrentPlatform();

        // Assert - URL should be returned for some platform
        // Exact URL depends on current OS
        Assert.NotNull(url);
    }

    [Fact]
    public void GetChecksumForCurrentPlatform_ReturnsCorrectChecksum()
    {
        // Arrange
        var info = CreateTestUpdateInfo("1.6.0");

        // Act
        var checksum = info.GetChecksumForCurrentPlatform();

        // Assert
        Assert.NotNull(checksum);
        Assert.StartsWith("sha256:", checksum);
    }

    private static UpdateInfo CreateTestUpdateInfo(string version, string releaseDate = "2026-01-15")
    {
        return new UpdateInfo
        {
            Version = version,
            ReleaseDate = releaseDate,
            ReleaseNotesUrl = "https://example.com/releases",
            Downloads = new Dictionary<string, string>
            {
                ["win-x64-msi"] = "https://example.com/win.msi",
                ["win-x64-zip"] = "https://example.com/win.zip",
                ["osx-universal"] = "https://example.com/mac.dmg",
                ["linux-x64-appimage"] = "https://example.com/linux.AppImage",
                ["linux-x64-deb"] = "https://example.com/linux.deb",
                ["linux-x64-rpm"] = "https://example.com/linux.rpm"
            },
            Checksums = new Dictionary<string, string>
            {
                ["win-x64-msi"] = "sha256:abc123",
                ["win-x64-zip"] = "sha256:def456",
                ["osx-universal"] = "sha256:ghi789",
                ["linux-x64-appimage"] = "sha256:jkl012",
                ["linux-x64-deb"] = "sha256:mno345",
                ["linux-x64-rpm"] = "sha256:pqr678"
            },
            IsRequired = false,
            MinimumVersion = null
        };
    }
}

public class UpdateCheckResultTests
{
    [Fact]
    public void NoUpdateAvailable_CreatesCorrectResult()
    {
        // Arrange
        var currentVersion = new Version(1, 5, 0);

        // Act
        var result = UpdateCheckResult.NoUpdateAvailable(currentVersion);

        // Assert
        Assert.False(result.IsUpdateAvailable);
        Assert.Equal(currentVersion, result.CurrentVersion);
        Assert.Null(result.UpdateInfo);
        Assert.Null(result.ErrorMessage);
        Assert.True(result.WasSuccessful);
    }

    [Fact]
    public void UpdateAvailable_CreatesCorrectResult()
    {
        // Arrange
        var currentVersion = new Version(1, 5, 0);
        var updateInfo = new UpdateInfo
        {
            Version = "1.6.0",
            ReleaseDate = "2026-01-15",
            ReleaseNotesUrl = "https://example.com",
            Downloads = new Dictionary<string, string>(),
            Checksums = new Dictionary<string, string>()
        };

        // Act
        var result = UpdateCheckResult.UpdateAvailable(currentVersion, updateInfo);

        // Assert
        Assert.True(result.IsUpdateAvailable);
        Assert.Equal(currentVersion, result.CurrentVersion);
        Assert.NotNull(result.UpdateInfo);
        Assert.Equal("1.6.0", result.UpdateInfo.Version);
        Assert.Null(result.ErrorMessage);
        Assert.True(result.WasSuccessful);
    }

    [Fact]
    public void Error_CreatesCorrectResult()
    {
        // Arrange
        var currentVersion = new Version(1, 5, 0);
        var errorMessage = "Network error";

        // Act
        var result = UpdateCheckResult.Error(currentVersion, errorMessage);

        // Assert
        Assert.False(result.IsUpdateAvailable);
        Assert.Equal(currentVersion, result.CurrentVersion);
        Assert.Null(result.UpdateInfo);
        Assert.Equal(errorMessage, result.ErrorMessage);
        Assert.False(result.WasSuccessful);
    }
}

public class DownloadProgressTests
{
    [Fact]
    public void BytesDownloadedFormatted_FormatsCorrectly()
    {
        // Arrange
        var progress = new DownloadProgress(1024 * 1024, 10 * 1024 * 1024, 10.0, 512 * 1024);

        // Act
        var formatted = progress.BytesDownloadedFormatted;

        // Assert
        Assert.Equal("1 MB", formatted);
    }

    [Fact]
    public void TotalBytesFormatted_FormatsCorrectly()
    {
        // Arrange
        var progress = new DownloadProgress(1024 * 1024, 10 * 1024 * 1024, 10.0, 512 * 1024);

        // Act
        var formatted = progress.TotalBytesFormatted;

        // Assert
        Assert.Equal("10 MB", formatted);
    }

    [Fact]
    public void TotalBytesFormatted_ReturnsNullWhenUnknown()
    {
        // Arrange
        var progress = new DownloadProgress(1024 * 1024, null, null, 512 * 1024);

        // Act
        var formatted = progress.TotalBytesFormatted;

        // Assert
        Assert.Null(formatted);
    }

    [Fact]
    public void SpeedFormatted_FormatsCorrectly()
    {
        // Arrange
        var progress = new DownloadProgress(1024 * 1024, 10 * 1024 * 1024, 10.0, 512 * 1024);

        // Act
        var formatted = progress.SpeedFormatted;

        // Assert
        Assert.Equal("512 KB/s", formatted);
    }

    [Fact]
    public void EstimatedSecondsRemaining_CalculatesCorrectly()
    {
        // Arrange - 1MB downloaded, 10MB total, 1MB/s speed
        var progress = new DownloadProgress(1024 * 1024, 10 * 1024 * 1024, 10.0, 1024 * 1024);

        // Act
        var remaining = progress.EstimatedSecondsRemaining;

        // Assert - Should be ~9 seconds remaining
        Assert.NotNull(remaining);
        Assert.Equal(9, remaining.Value, 0.1);
    }

    [Fact]
    public void EstimatedSecondsRemaining_ReturnsNullWhenTotalUnknown()
    {
        // Arrange
        var progress = new DownloadProgress(1024 * 1024, null, null, 1024 * 1024);

        // Act
        var remaining = progress.EstimatedSecondsRemaining;

        // Assert
        Assert.Null(remaining);
    }

    [Fact]
    public void EstimatedSecondsRemaining_ReturnsNullWhenSpeedIsZero()
    {
        // Arrange
        var progress = new DownloadProgress(1024 * 1024, 10 * 1024 * 1024, 10.0, 0);

        // Act
        var remaining = progress.EstimatedSecondsRemaining;

        // Assert
        Assert.Null(remaining);
    }

    [Fact]
    public void EstimatedTimeRemainingFormatted_FormatsSecondsCorrectly()
    {
        // Arrange - 45 seconds remaining
        var progress = new DownloadProgress(0, 45 * 1024 * 1024, 0, 1024 * 1024);

        // Act
        var formatted = progress.EstimatedTimeRemainingFormatted;

        // Assert
        Assert.NotNull(formatted);
        Assert.Contains("s remaining", formatted);
    }

    [Fact]
    public void EstimatedTimeRemainingFormatted_FormatsMinutesCorrectly()
    {
        // Arrange - 5 minutes remaining (300 seconds)
        var progress = new DownloadProgress(0, 300 * 1024 * 1024, 0, 1024 * 1024);

        // Act
        var formatted = progress.EstimatedTimeRemainingFormatted;

        // Assert
        Assert.NotNull(formatted);
        Assert.Contains("m", formatted);
        Assert.Contains("remaining", formatted);
    }

    [Fact]
    public void Initial_CreatesZeroProgress()
    {
        // Act
        var progress = DownloadProgress.Initial(10 * 1024 * 1024);

        // Assert
        Assert.Equal(0, progress.BytesDownloaded);
        Assert.Equal(10 * 1024 * 1024, progress.TotalBytes);
        Assert.Equal(0, progress.Percentage);
        Assert.Equal(0, progress.BytesPerSecond);
    }
}

public class UpdateChannelTests
{
    [Fact]
    public void UpdateChannel_HasExpectedValues()
    {
        // Assert
        Assert.Equal(0, (int)UpdateChannel.Stable);
        Assert.Equal(1, (int)UpdateChannel.Beta);
        Assert.Equal(2, (int)UpdateChannel.Dev);
    }
}
