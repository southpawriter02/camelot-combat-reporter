using System.Globalization;
using System.Text;
using System.Text.Json;
using CamelotCombatReporter.Core.Models;

namespace CamelotCombatReporter.Core.CrossRealm;

/// <summary>
/// Options for exporting cross-realm statistics.
/// </summary>
/// <param name="RealmFilter">
/// Filter exports to a specific realm. When null, includes all realms.
/// </param>
/// <param name="ClassFilter">
/// Filter exports to a specific character class. When null, includes all classes.
/// </param>
/// <param name="Since">
/// Only include sessions starting on or after this date. When null, no start date filter.
/// </param>
/// <param name="Until">
/// Only include sessions ending on or before this date. When null, no end date filter.
/// </param>
/// <param name="IncludeCharacterNames">
/// Whether to include character names in the export. Defaults to false for privacy.
/// </param>
/// <param name="AggregateOnly">
/// When true, only exports aggregated statistics. When false, includes individual session data.
/// Defaults to true.
/// </param>
public record ExportOptions(
    Realm? RealmFilter = null,
    CharacterClass? ClassFilter = null,
    DateTime? Since = null,
    DateTime? Until = null,
    bool IncludeCharacterNames = false,
    bool AggregateOnly = true
);

/// <summary>
/// Exports cross-realm statistics to various formats.
/// </summary>
public class CrossRealmExporter
{
    private readonly ICrossRealmStatisticsService _statisticsService;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public CrossRealmExporter(ICrossRealmStatisticsService statisticsService)
    {
        _statisticsService = statisticsService ?? throw new ArgumentNullException(nameof(statisticsService));
    }

    /// <summary>
    /// Exports statistics to JSON format.
    /// </summary>
    public async Task ExportToJsonAsync(Stream output, ExportOptions options, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(output);
        options ??= new ExportOptions();

        var exportData = await BuildExportDataAsync(options, cancellationToken);
        var json = JsonSerializer.Serialize(exportData, JsonOptions);

        await using var writer = new StreamWriter(output, Encoding.UTF8, leaveOpen: true);
        await writer.WriteAsync(json);
    }

    /// <summary>
    /// Exports statistics to CSV format.
    /// </summary>
    public async Task ExportToCsvAsync(Stream output, ExportOptions options, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(output);
        options ??= new ExportOptions();

        await using var writer = new StreamWriter(output, Encoding.UTF8, leaveOpen: true);

        if (options.AggregateOnly)
        {
            await WriteAggregatedCsvAsync(writer, options, cancellationToken);
        }
        else
        {
            await WriteDetailedCsvAsync(writer, options, cancellationToken);
        }
    }

    /// <summary>
    /// Gets export data as an object for custom serialization.
    /// </summary>
    public async Task<CrossRealmExportData> BuildExportDataAsync(ExportOptions options, CancellationToken cancellationToken = default)
    {
        options ??= new ExportOptions();

        var realmStats = await _statisticsService.GetAllRealmStatisticsAsync(cancellationToken);
        var classStats = new List<ClassStatistics>();

        // Get class stats for each realm if no filter
        if (!options.RealmFilter.HasValue)
        {
            foreach (var realm in new[] { Realm.Albion, Realm.Midgard, Realm.Hibernia })
            {
                var realmClassStats = await _statisticsService.GetClassStatisticsForRealmAsync(realm, cancellationToken);
                classStats.AddRange(realmClassStats.Where(c => c.SessionCount > 0));
            }
        }
        else
        {
            var realmClassStats = await _statisticsService.GetClassStatisticsForRealmAsync(options.RealmFilter.Value, cancellationToken);
            classStats.AddRange(realmClassStats.Where(c => c.SessionCount > 0));
        }

        // Filter by class if specified
        if (options.ClassFilter.HasValue)
        {
            classStats = classStats.Where(c => c.Class == options.ClassFilter.Value).ToList();
        }

        // Get sessions if not aggregate only
        List<CombatSessionExport>? sessions = null;
        if (!options.AggregateOnly)
        {
            var sessionList = await _statisticsService.GetSessionsAsync(
                options.RealmFilter,
                options.ClassFilter,
                options.Since,
                cancellationToken: cancellationToken);

            // Filter by Until date
            if (options.Until.HasValue)
            {
                sessionList = sessionList.Where(s => s.SessionStartUtc <= options.Until.Value).ToList();
            }

            sessions = sessionList.Select(s => new CombatSessionExport(
                s.Id,
                s.SessionStartUtc,
                options.IncludeCharacterNames ? s.Character.Name : null,
                s.Character.Realm,
                s.Character.Class,
                s.DurationMinutes,
                s.Dps,
                s.Hps,
                s.Kills,
                s.Deaths
            )).ToList();
        }

        // Get leaderboards
        var leaderboards = new Dictionary<string, List<LeaderboardExport>>();
        foreach (var metric in LeaderboardMetrics.All)
        {
            var entries = await _statisticsService.GetLocalLeaderboardAsync(
                metric,
                options.RealmFilter,
                options.ClassFilter,
                10,
                cancellationToken);

            leaderboards[metric] = entries.Select(e => new LeaderboardExport(
                e.Rank,
                options.IncludeCharacterNames ? e.Character.Name : null,
                e.Character.Realm,
                e.Character.Class,
                e.Value,
                e.SessionDateUtc
            )).ToList();
        }

        return new CrossRealmExportData(
            DateTime.UtcNow,
            "1.0",
            new ExportMetadata(
                options.RealmFilter?.ToString(),
                options.ClassFilter?.ToString(),
                options.Since,
                options.Until,
                options.AggregateOnly),
            realmStats
                .Where(r => !options.RealmFilter.HasValue || r.Realm == options.RealmFilter.Value)
                .Select(r => new RealmStatisticsExport(
                    r.Realm,
                    r.SessionCount,
                    r.AverageDps,
                    r.MedianDps,
                    r.MaxDps,
                    r.AverageHps,
                    r.MedianHps,
                    r.MaxHps,
                    r.AverageKdr,
                    r.TotalKills,
                    r.TotalDeaths))
                .ToList(),
            classStats.Select(c => new ClassStatisticsExport(
                c.Class,
                c.Realm,
                c.SessionCount,
                c.AverageDps,
                c.MedianDps,
                c.MaxDps,
                c.AverageHps,
                c.MedianHps,
                c.MaxHps,
                c.AverageKdr))
                .ToList(),
            leaderboards,
            sessions);
    }

    private async Task WriteAggregatedCsvAsync(StreamWriter writer, ExportOptions options, CancellationToken cancellationToken)
    {
        // Write realm statistics
        await writer.WriteLineAsync("=== Realm Statistics ===");
        await writer.WriteLineAsync("Realm,Sessions,Avg DPS,Median DPS,Max DPS,Avg HPS,Median HPS,Max HPS,Avg KDR,Total Kills,Total Deaths");

        var realmStats = await _statisticsService.GetAllRealmStatisticsAsync(cancellationToken);
        foreach (var stat in realmStats.Where(r => !options.RealmFilter.HasValue || r.Realm == options.RealmFilter.Value))
        {
            await writer.WriteLineAsync(string.Format(CultureInfo.InvariantCulture,
                "{0},{1},{2:F2},{3:F2},{4:F2},{5:F2},{6:F2},{7:F2},{8:F2},{9},{10}",
                stat.Realm,
                stat.SessionCount,
                stat.AverageDps,
                stat.MedianDps,
                stat.MaxDps,
                stat.AverageHps,
                stat.MedianHps,
                stat.MaxHps,
                stat.AverageKdr,
                stat.TotalKills,
                stat.TotalDeaths));
        }

        await writer.WriteLineAsync();

        // Write class statistics
        await writer.WriteLineAsync("=== Class Statistics ===");
        await writer.WriteLineAsync("Class,Realm,Sessions,Avg DPS,Median DPS,Max DPS,Avg HPS,Median HPS,Max HPS,Avg KDR");

        foreach (var realm in new[] { Realm.Albion, Realm.Midgard, Realm.Hibernia })
        {
            if (options.RealmFilter.HasValue && options.RealmFilter.Value != realm)
                continue;

            var classStats = await _statisticsService.GetClassStatisticsForRealmAsync(realm, cancellationToken);
            foreach (var stat in classStats.Where(c => c.SessionCount > 0 &&
                (!options.ClassFilter.HasValue || c.Class == options.ClassFilter.Value)))
            {
                await writer.WriteLineAsync(string.Format(CultureInfo.InvariantCulture,
                    "{0},{1},{2},{3:F2},{4:F2},{5:F2},{6:F2},{7:F2},{8:F2},{9:F2}",
                    stat.Class.GetDisplayName(),
                    stat.Realm,
                    stat.SessionCount,
                    stat.AverageDps,
                    stat.MedianDps,
                    stat.MaxDps,
                    stat.AverageHps,
                    stat.MedianHps,
                    stat.MaxHps,
                    stat.AverageKdr));
            }
        }
    }

    private async Task WriteDetailedCsvAsync(StreamWriter writer, ExportOptions options, CancellationToken cancellationToken)
    {
        // Write header
        var headerParts = new List<string> { "Session ID", "Date", "Realm", "Class", "Duration (min)", "DPS", "HPS", "Kills", "Deaths", "KDR" };
        if (options.IncludeCharacterNames)
        {
            headerParts.Insert(2, "Character");
        }
        await writer.WriteLineAsync(string.Join(",", headerParts));

        // Get and write sessions
        var sessions = await _statisticsService.GetSessionsAsync(
            options.RealmFilter,
            options.ClassFilter,
            options.Since,
            cancellationToken: cancellationToken);

        foreach (var session in sessions.Where(s => !options.Until.HasValue || s.SessionStartUtc <= options.Until.Value))
        {
            var kdr = session.Deaths > 0 ? (double)session.Kills / session.Deaths : session.Kills;
            var valueParts = new List<string>
            {
                session.Id.ToString(),
                session.SessionStartUtc.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
                session.Character.Realm.ToString(),
                session.Character.Class.GetDisplayName(),
                session.DurationMinutes.ToString("F2", CultureInfo.InvariantCulture),
                session.Dps.ToString("F2", CultureInfo.InvariantCulture),
                session.Hps.ToString("F2", CultureInfo.InvariantCulture),
                session.Kills.ToString(CultureInfo.InvariantCulture),
                session.Deaths.ToString(CultureInfo.InvariantCulture),
                kdr.ToString("F2", CultureInfo.InvariantCulture)
            };

            if (options.IncludeCharacterNames)
            {
                valueParts.Insert(2, EscapeCsvField(session.Character.Name));
            }

            await writer.WriteLineAsync(string.Join(",", valueParts));
        }
    }

    private static string EscapeCsvField(string value)
    {
        if (string.IsNullOrEmpty(value))
            return "";

        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        return value;
    }
}

#region Export Data Models

/// <summary>
/// Root export data structure.
/// </summary>
public record CrossRealmExportData(
    DateTime ExportedAtUtc,
    string Version,
    ExportMetadata Metadata,
    List<RealmStatisticsExport> RealmStatistics,
    List<ClassStatisticsExport> ClassStatistics,
    Dictionary<string, List<LeaderboardExport>> Leaderboards,
    List<CombatSessionExport>? Sessions
);

/// <summary>
/// Export metadata.
/// </summary>
public record ExportMetadata(
    string? RealmFilter,
    string? ClassFilter,
    DateTime? Since,
    DateTime? Until,
    bool AggregateOnly
);

/// <summary>
/// Realm statistics for export.
/// </summary>
public record RealmStatisticsExport(
    Realm Realm,
    int SessionCount,
    double AverageDps,
    double MedianDps,
    double MaxDps,
    double AverageHps,
    double MedianHps,
    double MaxHps,
    double AverageKdr,
    int TotalKills,
    int TotalDeaths
);

/// <summary>
/// Class statistics for export.
/// </summary>
public record ClassStatisticsExport(
    CharacterClass Class,
    Realm Realm,
    int SessionCount,
    double AverageDps,
    double MedianDps,
    double MaxDps,
    double AverageHps,
    double MedianHps,
    double MaxHps,
    double AverageKdr
);

/// <summary>
/// Leaderboard entry for export.
/// </summary>
public record LeaderboardExport(
    int Rank,
    string? CharacterName,
    Realm Realm,
    CharacterClass Class,
    double Value,
    DateTime SessionDateUtc
);

/// <summary>
/// Combat session for export.
/// </summary>
public record CombatSessionExport(
    Guid Id,
    DateTime SessionDateUtc,
    string? CharacterName,
    Realm Realm,
    CharacterClass Class,
    double DurationMinutes,
    double Dps,
    double Hps,
    int Kills,
    int Deaths
);

#endregion
