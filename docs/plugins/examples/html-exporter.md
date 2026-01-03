# Example: HTML Report Exporter Plugin

A complete Export Format plugin that generates styled HTML combat reports with charts and statistics.

## Overview

**Plugin Type:** Export Format
**Complexity:** Intermediate
**Permissions:** `FileWriteExternal` (for saving to user-selected location)

This plugin demonstrates:
- Extending `ExportPluginBase`
- Defining export options
- Writing to output streams
- Generating formatted output

## Complete Source Code

### HtmlExportPlugin.cs

```csharp
using System.Text;
using System.Web;
using CamelotCombatReporter.Core.Models;
using CamelotCombatReporter.Plugins.Abstractions;
using CamelotCombatReporter.PluginSdk;

namespace HtmlExporter;

/// <summary>
/// Exports combat data to a styled HTML report.
/// </summary>
public class HtmlExportPlugin : ExportPluginBase
{
    public override string Id => "html-exporter";
    public override string Name => "HTML Report Exporter";
    public override Version Version => new(1, 0, 0);
    public override string Author => "Example Author";
    public override string Description =>
        "Exports combat data to a beautiful HTML report with charts and tables.";

    public override string FileExtension => ".html";
    public override string MimeType => "text/html";
    public override string FormatDisplayName => "HTML Report";

    public override IReadOnlyCollection<ExportOptionDefinition> ExportOptions =>
        new[]
        {
            BoolOption(
                id: "include-charts",
                name: "Include Charts",
                description: "Embed interactive charts using Chart.js",
                defaultValue: true),

            BoolOption(
                id: "include-events",
                name: "Include Event Log",
                description: "Include detailed event log table",
                defaultValue: false),

            BoolOption(
                id: "dark-theme",
                name: "Dark Theme",
                description: "Use dark color scheme",
                defaultValue: false),

            StringOption(
                id: "report-title",
                name: "Report Title",
                description: "Custom title for the report",
                defaultValue: "Combat Report")
        };

    public override async Task<ExportResult> ExportAsync(
        ExportContext context,
        Stream outputStream,
        CancellationToken cancellationToken = default)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Read options
            var includeCharts = GetOption<bool>(context.Options, "include-charts", true);
            var includeEvents = GetOption<bool>(context.Options, "include-events", false);
            var darkTheme = GetOption<bool>(context.Options, "dark-theme", false);
            var title = GetOption<string>(context.Options, "report-title", "Combat Report");

            LogInfo($"Generating HTML report: charts={includeCharts}, events={includeEvents}");

            // Build HTML content
            var html = BuildHtml(context, title, includeCharts, includeEvents, darkTheme);

            // Write to stream
            var bytes = await WriteTextAsync(outputStream, html, cancellationToken);

            LogInfo($"HTML report generated: {bytes} bytes");
            return Success(bytes);
        }
        catch (OperationCanceledException)
        {
            LogWarning("Export cancelled");
            throw;
        }
        catch (Exception ex)
        {
            LogError("Export failed", ex);
            return Failure($"Failed to generate HTML: {ex.Message}");
        }
    }

    private static T GetOption<T>(IReadOnlyDictionary<string, object> options, string key, T defaultValue)
    {
        if (options.TryGetValue(key, out var value) && value is T typedValue)
            return typedValue;
        return defaultValue;
    }

    private string BuildHtml(
        ExportContext context,
        string title,
        bool includeCharts,
        bool includeEvents,
        bool darkTheme)
    {
        var sb = new StringBuilder();

        // HTML head
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang=\"en\">");
        sb.AppendLine("<head>");
        sb.AppendLine("  <meta charset=\"UTF-8\">");
        sb.AppendLine("  <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
        sb.AppendLine($"  <title>{Escape(title)}</title>");

        // Styles
        sb.AppendLine("  <style>");
        sb.AppendLine(GetStyles(darkTheme));
        sb.AppendLine("  </style>");

        // Chart.js (if needed)
        if (includeCharts)
        {
            sb.AppendLine("  <script src=\"https://cdn.jsdelivr.net/npm/chart.js\"></script>");
        }

        sb.AppendLine("</head>");
        sb.AppendLine("<body>");

        // Header
        sb.AppendLine($"  <h1>{Escape(title)}</h1>");
        sb.AppendLine($"  <p class=\"subtitle\">Combatant: {Escape(context.CombatantName)}</p>");
        sb.AppendLine($"  <p class=\"generated\">Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}</p>");

        // Statistics summary
        if (context.Statistics != null)
        {
            sb.AppendLine("  <section class=\"statistics\">");
            sb.AppendLine("    <h2>Combat Statistics</h2>");
            sb.AppendLine("    <div class=\"stat-grid\">");
            sb.AppendLine($"      <div class=\"stat-card\"><span class=\"stat-value\">{context.Statistics.TotalDamageDealt:N0}</span><span class=\"stat-label\">Damage Dealt</span></div>");
            sb.AppendLine($"      <div class=\"stat-card\"><span class=\"stat-value\">{context.Statistics.TotalDamageTaken:N0}</span><span class=\"stat-label\">Damage Taken</span></div>");
            sb.AppendLine($"      <div class=\"stat-card\"><span class=\"stat-value\">{context.Statistics.TotalHealing:N0}</span><span class=\"stat-label\">Healing</span></div>");
            sb.AppendLine($"      <div class=\"stat-card\"><span class=\"stat-value\">{context.Statistics.HitCount:N0}</span><span class=\"stat-label\">Hits</span></div>");
            sb.AppendLine("    </div>");
            sb.AppendLine("  </section>");
        }

        // Combat styles
        if (context.CombatStyles.Count > 0)
        {
            sb.AppendLine("  <section class=\"combat-styles\">");
            sb.AppendLine("    <h2>Combat Styles Used</h2>");
            sb.AppendLine("    <table>");
            sb.AppendLine("      <thead><tr><th>Style</th><th>Count</th></tr></thead>");
            sb.AppendLine("      <tbody>");
            foreach (var style in context.CombatStyles.OrderByDescending(s => s.Count))
            {
                sb.AppendLine($"        <tr><td>{Escape(style.StyleName)}</td><td>{style.Count}</td></tr>");
            }
            sb.AppendLine("      </tbody>");
            sb.AppendLine("    </table>");
            sb.AppendLine("  </section>");
        }

        // Spells
        if (context.Spells.Count > 0)
        {
            sb.AppendLine("  <section class=\"spells\">");
            sb.AppendLine("    <h2>Spells Cast</h2>");
            sb.AppendLine("    <table>");
            sb.AppendLine("      <thead><tr><th>Spell</th><th>Count</th></tr></thead>");
            sb.AppendLine("      <tbody>");
            foreach (var spell in context.Spells.OrderByDescending(s => s.Count))
            {
                sb.AppendLine($"        <tr><td>{Escape(spell.SpellName)}</td><td>{spell.Count}</td></tr>");
            }
            sb.AppendLine("      </tbody>");
            sb.AppendLine("    </table>");
            sb.AppendLine("  </section>");
        }

        // Charts
        if (includeCharts && context.Statistics?.DamageByType != null)
        {
            sb.AppendLine("  <section class=\"charts\">");
            sb.AppendLine("    <h2>Damage Distribution</h2>");
            sb.AppendLine("    <div class=\"chart-container\">");
            sb.AppendLine("      <canvas id=\"damageChart\"></canvas>");
            sb.AppendLine("    </div>");
            sb.AppendLine("    <script>");
            sb.AppendLine(BuildChartScript(context.Statistics.DamageByType));
            sb.AppendLine("    </script>");
            sb.AppendLine("  </section>");
        }

        // Event log
        if (includeEvents)
        {
            sb.AppendLine("  <section class=\"event-log\">");
            sb.AppendLine("    <h2>Event Log</h2>");
            sb.AppendLine("    <table>");
            sb.AppendLine("      <thead><tr><th>Time</th><th>Type</th><th>Source</th><th>Target</th><th>Details</th></tr></thead>");
            sb.AppendLine("      <tbody>");
            foreach (var evt in context.FilteredEvents.Take(500))
            {
                var (type, details) = GetEventDetails(evt);
                var source = GetEventSource(evt);
                var target = GetEventTarget(evt);
                sb.AppendLine($"        <tr><td>{evt.Timestamp:HH:mm:ss}</td><td>{type}</td><td>{Escape(source)}</td><td>{Escape(target)}</td><td>{Escape(details)}</td></tr>");
            }
            sb.AppendLine("      </tbody>");
            sb.AppendLine("    </table>");
            if (context.FilteredEvents.Count > 500)
            {
                sb.AppendLine($"    <p class=\"note\">Showing 500 of {context.FilteredEvents.Count} events</p>");
            }
            sb.AppendLine("  </section>");
        }

        // Footer
        sb.AppendLine("  <footer>");
        sb.AppendLine("    <p>Generated by Camelot Combat Reporter</p>");
        sb.AppendLine("  </footer>");

        sb.AppendLine("</body>");
        sb.AppendLine("</html>");

        return sb.ToString();
    }

    private static string GetStyles(bool darkTheme)
    {
        var bg = darkTheme ? "#1a1a2e" : "#ffffff";
        var fg = darkTheme ? "#eaeaea" : "#333333";
        var cardBg = darkTheme ? "#16213e" : "#f5f5f5";
        var accent = "#4a90d9";

        return $@"
            * {{ box-sizing: border-box; margin: 0; padding: 0; }}
            body {{
                font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
                background: {bg};
                color: {fg};
                padding: 2rem;
                line-height: 1.6;
            }}
            h1 {{ margin-bottom: 0.5rem; color: {accent}; }}
            h2 {{ margin: 2rem 0 1rem; border-bottom: 2px solid {accent}; padding-bottom: 0.5rem; }}
            .subtitle {{ font-size: 1.2rem; opacity: 0.8; }}
            .generated {{ font-size: 0.9rem; opacity: 0.6; margin-bottom: 2rem; }}
            .stat-grid {{
                display: grid;
                grid-template-columns: repeat(auto-fit, minmax(150px, 1fr));
                gap: 1rem;
            }}
            .stat-card {{
                background: {cardBg};
                padding: 1.5rem;
                border-radius: 8px;
                text-align: center;
            }}
            .stat-value {{
                display: block;
                font-size: 2rem;
                font-weight: bold;
                color: {accent};
            }}
            .stat-label {{
                display: block;
                font-size: 0.9rem;
                opacity: 0.7;
                margin-top: 0.5rem;
            }}
            table {{
                width: 100%;
                border-collapse: collapse;
                margin: 1rem 0;
            }}
            th, td {{
                padding: 0.75rem;
                text-align: left;
                border-bottom: 1px solid {(darkTheme ? "#333" : "#ddd")};
            }}
            th {{ background: {cardBg}; font-weight: 600; }}
            tr:hover {{ background: {(darkTheme ? "#1f3460" : "#f0f0f0")}; }}
            .chart-container {{
                max-width: 500px;
                margin: 1rem auto;
            }}
            .note {{ font-size: 0.9rem; opacity: 0.6; font-style: italic; }}
            footer {{
                margin-top: 3rem;
                padding-top: 1rem;
                border-top: 1px solid {(darkTheme ? "#333" : "#ddd")};
                text-align: center;
                opacity: 0.6;
            }}
        ";
    }

    private static string BuildChartScript(Dictionary<string, int> damageByType)
    {
        var labels = string.Join(",", damageByType.Keys.Select(k => $"'{k}'"));
        var data = string.Join(",", damageByType.Values);
        var colors = string.Join(",", damageByType.Keys.Select((_, i) =>
            $"'hsl({i * 360 / Math.Max(damageByType.Count, 1)}, 70%, 50%)'"));

        return $@"
            new Chart(document.getElementById('damageChart'), {{
                type: 'doughnut',
                data: {{
                    labels: [{labels}],
                    datasets: [{{
                        data: [{data}],
                        backgroundColor: [{colors}]
                    }}]
                }},
                options: {{
                    responsive: true,
                    plugins: {{
                        legend: {{ position: 'bottom' }}
                    }}
                }}
            }});
        ";
    }

    private static (string type, string details) GetEventDetails(LogEvent evt)
    {
        return evt switch
        {
            DamageEvent d => ("Damage", $"{d.DamageAmount} {d.DamageType}"),
            HealingEvent h => ("Healing", $"{h.HealingAmount} HP"),
            CombatStyleEvent c => ("Style", c.StyleName),
            SpellCastEvent s => ("Spell", s.SpellName),
            _ => ("Event", "-")
        };
    }

    private static string GetEventSource(LogEvent evt)
    {
        return evt switch
        {
            DamageEvent d => d.Source,
            HealingEvent h => h.Source,
            CombatStyleEvent c => c.Source,
            SpellCastEvent s => s.Source,
            _ => "-"
        };
    }

    private static string GetEventTarget(LogEvent evt)
    {
        return evt switch
        {
            DamageEvent d => d.Target,
            HealingEvent h => h.Target,
            CombatStyleEvent c => c.Target,
            SpellCastEvent s => s.Target,
            _ => "-"
        };
    }

    private static string Escape(string text) => HttpUtility.HtmlEncode(text);
}
```

### plugin.json

```json
{
  "id": "html-exporter",
  "name": "HTML Report Exporter",
  "version": "1.0.0",
  "author": "Example Author",
  "description": "Exports combat data to a beautiful HTML report with charts and tables.",
  "type": "ExportFormat",
  "entryPoint": {
    "assembly": "HtmlExporter.dll",
    "typeName": "HtmlExporter.HtmlExportPlugin"
  },
  "compatibility": {
    "minAppVersion": "1.0.0"
  },
  "permissions": [
    {
      "type": "FileWriteExternal",
      "reason": "Save HTML report to user-selected location"
    }
  ],
  "resources": {
    "maxMemoryMb": 64,
    "maxCpuTimeSeconds": 30
  },
  "metadata": {
    "tags": ["export", "html", "report", "charts"],
    "license": "MIT"
  }
}
```

### HtmlExporter.csproj

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="path/to/CamelotCombatReporter.PluginSdk.csproj" />
  </ItemGroup>

  <ItemGroup>
    <None Update="plugin.json">
      <CopyToOutputDirectory>Always</CopyToOutputDirectory>
    </None>
  </ItemGroup>

</Project>
```

## Key Concepts Explained

### Export Options

Export options allow users to customize the export:

```csharp
public override IReadOnlyCollection<ExportOptionDefinition> ExportOptions =>
    new[]
    {
        // Boolean option with default
        BoolOption("include-charts", "Include Charts", "Embed charts", true),

        // String option
        StringOption("title", "Title", "Report title", "Combat Report"),

        // Generic option for other types
        Option("max-events", "Max Events", "Maximum events", typeof(int), 1000)
    };
```

Options are accessed from `context.Options`:

```csharp
if (context.Options.TryGetValue("include-charts", out var val) && val is bool include)
{
    // Use include
}
```

### Writing Output

Use the `WriteTextAsync` helper for text content:

```csharp
var bytes = await WriteTextAsync(outputStream, htmlContent, cancellationToken);
return Success(bytes);
```

For binary content, write directly to the stream:

```csharp
var bytes = Encoding.UTF8.GetBytes(content);
await outputStream.WriteAsync(bytes, cancellationToken);
return Success(bytes.Length);
```

### Export Context

The `ExportContext` provides all data needed for export:

```csharp
public record ExportContext(
    CombatStatistics? Statistics,        // Pre-computed stats
    IReadOnlyList<LogEvent> Events,      // All events
    IReadOnlyList<LogEvent> FilteredEvents,  // After filters
    IReadOnlyDictionary<string, object> Options,  // User options
    string CombatantName,                 // Player name
    IReadOnlyList<CombatStyleInfo> CombatStyles,  // Style usage
    IReadOnlyList<SpellCastInfo> Spells   // Spell usage
);
```

## Testing

```csharp
[Fact]
public async Task ExportsValidHtml()
{
    var plugin = new HtmlExportPlugin();
    var context = new ExportContext(
        Statistics: new CombatStatistics(1000, 500, 200, 50, 5, null, null, new(), new()),
        Events: new List<LogEvent>(),
        FilteredEvents: new List<LogEvent>(),
        Options: new Dictionary<string, object>
        {
            ["include-charts"] = true,
            ["dark-theme"] = false
        },
        CombatantName: "TestPlayer",
        CombatStyles: new List<CombatStyleInfo>(),
        Spells: new List<SpellCastInfo>()
    );

    using var stream = new MemoryStream();
    var result = await plugin.ExportAsync(context, stream);

    Assert.True(result.Success);
    Assert.True(result.BytesWritten > 0);

    stream.Position = 0;
    var html = new StreamReader(stream).ReadToEnd();
    Assert.Contains("<!DOCTYPE html>", html);
    Assert.Contains("TestPlayer", html);
}
```

## Sample Output

The generated HTML report includes:

- **Header** with title and generation date
- **Statistics Cards** showing damage dealt/taken, healing, hits
- **Combat Styles Table** listing styles by usage
- **Spells Table** listing spells by cast count
- **Damage Chart** (optional) showing damage type distribution
- **Event Log** (optional) with detailed event table
- **Responsive styling** that works on desktop and mobile
