using CamelotCombatReporter.Core.Models;

namespace CamelotCombatReporter.Core.CrossRealm;

/// <summary>
/// Service interface for managing cross-realm combat statistics.
/// </summary>
public interface ICrossRealmStatisticsService
{
    /// <summary>
    /// Saves a combat session with extended statistics.
    /// </summary>
    Task SaveSessionAsync(ExtendedCombatStatistics stats, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all saved sessions, optionally filtered by realm, class, or date.
    /// </summary>
    Task<IReadOnlyList<CombatSessionSummary>> GetSessionsAsync(
        Realm? realm = null,
        CharacterClass? characterClass = null,
        DateTime? since = null,
        int? limit = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific session by its ID.
    /// </summary>
    Task<ExtendedCombatStatistics?> GetSessionAsync(Guid sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a specific session by its ID.
    /// </summary>
    Task<bool> DeleteSessionAsync(Guid sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets aggregated statistics for a realm.
    /// </summary>
    Task<RealmStatistics> GetRealmStatisticsAsync(Realm realm, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets aggregated statistics for a character class.
    /// </summary>
    Task<ClassStatistics> GetClassStatisticsAsync(CharacterClass characterClass, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets aggregated statistics for all realms.
    /// </summary>
    Task<IReadOnlyList<RealmStatistics>> GetAllRealmStatisticsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets aggregated statistics for all classes in a realm.
    /// </summary>
    Task<IReadOnlyList<ClassStatistics>> GetClassStatisticsForRealmAsync(Realm realm, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets local leaderboard entries for a specific metric.
    /// </summary>
    Task<IReadOnlyList<LeaderboardEntry>> GetLocalLeaderboardAsync(
        string metric,
        Realm? realm = null,
        CharacterClass? characterClass = null,
        int limit = 10,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Rebuilds the session index from disk.
    /// Useful after manual file changes or corruption recovery.
    /// </summary>
    Task RebuildIndexAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the total number of saved sessions.
    /// </summary>
    Task<int> GetSessionCountAsync(CancellationToken cancellationToken = default);
}
