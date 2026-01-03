# Changelog

All notable changes to Camelot Combat Reporter are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

No unreleased changes.

---

## [1.3.0] - 2025-01-02

### Added
- **Cross-Realm Analysis** - Track combat statistics by realm and character class
  - Character configuration dialog for realm, class, level, and realm rank
  - Session saving with character context to local JSON storage
  - Aggregated statistics by realm (Albion, Midgard, Hibernia)
  - Aggregated statistics by class (47 classes across all realms)
  - Local leaderboards for DPS, HPS, K/D ratio, and other metrics
  - JSON/CSV export for cross-realm statistics with privacy options
  - New "Cross-Realm Analysis" tab in the main window

- **Documentation Improvements**
  - Comprehensive ARCHITECTURE.md with system design and diagrams
  - CONTRIBUTING.md with development guidelines and coding standards
  - CHANGELOG.md following Keep a Changelog format
  - Updated roadmap with current implementation status
  - XML documentation for Core.Models and CrossRealm services

### Changed
- Main window now uses TabControl with two tabs: Combat Analysis and Cross-Realm Analysis
- Updated roadmap README with accurate feature locations and status

---

## [1.2.0] - 2025-01-02

### Added
- **Plugin System** - Full extensibility framework for third-party plugins
  - Plugin SDK with base classes for data analysis and export plugins
  - Sandboxed plugin execution with permission-based security
  - Plugin Manager UI for installing, enabling, and managing plugins
  - Comprehensive plugin documentation with examples
  - Support for plugin manifests with versioning and dependencies

### Documentation
- Plugin developer guide (docs/plugins/getting-started.md)
- API reference for plugin development (docs/plugins/api-reference.md)
- Example plugins: DPS Calculator, HTML Exporter, Damage Timeline, Critical Hit Parser

---

## [1.1.0] - 2025-01-02

### Added
- **GUI Enhancements** (PR #8)
  - Event type filtering (damage dealt/taken, healing done/received, combat styles, spells)
  - Damage type and target filtering with dropdowns
  - Time range selection with presets (First 5m, Last 5m, First 10m, Last 10m, All)
  - Statistics visibility toggles for customizing the display
  - Log comparison mode for comparing two combat logs
  - Detailed event table with search/filter functionality
  - Quick stats summary bar showing key metrics

- **Charts and Visualization** (PR #7)
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

---

## [1.0.0] - 2025-01-01

### Added
- **Avalonia GUI Application** (PR #6)
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
| 1.3.0 | 2025-01-02 | Cross-realm analysis, documentation improvements |
| 1.2.0 | 2025-01-02 | Plugin system with SDK and security |
| 1.1.0 | 2025-01-02 | GUI enhancements, filtering, charts |
| 1.0.0 | 2025-01-01 | Avalonia GUI, core features complete |
| 0.2.0 | 2024-12-31 | Enhanced statistics, damage taken |
| 0.1.0 | 2024-12-30 | Initial C# port from Python |

---

## Links

- [GitHub Repository](https://github.com/southpawriter02/camelot-combat-reporter)
- [Roadmap](roadmap/README.md)
- [Architecture](ARCHITECTURE.md)
- [Contributing](CONTRIBUTING.md)
