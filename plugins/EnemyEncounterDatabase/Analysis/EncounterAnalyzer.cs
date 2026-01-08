using System.Text.RegularExpressions;
using CamelotCombatReporter.Core.Models;
using EnemyEncounterDatabase.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace EnemyEncounterDatabase.Analysis;

/// <summary>
/// Analyzes combat events to detect and classify enemy encounters.
/// </summary>
/// <remarks>
/// <para>
/// The analyzer processes combat log events to identify unique enemy encounters,
/// aggregate damage statistics, and classify enemies using heuristic patterns.
/// </para>
/// <para>
/// <strong>Enemy Classification Rules</strong>:
/// <list type="bullet">
///   <item>
///     <term>Player</term>
///     <description>Single capitalized word, 3-15 characters (e.g., "Nemesis", "Darkblade")</description>
///   </item>
///   <item>
///     <term>Mob</term>
///     <description>Contains spaces, articles ("a", "an", "the"), or starts lowercase</description>
///   </item>
///   <item>
///     <term>Unknown</term>
///     <description>Cannot be reliably classified</description>
///   </item>
/// </list>
/// </para>
/// <para>
/// <strong>Encounter Grouping</strong>: Events are grouped by opponent name.
/// Each unique opponent becomes a separate encounter with aggregated statistics.
/// </para>
/// </remarks>
public static partial class EncounterAnalyzer
{
    /// <summary>
    /// Regular expression pattern for player-like names.
    /// Matches single capitalized words between 3-15 characters.
    /// </summary>
    /// <example>
    /// Matches: "Nemesis", "Darkblade", "Bob"
    /// Does not match: "a goblin", "forest wolf", "AB"
    /// </example>
    [GeneratedRegex(@"^[A-Z][a-z]{2,15}$")]
    private static partial Regex PlayerNamePattern();

    /// <summary>
    /// Detects all enemy encounters from a set of combat events.
    /// </summary>
    /// <param name="events">The combat log events to analyze.</param>
    /// <param name="combatantName">
    /// The player's character name. Used to determine which events involve the player
    /// and which direction damage flows (dealt vs taken).
    /// </param>
    /// <param name="logger">Optional logger for diagnostic output.</param>
    /// <returns>
    /// List of detected enemy encounters, one per unique opponent.
    /// Returns an empty list if no damage events involve the combatant.
    /// </returns>
    /// <remarks>
    /// <para>
    /// The algorithm:
    /// <list type="number">
    ///   <item><description>Filters events to only DamageEvents involving the player</description></item>
    ///   <item><description>Groups events by opponent name</description></item>
    ///   <item><description>For each opponent, aggregates damage dealt/taken</description></item>
    ///   <item><description>Calculates encounter duration from first to last event</description></item>
    ///   <item><description>Classifies the enemy type using heuristics</description></item>
    ///   <item><description>Attempts to determine encounter outcome</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    public static IReadOnlyList<DetectedEncounter> DetectEncounters(
        IReadOnlyList<LogEvent> events,
        string combatantName,
        ILogger? logger = null)
    {
        logger ??= NullLogger.Instance;
        var encounters = new List<DetectedEncounter>();

        // Filter to only damage events involving the player
        var damageEvents = events.OfType<DamageEvent>()
            .Where(e => e.Source == combatantName || e.Target == combatantName)
            .ToList();

        logger.LogDebug(
            "DetectEncounters: Found {DamageEventCount} damage events for {Combatant}",
            damageEvents.Count,
            combatantName);

        // Group all damage events by opponent
        var byEnemy = damageEvents
            .GroupBy(e => e.Source == combatantName ? e.Target : e.Source);

        foreach (var group in byEnemy)
        {
            var enemyName = group.Key;

            // Skip self-damage and empty names
            if (string.IsNullOrWhiteSpace(enemyName) || enemyName == combatantName)
            {
                continue;
            }

            var enemyEvents = group.ToList();
            if (enemyEvents.Count == 0)
            {
                continue;
            }

            // Calculate damage statistics
            var damageDealt = enemyEvents
                .Where(e => e.Source == combatantName)
                .Sum(e => e.DamageAmount);

            var damageTaken = enemyEvents
                .Where(e => e.Target == combatantName)
                .Sum(e => e.DamageAmount);

            // Calculate encounter duration
            var firstEvent = enemyEvents.MinBy(e => e.Timestamp)!;
            var lastEvent = enemyEvents.MaxBy(e => e.Timestamp)!;
            var duration = CalculateDuration(firstEvent.Timestamp, lastEvent.Timestamp);

            // Build ability damage breakdown
            var damageByAbility = BuildAbilityBreakdown(
                enemyEvents.Where(e => e.Source == combatantName));

            var damageTakenByAbility = BuildAbilityBreakdown(
                enemyEvents.Where(e => e.Target == combatantName));

            // Classify the enemy
            var (enemyType, realm) = ClassifyEnemy(enemyName, enemyEvents, logger);

            // Determine encounter outcome
            var outcome = DetermineOutcome(events, combatantName, enemyName, logger);

            var encounter = new DetectedEncounter(
                EnemyName: enemyName,
                Type: enemyType,
                Realm: realm,
                DamageDealt: damageDealt,
                DamageTaken: damageTaken,
                Duration: duration,
                Outcome: outcome,
                DamageByAbility: damageByAbility,
                DamageTakenByAbility: damageTakenByAbility,
                Timestamp: DateTime.UtcNow);

            encounters.Add(encounter);

            logger.LogDebug(
                "DetectEncounters: Detected {EnemyName} ({Type}) - dealt {Dealt}, took {Taken}, duration {Duration:F1}s",
                enemyName,
                enemyType,
                damageDealt,
                damageTaken,
                duration.TotalSeconds);
        }

        logger.LogInformation(
            "DetectEncounters: Detected {Count} enemy encounters for {Combatant}",
            encounters.Count,
            combatantName);

        return encounters;
    }

    /// <summary>
    /// Classifies an enemy as Mob, Player, or NPC based on naming patterns.
    /// </summary>
    /// <param name="name">The enemy's display name from the combat log.</param>
    /// <param name="events">Damage events for context-based classification.</param>
    /// <param name="logger">Optional logger for diagnostic output.</param>
    /// <returns>
    /// A tuple containing the classified <see cref="EnemyType"/> and optionally
    /// detected <see cref="Realm"/> for player enemies.
    /// </returns>
    /// <remarks>
    /// <para>
    /// The classification heuristics are designed for Dark Age of Camelot combat logs:
    /// </para>
    /// <list type="bullet">
    ///   <item>
    ///     <description>
    ///       <strong>Player names</strong>: Single capitalized word (e.g., "Nemesis")
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///       <strong>Mob names</strong>: May include articles ("a goblin"), 
    ///       multiple words ("forest wolf"), or be lowercase
    ///     </description>
    ///   </item>
    /// </list>
    /// <para>
    /// Future enhancements may use ability diversity and damage patterns to
    /// improve classification accuracy.
    /// </para>
    /// </remarks>
    private static (EnemyType Type, Realm? Realm) ClassifyEnemy(
        string name,
        IReadOnlyList<DamageEvent> events,
        ILogger? logger = null)
    {
        logger ??= NullLogger.Instance;

        // Check for player-like names (single capitalized word)
        if (PlayerNamePattern().IsMatch(name))
        {
            // Players typically use multiple ability types
            var damageTypes = events.Select(e => e.DamageType).Distinct().Count();

            logger.LogTrace(
                "ClassifyEnemy: {Name} matches player pattern, {TypeCount} damage types",
                name,
                damageTypes);

            // Note: Realm detection could be enhanced by analyzing ability names
            // that are realm-specific (e.g., "Essence Flames" = Hibernia)
            return (EnemyType.Player, null);
        }

        // Check for mob patterns (articles, spaces, lowercase start)
        if (HasMobNamingPattern(name))
        {
            logger.LogTrace("ClassifyEnemy: {Name} classified as Mob (naming pattern)", name);
            return (EnemyType.Mob, null);
        }

        // Check for lowercase first character (common for generic mobs)
        if (!string.IsNullOrEmpty(name) && char.IsLower(name[0]))
        {
            logger.LogTrace("ClassifyEnemy: {Name} classified as Mob (lowercase)", name);
            return (EnemyType.Mob, null);
        }

        logger.LogTrace("ClassifyEnemy: {Name} could not be classified", name);
        return (EnemyType.Unknown, null);
    }

    /// <summary>
    /// Checks if a name has patterns typical of mob naming conventions.
    /// </summary>
    /// <param name="name">The name to check.</param>
    /// <returns>True if the name matches mob naming patterns.</returns>
    private static bool HasMobNamingPattern(string name)
    {
        return name.Contains(' ')
            || name.StartsWith("a ", StringComparison.OrdinalIgnoreCase)
            || name.StartsWith("an ", StringComparison.OrdinalIgnoreCase)
            || name.StartsWith("the ", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Determines the outcome of an encounter based on death events.
    /// </summary>
    /// <param name="allEvents">All combat log events to search for death events.</param>
    /// <param name="combatantName">The player's character name.</param>
    /// <param name="enemyName">The enemy's name.</param>
    /// <param name="logger">Optional logger for diagnostic output.</param>
    /// <returns>
    /// The determined <see cref="EncounterOutcome"/>. Returns <see cref="EncounterOutcome.Unknown"/>
    /// if no death events are found for either party.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Current implementation returns <see cref="EncounterOutcome.Unknown"/> as a placeholder.
    /// A complete implementation would:
    /// </para>
    /// <list type="number">
    ///   <item><description>Look for "You have killed" messages</description></item>
    ///   <item><description>Look for "You have been slain by" messages</description></item>
    ///   <item><description>Check DeathEvent types if available</description></item>
    /// </list>
    /// </remarks>
    private static EncounterOutcome DetermineOutcome(
        IReadOnlyList<LogEvent> allEvents,
        string combatantName,
        string enemyName,
        ILogger? logger = null)
    {
        logger ??= NullLogger.Instance;

        // TODO: Implement death event parsing
        // This would require:
        // 1. A DeathEvent type in the Core models
        // 2. Parsing "You have killed X" messages
        // 3. Parsing "X has killed you" or "You have been slain by X" messages

        logger.LogTrace(
            "DetermineOutcome: Death event parsing not yet implemented for {Enemy}",
            enemyName);

        return EncounterOutcome.Unknown;
    }

    /// <summary>
    /// Builds a breakdown of damage by ability/damage type.
    /// </summary>
    /// <param name="events">Damage events to aggregate.</param>
    /// <returns>Dictionary mapping damage type to total damage.</returns>
    private static Dictionary<string, long> BuildAbilityBreakdown(IEnumerable<DamageEvent> events)
    {
        return events
            .Where(e => !string.IsNullOrEmpty(e.DamageType))
            .GroupBy(e => e.DamageType)
            .ToDictionary(g => g.Key, g => (long)g.Sum(e => e.DamageAmount));
    }

    /// <summary>
    /// Calculates duration between two timestamps, handling midnight crossover.
    /// </summary>
    /// <param name="start">The start timestamp.</param>
    /// <param name="end">The end timestamp.</param>
    /// <returns>
    /// The duration between the timestamps. If end is before start (midnight crossover),
    /// assumes the encounter spans into the next day.
    /// </returns>
    private static TimeSpan CalculateDuration(TimeOnly start, TimeOnly end)
    {
        if (end < start)
        {
            // Handle crossing midnight (e.g., 23:59 to 00:01)
            return TimeSpan.FromHours(24) - (start - end);
        }
        return end - start;
    }
}

/// <summary>
/// Represents a detected enemy encounter from log analysis.
/// </summary>
/// <remarks>
/// This is an intermediate data structure produced by <see cref="EncounterAnalyzer.DetectEncounters"/>
/// and consumed by the plugin to update the enemy database.
/// </remarks>
/// <param name="EnemyName">Display name of the enemy from the combat log.</param>
/// <param name="Type">Classified type of the enemy (Mob, Player, NPC, Unknown).</param>
/// <param name="Realm">Detected realm for player enemies (null if unknown or not a player).</param>
/// <param name="DamageDealt">Total damage the player dealt to this enemy.</param>
/// <param name="DamageTaken">Total damage this enemy dealt to the player.</param>
/// <param name="Duration">Duration from first to last damage event in this encounter.</param>
/// <param name="Outcome">How the encounter ended (Victory, Defeat, Escaped, Unknown).</param>
/// <param name="DamageByAbility">Player damage dealt, grouped by ability/damage type.</param>
/// <param name="DamageTakenByAbility">Enemy damage dealt, grouped by ability/damage type.</param>
/// <param name="Timestamp">When this encounter was processed (typically current time).</param>
public record DetectedEncounter(
    string EnemyName,
    EnemyType Type,
    Realm? Realm,
    int DamageDealt,
    int DamageTaken,
    TimeSpan Duration,
    EncounterOutcome Outcome,
    Dictionary<string, long> DamageByAbility,
    Dictionary<string, long> DamageTakenByAbility,
    DateTime Timestamp);
