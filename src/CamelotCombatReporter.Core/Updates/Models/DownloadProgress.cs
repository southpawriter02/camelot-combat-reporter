namespace CamelotCombatReporter.Core.Updates.Models;

/// <summary>
/// Represents the progress of a download operation.
/// </summary>
/// <param name="BytesDownloaded">Number of bytes downloaded so far.</param>
/// <param name="TotalBytes">Total number of bytes to download, or null if unknown.</param>
/// <param name="Percentage">Download percentage (0-100), or null if total is unknown.</param>
/// <param name="BytesPerSecond">Current download speed in bytes per second.</param>
public record DownloadProgress(
    long BytesDownloaded,
    long? TotalBytes,
    double? Percentage,
    double BytesPerSecond)
{
    /// <summary>
    /// Gets a human-readable string representation of bytes downloaded.
    /// </summary>
    public string BytesDownloadedFormatted => FormatBytes(BytesDownloaded);

    /// <summary>
    /// Gets a human-readable string representation of total bytes.
    /// </summary>
    public string? TotalBytesFormatted => TotalBytes.HasValue ? FormatBytes(TotalBytes.Value) : null;

    /// <summary>
    /// Gets a human-readable string representation of download speed.
    /// </summary>
    public string SpeedFormatted => $"{FormatBytes((long)BytesPerSecond)}/s";

    /// <summary>
    /// Gets the estimated time remaining in seconds, or null if unknown.
    /// </summary>
    public double? EstimatedSecondsRemaining
    {
        get
        {
            if (!TotalBytes.HasValue || BytesPerSecond <= 0)
                return null;

            var remaining = TotalBytes.Value - BytesDownloaded;
            return remaining / BytesPerSecond;
        }
    }

    /// <summary>
    /// Gets a human-readable string representation of estimated time remaining.
    /// </summary>
    public string? EstimatedTimeRemainingFormatted
    {
        get
        {
            var seconds = EstimatedSecondsRemaining;
            if (!seconds.HasValue)
                return null;

            var timeSpan = TimeSpan.FromSeconds(seconds.Value);
            if (timeSpan.TotalHours >= 1)
                return $"{timeSpan.Hours}h {timeSpan.Minutes}m remaining";
            if (timeSpan.TotalMinutes >= 1)
                return $"{timeSpan.Minutes}m {timeSpan.Seconds}s remaining";
            return $"{timeSpan.Seconds}s remaining";
        }
    }

    private static string FormatBytes(long bytes)
    {
        string[] sizes = ["B", "KB", "MB", "GB", "TB"];
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }

    /// <summary>
    /// Creates an initial progress with zero bytes downloaded.
    /// </summary>
    public static DownloadProgress Initial(long? totalBytes = null) =>
        new(0, totalBytes, totalBytes.HasValue ? 0 : null, 0);
}
