using System.Collections.Concurrent;
using System.Security.Cryptography;
using CamelotCombatReporter.Core.Models;

namespace CamelotCombatReporter.Core.Caching;

/// <summary>
/// In-memory cache for parsed log statistics with LRU eviction.
/// </summary>
public class StatisticsCacheService : IStatisticsCacheService
{
    private readonly ConcurrentDictionary<string, CacheEntry> _cache = new();
    private readonly object _evictionLock = new();

    /// <inheritdoc />
    public int MaxCacheEntries { get; set; } = 10;

    /// <inheritdoc />
    public int CachedEntryCount => _cache.Count;

    /// <inheritdoc />
    public async Task<CachedStatistics?> GetCachedAsync(string filePath)
    {
        if (string.IsNullOrEmpty(filePath))
            return null;

        var normalizedPath = Path.GetFullPath(filePath);

        if (!_cache.TryGetValue(normalizedPath, out var entry))
            return null;

        // Validate that the file hasn't changed
        if (!await IsCacheValidAsync(normalizedPath, entry))
        {
            _cache.TryRemove(normalizedPath, out _);
            return null;
        }

        // Update last access time for LRU
        entry.LastAccessed = DateTime.UtcNow;

        return entry.CachedStatistics;
    }

    /// <inheritdoc />
    public async Task CacheAsync(string filePath, IReadOnlyList<LogEvent> events, CombatStatistics statistics)
    {
        if (string.IsNullOrEmpty(filePath))
            return;

        var normalizedPath = Path.GetFullPath(filePath);
        var fileInfo = new FileInfo(normalizedPath);

        if (!fileInfo.Exists)
            return;

        var hash = await ComputeFileHashAsync(normalizedPath);

        var cachedStats = new CachedStatistics(
            FileHash: hash,
            FilePath: normalizedPath,
            FileSize: fileInfo.Length,
            LastModified: fileInfo.LastWriteTimeUtc,
            CachedAt: DateTime.UtcNow,
            Events: events,
            Statistics: statistics);

        var entry = new CacheEntry
        {
            CachedStatistics = cachedStats,
            LastAccessed = DateTime.UtcNow
        };

        _cache[normalizedPath] = entry;

        // Evict old entries if needed
        EvictIfNeeded();
    }

    /// <inheritdoc />
    public void Invalidate(string filePath)
    {
        if (string.IsNullOrEmpty(filePath))
            return;

        var normalizedPath = Path.GetFullPath(filePath);
        _cache.TryRemove(normalizedPath, out _);
    }

    /// <inheritdoc />
    public void ClearAll()
    {
        _cache.Clear();
    }

    /// <inheritdoc />
    public async Task<string> ComputeFileHashAsync(string filePath)
    {
        using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 8192, true);
        using var sha256 = SHA256.Create();
        var hashBytes = await sha256.ComputeHashAsync(stream);
        return Convert.ToHexString(hashBytes);
    }

    private async Task<bool> IsCacheValidAsync(string filePath, CacheEntry entry)
    {
        try
        {
            var fileInfo = new FileInfo(filePath);
            if (!fileInfo.Exists)
                return false;

            var cached = entry.CachedStatistics;

            // Quick check: file size and modification time
            if (fileInfo.Length != cached.FileSize ||
                fileInfo.LastWriteTimeUtc != cached.LastModified)
            {
                return false;
            }

            // For extra safety, verify hash (can be expensive for large files)
            // Only do this if file metadata matches
            var currentHash = await ComputeFileHashAsync(filePath);
            return currentHash == cached.FileHash;
        }
        catch
        {
            return false;
        }
    }

    private void EvictIfNeeded()
    {
        if (_cache.Count <= MaxCacheEntries)
            return;

        lock (_evictionLock)
        {
            if (_cache.Count <= MaxCacheEntries)
                return;

            // Find and remove the least recently used entries
            var toRemove = _cache
                .OrderBy(kvp => kvp.Value.LastAccessed)
                .Take(_cache.Count - MaxCacheEntries)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in toRemove)
            {
                _cache.TryRemove(key, out _);
            }
        }
    }

    private class CacheEntry
    {
        public required CachedStatistics CachedStatistics { get; init; }
        public DateTime LastAccessed { get; set; }
    }
}
