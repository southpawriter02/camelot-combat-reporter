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

    // Regex to capture damage taken by the player.
    private static readonly Regex DamageTakenPattern = new(
        @"^\[(?<timestamp>\d{2}:\d{2}:\d{2})\]\s+(?<source>.+?) hits you for (?<amount>\d+) points of(?: (?<type>\w+))? damage[.!]?$",
        RegexOptions.Compiled);

    // Regex to capture combat styles used by the player.
    private static readonly Regex CombatStylePattern = new(
        @"^\[(?<timestamp>\d{2}:\d{2}:\d{2})\]\s+You use (?<style>.+?) on (?<target>.+?)[.!]?$",
        RegexOptions.Compiled);

    // Regex to capture spells cast by the player.
    private static readonly Regex SpellCastPattern = new(
        @"^\[(?<timestamp>\d{2}:\d{2}:\d{2})\]\s+You cast (?<spell>.+?) on (?<target>.+?)[.!]?$",
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
            var dealtMatch = DamageDealtPattern.Match(line);
            if (dealtMatch.Success)
            {
                var groups = dealtMatch.Groups;

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
                continue; // Move to the next line
            }

            var takenMatch = DamageTakenPattern.Match(line);
            if (takenMatch.Success)
            {
                var groups = takenMatch.Groups;
                var timestamp = TimeOnly.ParseExact(groups["timestamp"].Value, "HH:mm:ss", CultureInfo.InvariantCulture);
                var source = groups["source"].Value.Trim();
                var amount = int.Parse(groups["amount"].Value);
                var damageType = groups["type"].Success ? groups["type"].Value.Trim() : "Unknown";

                yield return new DamageEvent(
                    Timestamp: timestamp,
                    Source: source,
                    Target: "You",
                    DamageAmount: amount,
                    DamageType: damageType
                );
                continue;
            }

            var styleMatch = CombatStylePattern.Match(line);
            if (styleMatch.Success)
            {
                var groups = styleMatch.Groups;
                var timestamp = TimeOnly.ParseExact(groups["timestamp"].Value, "HH:mm:ss", CultureInfo.InvariantCulture);
                var style = groups["style"].Value.Trim();
                var target = groups["target"].Value.Trim();

                yield return new CombatStyleEvent(
                    Timestamp: timestamp,
                    Source: "You",
                    Target: target,
                    StyleName: style
                );
                continue;
            }

            var spellMatch = SpellCastPattern.Match(line);
            if (spellMatch.Success)
            {
                var groups = spellMatch.Groups;
                var timestamp = TimeOnly.ParseExact(groups["timestamp"].Value, "HH:mm:ss", CultureInfo.InvariantCulture);
                var spell = groups["spell"].Value.Trim();
                var target = groups["target"].Value.Trim();

                yield return new SpellCastEvent(
                    Timestamp: timestamp,
                    Source: "You",
                    Target: target,
                    SpellName: spell
                );
                continue;
            }
        }
    }
}
