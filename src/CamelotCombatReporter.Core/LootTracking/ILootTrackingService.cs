using CamelotCombatReporter.Core.Models;

namespace CamelotCombatReporter.Core.LootTracking;

/// <summary>
/// Service interface for tracking and analyzing loot drop data.
/// </summary>
public interface ILootTrackingService
{
    #region Session Management

    /// <summary>
    /// Saves loot events from a parsed log file as a new session.
    /// </summary>
    /// <param name="events">The loot events to save.</param>
    /// <param name="logFilePath">The source log file path.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The session summary.</returns>
    Task<LootSessionSummary> SaveSessionAsync(
        IReadOnlyList<LootEvent> events,
        string logFilePath,
        CancellationToken ct = default);

    /// <summary>
    /// Gets a specific session by its ID.
    /// </summary>
    /// <param name="sessionId">The session ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The session summary, or null if not found.</returns>
    Task<LootSessionSummary?> GetSessionAsync(Guid sessionId, CancellationToken ct = default);

    /// <summary>
    /// Gets the most recent sessions.
    /// </summary>
    /// <param name="limit">Maximum number of sessions to return.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of recent session summaries.</returns>
    Task<IReadOnlyList<LootSessionSummary>> GetRecentSessionsAsync(int limit = 10, CancellationToken ct = default);

    /// <summary>
    /// Deletes a session and its data.
    /// </summary>
    /// <param name="sessionId">The session ID to delete.</param>
    /// <param name="ct">Cancellation token.</param>
    Task DeleteSessionAsync(Guid sessionId, CancellationToken ct = default);

    #endregion

    #region Mob Queries

    /// <summary>
    /// Gets the loot table for a specific mob.
    /// </summary>
    /// <param name="mobName">The name of the mob.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The mob's loot table, or null if not found.</returns>
    Task<MobLootTable?> GetMobLootTableAsync(string mobName, CancellationToken ct = default);

    /// <summary>
    /// Searches for mobs matching the given criteria.
    /// </summary>
    /// <param name="query">Optional search query for mob names.</param>
    /// <param name="sortBy">Sort field (e.g., "name", "kills", "lastSeen").</param>
    /// <param name="limit">Maximum number of results.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of matching mob loot tables.</returns>
    Task<IReadOnlyList<MobLootTable>> SearchMobsAsync(
        string? query = null,
        string sortBy = "kills",
        int limit = 50,
        CancellationToken ct = default);

    /// <summary>
    /// Gets the total number of kills recorded for a specific mob.
    /// </summary>
    /// <param name="mobName">The name of the mob.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Total kill count.</returns>
    Task<int> GetTotalKillsAsync(string mobName, CancellationToken ct = default);

    #endregion

    #region Item Queries

    /// <summary>
    /// Gets all mobs that can drop a specific item.
    /// </summary>
    /// <param name="itemName">The name of the item.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of item drop statistics from different sources.</returns>
    Task<IReadOnlyList<ItemDropStatistic>> GetItemSourcesAsync(string itemName, CancellationToken ct = default);

    /// <summary>
    /// Searches for items matching the given criteria.
    /// </summary>
    /// <param name="query">Optional search query for item names.</param>
    /// <param name="sortBy">Sort field (e.g., "name", "drops", "rate").</param>
    /// <param name="limit">Maximum number of results.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of matching item drop statistics.</returns>
    Task<IReadOnlyList<ItemDropStatistic>> SearchItemsAsync(
        string? query = null,
        string sortBy = "drops",
        int limit = 50,
        CancellationToken ct = default);

    #endregion

    #region Statistics

    /// <summary>
    /// Gets overall loot tracking statistics.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Overall statistics summary.</returns>
    Task<LootTrackingStats> GetOverallStatsAsync(CancellationToken ct = default);

    /// <summary>
    /// Rebuilds all statistics from stored session data.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    Task RebuildStatisticsAsync(CancellationToken ct = default);

    #endregion
}

/// <summary>
/// Overall loot tracking statistics.
/// </summary>
/// <param name="TotalSessions">Total number of sessions tracked.</param>
/// <param name="TotalMobsTracked">Total unique mobs tracked.</param>
/// <param name="TotalItemsTracked">Total unique items tracked.</param>
/// <param name="TotalKills">Total mob kills across all sessions.</param>
/// <param name="TotalItemDrops">Total item drops across all sessions.</param>
/// <param name="TotalCurrencyEarned">Total currency earned in copper.</param>
/// <param name="FirstSession">Date of first session.</param>
/// <param name="LastSession">Date of most recent session.</param>
public record LootTrackingStats(
    int TotalSessions,
    int TotalMobsTracked,
    int TotalItemsTracked,
    int TotalKills,
    int TotalItemDrops,
    long TotalCurrencyEarned,
    DateTime? FirstSession,
    DateTime? LastSession
)
{
    /// <summary>
    /// Gets the total currency formatted as a display string.
    /// </summary>
    public string TotalCurrencyFormatted
    {
        get
        {
            var gold = TotalCurrencyEarned / 10000;
            var silver = (TotalCurrencyEarned % 10000) / 100;
            var copper = TotalCurrencyEarned % 100;

            var parts = new List<string>();
            if (gold > 0) parts.Add($"{gold}g");
            if (silver > 0) parts.Add($"{silver}s");
            if (copper > 0 || parts.Count == 0) parts.Add($"{copper}c");
            return string.Join(" ", parts);
        }
    }

    /// <summary>
    /// Empty statistics for when no data exists.
    /// </summary>
    public static LootTrackingStats Empty => new(0, 0, 0, 0, 0, 0, null, null);
}
