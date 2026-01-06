using System.Globalization;
using System.Text.RegularExpressions;
using CamelotCombatReporter.Core.Models;
using CamelotCombatReporter.Core.RealmAbilities.Models;
using CamelotCombatReporter.Core.RvR;
using CamelotCombatReporter.Core.RvR.Models;

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
    // Supports: "You hit X for N damage!" and "You hit X for N (+M) damage!" and "You hit X for N points of type damage!"
    private static readonly Regex DamageDealtPattern = new(
        @"^\[(?<timestamp>\d{2}:\d{2}:\d{2})\]\s+You hit (?:the )?(?<target>.+?) for (?<amount>\d+)(?:\s*\((?<modifier>[+-]?\d+)\))?(?:\s+points of)?(?: (?<type>\w+))? damage[!.]?$",
        RegexOptions.Compiled);

    // Regex to capture damage taken by the player.
    // Supports: "X hits you for N points of type damage!" and "X hits your torso for N (+M) damage!"
    private static readonly Regex DamageTakenPattern = new(
        @"^\[(?<timestamp>\d{2}:\d{2}:\d{2})\]\s+(?<source>.+?) hits (?:you|your (?<bodypart>\w+)) for (?<amount>\d+)(?:\s*\((?<modifier>[+-]?\d+)\))?(?:\s+points of)?(?: (?<type>\w+))? damage[!.]?$",
        RegexOptions.Compiled);

    // Alternate damage taken: "You are hit for N damage."
    private static readonly Regex AlternateDamageTakenPattern = new(
        @"^\[(?<timestamp>\d{2}:\d{2}:\d{2})\]\s+You are hit for (?<amount>\d+) damage\.$",
        RegexOptions.Compiled);

    // Melee attack: "You attack X with your weapon and hit for N damage!"
    private static readonly Regex MeleeAttackPattern = new(
        @"^\[(?<timestamp>\d{2}:\d{2}:\d{2})\]\s+You attack (?:the )?(?<target>.+?) with your (?<weapon>.+?) and hit for (?<amount>\d+)(?:\s*\((?<modifier>[+-]?\d+)\))? damage[!.]?$",
        RegexOptions.Compiled);

    // Ranged attack: "You shot X with your bow and hit for N damage!"
    private static readonly Regex RangedAttackPattern = new(
        @"^\[(?<timestamp>\d{2}:\d{2}:\d{2})\]\s+You shot (?<target>.+?) with your (?<weapon>.+?) and hit for (?<amount>\d+)(?:\s*\((?<modifier>[+-]?\d+)\))? damage[!.]?$",
        RegexOptions.Compiled);

    // Critical hit: "You critical hit for an additional N damage!" or "You critical hit Target for an additional N damage! (20%)"
    private static readonly Regex CriticalHitPattern = new(
        @"^\[(?<timestamp>\d{2}:\d{2}:\d{2})\]\s+You critical hit(?: (?<target>.+?))? for an additional (?<amount>\d+) damage!(?:\s*\((?<percent>\d+)%\))?$",
        RegexOptions.Compiled);

    // Pet damage: "Your spirit warrior attacks X and hits for N damage!"
    private static readonly Regex PetDamagePattern = new(
        @"^\[(?<timestamp>\d{2}:\d{2}:\d{2})\]\s+Your (?<pet>.+?) attacks (?:the )?(?<target>.+?) and hits for (?<amount>\d+)(?:\s*\((?<modifier>[+-]?\d+)\))? damage[!.]?$",
        RegexOptions.Compiled);

    // Pet extra damage: "Your pet hits target for N extra damage!"
    private static readonly Regex PetExtraDamagePattern = new(
        @"^\[(?<timestamp>\d{2}:\d{2}:\d{2})\]\s+Your pet hits (?<target>.+?) for (?<amount>\d+) extra damage!$",
        RegexOptions.Compiled);

    // Death event: "The swamp rat dies!" or "X kills Y!"
    private static readonly Regex DeathPattern = new(
        @"^\[(?<timestamp>\d{2}:\d{2}:\d{2})\]\s+(?:The )?(?<target>.+?) dies!$",
        RegexOptions.Compiled);

    // Kill event: "The wolf sage kills the siabra mireguard!"
    private static readonly Regex KillPattern = new(
        @"^\[(?<timestamp>\d{2}:\d{2}:\d{2})\]\s+(?:The )?(?<killer>.+?) kills (?:the )?(?<target>.+?)!$",
        RegexOptions.Compiled);

    // Resist event: "The swamp rat resists the effect!"
    private static readonly Regex ResistPattern = new(
        @"^\[(?<timestamp>\d{2}:\d{2}:\d{2})\]\s+(?:The )?(?<target>.+?) resists the effect!$",
        RegexOptions.Compiled);

    // Stun applied: "The giant snowcrab is stunned!" or "The troll is stunned for 9 seconds!"
    private static readonly Regex StunAppliedPattern = new(
        @"^\[(?<timestamp>\d{2}:\d{2}:\d{2})\]\s+(?:The )?(?<target>.+?) is stunned(?:\s+for\s+(?<duration>\d+)\s+seconds?)?!$",
        RegexOptions.Compiled);

    // Stun recovered: "The giant snowcrab recovers from the stun."
    private static readonly Regex StunRecoveredPattern = new(
        @"^\[(?<timestamp>\d{2}:\d{2}:\d{2})\]\s+(?:The )?(?<target>.+?) recovers from the stun\.$",
        RegexOptions.Compiled);

    // Mez applied: "The goblin is mesmerized!" or "The goblin is mesmerized for 60 seconds!"
    private static readonly Regex MezAppliedPattern = new(
        @"^\[(?<timestamp>\d{2}:\d{2}:\d{2})\]\s+(?:The )?(?<target>.+?) is mesmerized(?:\s+for\s+(?<duration>\d+)\s+seconds?)?!$",
        RegexOptions.Compiled);

    // Mez recovered: "The goblin is no longer mesmerized."
    private static readonly Regex MezRecoveredPattern = new(
        @"^\[(?<timestamp>\d{2}:\d{2}:\d{2})\]\s+(?:The )?(?<target>.+?) is no longer mesmerized\.$",
        RegexOptions.Compiled);

    // Root applied: "The troll is rooted!" or "The troll is rooted for 15 seconds!"
    private static readonly Regex RootAppliedPattern = new(
        @"^\[(?<timestamp>\d{2}:\d{2}:\d{2})\]\s+(?:The )?(?<target>.+?) is rooted(?:\s+for\s+(?<duration>\d+)\s+seconds?)?!$",
        RegexOptions.Compiled);

    // Root recovered: "The troll is no longer rooted."
    private static readonly Regex RootRecoveredPattern = new(
        @"^\[(?<timestamp>\d{2}:\d{2}:\d{2})\]\s+(?:The )?(?<target>.+?) is no longer rooted\.$",
        RegexOptions.Compiled);

    // Snare applied: "The goblin is snared!" or "The goblin is snared for 30 seconds!"
    private static readonly Regex SnareAppliedPattern = new(
        @"^\[(?<timestamp>\d{2}:\d{2}:\d{2})\]\s+(?:The )?(?<target>.+?) is snared(?:\s+for\s+(?<duration>\d+)\s+seconds?)?!$",
        RegexOptions.Compiled);

    // Snare recovered: "The goblin is no longer snared."
    private static readonly Regex SnareRecoveredPattern = new(
        @"^\[(?<timestamp>\d{2}:\d{2}:\d{2})\]\s+(?:The )?(?<target>.+?) is no longer snared\.$",
        RegexOptions.Compiled);

    // Realm Ability patterns

    // RA activation by player: "You activate Purge!"
    // Note: "You use X on Y!" is handled by CombatStylePattern, so we only match "activate" here
    // or "use" when NOT followed by "on" (i.e., RA use without a target)
    private static readonly Regex RealmAbilityActivatePattern = new(
        @"^\[(?<timestamp>\d{2}:\d{2}:\d{2})\]\s+You activate (?<ability>.+?)!$",
        RegexOptions.Compiled);

    // RA effect damage: "Your Volcanic Pillar hits X for N damage!" or "Your Volcanic Pillar hits X for N fire damage!"
    private static readonly Regex RealmAbilityDamagePattern = new(
        @"^\[(?<timestamp>\d{2}:\d{2}:\d{2})\]\s+Your (?<ability>.+?) hits (?:the )?(?<target>.+?) for (?<amount>\d+)(?: (?<type>\w+))? damage[!.]?$",
        RegexOptions.Compiled);

    // RA healing: "Your Gift of Perizor heals your group for N hit points!" or "Your First Aid heals you for N hit points!"
    private static readonly Regex RealmAbilityHealPattern = new(
        @"^\[(?<timestamp>\d{2}:\d{2}:\d{2})\]\s+Your (?<ability>.+?) heals (?<target>.+?) for (?<amount>\d+) hit points[!.]?$",
        RegexOptions.Compiled);

    // RA ready: "Purge is ready to use." or "Volcanic Pillar is now ready!"
    private static readonly Regex RealmAbilityReadyPattern = new(
        @"^\[(?<timestamp>\d{2}:\d{2}:\d{2})\]\s+(?<ability>.+?) is (?:ready to use|now ready)[.!]?$",
        RegexOptions.Compiled);

    // Enemy RA activation: "Enemyname activates Purge!"
    // Note: We only match "activates" to avoid conflicts with combat style messages
    private static readonly Regex EnemyRealmAbilityPattern = new(
        @"^\[(?<timestamp>\d{2}:\d{2}:\d{2})\]\s+(?<source>.+?) activates (?<ability>.+?)!$",
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

    // Combat mode enter: "[HH:mm:ss] You enter combat mode and target [the siabra mireguard]" or "You enter combat mode but have no target!"
    private static readonly Regex CombatModeEnterPattern = new(
        @"^\[(?<timestamp>\d{2}:\d{2}:\d{2})\]\s+You enter combat mode(?:\s+and target \[(?:the )?(?<target>.+?)\]|\s+but have no target)?[!.]?$",
        RegexOptions.Compiled);

    // Sit down: "[HH:mm:ss] You sit down.  Type '/stand' or move to stand up."
    private static readonly Regex SitDownPattern = new(
        @"^\[(?<timestamp>\d{2}:\d{2}:\d{2})\]\s+You sit down\.",
        RegexOptions.Compiled);

    // Stand up: "[HH:mm:ss] You stand up."
    private static readonly Regex StandUpPattern = new(
        @"^\[(?<timestamp>\d{2}:\d{2}:\d{2})\]\s+You stand up\.$",
        RegexOptions.Compiled);

    // Chat log boundary: "*** Chat Log Opened: Wed Dec 20 08:26:35 2017" or "*** Chat Log Closed: ..."
    private static readonly Regex ChatLogBoundaryPattern = new(
        @"^\*\*\*\s+Chat Log (?<action>Opened|Closed):\s+(?<datetime>.+)$",
        RegexOptions.Compiled);

    // ==================== RVR / SIEGE PATTERNS ====================

    // Door damage: "[HH:mm:ss] You hit the Outer Door of Caer Benowyc for 234 damage!"
    // Also matches: "The ram hits the Outer Door..." or "Player hits the Inner Gate..."
    private static readonly Regex DoorDamagePattern = new(
        @"^\[(?<timestamp>\d{2}:\d{2}:\d{2})\]\s+(?<source>.+?) hits? the (?<door>(?:Outer|Inner) (?:Door|Gate)) of (?<keep>.+?) for (?<amount>\d+)(?: (?<type>\w+))? damage[!.]?$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    // Door destroyed: "[HH:mm:ss] The Outer Door of Caer Benowyc has been destroyed!"
    private static readonly Regex DoorDestroyedPattern = new(
        @"^\[(?<timestamp>\d{2}:\d{2}:\d{2})\]\s+The (?<door>(?:Outer|Inner) (?:Door|Gate)) of (?<keep>.+?) has been destroyed!$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    // Keep captured: "[HH:mm:ss] Caer Benowyc has been captured by Midgard!" or with guild claim
    private static readonly Regex KeepCapturedPattern = new(
        @"^\[(?<timestamp>\d{2}:\d{2}:\d{2})\]\s+(?<keep>.+?) has been captured by (?<realm>Albion|Midgard|Hibernia)(?:\s*\((?<guild>.+?)\))?!$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    // Lord/Lady killed: "[HH:mm:ss] Lord Benowyc has been slain!" or "Lady X dies!"
    private static readonly Regex LordKilledPattern = new(
        @"^\[(?<timestamp>\d{2}:\d{2}:\d{2})\]\s+(?:Lord|Lady) (?<name>.+?) (?:has been slain|dies)!$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    // Guard killed: "[HH:mm:ss] You killed the Keep Guard!" or "Player kills the Tower Guard!"
    private static readonly Regex GuardKillPattern = new(
        @"^\[(?<timestamp>\d{2}:\d{2}:\d{2})\]\s+(?<killer>.+?) (?:killed|kills) the (?<guard>Keep Guard|Tower Guard|Castle Guard)!$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    // Tower captured: "[HH:mm:ss] Dun Crauchon Tower 1 has been captured by Albion!"
    private static readonly Regex TowerCapturedPattern = new(
        @"^\[(?<timestamp>\d{2}:\d{2}:\d{2})\]\s+(?<tower>.+?(?:Tower|Keep Tower)(?: \d+)?) has been captured by (?<realm>Albion|Midgard|Hibernia)!$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    // Siege weapon deployed: "[HH:mm:ss] You deploy a ram!" or "Player deploys a trebuchet!"
    private static readonly Regex SiegeDeployPattern = new(
        @"^\[(?<timestamp>\d{2}:\d{2}:\d{2})\]\s+(?<player>.+?) deploy(?:s|ed)? (?:a |an )?(?<weapon>ram|trebuchet|ballista|catapult|boiling oil)[!.]?$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    // Siege weapon destroyed: "[HH:mm:ss] The ram has been destroyed!"
    private static readonly Regex SiegeDestroyedPattern = new(
        @"^\[(?<timestamp>\d{2}:\d{2}:\d{2})\]\s+The (?<weapon>ram|trebuchet|ballista|catapult) has been destroyed!$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    // ==================== RELIC PATTERNS ====================

    // Relic picked up: "[HH:mm:ss] PlayerName has picked up Thor's Hammer!"
    private static readonly Regex RelicPickupPattern = new(
        @"^\[(?<timestamp>\d{2}:\d{2}:\d{2})\]\s+(?<player>.+?) has picked up (?<relic>.+?)!$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    // Relic dropped: "[HH:mm:ss] PlayerName has dropped Thor's Hammer!"
    private static readonly Regex RelicDropPattern = new(
        @"^\[(?<timestamp>\d{2}:\d{2}:\d{2})\]\s+(?<player>.+?) has dropped (?<relic>.+?)!$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    // Relic captured: "[HH:mm:ss] Midgard has captured Merlin's Staff!"
    private static readonly Regex RelicCapturedPattern = new(
        @"^\[(?<timestamp>\d{2}:\d{2}:\d{2})\]\s+(?<realm>Albion|Midgard|Hibernia) has captured (?<relic>.+?)!$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    // Relic returned: "[HH:mm:ss] Thor's Hammer has been returned to Midgard!"
    private static readonly Regex RelicReturnedPattern = new(
        @"^\[(?<timestamp>\d{2}:\d{2}:\d{2})\]\s+(?<relic>.+?) has been returned to (?<realm>Albion|Midgard|Hibernia)!$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    // ==================== ZONE / BATTLEGROUND PATTERNS ====================

    // Zone entry: "[HH:mm:ss] You have entered Thidranki!" or "You enter Emain Macha."
    private static readonly Regex ZoneEnterPattern = new(
        @"^\[(?<timestamp>\d{2}:\d{2}:\d{2})\]\s+You (?:have entered|enter) (?<zone>.+?)[!.]?$",
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

            // Melee attack pattern: "You attack X with your weapon and hit for N damage!"
            var meleeMatch = MeleeAttackPattern.Match(line);
            if (meleeMatch.Success)
            {
                var groups = meleeMatch.Groups;
                var timestamp = TimeOnly.ParseExact(groups["timestamp"].Value, "HH:mm:ss", CultureInfo.InvariantCulture);
                var target = groups["target"].Value.Trim();
                var weapon = groups["weapon"].Value.Trim();
                var amount = int.Parse(groups["amount"].Value);
                int? modifier = groups["modifier"].Success ? int.Parse(groups["modifier"].Value) : null;

                var evt = new DamageEvent(
                    Timestamp: timestamp,
                    Source: "You",
                    Target: target,
                    DamageAmount: amount,
                    DamageType: "Unknown",
                    Modifier: modifier,
                    WeaponUsed: weapon
                );
                AddToRecentEvents(recentEvents, evt, MaxRecentEvents);
                yield return evt;
                continue;
            }

            // Ranged attack pattern: "You shot X with your bow and hit for N damage!"
            var rangedMatch = RangedAttackPattern.Match(line);
            if (rangedMatch.Success)
            {
                var groups = rangedMatch.Groups;
                var timestamp = TimeOnly.ParseExact(groups["timestamp"].Value, "HH:mm:ss", CultureInfo.InvariantCulture);
                var target = groups["target"].Value.Trim();
                var weapon = groups["weapon"].Value.Trim();
                var amount = int.Parse(groups["amount"].Value);
                int? modifier = groups["modifier"].Success ? int.Parse(groups["modifier"].Value) : null;

                var evt = new DamageEvent(
                    Timestamp: timestamp,
                    Source: "You",
                    Target: target,
                    DamageAmount: amount,
                    DamageType: "Unknown",
                    Modifier: modifier,
                    WeaponUsed: weapon
                );
                AddToRecentEvents(recentEvents, evt, MaxRecentEvents);
                yield return evt;
                continue;
            }

            var dealtMatch = DamageDealtPattern.Match(line);
            if (dealtMatch.Success)
            {
                var groups = dealtMatch.Groups;

                var timestamp = TimeOnly.ParseExact(groups["timestamp"].Value, "HH:mm:ss", CultureInfo.InvariantCulture);
                var target = groups["target"].Value.Trim();
                var amount = int.Parse(groups["amount"].Value);
                var damageType = groups["type"].Success ? groups["type"].Value.Trim() : "Unknown";
                int? modifier = groups["modifier"].Success ? int.Parse(groups["modifier"].Value) : null;

                var evt = new DamageEvent(
                    Timestamp: timestamp,
                    Source: "You",
                    Target: target,
                    DamageAmount: amount,
                    DamageType: damageType,
                    Modifier: modifier
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
                int? modifier = groups["modifier"].Success ? int.Parse(groups["modifier"].Value) : null;
                string? bodyPart = groups["bodypart"].Success ? groups["bodypart"].Value.Trim() : null;

                var evt = new DamageEvent(
                    Timestamp: timestamp,
                    Source: source,
                    Target: "You",
                    DamageAmount: amount,
                    DamageType: damageType,
                    Modifier: modifier,
                    BodyPart: bodyPart
                );
                AddToRecentEvents(recentEvents, evt, MaxRecentEvents);
                yield return evt;
                continue;
            }

            // Alternate damage taken: "You are hit for N damage."
            var altTakenMatch = AlternateDamageTakenPattern.Match(line);
            if (altTakenMatch.Success)
            {
                var groups = altTakenMatch.Groups;
                var timestamp = TimeOnly.ParseExact(groups["timestamp"].Value, "HH:mm:ss", CultureInfo.InvariantCulture);
                var amount = int.Parse(groups["amount"].Value);

                var evt = new DamageEvent(
                    Timestamp: timestamp,
                    Source: "Unknown",
                    Target: "You",
                    DamageAmount: amount,
                    DamageType: "Unknown"
                );
                AddToRecentEvents(recentEvents, evt, MaxRecentEvents);
                yield return evt;
                continue;
            }

            // Critical hit pattern
            var critMatch = CriticalHitPattern.Match(line);
            if (critMatch.Success)
            {
                var groups = critMatch.Groups;
                var timestamp = TimeOnly.ParseExact(groups["timestamp"].Value, "HH:mm:ss", CultureInfo.InvariantCulture);
                var amount = int.Parse(groups["amount"].Value);
                string? target = groups["target"].Success ? groups["target"].Value.Trim() : null;
                int? percent = groups["percent"].Success ? int.Parse(groups["percent"].Value) : null;

                var evt = new CriticalHitEvent(
                    Timestamp: timestamp,
                    Target: target,
                    DamageAmount: amount,
                    CritPercent: percent
                );
                AddToRecentEvents(recentEvents, evt, MaxRecentEvents);
                yield return evt;
                continue;
            }

            // Pet damage pattern
            var petDamageMatch = PetDamagePattern.Match(line);
            if (petDamageMatch.Success)
            {
                var groups = petDamageMatch.Groups;
                var timestamp = TimeOnly.ParseExact(groups["timestamp"].Value, "HH:mm:ss", CultureInfo.InvariantCulture);
                var petName = groups["pet"].Value.Trim();
                var target = groups["target"].Value.Trim();
                var amount = int.Parse(groups["amount"].Value);
                int? modifier = groups["modifier"].Success ? int.Parse(groups["modifier"].Value) : null;

                var evt = new PetDamageEvent(
                    Timestamp: timestamp,
                    PetName: petName,
                    Target: target,
                    DamageAmount: amount,
                    Modifier: modifier
                );
                AddToRecentEvents(recentEvents, evt, MaxRecentEvents);
                yield return evt;
                continue;
            }

            // Pet extra damage pattern
            var petExtraMatch = PetExtraDamagePattern.Match(line);
            if (petExtraMatch.Success)
            {
                var groups = petExtraMatch.Groups;
                var timestamp = TimeOnly.ParseExact(groups["timestamp"].Value, "HH:mm:ss", CultureInfo.InvariantCulture);
                var target = groups["target"].Value.Trim();
                var amount = int.Parse(groups["amount"].Value);

                var evt = new PetDamageEvent(
                    Timestamp: timestamp,
                    PetName: "pet",
                    Target: target,
                    DamageAmount: amount
                );
                AddToRecentEvents(recentEvents, evt, MaxRecentEvents);
                yield return evt;
                continue;
            }

            // Kill event: "The wolf sage kills the siabra mireguard!"
            var killMatch = KillPattern.Match(line);
            if (killMatch.Success)
            {
                var groups = killMatch.Groups;
                var timestamp = TimeOnly.ParseExact(groups["timestamp"].Value, "HH:mm:ss", CultureInfo.InvariantCulture);
                var killer = groups["killer"].Value.Trim();
                var target = groups["target"].Value.Trim();

                var evt = new DeathEvent(
                    Timestamp: timestamp,
                    Target: target,
                    Killer: killer
                );
                AddToRecentEvents(recentEvents, evt, MaxRecentEvents);
                yield return evt;
                continue;
            }

            // Death pattern: "The swamp rat dies!"
            var deathMatch = DeathPattern.Match(line);
            if (deathMatch.Success)
            {
                var groups = deathMatch.Groups;
                var timestamp = TimeOnly.ParseExact(groups["timestamp"].Value, "HH:mm:ss", CultureInfo.InvariantCulture);
                var target = groups["target"].Value.Trim();

                var evt = new DeathEvent(
                    Timestamp: timestamp,
                    Target: target
                );
                AddToRecentEvents(recentEvents, evt, MaxRecentEvents);
                yield return evt;
                continue;
            }

            // Resist pattern
            var resistMatch = ResistPattern.Match(line);
            if (resistMatch.Success)
            {
                var groups = resistMatch.Groups;
                var timestamp = TimeOnly.ParseExact(groups["timestamp"].Value, "HH:mm:ss", CultureInfo.InvariantCulture);
                var target = groups["target"].Value.Trim();

                var evt = new ResistEvent(
                    Timestamp: timestamp,
                    Target: target
                );
                AddToRecentEvents(recentEvents, evt, MaxRecentEvents);
                yield return evt;
                continue;
            }

            // Stun applied pattern
            var stunAppliedMatch = StunAppliedPattern.Match(line);
            if (stunAppliedMatch.Success)
            {
                var groups = stunAppliedMatch.Groups;
                var timestamp = TimeOnly.ParseExact(groups["timestamp"].Value, "HH:mm:ss", CultureInfo.InvariantCulture);
                var target = groups["target"].Value.Trim();
                int? duration = groups["duration"].Success ? int.Parse(groups["duration"].Value) : null;

                var evt = new CrowdControlEvent(
                    Timestamp: timestamp,
                    Target: target,
                    EffectType: "stun",
                    IsApplied: true,
                    Duration: duration
                );
                AddToRecentEvents(recentEvents, evt, MaxRecentEvents);
                yield return evt;
                continue;
            }

            // Stun recovered pattern
            var stunRecoveredMatch = StunRecoveredPattern.Match(line);
            if (stunRecoveredMatch.Success)
            {
                var groups = stunRecoveredMatch.Groups;
                var timestamp = TimeOnly.ParseExact(groups["timestamp"].Value, "HH:mm:ss", CultureInfo.InvariantCulture);
                var target = groups["target"].Value.Trim();

                var evt = new CrowdControlEvent(
                    Timestamp: timestamp,
                    Target: target,
                    EffectType: "stun",
                    IsApplied: false
                );
                AddToRecentEvents(recentEvents, evt, MaxRecentEvents);
                yield return evt;
                continue;
            }

            // Mez applied pattern
            var mezAppliedMatch = MezAppliedPattern.Match(line);
            if (mezAppliedMatch.Success)
            {
                var groups = mezAppliedMatch.Groups;
                var timestamp = TimeOnly.ParseExact(groups["timestamp"].Value, "HH:mm:ss", CultureInfo.InvariantCulture);
                var target = groups["target"].Value.Trim();
                int? duration = groups["duration"].Success ? int.Parse(groups["duration"].Value) : null;

                var evt = new CrowdControlEvent(
                    Timestamp: timestamp,
                    Target: target,
                    EffectType: "mez",
                    IsApplied: true,
                    Duration: duration
                );
                AddToRecentEvents(recentEvents, evt, MaxRecentEvents);
                yield return evt;
                continue;
            }

            // Mez recovered pattern
            var mezRecoveredMatch = MezRecoveredPattern.Match(line);
            if (mezRecoveredMatch.Success)
            {
                var groups = mezRecoveredMatch.Groups;
                var timestamp = TimeOnly.ParseExact(groups["timestamp"].Value, "HH:mm:ss", CultureInfo.InvariantCulture);
                var target = groups["target"].Value.Trim();

                var evt = new CrowdControlEvent(
                    Timestamp: timestamp,
                    Target: target,
                    EffectType: "mez",
                    IsApplied: false
                );
                AddToRecentEvents(recentEvents, evt, MaxRecentEvents);
                yield return evt;
                continue;
            }

            // Root applied pattern
            var rootAppliedMatch = RootAppliedPattern.Match(line);
            if (rootAppliedMatch.Success)
            {
                var groups = rootAppliedMatch.Groups;
                var timestamp = TimeOnly.ParseExact(groups["timestamp"].Value, "HH:mm:ss", CultureInfo.InvariantCulture);
                var target = groups["target"].Value.Trim();
                int? duration = groups["duration"].Success ? int.Parse(groups["duration"].Value) : null;

                var evt = new CrowdControlEvent(
                    Timestamp: timestamp,
                    Target: target,
                    EffectType: "root",
                    IsApplied: true,
                    Duration: duration
                );
                AddToRecentEvents(recentEvents, evt, MaxRecentEvents);
                yield return evt;
                continue;
            }

            // Root recovered pattern
            var rootRecoveredMatch = RootRecoveredPattern.Match(line);
            if (rootRecoveredMatch.Success)
            {
                var groups = rootRecoveredMatch.Groups;
                var timestamp = TimeOnly.ParseExact(groups["timestamp"].Value, "HH:mm:ss", CultureInfo.InvariantCulture);
                var target = groups["target"].Value.Trim();

                var evt = new CrowdControlEvent(
                    Timestamp: timestamp,
                    Target: target,
                    EffectType: "root",
                    IsApplied: false
                );
                AddToRecentEvents(recentEvents, evt, MaxRecentEvents);
                yield return evt;
                continue;
            }

            // Snare applied pattern
            var snareAppliedMatch = SnareAppliedPattern.Match(line);
            if (snareAppliedMatch.Success)
            {
                var groups = snareAppliedMatch.Groups;
                var timestamp = TimeOnly.ParseExact(groups["timestamp"].Value, "HH:mm:ss", CultureInfo.InvariantCulture);
                var target = groups["target"].Value.Trim();
                int? duration = groups["duration"].Success ? int.Parse(groups["duration"].Value) : null;

                var evt = new CrowdControlEvent(
                    Timestamp: timestamp,
                    Target: target,
                    EffectType: "snare",
                    IsApplied: true,
                    Duration: duration
                );
                AddToRecentEvents(recentEvents, evt, MaxRecentEvents);
                yield return evt;
                continue;
            }

            // Snare recovered pattern
            var snareRecoveredMatch = SnareRecoveredPattern.Match(line);
            if (snareRecoveredMatch.Success)
            {
                var groups = snareRecoveredMatch.Groups;
                var timestamp = TimeOnly.ParseExact(groups["timestamp"].Value, "HH:mm:ss", CultureInfo.InvariantCulture);
                var target = groups["target"].Value.Trim();

                var evt = new CrowdControlEvent(
                    Timestamp: timestamp,
                    Target: target,
                    EffectType: "snare",
                    IsApplied: false
                );
                AddToRecentEvents(recentEvents, evt, MaxRecentEvents);
                yield return evt;
                continue;
            }

            // Realm Ability activation by player: "You activate Purge!"
            var raActivateMatch = RealmAbilityActivatePattern.Match(line);
            if (raActivateMatch.Success)
            {
                var groups = raActivateMatch.Groups;
                var timestamp = TimeOnly.ParseExact(groups["timestamp"].Value, "HH:mm:ss", CultureInfo.InvariantCulture);
                var ability = groups["ability"].Value.Trim();

                var evt = new RealmAbilityEvent(
                    Timestamp: timestamp,
                    AbilityName: ability,
                    SourceName: "You",
                    IsActivation: true
                );
                AddToRecentEvents(recentEvents, evt, MaxRecentEvents);
                yield return evt;
                continue;
            }

            // RA damage effect: "Your Volcanic Pillar hits X for N damage!"
            var raDamageMatch = RealmAbilityDamagePattern.Match(line);
            if (raDamageMatch.Success)
            {
                var groups = raDamageMatch.Groups;
                var timestamp = TimeOnly.ParseExact(groups["timestamp"].Value, "HH:mm:ss", CultureInfo.InvariantCulture);
                var ability = groups["ability"].Value.Trim();
                var target = groups["target"].Value.Trim();
                var amount = int.Parse(groups["amount"].Value);
                var damageType = groups["type"].Success ? groups["type"].Value.Trim() : null;

                var evt = new RealmAbilityEvent(
                    Timestamp: timestamp,
                    AbilityName: ability,
                    SourceName: "You",
                    TargetName: target,
                    EffectValue: amount,
                    EffectType: damageType ?? "damage",
                    IsActivation: false
                );
                AddToRecentEvents(recentEvents, evt, MaxRecentEvents);
                yield return evt;
                continue;
            }

            // RA healing: "Your Gift of Perizor heals your group for N hit points!"
            var raHealMatch = RealmAbilityHealPattern.Match(line);
            if (raHealMatch.Success)
            {
                var groups = raHealMatch.Groups;
                var timestamp = TimeOnly.ParseExact(groups["timestamp"].Value, "HH:mm:ss", CultureInfo.InvariantCulture);
                var ability = groups["ability"].Value.Trim();
                var target = groups["target"].Value.Trim();
                var amount = int.Parse(groups["amount"].Value);

                var evt = new RealmAbilityEvent(
                    Timestamp: timestamp,
                    AbilityName: ability,
                    SourceName: "You",
                    TargetName: target,
                    EffectValue: amount,
                    EffectType: "healing",
                    IsActivation: false
                );
                AddToRecentEvents(recentEvents, evt, MaxRecentEvents);
                yield return evt;
                continue;
            }

            // RA ready notification: "Purge is ready to use."
            var raReadyMatch = RealmAbilityReadyPattern.Match(line);
            if (raReadyMatch.Success)
            {
                var groups = raReadyMatch.Groups;
                var timestamp = TimeOnly.ParseExact(groups["timestamp"].Value, "HH:mm:ss", CultureInfo.InvariantCulture);
                var ability = groups["ability"].Value.Trim();

                var evt = new RealmAbilityReadyEvent(
                    Timestamp: timestamp,
                    AbilityName: ability
                );
                AddToRecentEvents(recentEvents, evt, MaxRecentEvents);
                yield return evt;
                continue;
            }

            // Enemy RA activation: "Enemyname activates Purge!"
            var enemyRaMatch = EnemyRealmAbilityPattern.Match(line);
            if (enemyRaMatch.Success)
            {
                var groups = enemyRaMatch.Groups;
                var timestamp = TimeOnly.ParseExact(groups["timestamp"].Value, "HH:mm:ss", CultureInfo.InvariantCulture);
                var source = groups["source"].Value.Trim();
                var ability = groups["ability"].Value.Trim();

                // Skip if source is "You" (already handled by player pattern)
                if (!source.Equals("You", StringComparison.OrdinalIgnoreCase))
                {
                    var evt = new RealmAbilityEvent(
                        Timestamp: timestamp,
                        AbilityName: ability,
                        SourceName: source,
                        IsActivation: true
                    );
                    AddToRecentEvents(recentEvents, evt, MaxRecentEvents);
                    yield return evt;
                    continue;
                }
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

            // Combat mode enter
            var combatEnterMatch = CombatModeEnterPattern.Match(line);
            if (combatEnterMatch.Success)
            {
                var groups = combatEnterMatch.Groups;
                var timestamp = TimeOnly.ParseExact(groups["timestamp"].Value, "HH:mm:ss", CultureInfo.InvariantCulture);
                string? target = groups["target"].Success ? groups["target"].Value.Trim() : null;

                var evt = new CombatModeEnterEvent(
                    Timestamp: timestamp,
                    TargetName: target
                );
                AddToRecentEvents(recentEvents, evt, MaxRecentEvents);
                yield return evt;
                continue;
            }

            // Sit down (rest start)
            var sitMatch = SitDownPattern.Match(line);
            if (sitMatch.Success)
            {
                var groups = sitMatch.Groups;
                var timestamp = TimeOnly.ParseExact(groups["timestamp"].Value, "HH:mm:ss", CultureInfo.InvariantCulture);

                var evt = new RestStartEvent(Timestamp: timestamp);
                AddToRecentEvents(recentEvents, evt, MaxRecentEvents);
                yield return evt;
                continue;
            }

            // Stand up (rest end)
            var standMatch = StandUpPattern.Match(line);
            if (standMatch.Success)
            {
                var groups = standMatch.Groups;
                var timestamp = TimeOnly.ParseExact(groups["timestamp"].Value, "HH:mm:ss", CultureInfo.InvariantCulture);

                var evt = new RestEndEvent(Timestamp: timestamp);
                AddToRecentEvents(recentEvents, evt, MaxRecentEvents);
                yield return evt;
                continue;
            }

            // Chat log boundary (no timestamp in line, uses log datetime)
            var logBoundaryMatch = ChatLogBoundaryPattern.Match(line);
            if (logBoundaryMatch.Success)
            {
                var groups = logBoundaryMatch.Groups;
                var isOpened = groups["action"].Value == "Opened";
                var dateStr = groups["datetime"].Value.Trim();

                // Parse datetime like "Wed Dec 20 08:26:35 2017"
                if (DateTime.TryParse(dateStr, CultureInfo.InvariantCulture, DateTimeStyles.None, out var logDateTime))
                {
                    var timestamp = TimeOnly.FromDateTime(logDateTime);
                    var evt = new ChatLogBoundaryEvent(
                        Timestamp: timestamp,
                        IsOpened: isOpened,
                        LogDateTime: logDateTime
                    );
                    AddToRecentEvents(recentEvents, evt, MaxRecentEvents);
                    yield return evt;
                }
                continue;
            }

            // ==================== RVR / SIEGE PARSING ====================

            // Door damage pattern
            var doorDamageMatch = DoorDamagePattern.Match(line);
            if (doorDamageMatch.Success)
            {
                var groups = doorDamageMatch.Groups;
                var timestamp = TimeOnly.ParseExact(groups["timestamp"].Value, "HH:mm:ss", CultureInfo.InvariantCulture);
                var source = groups["source"].Value.Trim();
                var door = groups["door"].Value.Trim();
                var keep = groups["keep"].Value.Trim();
                var amount = int.Parse(groups["amount"].Value);

                var evt = new DoorDamageEvent(
                    Timestamp: timestamp,
                    KeepName: keep,
                    DoorName: door,
                    DamageAmount: amount,
                    Source: source,
                    IsDestroyed: false
                );
                AddToRecentEvents(recentEvents, evt, MaxRecentEvents);
                yield return evt;
                continue;
            }

            // Door destroyed pattern
            var doorDestroyedMatch = DoorDestroyedPattern.Match(line);
            if (doorDestroyedMatch.Success)
            {
                var groups = doorDestroyedMatch.Groups;
                var timestamp = TimeOnly.ParseExact(groups["timestamp"].Value, "HH:mm:ss", CultureInfo.InvariantCulture);
                var door = groups["door"].Value.Trim();
                var keep = groups["keep"].Value.Trim();

                var evt = new DoorDamageEvent(
                    Timestamp: timestamp,
                    KeepName: keep,
                    DoorName: door,
                    DamageAmount: 0,
                    Source: "Unknown",
                    IsDestroyed: true
                );
                AddToRecentEvents(recentEvents, evt, MaxRecentEvents);
                yield return evt;
                continue;
            }

            // Keep captured pattern
            var keepCapturedMatch = KeepCapturedPattern.Match(line);
            if (keepCapturedMatch.Success)
            {
                var groups = keepCapturedMatch.Groups;
                var timestamp = TimeOnly.ParseExact(groups["timestamp"].Value, "HH:mm:ss", CultureInfo.InvariantCulture);
                var keep = groups["keep"].Value.Trim();
                var realmStr = groups["realm"].Value.Trim();
                var guild = groups["guild"].Success ? groups["guild"].Value.Trim() : null;

                var newOwner = ParseRealm(realmStr);

                var evt = new KeepCapturedEvent(
                    Timestamp: timestamp,
                    KeepName: keep,
                    NewOwner: newOwner,
                    PreviousOwner: null,
                    ClaimingGuild: guild
                );
                AddToRecentEvents(recentEvents, evt, MaxRecentEvents);
                yield return evt;
                continue;
            }

            // Lord killed pattern
            var lordKilledMatch = LordKilledPattern.Match(line);
            if (lordKilledMatch.Success)
            {
                var groups = lordKilledMatch.Groups;
                var timestamp = TimeOnly.ParseExact(groups["timestamp"].Value, "HH:mm:ss", CultureInfo.InvariantCulture);
                var name = groups["name"].Value.Trim();

                // Try to find the keep from the lord name or recent events
                var keepName = ExtractKeepFromLordName(name) ?? "Unknown Keep";

                var evt = new GuardKillEvent(
                    Timestamp: timestamp,
                    KeepName: keepName,
                    GuardName: $"Lord {name}",
                    Killer: "Unknown",
                    IsLordKill: true
                );
                AddToRecentEvents(recentEvents, evt, MaxRecentEvents);
                yield return evt;
                continue;
            }

            // Guard killed pattern
            var guardKillMatch = GuardKillPattern.Match(line);
            if (guardKillMatch.Success)
            {
                var groups = guardKillMatch.Groups;
                var timestamp = TimeOnly.ParseExact(groups["timestamp"].Value, "HH:mm:ss", CultureInfo.InvariantCulture);
                var killer = groups["killer"].Value.Trim();
                var guard = groups["guard"].Value.Trim();

                // Try to find keep context from recent events
                var keepName = FindRecentKeepName(recentEvents) ?? "Unknown Keep";

                var evt = new GuardKillEvent(
                    Timestamp: timestamp,
                    KeepName: keepName,
                    GuardName: guard,
                    Killer: killer,
                    IsLordKill: false
                );
                AddToRecentEvents(recentEvents, evt, MaxRecentEvents);
                yield return evt;
                continue;
            }

            // Tower captured pattern
            var towerCapturedMatch = TowerCapturedPattern.Match(line);
            if (towerCapturedMatch.Success)
            {
                var groups = towerCapturedMatch.Groups;
                var timestamp = TimeOnly.ParseExact(groups["timestamp"].Value, "HH:mm:ss", CultureInfo.InvariantCulture);
                var tower = groups["tower"].Value.Trim();
                var realmStr = groups["realm"].Value.Trim();

                var newOwner = ParseRealm(realmStr);

                var evt = new TowerCapturedEvent(
                    Timestamp: timestamp,
                    TowerName: tower,
                    NewOwner: newOwner
                );
                AddToRecentEvents(recentEvents, evt, MaxRecentEvents);
                yield return evt;
                continue;
            }

            // Siege weapon deployed pattern
            var siegeDeployMatch = SiegeDeployPattern.Match(line);
            if (siegeDeployMatch.Success)
            {
                var groups = siegeDeployMatch.Groups;
                var timestamp = TimeOnly.ParseExact(groups["timestamp"].Value, "HH:mm:ss", CultureInfo.InvariantCulture);
                var player = groups["player"].Value.Trim();
                var weaponStr = groups["weapon"].Value.Trim();

                var weaponType = ParseSiegeWeaponType(weaponStr);
                var keepName = FindRecentKeepName(recentEvents) ?? "Unknown Keep";

                var evt = new SiegeWeaponEvent(
                    Timestamp: timestamp,
                    KeepName: keepName,
                    WeaponType: weaponType,
                    PlayerName: player,
                    IsDeployed: true
                );
                AddToRecentEvents(recentEvents, evt, MaxRecentEvents);
                yield return evt;
                continue;
            }

            // Siege weapon destroyed pattern
            var siegeDestroyedMatch = SiegeDestroyedPattern.Match(line);
            if (siegeDestroyedMatch.Success)
            {
                var groups = siegeDestroyedMatch.Groups;
                var timestamp = TimeOnly.ParseExact(groups["timestamp"].Value, "HH:mm:ss", CultureInfo.InvariantCulture);
                var weaponStr = groups["weapon"].Value.Trim();

                var weaponType = ParseSiegeWeaponType(weaponStr);
                var keepName = FindRecentKeepName(recentEvents) ?? "Unknown Keep";

                var evt = new SiegeWeaponEvent(
                    Timestamp: timestamp,
                    KeepName: keepName,
                    WeaponType: weaponType,
                    PlayerName: "Unknown",
                    IsDeployed: false
                );
                AddToRecentEvents(recentEvents, evt, MaxRecentEvents);
                yield return evt;
                continue;
            }

            // ==================== RELIC PARSING ====================

            // Relic pickup pattern
            var relicPickupMatch = RelicPickupPattern.Match(line);
            if (relicPickupMatch.Success)
            {
                var groups = relicPickupMatch.Groups;
                var timestamp = TimeOnly.ParseExact(groups["timestamp"].Value, "HH:mm:ss", CultureInfo.InvariantCulture);
                var player = groups["player"].Value.Trim();
                var relicName = groups["relic"].Value.Trim();

                // Look up relic info
                var relicInfo = RelicDatabase.GetByName(relicName);
                var relicType = relicInfo?.Type ?? RelicType.Strength;
                var originRealm = relicInfo?.HomeRealm ?? Realm.Albion;

                var evt = new RelicPickupEvent(
                    Timestamp: timestamp,
                    RelicName: relicName,
                    Type: relicType,
                    CarrierName: player,
                    OriginRealm: originRealm
                );
                AddToRecentEvents(recentEvents, evt, MaxRecentEvents);
                yield return evt;
                continue;
            }

            // Relic dropped pattern
            var relicDropMatch = RelicDropPattern.Match(line);
            if (relicDropMatch.Success)
            {
                var groups = relicDropMatch.Groups;
                var timestamp = TimeOnly.ParseExact(groups["timestamp"].Value, "HH:mm:ss", CultureInfo.InvariantCulture);
                var player = groups["player"].Value.Trim();
                var relicName = groups["relic"].Value.Trim();

                var relicInfo = RelicDatabase.GetByName(relicName);
                var relicType = relicInfo?.Type ?? RelicType.Strength;

                var evt = new RelicDropEvent(
                    Timestamp: timestamp,
                    RelicName: relicName,
                    Type: relicType,
                    CarrierName: player,
                    KillerName: null
                );
                AddToRecentEvents(recentEvents, evt, MaxRecentEvents);
                yield return evt;
                continue;
            }

            // Relic captured pattern
            var relicCapturedMatch = RelicCapturedPattern.Match(line);
            if (relicCapturedMatch.Success)
            {
                var groups = relicCapturedMatch.Groups;
                var timestamp = TimeOnly.ParseExact(groups["timestamp"].Value, "HH:mm:ss", CultureInfo.InvariantCulture);
                var realmStr = groups["realm"].Value.Trim();
                var relicName = groups["relic"].Value.Trim();

                var capturingRealm = ParseRealm(realmStr);
                var relicInfo = RelicDatabase.GetByName(relicName);
                var relicType = relicInfo?.Type ?? RelicType.Strength;
                var originRealm = relicInfo?.HomeRealm ?? Realm.Albion;

                var evt = new RelicCapturedEvent(
                    Timestamp: timestamp,
                    RelicName: relicName,
                    Type: relicType,
                    CapturingRealm: capturingRealm,
                    OriginRealm: originRealm
                );
                AddToRecentEvents(recentEvents, evt, MaxRecentEvents);
                yield return evt;
                continue;
            }

            // Relic returned pattern
            var relicReturnedMatch = RelicReturnedPattern.Match(line);
            if (relicReturnedMatch.Success)
            {
                var groups = relicReturnedMatch.Groups;
                var timestamp = TimeOnly.ParseExact(groups["timestamp"].Value, "HH:mm:ss", CultureInfo.InvariantCulture);
                var relicName = groups["relic"].Value.Trim();
                var realmStr = groups["realm"].Value.Trim();

                var homeRealm = ParseRealm(realmStr);
                var relicInfo = RelicDatabase.GetByName(relicName);
                var relicType = relicInfo?.Type ?? RelicType.Strength;

                var evt = new RelicReturnedEvent(
                    Timestamp: timestamp,
                    RelicName: relicName,
                    Type: relicType,
                    HomeRealm: homeRealm
                );
                AddToRecentEvents(recentEvents, evt, MaxRecentEvents);
                yield return evt;
                continue;
            }

            // ==================== ZONE / BATTLEGROUND PARSING ====================

            // Zone entry pattern
            var zoneEnterMatch = ZoneEnterPattern.Match(line);
            if (zoneEnterMatch.Success)
            {
                var groups = zoneEnterMatch.Groups;
                var timestamp = TimeOnly.ParseExact(groups["timestamp"].Value, "HH:mm:ss", CultureInfo.InvariantCulture);
                var zone = groups["zone"].Value.Trim();

                var evt = new ZoneEntryEvent(
                    Timestamp: timestamp,
                    ZoneName: zone
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

    /// <summary>
    /// Parses a realm name string to the Realm enum.
    /// </summary>
    private static Realm ParseRealm(string realmStr)
    {
        return realmStr.ToLowerInvariant() switch
        {
            "albion" => Realm.Albion,
            "midgard" => Realm.Midgard,
            "hibernia" => Realm.Hibernia,
            _ => Realm.Albion // Default fallback
        };
    }

    /// <summary>
    /// Parses a siege weapon type string to the SiegeWeaponType enum.
    /// </summary>
    private static SiegeWeaponType ParseSiegeWeaponType(string weaponStr)
    {
        return weaponStr.ToLowerInvariant() switch
        {
            "ram" => SiegeWeaponType.Ram,
            "trebuchet" => SiegeWeaponType.Trebuchet,
            "ballista" => SiegeWeaponType.Ballista,
            "catapult" => SiegeWeaponType.Catapult,
            "boiling oil" => SiegeWeaponType.BoilingOil,
            _ => SiegeWeaponType.Ram // Default fallback
        };
    }

    /// <summary>
    /// Attempts to extract a keep name from a lord name (e.g., "Benowyc" -> "Caer Benowyc").
    /// </summary>
    private static string? ExtractKeepFromLordName(string lordName)
    {
        // Try to find a matching keep in the database
        var keep = KeepDatabase.FindByPartialName(lordName);
        return keep?.Name;
    }

    /// <summary>
    /// Attempts to find the name of a recent keep from siege events in the history.
    /// </summary>
    private static string? FindRecentKeepName(List<LogEvent> recentEvents)
    {
        // Look backwards through recent events for a siege event with a keep name
        for (var i = recentEvents.Count - 1; i >= 0; i--)
        {
            if (recentEvents[i] is SiegeEvent siegeEvent)
            {
                return siegeEvent.KeepName;
            }
            if (recentEvents[i] is DoorDamageEvent doorEvent)
            {
                return doorEvent.KeepName;
            }
        }
        return null;
    }

    #region Async Parsing Methods

    /// <summary>
    /// Parses the log file asynchronously with progress reporting.
    /// </summary>
    /// <param name="progress">Optional progress reporter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The parse result containing all events.</returns>
    public async Task<ParseResult> ParseAsync(
        IProgress<ParseProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;

        if (!File.Exists(_logFilePath))
        {
            return new ParseResult(
                Events: Array.Empty<LogEvent>(),
                TotalLines: 0,
                ParseTime: TimeSpan.Zero,
                WasCancelled: false);
        }

        // Count total lines for progress reporting
        long totalLines = 0;
        await using (var countStream = new FileStream(_logFilePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous | FileOptions.SequentialScan))
        using (var countReader = new StreamReader(countStream))
        {
            while (await countReader.ReadLineAsync(cancellationToken).ConfigureAwait(false) != null)
            {
                totalLines++;
                if (cancellationToken.IsCancellationRequested)
                {
                    return new ParseResult(
                        Events: Array.Empty<LogEvent>(),
                        TotalLines: 0,
                        ParseTime: DateTime.UtcNow - startTime,
                        WasCancelled: true);
                }
            }
        }

        // Now parse with progress
        var events = new List<LogEvent>();
        var recentEvents = new List<LogEvent>();
        const int MaxRecentEvents = 10;
        const int ChunkSize = 1000;
        const int ProgressReportInterval = 100;

        long lineNumber = 0;
        long lastProgressReport = 0;

        await using var parseStream = new FileStream(_logFilePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous | FileOptions.SequentialScan);
        using var reader = new StreamReader(parseStream);

        string? line;
        while ((line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false)) != null)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return new ParseResult(
                    Events: events,
                    TotalLines: lineNumber,
                    ParseTime: DateTime.UtcNow - startTime,
                    WasCancelled: true);
            }

            lineNumber++;

            // Parse the line using the synchronous method
            var parsedEvent = ParseLine(line, (int)lineNumber, recentEvents);
            if (parsedEvent != null)
            {
                AddToRecentEvents(recentEvents, parsedEvent, MaxRecentEvents);
                events.Add(parsedEvent);
            }

            // Report progress periodically
            if (lineNumber - lastProgressReport >= ProgressReportInterval)
            {
                lastProgressReport = lineNumber;
                var elapsed = DateTime.UtcNow - startTime;
                var percentComplete = totalLines > 0 ? (lineNumber * 100.0 / totalLines) : 0;
                var linesPerSecond = elapsed.TotalSeconds > 0 ? lineNumber / elapsed.TotalSeconds : 0;
                var remainingLines = totalLines - lineNumber;
                var estimatedRemaining = linesPerSecond > 0
                    ? TimeSpan.FromSeconds(remainingLines / linesPerSecond)
                    : (TimeSpan?)null;

                progress?.Report(new ParseProgress(
                    LinesProcessed: lineNumber,
                    TotalLines: totalLines,
                    EventsFound: events.Count,
                    PercentComplete: percentComplete,
                    Elapsed: elapsed,
                    EstimatedRemaining: estimatedRemaining));
            }

            // Yield control periodically for UI responsiveness
            if (lineNumber % ChunkSize == 0)
            {
                await Task.Yield();
            }
        }

        var parseTime = DateTime.UtcNow - startTime;

        // Final progress report
        progress?.Report(new ParseProgress(
            LinesProcessed: lineNumber,
            TotalLines: totalLines,
            EventsFound: events.Count,
            PercentComplete: 100,
            Elapsed: parseTime,
            EstimatedRemaining: TimeSpan.Zero));

        return new ParseResult(
            Events: events,
            TotalLines: lineNumber,
            ParseTime: parseTime,
            WasCancelled: false);
    }

    /// <summary>
    /// Parses a single line and returns the event if it matches any pattern.
    /// </summary>
    /// <param name="line">The line to parse.</param>
    /// <param name="lineNumber">The line number.</param>
    /// <param name="recentEvents">Recent events for context.</param>
    /// <returns>The parsed event, or null if the line doesn't match any pattern.</returns>
    public LogEvent? ParseLine(string line, int lineNumber, List<LogEvent> recentEvents)
    {
        // Try plugins first (ordered by priority, highest first)
        foreach (var plugin in _plugins)
        {
            var result = plugin.TryParse(line, lineNumber, recentEvents.AsReadOnly());
            if (result is ParserPluginSuccess success)
            {
                return success.Event;
            }
        }

        // Fall back to built-in patterns - check each pattern and return if matched
        return TryParseBuiltInPatterns(line, recentEvents);
    }

    /// <summary>
    /// Tries all built-in patterns against a line.
    /// Note: This is a simplified version for the async parser.
    /// For full pattern matching, use the Parse() method.
    /// </summary>
    private LogEvent? TryParseBuiltInPatterns(string line, List<LogEvent> recentEvents)
    {
        // Melee attack pattern
        var meleeMatch = MeleeAttackPattern.Match(line);
        if (meleeMatch.Success)
        {
            var groups = meleeMatch.Groups;
            var timestamp = TimeOnly.ParseExact(groups["timestamp"].Value, "HH:mm:ss", CultureInfo.InvariantCulture);
            var target = groups["target"].Value.Trim();
            var weapon = groups["weapon"].Value.Trim();
            var amount = int.Parse(groups["amount"].Value);
            int? modifier = groups["modifier"].Success ? int.Parse(groups["modifier"].Value) : null;

            return new DamageEvent(
                Timestamp: timestamp,
                Source: "You",
                Target: target,
                DamageAmount: amount,
                DamageType: "Melee",
                Modifier: modifier,
                WeaponUsed: weapon
            );
        }

        // Ranged attack pattern
        var rangedMatch = RangedAttackPattern.Match(line);
        if (rangedMatch.Success)
        {
            var groups = rangedMatch.Groups;
            var timestamp = TimeOnly.ParseExact(groups["timestamp"].Value, "HH:mm:ss", CultureInfo.InvariantCulture);
            var target = groups["target"].Value.Trim();
            var weapon = groups["weapon"].Value.Trim();
            var amount = int.Parse(groups["amount"].Value);
            int? modifier = groups["modifier"].Success ? int.Parse(groups["modifier"].Value) : null;

            return new DamageEvent(
                Timestamp: timestamp,
                Source: "You",
                Target: target,
                DamageAmount: amount,
                DamageType: "Ranged",
                Modifier: modifier,
                WeaponUsed: weapon
            );
        }

        // Damage dealt pattern
        var damageDealtMatch = DamageDealtPattern.Match(line);
        if (damageDealtMatch.Success)
        {
            var groups = damageDealtMatch.Groups;
            var timestamp = TimeOnly.ParseExact(groups["timestamp"].Value, "HH:mm:ss", CultureInfo.InvariantCulture);
            var target = groups["target"].Value.Trim();
            var amount = int.Parse(groups["amount"].Value);
            var damageType = groups["type"].Success ? groups["type"].Value.Trim() : "Unknown";
            int? modifier = groups["modifier"].Success ? int.Parse(groups["modifier"].Value) : null;

            return new DamageEvent(
                Timestamp: timestamp,
                Source: "You",
                Target: target,
                DamageAmount: amount,
                DamageType: damageType,
                Modifier: modifier
            );
        }

        // Damage taken pattern - uses DamageEvent with Target="You"
        var damageTakenMatch = DamageTakenPattern.Match(line);
        if (damageTakenMatch.Success)
        {
            var groups = damageTakenMatch.Groups;
            var timestamp = TimeOnly.ParseExact(groups["timestamp"].Value, "HH:mm:ss", CultureInfo.InvariantCulture);
            var source = groups["source"].Value.Trim();
            var amount = int.Parse(groups["amount"].Value);
            var damageType = groups["type"].Success ? groups["type"].Value.Trim() : "Unknown";
            int? modifier = groups["modifier"].Success ? int.Parse(groups["modifier"].Value) : null;
            string? bodyPart = groups["bodypart"].Success ? groups["bodypart"].Value.Trim() : null;

            return new DamageEvent(
                Timestamp: timestamp,
                Source: source,
                Target: "You",
                DamageAmount: amount,
                DamageType: damageType,
                Modifier: modifier,
                BodyPart: bodyPart
            );
        }

        // Healing done pattern
        var healDoneMatch = HealingDonePattern.Match(line);
        if (healDoneMatch.Success)
        {
            var groups = healDoneMatch.Groups;
            var timestamp = TimeOnly.ParseExact(groups["timestamp"].Value, "HH:mm:ss", CultureInfo.InvariantCulture);
            var target = groups["target"].Value.Trim();
            var amount = int.Parse(groups["amount"].Value);

            return new HealingEvent(
                Timestamp: timestamp,
                Source: "You",
                Target: target,
                HealingAmount: amount
            );
        }

        // Healing received pattern
        var healReceivedMatch = HealingReceivedPattern.Match(line);
        if (healReceivedMatch.Success)
        {
            var groups = healReceivedMatch.Groups;
            var timestamp = TimeOnly.ParseExact(groups["timestamp"].Value, "HH:mm:ss", CultureInfo.InvariantCulture);
            var source = groups["source"].Value.Trim();
            var amount = int.Parse(groups["amount"].Value);

            return new HealingEvent(
                Timestamp: timestamp,
                Source: source,
                Target: "You",
                HealingAmount: amount
            );
        }

        // Death pattern
        var deathMatch = DeathPattern.Match(line);
        if (deathMatch.Success)
        {
            var groups = deathMatch.Groups;
            var timestamp = TimeOnly.ParseExact(groups["timestamp"].Value, "HH:mm:ss", CultureInfo.InvariantCulture);
            var target = groups["target"].Value.Trim();

            return new DeathEvent(
                Timestamp: timestamp,
                Target: target
            );
        }

        // Return null if no pattern matched - other patterns will still work via the main Parse() method
        return null;
    }

    #endregion
}
