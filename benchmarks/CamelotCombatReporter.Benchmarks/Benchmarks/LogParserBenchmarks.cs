using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using CamelotCombatReporter.Core.Parsing;

namespace CamelotCombatReporter.Benchmarks.Benchmarks;

/// <summary>
/// Benchmarks for LogParser performance.
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class LogParserBenchmarks
{
    private string _tempFilePath = string.Empty;

    /// <summary>
    /// Number of log lines to generate for benchmarking.
    /// </summary>
    [Params(100, 1000, 10000)]
    public int LineCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        // Generate sample log content
        var lines = new List<string>();
        var random = new Random(42); // Fixed seed for reproducibility
        var baseTime = new TimeOnly(12, 0, 0);

        for (int i = 0; i < LineCount; i++)
        {
            var time = baseTime.Add(TimeSpan.FromSeconds(i));
            var timeStr = time.ToString("HH:mm:ss");

            // Generate different event types
            var eventType = random.Next(10);
            var line = eventType switch
            {
                0 or 1 or 2 => $"[{timeStr}] You hit Goblin for 150 damage!",
                3 or 4 => $"[{timeStr}] Goblin hits you for 75 damage!",
                5 => $"[{timeStr}] You are healed by Cleric for 200 hit points.",
                6 => $"[{timeStr}] You heal Warrior for 180 hit points.",
                7 => $"[{timeStr}] You cast Lightning Bolt",
                8 => $"[{timeStr}] You perform Amethyst Slash",
                _ => $"[{timeStr}] Goblin was killed by you!"
            };
            lines.Add(line);
        }

        var sampleLogContent = string.Join(Environment.NewLine, lines);

        // Create temp file for file-based parsing
        _tempFilePath = Path.GetTempFileName();
        File.WriteAllText(_tempFilePath, sampleLogContent);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        if (File.Exists(_tempFilePath))
        {
            File.Delete(_tempFilePath);
        }
    }

    [Benchmark(Baseline = true)]
    public void ParseFile_Sync()
    {
        var parser = new LogParser(_tempFilePath);
        var events = parser.Parse();
        // Force enumeration
        _ = events.Count();
    }

    [Benchmark]
    public async Task ParseFile_Async()
    {
        var parser = new LogParser(_tempFilePath);
        var result = await parser.ParseAsync();
        _ = result.Events.Count;
    }
}
