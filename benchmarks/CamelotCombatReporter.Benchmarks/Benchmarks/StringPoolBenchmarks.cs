using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using CamelotCombatReporter.Core.Optimization;

namespace CamelotCombatReporter.Benchmarks.Benchmarks;

/// <summary>
/// Benchmarks for StringPool performance.
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class StringPoolBenchmarks
{
    private StringPool _pool = null!;
    private string[] _strings = Array.Empty<string>();
    private string[] _duplicateStrings = Array.Empty<string>();

    [Params(100, 1000, 10000)]
    public int StringCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _pool = new StringPool();

        // Generate unique strings
        _strings = Enumerable.Range(0, StringCount)
            .Select(i => $"String_{i}_Value")
            .ToArray();

        // Generate strings with many duplicates (simulating combat log patterns)
        var baseStrings = new[] { "You", "Player", "Goblin", "Skeleton", "Orc", "hits", "damage", "heals" };
        _duplicateStrings = Enumerable.Range(0, StringCount)
            .Select(i => baseStrings[i % baseStrings.Length])
            .ToArray();
    }

    [IterationSetup]
    public void IterationSetup()
    {
        _pool.Clear();
    }

    [Benchmark(Baseline = true)]
    public void InternUniqueStrings()
    {
        foreach (var str in _strings)
        {
            _ = _pool.Intern(str);
        }
    }

    [Benchmark]
    public void InternDuplicateStrings()
    {
        foreach (var str in _duplicateStrings)
        {
            _ = _pool.Intern(str);
        }
    }

    [Benchmark]
    public void InternWithoutPool()
    {
        // Baseline: what happens without pooling
        var strings = new List<string>();
        foreach (var str in _duplicateStrings)
        {
            strings.Add(str);
        }
    }
}
