using System.Diagnostics;
using System.Text.Json;
using CamelotCombatReporter.Core.Logging;
using CamelotCombatReporter.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace CamelotCombatReporter.Core.CrossRealm;

/// <summary>
/// Service for managing cross-realm combat statistics with local JSON storage.
/// </summary>
public class CrossRealmStatisticsService : ICrossRealmStatisticsService, IDisposable
{
    private readonly string _sessionsDirectory;
    private readonly string _indexPath;
    private readonly SemaphoreSlim _indexLock = new(1, 1);
    private readonly ILogger<CrossRealmStatisticsService> _logger;
    private SessionIndex? _cachedIndex;
    private bool _disposed;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Creates a new CrossRealmStatisticsService with default storage location.
    /// </summary>
    /// <param name="logger">Optional logger instance.</param>
    public CrossRealmStatisticsService(ILogger<CrossRealmStatisticsService>? logger = null)
        : this(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "CamelotCombatReporter",
            "cross-realm"), logger)
    {
    }

    /// <summary>
    /// Creates a new CrossRealmStatisticsService with custom storage location.
    /// </summary>
    /// <param name="baseDirectory">Custom base directory for storage.</param>
    /// <param name="logger">Optional logger instance.</param>
    public CrossRealmStatisticsService(string baseDirectory, ILogger<CrossRealmStatisticsService>? logger = null)
    {
        _logger = logger ?? NullLogger<CrossRealmStatisticsService>.Instance;
        _sessionsDirectory = Path.Combine(baseDirectory, "sessions");
        _indexPath = Path.Combine(baseDirectory, "sessions-index.json");
        EnsureDirectoryExists();
        _logger.LogServiceInitializing(nameof(CrossRealmStatisticsService));
    }

    public async Task SaveSessionAsync(ExtendedCombatStatistics stats, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stats);

        _logger.LogSavingCrossRealmSession(stats.Character.Name, stats.Character.Realm.ToString());

        var fileName = GenerateSessionFileName(stats);
        var filePath = Path.Combine(_sessionsDirectory, fileName);

        // Save the session file
        var json = JsonSerializer.Serialize(stats, JsonOptions);
        await File.WriteAllTextAsync(filePath, json, cancellationToken);

        // Update the index
        await _indexLock.WaitAsync(cancellationToken);
        try
        {
            var index = await LoadIndexAsync(cancellationToken);

            // Remove existing entry if session ID exists
            index.Sessions.RemoveAll(s => s.Id == stats.Id);

            // Add new entry
            var entry = CreateIndexEntry(stats, fileName);
            index.Sessions.Add(entry);

            await SaveIndexAsync(index, cancellationToken);
        }
        finally
        {
            _indexLock.Release();
        }

        _logger.LogCrossRealmSessionSaved(stats.Id);
    }

    public async Task<IReadOnlyList<CombatSessionSummary>> GetSessionsAsync(
        Realm? realm = null,
        CharacterClass? characterClass = null,
        DateTime? since = null,
        int? limit = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogLoadingCrossRealmSessions(realm?.ToString(), characterClass?.ToString());
        var index = await LoadIndexAsync(cancellationToken);

        var query = index.Sessions.AsEnumerable();

        if (realm.HasValue)
            query = query.Where(s => s.Realm == realm.Value);

        if (characterClass.HasValue)
            query = query.Where(s => s.Class == characterClass.Value);

        if (since.HasValue)
            query = query.Where(s => s.SessionStartUtc >= since.Value);

        query = query.OrderByDescending(s => s.SessionStartUtc);

        if (limit.HasValue)
            query = query.Take(limit.Value);

        var results = new List<CombatSessionSummary>();
        foreach (var entry in query)
        {
            var session = await LoadSessionFromFileAsync(entry.FileName, cancellationToken);
            if (session != null)
            {
                results.Add(CombatSessionSummary.FromExtended(session));
            }
        }

        return results;
    }

    public async Task<ExtendedCombatStatistics?> GetSessionAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        var index = await LoadIndexAsync(cancellationToken);
        var entry = index.Sessions.FirstOrDefault(s => s.Id == sessionId);

        if (entry == null)
            return null;

        return await LoadSessionFromFileAsync(entry.FileName, cancellationToken);
    }

    public async Task<bool> DeleteSessionAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        await _indexLock.WaitAsync(cancellationToken);
        try
        {
            var index = await LoadIndexAsync(cancellationToken);
            var entry = index.Sessions.FirstOrDefault(s => s.Id == sessionId);

            if (entry == null)
                return false;

            // Delete the file
            var filePath = Path.Combine(_sessionsDirectory, entry.FileName);
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            // Update the index
            index.Sessions.RemoveAll(s => s.Id == sessionId);
            await SaveIndexAsync(index, cancellationToken);

            return true;
        }
        finally
        {
            _indexLock.Release();
        }
    }

    public async Task<RealmStatistics> GetRealmStatisticsAsync(Realm realm, CancellationToken cancellationToken = default)
    {
        var index = await LoadIndexAsync(cancellationToken);
        var realmSessions = index.Sessions.Where(s => s.Realm == realm).ToList();

        if (realmSessions.Count == 0)
            return RealmStatistics.Empty(realm);

        return CalculateRealmStatistics(realm, realmSessions);
    }

    public async Task<ClassStatistics> GetClassStatisticsAsync(CharacterClass characterClass, CancellationToken cancellationToken = default)
    {
        var index = await LoadIndexAsync(cancellationToken);
        var classSessions = index.Sessions.Where(s => s.Class == characterClass).ToList();

        if (classSessions.Count == 0)
            return ClassStatistics.Empty(characterClass);

        return CalculateClassStatistics(characterClass, classSessions);
    }

    public async Task<IReadOnlyList<RealmStatistics>> GetAllRealmStatisticsAsync(CancellationToken cancellationToken = default)
    {
        var index = await LoadIndexAsync(cancellationToken);
        var results = new List<RealmStatistics>();

        foreach (var realm in new[] { Realm.Albion, Realm.Midgard, Realm.Hibernia })
        {
            var realmSessions = index.Sessions.Where(s => s.Realm == realm).ToList();
            results.Add(realmSessions.Count > 0
                ? CalculateRealmStatistics(realm, realmSessions)
                : RealmStatistics.Empty(realm));
        }

        return results;
    }

    public async Task<IReadOnlyList<ClassStatistics>> GetClassStatisticsForRealmAsync(Realm realm, CancellationToken cancellationToken = default)
    {
        var index = await LoadIndexAsync(cancellationToken);
        var results = new List<ClassStatistics>();

        foreach (var characterClass in realm.GetClasses())
        {
            var classSessions = index.Sessions.Where(s => s.Class == characterClass).ToList();
            results.Add(classSessions.Count > 0
                ? CalculateClassStatistics(characterClass, classSessions)
                : ClassStatistics.Empty(characterClass));
        }

        return results;
    }

    public async Task<IReadOnlyList<LeaderboardEntry>> GetLocalLeaderboardAsync(
        string metric,
        Realm? realm = null,
        CharacterClass? characterClass = null,
        int limit = 10,
        CancellationToken cancellationToken = default)
    {
        var index = await LoadIndexAsync(cancellationToken);

        var query = index.Sessions.AsEnumerable();

        if (realm.HasValue)
            query = query.Where(s => s.Realm == realm.Value);

        if (characterClass.HasValue)
            query = query.Where(s => s.Class == characterClass.Value);

        var sessions = query.ToList();
        var entries = new List<(SessionIndexEntry Entry, double Value)>();

        foreach (var session in sessions)
        {
            var value = metric switch
            {
                LeaderboardMetrics.Dps => session.Dps,
                LeaderboardMetrics.Hps => session.Hps,
                LeaderboardMetrics.Kdr => session.Deaths > 0 ? (double)session.Kills / session.Deaths : session.Kills,
                LeaderboardMetrics.TotalDamage => await GetSessionDamageAsync(session, cancellationToken),
                LeaderboardMetrics.TotalHealing => await GetSessionHealingAsync(session, cancellationToken),
                LeaderboardMetrics.Kills => session.Kills,
                _ => 0
            };

            entries.Add((session, value));
        }

        var ranked = entries
            .OrderByDescending(e => e.Value)
            .Take(limit)
            .Select((e, i) => new LeaderboardEntry(
                i + 1,
                new CharacterInfo("", e.Entry.Realm, e.Entry.Class),
                e.Value,
                metric,
                e.Entry.SessionStartUtc,
                e.Entry.Id))
            .ToList();

        // Load character info for each entry
        var results = new List<LeaderboardEntry>();
        foreach (var entry in ranked)
        {
            var session = await GetSessionAsync(entry.SessionId, cancellationToken);
            if (session != null)
            {
                results.Add(entry with { Character = session.Character });
            }
            else
            {
                results.Add(entry);
            }
        }

        return results;
    }

    public async Task RebuildIndexAsync(CancellationToken cancellationToken = default)
    {
        await _indexLock.WaitAsync(cancellationToken);
        try
        {
            var newIndex = SessionIndex.Empty();

            if (!Directory.Exists(_sessionsDirectory))
            {
                await SaveIndexAsync(newIndex, cancellationToken);
                return;
            }

            var files = Directory.GetFiles(_sessionsDirectory, "*.json");
            _logger.LogIndexRebuildStarted(files.Length);

            foreach (var file in files)
            {
                try
                {
                    var json = await File.ReadAllTextAsync(file, cancellationToken);
                    var session = JsonSerializer.Deserialize<ExtendedCombatStatistics>(json, JsonOptions);

                    if (session != null)
                    {
                        var entry = CreateIndexEntry(session, Path.GetFileName(file));
                        newIndex.Sessions.Add(entry);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogCorruptedSessionFile(file, ex);
                }
            }

            await SaveIndexAsync(newIndex, cancellationToken);
        }
        finally
        {
            _indexLock.Release();
        }
    }

    public async Task<int> GetSessionCountAsync(CancellationToken cancellationToken = default)
    {
        var index = await LoadIndexAsync(cancellationToken);
        return index.Sessions.Count;
    }

    #region Private Methods

    private void EnsureDirectoryExists()
    {
        if (!Directory.Exists(_sessionsDirectory))
        {
            Directory.CreateDirectory(_sessionsDirectory);
        }
    }

    private async Task<SessionIndex> LoadIndexAsync(CancellationToken cancellationToken)
    {
        if (_cachedIndex != null)
            return _cachedIndex;

        if (!File.Exists(_indexPath))
        {
            _cachedIndex = SessionIndex.Empty();
            return _cachedIndex;
        }

        try
        {
            var json = await File.ReadAllTextAsync(_indexPath, cancellationToken);
            _cachedIndex = JsonSerializer.Deserialize<SessionIndex>(json, JsonOptions) ?? SessionIndex.Empty();
            return _cachedIndex;
        }
        catch (Exception ex)
        {
            _logger.LogIndexLoadFailed(_indexPath, ex);
            _cachedIndex = SessionIndex.Empty();
            return _cachedIndex;
        }
    }

    private async Task SaveIndexAsync(SessionIndex index, CancellationToken cancellationToken)
    {
        var updatedIndex = index with { LastUpdatedUtc = DateTime.UtcNow };
        var json = JsonSerializer.Serialize(updatedIndex, JsonOptions);
        await File.WriteAllTextAsync(_indexPath, json, cancellationToken);
        _cachedIndex = updatedIndex;
    }

    private async Task<ExtendedCombatStatistics?> LoadSessionFromFileAsync(string fileName, CancellationToken cancellationToken)
    {
        var filePath = Path.Combine(_sessionsDirectory, fileName);
        if (!File.Exists(filePath))
            return null;

        try
        {
            var json = await File.ReadAllTextAsync(filePath, cancellationToken);
            return JsonSerializer.Deserialize<ExtendedCombatStatistics>(json, JsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogCrossRealmSessionLoadError(filePath, ex);
            return null;
        }
    }

    private static string GenerateSessionFileName(ExtendedCombatStatistics stats)
    {
        var timestamp = stats.SessionStartUtc.ToString("yyyyMMdd_HHmmss");
        var realm = stats.Character.Realm.ToString().ToLowerInvariant();
        var characterClass = stats.Character.Class.ToString().ToLowerInvariant();
        return $"{timestamp}_{realm}_{characterClass}_{stats.Id:N}.json";
    }

    private static SessionIndexEntry CreateIndexEntry(ExtendedCombatStatistics stats, string fileName)
    {
        return new SessionIndexEntry(
            stats.Id,
            stats.SessionStartUtc,
            stats.Character.Realm,
            stats.Character.Class,
            stats.BaseStats.Dps,
            stats.Hps,
            stats.KillCount,
            stats.DeathCount,
            fileName);
    }

    private static RealmStatistics CalculateRealmStatistics(Realm realm, List<SessionIndexEntry> sessions)
    {
        var dpsValues = sessions.Select(s => s.Dps).OrderBy(v => v).ToList();
        var hpsValues = sessions.Select(s => s.Hps).OrderBy(v => v).ToList();
        var kdrValues = sessions.Select(s => s.Deaths > 0 ? (double)s.Kills / s.Deaths : s.Kills).ToList();

        return new RealmStatistics(
            realm,
            sessions.Count,
            dpsValues.Average(),
            CalculateMedian(dpsValues),
            dpsValues.Max(),
            hpsValues.Average(),
            CalculateMedian(hpsValues),
            hpsValues.Max(),
            kdrValues.Average(),
            0, // Total damage would require loading all sessions
            0, // Total healing would require loading all sessions
            sessions.Sum(s => s.Kills),
            sessions.Sum(s => s.Deaths));
    }

    private static ClassStatistics CalculateClassStatistics(CharacterClass characterClass, List<SessionIndexEntry> sessions)
    {
        var dpsValues = sessions.Select(s => s.Dps).OrderBy(v => v).ToList();
        var hpsValues = sessions.Select(s => s.Hps).OrderBy(v => v).ToList();
        var kdrValues = sessions.Select(s => s.Deaths > 0 ? (double)s.Kills / s.Deaths : s.Kills).ToList();

        return new ClassStatistics(
            characterClass,
            characterClass.GetRealm(),
            sessions.Count,
            dpsValues.Average(),
            CalculateMedian(dpsValues),
            dpsValues.Max(),
            hpsValues.Average(),
            CalculateMedian(hpsValues),
            hpsValues.Max(),
            kdrValues.Average(),
            0, // Total damage would require loading all sessions
            0  // Total healing would require loading all sessions
        );
    }

    private static double CalculateMedian(List<double> sortedValues)
    {
        if (sortedValues.Count == 0)
            return 0;

        var mid = sortedValues.Count / 2;
        return sortedValues.Count % 2 != 0
            ? sortedValues[mid]
            : (sortedValues[mid - 1] + sortedValues[mid]) / 2.0;
    }

    private async Task<double> GetSessionDamageAsync(SessionIndexEntry entry, CancellationToken cancellationToken)
    {
        var session = await LoadSessionFromFileAsync(entry.FileName, cancellationToken);
        return session?.TotalDamageDealt ?? 0;
    }

    private async Task<double> GetSessionHealingAsync(SessionIndexEntry entry, CancellationToken cancellationToken)
    {
        var session = await LoadSessionFromFileAsync(entry.FileName, cancellationToken);
        return session?.TotalHealingDone ?? 0;
    }

    #endregion

    #region IDisposable

    /// <summary>
    /// Disposes resources used by the service.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Disposes resources used by the service.
    /// </summary>
    /// <param name="disposing">True if called from Dispose, false if from finalizer.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;

        if (disposing)
        {
            _logger.LogServiceDisposing(nameof(CrossRealmStatisticsService));
            _indexLock.Dispose();
        }

        _disposed = true;
    }

    #endregion
}
