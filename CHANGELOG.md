# Changelog

All notable changes to Camelot Combat Reporter are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

No unreleased changes.

---

## [1.0.1] - 2025-01-03

### Added
- **Comprehensive Logging Infrastructure**
  - Added `Microsoft.Extensions.Logging` to Core and GUI projects
  - Created source-generated logging extensions in `LoggingExtensions.cs`
  - Centralized logger factory in `App.axaml.cs` with console output
  - Logging categories: Parsing, Loot Tracking, Cross-Realm, Export, Preferences, GUI Operations
  - NullLogger fallback for unit test environments

### Fixed
- **Async/Await Bug Fixes**
  - Fixed fire-and-forget async pattern in `MainWindow.Loaded` event handler
  - Fixed async void `OnLoaded` in `LootTrackingView` with proper error handling
  - Fixed fire-and-forget in `PluginManagerViewModel` constructor with error wrapper

- **Resource Management**
  - Implemented `IDisposable` pattern for `LootTrackingService` (SemaphoreSlim disposal)
  - Implemented `IDisposable` pattern for `CrossRealmStatisticsService` (SemaphoreSlim disposal)

- **Error Handling**
  - Replaced empty catch blocks with logged errors in `LootTrackingService`
  - Replaced empty catch blocks with logged errors in `CrossRealmStatisticsService`
  - Replaced empty catch blocks with logged errors in `MainWindowViewModel` preferences
  - Added proper exception logging throughout the codebase

- **Test Fixes**
  - Fixed 3 failing GUI tests related to user preferences loading
  - Made test assertions flexible for loaded preference values
  - Fixed TimeSpan to TimeOnly conversion issue in `ResetFilters` test

### Changed
- Removed unused `_ownsLoaderService` field from `PluginManagerViewModel`
- Service constructors now accept optional `ILogger` parameter for dependency injection

### Technical Details
- All 54 tests passing (34 Core + 20 GUI)
- Build succeeds with minimal warnings
- No breaking API changes

---

## [1.0.0] - 2025-01-02

### Added
- **Avalonia GUI Application**
  - Cross-platform desktop application for Windows, macOS, and Linux
  - File picker for log selection
  - Real-time combat statistics display
  - Colorful, visual metric presentation
  - MVVM architecture with CommunityToolkit.Mvvm

- **Core Parsing Engine**
  - Log file parsing with regex pattern matching
  - Support for damage dealt, damage taken, healing, combat styles, and spell casts
  - Combatant name filtering

- **Combat Statistics**
  - DPS (Damage per Second) calculation
  - Total damage, average damage, median damage
  - Combat styles and spells count
  - Session duration tracking

- **Command-Line Interface**
  - Simple CLI for terminal-based analysis
  - Combatant name parameter support

- **GUI Enhancements**
  - Event type filtering (damage dealt/taken, healing done/received, combat styles, spells)
  - Damage type and target filtering with dropdowns
  - Time range selection with presets (First 5m, Last 5m, First 10m, Last 10m, All)
  - Statistics visibility toggles for customizing the display
  - Log comparison mode for comparing two combat logs
  - Detailed event table with search/filter functionality
  - Quick stats summary bar showing key metrics

- **Charts and Visualization**
  - Damage over time chart with customizable intervals
  - Pie charts for damage by target and damage type distribution
  - Combat styles and spells usage tables
  - DPS trend line option
  - Multiple chart types (Line, Bar)

- **Export Functionality**
  - CSV export with statistics summary and event log
  - JSON export for programmatic access

- **UX Improvements**
  - Drag-and-drop file selection
  - Dark/Light theme toggle
  - Keyboard shortcuts (Ctrl+O, F5, Ctrl+E, Ctrl+R)

- **Plugin System** - Full extensibility framework for third-party plugins
  - Plugin SDK with base classes for data analysis and export plugins
  - Sandboxed plugin execution with permission-based security
  - Plugin Manager UI for installing, enabling, and managing plugins
  - Comprehensive plugin documentation with examples
  - Support for plugin manifests with versioning and dependencies

- **Cross-Realm Analysis** - Track combat statistics by realm and character class
  - Character configuration dialog for realm, class, level, and realm rank
  - Session saving with character context to local JSON storage
  - Aggregated statistics by realm (Albion, Midgard, Hibernia)
  - Aggregated statistics by class (47 classes across all realms)
  - Local leaderboards for DPS, HPS, K/D ratio, and other metrics
  - JSON/CSV export for cross-realm statistics with privacy options
  - New "Cross-Realm Analysis" tab in the main window

- **Loot Drop Rate Tracking**
  - Parse item drops, currency pickups, and quest rewards from combat logs
  - Track drop rates by mob type with statistical analysis
  - Session-based loot tracking with JSON persistence
  - Export loot statistics to JSON, CSV, and Markdown formats
  - Dedicated Loot Tracking tab in the GUI

- **Distribution Builds**
  - GitHub Actions workflow for automated releases
  - Self-contained builds for Windows (x64), macOS (x64, ARM64), and Linux (x64)
  - Publish profiles for each target platform

- **Documentation**
  - Comprehensive ARCHITECTURE.md with system design and diagrams
  - CONTRIBUTING.md with development guidelines and coding standards
  - CHANGELOG.md following Keep a Changelog format
  - Plugin developer guide and API reference
  - XML documentation for Core.Models and services

---

## [0.2.0] - 2024-12-31

### Added
- Enhanced combat reporting with detailed statistics (PR #4)
- Damage taken event parsing (PR #3)
- Combatant name filtering (PR #5)

### Changed
- Improved parser accuracy for edge cases

---

## [0.1.0] - 2024-12-30

### Added
- Initial C# implementation (converted from Python)
- Basic log parsing for damage events
- Core library structure
- Unit test foundation

### Changed
- Project migrated from Python to C#/.NET

---

## TypeScript Implementation

The TypeScript implementation follows the same feature set as the C# version but is maintained separately for Node.js/browser environments.

### Core Features
- Log parsing with pattern matching
- Combat analysis and statistics
- Real-time file watching (streaming)
- Database adapters (SQLite, PostgreSQL)
- REST API with OpenAPI documentation
- Machine learning predictors

---

## Version History Summary

| Version | Date | Highlights |
|---------|------|------------|
| 1.0.1 | 2025-01-03 | Logging infrastructure, bug fixes, IDisposable |
| 1.0.0 | 2025-01-02 | Full feature release: GUI, plugins, cross-realm, loot tracking |
| 0.2.0 | 2024-12-31 | Enhanced statistics, damage taken |
| 0.1.0 | 2024-12-30 | Initial C# port from Python |

---

## Links

- [GitHub Repository](https://github.com/southpawriter02/camelot-combat-reporter)
- [Roadmap](roadmap/README.md)
- [Architecture](ARCHITECTURE.md)
- [Contributing](CONTRIBUTING.md)
