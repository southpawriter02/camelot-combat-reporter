# Getting Started with Plugin Development

This guide walks you through creating your first Camelot Combat Reporter plugin, from setting up your development environment to testing and installing your plugin.

## Prerequisites

- **.NET 9.0 SDK** or later
- **Visual Studio 2022**, **JetBrains Rider**, or **VS Code** with C# extension
- Basic knowledge of C# and .NET development

## Development Environment Setup

### 1. Create a New Class Library Project

```bash
# Create a new directory for your plugin
mkdir MyFirstPlugin
cd MyFirstPlugin

# Create the class library project
dotnet new classlib -n MyFirstPlugin -f net9.0
cd MyFirstPlugin
```

### 2. Reference the Plugin SDK

You have two options for referencing the SDK:

**Option A: Project Reference (for development)**

If you have access to the Camelot Combat Reporter source code:

```bash
dotnet add reference /path/to/CamelotCombatReporter.PluginSdk.csproj
```

**Option B: NuGet Package (when available)**

```bash
dotnet add package CamelotCombatReporter.PluginSdk
```

### 3. Update Your Project File

Edit `MyFirstPlugin.csproj` to configure the build output:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>

    <!-- Plugin metadata -->
    <AssemblyTitle>My First Plugin</AssemblyTitle>
    <Description>A sample plugin for Camelot Combat Reporter</Description>

    <!-- Copy dependencies to output -->
    <CopyLocalLockFileAssemblies>true</CopyLocalLockFileAssemblies>
  </PropertyGroup>

  <ItemGroup>
    <!-- Reference the SDK -->
    <ProjectReference Include="path/to/CamelotCombatReporter.PluginSdk.csproj" />
  </ItemGroup>

  <!-- Copy plugin.json to output -->
  <ItemGroup>
    <None Update="plugin.json">
      <CopyToOutputDirectory>Always</CopyToOutputDirectory>
    </None>
  </ItemGroup>

</Project>
```

## Creating Your First Plugin

Let's create a simple Data Analysis plugin that calculates damage per second (DPS).

### Step 1: Create the Plugin Class

Create `DpsAnalyzerPlugin.cs`:

```csharp
using CamelotCombatReporter.Core.Models;
using CamelotCombatReporter.Plugins.Abstractions;
using CamelotCombatReporter.Plugins.Manifest;
using CamelotCombatReporter.PluginSdk;

namespace MyFirstPlugin;

/// <summary>
/// A plugin that calculates DPS (Damage Per Second) from combat logs.
/// </summary>
public class DpsAnalyzerPlugin : DataAnalysisPluginBase
{
    // Unique identifier - must match plugin.json
    public override string Id => "my-dps-analyzer";

    // Display name shown in Plugin Manager
    public override string Name => "DPS Analyzer";

    // Plugin version - must match plugin.json
    public override Version Version => new(1, 0, 0);

    // Your name or organization
    public override string Author => "Your Name";

    // Brief description of functionality
    public override string Description =>
        "Calculates damage per second (DPS) for each combatant.";

    // Define what statistics this plugin provides
    public override IReadOnlyCollection<StatisticDefinition> ProvidedStatistics =>
        new[]
        {
            DefineNumericStatistic(
                id: "dps",
                name: "DPS",
                description: "Damage dealt per second",
                category: "Performance"
            ),
            DefineNumericStatistic(
                id: "burst-dps",
                name: "Burst DPS",
                description: "Peak 10-second damage per second",
                category: "Performance"
            ),
            DefineNumericStatistic(
                id: "damage-efficiency",
                name: "Damage Efficiency",
                description: "Percentage of attacks that dealt damage",
                category: "Efficiency",
                formatString: "P1" // Format as percentage
            )
        };

    public override Task InitializeAsync(IPluginContext context, CancellationToken ct = default)
    {
        LogInfo("DPS Analyzer plugin initialized!");
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
            // Get damage events for the specified combatant
            var damageEvents = GetDamageDealt(events, options.CombatantName);

            if (!damageEvents.Any())
            {
                return Task.FromResult(Success(new Dictionary<string, object>
                {
                    ["dps"] = 0.0,
                    ["burst-dps"] = 0.0,
                    ["damage-efficiency"] = 0.0
                }));
            }

            // Calculate time span
            var firstEvent = damageEvents.First();
            var lastEvent = damageEvents.Last();
            var duration = CalculateDuration(firstEvent.Timestamp, lastEvent.Timestamp);
            var durationSeconds = Math.Max(duration.TotalSeconds, 1);

            // Calculate total damage
            var totalDamage = damageEvents.Sum(e => e.DamageAmount);
            var dps = totalDamage / durationSeconds;

            // Calculate burst DPS (peak 10-second window)
            var burstDps = CalculateBurstDps(damageEvents, TimeSpan.FromSeconds(10));

            // Calculate efficiency
            var allAttacks = events
                .Where(e => e is DamageEvent or CombatStyleEvent)
                .Count();
            var successfulHits = damageEvents.Count;
            var efficiency = allAttacks > 0
                ? (double)successfulHits / allAttacks
                : 0.0;

            LogInfo($"Calculated DPS: {dps:F2} for {options.CombatantName}");

            return Task.FromResult(Success(new Dictionary<string, object>
            {
                ["dps"] = Math.Round(dps, 2),
                ["burst-dps"] = Math.Round(burstDps, 2),
                ["damage-efficiency"] = efficiency
            }));
        }
        catch (Exception ex)
        {
            LogError($"Error calculating DPS: {ex.Message}", ex);
            return Task.FromResult(Failure($"Failed to calculate DPS: {ex.Message}"));
        }
    }

    private static TimeSpan CalculateDuration(TimeOnly start, TimeOnly end)
    {
        // Handle crossing midnight
        if (end < start)
        {
            return TimeSpan.FromHours(24) - (start - end);
        }
        return end - start;
    }

    private static double CalculateBurstDps(
        IReadOnlyList<DamageEvent> events,
        TimeSpan windowSize)
    {
        if (events.Count < 2)
            return events.FirstOrDefault()?.DamageAmount ?? 0;

        double maxDps = 0;

        for (int i = 0; i < events.Count; i++)
        {
            var windowStart = events[i].Timestamp;
            var windowEnd = windowStart.Add(windowSize);

            var windowDamage = events
                .Skip(i)
                .TakeWhile(e => e.Timestamp <= windowEnd)
                .Sum(e => e.DamageAmount);

            var dps = windowDamage / windowSize.TotalSeconds;
            maxDps = Math.Max(maxDps, dps);
        }

        return maxDps;
    }
}
```

### Step 2: Create the Plugin Manifest

Create `plugin.json` in your project root:

```json
{
  "id": "my-dps-analyzer",
  "name": "DPS Analyzer",
  "version": "1.0.0",
  "author": "Your Name",
  "description": "Calculates damage per second (DPS) for each combatant.",
  "type": "DataAnalysis",
  "entryPoint": {
    "assembly": "MyFirstPlugin.dll",
    "typeName": "MyFirstPlugin.DpsAnalyzerPlugin"
  },
  "compatibility": {
    "minAppVersion": "1.0.0"
  },
  "permissions": [],
  "resources": {
    "maxMemoryMb": 32,
    "maxCpuTimeSeconds": 10
  }
}
```

### Step 3: Build the Plugin

```bash
dotnet build -c Release
```

Your plugin will be built to `bin/Release/net9.0/`.

## Installing Your Plugin

### Manual Installation

1. Navigate to the plugins directory:
   - **Windows**: `%APPDATA%\CamelotCombatReporter\plugins\installed\`
   - **macOS**: `~/Library/Application Support/CamelotCombatReporter/plugins/installed/`
   - **Linux**: `~/.config/CamelotCombatReporter/plugins/installed/`

2. Create a folder for your plugin (e.g., `my-dps-analyzer`)

3. Copy all files from your `bin/Release/net9.0/` folder to this directory:
   - `MyFirstPlugin.dll`
   - `plugin.json`
   - Any other dependency DLLs

### Using the Plugin Manager

1. Open Camelot Combat Reporter
2. Go to **Tools > Plugin Manager**
3. Click **Install from File**
4. Select your `plugin.json` or the plugin folder
5. Review permissions and confirm installation

## Testing Your Plugin

### Unit Testing

Create a test project for your plugin:

```bash
dotnet new xunit -n MyFirstPlugin.Tests
cd MyFirstPlugin.Tests
dotnet add reference ../MyFirstPlugin/MyFirstPlugin.csproj
```

Create `DpsAnalyzerTests.cs`:

```csharp
using CamelotCombatReporter.Core.Models;
using MyFirstPlugin;

namespace MyFirstPlugin.Tests;

public class DpsAnalyzerTests
{
    [Fact]
    public async Task AnalyzeAsync_WithDamageEvents_CalculatesDps()
    {
        // Arrange
        var plugin = new DpsAnalyzerPlugin();
        var events = new List<LogEvent>
        {
            new DamageEvent(
                Timestamp: new TimeOnly(12, 0, 0),
                Source: "You",
                Target: "Goblin",
                DamageAmount: 100,
                DamageType: "Slash"
            ),
            new DamageEvent(
                Timestamp: new TimeOnly(12, 0, 10),
                Source: "You",
                Target: "Goblin",
                DamageAmount: 150,
                DamageType: "Slash"
            ),
        };

        var options = new AnalysisOptions("You");

        // Act
        var result = await plugin.AnalyzeAsync(events, null, options);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(25.0, result.Statistics["dps"]); // 250 damage / 10 seconds
    }

    [Fact]
    public async Task AnalyzeAsync_WithNoEvents_ReturnsZero()
    {
        var plugin = new DpsAnalyzerPlugin();
        var events = new List<LogEvent>();
        var options = new AnalysisOptions("You");

        var result = await plugin.AnalyzeAsync(events, null, options);

        Assert.True(result.Success);
        Assert.Equal(0.0, result.Statistics["dps"]);
    }
}
```

### Integration Testing

Test with real log data:

```csharp
[Fact]
public async Task AnalyzeAsync_WithRealLogData_ProducesReasonableResults()
{
    var plugin = new DpsAnalyzerPlugin();

    // Parse a real log file
    var parser = new LogParser("path/to/test-combat.log");
    var events = parser.Parse().ToList();

    var options = new AnalysisOptions("You");
    var result = await plugin.AnalyzeAsync(events, null, options);

    Assert.True(result.Success);
    Assert.True((double)result.Statistics["dps"] >= 0);
    Assert.True((double)result.Statistics["dps"] <= 10000); // Reasonable upper bound
}
```

## Debugging Tips

### Enable Logging

Use the built-in logging methods:

```csharp
LogDebug("Detailed debug information");
LogInfo("General information");
LogWarning("Warning messages");
LogError("Error messages", exception);
```

Logs are written to:
- **Windows**: `%APPDATA%\CamelotCombatReporter\logs\plugins\`
- **macOS**: `~/Library/Logs/CamelotCombatReporter/plugins/`
- **Linux**: `~/.local/share/CamelotCombatReporter/logs/plugins/`

### Common Issues

**Plugin not loading:**
- Verify `plugin.json` is in the plugin directory
- Check that `id` in manifest matches the class property
- Ensure `assembly` path is correct
- Check compatibility version

**Permission denied errors:**
- Add required permissions to `plugin.json`
- User must approve requested permissions

**Plugin crashes application:**
- Check logs for exceptions
- Ensure all async operations handle cancellation
- Verify resource limits aren't exceeded

## Next Steps

Now that you've created a basic plugin, explore:

- [Plugin Manifest Reference](manifest.md) - Complete manifest configuration
- [API Reference](api-reference.md) - Full SDK documentation
- [Permissions Guide](permissions.md) - Understanding the security model
- [Examples](examples/) - Sample plugins for each type

### Other Plugin Types

- **Export Plugin**: Export data to custom formats (XML, HTML, PDF)
- **UI Plugin**: Add custom tabs, panels, and visualizations
- **Parser Plugin**: Support custom log formats and event types

See the [Examples](examples/) directory for complete implementations of each plugin type.
