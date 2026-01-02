using System.Globalization;
using System.Text.RegularExpressions;
using CamelotCombatReporter.Core.Models;

namespace CamelotCombatReporter.Core.Parsing;

/// <summary>
/// Interface for parser plugins to participate in log parsing.
/// </summary>
public interface ILogParserPlugin
{
    /// <summary>
    /// Priority for pattern matching (higher = checked first).
    /// Built-in patterns have priority -100.
    /// </summary>
    int Priority { get; }

    /// <summary>
    /// Attempts to parse a log line.
    /// </summary>
    /// <param name="line">The log line to parse.</param>
    /// <param name="lineNumber">Current line number.</param>
    /// <param name="recentEvents">Recent events for context.</param>
    /// <returns>Parse result indicating success or skip.</returns>
    ParserPluginResult TryParse(string line, int lineNumber, IReadOnlyList<LogEvent> recentEvents);
}

/// <summary>
/// Result from a parser plugin.
/// </summary>
public abstract record ParserPluginResult;

/// <summary>
/// Plugin successfully parsed the line.
/// </summary>
public sealed record ParserPluginSuccess(LogEvent Event) : ParserPluginResult;

/// <summary>
/// Plugin did not recognize the line.
/// </summary>
public sealed record ParserPluginSkip : ParserPluginResult
{
    public static ParserPluginSkip Instance { get; } = new();
}

/// <summary>
/// Parses a log file and extracts combat data.
/// </summary>
public class LogParser
{
    private readonly string _logFilePath;
    private readonly List<ILogParserPlugin> _plugins = new();

    // Built-in parser priority
    private const int BuiltInPriority = -100;

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

    // Regex to capture healing done by the player.
    private static readonly Regex HealingDonePattern = new(
        @"^\[(?<timestamp>\d{2}:\d{2}:\d{2})\]\s+You heal (?<target>.+?) for (?<amount>\d+) hit points[.!]?$",
        RegexOptions.Compiled);

    // Regex to capture healing received by the player.
    private static readonly Regex HealingReceivedPattern = new(
        @"^\[(?<timestamp>\d{2}:\d{2}:\d{2})\]\s+(?<source>.+?) heals you for (?<amount>\d+) hit points[.!]?$",
        RegexOptions.Compiled);

    public LogParser(string logFilePath)
    {
        _logFilePath = logFilePath;
    }

    /// <summary>
    /// Registers a parser plugin.
    /// </summary>
    public void RegisterPlugin(ILogParserPlugin plugin)
    {
        _plugins.Add(plugin);
        _plugins.Sort((a, b) => b.Priority.CompareTo(a.Priority)); // Higher priority first
    }

    /// <summary>
    /// Unregisters a parser plugin.
    /// </summary>
    public void UnregisterPlugin(ILogParserPlugin plugin)
    {
        _plugins.Remove(plugin);
    }

    /// <summary>
    /// Clears all registered parser plugins.
    /// </summary>
    public void ClearPlugins()
    {
        _plugins.Clear();
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

        var recentEvents = new List<LogEvent>();
        const int MaxRecentEvents = 10;
        var lineNumber = 0;

        // Using File.ReadLines for memory efficiency with large files.
        foreach (var line in File.ReadLines(_logFilePath))
        {
            lineNumber++;

            // Try plugins first (ordered by priority, highest first)
            var pluginHandled = false;
            foreach (var plugin in _plugins)
            {
                var result = plugin.TryParse(line, lineNumber, recentEvents.AsReadOnly());
                if (result is ParserPluginSuccess success)
                {
                    AddToRecentEvents(recentEvents, success.Event, MaxRecentEvents);
                    yield return success.Event;
                    pluginHandled = true;
                    break;
                }
            }

            if (pluginHandled)
            {
                continue;
            }

            // Fall back to built-in patterns
            var dealtMatch = DamageDealtPattern.Match(line);
            if (dealtMatch.Success)
            {
                var groups = dealtMatch.Groups;

                var timestamp = TimeOnly.ParseExact(groups["timestamp"].Value, "HH:mm:ss", CultureInfo.InvariantCulture);
                var target = groups["target"].Value.Trim();
                var amount = int.Parse(groups["amount"].Value);
                var damageType = groups["type"].Success ? groups["type"].Value.Trim() : "Unknown";

                var evt = new DamageEvent(
                    Timestamp: timestamp,
                    Source: "You",
                    Target: target,
                    DamageAmount: amount,
                    DamageType: damageType
                );
                AddToRecentEvents(recentEvents, evt, MaxRecentEvents);
                yield return evt;
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

                var evt = new DamageEvent(
                    Timestamp: timestamp,
                    Source: source,
                    Target: "You",
                    DamageAmount: amount,
                    DamageType: damageType
                );
                AddToRecentEvents(recentEvents, evt, MaxRecentEvents);
                yield return evt;
                continue;
            }

            var styleMatch = CombatStylePattern.Match(line);
            if (styleMatch.Success)
            {
                var groups = styleMatch.Groups;
                var timestamp = TimeOnly.ParseExact(groups["timestamp"].Value, "HH:mm:ss", CultureInfo.InvariantCulture);
                var style = groups["style"].Value.Trim();
                var target = groups["target"].Value.Trim();

                var evt = new CombatStyleEvent(
                    Timestamp: timestamp,
                    Source: "You",
                    Target: target,
                    StyleName: style
                );
                AddToRecentEvents(recentEvents, evt, MaxRecentEvents);
                yield return evt;
                continue;
            }

            var spellMatch = SpellCastPattern.Match(line);
            if (spellMatch.Success)
            {
                var groups = spellMatch.Groups;
                var timestamp = TimeOnly.ParseExact(groups["timestamp"].Value, "HH:mm:ss", CultureInfo.InvariantCulture);
                var spell = groups["spell"].Value.Trim();
                var target = groups["target"].Value.Trim();

                var evt = new SpellCastEvent(
                    Timestamp: timestamp,
                    Source: "You",
                    Target: target,
                    SpellName: spell
                );
                AddToRecentEvents(recentEvents, evt, MaxRecentEvents);
                yield return evt;
                continue;
            }

            var healDoneMatch = HealingDonePattern.Match(line);
            if (healDoneMatch.Success)
            {
                var groups = healDoneMatch.Groups;
                var timestamp = TimeOnly.ParseExact(groups["timestamp"].Value, "HH:mm:ss", CultureInfo.InvariantCulture);
                var target = groups["target"].Value.Trim();
                var amount = int.Parse(groups["amount"].Value);

                var evt = new HealingEvent(
                    Timestamp: timestamp,
                    Source: "You",
                    Target: target,
                    HealingAmount: amount
                );
                AddToRecentEvents(recentEvents, evt, MaxRecentEvents);
                yield return evt;
                continue;
            }

            var healReceivedMatch = HealingReceivedPattern.Match(line);
            if (healReceivedMatch.Success)
            {
                var groups = healReceivedMatch.Groups;
                var timestamp = TimeOnly.ParseExact(groups["timestamp"].Value, "HH:mm:ss", CultureInfo.InvariantCulture);
                var source = groups["source"].Value.Trim();
                var amount = int.Parse(groups["amount"].Value);

                var evt = new HealingEvent(
                    Timestamp: timestamp,
                    Source: source,
                    Target: "You",
                    HealingAmount: amount
                );
                AddToRecentEvents(recentEvents, evt, MaxRecentEvents);
                yield return evt;
                continue;
            }
        }
    }

    private static void AddToRecentEvents(List<LogEvent> recentEvents, LogEvent evt, int maxCount)
    {
        recentEvents.Add(evt);
        if (recentEvents.Count > maxCount)
        {
            recentEvents.RemoveAt(0);
        }
    }
}
