using System.Text.Json;
using System.Text.Json.Serialization;
using EnemyEncounterDatabase.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace EnemyEncounterDatabase.Services;

/// <summary>
/// JSON file-based implementation of the enemy database.
/// </summary>
/// <remarks>
/// <para>
/// This implementation provides thread-safe, persistent storage for enemy records
/// using a local JSON file. All operations are protected by a <see cref="SemaphoreSlim"/>
/// to ensure data integrity during concurrent access.
/// </para>
/// <para>
/// <strong>Storage Location</strong>: Data is persisted to <c>{PluginDataDirectory}/enemies.json</c>.
/// The file uses camelCase property naming and string enum serialization for readability.
/// </para>
/// <para>
/// <strong>Lazy Loading</strong>: The database is loaded on first access rather than
/// during construction. This improves startup performance when the plugin is loaded
/// but not immediately used.
/// </para>
/// <para>
/// <strong>Write-Behind Pattern</strong>: Changes are accumulated in memory and only
/// persisted when <see cref="SaveChangesAsync"/> is called. This batches multiple
/// operations and reduces disk I/O.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Create database instance
/// var database = new JsonEnemyDatabase(pluginDataDirectory, logger);
/// 
/// // Get or create an enemy
/// var enemy = await database.GetOrCreateAsync("Goblin", EnemyType.Mob);
/// 
/// // Add an encounter
/// await database.AddEncounterAsync(enemy.Id, encounterSummary);
/// 
/// // Persist changes
/// await database.SaveChangesAsync();
/// </code>
/// </example>
public sealed class JsonEnemyDatabase : IEnemyDatabase, IDisposable
{
    private const string DatabaseFileName = "enemies.json";

    private readonly string _databasePath;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly ILogger<JsonEnemyDatabase> _logger;
    private Dictionary<string, EnemyRecord> _enemies = new();
    private bool _isDirty;
    private bool _isLoaded;
    private bool _disposed;

    /// <summary>
    /// Creates a new JSON enemy database instance.
    /// </summary>
    /// <param name="pluginDataDirectory">
    /// The directory where plugin data should be stored.
    /// This is typically provided by <see cref="CamelotCombatReporter.Plugins.Abstractions.IPluginContext.PluginDataDirectory"/>.
    /// </param>
    /// <param name="logger">
    /// Optional logger for diagnostic output. If null, logging is disabled.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="pluginDataDirectory"/> is null or empty.
    /// </exception>
    public JsonEnemyDatabase(string pluginDataDirectory, ILogger<JsonEnemyDatabase>? logger = null)
    {
        if (string.IsNullOrWhiteSpace(pluginDataDirectory))
        {
            throw new ArgumentNullException(nameof(pluginDataDirectory));
        }

        _databasePath = Path.Combine(pluginDataDirectory, DatabaseFileName);
        _logger = logger ?? NullLogger<JsonEnemyDatabase>.Instance;

        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = { new JsonStringEnumConverter() }
        };

        _logger.LogDebug("JsonEnemyDatabase initialized with path: {DatabasePath}", _databasePath);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Returns null if no enemy with the specified ID exists.
    /// The operation is O(1) as it uses a dictionary lookup.
    /// </remarks>
    public async Task<EnemyRecord?> GetEnemyAsync(string id, CancellationToken ct = default)
    {
        await EnsureLoadedAsync(ct).ConfigureAwait(false);
        await _lock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var found = _enemies.TryGetValue(id, out var enemy);
            _logger.LogTrace("GetEnemyAsync({Id}): found={Found}", id, found);
            return enemy;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc/>
    /// <remarks>
    /// <para>
    /// This method implements an "upsert" pattern - it returns an existing record
    /// if one exists for the given name/type combination, or creates a new one.
    /// </para>
    /// <para>
    /// The enemy ID is generated deterministically from the name and type using
    /// SHA256, making lookups consistent across sessions.
    /// </para>
    /// </remarks>
    public async Task<EnemyRecord> GetOrCreateAsync(
        string name,
        EnemyType type,
        CamelotCombatReporter.Core.Models.Realm? realm = null,
        CancellationToken ct = default)
    {
        await EnsureLoadedAsync(ct).ConfigureAwait(false);

        var id = EnemyRecord.GenerateId(name, type);

        await _lock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            if (_enemies.TryGetValue(id, out var existing))
            {
                _logger.LogTrace("GetOrCreateAsync: Found existing enemy {Name} ({Id})", name, id);
                return existing;
            }

            var newRecord = EnemyRecord.CreateNew(name, type, realm);
            _enemies[id] = newRecord;
            _isDirty = true;

            _logger.LogDebug("GetOrCreateAsync: Created new enemy {Name} as {Type} ({Id})", name, type, id);
            return newRecord;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc/>
    /// <remarks>
    /// <para>
    /// Search is performed in-memory using LINQ. For large databases (1000+ enemies),
    /// consider adding caching or database indexing if performance becomes an issue.
    /// </para>
    /// <para>
    /// Results are paginated using Skip/Take. The default limit is 100 records.
    /// </para>
    /// </remarks>
    public async Task<IReadOnlyList<EnemyRecord>> SearchAsync(
        EnemySearchCriteria criteria,
        CancellationToken ct = default)
    {
        await EnsureLoadedAsync(ct).ConfigureAwait(false);
        await _lock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            IEnumerable<EnemyRecord> query = _enemies.Values;

            // Apply filters
            if (!string.IsNullOrWhiteSpace(criteria.NameContains))
            {
                var searchTerm = criteria.NameContains.Trim();
                query = query.Where(e =>
                    e.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase));
            }

            if (criteria.Type.HasValue)
            {
                query = query.Where(e => e.Type == criteria.Type.Value);
            }

            if (criteria.Realm.HasValue)
            {
                query = query.Where(e => e.Realm == criteria.Realm.Value);
            }

            if (criteria.FavoritesOnly)
            {
                query = query.Where(e => e.IsFavorite);
            }

            // Apply sorting
            query = ApplySorting(query, criteria.SortBy, criteria.SortDescending);

            // Apply pagination
            var results = query.Skip(criteria.Skip).Take(criteria.Take).ToList();

            _logger.LogDebug(
                "SearchAsync: Found {Count} enemies matching criteria (name={Name}, type={Type})",
                results.Count,
                criteria.NameContains ?? "(any)",
                criteria.Type?.ToString() ?? "(any)");

            return results;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task SaveEnemyAsync(EnemyRecord record, CancellationToken ct = default)
    {
        await EnsureLoadedAsync(ct).ConfigureAwait(false);
        await _lock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            _enemies[record.Id] = record;
            _isDirty = true;

            _logger.LogDebug("SaveEnemyAsync: Saved enemy {Name} ({Id})", record.Name, record.Id);
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc/>
    /// <remarks>
    /// <para>
    /// This method performs several updates atomically:
    /// <list type="number">
    ///   <item><description>Updates cumulative statistics (damage, kills, deaths)</description></item>
    ///   <item><description>Recalculates averages (encounter duration, DPS)</description></item>
    ///   <item><description>Merges ability-specific damage breakdowns</description></item>
    ///   <item><description>Adds encounter to recent history (capped at 50)</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    public async Task AddEncounterAsync(
        string enemyId,
        EncounterSummary encounter,
        Dictionary<string, long>? abilityDamage = null,
        Dictionary<string, long>? abilityDamageTaken = null,
        CancellationToken ct = default)
    {
        await EnsureLoadedAsync(ct).ConfigureAwait(false);
        await _lock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            if (!_enemies.TryGetValue(enemyId, out var enemy))
            {
                _logger.LogWarning("AddEncounterAsync: Enemy {Id} not found", enemyId);
                return;
            }

            // Update statistics
            var stats = enemy.Statistics;
            var newDamageByAbility = MergeDictionaries(
                stats.DamageByAbility,
                abilityDamage ?? new Dictionary<string, long>());
            var newDamageTakenByAbility = MergeDictionaries(
                stats.DamageTakenByAbility,
                abilityDamageTaken ?? new Dictionary<string, long>());

            var newKills = stats.TotalKills + (encounter.Outcome == EncounterOutcome.Victory ? 1 : 0);
            var newDeaths = stats.TotalDeaths + (encounter.Outcome == EncounterOutcome.Defeat ? 1 : 0);
            var newEncounterCount = enemy.EncounterCount + 1;

            // Calculate running averages
            var totalDuration = stats.AverageEncounterDuration * enemy.EncounterCount
                + encounter.Duration.TotalSeconds;
            var newAvgDuration = totalDuration / newEncounterCount;

            var totalDamage = stats.TotalDamageDealt + encounter.DamageDealt;
            var newAvgDps = totalDuration > 0 ? totalDamage / totalDuration : 0;

            var newStats = new EnemyStatistics(
                TotalDamageDealt: stats.TotalDamageDealt + encounter.DamageDealt,
                TotalDamageTaken: stats.TotalDamageTaken + encounter.DamageTaken,
                TotalKills: newKills,
                TotalDeaths: newDeaths,
                AverageEncounterDuration: newAvgDuration,
                AverageDps: newAvgDps,
                DamageByAbility: newDamageByAbility,
                DamageTakenByAbility: newDamageTakenByAbility);

            // Update recent encounters (keep most recent 50)
            var recentEncounters = enemy.RecentEncounters
                .Prepend(encounter)
                .Take(EnemyRecord.MaxRecentEncounters)
                .ToList();

            // Create updated record using immutable pattern
            var updatedEnemy = enemy with
            {
                EncounterCount = newEncounterCount,
                LastSeen = encounter.Timestamp,
                Statistics = newStats,
                RecentEncounters = recentEncounters
            };

            _enemies[enemyId] = updatedEnemy;
            _isDirty = true;

            _logger.LogDebug(
                "AddEncounterAsync: Added encounter to {Name} - outcome={Outcome}, dmg={Damage}, count={Count}",
                enemy.Name,
                encounter.Outcome,
                encounter.DamageDealt,
                newEncounterCount);
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task UpdateNotesAsync(string enemyId, string? notes, CancellationToken ct = default)
    {
        await EnsureLoadedAsync(ct).ConfigureAwait(false);
        await _lock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            if (_enemies.TryGetValue(enemyId, out var enemy))
            {
                _enemies[enemyId] = enemy with { Notes = notes };
                _isDirty = true;

                _logger.LogDebug(
                    "UpdateNotesAsync: Updated notes for {Name} ({Id})",
                    enemy.Name,
                    enemyId);
            }
            else
            {
                _logger.LogWarning("UpdateNotesAsync: Enemy {Id} not found", enemyId);
            }
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task SetFavoriteAsync(string enemyId, bool isFavorite, CancellationToken ct = default)
    {
        await EnsureLoadedAsync(ct).ConfigureAwait(false);
        await _lock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            if (_enemies.TryGetValue(enemyId, out var enemy))
            {
                _enemies[enemyId] = enemy with { IsFavorite = isFavorite };
                _isDirty = true;

                _logger.LogDebug(
                    "SetFavoriteAsync: Set favorite={Favorite} for {Name}",
                    isFavorite,
                    enemy.Name);
            }
            else
            {
                _logger.LogWarning("SetFavoriteAsync: Enemy {Id} not found", enemyId);
            }
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<int> GetTotalCountAsync(CancellationToken ct = default)
    {
        await EnsureLoadedAsync(ct).ConfigureAwait(false);
        await _lock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            return _enemies.Count;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc/>
    /// <remarks>
    /// <para>
    /// This method writes the entire database to disk atomically. It should be called:
    /// <list type="bullet">
    ///   <item><description>After processing a batch of combat log events</description></item>
    ///   <item><description>When the plugin is being unloaded</description></item>
    ///   <item><description>Periodically (e.g., every 5 minutes) to prevent data loss</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// If the data directory does not exist, it will be created automatically.
    /// </para>
    /// </remarks>
    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            if (!_isDirty)
            {
                _logger.LogTrace("SaveChangesAsync: No changes to save");
                return;
            }

            var directory = Path.GetDirectoryName(_databasePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
                _logger.LogDebug("SaveChangesAsync: Created directory {Directory}", directory);
            }

            var json = JsonSerializer.Serialize(_enemies, _jsonOptions);
            await File.WriteAllTextAsync(_databasePath, json, ct).ConfigureAwait(false);
            _isDirty = false;

            _logger.LogInformation(
                "SaveChangesAsync: Persisted {Count} enemies to {Path}",
                _enemies.Count,
                _databasePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SaveChangesAsync: Failed to save database to {Path}", _databasePath);
            throw;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>
    /// Disposes resources and saves any pending changes.
    /// </summary>
    /// <remarks>
    /// This method performs a best-effort save of any unsaved changes.
    /// If the save fails, the exception is logged but not rethrown.
    /// </remarks>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        // Best-effort save on dispose
        if (_isDirty)
        {
            _logger.LogDebug("Dispose: Saving pending changes");
            try
            {
                SaveChangesAsync().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Dispose: Failed to save pending changes");
            }
        }

        _lock.Dispose();
        _logger.LogDebug("JsonEnemyDatabase disposed");
    }

    /// <summary>
    /// Ensures the database has been loaded from disk.
    /// </summary>
    /// <remarks>
    /// Uses double-checked locking pattern for thread-safe lazy initialization.
    /// </remarks>
    private async Task EnsureLoadedAsync(CancellationToken ct = default)
    {
        if (_isLoaded) return;

        await _lock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            // Double-check after acquiring lock
            if (_isLoaded) return;

            if (File.Exists(_databasePath))
            {
                try
                {
                    var json = await File.ReadAllTextAsync(_databasePath, ct).ConfigureAwait(false);
                    _enemies = JsonSerializer.Deserialize<Dictionary<string, EnemyRecord>>(
                        json, _jsonOptions) ?? new();

                    _logger.LogInformation(
                        "EnsureLoadedAsync: Loaded {Count} enemies from {Path}",
                        _enemies.Count,
                        _databasePath);
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, "EnsureLoadedAsync: Failed to parse database, starting fresh");
                    _enemies = new Dictionary<string, EnemyRecord>();
                }
            }
            else
            {
                _logger.LogDebug("EnsureLoadedAsync: No existing database at {Path}", _databasePath);
            }

            _isLoaded = true;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>
    /// Applies sorting to the query based on the specified criteria.
    /// </summary>
    /// <param name="query">The query to sort.</param>
    /// <param name="sortBy">The property to sort by.</param>
    /// <param name="descending">Whether to sort in descending order.</param>
    /// <returns>The sorted query.</returns>
    private static IEnumerable<EnemyRecord> ApplySorting(
        IEnumerable<EnemyRecord> query,
        EnemySortBy sortBy,
        bool descending)
    {
        return sortBy switch
        {
            EnemySortBy.LastSeen => descending
                ? query.OrderByDescending(e => e.LastSeen)
                : query.OrderBy(e => e.LastSeen),
            EnemySortBy.FirstSeen => descending
                ? query.OrderByDescending(e => e.FirstSeen)
                : query.OrderBy(e => e.FirstSeen),
            EnemySortBy.EncounterCount => descending
                ? query.OrderByDescending(e => e.EncounterCount)
                : query.OrderBy(e => e.EncounterCount),
            EnemySortBy.Name => descending
                ? query.OrderByDescending(e => e.Name)
                : query.OrderBy(e => e.Name),
            EnemySortBy.DamageDealt => descending
                ? query.OrderByDescending(e => e.Statistics.TotalDamageDealt)
                : query.OrderBy(e => e.Statistics.TotalDamageDealt),
            EnemySortBy.DamageTaken => descending
                ? query.OrderByDescending(e => e.Statistics.TotalDamageTaken)
                : query.OrderBy(e => e.Statistics.TotalDamageTaken),
            EnemySortBy.WinRate => descending
                ? query.OrderByDescending(e => e.Statistics.WinRate)
                : query.OrderBy(e => e.Statistics.WinRate),
            EnemySortBy.Kills => descending
                ? query.OrderByDescending(e => e.Statistics.TotalKills)
                : query.OrderBy(e => e.Statistics.TotalKills),
            _ => query.OrderByDescending(e => e.LastSeen)
        };
    }

    /// <summary>
    /// Merges two dictionaries, summing values for matching keys.
    /// </summary>
    /// <param name="existing">The existing dictionary.</param>
    /// <param name="additions">The additions to merge.</param>
    /// <returns>A new dictionary with merged values.</returns>
    private static Dictionary<string, long> MergeDictionaries(
        IReadOnlyDictionary<string, long> existing,
        Dictionary<string, long> additions)
    {
        var result = new Dictionary<string, long>(existing);
        foreach (var (key, value) in additions)
        {
            result[key] = result.GetValueOrDefault(key) + value;
        }
        return result;
    }
}
