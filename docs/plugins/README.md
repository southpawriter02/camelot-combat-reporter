# Camelot Combat Reporter Plugin System

The Camelot Combat Reporter plugin system allows developers to extend the application with custom functionality. Plugins can add new analysis capabilities, export formats, UI components, and log parsing patterns.

## Overview

The plugin system provides:

- **Full Isolation**: Each plugin runs in its own `AssemblyLoadContext` for complete isolation
- **Permission-Based Security**: Layered permission model protects user data and system resources
- **Code Signing Support**: Optional Authenticode signature verification for trusted plugins
- **Resource Limits**: CPU time and memory quotas prevent runaway plugins
- **Hot Loading/Unloading**: Plugins can be installed and removed without restarting the application

## Plugin Types

| Type | Description | Use Cases |
|------|-------------|-----------|
| **Data Analysis** | Custom statistics and metrics | DPS calculations, performance scoring, combat efficiency |
| **Export Format** | New file export formats | XML, HTML, PDF, JSON export |
| **UI Component** | Custom tabs, panels, charts | Custom visualizations, data views |
| **Custom Parser** | New log parsing patterns | Support for custom event types, mods |

## Documentation

- [Getting Started](getting-started.md) - Create your first plugin
- [Plugin Manifest](manifest.md) - Plugin configuration reference
- [API Reference](api-reference.md) - Complete SDK documentation
- [Permissions](permissions.md) - Security and permissions guide
- [Examples](examples/) - Sample plugin implementations

## Quick Start

### 1. Create a New Plugin Project

```bash
dotnet new classlib -n MyPlugin
cd MyPlugin
dotnet add reference path/to/CamelotCombatReporter.PluginSdk.csproj
```

### 2. Create the Plugin Class

```csharp
using CamelotCombatReporter.PluginSdk;
using CamelotCombatReporter.Plugins.Abstractions;

public class MyAnalysisPlugin : DataAnalysisPluginBase
{
    public override string Id => "my-analysis-plugin";
    public override string Name => "My Analysis Plugin";
    public override Version Version => new(1, 0, 0);
    public override string Author => "Your Name";
    public override string Description => "Provides custom combat analysis.";

    public override IReadOnlyCollection<StatisticDefinition> ProvidedStatistics =>
        new[] { DefineNumericStatistic("my-stat", "My Statistic", "A custom stat", "Custom") };

    public override Task<AnalysisResult> AnalyzeAsync(
        IReadOnlyList<LogEvent> events,
        CombatStatistics? baseStatistics,
        AnalysisOptions options,
        CancellationToken ct = default)
    {
        var damageDealt = GetDamageDealt(events, options.CombatantName);
        var totalDamage = damageDealt.Sum(e => e.DamageAmount);

        return Task.FromResult(Success(new Dictionary<string, object>
        {
            ["my-stat"] = totalDamage
        }));
    }
}
```

### 3. Create the Manifest

Create `plugin.json` in your project root:

```json
{
  "id": "my-analysis-plugin",
  "name": "My Analysis Plugin",
  "version": "1.0.0",
  "author": "Your Name",
  "description": "Provides custom combat analysis.",
  "type": "DataAnalysis",
  "entryPoint": {
    "assembly": "MyPlugin.dll",
    "typeName": "MyPlugin.MyAnalysisPlugin"
  },
  "compatibility": {
    "minAppVersion": "1.0.0"
  },
  "permissions": []
}
```

### 4. Build and Install

```bash
dotnet build -c Release
# Copy the output folder to the plugins/installed directory
```

## Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    Camelot Combat Reporter                   │
├─────────────────────────────────────────────────────────────┤
│  ┌─────────────────┐  ┌─────────────────┐                   │
│  │  Plugin Manager │  │  Plugin Loader  │                   │
│  │      (GUI)      │  │    Service      │                   │
│  └────────┬────────┘  └────────┬────────┘                   │
│           │                    │                             │
│  ┌────────▼────────────────────▼────────┐                   │
│  │           Plugin Registry            │                   │
│  └────────┬─────────────────────────────┘                   │
│           │                                                  │
│  ┌────────▼────────┐  ┌─────────────────┐                   │
│  │   Permission    │  │    Security     │                   │
│  │    Manager      │  │   Audit Logger  │                   │
│  └────────┬────────┘  └─────────────────┘                   │
│           │                                                  │
├───────────┼──────────────────────────────────────────────────┤
│           ▼           PLUGIN SANDBOX                         │
│  ┌─────────────────────────────────────────────────────────┐│
│  │              AssemblyLoadContext (Isolated)             ││
│  │  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐     ││
│  │  │  Plugin A   │  │  Plugin B   │  │  Plugin C   │     ││
│  │  └─────────────┘  └─────────────┘  └─────────────┘     ││
│  │                                                          ││
│  │  ┌─────────────────────────────────────────────────┐    ││
│  │  │           Sandboxed Plugin Context              │    ││
│  │  │  ┌──────────┐ ┌──────────┐ ┌──────────────────┐ │    ││
│  │  │  │FileSystem│ │ Network  │ │   Combat Data    │ │    ││
│  │  │  │  Proxy   │ │  Proxy   │ │      Proxy       │ │    ││
│  │  │  └──────────┘ └──────────┘ └──────────────────┘ │    ││
│  │  └─────────────────────────────────────────────────┘    ││
│  └─────────────────────────────────────────────────────────┘│
└─────────────────────────────────────────────────────────────┘
```

## Security Model

### Permission Tiers

| Tier | Permissions | Behavior |
|------|-------------|----------|
| **Auto-Grant** | FileRead, FileWrite, SettingsRead, SettingsWrite, CombatDataAccess | Automatically granted (low risk) |
| **Requires Approval** | NetworkAccess, UIModification, UINotifications, ClipboardAccess, FileReadExternal, FileWriteExternal | User must approve |
| **Trusted Only** | All permissions | Only for signed/trusted plugins |

### Trust Levels

- **OfficialTrusted**: Plugins signed by the Camelot Combat Reporter team
- **SignedTrusted**: Plugins signed with a trusted certificate
- **UserTrusted**: Plugins explicitly trusted by the user
- **Untrusted**: Default level for unsigned plugins

## Support

- **Issues**: [GitHub Issues](https://github.com/your-repo/issues)
- **Discussions**: [GitHub Discussions](https://github.com/your-repo/discussions)

## License

Plugins can be distributed under any license. The PluginSdk is provided under the same license as Camelot Combat Reporter.
