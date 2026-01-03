# Example: DPS Calculator Plugin

A complete Data Analysis plugin that calculates damage per second (DPS), burst damage, and combat efficiency metrics.

## Overview

**Plugin Type:** Data Analysis
**Complexity:** Beginner
**Permissions:** None (uses only auto-granted permissions)

This plugin demonstrates:
- Extending `DataAnalysisPluginBase`
- Defining custom statistics
- Filtering and analyzing combat events
- Returning results with insights

## Complete Source Code

### DpsCalculatorPlugin.cs

```csharp
using CamelotCombatReporter.Core.Models;
using CamelotCombatReporter.Plugins.Abstractions;
using CamelotCombatReporter.PluginSdk;

namespace DpsCalculator;

/// <summary>
/// Calculates DPS (Damage Per Second) and related combat metrics.
/// </summary>
public class DpsCalculatorPlugin : DataAnalysisPluginBase
{
    public override string Id => "dps-calculator";
    public override string Name => "DPS Calculator";
    public override Version Version => new(1, 0, 0);
    public override string Author => "Example Author";
    public override string Description =>
        "Calculates DPS, burst damage, and combat efficiency metrics.";

    public override IReadOnlyCollection<StatisticDefinition> ProvidedStatistics =>
        new[]
        {
            DefineNumericStatistic(
                id: "dps",
                name: "DPS",
                description: "Average damage dealt per second",
                category: "Performance"),

            DefineNumericStatistic(
                id: "burst-dps-10s",
                name: "Burst DPS (10s)",
                description: "Peak damage per second in any 10-second window",
                category: "Performance"),

            DefineNumericStatistic(
                id: "burst-dps-30s",
                name: "Burst DPS (30s)",
                description: "Peak damage per second in any 30-second window",
                category: "Performance"),

            DefineNumericStatistic(
                id: "damage-efficiency",
                name: "Damage Efficiency",
                description: "Percentage of combat time spent dealing damage",
                category: "Efficiency"),

            DefineNumericStatistic(
                id: "avg-hit-damage",
                name: "Average Hit",
                description: "Average damage per successful hit",
                category: "Damage"),

            DefineNumericStatistic(
                id: "max-hit-damage",
                name: "Max Hit",
                description: "Highest single damage hit",
                category: "Damage"),

            DefineStatistic(
                id: "best-damage-type",
                name: "Best Damage Type",
                description: "Damage type with highest total damage",
                category: "Damage",
                valueType: typeof(string))
        };

    public override Task InitializeAsync(IPluginContext context, CancellationToken ct = default)
    {
        LogInfo("DPS Calculator plugin initialized");
        return base.InitializeAsync(context, ct);
    }

    public override Task<AnalysisResult> AnalyzeAsync(
        IReadOnlyList<LogEvent> events,
        CombatStatistics? baseStatistics,
        AnalysisOptions options,
        CancellationToken cancellationToken = default)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            var damageEvents = GetDamageDealt(events, options.CombatantName).ToList();

            if (damageEvents.Count == 0)
            {
                LogDebug("No damage events found");
                return Task.FromResult(Empty());
            }

            // Calculate combat duration
            var duration = CalculateCombatDuration(damageEvents);
            var durationSeconds = Math.Max(duration.TotalSeconds, 1);

            // Basic DPS
            var totalDamage = damageEvents.Sum(e => e.DamageAmount);
            var dps = totalDamage / durationSeconds;

            // Burst DPS
            var burstDps10 = CalculateBurstDps(damageEvents, TimeSpan.FromSeconds(10));
            var burstDps30 = CalculateBurstDps(damageEvents, TimeSpan.FromSeconds(30));

            // Damage statistics
            var avgHit = damageEvents.Average(e => e.DamageAmount);
            var maxHit = damageEvents.Max(e => e.DamageAmount);

            // Efficiency (time between attacks)
            var efficiency = CalculateEfficiency(damageEvents, duration);

            // Best damage type
            var bestDamageType = damageEvents
                .GroupBy(e => e.DamageType)
                .OrderByDescending(g => g.Sum(e => e.DamageAmount))
                .First()
                .Key;

            // Build insights
            var insights = new List<AnalysisInsight>();

            if (dps > 100)
            {
                insights.Add(Insight(
                    "High DPS",
                    $"Your DPS of {dps:F1} is above average!",
                    InsightSeverity.Info));
            }

            if (efficiency < 0.5)
            {
                insights.Add(Insight(
                    "Low Combat Efficiency",
                    "You're spending less than half your time dealing damage. Consider using faster attacks.",
                    InsightSeverity.Suggestion));
            }

            if (burstDps10 > dps * 2)
            {
                insights.Add(Insight(
                    "Bursty Damage Profile",
                    "Your burst damage is much higher than sustained. Consider cooldown management.",
                    InsightSeverity.Info));
            }

            LogInfo($"Analysis complete: DPS={dps:F2}, Events={damageEvents.Count}");

            return Task.FromResult(Success(
                new Dictionary<string, object>
                {
                    ["dps"] = Math.Round(dps, 2),
                    ["burst-dps-10s"] = Math.Round(burstDps10, 2),
                    ["burst-dps-30s"] = Math.Round(burstDps30, 2),
                    ["damage-efficiency"] = Math.Round(efficiency, 4),
                    ["avg-hit-damage"] = Math.Round(avgHit, 1),
                    ["max-hit-damage"] = maxHit,
                    ["best-damage-type"] = bestDamageType
                },
                insights));
        }
        catch (OperationCanceledException)
        {
            LogWarning("Analysis cancelled");
            throw;
        }
        catch (Exception ex)
        {
            LogError("Analysis failed", ex);
            return Task.FromResult(Empty());
        }
    }

    private static TimeSpan CalculateCombatDuration(IReadOnlyList<DamageEvent> events)
    {
        if (events.Count < 2)
            return TimeSpan.Zero;

        var first = events.First().Timestamp;
        var last = events.Last().Timestamp;

        // Handle midnight crossing
        if (last < first)
        {
            return TimeSpan.FromHours(24) - (first - last);
        }

        return last - first;
    }

    private static double CalculateBurstDps(
        IReadOnlyList<DamageEvent> events,
        TimeSpan windowSize)
    {
        if (events.Count < 2)
            return events.FirstOrDefault()?.DamageAmount ?? 0;

        double maxDps = 0;
        var windowSeconds = windowSize.TotalSeconds;

        for (int i = 0; i < events.Count; i++)
        {
            var windowStart = events[i].Timestamp;
            var windowEnd = windowStart.Add(windowSize);

            // Sum damage in window
            long windowDamage = 0;
            for (int j = i; j < events.Count; j++)
            {
                var eventTime = events[j].Timestamp;

                // Handle midnight crossing in window comparison
                var isInWindow = eventTime >= windowStart &&
                    (eventTime <= windowEnd ||
                     (windowEnd.Hour < windowStart.Hour && eventTime.Hour < windowEnd.Hour));

                if (!isInWindow && j > i) break;

                windowDamage += events[j].DamageAmount;
            }

            var dps = windowDamage / windowSeconds;
            maxDps = Math.Max(maxDps, dps);
        }

        return maxDps;
    }

    private static double CalculateEfficiency(
        IReadOnlyList<DamageEvent> events,
        TimeSpan totalDuration)
    {
        if (events.Count < 2 || totalDuration.TotalSeconds < 1)
            return 1.0;

        // Calculate "active" time (sum of gaps between consecutive attacks, capped at 3s each)
        var activeTime = TimeSpan.Zero;
        var maxGap = TimeSpan.FromSeconds(3);

        for (int i = 1; i < events.Count; i++)
        {
            var gap = events[i].Timestamp - events[i - 1].Timestamp;

            // Handle midnight crossing
            if (gap < TimeSpan.Zero)
                gap += TimeSpan.FromHours(24);

            activeTime += gap > maxGap ? maxGap : gap;
        }

        return activeTime.TotalSeconds / totalDuration.TotalSeconds;
    }
}
```

### plugin.json

```json
{
  "id": "dps-calculator",
  "name": "DPS Calculator",
  "version": "1.0.0",
  "author": "Example Author",
  "description": "Calculates DPS, burst damage, and combat efficiency metrics.",
  "type": "DataAnalysis",
  "entryPoint": {
    "assembly": "DpsCalculator.dll",
    "typeName": "DpsCalculator.DpsCalculatorPlugin"
  },
  "compatibility": {
    "minAppVersion": "1.0.0"
  },
  "permissions": [],
  "resources": {
    "maxMemoryMb": 32,
    "maxCpuTimeSeconds": 10
  },
  "metadata": {
    "tags": ["dps", "damage", "performance", "statistics"],
    "license": "MIT"
  }
}
```

### DpsCalculator.csproj

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <AssemblyTitle>DPS Calculator Plugin</AssemblyTitle>
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

### Defining Statistics

Statistics are defined using helper methods from `DataAnalysisPluginBase`:

```csharp
// Numeric statistic (double)
DefineNumericStatistic(
    id: "dps",              // Unique ID
    name: "DPS",            // Display name
    description: "...",     // Tooltip text
    category: "Performance" // Grouping category
);

// Statistic with custom type
DefineStatistic(
    id: "best-type",
    name: "Best Type",
    description: "...",
    category: "Damage",
    valueType: typeof(string)  // Any serializable type
);
```

### Filtering Events

Use the built-in helper methods to filter events:

```csharp
// Get damage events where Source == combatantName
var damageDealt = GetDamageDealt(events, options.CombatantName);

// Get damage events where Target == combatantName
var damageTaken = GetDamageTaken(events, options.CombatantName);

// Get healing events where Source == combatantName
var healingDone = GetHealingDone(events, options.CombatantName);
```

### Returning Results

Use `Success()` with a dictionary of statistic values:

```csharp
return Task.FromResult(Success(
    new Dictionary<string, object>
    {
        ["dps"] = 150.5,
        ["max-hit"] = 500,
        ["best-type"] = "Slash"
    },
    insights  // Optional list of AnalysisInsight
));
```

For empty results (no data to analyze):

```csharp
return Task.FromResult(Empty());
```

### Adding Insights

Insights are optional recommendations or observations:

```csharp
var insights = new List<AnalysisInsight>();

if (dps > 100)
{
    insights.Add(Insight(
        title: "High DPS",
        description: "Your DPS is above average!",
        severity: InsightSeverity.Info
    ));
}
```

Severity levels:
- `Info` - Informational
- `Suggestion` - Improvement suggestion
- `Warning` - Something to watch
- `Critical` - Significant issue

## Testing

### Unit Test Example

```csharp
using Xunit;
using CamelotCombatReporter.Core.Models;

public class DpsCalculatorTests
{
    [Fact]
    public async Task CalculatesDpsCorrectly()
    {
        // Arrange
        var plugin = new DpsCalculatorPlugin();
        var events = new List<LogEvent>
        {
            new DamageEvent(new TimeOnly(12, 0, 0), "You", "Mob", 100, "Slash"),
            new DamageEvent(new TimeOnly(12, 0, 5), "You", "Mob", 100, "Slash"),
            new DamageEvent(new TimeOnly(12, 0, 10), "You", "Mob", 100, "Slash"),
        };
        var options = new AnalysisOptions(null, null, null, null, "You");

        // Act
        var result = await plugin.AnalyzeAsync(events, null, options);

        // Assert
        Assert.Equal(30.0, result.Statistics["dps"]); // 300 damage / 10 seconds
    }

    [Fact]
    public async Task ReturnsEmptyForNoEvents()
    {
        var plugin = new DpsCalculatorPlugin();
        var options = new AnalysisOptions(null, null, null, null, "You");

        var result = await plugin.AnalyzeAsync(new List<LogEvent>(), null, options);

        Assert.Empty(result.Statistics);
    }
}
```

## Building and Installing

```bash
# Build
cd DpsCalculator
dotnet build -c Release

# Install (macOS example)
mkdir -p ~/Library/Application\ Support/CamelotCombatReporter/plugins/installed/dps-calculator
cp bin/Release/net9.0/* ~/Library/Application\ Support/CamelotCombatReporter/plugins/installed/dps-calculator/
```

## Expected Output

After loading a combat log, this plugin adds the following statistics to the analysis:

| Statistic | Example Value | Description |
|-----------|---------------|-------------|
| DPS | 125.50 | Average damage per second |
| Burst DPS (10s) | 250.00 | Peak 10-second DPS |
| Burst DPS (30s) | 180.00 | Peak 30-second DPS |
| Damage Efficiency | 75% | Combat uptime |
| Average Hit | 85.3 | Mean damage per hit |
| Max Hit | 523 | Highest single hit |
| Best Damage Type | Slash | Highest damage type |
