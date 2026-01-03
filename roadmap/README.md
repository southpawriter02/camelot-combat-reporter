# Camelot Combat Reporter Roadmap

This document outlines the feature roadmap for Camelot Combat Reporter. The project provides powerful tools for players of Dark Age of Camelot to analyze their combat logs, gain insights into their performance, and improve their gameplay.

---

## Current Version: v1.3.0

Released: January 2025

### What's in v1.0.0

**Core Parsing & Analysis**
- Full combat log parsing with regex-based event extraction
- Damage dealt/received tracking with source attribution
- Healing done/received analysis
- Player statistics aggregation (DPS, HPS, K/D ratios)
- Timeline visualization with LiveCharts2

**GUI Application**
- Cross-platform desktop app (Windows, macOS, Linux) using Avalonia
- Drag-and-drop log file import
- Dark/light theme toggle
- Real-time filtering and search
- Export to JSON/CSV

**Loot Tracking**
- Item drop parsing from combat logs
- Currency pickup tracking (gold/silver/copper)
- Drop rate calculations with 95% confidence intervals
- Mob loot table aggregation
- Session-based statistics persistence

**Plugin System**
- Plugin SDK for third-party developers
- Sandboxed execution with permission-based security
- Plugin Manager UI with enable/disable controls
- Comprehensive documentation and examples

**Cross-Realm Analysis**
- Character configuration (realm, class, level, realm rank)
- Session saving with character context
- Local leaderboards and aggregated statistics

**Distribution Builds**
- GitHub Actions CI/CD pipelines
- Self-contained builds for Windows x64, macOS (x64/ARM64), Linux x64
- Portable ZIP archives for all platforms
- Automated release creation on version tags

---

## Implementation Status

### Core Features (4/4 Complete)

| Feature | Status | Version | Location |
|---------|--------|---------|----------|
| [Log Parsing](core-features/01-log-parsing.md) | ✅ Complete | v1.0.0 | `src/CamelotCombatReporter.Core/Parsing/` |
| [Combat Analysis](core-features/02-combat-analysis.md) | ✅ Complete | v1.0.0 | `src/CamelotCombatReporter.Core/` |
| [Player Statistics](core-features/03-player-statistics.md) | ✅ Complete | v1.0.0 | `src/CamelotCombatReporter.Core/Models/` |
| [Timeline View](core-features/04-timeline-view.md) | ✅ Complete | v1.0.0 | `src/CamelotCombatReporter.Gui/` |

### Advanced Features (3/4 Complete)

| Feature | Status | Version | Location |
|---------|--------|---------|----------|
| [UI Dashboard](advanced-features/01-ui-dashboard.md) | ⏸️ Deferred | — | — |
| [Real-Time Parsing](advanced-features/02-real-time-parsing.md) | ✅ Complete | v1.0.0 | `src/streaming/` (TypeScript) |
| [Database Integration](advanced-features/03-database-integration.md) | ✅ Complete | v1.0.0 | `src/database/` (TypeScript) |
| [API Exposure](advanced-features/04-api-exposure.md) | ✅ Complete | v1.0.0 | `src/api/` (TypeScript) |

### Future Enhancements (4/18 Complete)

| Feature | Status | Version | Location |
|---------|--------|---------|----------|
| [Plugin System](future-enhancements/01-plugin-system.md) | ✅ Complete | v1.0.0 | `src/CamelotCombatReporter.Plugins/`, `src/CamelotCombatReporter.PluginSdk/` |
| [Machine Learning Insights](future-enhancements/02-machine-learning-insights.md) | 📋 Planned | — | — |
| [Cross-Realm Analysis](future-enhancements/03-cross-realm-analysis.md) | ✅ Phase 1 | v1.0.0 | `src/CamelotCombatReporter.Core/CrossRealm/`, `src/CamelotCombatReporter.Gui/CrossRealm/` |
| [Loot Drop Rate Tracking](future-enhancements/04-loot-drop-tracking.md) | ✅ Complete | v1.0.0 | `src/CamelotCombatReporter.Core/LootTracking/`, `src/CamelotCombatReporter.Gui/LootTracking/` |
| [Realm Ability Tracking](future-enhancements/05-realm-ability-tracking.md) | 📋 Planned | — | — |
| [Server Type Filters](future-enhancements/06-server-type-filters.md) | 📋 Planned | — | — |
| [Distribution Builds](future-enhancements/07-distribution-builds.md) | ✅ Phase 1 | v1.0.0 | `.github/workflows/`, `Directory.Build.props` |
| [Chat Filtering](future-enhancements/08-chat-filtering.md) | 📋 Planned | — | — |
| [Group Composition Analysis](future-enhancements/09-group-composition-analysis.md) | 📋 Planned | — | — |
| [Keep and Siege Tracking](future-enhancements/10-keep-siege-tracking.md) | 📋 Planned | — | — |
| [Combat Replay System](future-enhancements/11-combat-replay.md) | 📋 Planned | — | — |
| [Voice Chat Integration](future-enhancements/12-voice-integration.md) | 📋 Planned | — | — |
| [In-Game Overlay HUD](future-enhancements/13-overlay-hud.md) | 📋 Planned | — | — |
| [Death Analysis](future-enhancements/14-death-analysis.md) | 📋 Planned | — | — |
| [Buff/Debuff Tracking](future-enhancements/15-buff-debuff-tracking.md) | 📋 Planned | — | — |
| [Crowd Control Analysis](future-enhancements/16-crowd-control-analysis.md) | 📋 Planned | — | — |
| [Combat Alerts](future-enhancements/17-combat-alerts.md) | 📋 Planned | — | — |
| [Session Comparison](future-enhancements/18-session-comparison.md) | 📋 Planned | — | — |

**Legend:**
- ✅ Complete - Feature fully implemented and tested
- ✅ Phase 1 - Initial implementation complete, future phases planned
- ⏸️ Deferred - Implementation postponed
- 📋 Planned - Not yet started

---

## Version History

### v1.0.0 (January 2025)

**New Features:**
- Loot Drop Rate Tracking with statistical analysis
- Distribution builds via GitHub Actions
- Cross-platform publish profiles (Windows, macOS, Linux)
- Centralized version management with Directory.Build.props

**Core Components:**
- Full combat log parsing engine
- GUI application with Avalonia
- Plugin system with SDK
- Cross-realm analysis (Phase 1)

**Infrastructure:**
- CI/CD pipeline for build and test
- Release workflow for multi-platform distribution
- Self-contained executables (~107MB)

---

## What's Next

### v1.1.0 Candidates

#### Distribution Builds Phase 2
- Windows MSI installer with WiX Toolset
- macOS DMG with notarization
- Linux AppImage, .deb, .rpm packages
- Auto-update mechanism

#### Cross-Realm Analysis Phase 2
- Central server for community statistics
- Public leaderboards
- Auto-detection of character class from logs
- Opt-in anonymous data sharing

#### Realm Ability Tracking
- Track RA activations and cooldowns
- Measure damage/healing contribution per RA
- Monitor realm point progression
- RA spec optimization suggestions

### Future Releases

#### Server Type Filters
- Support for Classic, SI, ToA, and Live servers
- Filter classes and abilities by era
- Custom profiles for private servers

#### Chat Filtering
- Pre-parse filtering for performance
- Configurable channel filters
- Privacy-safe export options

#### Group Composition Analysis
- Detect group members and their classes
- Role distribution analysis
- Performance correlation with composition

#### Keep and Siege Tracking
- Door and structure damage tracking
- Siege contribution scoring
- Keep capture history

---

## Plugin Ideas

The plugin system enables third-party extensibility. See [Plugin Ideas](plugin-ideas/README.md) for concepts and specifications.

### Data Analysis Plugins

| Plugin | Description | Complexity |
|--------|-------------|------------|
| [Combat Style Optimizer](plugin-ideas/01-combat-style-optimizer.md) | Analyze melee style chains and recommend optimal rotations | Medium |
| [Realm Points Calculator](plugin-ideas/02-realm-points-calculator.md) | Track RP gains, rank progression, and time-to-next projections | Medium |
| [Group Performance Analyzer](plugin-ideas/03-group-performance-analyzer.md) | Measure individual contributions within group combat | Medium-High |
| [Heal Efficiency Tracker](plugin-ideas/09-heal-efficiency-tracker.md) | Track effective healing, overhealing, and reaction times | Medium |
| [PvP Matchup Analyzer](plugin-ideas/10-pvp-matchup-analyzer.md) | Win/loss tracking by class and player with tactical insights | Medium-High |

### Export Plugins

| Plugin | Description | Complexity |
|--------|-------------|------------|
| [HTML Report Exporter](plugin-ideas/04-html-report-exporter.md) | Generate shareable HTML reports with charts and tables | Medium |
| [Discord Bot Integration](plugin-ideas/08-discord-bot-integration.md) | Post combat summaries to Discord via webhooks | Low-Medium |

### UI Plugins

| Plugin | Description | Complexity |
|--------|-------------|------------|
| [Damage Breakdown Chart](plugin-ideas/05-damage-breakdown-chart.md) | Interactive sunburst/treemap visualization | Medium |
| [Enemy Encounter Database](plugin-ideas/06-enemy-encounter-database.md) | Historical database of enemy encounters | Medium-High |

### Utility Plugins

| Plugin | Description | Complexity |
|--------|-------------|------------|
| [Combat Log Merger](plugin-ideas/07-combat-log-merger.md) | Merge multiple log files into unified timeline | Medium |

---

## Related Documentation

- [Architecture Overview](../ARCHITECTURE.md)
- [Contributing Guide](../CONTRIBUTING.md)
- [Changelog](../CHANGELOG.md)
- [Plugin Developer Guide](../docs/plugins/getting-started.md)
- [Plugin SDK Reference](../docs/plugins/api-reference.md)
