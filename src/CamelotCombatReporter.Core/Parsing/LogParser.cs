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
        @"^\[(?<timestamp>\d{2}:\d{2}:\d{2})\]\s+You use (?<style>.+?) on (?:the )?(?<target>.+?)[.!]?$",
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

    // Loot patterns

    // Item drops from mobs: "[HH:mm:ss] The bright aurora drops a crystalline orb, which you pick up."
    private static readonly Regex ItemDropPattern = new(
        @"^\[(?<timestamp>\d{2}:\d{2}:\d{2})\]\s+(?:The\s+)?(?<mob>.+?)\s+drops\s+(?<article>a|an|the)\s+(?<item>.+?),\s+which you pick up\.$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    // Currency pickup: "[HH:mm:ss] You pick up 1 gold, 48 silver and 98 copper pieces."
    // Handles various formats: "1 gold, 48 silver and 98 copper", "1 gold and 15 silver", "75 silver and 90 copper"
    private static readonly Regex CurrencyPickupPattern = new(
        @"^\[(?<timestamp>\d{2}:\d{2}:\d{2})\]\s+You pick up\s+(?:(?<gold>\d+)\s+gold)?(?:,?\s+|\s+and\s+)?(?:(?<silver>\d+)\s+silver)?(?:,?\s+|\s+and\s+)?(?:(?<copper>\d+)\s+copper)?\s*pieces?\.$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    // Item receive from NPC/source: "[HH:mm:ss] You receive the Biting Wind Eye from the biting wind!"
    private static readonly Regex ItemReceivePattern = new(
        @"^\[(?<timestamp>\d{2}:\d{2}:\d{2})\]\s+You receive\s+(?:the\s+)?(?<item>.+?)\s+from\s+(?<source>.+?)!$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    // Reward receive with quantity: "[HH:mm:ss] You received 23 Soil of Albion as your reward!"
    private static readonly Regex RewardReceivePattern = new(
        @"^\[(?<timestamp>\d{2}:\d{2}:\d{2})\]\s+You received\s+(?<qty>\d+)\s+(?<item>.+?)\s+as your reward!$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    // Currency receive (without pieces): "[HH:mm:ss] You receive 1 gold, 3 silver, 76 copper"
    private static readonly Regex CurrencyReceivePattern = new(
        @"^\[(?<timestamp>\d{2}:\d{2}:\d{2})\]\s+You receive\s+(?:(?<gold>\d+)\s+gold)?(?:,?\s*)?(?:(?<silver>\d+)\s+silver)?(?:,?\s*)?(?:(?<copper>\d+)\s+copper)?$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    // Bonus currency from outposts: "[HH:mm:ss] You find an additional 8 copper pieces thanks to your realm owning outposts!"
    private static readonly Regex BonusCurrencyOutpostPattern = new(
        @"^\[(?<timestamp>\d{2}:\d{2}:\d{2})\]\s+You find an additional\s+(?:(?<gold>\d+)\s+gold)?(?:,?\s*)?(?:(?<silver>\d+)\s+silver)?(?:\s+and\s+)?(?:(?<copper>\d+)\s+copper)?\s*pieces?\s+thanks to your realm owning outposts!$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    // Bonus currency from area: "[HH:mm:ss] You gain an additional 4 silver and 53 copper pieces for adventuring in this area!"
    private static readonly Regex BonusCurrencyAreaPattern = new(
        @"^\[(?<timestamp>\d{2}:\d{2}:\d{2})\]\s+You gain an additional\s+(?:(?<gold>\d+)\s+gold)?(?:,?\s*)?(?:(?<silver>\d+)\s+silver)?(?:\s+and\s+)?(?:(?<copper>\d+)\s+copper)?\s*pieces?\s+for adventuring in this area!$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

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

            // Loot parsing

            // Item drop from mob
            var itemDropMatch = ItemDropPattern.Match(line);
            if (itemDropMatch.Success)
            {
                var groups = itemDropMatch.Groups;
                var timestamp = TimeOnly.ParseExact(groups["timestamp"].Value, "HH:mm:ss", CultureInfo.InvariantCulture);
                var mob = groups["mob"].Value.Trim();
                var article = groups["article"].Value.ToLowerInvariant();
                var item = groups["item"].Value.Trim();

                // Skip "bag of coins" as a regular item - currency is handled separately
                if (!item.Equals("bag of coins", StringComparison.OrdinalIgnoreCase))
                {
                    var evt = new ItemDropEvent(
                        Timestamp: timestamp,
                        MobName: mob,
                        ItemName: item,
                        IsNamedItem: article == "the"
                    );
                    AddToRecentEvents(recentEvents, evt, MaxRecentEvents);
                    yield return evt;
                }
                continue;
            }

            // Currency pickup
            var currencyPickupMatch = CurrencyPickupPattern.Match(line);
            if (currencyPickupMatch.Success)
            {
                var groups = currencyPickupMatch.Groups;
                var timestamp = TimeOnly.ParseExact(groups["timestamp"].Value, "HH:mm:ss", CultureInfo.InvariantCulture);
                var gold = groups["gold"].Success ? int.Parse(groups["gold"].Value) : 0;
                var silver = groups["silver"].Success ? int.Parse(groups["silver"].Value) : 0;
                var copper = groups["copper"].Success ? int.Parse(groups["copper"].Value) : 0;

                // Try to find the mob from recent events (look for a recent death or item drop)
                string? mobName = FindRecentMobName(recentEvents);

                var evt = new CurrencyDropEvent(
                    Timestamp: timestamp,
                    MobName: mobName,
                    Gold: gold,
                    Silver: silver,
                    Copper: copper
                );
                AddToRecentEvents(recentEvents, evt, MaxRecentEvents);
                yield return evt;
                continue;
            }

            // Currency receive (alternative format without "pieces")
            var currencyReceiveMatch = CurrencyReceivePattern.Match(line);
            if (currencyReceiveMatch.Success)
            {
                var groups = currencyReceiveMatch.Groups;
                var timestamp = TimeOnly.ParseExact(groups["timestamp"].Value, "HH:mm:ss", CultureInfo.InvariantCulture);
                var gold = groups["gold"].Success ? int.Parse(groups["gold"].Value) : 0;
                var silver = groups["silver"].Success ? int.Parse(groups["silver"].Value) : 0;
                var copper = groups["copper"].Success ? int.Parse(groups["copper"].Value) : 0;

                var evt = new CurrencyDropEvent(
                    Timestamp: timestamp,
                    MobName: null,
                    Gold: gold,
                    Silver: silver,
                    Copper: copper
                );
                AddToRecentEvents(recentEvents, evt, MaxRecentEvents);
                yield return evt;
                continue;
            }

            // Item receive from NPC/source
            var itemReceiveMatch = ItemReceivePattern.Match(line);
            if (itemReceiveMatch.Success)
            {
                var groups = itemReceiveMatch.Groups;
                var timestamp = TimeOnly.ParseExact(groups["timestamp"].Value, "HH:mm:ss", CultureInfo.InvariantCulture);
                var item = groups["item"].Value.Trim();
                var source = groups["source"].Value.Trim();

                var evt = new ItemReceiveEvent(
                    Timestamp: timestamp,
                    ItemName: item,
                    SourceName: source,
                    Quantity: null
                );
                AddToRecentEvents(recentEvents, evt, MaxRecentEvents);
                yield return evt;
                continue;
            }

            // Reward receive with quantity
            var rewardReceiveMatch = RewardReceivePattern.Match(line);
            if (rewardReceiveMatch.Success)
            {
                var groups = rewardReceiveMatch.Groups;
                var timestamp = TimeOnly.ParseExact(groups["timestamp"].Value, "HH:mm:ss", CultureInfo.InvariantCulture);
                var qty = int.Parse(groups["qty"].Value);
                var item = groups["item"].Value.Trim();

                var evt = new ItemReceiveEvent(
                    Timestamp: timestamp,
                    ItemName: item,
                    SourceName: "Reward",
                    Quantity: qty
                );
                AddToRecentEvents(recentEvents, evt, MaxRecentEvents);
                yield return evt;
                continue;
            }

            // Bonus currency from outposts
            var bonusOutpostMatch = BonusCurrencyOutpostPattern.Match(line);
            if (bonusOutpostMatch.Success)
            {
                var groups = bonusOutpostMatch.Groups;
                var timestamp = TimeOnly.ParseExact(groups["timestamp"].Value, "HH:mm:ss", CultureInfo.InvariantCulture);
                var gold = groups["gold"].Success ? int.Parse(groups["gold"].Value) : 0;
                var silver = groups["silver"].Success ? int.Parse(groups["silver"].Value) : 0;
                var copper = groups["copper"].Success ? int.Parse(groups["copper"].Value) : 0;

                var evt = new BonusCurrencyEvent(
                    Timestamp: timestamp,
                    Gold: gold,
                    Silver: silver,
                    Copper: copper,
                    BonusSource: "outpost"
                );
                AddToRecentEvents(recentEvents, evt, MaxRecentEvents);
                yield return evt;
                continue;
            }

            // Bonus currency from area
            var bonusAreaMatch = BonusCurrencyAreaPattern.Match(line);
            if (bonusAreaMatch.Success)
            {
                var groups = bonusAreaMatch.Groups;
                var timestamp = TimeOnly.ParseExact(groups["timestamp"].Value, "HH:mm:ss", CultureInfo.InvariantCulture);
                var gold = groups["gold"].Success ? int.Parse(groups["gold"].Value) : 0;
                var silver = groups["silver"].Success ? int.Parse(groups["silver"].Value) : 0;
                var copper = groups["copper"].Success ? int.Parse(groups["copper"].Value) : 0;

                var evt = new BonusCurrencyEvent(
                    Timestamp: timestamp,
                    Gold: gold,
                    Silver: silver,
                    Copper: copper,
                    BonusSource: "area"
                );
                AddToRecentEvents(recentEvents, evt, MaxRecentEvents);
                yield return evt;
                continue;
            }
        }
    }

    /// <summary>
    /// Attempts to find the name of a recently killed mob from the event history.
    /// </summary>
    private static string? FindRecentMobName(List<LogEvent> recentEvents)
    {
        // Look backwards through recent events for an ItemDropEvent (currency often follows item drops)
        for (var i = recentEvents.Count - 1; i >= 0; i--)
        {
            if (recentEvents[i] is ItemDropEvent itemDrop)
            {
                return itemDrop.MobName;
            }
        }
        return null;
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
