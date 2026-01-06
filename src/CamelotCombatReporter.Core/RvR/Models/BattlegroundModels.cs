using CamelotCombatReporter.Core.Models;

namespace CamelotCombatReporter.Core.RvR.Models;

/// <summary>
/// Statistics for a single battleground session.
/// </summary>
public record BattlegroundStatistics(
    int Kills,
    int Deaths,
    int DeathBlows,
    int SoloKills,
    int DamageDealt,
    int DamageReceived,
    int HealingDone,
    int HealingReceived,
    int RealmPointsEarned,
    double KillDeathRatio
)
{
    /// <summary>
    /// An empty statistics record with all zeros.
    /// </summary>
    public static readonly BattlegroundStatistics Empty = new(0, 0, 0, 0, 0, 0, 0, 0, 0, 0.0);
}

/// <summary>
/// A battleground session.
/// </summary>
public record BattlegroundSession(
    Guid Id,
    BattlegroundType BattlegroundType,
    string ZoneName,
    TimeOnly StartTime,
    TimeOnly EndTime,
    TimeSpan Duration,
    IReadOnlyList<LogEvent> Events,
    BattlegroundStatistics Statistics
);

/// <summary>
/// All battleground statistics combined.
/// </summary>
public record AllBattlegroundStatistics(
    int TotalSessions,
    TimeSpan TotalTimeInBattlegrounds,
    IReadOnlyDictionary<BattlegroundType, BattlegroundStatistics> StatsByType,
    IReadOnlyDictionary<BattlegroundType, int> SessionCountByType,
    BattlegroundStatistics OverallStatistics,
    BattlegroundType? BestPerformingBattleground,
    BattlegroundType? MostPlayedBattleground
);

/// <summary>
/// Helper for determining battleground type from zone name.
/// </summary>
public static class BattlegroundZones
{
    private static readonly Dictionary<string, BattlegroundType> ZoneMapping = new(StringComparer.OrdinalIgnoreCase)
    {
        { "Thidranki", BattlegroundType.Thidranki },
        { "Molvik", BattlegroundType.Molvik },
        { "Cathal Valley", BattlegroundType.CathalValley },
        { "Killaloe", BattlegroundType.Killaloe },
        // Frontier zones map to OpenRvR
        { "Emain Macha", BattlegroundType.OpenRvR },
        { "Odin's Gate", BattlegroundType.OpenRvR },
        { "Hadrian's Wall", BattlegroundType.OpenRvR },
        { "Forest Sauvage", BattlegroundType.OpenRvR },
        { "Snowdonia", BattlegroundType.OpenRvR },
        { "Pennine Mountains", BattlegroundType.OpenRvR },
        { "Jamtland Mountains", BattlegroundType.OpenRvR },
        { "Uppland", BattlegroundType.OpenRvR },
        { "Cruachan Gorge", BattlegroundType.OpenRvR },
        { "Breifine", BattlegroundType.OpenRvR },
    };

    /// <summary>
    /// Tries to get the battleground type for a zone name.
    /// </summary>
    public static bool TryGetBattlegroundType(string zoneName, out BattlegroundType type)
    {
        return ZoneMapping.TryGetValue(zoneName, out type);
    }

    /// <summary>
    /// Gets the battleground type for a zone name, or null if not a BG zone.
    /// </summary>
    public static BattlegroundType? GetBattlegroundType(string zoneName)
    {
        return TryGetBattlegroundType(zoneName, out var type) ? type : null;
    }

    /// <summary>
    /// Checks if a zone is a battleground zone.
    /// </summary>
    public static bool IsBattlegroundZone(string zoneName)
    {
        return ZoneMapping.ContainsKey(zoneName);
    }
}
