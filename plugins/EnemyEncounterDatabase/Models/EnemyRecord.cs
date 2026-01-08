using CamelotCombatReporter.Core.Models;

namespace EnemyEncounterDatabase.Models;

/// <summary>
/// Summary of a single encounter with an enemy.
/// </summary>
/// <param name="Timestamp">When the encounter occurred.</param>
/// <param name="Duration">How long the encounter lasted.</param>
/// <param name="DamageDealt">Total damage you dealt to the enemy.</param>
/// <param name="DamageTaken">Total damage the enemy dealt to you.</param>
/// <param name="Outcome">How the encounter ended.</param>
/// <param name="Location">Optional zone or location name.</param>
public record EncounterSummary(
    DateTime Timestamp,
    TimeSpan Duration,
    int DamageDealt,
    int DamageTaken,
    EncounterOutcome Outcome,
    string? Location = null);

/// <summary>
/// Aggregated statistics for all encounters with a specific enemy.
/// </summary>
/// <param name="TotalDamageDealt">Total damage you've dealt to this enemy.</param>
/// <param name="TotalDamageTaken">Total damage this enemy has dealt to you.</param>
/// <param name="TotalKills">Number of times you've killed this enemy.</param>
/// <param name="TotalDeaths">Number of times this enemy has killed you.</param>
/// <param name="AverageEncounterDuration">Average duration of encounters in seconds.</param>
/// <param name="AverageDps">Average damage per second against this enemy.</param>
/// <param name="DamageByAbility">Damage dealt grouped by ability name.</param>
/// <param name="DamageTakenByAbility">Damage taken grouped by ability name.</param>
public record EnemyStatistics(
    long TotalDamageDealt,
    long TotalDamageTaken,
    int TotalKills,
    int TotalDeaths,
    double AverageEncounterDuration,
    double AverageDps,
    IReadOnlyDictionary<string, long> DamageByAbility,
    IReadOnlyDictionary<string, long> DamageTakenByAbility)
{
    /// <summary>
    /// Win rate as a percentage (0-100).
    /// </summary>
    public double WinRate => TotalKills + TotalDeaths > 0
        ? (double)TotalKills / (TotalKills + TotalDeaths) * 100
        : 0;

    /// <summary>
    /// Creates an empty statistics record.
    /// </summary>
    public static EnemyStatistics Empty => new(
        0, 0, 0, 0, 0, 0,
        new Dictionary<string, long>(),
        new Dictionary<string, long>());
}

/// <summary>
/// Complete record for a cataloged enemy.
/// </summary>
/// <param name="Id">Unique identifier (hash of name + type).</param>
/// <param name="Name">Display name of the enemy.</param>
/// <param name="Type">Classification (Mob, Player, NPC).</param>
/// <param name="Realm">Realm for player enemies.</param>
/// <param name="Class">Character class if detectable.</param>
/// <param name="EncounterCount">Total number of encounters.</param>
/// <param name="FirstSeen">Date of first encounter.</param>
/// <param name="LastSeen">Date of most recent encounter.</param>
/// <param name="Statistics">Aggregated combat statistics.</param>
/// <param name="RecentEncounters">Most recent encounter summaries (up to 50).</param>
/// <param name="Notes">User's personal notes about this enemy.</param>
/// <param name="IsFavorite">Whether this enemy is bookmarked.</param>
public record EnemyRecord(
    string Id,
    string Name,
    EnemyType Type,
    Realm? Realm,
    CharacterClass? Class,
    int EncounterCount,
    DateTime FirstSeen,
    DateTime LastSeen,
    EnemyStatistics Statistics,
    IReadOnlyList<EncounterSummary> RecentEncounters,
    string? Notes,
    bool IsFavorite)
{
    /// <summary>
    /// Maximum number of recent encounters to store per enemy.
    /// </summary>
    public const int MaxRecentEncounters = 50;

    /// <summary>
    /// Creates a new enemy record for a first-time encounter.
    /// </summary>
    public static EnemyRecord CreateNew(string name, EnemyType type, Realm? realm = null)
    {
        var id = GenerateId(name, type);
        var now = DateTime.UtcNow;

        return new EnemyRecord(
            Id: id,
            Name: name,
            Type: type,
            Realm: realm,
            Class: null,
            EncounterCount: 0,
            FirstSeen: now,
            LastSeen: now,
            Statistics: EnemyStatistics.Empty,
            RecentEncounters: Array.Empty<EncounterSummary>(),
            Notes: null,
            IsFavorite: false);
    }

    /// <summary>
    /// Generates a stable ID for an enemy based on name and type.
    /// </summary>
    public static string GenerateId(string name, EnemyType type)
    {
        var input = $"{type}:{name.ToLowerInvariant()}";
        var bytes = System.Text.Encoding.UTF8.GetBytes(input);
        var hash = System.Security.Cryptography.SHA256.HashData(bytes);
        return Convert.ToHexString(hash)[..16].ToLowerInvariant();
    }
}
