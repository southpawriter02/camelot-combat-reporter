using System.Collections.Concurrent;

namespace CamelotCombatReporter.Core.Optimization;

/// <summary>
/// Thread-safe string pool for interning frequently used strings.
/// Reduces memory usage when parsing logs with many repeated strings.
/// </summary>
public class StringPool
{
    private readonly ConcurrentDictionary<string, string> _pool = new();
    private readonly int _maxPoolSize;
    private int _evictionCount;

    /// <summary>
    /// Creates a new string pool.
    /// </summary>
    /// <param name="maxPoolSize">Maximum number of strings to keep in the pool.</param>
    public StringPool(int maxPoolSize = 10000)
    {
        _maxPoolSize = maxPoolSize;
    }

    /// <summary>
    /// Gets the current number of interned strings.
    /// </summary>
    public int Count => _pool.Count;

    /// <summary>
    /// Interns a string, returning a pooled instance if available.
    /// </summary>
    /// <param name="value">The string to intern.</param>
    /// <returns>The pooled string instance, or the original if not pooled.</returns>
    public string Intern(string value)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        // Try to get existing instance
        if (_pool.TryGetValue(value, out var existing))
            return existing;

        // Check if we need to evict
        if (_pool.Count >= _maxPoolSize)
        {
            EvictOldEntries();
        }

        // Add to pool
        _pool.TryAdd(value, value);
        return _pool.GetOrAdd(value, value);
    }

    /// <summary>
    /// Clears the string pool.
    /// </summary>
    public void Clear()
    {
        _pool.Clear();
        _evictionCount = 0;
    }

    /// <summary>
    /// Gets statistics about the pool.
    /// </summary>
    public (int Count, int EvictionCount, long ApproximateMemorySaved) GetStatistics()
    {
        // Estimate memory saved: each duplicate string reference saves ~40+ bytes
        // This is a rough approximation
        long memorySaved = _pool.Count * 40L;
        return (_pool.Count, _evictionCount, memorySaved);
    }

    private void EvictOldEntries()
    {
        // Simple eviction: remove a portion of entries
        // More sophisticated LRU could be implemented if needed
        var toRemove = _pool.Keys.Take(_maxPoolSize / 4).ToList();
        foreach (var key in toRemove)
        {
            _pool.TryRemove(key, out _);
            _evictionCount++;
        }
    }

    /// <summary>
    /// Gets the default shared instance for common use cases.
    /// </summary>
    public static StringPool Shared { get; } = new();
}
