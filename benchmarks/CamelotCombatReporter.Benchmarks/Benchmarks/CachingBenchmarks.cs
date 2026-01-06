using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using CamelotCombatReporter.Core.Caching;
using CamelotCombatReporter.Core.Models;
using CamelotCombatReporter.Core.Parsing;

namespace CamelotCombatReporter.Benchmarks.Benchmarks;

/// <summary>
/// Benchmarks for statistics caching performance.
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class CachingBenchmarks
{
    private StatisticsCacheService _cacheService = null!;
    private string _tempFilePath = string.Empty;
    private IReadOnlyList<LogEvent> _events = Array.Empty<LogEvent>();
    private CombatStatistics _stats = null!;

    [GlobalSetup]
    public void Setup()
    {
        _cacheService = new StatisticsCacheService();

        // Create a sample log file
        var lines = new List<string>();
        var baseTime = new TimeOnly(12, 0, 0);

        for (int i = 0; i < 1000; i++)
        {
            var time = baseTime.Add(TimeSpan.FromSeconds(i));
            lines.Add($"[{time:HH:mm:ss}] You hit Goblin for {100 + (i % 100)} damage!");
        }

        _tempFilePath = Path.GetTempFileName();
        File.WriteAllText(_tempFilePath, string.Join(Environment.NewLine, lines));

        // Pre-parse for cache operations
        var parser = new LogParser(_tempFilePath);
        _events = parser.Parse().ToList();
        _stats = new CombatStatistics(
            DurationMinutes: 16.67,
            TotalDamage: 149500,
            Dps: 149.5,
            AverageDamage: 149.5,
            MedianDamage: 149.0,
            CombatStylesCount: 0,
            SpellsCastCount: 0);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        if (File.Exists(_tempFilePath))
        {
            File.Delete(_tempFilePath);
        }
    }

    [IterationSetup]
    public void IterationSetup()
    {
        _cacheService.ClearAll();
    }

    [Benchmark(Baseline = true)]
    public async Task ParseWithoutCache()
    {
        var parser = new LogParser(_tempFilePath);
        var result = await parser.ParseAsync();
        _ = result.Events.Count;
    }

    [Benchmark]
    public async Task CacheAndRetrieve()
    {
        // Cache the stats
        await _cacheService.CacheAsync(_tempFilePath, _events, _stats);

        // Retrieve from cache
        var cached = await _cacheService.GetCachedAsync(_tempFilePath);
        _ = cached?.Events.Count;
    }

    [Benchmark]
    public async Task ComputeFileHash()
    {
        _ = await _cacheService.ComputeFileHashAsync(_tempFilePath);
    }
}
