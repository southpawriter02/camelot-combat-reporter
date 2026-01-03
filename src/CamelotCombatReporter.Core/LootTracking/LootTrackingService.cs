using System.Diagnostics;
using System.Text.Json;
using CamelotCombatReporter.Core.Logging;
using CamelotCombatReporter.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace CamelotCombatReporter.Core.LootTracking;

/// <summary>
/// Implementation of loot tracking service with JSON file storage.
/// </summary>
public class LootTrackingService : ILootTrackingService, IDisposable
{
    private readonly string _dataDirectory;
    private readonly string _sessionsDirectory;
    private readonly string _indexPath;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly ILogger<LootTrackingService> _logger;

    private SessionIndex? _sessionIndex;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private bool _disposed;

    // Aggregated data caches
    private Dictionary<string, MobAggregateData> _mobData = new(StringComparer.OrdinalIgnoreCase);
    private Dictionary<string, ItemAggregateData> _itemData = new(StringComparer.OrdinalIgnoreCase);
    private bool _isInitialized;

    /// <summary>
    /// Creates a new LootTrackingService with optional data directory and logger.
    /// </summary>
    /// <param name="dataDirectory">Custom data directory, or null for default.</param>
    /// <param name="logger">Optional logger instance.</param>
    public LootTrackingService(string? dataDirectory = null, ILogger<LootTrackingService>? logger = null)
    {
        _logger = logger ?? NullLogger<LootTrackingService>.Instance;
        _dataDirectory = dataDirectory ?? GetDefaultDataDirectory();
        _sessionsDirectory = Path.Combine(_dataDirectory, "sessions");
        _indexPath = Path.Combine(_dataDirectory, "index.json");

        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        // Ensure directories exist
        Directory.CreateDirectory(_dataDirectory);
        Directory.CreateDirectory(_sessionsDirectory);

        _logger.LogServiceInitializing(nameof(LootTrackingService));
    }

    private static string GetDefaultDataDirectory()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(appData, "CamelotCombatReporter", "loot-tracking");
    }

    private async Task EnsureInitializedAsync(CancellationToken ct = default)
    {
        if (_isInitialized) return;

        await _lock.WaitAsync(ct);
        try
        {
            if (_isInitialized) return;

            var sw = Stopwatch.StartNew();
            await LoadIndexAsync(ct);
            await RebuildCachesAsync(ct);
            _isInitialized = true;
            sw.Stop();
            _logger.LogServiceInitialized(nameof(LootTrackingService), sw.ElapsedMilliseconds);
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task LoadIndexAsync(CancellationToken ct)
    {
        if (File.Exists(_indexPath))
        {
            var json = await File.ReadAllTextAsync(_indexPath, ct);
            _sessionIndex = JsonSerializer.Deserialize<SessionIndex>(json, _jsonOptions) ?? new SessionIndex();
        }
        else
        {
            _sessionIndex = new SessionIndex();
        }
    }

    private async Task SaveIndexAsync(CancellationToken ct)
    {
        var json = JsonSerializer.Serialize(_sessionIndex, _jsonOptions);
        await File.WriteAllTextAsync(_indexPath, json, ct);
    }

    private async Task RebuildCachesAsync(CancellationToken ct)
    {
        _mobData.Clear();
        _itemData.Clear();

        if (_sessionIndex == null) return;

        var sw = Stopwatch.StartNew();
        _logger.LogCacheRebuildStarted(_sessionIndex.Sessions.Count);

        foreach (var entry in _sessionIndex.Sessions)
        {
            var sessionPath = GetSessionPath(entry.Id);
            if (!File.Exists(sessionPath)) continue;

            try
            {
                var json = await File.ReadAllTextAsync(sessionPath, ct);
                var sessionData = JsonSerializer.Deserialize<LootSessionData>(json, _jsonOptions);
                if (sessionData != null)
                {
                    ProcessSessionForCaches(sessionData);
                }
            }
            catch (Exception ex)
            {
                _logger.LogCorruptedSessionFile(sessionPath, ex);
            }
        }

        sw.Stop();
        _logger.LogCacheRebuildCompleted(sw.ElapsedMilliseconds);
    }

    private void ProcessSessionForCaches(LootSessionData session)
    {
        // Track mob kills and drops
        var mobKillsInSession = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var evt in session.Events)
        {
            switch (evt.EventType)
            {
                case "ItemDrop" when evt.MobName != null && evt.ItemName != null:
                    // Track this as a kill for the mob
                    if (!mobKillsInSession.Contains(evt.MobName))
                    {
                        mobKillsInSession.Add(evt.MobName);
                        GetOrCreateMobData(evt.MobName).TotalKills++;
                    }

                    // Track the item drop
                    GetOrCreateMobData(evt.MobName).AddItemDrop(evt.ItemName, evt.IsNamedItem ?? false);
                    GetOrCreateItemData(evt.ItemName).AddDrop(evt.MobName);
                    break;

                case "CurrencyDrop" when evt.Gold != null || evt.Silver != null || evt.Copper != null:
                    var copper = (evt.Gold ?? 0) * 10000 + (evt.Silver ?? 0) * 100 + (evt.Copper ?? 0);
                    if (evt.MobName != null)
                    {
                        GetOrCreateMobData(evt.MobName).AddCurrencyDrop(copper);
                    }
                    break;
            }
        }
    }

    private MobAggregateData GetOrCreateMobData(string mobName)
    {
        if (!_mobData.TryGetValue(mobName, out var data))
        {
            data = new MobAggregateData { MobName = mobName };
            _mobData[mobName] = data;
        }
        return data;
    }

    private ItemAggregateData GetOrCreateItemData(string itemName)
    {
        if (!_itemData.TryGetValue(itemName, out var data))
        {
            data = new ItemAggregateData { ItemName = itemName };
            _itemData[itemName] = data;
        }
        return data;
    }

    private string GetSessionPath(Guid sessionId)
    {
        return Path.Combine(_sessionsDirectory, $"{sessionId}.json");
    }

    #region ILootTrackingService Implementation

    public async Task<LootSessionSummary> SaveSessionAsync(
        IReadOnlyList<LootEvent> events,
        string logFilePath,
        CancellationToken ct = default)
    {
        await EnsureInitializedAsync(ct);

        _logger.LogSavingLootSession(events.Count, logFilePath);
        var sessionId = Guid.NewGuid();
        var now = DateTime.Now;

        // Determine session time range from events
        var startTime = events.Count > 0
            ? now.Date.Add(events.First().Timestamp.ToTimeSpan())
            : now;
        var endTime = events.Count > 0
            ? now.Date.Add(events.Last().Timestamp.ToTimeSpan())
            : now;

        // Serialize events
        var serializedEvents = events.Select(SerializedLootEvent.FromLootEvent).ToList();

        var sessionData = new LootSessionData(
            sessionId,
            startTime,
            endTime,
            logFilePath,
            serializedEvents);

        // Save session file
        var sessionPath = GetSessionPath(sessionId);
        var json = JsonSerializer.Serialize(sessionData, _jsonOptions);
        await File.WriteAllTextAsync(sessionPath, json, ct);

        // Calculate summary
        var itemDrops = events.OfType<ItemDropEvent>().ToList();
        var currencyDrops = events.OfType<CurrencyDropEvent>().ToList();
        var bonusCurrency = events.OfType<BonusCurrencyEvent>().ToList();

        var totalCurrency = currencyDrops.Sum(c => c.TotalCopper) +
                           bonusCurrency.Sum(b => b.Gold * 10000 + b.Silver * 100 + b.Copper);

        var uniqueMobs = itemDrops.Select(d => d.MobName).Distinct(StringComparer.OrdinalIgnoreCase).Count();
        var uniqueItems = itemDrops.Select(d => d.ItemName).Distinct(StringComparer.OrdinalIgnoreCase).Count();
        var notableDrops = itemDrops.Where(d => d.IsNamedItem).Select(d => d.ItemName).Distinct().ToList();

        var summary = new LootSessionSummary(
            sessionId,
            startTime,
            endTime,
            itemDrops.Count,
            totalCurrency,
            uniqueMobs,
            uniqueItems,
            notableDrops);

        // Update index
        await _lock.WaitAsync(ct);
        try
        {
            _sessionIndex ??= new SessionIndex();
            _sessionIndex.Sessions.Add(new SessionIndexEntry(
                sessionId,
                startTime,
                endTime,
                logFilePath,
                itemDrops.Count,
                totalCurrency));

            await SaveIndexAsync(ct);

            // Update caches
            ProcessSessionForCaches(sessionData);
        }
        finally
        {
            _lock.Release();
        }

        var currencyStr = $"{summary.TotalCurrencyCopper / 10000}g {summary.TotalCurrencyCopper % 10000 / 100}s {summary.TotalCurrencyCopper % 100}c";
        _logger.LogLootSessionSaved(sessionId, summary.TotalItemDrops, currencyStr);

        return summary;
    }

    public async Task<LootSessionSummary?> GetSessionAsync(Guid sessionId, CancellationToken ct = default)
    {
        await EnsureInitializedAsync(ct);

        var entry = _sessionIndex?.Sessions.FirstOrDefault(s => s.Id == sessionId);
        if (entry == null) return null;

        var sessionPath = GetSessionPath(sessionId);
        if (!File.Exists(sessionPath)) return null;

        var json = await File.ReadAllTextAsync(sessionPath, ct);
        var sessionData = JsonSerializer.Deserialize<LootSessionData>(json, _jsonOptions);
        if (sessionData == null) return null;

        // Rebuild summary from session data
        var itemDropEvents = sessionData.Events
            .Where(e => e.EventType == "ItemDrop")
            .ToList();

        var notableDrops = itemDropEvents
            .Where(e => e.IsNamedItem == true)
            .Select(e => e.ItemName!)
            .Distinct()
            .ToList();

        return new LootSessionSummary(
            sessionId,
            sessionData.StartTime,
            sessionData.EndTime,
            itemDropEvents.Count,
            entry.TotalCurrency,
            itemDropEvents.Select(e => e.MobName).Distinct(StringComparer.OrdinalIgnoreCase).Count(),
            itemDropEvents.Select(e => e.ItemName).Distinct(StringComparer.OrdinalIgnoreCase).Count(),
            notableDrops);
    }

    public async Task<IReadOnlyList<LootSessionSummary>> GetRecentSessionsAsync(int limit = 10, CancellationToken ct = default)
    {
        await EnsureInitializedAsync(ct);

        if (_sessionIndex == null) return Array.Empty<LootSessionSummary>();

        var results = new List<LootSessionSummary>();
        var entries = _sessionIndex.Sessions
            .OrderByDescending(s => s.EndTime)
            .Take(limit);

        foreach (var entry in entries)
        {
            var summary = await GetSessionAsync(entry.Id, ct);
            if (summary != null)
            {
                results.Add(summary);
            }
        }

        return results;
    }

    public async Task DeleteSessionAsync(Guid sessionId, CancellationToken ct = default)
    {
        await EnsureInitializedAsync(ct);

        await _lock.WaitAsync(ct);
        try
        {
            // Remove from index
            var entry = _sessionIndex?.Sessions.FirstOrDefault(s => s.Id == sessionId);
            if (entry != null)
            {
                _sessionIndex?.Sessions.Remove(entry);
                await SaveIndexAsync(ct);
            }

            // Delete session file
            var sessionPath = GetSessionPath(sessionId);
            if (File.Exists(sessionPath))
            {
                File.Delete(sessionPath);
            }

            // Rebuild caches
            await RebuildCachesAsync(ct);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<MobLootTable?> GetMobLootTableAsync(string mobName, CancellationToken ct = default)
    {
        await EnsureInitializedAsync(ct);

        if (!_mobData.TryGetValue(mobName, out var data))
        {
            return null;
        }

        return data.ToMobLootTable();
    }

    public async Task<IReadOnlyList<MobLootTable>> SearchMobsAsync(
        string? query = null,
        string sortBy = "kills",
        int limit = 50,
        CancellationToken ct = default)
    {
        await EnsureInitializedAsync(ct);

        var mobs = _mobData.Values.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(query))
        {
            mobs = mobs.Where(m =>
                m.MobName.Contains(query, StringComparison.OrdinalIgnoreCase));
        }

        mobs = sortBy.ToLowerInvariant() switch
        {
            "name" => mobs.OrderBy(m => m.MobName),
            "lastseen" => mobs.OrderByDescending(m => m.LastSeen),
            _ => mobs.OrderByDescending(m => m.TotalKills)
        };

        return mobs
            .Take(limit)
            .Select(m => m.ToMobLootTable())
            .ToList();
    }

    public async Task<int> GetTotalKillsAsync(string mobName, CancellationToken ct = default)
    {
        await EnsureInitializedAsync(ct);

        return _mobData.TryGetValue(mobName, out var data) ? data.TotalKills : 0;
    }

    public async Task<IReadOnlyList<ItemDropStatistic>> GetItemSourcesAsync(string itemName, CancellationToken ct = default)
    {
        await EnsureInitializedAsync(ct);

        if (!_itemData.TryGetValue(itemName, out var data))
        {
            return Array.Empty<ItemDropStatistic>();
        }

        var results = new List<ItemDropStatistic>();

        foreach (var (mobName, dropCount) in data.DropsByMob)
        {
            if (_mobData.TryGetValue(mobName, out var mobData))
            {
                var stat = ItemDropStatistic.Create(
                    itemName,
                    dropCount,
                    mobData.TotalKills,
                    data.FirstSeen,
                    data.LastSeen);
                results.Add(stat);
            }
        }

        return results.OrderByDescending(s => s.DropRate).ToList();
    }

    public async Task<IReadOnlyList<ItemDropStatistic>> SearchItemsAsync(
        string? query = null,
        string sortBy = "drops",
        int limit = 50,
        CancellationToken ct = default)
    {
        await EnsureInitializedAsync(ct);

        var items = _itemData.Values.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(query))
        {
            items = items.Where(i =>
                i.ItemName.Contains(query, StringComparison.OrdinalIgnoreCase));
        }

        items = sortBy.ToLowerInvariant() switch
        {
            "name" => items.OrderBy(i => i.ItemName),
            "rate" => items.OrderByDescending(i => i.GetBestDropRate(_mobData)),
            _ => items.OrderByDescending(i => i.TotalDrops)
        };

        return items
            .Take(limit)
            .Select(i => i.ToBestStatistic(_mobData))
            .ToList();
    }

    public async Task<LootTrackingStats> GetOverallStatsAsync(CancellationToken ct = default)
    {
        await EnsureInitializedAsync(ct);

        if (_sessionIndex == null || _sessionIndex.Sessions.Count == 0)
        {
            return LootTrackingStats.Empty;
        }

        var totalCurrency = _sessionIndex.Sessions.Sum(s => s.TotalCurrency);
        var totalItemDrops = _sessionIndex.Sessions.Sum(s => s.ItemDropCount);

        return new LootTrackingStats(
            TotalSessions: _sessionIndex.Sessions.Count,
            TotalMobsTracked: _mobData.Count,
            TotalItemsTracked: _itemData.Count,
            TotalKills: _mobData.Values.Sum(m => m.TotalKills),
            TotalItemDrops: totalItemDrops,
            TotalCurrencyEarned: totalCurrency,
            FirstSession: _sessionIndex.Sessions.Min(s => s.StartTime),
            LastSession: _sessionIndex.Sessions.Max(s => s.EndTime));
    }

    public async Task RebuildStatisticsAsync(CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            await RebuildCachesAsync(ct);
        }
        finally
        {
            _lock.Release();
        }
    }

    #endregion

    #region Internal Data Classes

    private class SessionIndex
    {
        public List<SessionIndexEntry> Sessions { get; set; } = new();
    }

    private record SessionIndexEntry(
        Guid Id,
        DateTime StartTime,
        DateTime EndTime,
        string LogFilePath,
        int ItemDropCount,
        long TotalCurrency);

    private class MobAggregateData
    {
        public required string MobName { get; init; }
        public int TotalKills { get; set; }
        public DateTime FirstSeen { get; set; } = DateTime.MaxValue;
        public DateTime LastSeen { get; set; } = DateTime.MinValue;
        public Dictionary<string, ItemDropData> ItemDrops { get; } = new(StringComparer.OrdinalIgnoreCase);
        public List<int> CurrencyDrops { get; } = new();

        public void AddItemDrop(string itemName, bool isNamed)
        {
            var now = DateTime.Now;
            if (now < FirstSeen) FirstSeen = now;
            if (now > LastSeen) LastSeen = now;

            if (!ItemDrops.TryGetValue(itemName, out var data))
            {
                data = new ItemDropData { ItemName = itemName, IsNamed = isNamed };
                ItemDrops[itemName] = data;
            }
            data.DropCount++;
            if (now < data.FirstSeen) data.FirstSeen = now;
            if (now > data.LastSeen) data.LastSeen = now;
        }

        public void AddCurrencyDrop(int copperValue)
        {
            CurrencyDrops.Add(copperValue);
        }

        public MobLootTable ToMobLootTable()
        {
            var items = ItemDrops.Values
                .Select(d => ItemDropStatistic.Create(
                    d.ItemName,
                    d.DropCount,
                    TotalKills,
                    d.FirstSeen,
                    d.LastSeen))
                .OrderByDescending(s => s.DropRate)
                .ToList();

            var currency = CurrencyDrops.Count > 0
                ? new CurrencyStatistic(
                    CurrencyDrops.Count,
                    CurrencyDrops.Sum(c => (long)c),
                    CurrencyDrops.Average(),
                    CurrencyDrops.Min(),
                    CurrencyDrops.Max())
                : new CurrencyStatistic(0, 0, 0, 0, 0);

            return new MobLootTable(
                MobName,
                TotalKills,
                items,
                currency,
                FirstSeen,
                LastSeen);
        }
    }

    private class ItemDropData
    {
        public required string ItemName { get; init; }
        public bool IsNamed { get; init; }
        public int DropCount { get; set; }
        public DateTime FirstSeen { get; set; } = DateTime.MaxValue;
        public DateTime LastSeen { get; set; } = DateTime.MinValue;
    }

    private class ItemAggregateData
    {
        public required string ItemName { get; init; }
        public int TotalDrops { get; set; }
        public DateTime FirstSeen { get; set; } = DateTime.MaxValue;
        public DateTime LastSeen { get; set; } = DateTime.MinValue;
        public Dictionary<string, int> DropsByMob { get; } = new(StringComparer.OrdinalIgnoreCase);

        public void AddDrop(string mobName)
        {
            TotalDrops++;
            var now = DateTime.Now;
            if (now < FirstSeen) FirstSeen = now;
            if (now > LastSeen) LastSeen = now;

            if (!DropsByMob.TryGetValue(mobName, out _))
            {
                DropsByMob[mobName] = 0;
            }
            DropsByMob[mobName]++;
        }

        public double GetBestDropRate(Dictionary<string, MobAggregateData> mobData)
        {
            if (DropsByMob.Count == 0) return 0;

            return DropsByMob.Max(kvp =>
            {
                if (mobData.TryGetValue(kvp.Key, out var mob) && mob.TotalKills > 0)
                {
                    return (double)kvp.Value / mob.TotalKills * 100;
                }
                return 0;
            });
        }

        public ItemDropStatistic ToBestStatistic(Dictionary<string, MobAggregateData> mobData)
        {
            // Find the mob with the best sample size
            var bestMob = DropsByMob
                .Where(kvp => mobData.ContainsKey(kvp.Key))
                .OrderByDescending(kvp => mobData[kvp.Key].TotalKills)
                .FirstOrDefault();

            var totalKills = bestMob.Key != null && mobData.TryGetValue(bestMob.Key, out var mob)
                ? mob.TotalKills
                : 0;

            return ItemDropStatistic.Create(
                ItemName,
                TotalDrops,
                totalKills,
                FirstSeen,
                LastSeen);
        }
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
            _logger.LogServiceDisposing(nameof(LootTrackingService));
            _lock.Dispose();
        }

        _disposed = true;
    }

    #endregion
}
