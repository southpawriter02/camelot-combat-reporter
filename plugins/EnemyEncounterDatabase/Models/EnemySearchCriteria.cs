namespace EnemyEncounterDatabase.Models;

/// <summary>
/// Criteria for searching and filtering enemies.
/// </summary>
/// <param name="NameContains">Filter by name substring (case-insensitive).</param>
/// <param name="Type">Filter by enemy type.</param>
/// <param name="Realm">Filter by realm (for players).</param>
/// <param name="FavoritesOnly">Only return favorited enemies.</param>
/// <param name="SortBy">Property to sort by.</param>
/// <param name="SortDescending">Sort in descending order.</param>
/// <param name="Skip">Number of results to skip (for pagination).</param>
/// <param name="Take">Maximum number of results to return.</param>
public record EnemySearchCriteria(
    string? NameContains = null,
    EnemyType? Type = null,
    CamelotCombatReporter.Core.Models.Realm? Realm = null,
    bool FavoritesOnly = false,
    EnemySortBy SortBy = EnemySortBy.LastSeen,
    bool SortDescending = true,
    int Skip = 0,
    int Take = 100);

/// <summary>
/// Properties that can be used for sorting enemy records.
/// </summary>
public enum EnemySortBy
{
    /// <summary>Sort by most recent encounter.</summary>
    LastSeen,

    /// <summary>Sort by first encounter.</summary>
    FirstSeen,

    /// <summary>Sort by number of encounters.</summary>
    EncounterCount,

    /// <summary>Sort by enemy name.</summary>
    Name,

    /// <summary>Sort by total damage dealt.</summary>
    DamageDealt,

    /// <summary>Sort by total damage taken.</summary>
    DamageTaken,

    /// <summary>Sort by win rate (kills vs deaths).</summary>
    WinRate,

    /// <summary>Sort by total kills.</summary>
    Kills
}
