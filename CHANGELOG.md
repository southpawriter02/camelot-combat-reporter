# Changelog

All notable changes to Camelot Combat Reporter are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

No unreleased changes.

---

## [1.1.0] - 2025-01-05

### Added
- **Death Analysis System**
  - Pre-death damage reconstruction with configurable 15-second analysis window
  - Death categorization into 9 types: Burst (Alpha Strike, Coordinated, CC Chain), Attrition (Healing Deficit, Resource Exhaustion, Positional), Execution (Low Health, DoT), Environmental
  - Killing blow identification with attacker class detection
  - Time-to-death (TTD) calculation from first damage to death
  - Per-death recommendations based on death category and circumstances
  - Damage timeline visualization showing DPS per second before death
  - Death statistics: total deaths, average TTD, top killer class, CC death percentage
  - New "Death Analysis" tab in main window

- **Crowd Control Analysis**
  - Full CC tracking with Diminishing Returns (DR) system
  - DR levels: Full (100%) → Reduced (50%) → Minimal (25%) → Immune (0%)
  - 60-second DR decay timer per target per CC type
  - CC chain detection with configurable gap threshold
  - Support for mez, root, snare, stun, silence, and disarm effects
  - CC timeline visualization with DR level color coding
  - CC statistics: total applications, chain count, average chain length
  - New "CC Analysis" tab in main window

- **Server Profile System**
  - Era-based server profiles: Classic, Shrouded Isles, Trials of Atlantis, New Frontiers, Live, Custom
  - Per-profile class availability and feature flags (Master Levels, Artifacts, Champion Levels, Maulers)
  - Built-in profiles with appropriate era settings
  - Custom profile creation with manual class/feature selection
  - Profile persistence to JSON configuration
  - Settings view for server profile management

- **Chat Filtering System**
  - Pre-parse filtering with 16 chat message types (Say, Yell, Group, Guild, Alliance, Broadcast, Send, Tell, Region, Trade, Emote, NpcDialog, Combat, LFG, Advice, System)
  - Filter presets: All Messages, Combat Only, Tactical, Custom
  - Per-channel enable/disable with "keep during combat" option
  - Keyword whitelist for always-keep messages
  - Sender whitelist for important players
  - Combat context window (configurable seconds around combat)
  - Privacy mode with player name anonymization
  - Settings view for chat filter configuration

- **Settings Window**
  - Unified settings interface with tabbed navigation
  - Server Profiles tab for era selection
  - Chat Filtering tab with preset and channel configuration
  - Privacy Settings tab for anonymization options

- **Enhanced Log Parsing**
  - Extended `CrowdControlEvent` with Source and Duration fields
  - New CC parsing patterns for mez, root, snare, silence, and disarm effects
  - Filter pipeline infrastructure for pre-parse filtering
  - Filter context tracking for combat state awareness

- **New Core Services**
  - `IDeathAnalysisService` / `DeathAnalysisService` - Death analysis and categorization
  - `ICCAnalysisService` / `CCAnalysisService` - CC tracking and chain detection
  - `DRTracker` - Diminishing returns state management
  - `ServerProfileService` - Server profile management
  - `ChatFilter` - Chat message filtering with presets
  - `FilterPipeline` - Composable filter chain
  - `PrivacyAnonymizer` - Player name anonymization

### Changed
- MainWindow now includes Death Analysis, CC Analysis tabs
- Menu bar includes Settings option for configuration access
- About dialog updated to show version 1.1.0

### Technical Details
- 54 new files added across Core and GUI projects
- New namespaces: `DeathAnalysis`, `CrowdControlAnalysis`, `ServerProfiles`, `ChatFiltering`, `Filtering`
- Full MVVM implementation with CommunityToolkit.Mvvm source generators
- LiveCharts2 integration for timeline and distribution charts
- All existing tests continue to pass

---

## [1.0.2] - 2025-01-03

### Fixed
- **Log Parsing Accuracy**
  - Fixed damage patterns to match actual game log formats (removed incorrect "points of" requirement)
  - Fixed damage taken pattern to correctly parse "hits you" and "hits your torso" formats
  - Added support for damage modifiers in parentheses (e.g., `+28` or `-9`)
  - Added body part capture for incoming damage (e.g., "hits your torso")

### Added
- **New Combat Event Types**
  - `CriticalHitEvent` - Parse critical hit lines with optional percentage display
  - `PetDamageEvent` - Track damage dealt by player pets (e.g., "Your wolf sage attacks...")
  - `DeathEvent` - Track mob deaths and kills (e.g., "The swamp rat dies!")
  - `ResistEvent` - Track spell/effect resists
  - `CrowdControlEvent` - Track stun applied/recovered events

- **New Damage Patterns**
  - Melee attack pattern: "You attack X with your weapon and hit for N damage!"
  - Ranged attack pattern: "You shot X with your bow and hit for N damage!"
  - Alternate damage taken: "You are hit for N damage."

- **Enhanced DamageEvent Model**
  - `Modifier` property - Captures (+N) or (-N) damage modifiers
  - `BodyPart` property - Captures hit location (torso, head, etc.)
  - `WeaponUsed` property - Captures weapon used for melee/ranged attacks

- **Test Coverage**
  - Added 28 new tests for v1.0.2 patterns using real log samples from roadmap/logs
  - Total test count increased from 54 to 82

### Technical Details
- All 82 tests passing (62 Core + 20 GUI)
- New event types in `CombatEvents.cs`
- 12 new regex patterns in `LogParser.cs`
- Full backwards compatibility with existing damage parsing

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
| 1.1.0 | 2025-01-05 | Death Analysis, CC Analysis with DR tracking, Server Profiles, Chat Filtering |
| 1.0.2 | 2025-01-03 | Log parsing fixes, new event types (crit, pet, death, CC) |
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
