using CamelotCombatReporter.Core.Caching;
using CamelotCombatReporter.Core.Models;
using Xunit;

namespace CamelotCombatReporter.Core.Tests.Caching;

public class StatisticsCacheServiceTests : IDisposable
{
    private readonly StatisticsCacheService _service;
    private readonly string _tempDir;
    private readonly List<string> _tempFiles = new();

    public StatisticsCacheServiceTests()
    {
        _service = new StatisticsCacheService();
        _tempDir = Path.Combine(Path.GetTempPath(), $"CacheTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        foreach (var file in _tempFiles)
        {
            if (File.Exists(file))
                File.Delete(file);
        }
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }

    private string CreateTempFile(string content)
    {
        var path = Path.Combine(_tempDir, $"test_{Guid.NewGuid():N}.log");
        File.WriteAllText(path, content);
        _tempFiles.Add(path);
        return path;
    }

    [Fact]
    public async Task GetCachedAsync_ReturnsNullForUncachedFile()
    {
        var file = CreateTempFile("[00:00:00] Test content");

        var result = await _service.GetCachedAsync(file);

        Assert.Null(result);
    }

    [Fact]
    public async Task CacheAsync_ThenGetCachedAsync_ReturnsCachedData()
    {
        var file = CreateTempFile("[00:00:00] Test content");
        var events = new List<LogEvent>
        {
            new DamageEvent(new TimeOnly(0, 0, 0), "Player", "Target", 100, "melee")
        };
        var stats = new CombatStatistics(1.0, 100, 1.67, 100, 100, 1, 0);

        await _service.CacheAsync(file, events, stats);
        var result = await _service.GetCachedAsync(file);

        Assert.NotNull(result);
        Assert.Equal(stats, result!.Statistics);
        Assert.Single(result.Events);
    }

    [Fact]
    public async Task GetCachedAsync_ReturnsNullAfterFileModified()
    {
        var file = CreateTempFile("[00:00:00] Original content");
        var events = new List<LogEvent>();
        var stats = new CombatStatistics(1.0, 100, 1.67, 100, 100, 1, 0);

        await _service.CacheAsync(file, events, stats);

        // Modify the file
        await Task.Delay(50); // Ensure different modification time
        File.WriteAllText(file, "[00:00:00] Modified content");

        var result = await _service.GetCachedAsync(file);

        Assert.Null(result);
    }

    [Fact]
    public async Task Invalidate_RemovesCacheEntry()
    {
        var file = CreateTempFile("[00:00:00] Test content");
        var events = new List<LogEvent>();
        var stats = new CombatStatistics(1.0, 100, 1.67, 100, 100, 1, 0);

        await _service.CacheAsync(file, events, stats);
        Assert.Equal(1, _service.CachedEntryCount);

        _service.Invalidate(file);

        Assert.Equal(0, _service.CachedEntryCount);
    }

    [Fact]
    public async Task ClearAll_RemovesAllEntries()
    {
        var file1 = CreateTempFile("[00:00:00] Test 1");
        var file2 = CreateTempFile("[00:00:00] Test 2");
        var events = new List<LogEvent>();
        var stats = new CombatStatistics(1.0, 100, 1.67, 100, 100, 1, 0);

        await _service.CacheAsync(file1, events, stats);
        await _service.CacheAsync(file2, events, stats);
        Assert.Equal(2, _service.CachedEntryCount);

        _service.ClearAll();

        Assert.Equal(0, _service.CachedEntryCount);
    }

    [Fact]
    public async Task ComputeFileHashAsync_ReturnsSameHashForSameContent()
    {
        var file1 = CreateTempFile("[00:00:00] Same content");
        var file2 = CreateTempFile("[00:00:00] Same content");

        var hash1 = await _service.ComputeFileHashAsync(file1);
        var hash2 = await _service.ComputeFileHashAsync(file2);

        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public async Task ComputeFileHashAsync_ReturnsDifferentHashForDifferentContent()
    {
        var file1 = CreateTempFile("[00:00:00] Content A");
        var file2 = CreateTempFile("[00:00:00] Content B");

        var hash1 = await _service.ComputeFileHashAsync(file1);
        var hash2 = await _service.ComputeFileHashAsync(file2);

        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public async Task LruEviction_RemovesOldestEntries()
    {
        _service.MaxCacheEntries = 3;

        var files = new List<string>();
        var events = new List<LogEvent>();
        var stats = new CombatStatistics(1.0, 100, 1.67, 100, 100, 1, 0);

        // Add 4 files (exceeds max of 3)
        for (int i = 0; i < 4; i++)
        {
            var file = CreateTempFile($"[00:00:00] Content {i}");
            files.Add(file);
            await _service.CacheAsync(file, events, stats);
            await Task.Delay(10); // Ensure different access times
        }

        Assert.Equal(3, _service.CachedEntryCount);

        // First file should have been evicted (LRU)
        var firstCached = await _service.GetCachedAsync(files[0]);
        Assert.Null(firstCached);

        // Last file should still be cached
        var lastCached = await _service.GetCachedAsync(files[3]);
        Assert.NotNull(lastCached);
    }

    [Fact]
    public async Task GetCachedAsync_UpdatesAccessTimeForLru()
    {
        _service.MaxCacheEntries = 2;

        var events = new List<LogEvent>();
        var stats = new CombatStatistics(1.0, 100, 1.67, 100, 100, 1, 0);

        var file1 = CreateTempFile("[00:00:00] Content 1");
        var file2 = CreateTempFile("[00:00:00] Content 2");

        await _service.CacheAsync(file1, events, stats);
        await Task.Delay(10);
        await _service.CacheAsync(file2, events, stats);

        // Access file1 to update its access time (making it more recent than file2)
        await _service.GetCachedAsync(file1);
        await Task.Delay(10);

        // Add file3, which should evict file2 (least recently accessed)
        var file3 = CreateTempFile("[00:00:00] Content 3");
        await _service.CacheAsync(file3, events, stats);

        // file1 should still be cached (was accessed more recently)
        var file1Cached = await _service.GetCachedAsync(file1);
        Assert.NotNull(file1Cached);

        // file2 should be evicted
        var file2Cached = await _service.GetCachedAsync(file2);
        Assert.Null(file2Cached);
    }

    [Fact]
    public async Task GetCachedAsync_ReturnsNullForDeletedFile()
    {
        var file = CreateTempFile("[00:00:00] Test content");
        var events = new List<LogEvent>();
        var stats = new CombatStatistics(1.0, 100, 1.67, 100, 100, 1, 0);

        await _service.CacheAsync(file, events, stats);

        // Delete the file
        File.Delete(file);
        _tempFiles.Remove(file);

        var result = await _service.GetCachedAsync(file);

        Assert.Null(result);
    }

    [Fact]
    public async Task CacheAsync_HandlesNullPath()
    {
        var events = new List<LogEvent>();
        var stats = new CombatStatistics(1.0, 100, 1.67, 100, 100, 1, 0);

        // Should not throw
        await _service.CacheAsync(null!, events, stats);
        await _service.CacheAsync("", events, stats);

        Assert.Equal(0, _service.CachedEntryCount);
    }

    [Fact]
    public async Task GetCachedAsync_HandlesNullPath()
    {
        var result1 = await _service.GetCachedAsync(null!);
        var result2 = await _service.GetCachedAsync("");

        Assert.Null(result1);
        Assert.Null(result2);
    }

    [Fact]
    public void Invalidate_HandlesNullPath()
    {
        // Should not throw
        _service.Invalidate(null!);
        _service.Invalidate("");
    }

    [Fact]
    public void MaxCacheEntries_CanBeModified()
    {
        Assert.Equal(10, _service.MaxCacheEntries);

        _service.MaxCacheEntries = 20;

        Assert.Equal(20, _service.MaxCacheEntries);
    }
}
