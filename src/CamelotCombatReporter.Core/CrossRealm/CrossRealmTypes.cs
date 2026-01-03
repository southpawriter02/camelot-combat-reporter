using CamelotCombatReporter.Core.Models;

namespace CamelotCombatReporter.Core.CrossRealm;

/// <summary>
/// Aggregated statistics for a realm.
/// </summary>
public record RealmStatistics(
    Realm Realm,
    int SessionCount,
    double AverageDps,
    double MedianDps,
    double MaxDps,
    double AverageHps,
    double MedianHps,
    double MaxHps,
    double AverageKdr,
    long TotalDamage,
    long TotalHealing,
    int TotalKills,
    int TotalDeaths
)
{
    /// <summary>
    /// Creates empty statistics for a realm.
    /// </summary>
    public static RealmStatistics Empty(Realm realm) =>
        new(realm, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
}

/// <summary>
/// Aggregated statistics for a character class.
/// </summary>
public record ClassStatistics(
    CharacterClass Class,
    Realm Realm,
    int SessionCount,
    double AverageDps,
    double MedianDps,
    double MaxDps,
    double AverageHps,
    double MedianHps,
    double MaxHps,
    double AverageKdr,
    long TotalDamage,
    long TotalHealing
)
{
    /// <summary>
    /// Creates empty statistics for a class.
    /// </summary>
    public static ClassStatistics Empty(CharacterClass characterClass) =>
        new(characterClass, characterClass.GetRealm(), 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
}

/// <summary>
/// A leaderboard entry for local rankings.
/// </summary>
public record LeaderboardEntry(
    int Rank,
    CharacterInfo Character,
    double Value,
    string Metric,
    DateTime SessionDateUtc,
    Guid SessionId
);

/// <summary>
/// Available metrics for leaderboards.
/// </summary>
public static class LeaderboardMetrics
{
    public const string Dps = "dps";
    public const string Hps = "hps";
    public const string Kdr = "kdr";
    public const string TotalDamage = "total_damage";
    public const string TotalHealing = "total_healing";
    public const string Kills = "kills";

    public static readonly string[] All = [Dps, Hps, Kdr, TotalDamage, TotalHealing, Kills];

    public static string GetDisplayName(string metric) => metric switch
    {
        Dps => "DPS",
        Hps => "HPS",
        Kdr => "K/D Ratio",
        TotalDamage => "Total Damage",
        TotalHealing => "Total Healing",
        Kills => "Kills",
        _ => metric
    };
}

/// <summary>
/// Index entry for fast session lookups.
/// </summary>
public record SessionIndexEntry(
    Guid Id,
    DateTime SessionStartUtc,
    Realm Realm,
    CharacterClass Class,
    double Dps,
    double Hps,
    int Kills,
    int Deaths,
    string FileName
);

/// <summary>
/// The session index file structure.
/// </summary>
public record SessionIndex(
    int Version,
    DateTime LastUpdatedUtc,
    List<SessionIndexEntry> Sessions
)
{
    public const int CurrentVersion = 1;

    public static SessionIndex Empty() =>
        new(CurrentVersion, DateTime.UtcNow, new List<SessionIndexEntry>());
}
