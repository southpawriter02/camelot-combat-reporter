using System.Globalization;
using System.Text.RegularExpressions;
using CamelotCombatReporter.Core.Models;

namespace CamelotCombatReporter.Core.Parsing;

/// <summary>
/// Parses a log file and extracts combat data.
/// </summary>
public class LogParser
{
    private readonly string _logFilePath;

    // Regex to capture damage dealt by the player.
    // Note the change from Python's (?P<name>...) to .NET's (?<name>...).
    private static readonly Regex DamageDealtPattern = new(
        @"^\[(?<timestamp>\d{2}:\d{2}:\d{2})\]\s+You hit (the )?(?<target>.+?) for (?<amount>\d+) points of(?: (?<type>\w+))? damage[!.]?$",
        RegexOptions.Compiled);

    public LogParser(string logFilePath)
    {
        _logFilePath = logFilePath;
    }

    /// <summary>
    /// Parses the log file and yields structured data for each relevant log entry.
    /// </summary>
    /// <returns>An enumerable collection of log events.</returns>
    public IEnumerable<LogEvent> Parse()
    {
        if (!File.Exists(_logFilePath))
        {
            // In a real app, we might use a logging framework or a more structured error handling approach.
            Console.Error.WriteLine($"Error: Log file not found at {_logFilePath}");
            yield break; // Stop iteration
        }

        // Using File.ReadLines for memory efficiency with large files.
        foreach (var line in File.ReadLines(_logFilePath))
        {
            var match = DamageDealtPattern.Match(line);
            if (match.Success)
            {
                var groups = match.Groups;

                var timestamp = TimeOnly.ParseExact(groups["timestamp"].Value, "HH:mm:ss", CultureInfo.InvariantCulture);
                var target = groups["target"].Value.Trim();
                var amount = int.Parse(groups["amount"].Value);
                var damageType = groups["type"].Success ? groups["type"].Value.Trim() : "Unknown";

                yield return new DamageEvent(
                    Timestamp: timestamp,
                    Source: "You",
                    Target: target,
                    DamageAmount: amount,
                    DamageType: damageType
                );
            }
            // In the future, other patterns (healing, damage taken, etc.) would be checked here.
        }
    }
}
