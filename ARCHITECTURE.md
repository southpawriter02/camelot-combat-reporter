# Architecture Overview

This document describes the architecture of Camelot Combat Reporter, a cross-platform tool for analyzing Dark Age of Camelot combat logs.

## Table of Contents

- [System Overview](#system-overview)
- [Project Structure](#project-structure)
- [C# Projects](#c-projects)
- [TypeScript Library](#typescript-library)
- [Data Flow](#data-flow)
- [Plugin Architecture](#plugin-architecture)
- [Cross-Realm Analysis](#cross-realm-analysis)
- [Security Model](#security-model)
- [Design Decisions](#design-decisions)

---

## System Overview

Camelot Combat Reporter is a dual-implementation project:

1. **C# (.NET 9.0)** - Primary implementation with GUI, CLI, and plugin system
2. **TypeScript** - Library implementation for Node.js/browser environments

Both implementations share the same parsing logic concepts but are independent codebases.

```
┌─────────────────────────────────────────────────────────────────┐
│                        User Interfaces                          │
├─────────────────────┬─────────────────────┬─────────────────────┤
│   Avalonia GUI      │       CLI           │   TypeScript API    │
│   (Desktop App)     │   (Terminal)        │   (Node.js/Web)     │
└─────────┬───────────┴──────────┬──────────┴──────────┬──────────┘
          │                      │                     │
          ▼                      ▼                     ▼
┌─────────────────────────────────────────┐   ┌───────────────────┐
│           .NET Core Library              │   │  TypeScript Lib   │
│  ┌─────────────┐  ┌──────────────────┐  │   │  ┌─────────────┐  │
│  │   Parser    │  │     Analysis     │  │   │  │   Parser    │  │
│  └─────────────┘  └──────────────────┘  │   │  └─────────────┘  │
│  ┌─────────────┐  ┌──────────────────┐  │   │  ┌─────────────┐  │
│  │   Models    │  │   Cross-Realm    │  │   │  │   Analysis  │  │
│  └─────────────┘  └──────────────────┘  │   │  └─────────────┘  │
│  ┌─────────────┐  ┌──────────────────┐  │   │  ┌─────────────┐  │
│  │  Exporting  │  │     Plugins      │  │   │  │     ML      │  │
│  └─────────────┘  └──────────────────┘  │   │  └─────────────┘  │
└─────────────────────────────────────────┘   └───────────────────┘
```

---

## Project Structure

```
camelot-combat-reporter/
├── src/
│   ├── CamelotCombatReporter.Core/       # Core C# library
│   ├── CamelotCombatReporter.Cli/        # Command-line interface
│   ├── CamelotCombatReporter.Gui/        # Avalonia desktop app
│   ├── CamelotCombatReporter.Plugins/    # Plugin host infrastructure
│   ├── CamelotCombatReporter.PluginSdk/  # Plugin development SDK
│   │
│   │   # TypeScript Implementation
│   ├── index.ts                          # TypeScript entry point
│   ├── parser/                           # TS log parsing
│   ├── analysis/                         # TS combat analysis
│   ├── streaming/                        # TS real-time parsing
│   ├── database/                         # TS database adapters
│   ├── api/                              # TS REST API
│   ├── ml/                               # TS machine learning
│   ├── types/                            # TS type definitions
│   ├── file/                             # TS file utilities
│   ├── utils/                            # TS shared utilities
│   └── errors/                           # TS error types
│
├── tests/
│   └── CamelotCombatReporter.Core.Tests/ # Unit tests
│
├── docs/
│   └── plugins/                          # Plugin documentation
│       ├── api-reference.md
│       ├── getting-started.md
│       ├── manifest.md
│       ├── permissions.md
│       └── examples/
│
├── roadmap/                              # Feature roadmap
│   ├── core-features/
│   ├── advanced-features/
│   └── future-enhancements/
│
└── data/                                 # Sample log files
```

---

## C# Projects

### CamelotCombatReporter.Core

The foundation library containing all parsing, analysis, and data models.

```
CamelotCombatReporter.Core/
├── Models/
│   ├── LogEvent.cs              # Base event record
│   ├── DamageEvent.cs           # Damage dealt/taken
│   ├── HealingEvent.cs          # Healing done/received
│   ├── CombatStyleEvent.cs      # Combat style usage
│   ├── SpellCastEvent.cs        # Spell casting
│   ├── CombatStatistics.cs      # Aggregated statistics
│   ├── GameEnums.cs             # Realm, CharacterClass enums
│   ├── CharacterInfo.cs         # Character configuration
│   └── ExtendedCombatStatistics.cs  # Cross-realm stats
│
├── Parsing/
│   └── LogParser.cs             # Main log file parser
│
├── Exporting/
│   └── CsvExporter.cs           # CSV export functionality
│
└── CrossRealm/
    ├── ICrossRealmStatisticsService.cs
    ├── CrossRealmStatisticsService.cs  # Local JSON storage
    ├── CrossRealmExporter.cs           # JSON/CSV export
    └── CrossRealmTypes.cs              # Supporting types
```

**Key Responsibilities:**
- Parse DAoC combat log files
- Extract and categorize combat events
- Calculate combat statistics (DPS, damage totals, etc.)
- Provide cross-realm analysis and storage
- Export data to CSV/JSON formats

**Dependencies:** None (pure .NET library)

### CamelotCombatReporter.Cli

Simple command-line interface for quick log analysis.

```
Usage: dotnet run --project src/CamelotCombatReporter.Cli -- <log_file> [combatant_name]
```

**Dependencies:** Core

### CamelotCombatReporter.Gui

Desktop application built with Avalonia UI framework.

```
CamelotCombatReporter.Gui/
├── Views/
│   └── MainWindow.axaml         # Main application window
│
├── ViewModels/
│   └── MainWindowViewModel.cs   # MVVM view model
│
├── CrossRealm/
│   ├── Views/
│   │   ├── CrossRealmView.axaml
│   │   └── CharacterConfigDialog.axaml
│   └── ViewModels/
│       ├── CrossRealmViewModel.cs
│       └── CharacterConfigViewModel.cs
│
└── Plugins/
    ├── Views/
    │   └── PluginManagerWindow.axaml
    └── ViewModels/
        └── PluginManagerViewModel.cs
```

**Key Features:**
- File selection with drag-and-drop support
- Real-time combat statistics display
- Interactive charts (LiveCharts2)
- Filtering by event type, target, damage type
- Time range selection
- Cross-realm statistics tracking
- Plugin management interface
- Dark/Light theme toggle
- CSV/JSON export

**Dependencies:** Core, Plugins, PluginSdk, Avalonia, CommunityToolkit.Mvvm, LiveChartsCore

### CamelotCombatReporter.Plugins

Plugin host infrastructure providing isolation, security, and lifecycle management.

```
CamelotCombatReporter.Plugins/
├── Loading/
│   ├── PluginLoaderService.cs   # Assembly loading
│   └── PluginRegistry.cs        # Plugin registration
│
├── Security/
│   ├── PluginVerificationService.cs  # Signature verification
│   └── SecurityAuditLogger.cs        # Security event logging
│
└── Sandbox/
    ├── PluginSandbox.cs         # Isolated execution context
    ├── FileSystemProxy.cs       # Restricted file access
    ├── NetworkProxy.cs          # Controlled network access
    └── CombatDataProxy.cs       # Read-only combat data
```

**Key Responsibilities:**
- Load plugin assemblies safely
- Verify plugin signatures (optional)
- Provide sandboxed execution environment
- Enforce permission boundaries
- Manage plugin lifecycle

**Dependencies:** Core, PluginSdk

### CamelotCombatReporter.PluginSdk

SDK for third-party plugin developers.

```
CamelotCombatReporter.PluginSdk/
├── Abstractions/
│   ├── IPlugin.cs               # Base plugin interface
│   ├── PluginBase.cs            # Common plugin functionality
│   ├── DataAnalysisPluginBase.cs    # Analysis plugin base
│   └── DataExportPluginBase.cs      # Export plugin base
│
├── Models/
│   ├── PluginManifest.cs        # Manifest schema
│   ├── PluginPermission.cs      # Permission definitions
│   └── AnalysisResult.cs        # Analysis output types
│
└── Context/
    ├── IPluginContext.cs        # Runtime context interface
    └── IPluginLogger.cs         # Logging interface
```

**Dependencies:** Core (models only)

---

## TypeScript Library

The TypeScript implementation provides the same core functionality for Node.js and browser environments.

### Module Structure

| Module | Purpose |
|--------|---------|
| `parser/` | Log file parsing with pattern matching |
| `analysis/` | Combat statistics calculation |
| `streaming/` | Real-time log file watching |
| `database/` | PostgreSQL adapter for data persistence |
| `api/` | REST API endpoints |
| `ml/` | Machine learning models for insights |
| `types/` | TypeScript type definitions |
| `file/` | File I/O utilities |
| `utils/` | Shared utility functions |
| `errors/` | Custom error types |

### Usage

```typescript
import { LogParser } from 'camelot-combat-reporter';

const parser = new LogParser();
const result = await parser.parseFile('./chat.log');

console.log(`Duration: ${result.statistics.durationMinutes} minutes`);
console.log(`Total Damage: ${result.statistics.totalDamage}`);
console.log(`DPS: ${result.statistics.dps}`);
```

---

## Data Flow

### Log Parsing Pipeline

```
┌──────────────┐    ┌──────────────┐    ┌──────────────┐    ┌──────────────┐
│   Log File   │───▶│  LogParser   │───▶│  Log Events  │───▶│  Statistics  │
│  (chat.log)  │    │              │    │   (List<>)   │    │  (Computed)  │
└──────────────┘    └──────────────┘    └──────────────┘    └──────────────┘
                           │
                           ▼
                    ┌──────────────┐
                    │  Regex       │
                    │  Pattern     │
                    │  Matching    │
                    └──────────────┘
```

### Event Types

| Type | Pattern Example | Captured Data |
|------|-----------------|---------------|
| `DamageEvent` | "You hit X for Y damage!" | Source, Target, Amount, DamageType |
| `HealingEvent` | "You heal X for Y hit points" | Source, Target, Amount |
| `CombatStyleEvent` | "You perform Style!" | StyleName, Timestamp |
| `SpellCastEvent` | "You cast Spell!" | SpellName, Timestamp |

### Statistics Calculation

```csharp
record CombatStatistics(
    double DurationMinutes,     // Time span of combat
    int TotalDamage,            // Sum of all damage
    double Dps,                 // TotalDamage / DurationSeconds
    double AverageDamage,       // Mean damage per hit
    double MedianDamage,        // Median damage value
    int CombatStylesCount,      // Unique styles used
    int SpellsCastCount         // Unique spells cast
);
```

---

## Plugin Architecture

### Plugin Types

1. **Data Analysis Plugins** - Process combat data and return statistics
2. **Data Export Plugins** - Export data to custom formats
3. **Visualization Plugins** - Provide custom UI components (planned)

### Plugin Lifecycle

```
┌─────────────┐    ┌─────────────┐    ┌─────────────┐    ┌─────────────┐
│  Discovery  │───▶│  Loading    │───▶│  Initialize │───▶│   Execute   │
│             │    │             │    │             │    │             │
│ Scan plugin │    │ Load DLL    │    │ Call Init() │    │ AnalyzeAsync│
│ directories │    │ Verify sig  │    │ Set context │    │ ExportAsync │
└─────────────┘    └─────────────┘    └─────────────┘    └─────────────┘
                                                                │
                                                                ▼
┌─────────────┐    ┌─────────────┐                      ┌─────────────┐
│   Dispose   │◀───│  Shutdown   │◀─────────────────────│   Result    │
│             │    │             │                      │             │
│ Cleanup     │    │ Graceful    │                      │ Statistics  │
│ Resources   │    │ Termination │                      │ Insights    │
└─────────────┘    └─────────────┘                      └─────────────┘
```

### Security Boundaries

```
┌────────────────────────────────────────────────────────────────┐
│                        Host Application                         │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │                      Plugin Sandbox                       │  │
│  │  ┌────────────┐  ┌────────────┐  ┌────────────────────┐  │  │
│  │  │ Plugin DLL │  │  Proxies   │  │    Permissions     │  │  │
│  │  │            │  │            │  │                    │  │  │
│  │  │ - Analysis │  │ - FileSystem│ │ - FileSystemRead  │  │  │
│  │  │ - Export   │  │ - Network  │  │ - NetworkAccess   │  │  │
│  │  │ - Custom   │  │ - CombatData│ │ - PluginDataWrite │  │  │
│  │  └────────────┘  └────────────┘  └────────────────────┘  │  │
│  └──────────────────────────────────────────────────────────┘  │
└────────────────────────────────────────────────────────────────┘
```

---

## Cross-Realm Analysis

### Architecture

```
┌─────────────┐    ┌──────────────────────┐    ┌─────────────────┐
│ Combat Log  │───▶│  Analysis + Config   │───▶│ Extended Stats  │
│             │    │                      │    │ (with Realm/    │
│             │    │  CharacterInfo:      │    │  Class context) │
│             │    │  - Realm             │    │                 │
│             │    │  - Class             │    │                 │
│             │    │  - Level             │    │                 │
└─────────────┘    └──────────────────────┘    └────────┬────────┘
                                                        │
                                                        ▼
                   ┌──────────────────────┐    ┌─────────────────┐
                   │  CrossRealmService   │───▶│  JSON Storage   │
                   │                      │    │                 │
                   │  - SaveSession       │    │ %APPDATA%/      │
                   │  - GetStatistics     │    │ cross-realm/    │
                   │  - GetLeaderboard    │    │ sessions/       │
                   └──────────────────────┘    └─────────────────┘
                              │
                              ▼
                   ┌──────────────────────┐
                   │   CrossRealmExporter │
                   │                      │
                   │  - ExportToJson()    │
                   │  - ExportToCsv()     │
                   └──────────────────────┘
```

### Storage Format

Sessions are stored as individual JSON files:
```
%APPDATA%/CamelotCombatReporter/cross-realm/
├── sessions/
│   ├── 20240101_120000_albion_cleric_abc123.json
│   ├── 20240101_140000_midgard_healer_def456.json
│   └── ...
└── sessions-index.json
```

---

## Security Model

### Plugin Permissions

| Permission | Access Granted |
|------------|----------------|
| `FileSystemRead` | Read files within plugin directory |
| `FileSystemWrite` | Write files within plugin directory |
| `NetworkAccess` | HTTP/HTTPS requests to specified hosts |
| `PluginDataRead` | Read combat data from analysis |
| `PluginDataWrite` | Store custom plugin data |

### Automatic Grants

All plugins receive these permissions without explicit request:
- Read access to combat statistics
- Write access to analysis results
- Logging capabilities

### Verification

Plugins can optionally be signed with X.509 certificates for verification.

---

## Design Decisions

### Why Avalonia UI?

- **Cross-platform**: Single codebase for Windows, macOS, Linux
- **XAML-based**: Familiar to WPF developers
- **Modern**: Supports MVVM with CommunityToolkit.Mvvm
- **Active**: Strong community and regular updates

### Why Dual Implementation (C# + TypeScript)?

- **C#**: Desktop application with rich UI, plugin system
- **TypeScript**: Web integration, Node.js tooling, npm ecosystem

### Why Local-First Cross-Realm?

- **Privacy**: No data leaves user's machine by default
- **Simplicity**: No server infrastructure required
- **Export**: Users can share data voluntarily via JSON/CSV

### Why Plugin Sandboxing?

- **Security**: Plugins can't access arbitrary system resources
- **Stability**: Plugin crashes don't affect host application
- **Trust**: Users can install third-party plugins safely

---

## Future Considerations

### Planned Enhancements

1. **Central Server** (Optional)
   - Community statistics aggregation
   - Public leaderboards
   - Opt-in data sharing

2. **Auto-Detection**
   - Detect character class from combat patterns
   - Reduce manual configuration

3. **Machine Learning**
   - Combat pattern recognition
   - Performance predictions
   - Anomaly detection

### Extensibility Points

- Plugin system for custom analysis
- Export format plugins
- Theme customization
- Localization support

---

## Related Documentation

- [Plugin Developer Guide](docs/plugins/getting-started.md)
- [Plugin API Reference](docs/plugins/api-reference.md)
- [Feature Roadmap](roadmap/README.md)
- [Contributing Guidelines](CONTRIBUTING.md)
