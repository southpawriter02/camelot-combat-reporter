using EnemyEncounterDatabase.Models;

namespace EnemyEncounterDatabase.Services;

/// <summary>
/// Interface for enemy encounter database operations.
/// Provides CRUD operations for enemy records and encounter summaries.
/// </summary>
public interface IEnemyDatabase
{
    /// <summary>
    /// Gets an enemy record by its unique ID.
    /// </summary>
    /// <param name="id">The enemy's unique identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The enemy record if found, null otherwise.</returns>
    Task<EnemyRecord?> GetEnemyAsync(string id, CancellationToken ct = default);

    /// <summary>
    /// Gets an enemy record by name and type, or creates a new one if not found.
    /// </summary>
    /// <param name="name">The enemy's display name.</param>
    /// <param name="type">The enemy type classification.</param>
    /// <param name="realm">Optional realm for player enemies.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The existing or newly created enemy record.</returns>
    Task<EnemyRecord> GetOrCreateAsync(
        string name,
        EnemyType type,
        CamelotCombatReporter.Core.Models.Realm? realm = null,
        CancellationToken ct = default);

    /// <summary>
    /// Searches for enemies matching the specified criteria.
    /// </summary>
    /// <param name="criteria">Search and filter criteria.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of matching enemy records.</returns>
    Task<IReadOnlyList<EnemyRecord>> SearchAsync(
        EnemySearchCriteria criteria,
        CancellationToken ct = default);

    /// <summary>
    /// Saves an enemy record to the database.
    /// </summary>
    /// <param name="record">The record to save.</param>
    /// <param name="ct">Cancellation token.</param>
    Task SaveEnemyAsync(EnemyRecord record, CancellationToken ct = default);

    /// <summary>
    /// Adds a new encounter to an enemy's history.
    /// Updates statistics and recent encounters list.
    /// </summary>
    /// <param name="enemyId">The enemy's unique identifier.</param>
    /// <param name="encounter">The encounter summary to add.</param>
    /// <param name="abilityDamage">Damage dealt grouped by ability.</param>
    /// <param name="abilityDamageTaken">Damage taken grouped by ability.</param>
    /// <param name="ct">Cancellation token.</param>
    Task AddEncounterAsync(
        string enemyId,
        EncounterSummary encounter,
        Dictionary<string, long>? abilityDamage = null,
        Dictionary<string, long>? abilityDamageTaken = null,
        CancellationToken ct = default);

    /// <summary>
    /// Updates the notes for an enemy.
    /// </summary>
    /// <param name="enemyId">The enemy's unique identifier.</param>
    /// <param name="notes">The new notes text (null to clear).</param>
    /// <param name="ct">Cancellation token.</param>
    Task UpdateNotesAsync(string enemyId, string? notes, CancellationToken ct = default);

    /// <summary>
    /// Sets or clears the favorite flag for an enemy.
    /// </summary>
    /// <param name="enemyId">The enemy's unique identifier.</param>
    /// <param name="isFavorite">True to mark as favorite.</param>
    /// <param name="ct">Cancellation token.</param>
    Task SetFavoriteAsync(string enemyId, bool isFavorite, CancellationToken ct = default);

    /// <summary>
    /// Gets the total number of unique enemies in the database.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    Task<int> GetTotalCountAsync(CancellationToken ct = default);

    /// <summary>
    /// Persists any pending changes to storage.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    Task SaveChangesAsync(CancellationToken ct = default);
}
