# HTML Report Exporter Plugin

## Plugin Type: Export Format

## Overview

Generate beautiful, shareable HTML combat reports with interactive charts, detailed breakdowns, and a responsive design. Reports can be opened in any browser and shared with guildmates, posted on forums, or archived for reference.

## Problem Statement

Players want to share combat performance with their guild or community but:
- CSV exports aren't visually appealing
- JSON exports require technical knowledge
- Screenshots miss detailed data
- Need a portable, self-contained format

## Features

### Report Sections
- Executive summary with key metrics
- Damage breakdown by type and target
- Timeline chart with DPS over time
- Combat style and spell usage tables
- Healing summary (if applicable)
- Kill/death log
- Session metadata

### Visual Design
- Modern, clean CSS styling
- Realm-themed color schemes (Albion blue, Midgard red, Hibernia green)
- Responsive layout for mobile viewing
- Dark/light theme support
- Custom CSS injection option

### Interactive Elements
- Sortable data tables
- Collapsible sections
- Hover tooltips with details
- Chart zoom and pan (via embedded JS library)
- Copy-to-clipboard for sharing snippets

### Customization
- Template selection (compact, detailed, presentation)
- Include/exclude specific sections
- Custom branding (logo, title, footer)
- Privacy options (anonymize names)

## Technical Specification

### Plugin Manifest

```json
{
  "id": "html-report-exporter",
  "name": "HTML Report Exporter",
  "version": "1.0.0",
  "author": "CCR Community",
  "description": "Generates shareable HTML combat reports with charts and tables",
  "type": "ExportFormat",
  "entryPoint": {
    "assembly": "HtmlReportExporter.dll",
    "typeName": "HtmlReportExporter.HtmlExportPlugin"
  },
  "compatibility": {
    "minAppVersion": "1.0.0"
  },
  "permissions": [
    "CombatDataAccess",
    "FileWrite"
  ],
  "resources": {
    "maxMemoryMb": 128,
    "maxCpuTimeSeconds": 60
  }
}
```

### Export Options

| Option ID | Name | Type | Default | Description |
|-----------|------|------|---------|-------------|
| `template` | Template | enum | "detailed" | Report template style |
| `theme` | Theme | enum | "auto" | Color theme |
| `include-charts` | Include Charts | bool | true | Embed interactive charts |
| `include-timeline` | Include Timeline | bool | true | DPS/HPS timeline |
| `include-events` | Include Event Log | bool | true | Detailed event table |
| `anonymize` | Anonymize Names | bool | false | Replace player names |
| `embed-css` | Embed CSS | bool | true | Inline all styles |
| `custom-title` | Custom Title | string | null | Report title override |

### Implementation Outline

```csharp
public class HtmlExportPlugin : ExportPluginBase
{
    public override string Id => "html-report-exporter";
    public override string Name => "HTML Report Exporter";
    public override Version Version => new(1, 0, 0);
    public override string Author => "CCR Community";
    public override string Description =>
        "Generates shareable HTML combat reports with charts and tables";

    public override string FileExtension => ".html";
    public override string MimeType => "text/html";
    public override string FormatDisplayName => "HTML Report";

    public override IReadOnlyCollection<ExportOptionDefinition> ExportOptions =>
        new[]
        {
            Option("template", "Template", "Report template style",
                typeof(ReportTemplate), ReportTemplate.Detailed),
            Option("theme", "Theme", "Color theme",
                typeof(ReportTheme), ReportTheme.Auto),
            BoolOption("include-charts", "Include Charts",
                "Embed interactive charts", true),
            BoolOption("include-timeline", "Include Timeline",
                "DPS/HPS timeline chart", true),
            BoolOption("include-events", "Include Event Log",
                "Detailed event table", true),
            BoolOption("anonymize", "Anonymize Names",
                "Replace player names with placeholders", false),
            StringOption("custom-title", "Custom Title",
                "Override report title", null)
        };

    public override async Task<ExportResult> ExportAsync(
        ExportContext context,
        Stream outputStream,
        CancellationToken ct = default)
    {
        try
        {
            var options = ParseOptions(context.Options);
            var html = await GenerateHtmlAsync(context, options, ct);
            var bytes = await WriteTextAsync(outputStream, html, ct);
            return Success(bytes);
        }
        catch (Exception ex)
        {
            LogError("Export failed", ex);
            return Failure($"Export failed: {ex.Message}");
        }
    }

    private async Task<string> GenerateHtmlAsync(
        ExportContext context,
        HtmlExportOptions options,
        CancellationToken ct)
    {
        var sb = new StringBuilder();

        // HTML Header
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang=\"en\">");
        sb.AppendLine("<head>");
        sb.AppendLine("  <meta charset=\"UTF-8\">");
        sb.AppendLine("  <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
        sb.AppendLine($"  <title>{GetTitle(context, options)}</title>");
        sb.AppendLine(GetStyles(options));
        if (options.IncludeCharts)
        {
            sb.AppendLine(GetChartLibrary());
        }
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");

        // Report Content
        sb.AppendLine(GenerateHeader(context, options));
        sb.AppendLine(GenerateSummary(context));

        if (options.IncludeCharts)
        {
            sb.AppendLine(GenerateDamageBreakdownChart(context));
        }

        if (options.IncludeTimeline)
        {
            sb.AppendLine(GenerateTimelineChart(context));
        }

        sb.AppendLine(GenerateCombatStylesTable(context));
        sb.AppendLine(GenerateSpellsTable(context));

        if (options.IncludeEvents)
        {
            sb.AppendLine(GenerateEventLog(context, options));
        }

        sb.AppendLine(GenerateFooter());

        sb.AppendLine("</body>");
        sb.AppendLine("</html>");

        return sb.ToString();
    }

    private string GenerateSummary(ExportContext context)
    {
        var stats = context.Statistics;
        return $@"
<section class='summary'>
  <h2>Combat Summary</h2>
  <div class='stats-grid'>
    <div class='stat-card'>
      <span class='stat-value'>{stats?.Dps:F0}</span>
      <span class='stat-label'>DPS</span>
    </div>
    <div class='stat-card'>
      <span class='stat-value'>{stats?.TotalDamage:N0}</span>
      <span class='stat-label'>Total Damage</span>
    </div>
    <div class='stat-card'>
      <span class='stat-value'>{stats?.DurationMinutes:F1}m</span>
      <span class='stat-label'>Duration</span>
    </div>
    <div class='stat-card'>
      <span class='stat-value'>{context.CombatStyles.Count}</span>
      <span class='stat-label'>Combat Styles</span>
    </div>
  </div>
</section>";
    }

    private string GenerateDamageBreakdownChart(ExportContext context)
    {
        var damageByType = context.Events
            .OfType<DamageEvent>()
            .GroupBy(e => e.DamageType)
            .Select(g => new { Type = g.Key, Total = g.Sum(e => e.DamageAmount) })
            .ToList();

        var labels = string.Join(",", damageByType.Select(d => $"'{d.Type}'"));
        var data = string.Join(",", damageByType.Select(d => d.Total));

        return $@"
<section class='chart-section'>
  <h2>Damage Breakdown</h2>
  <canvas id='damageChart'></canvas>
  <script>
    new Chart(document.getElementById('damageChart'), {{
      type: 'doughnut',
      data: {{
        labels: [{labels}],
        datasets: [{{ data: [{data}] }}]
      }}
    }});
  </script>
</section>";
    }
}
```

### CSS Styling

The plugin embeds comprehensive CSS:

```css
:root {
  --primary-color: #2196F3;
  --secondary-color: #757575;
  --background: #ffffff;
  --surface: #f5f5f5;
  --text-primary: #212121;
  --text-secondary: #757575;
  --border: #e0e0e0;
  --success: #4caf50;
  --warning: #ff9800;
  --danger: #f44336;
}

[data-theme="dark"] {
  --background: #121212;
  --surface: #1e1e1e;
  --text-primary: #ffffff;
  --text-secondary: #b0b0b0;
  --border: #333333;
}

body {
  font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
  background: var(--background);
  color: var(--text-primary);
  line-height: 1.6;
  max-width: 1200px;
  margin: 0 auto;
  padding: 20px;
}

.stats-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(150px, 1fr));
  gap: 16px;
  margin: 20px 0;
}

.stat-card {
  background: var(--surface);
  border-radius: 8px;
  padding: 20px;
  text-align: center;
  box-shadow: 0 2px 4px rgba(0,0,0,0.1);
}

.stat-value {
  display: block;
  font-size: 2rem;
  font-weight: bold;
  color: var(--primary-color);
}

.stat-label {
  display: block;
  font-size: 0.875rem;
  color: var(--text-secondary);
  margin-top: 4px;
}

table {
  width: 100%;
  border-collapse: collapse;
  margin: 20px 0;
}

th, td {
  padding: 12px;
  text-align: left;
  border-bottom: 1px solid var(--border);
}

th {
  background: var(--surface);
  font-weight: 600;
}

tr:hover {
  background: var(--surface);
}
```

### Report Templates

**Compact Template:**
- Single page, minimal charts
- Quick overview for Discord sharing
- ~50KB output size

**Detailed Template (default):**
- Full charts and tables
- Collapsible sections
- All event data
- ~200KB output size

**Presentation Template:**
- Large charts, minimal text
- Suitable for streaming/recording
- Clean visual focus
- ~150KB output size

## Output Example

The generated HTML includes:

1. **Header** - Report title, date, combatant name, realm badge
2. **Summary Cards** - DPS, total damage, duration, style count
3. **Damage Chart** - Doughnut chart by damage type
4. **Timeline** - Line chart of DPS over time
5. **Combat Styles Table** - Sortable table of styles used
6. **Spells Table** - Sortable table of spells cast
7. **Event Log** - Full scrollable event list
8. **Footer** - Generated by CCR with timestamp

## Dependencies

- No external dependencies (self-contained)
- Optional: Chart.js embedded for interactive charts
- CSS is embedded inline

## Complexity

**Medium** - Template generation is straightforward, but chart integration and responsive design require attention.

## Future Enhancements

- [ ] PDF export via headless browser
- [ ] Template marketplace
- [ ] Localization support
- [ ] Animated timeline replay
- [ ] Social media meta tags for link previews
- [ ] Embed video clips from replay system
