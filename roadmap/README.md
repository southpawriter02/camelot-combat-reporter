# Camelot Combat Reporter Roadmap

This document outlines the feature roadmap for Camelot Combat Reporter. The project provides powerful tools for players of Dark Age of Camelot to analyze their combat logs, gain insights into their performance, and improve their gameplay.

---

## Current Version: v1.0.2

Released: January 2025

### What's in v1.0.x

**Core Parsing & Analysis**
- Full combat log parsing with regex-based event extraction
- Damage dealt/received tracking with source attribution
- Healing done/received analysis
- Player statistics aggregation (DPS, HPS, K/D ratios)
- Timeline visualization with LiveCharts2
- Critical hit, pet damage, death, resist, and CC event parsing (v1.0.2)

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

| Feature | Status | Target | Location |
|---------|--------|--------|----------|
| [Plugin System](future-enhancements/01-plugin-system.md) | ✅ Complete | v1.0.0 | `src/CamelotCombatReporter.Plugins/`, `src/CamelotCombatReporter.PluginSdk/` |
| [Machine Learning Insights](future-enhancements/02-machine-learning-insights.md) | 📋 Planned | v2.0.0 | — |
| [Cross-Realm Analysis](future-enhancements/03-cross-realm-analysis.md) | ✅ Phase 1 | v1.0.0 | `src/CamelotCombatReporter.Core/CrossRealm/`, `src/CamelotCombatReporter.Gui/CrossRealm/` |
| [Loot Drop Rate Tracking](future-enhancements/04-loot-drop-tracking.md) | ✅ Complete | v1.0.0 | `src/CamelotCombatReporter.Core/LootTracking/`, `src/CamelotCombatReporter.Gui/LootTracking/` |
| [Realm Ability Tracking](future-enhancements/05-realm-ability-tracking.md) | 📋 Planned | v1.2.0 | — |
| [Server Type Filters](future-enhancements/06-server-type-filters.md) | 📋 Planned | v1.1.0 | — |
| [Distribution Builds](future-enhancements/07-distribution-builds.md) | ✅ Phase 1 | v1.0.0 | `.github/workflows/`, `Directory.Build.props` |
| [Chat Filtering](future-enhancements/08-chat-filtering.md) | 📋 Planned | v1.1.0 | — |
| [Group Composition Analysis](future-enhancements/09-group-composition-analysis.md) | 📋 Planned | v1.4.0 | — |
| [Keep and Siege Tracking](future-enhancements/10-keep-siege-tracking.md) | 📋 Planned | v1.5.0 | — |
| [Combat Replay System](future-enhancements/11-combat-replay.md) | 📋 Planned | v2.0.0 | — |
| [Voice Chat Integration](future-enhancements/12-voice-integration.md) | 📋 Planned | v2.0.0 | — |
| [In-Game Overlay HUD](future-enhancements/13-overlay-hud.md) | 📋 Planned | v2.0.0 | — |
| [Death Analysis](future-enhancements/14-death-analysis.md) | 📋 Planned | v1.1.0 | — |
| [Buff/Debuff Tracking](future-enhancements/15-buff-debuff-tracking.md) | 📋 Planned | v1.2.0 | — |
| [Crowd Control Analysis](future-enhancements/16-crowd-control-analysis.md) | 📋 Planned | v1.1.0 | — |
| [Combat Alerts](future-enhancements/17-combat-alerts.md) | 📋 Planned | v1.3.0 | — |
| [Session Comparison](future-enhancements/18-session-comparison.md) | 📋 Planned | v1.3.0 | — |

**Legend:**
- ✅ Complete - Feature fully implemented and tested
- ✅ Phase 1 - Initial implementation complete, future phases planned
- ⏸️ Deferred - Implementation postponed
- 📋 Planned - Not yet started

---

## Release Schedule

### v1.1.0 - Combat Intelligence

**Focus:** Death and crowd control analysis (builds on v1.0.2 events)

| Item | Type | Description |
|------|------|-------------|
| [Death Analysis](future-enhancements/14-death-analysis.md) | Feature | Pre-death analysis, killing blow breakdown |
| [Crowd Control Analysis](future-enhancements/16-crowd-control-analysis.md) | Feature | CC chains, diminishing returns tracking |
| [Server Type Filters](future-enhancements/06-server-type-filters.md) | Feature | Classic, SI, ToA, Live server profiles |
| [Chat Filtering](future-enhancements/08-chat-filtering.md) | Feature | Pre-parse filtering for non-combat messages |
| Death Report UI | Feature | Visual death timeline and recommendations |
| Bug Fixes | Maintenance | Address issues from v1.0.x community feedback |

**Dependencies:** Uses DeathEvent, CrowdControlEvent, ResistEvent from v1.0.2

---

### v1.2.0 - Realm Abilities & Buffs

**Focus:** Advanced combat tracking

| Item | Type | Description |
|------|------|-------------|
| [Realm Ability Tracking](future-enhancements/05-realm-ability-tracking.md) | Feature | RA usage statistics and cooldown tracking |
| [Buff/Debuff Tracking](future-enhancements/15-buff-debuff-tracking.md) | Feature | Buff uptime, debuff effectiveness |
| RA Database | Feature | Complete RA data for all three realms |
| Buff Timeline Widget | Feature | Visual buff bar with duration tracking |
| Bug Fixes | Maintenance | Monthly bug fix cycle |

---

### v1.3.0 - Alerts & Comparison

**Focus:** Proactive feedback and historical analysis

| Item | Type | Description |
|------|------|-------------|
| [Combat Alerts](future-enhancements/17-combat-alerts.md) | Feature | Real-time alert engine with configurable triggers |
| [Session Comparison](future-enhancements/18-session-comparison.md) | Feature | Side-by-side session analysis, trend visualization |
| Audio Notifications | Feature | Sound effects and TTS alerts |
| Goal Tracking | Feature | Personal goals with progress tracking |
| Personal Best Tracking | Feature | Automatic PB detection and celebration |
| Bug Fixes | Maintenance | Monthly bug fix cycle |

---

### v1.4.0 - Group Analysis

**Focus:** Group and team performance

| Item | Type | Description |
|------|------|-------------|
| [Group Composition Analysis](future-enhancements/09-group-composition-analysis.md) | Feature | Group member detection and role analysis |
| Team Performance Metrics | Feature | Coordinated damage/healing tracking |
| Role Distribution | Feature | Tank, healer, DPS role classification |
| Performance Correlation | Feature | Link composition to success rates |
| Bug Fixes | Maintenance | Monthly bug fix cycle |

---

### v1.5.0 - RvR Features

**Focus:** Realm vs Realm combat tracking

| Item | Type | Description |
|------|------|-------------|
| [Keep and Siege Tracking](future-enhancements/10-keep-siege-tracking.md) | Feature | Door/structure damage, siege scoring |
| Keep Capture History | Feature | Track keep take participation |
| Relic Tracking | Feature | Relic raid contribution metrics |
| Battleground Statistics | Feature | BG-specific performance tracking |
| Bug Fixes | Maintenance | Monthly bug fix cycle |

---

### v1.6.0 - Distribution & Community

**Focus:** Improved installers and community features

| Item | Type | Description |
|------|------|-------------|
| Distribution Builds Phase 2 | Feature | Windows MSI installer, macOS DMG with notarization |
| Linux Packages | Feature | AppImage, .deb, .rpm package support |
| Auto-Update | Feature | In-app update checking and installation |
| Cross-Realm Analysis Phase 2 | Feature | Central server for community statistics |
| Public Leaderboards | Feature | Opt-in anonymous leaderboard participation |
| Bug Fixes | Maintenance | Monthly bug fix cycle |

---

### v1.7.0 - Polish & Preparation

**Focus:** Stability and v2.0 preparation

| Item | Type | Description |
|------|------|-------------|
| Performance Audit | Maintenance | Full performance optimization pass |
| UX Improvements | Enhancement | UI/UX refinements based on feedback |
| Documentation Update | Maintenance | Comprehensive docs for all v1.x features |
| API Stabilization | Maintenance | Lock down public API for v2.0 compatibility |
| Bug Fixes | Maintenance | Final v1.x bug fixes |

---

### v2.0.0 - Next Generation

**Focus:** Major new capabilities

| Item | Type | Description |
|------|------|-------------|
| [Combat Replay System](future-enhancements/11-combat-replay.md) | Feature | Full combat reconstruction and playback |
| [In-Game Overlay HUD](future-enhancements/13-overlay-hud.md) | Feature | Real-time overlay during gameplay |
| [Voice Chat Integration](future-enhancements/12-voice-integration.md) | Feature | Voice comms sync with combat events |
| [Machine Learning Insights](future-enhancements/02-machine-learning-insights.md) | Feature | AI-powered performance predictions |
| Mobile Companion App | Feature | View stats on mobile devices |

---

## Maintenance Schedule

### Bug Fix Releases (x.x.1, x.x.2, etc.)

Bug fix releases occur as needed between feature releases:

- **Critical bugs:** Patch release within 1-2 days
- **High priority bugs:** Next scheduled patch (weekly if needed)
- **Low priority bugs:** Bundled with next minor release

### Long-Term Support

- **v1.x Series:** Supported until 6 months after v2.0.0 release
- **Security patches:** Provided for 12 months after major version EOL

---

## Version History

### v1.0.2 (January 2025)

**Log Parsing Accuracy:**
- Fixed damage patterns to match actual game formats
- Added damage modifiers, body parts, weapon tracking
- New events: CriticalHit, PetDamage, Death, Resist, CrowdControl

### v1.0.1 (January 2025)

**Quality Improvements:**
- Comprehensive logging infrastructure
- Async/await bug fixes
- IDisposable resource management
- Error handling improvements

### v1.0.0 (January 2025)

**Initial Release:**
- Full combat log parsing engine
- GUI application with Avalonia
- Plugin system with SDK
- Cross-realm analysis (Phase 1)
- Loot drop rate tracking
- CI/CD pipeline for multi-platform releases

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

## Feature Dependencies

```
v1.0.2 (Events Foundation)
    │
    ├── v1.1.0 Death Analysis ──────────┐
    │       └── Uses: DeathEvent        │
    │                                   │
    ├── v1.1.0 CC Analysis ─────────────┤
    │       └── Uses: CrowdControlEvent │
    │                                   │
    └── v1.2.0 Buff/Debuff ─────────────┤
            └── Uses: ResistEvent       │
                                        │
                                        ▼
                              v1.3.0 Combat Alerts
                                  └── Uses: All events
                                        │
                                        ▼
                              v2.0.0 Combat Replay
                                  └── Full event reconstruction
```

---

## Contributing

See [CONTRIBUTING.md](../CONTRIBUTING.md) for guidelines on:
- Submitting bug reports
- Proposing new features
- Contributing code
- Writing documentation

---

## Related Documentation

- [Architecture Overview](../ARCHITECTURE.md)
- [Contributing Guide](../CONTRIBUTING.md)
- [Changelog](../CHANGELOG.md)
- [Plugin Developer Guide](../docs/plugins/getting-started.md)
- [Plugin SDK Reference](../docs/plugins/api-reference.md)
