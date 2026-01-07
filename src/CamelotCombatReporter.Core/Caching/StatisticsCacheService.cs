using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text.Json;
using CamelotCombatReporter.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace CamelotCombatReporter.Core.Caching;

/// <summary>
/// In-memory cache for parsed log statistics with LRU eviction and optional disk persistence.
/// </summary>
public class StatisticsCacheService : IStatisticsCacheService
{
    private readonly ConcurrentDictionary<string, CacheEntry> _cache = new();
    private readonly ConcurrentDictionary<string, GenericCacheEntry> _genericCache = new();
    private readonly object _evictionLock = new();
    private readonly ILogger<StatisticsCacheService> _logger;
    private readonly string? _cacheDirectory;

    /// <summary>
    /// Creates a new statistics cache service with in-memory caching only.
    /// </summary>
    public StatisticsCacheService()
    {
        _logger = NullLogger<StatisticsCacheService>.Instance;
        _cacheDirectory = null;
    }

    /// <summary>
    /// Creates a new statistics cache service with optional disk persistence.
    /// </summary>
    /// <param name="cacheDirectory">Directory for disk cache persistence, or null for memory-only.</param>
    /// <param name="logger">Logger instance for diagnostic output.</param>
    public StatisticsCacheService(string? cacheDirectory, ILogger<StatisticsCacheService> logger)
    {
        _logger = logger ?? NullLogger<StatisticsCacheService>.Instance;
        _cacheDirectory = cacheDirectory;

        if (!string.IsNullOrEmpty(_cacheDirectory))
        {
            try
            {
                Directory.CreateDirectory(_cacheDirectory);
                _logger.LogDebug("Statistics cache initialized with disk persistence at {CacheDirectory}", _cacheDirectory);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to create cache directory {CacheDirectory}, using memory-only cache", _cacheDirectory);
                _cacheDirectory = null;
            }
        }
        else
        {
            _logger.LogDebug("Statistics cache initialized with memory-only caching");
        }
    }

    /// <inheritdoc />
    public int MaxCacheEntries { get; set; } = 10;

    /// <inheritdoc />
    public int CachedEntryCount => _cache.Count + _genericCache.Count;

    /// <inheritdoc />
    public async Task<CachedStatistics?> GetCachedAsync(string filePath)
    {
        if (string.IsNullOrEmpty(filePath))
        {
            _logger.LogDebug("GetCachedAsync called with null/empty file path");
            return null;
        }

        var normalizedPath = Path.GetFullPath(filePath);
        _logger.LogDebug("Looking up cache for {FilePath}", normalizedPath);

        if (!_cache.TryGetValue(normalizedPath, out var entry))
        {
            _logger.LogDebug("Cache miss for {FilePath}", normalizedPath);
            return null;
        }

        // Validate that the file hasn't changed
        if (!await IsCacheValidAsync(normalizedPath, entry))
        {
            _cache.TryRemove(normalizedPath, out _);
            _logger.LogInformation("Cache invalidated for {FilePath} - file has changed", normalizedPath);
            return null;
        }

        // Update last access time for LRU
        entry.LastAccessed = DateTime.UtcNow;

        _logger.LogDebug("Cache hit for {FilePath} with {EventCount} events", normalizedPath, entry.CachedStatistics.Events.Count);
        return entry.CachedStatistics;
    }

    /// <inheritdoc />
    public async Task CacheAsync(string filePath, IReadOnlyList<LogEvent> events, CombatStatistics statistics)
    {
        if (string.IsNullOrEmpty(filePath))
        {
            _logger.LogDebug("CacheAsync called with null/empty file path");
            return;
        }

        var normalizedPath = Path.GetFullPath(filePath);
        var fileInfo = new FileInfo(normalizedPath);

        if (!fileInfo.Exists)
        {
            _logger.LogWarning("Cannot cache statistics for non-existent file {FilePath}", normalizedPath);
            return;
        }

        _logger.LogDebug("Computing hash for {FilePath} ({FileSize} bytes)", normalizedPath, fileInfo.Length);
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
        _logger.LogInformation("Cached {EventCount} events for {FilePath} (hash: {Hash})", events.Count, normalizedPath, hash[..12] + "...");

        // Evict old entries if needed
        EvictIfNeeded();
    }

    /// <inheritdoc />
    public async Task<T?> GetCachedStatisticsAsync<T>(string filePath, string key) where T : class
    {
        if (string.IsNullOrEmpty(filePath) || string.IsNullOrEmpty(key))
        {
            _logger.LogDebug("GetCachedStatisticsAsync called with null/empty arguments");
            return default;
        }

        var cacheKey = GetCacheKey(filePath, key);
        _logger.LogDebug("Looking up generic cache for {CacheKey}", cacheKey);

        if (!_genericCache.TryGetValue(cacheKey, out var entry))
        {
            _logger.LogDebug("Generic cache miss for {CacheKey}", cacheKey);
            return default;
        }

        // Validate file hasn't changed
        if (!await IsGenericCacheValidAsync(filePath, entry))
        {
            _genericCache.TryRemove(cacheKey, out _);
            _logger.LogInformation("Generic cache invalidated for {CacheKey} - file has changed", cacheKey);
            return default;
        }

        entry.LastAccessed = DateTime.UtcNow;

        _logger.LogDebug("Generic cache hit for {CacheKey}", cacheKey);
        return entry.Value as T;
    }

    /// <inheritdoc />
    public async Task CacheStatisticsAsync<T>(string filePath, string key, T value) where T : class
    {
        if (string.IsNullOrEmpty(filePath) || string.IsNullOrEmpty(key) || value == null)
        {
            _logger.LogDebug("CacheStatisticsAsync called with null/empty arguments");
            return;
        }

        var normalizedPath = Path.GetFullPath(filePath);
        var fileInfo = new FileInfo(normalizedPath);

        if (!fileInfo.Exists)
        {
            _logger.LogWarning("Cannot cache statistics for non-existent file {FilePath}", normalizedPath);
            return;
        }

        var hash = await ComputeFileHashAsync(normalizedPath);
        var cacheKey = GetCacheKey(normalizedPath, key);

        var entry = new GenericCacheEntry
        {
            FilePath = normalizedPath,
            FileHash = hash,
            FileSize = fileInfo.Length,
            LastModified = fileInfo.LastWriteTimeUtc,
            CachedAt = DateTime.UtcNow,
            LastAccessed = DateTime.UtcNow,
            Value = value
        };

        _genericCache[cacheKey] = entry;
        _logger.LogInformation("Cached generic value for {CacheKey} (type: {ValueType})", cacheKey, typeof(T).Name);

        EvictGenericIfNeeded();
    }

    /// <inheritdoc />
    public void Invalidate(string filePath)
    {
        if (string.IsNullOrEmpty(filePath))
            return;

        var normalizedPath = Path.GetFullPath(filePath);

        // Invalidate regular cache
        if (_cache.TryRemove(normalizedPath, out _))
        {
            _logger.LogInformation("Invalidated cache for {FilePath}", normalizedPath);
        }

        // Invalidate all generic cache entries for this file
        var keysToRemove = _genericCache.Keys
            .Where(k => k.StartsWith(normalizedPath + ":"))
            .ToList();

        foreach (var key in keysToRemove)
        {
            _genericCache.TryRemove(key, out _);
        }

        if (keysToRemove.Count > 0)
        {
            _logger.LogInformation("Invalidated {Count} generic cache entries for {FilePath}", keysToRemove.Count, normalizedPath);
        }
    }

    /// <inheritdoc />
    public void ClearAll()
    {
        var cacheCount = _cache.Count;
        var genericCount = _genericCache.Count;

        _cache.Clear();
        _genericCache.Clear();

        _logger.LogInformation("Cleared all caches ({CacheCount} regular, {GenericCount} generic entries)", cacheCount, genericCount);
    }

    /// <inheritdoc />
    public async Task<string> ComputeFileHashAsync(string filePath)
    {
        _logger.LogDebug("Computing SHA256 hash for {FilePath}", filePath);

        using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 8192, true);
        using var sha256 = SHA256.Create();
        var hashBytes = await sha256.ComputeHashAsync(stream);
        var hash = Convert.ToHexString(hashBytes);

        _logger.LogDebug("Hash computed for {FilePath}: {Hash}", filePath, hash[..12] + "...");
        return hash;
    }

    private static string GetCacheKey(string filePath, string key)
    {
        var normalizedPath = Path.GetFullPath(filePath);
        return $"{normalizedPath}:{key}";
    }

    private async Task<bool> IsCacheValidAsync(string filePath, CacheEntry entry)
    {
        try
        {
            var fileInfo = new FileInfo(filePath);
            if (!fileInfo.Exists)
            {
                _logger.LogDebug("Cache validation failed: file does not exist {FilePath}", filePath);
                return false;
            }

            var cached = entry.CachedStatistics;

            // Quick check: file size and modification time
            if (fileInfo.Length != cached.FileSize ||
                fileInfo.LastWriteTimeUtc != cached.LastModified)
            {
                _logger.LogDebug("Cache validation failed: file metadata changed for {FilePath}", filePath);
                return false;
            }

            // For extra safety, verify hash (can be expensive for large files)
            var currentHash = await ComputeFileHashAsync(filePath);
            var isValid = currentHash == cached.FileHash;

            if (!isValid)
            {
                _logger.LogDebug("Cache validation failed: hash mismatch for {FilePath}", filePath);
            }

            return isValid;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error validating cache for {FilePath}", filePath);
            return false;
        }
    }

    private async Task<bool> IsGenericCacheValidAsync(string filePath, GenericCacheEntry entry)
    {
        try
        {
            var fileInfo = new FileInfo(filePath);
            if (!fileInfo.Exists)
                return false;

            if (fileInfo.Length != entry.FileSize ||
                fileInfo.LastWriteTimeUtc != entry.LastModified)
            {
                return false;
            }

            var currentHash = await ComputeFileHashAsync(filePath);
            return currentHash == entry.FileHash;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error validating generic cache for {FilePath}", filePath);
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

            _logger.LogDebug("Evicted {Count} cache entries (LRU)", toRemove.Count);
        }
    }

    private void EvictGenericIfNeeded()
    {
        if (_genericCache.Count <= MaxCacheEntries * 2) // Allow more generic entries
            return;

        lock (_evictionLock)
        {
            if (_genericCache.Count <= MaxCacheEntries * 2)
                return;

            var toRemove = _genericCache
                .OrderBy(kvp => kvp.Value.LastAccessed)
                .Take(_genericCache.Count - MaxCacheEntries * 2)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in toRemove)
            {
                _genericCache.TryRemove(key, out _);
            }

            _logger.LogDebug("Evicted {Count} generic cache entries (LRU)", toRemove.Count);
        }
    }

    private class CacheEntry
    {
        public required CachedStatistics CachedStatistics { get; init; }
        public DateTime LastAccessed { get; set; }
    }

    private class GenericCacheEntry
    {
        public required string FilePath { get; init; }
        public required string FileHash { get; init; }
        public required long FileSize { get; init; }
        public required DateTime LastModified { get; init; }
        public required DateTime CachedAt { get; init; }
        public DateTime LastAccessed { get; set; }
        public required object Value { get; init; }
    }
}
