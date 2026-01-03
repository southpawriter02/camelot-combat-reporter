using System.Text;
using System.Text.Json;
using CamelotCombatReporter.Core.Models;

namespace CamelotCombatReporter.Core.LootTracking;

/// <summary>
/// Options for controlling loot data exports.
/// </summary>
/// <param name="MobFilter">Optional filter for mob names (contains match).</param>
/// <param name="ItemFilter">Optional filter for item names (contains match).</param>
/// <param name="Since">Only include data from sessions after this date.</param>
/// <param name="MinSampleSize">Minimum kills required to include drop rate data.</param>
/// <param name="IncludeConfidenceIntervals">Whether to include 95% CI in output.</param>
/// <param name="WikiFormat">Format output for wiki contribution.</param>
public record LootExportOptions(
    string? MobFilter = null,
    string? ItemFilter = null,
    DateTime? Since = null,
    int MinSampleSize = 1,
    bool IncludeConfidenceIntervals = true,
    bool WikiFormat = false
);

/// <summary>
/// Exports loot tracking data in various formats.
/// </summary>
public class LootExporter
{
    private readonly ILootTrackingService _service;

    public LootExporter(ILootTrackingService service)
    {
        _service = service;
    }

    /// <summary>
    /// Exports loot data to JSON format.
    /// </summary>
    public async Task<string> ExportToJsonAsync(LootExportOptions? options = null, CancellationToken ct = default)
    {
        options ??= new LootExportOptions();
        var mobTables = await GetFilteredMobTablesAsync(options, ct);

        var exportData = new
        {
            ExportDate = DateTime.Now,
            ExportOptions = new
            {
                options.MobFilter,
                options.ItemFilter,
                options.Since,
                options.MinSampleSize,
                options.IncludeConfidenceIntervals
            },
            Summary = new
            {
                TotalMobs = mobTables.Count,
                TotalItems = mobTables.Sum(m => m.Items.Count),
                TotalKills = mobTables.Sum(m => m.TotalKills)
            },
            Mobs = mobTables.Select(m => new
            {
                m.MobName,
                m.TotalKills,
                m.FirstEncounter,
                m.LastEncounter,
                Currency = new
                {
                    m.CurrencyDrops.TotalDrops,
                    m.CurrencyDrops.TotalCopperValue,
                    Average = m.CurrencyDrops.AverageFormatted,
                    m.CurrencyDrops.MinDrop,
                    m.CurrencyDrops.MaxDrop
                },
                Items = m.Items
                    .Where(i => i.TotalKills >= options.MinSampleSize)
                    .Select(i => options.IncludeConfidenceIntervals
                        ? new
                        {
                            i.ItemName,
                            i.TotalDrops,
                            DropRate = Math.Round(i.DropRate, 2),
                            ConfidenceInterval = new
                            {
                                Lower = Math.Round(i.ConfidenceLower, 2),
                                Upper = Math.Round(i.ConfidenceUpper, 2)
                            },
                            i.FirstSeen,
                            i.LastSeen
                        } as object
                        : new
                        {
                            i.ItemName,
                            i.TotalDrops,
                            DropRate = Math.Round(i.DropRate, 2),
                            i.FirstSeen,
                            i.LastSeen
                        })
            })
        };

        return JsonSerializer.Serialize(exportData, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
    }

    /// <summary>
    /// Exports loot data to CSV format.
    /// </summary>
    public async Task<string> ExportToCsvAsync(LootExportOptions? options = null, CancellationToken ct = default)
    {
        options ??= new LootExportOptions();
        var mobTables = await GetFilteredMobTablesAsync(options, ct);

        var sb = new StringBuilder();

        // Header
        if (options.IncludeConfidenceIntervals)
        {
            sb.AppendLine("MobName,ItemName,TotalDrops,TotalKills,DropRate,ConfidenceLower,ConfidenceUpper,FirstSeen,LastSeen");
        }
        else
        {
            sb.AppendLine("MobName,ItemName,TotalDrops,TotalKills,DropRate,FirstSeen,LastSeen");
        }

        // Data rows
        foreach (var mob in mobTables)
        {
            foreach (var item in mob.Items.Where(i => i.TotalKills >= options.MinSampleSize))
            {
                var mobName = EscapeCsvField(mob.MobName);
                var itemName = EscapeCsvField(item.ItemName);

                if (options.IncludeConfidenceIntervals)
                {
                    sb.AppendLine($"{mobName},{itemName},{item.TotalDrops},{item.TotalKills}," +
                                  $"{item.DropRate:F2},{item.ConfidenceLower:F2},{item.ConfidenceUpper:F2}," +
                                  $"{item.FirstSeen:yyyy-MM-dd},{item.LastSeen:yyyy-MM-dd}");
                }
                else
                {
                    sb.AppendLine($"{mobName},{itemName},{item.TotalDrops},{item.TotalKills}," +
                                  $"{item.DropRate:F2}," +
                                  $"{item.FirstSeen:yyyy-MM-dd},{item.LastSeen:yyyy-MM-dd}");
                }
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Exports loot data to Markdown format suitable for wiki contribution.
    /// </summary>
    public async Task<string> ExportToMarkdownAsync(LootExportOptions? options = null, CancellationToken ct = default)
    {
        options ??= new LootExportOptions { WikiFormat = true };
        var mobTables = await GetFilteredMobTablesAsync(options, ct);

        var sb = new StringBuilder();

        sb.AppendLine("# Loot Drop Rate Data");
        sb.AppendLine();
        sb.AppendLine($"*Exported: {DateTime.Now:yyyy-MM-dd HH:mm}*");
        sb.AppendLine();

        if (options.MinSampleSize > 1)
        {
            sb.AppendLine($"> Note: Only includes items with at least {options.MinSampleSize} kills for statistical significance.");
            sb.AppendLine();
        }

        foreach (var mob in mobTables.OrderBy(m => m.MobName))
        {
            var items = mob.Items
                .Where(i => i.TotalKills >= options.MinSampleSize)
                .OrderByDescending(i => i.DropRate)
                .ToList();

            if (!items.Any()) continue;

            sb.AppendLine($"## {mob.MobName}");
            sb.AppendLine();
            sb.AppendLine($"*Total kills tracked: {mob.TotalKills}*");
            sb.AppendLine();

            if (options.IncludeConfidenceIntervals)
            {
                sb.AppendLine("| Item | Drops | Kills | Rate | 95% CI |");
                sb.AppendLine("|------|-------|-------|------|--------|");

                foreach (var item in items)
                {
                    sb.AppendLine($"| {item.ItemName} | {item.TotalDrops} | {item.TotalKills} | " +
                                  $"{item.DropRate:F1}% | {item.ConfidenceLower:F1}% - {item.ConfidenceUpper:F1}% |");
                }
            }
            else
            {
                sb.AppendLine("| Item | Drops | Kills | Rate |");
                sb.AppendLine("|------|-------|-------|------|");

                foreach (var item in items)
                {
                    sb.AppendLine($"| {item.ItemName} | {item.TotalDrops} | {item.TotalKills} | {item.DropRate:F1}% |");
                }
            }

            sb.AppendLine();
        }

        // Add footer with generation info
        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine("*Generated by Camelot Combat Reporter*");

        return sb.ToString();
    }

    /// <summary>
    /// Exports currency statistics to CSV format.
    /// </summary>
    public async Task<string> ExportCurrencyToCsvAsync(LootExportOptions? options = null, CancellationToken ct = default)
    {
        options ??= new LootExportOptions();
        var mobTables = await GetFilteredMobTablesAsync(options, ct);

        var sb = new StringBuilder();
        sb.AppendLine("MobName,TotalKills,CurrencyDrops,TotalCopper,AverageCopper,MinCopper,MaxCopper");

        foreach (var mob in mobTables.Where(m => m.CurrencyDrops.TotalDrops > 0))
        {
            var mobName = EscapeCsvField(mob.MobName);
            sb.AppendLine($"{mobName},{mob.TotalKills},{mob.CurrencyDrops.TotalDrops}," +
                          $"{mob.CurrencyDrops.TotalCopperValue},{mob.CurrencyDrops.AveragePerDrop:F2}," +
                          $"{mob.CurrencyDrops.MinDrop},{mob.CurrencyDrops.MaxDrop}");
        }

        return sb.ToString();
    }

    /// <summary>
    /// Writes export data to a file.
    /// </summary>
    public async Task ExportToFileAsync(
        string filePath,
        string format,
        LootExportOptions? options = null,
        CancellationToken ct = default)
    {
        var content = format.ToLowerInvariant() switch
        {
            "json" => await ExportToJsonAsync(options, ct),
            "csv" => await ExportToCsvAsync(options, ct),
            "md" or "markdown" => await ExportToMarkdownAsync(options, ct),
            "currency" or "currency-csv" => await ExportCurrencyToCsvAsync(options, ct),
            _ => throw new ArgumentException($"Unsupported format: {format}")
        };

        await File.WriteAllTextAsync(filePath, content, ct);
    }

    private async Task<IReadOnlyList<MobLootTable>> GetFilteredMobTablesAsync(
        LootExportOptions options,
        CancellationToken ct)
    {
        var mobTables = await _service.SearchMobsAsync(options.MobFilter, "kills", 10000, ct);

        // Apply item filter if specified
        if (!string.IsNullOrWhiteSpace(options.ItemFilter))
        {
            mobTables = mobTables
                .Where(m => m.Items.Any(i =>
                    i.ItemName.Contains(options.ItemFilter, StringComparison.OrdinalIgnoreCase)))
                .ToList();
        }

        // Apply minimum sample size filter
        if (options.MinSampleSize > 1)
        {
            mobTables = mobTables
                .Where(m => m.TotalKills >= options.MinSampleSize)
                .ToList();
        }

        return mobTables;
    }

    private static string EscapeCsvField(string field)
    {
        if (field.Contains(',') || field.Contains('"') || field.Contains('\n'))
        {
            return $"\"{field.Replace("\"", "\"\"")}\"";
        }
        return field;
    }
}
