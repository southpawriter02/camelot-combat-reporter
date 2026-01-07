using CamelotCombatReporter.Core.Models;

namespace CamelotCombatReporter.Core.Caching;

/// <summary>
/// Represents a cached statistics entry for a parsed log file.
/// </summary>
/// <param name="FileHash">SHA256 hash of the file for cache validation.</param>
/// <param name="FilePath">The path to the original file.</param>
/// <param name="FileSize">Size of the file in bytes.</param>
/// <param name="LastModified">Last modification time of the file.</param>
/// <param name="CachedAt">When the statistics were cached.</param>
/// <param name="Events">The parsed events.</param>
/// <param name="Statistics">Pre-computed combat statistics.</param>
public record CachedStatistics(
    string FileHash,
    string FilePath,
    long FileSize,
    DateTime LastModified,
    DateTime CachedAt,
    IReadOnlyList<LogEvent> Events,
    CombatStatistics Statistics);

/// <summary>
/// Service for caching parsed log statistics to avoid re-parsing unchanged files.
/// </summary>
public interface IStatisticsCacheService
{
    /// <summary>
    /// Gets the maximum number of entries to keep in the cache.
    /// </summary>
    int MaxCacheEntries { get; set; }

    /// <summary>
    /// Gets the current number of cached entries.
    /// </summary>
    int CachedEntryCount { get; }

    /// <summary>
    /// Attempts to retrieve cached statistics for a file.
    /// </summary>
    /// <param name="filePath">Path to the log file.</param>
    /// <returns>Cached statistics if valid cache exists, null otherwise.</returns>
    Task<CachedStatistics?> GetCachedAsync(string filePath);

    /// <summary>
    /// Caches statistics for a file.
    /// </summary>
    /// <param name="filePath">Path to the log file.</param>
    /// <param name="events">The parsed events.</param>
    /// <param name="statistics">The computed statistics.</param>
    Task CacheAsync(string filePath, IReadOnlyList<LogEvent> events, CombatStatistics statistics);

    /// <summary>
    /// Attempts to retrieve a cached value for a file with a specific key.
    /// </summary>
    /// <typeparam name="T">The type of the cached value.</typeparam>
    /// <param name="filePath">Path to the log file.</param>
    /// <param name="key">A key identifying the cached value type.</param>
    /// <returns>The cached value if valid cache exists, default otherwise.</returns>
    Task<T?> GetCachedStatisticsAsync<T>(string filePath, string key) where T : class;

    /// <summary>
    /// Caches a value for a file with a specific key.
    /// </summary>
    /// <typeparam name="T">The type of the value to cache.</typeparam>
    /// <param name="filePath">Path to the log file.</param>
    /// <param name="key">A key identifying the cached value type.</param>
    /// <param name="value">The value to cache.</param>
    Task CacheStatisticsAsync<T>(string filePath, string key, T value) where T : class;

    /// <summary>
    /// Invalidates the cache for a specific file.
    /// </summary>
    /// <param name="filePath">Path to the log file.</param>
    void Invalidate(string filePath);

    /// <summary>
    /// Clears all cached entries.
    /// </summary>
    void ClearAll();

    /// <summary>
    /// Computes the hash of a file for cache validation.
    /// </summary>
    /// <param name="filePath">Path to the file.</param>
    /// <returns>SHA256 hash as a hex string.</returns>
    Task<string> ComputeFileHashAsync(string filePath);
}
