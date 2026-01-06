using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;
using CamelotCombatReporter.Benchmarks.Benchmarks;

namespace CamelotCombatReporter.Benchmarks;

/// <summary>
/// Performance benchmarks for Camelot Combat Reporter.
///
/// Usage:
///   dotnet run -c Release              # Run all benchmarks
///   dotnet run -c Release -- --filter *Parser*    # Run parser benchmarks only
///   dotnet run -c Release -- --filter *Cache*     # Run cache benchmarks only
///   dotnet run -c Release -- --filter *StringPool*  # Run string pool benchmarks only
/// </summary>
public class Program
{
    public static void Main(string[] args)
    {
        var config = DefaultConfig.Instance;

        // Parse command line args to determine which benchmarks to run
        if (args.Length == 0)
        {
            // Run all benchmarks
            Console.WriteLine("Running all benchmarks...");
            Console.WriteLine("Use --filter to run specific benchmarks:");
            Console.WriteLine("  --filter *Parser*     - Log parser benchmarks");
            Console.WriteLine("  --filter *Cache*      - Caching benchmarks");
            Console.WriteLine("  --filter *StringPool* - String pool benchmarks");
            Console.WriteLine();

            BenchmarkRunner.Run(new[]
            {
                typeof(LogParserBenchmarks),
                typeof(StringPoolBenchmarks),
                typeof(CachingBenchmarks)
            }, config, args);
        }
        else
        {
            // Use BenchmarkSwitcher for filtered runs
            BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, config);
        }
    }
}
