# Camelot Combat Reporter Roadmap

This document outlines the feature roadmap for Camelot Combat Reporter. The project provides powerful tools for players of Dark Age of Camelot to analyze their combat logs, gain insights into their performance, and improve their gameplay.

---

## Current Version: v1.7.0

Released: January 2026

### What's in v1.7.x

**GUI Polish** (New in v1.7.0)
- Centralized style resources (Colors, Brushes, Styles, Icons)
- ThemeService with System/Light/Dark mode support and JSON persistence
- Loading indicators: LoadingSpinner, LoadingOverlay, ProgressCard controls
- Keyboard shortcuts with F1 help window and Ctrl+T theme toggle
- Status bar with event count, theme status, and version display

**Performance Optimization** (New in v1.7.0)
- Async log parser with IProgress<ParseProgress> support
- Statistics caching layer with SHA256 file hash validation
- LRU cache eviction with configurable max entries
- DataGrid virtualization using VirtualizingStackPanel
- StringPool for memory-efficient string interning
- ObjectPool<T> and StringBuilderPool for reduced allocations
- BenchmarkDotNet performance benchmarks for parser, cache, and pools

### What's in v1.6.x

**Windows MSI Installer** (New in v1.6.0)
- WiX Toolset v4-based installer with full installation flow
- File associations for `.combat` and `.log` files
- Start menu shortcuts and optional desktop shortcut
- Upgrade/downgrade support with version migration

**macOS DMG Distribution** (New in v1.6.0)
- Universal binary support (x64 + ARM64) via `lipo`
- Drag-to-Applications DMG installer with custom background
- Full code signing infrastructure with entitlements
- Notarization workflow for Gatekeeper compatibility

**Linux Package Support** (New in v1.6.0)
- AppImage for universal Linux distribution
- Debian package (.deb) for Ubuntu/Debian/Mint
- RPM package for Fedora/RHEL/CentOS
- Desktop entry with icon integration

**Auto-Update System** (New in v1.6.0)
- Automatic update checking with version comparison
- Download progress tracking with speed and ETA
- SHA256 checksum verification before installation
- Platform-specific installer execution
- Version rollback support with backup management
- Update channels: Stable, Beta, Dev

### What's in v1.5.x

**Keep and Siege Tracking** (New in v1.5.0)
- Door/structure damage tracking with per-door damage aggregation
- Siege session detection with configurable time gap threshold
- Siege phase detection: Approach, Outer Siege, Inner Siege, Lord Fight, Capture
- Keep capture event tracking with realm and guild attribution
- Guard kill tracking with lord kill detection
- Siege contribution scoring and timeline visualization

**Relic Tracking** (New in v1.5.0)
- Relic pickup, drop, capture, and return event tracking
- Relic raid session resolution from event sequences
- Relic status tracking (Home, InTransit, Captured)
- Relic carrier statistics with delivery success rates
- Contribution scoring for relic raids including carrier bonuses

**Battleground Statistics** (New in v1.5.0)
- Zone-based battleground session detection
- Support for Thidranki, Molvik, Cathal Valley, Killaloe, and Open RvR
- Per-session and aggregated combat statistics
- Best performing and most played battleground tracking
- Total time in battlegrounds and estimated realm points

### What's in v1.4.x

**Group Composition Analysis** (New in v1.4.0)
- Automatic group member detection from healing, buffs, and combat patterns
- Manual member configuration with class/realm override capability
- 7 group roles: Tank, Healer, CrowdControl, MeleeDps, CasterDps, Support, Hybrid
- Dual-role system (primary + secondary) for all 48 character classes
- Standard DAoC group size categories: Solo, Small-Man, 8-Man, Battlegroup

**Role Classification** (New in v1.4.0)
- Complete class-to-role mapping for Albion, Midgard, and Hibernia
- Role coverage analysis with status indicators
- Balance scoring (0-100) based on composition quality

**Group Templates** (New in v1.4.0)
- 6 pre-defined templates: 8-Man RvR, Small-Man, Zerg Support, Gank Group, Keep Defense, Duo
- Template matching with score-based evaluation
- Composition recommendations based on template requirements

**Performance Metrics** (New in v1.4.0)
- Group DPS/HPS totals and averages
- Kill/Death tracking with ratio calculations
- Per-member contribution percentages
- Combat duration tracking

### What's in v1.3.x

**Real-Time Combat Alerts** (New in v1.3.0)
- Alert engine with real-time event processing and rule evaluation
- 6 built-in alert conditions: Health threshold, burst damage, kill streaks, enemy class, ability usage, debuff detection
- 4 notification types: Sound effects, screen flash, text-to-speech, Discord webhooks
- Configurable rules with priority levels (Low → Critical) and cooldown management
- AND/OR condition logic for complex alert triggers
- Trigger history tracking with session-based limits

**Session Comparison & Analytics** (New in v1.3.0)
- Side-by-side session comparison with delta calculations
- Change direction indicators (Improved ↑, Declined ↓, Unchanged →)
- Significance thresholds for meaningful changes
- Category-based metric grouping (Damage, Healing, Combat, General, Custom)

**Trend Analysis** (New in v1.3.0)
- Linear regression for performance trend detection
- Rolling averages for smoothed trend visualization
- Statistical analysis (mean, median, standard deviation, min/max)
- Predictive value extrapolation
- R-squared coefficient for trend confidence

**Goal Tracking** (New in v1.3.0)
- Performance goal creation with target values
- Progress monitoring with percentage completion
- Goal status tracking (NotStarted → InProgress → Achieved/Failed/Expired)
- JSON persistence for goal history

**Personal Best Tracking** (New in v1.3.0)
- Automatic PB detection across all metrics
- Improvement percentage calculations
- PB history with timestamps
- Event notifications for new personal bests

### What's in v1.2.x

**Realm Ability Tracking** (New in v1.2.0)
- Comprehensive database with 100+ realm abilities organized by realm (Albion, Midgard, Hibernia, Universal)
- Era-based ability gating (Classic through Live)
- Activation tracking with cooldown management
- Statistics: total activations, cooldown efficiency, damage/healing totals
- Per-ability usage statistics with effectiveness metrics
- Timeline visualization and type distribution charts

**Buff/Debuff Tracking** (New in v1.2.0)
- Buff state tracker with timer-based expiry estimation
- 40+ buff/debuff definitions across 9 categories
- Uptime calculations per buff with gap detection
- Critical gap identification for expected buffs
- Timeline visualization and distribution charts
- Category distribution and uptime bar charts

### What's in v1.1.x

**Death Analysis** (New in v1.1.0)
- Pre-death damage reconstruction with 15-second analysis window
- Death categorization: Burst (Alpha Strike, Coordinated, CC Chain), Attrition (Healing Deficit, Resource Exhaustion, Positional), Execution (Low Health, DoT), Environmental
- Killing blow identification and attacker class detection
- Per-death recommendations based on category and circumstances
- Damage timeline visualization and death statistics

**Crowd Control Analysis** (New in v1.1.0)
- Full CC tracking with Diminishing Returns (DR) system
- DR levels: Full (100%) → Reduced (50%) → Minimal (25%) → Immune (0%)
- 60-second DR decay timer per target per CC type
- CC chain detection with gap threshold configuration
- Support for mez, root, snare, stun, silence, and disarm effects
- CC timeline visualization with DR level color coding

**Server Profile System** (New in v1.1.0)
- Era-based server profiles: Classic, Shrouded Isles, Trials of Atlantis, New Frontiers, Live, Custom
- Per-profile class availability and feature flags
- Built-in profiles with appropriate era settings
- Custom profile creation with manual configuration

**Chat Filtering** (New in v1.1.0)
- Pre-parse filtering with 16 chat message types
- Filter presets: All Messages, Combat Only, Tactical, Custom
- Per-channel enable/disable with combat context options
- Keyword and sender whitelists
- Privacy mode with player name anonymization

**Settings Window** (New in v1.1.0)
- Unified settings interface with tabbed navigation
- Server Profiles, Chat Filtering, and Privacy Settings tabs

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

### Future Enhancements (15/18 Complete)

| Feature | Status | Target | Location |
|---------|--------|--------|----------|
| [Plugin System](future-enhancements/01-plugin-system.md) | ✅ Complete | v1.0.0 | `src/CamelotCombatReporter.Plugins/`, `src/CamelotCombatReporter.PluginSdk/` |
| [Machine Learning Insights](future-enhancements/02-machine-learning-insights.md) | 📋 Planned | v2.0.0 | — |
| [Cross-Realm Analysis](future-enhancements/03-cross-realm-analysis.md) | ✅ Phase 1 | v1.0.0 | `src/CamelotCombatReporter.Core/CrossRealm/`, `src/CamelotCombatReporter.Gui/CrossRealm/` |
| [Loot Drop Rate Tracking](future-enhancements/04-loot-drop-tracking.md) | ✅ Complete | v1.0.0 | `src/CamelotCombatReporter.Core/LootTracking/`, `src/CamelotCombatReporter.Gui/LootTracking/` |
| [Realm Ability Tracking](future-enhancements/05-realm-ability-tracking.md) | ✅ Complete | v1.2.0 | `src/CamelotCombatReporter.Core/RealmAbilities/`, `src/CamelotCombatReporter.Gui/RealmAbilities/` |
| [Server Type Filters](future-enhancements/06-server-type-filters.md) | ✅ Complete | v1.1.0 | `src/CamelotCombatReporter.Core/ServerProfiles/`, `src/CamelotCombatReporter.Gui/Settings/` |
| [Distribution Builds](future-enhancements/07-distribution-builds.md) | ✅ Complete | v1.6.0 | `installer/`, `.github/workflows/`, `src/CamelotCombatReporter.Core/Updates/` |
| [Chat Filtering](future-enhancements/08-chat-filtering.md) | ✅ Complete | v1.1.0 | `src/CamelotCombatReporter.Core/ChatFiltering/`, `src/CamelotCombatReporter.Gui/Settings/` |
| [Group Composition Analysis](future-enhancements/09-group-composition-analysis.md) | ✅ Complete | v1.4.0 | `src/CamelotCombatReporter.Core/GroupAnalysis/`, `src/CamelotCombatReporter.Gui/GroupAnalysis/` |
| [Keep and Siege Tracking](future-enhancements/10-keep-siege-tracking.md) | ✅ Complete | v1.5.0 | `src/CamelotCombatReporter.Core/RvR/`, `src/CamelotCombatReporter.Gui/RvR/` |
| [Combat Replay System](future-enhancements/11-combat-replay.md) | 📋 Planned | v2.0.0 | — |
| [Voice Chat Integration](future-enhancements/12-voice-integration.md) | 📋 Planned | v2.0.0 | — |
| [In-Game Overlay HUD](future-enhancements/13-overlay-hud.md) | 📋 Planned | v2.0.0 | — |
| [Death Analysis](future-enhancements/14-death-analysis.md) | ✅ Complete | v1.1.0 | `src/CamelotCombatReporter.Core/DeathAnalysis/`, `src/CamelotCombatReporter.Gui/DeathAnalysis/` |
| [Buff/Debuff Tracking](future-enhancements/15-buff-debuff-tracking.md) | ✅ Complete | v1.2.0 | `src/CamelotCombatReporter.Core/BuffTracking/`, `src/CamelotCombatReporter.Gui/BuffTracking/` |
| [Crowd Control Analysis](future-enhancements/16-crowd-control-analysis.md) | ✅ Complete | v1.1.0 | `src/CamelotCombatReporter.Core/CrowdControlAnalysis/`, `src/CamelotCombatReporter.Gui/CrowdControlAnalysis/` |
| [Combat Alerts](future-enhancements/17-combat-alerts.md) | ✅ Complete | v1.3.0 | `src/CamelotCombatReporter.Core/Alerts/`, `src/CamelotCombatReporter.Gui/Alerts/` |
| [Session Comparison](future-enhancements/18-session-comparison.md) | ✅ Complete | v1.3.0 | `src/CamelotCombatReporter.Core/Comparison/`, `src/CamelotCombatReporter.Gui/Comparison/` |

**Legend:**
- ✅ Complete - Feature fully implemented and tested
- ✅ Phase 1 - Initial implementation complete, future phases planned
- ⏸️ Deferred - Implementation postponed
- 📋 Planned - Not yet started

---

## Release Schedule

### v1.1.0 - Combat Intelligence ✅ RELEASED

**Released:** January 2025
**Focus:** Death and crowd control analysis (builds on v1.0.2 events)

| Item | Type | Status | Description |
|------|------|--------|-------------|
| [Death Analysis](future-enhancements/14-death-analysis.md) | Feature | ✅ Complete | Pre-death analysis, killing blow breakdown |
| [Crowd Control Analysis](future-enhancements/16-crowd-control-analysis.md) | Feature | ✅ Complete | CC chains, diminishing returns tracking |
| [Server Type Filters](future-enhancements/06-server-type-filters.md) | Feature | ✅ Complete | Classic, SI, ToA, Live server profiles |
| [Chat Filtering](future-enhancements/08-chat-filtering.md) | Feature | ✅ Complete | Pre-parse filtering for non-combat messages |
| Death Report UI | Feature | ✅ Complete | Visual death timeline and recommendations |
| Settings Window | Feature | ✅ Complete | Unified settings with tabs for all configuration |

**Dependencies:** Uses DeathEvent, CrowdControlEvent, ResistEvent from v1.0.2

---

### v1.2.0 - Realm Abilities & Buffs ✅ RELEASED

**Released:** January 2026
**Focus:** Advanced combat tracking

| Item | Type | Status | Description |
|------|------|--------|-------------|
| [Realm Ability Tracking](future-enhancements/05-realm-ability-tracking.md) | Feature | ✅ Complete | RA usage statistics and cooldown tracking |
| [Buff/Debuff Tracking](future-enhancements/15-buff-debuff-tracking.md) | Feature | ✅ Complete | Buff uptime, debuff effectiveness |
| RA Database | Feature | ✅ Complete | 100+ RA data for all three realms + universal |
| Buff Timeline Widget | Feature | ✅ Complete | Visual buff events with uptime charts |
| Realm Abilities Tab | Feature | ✅ Complete | New tab in MainWindow |
| Buff Tracking Tab | Feature | ✅ Complete | New tab in MainWindow |

---

### v1.3.0 - Alerts & Comparison ✅ RELEASED

**Released:** January 2026
**Focus:** Proactive feedback and historical analysis

| Item | Type | Status | Description |
|------|------|--------|-------------|
| [Combat Alerts](future-enhancements/17-combat-alerts.md) | Feature | ✅ Complete | Real-time alert engine with 6 conditions, 4 notification types |
| [Session Comparison](future-enhancements/18-session-comparison.md) | Feature | ✅ Complete | Side-by-side session analysis with delta calculations |
| Trend Analysis | Feature | ✅ Complete | Linear regression, rolling averages, statistical analysis |
| Audio Notifications | Feature | ✅ Complete | Sound effects and TTS alerts via service interfaces |
| Goal Tracking | Feature | ✅ Complete | Personal goals with progress monitoring and persistence |
| Personal Best Tracking | Feature | ✅ Complete | Automatic PB detection with improvement tracking |
| Alerts Tab | Feature | ✅ Complete | New tab in MainWindow for alert configuration |
| Session Comparison Tab | Feature | ✅ Complete | New tab in MainWindow with trend charts |

---

### v1.4.0 - Group Analysis ✅ RELEASED

**Released:** January 2026
**Focus:** Group and team performance

| Item | Type | Status | Description |
|------|------|--------|-------------|
| [Group Composition Analysis](future-enhancements/09-group-composition-analysis.md) | Feature | ✅ Complete | Automatic member detection + manual config |
| Role Classification | Feature | ✅ Complete | 7 roles with dual-role system for 48 classes |
| Group Templates | Feature | ✅ Complete | 6 templates (8-Man RvR, Small-Man, etc.) |
| Balance Scoring | Feature | ✅ Complete | 0-100 score based on composition quality |
| Role Coverage Analysis | Feature | ✅ Complete | Per-role status and member tracking |
| Composition Recommendations | Feature | ✅ Complete | Priority-based suggestions for improvement |
| Team Performance Metrics | Feature | ✅ Complete | DPS/HPS totals, K/D, contributions |
| Group Analysis Tab | Feature | ✅ Complete | Full analysis dashboard in MainWindow |

---

### v1.5.0 - RvR Features ✅ RELEASED

**Released:** January 2026
**Focus:** Realm vs Realm combat tracking

| Item | Type | Status | Description |
|------|------|--------|-------------|
| [Keep and Siege Tracking](future-enhancements/10-keep-siege-tracking.md) | Feature | ✅ Complete | Door/structure damage, siege phases, contribution scoring |
| Keep Capture History | Feature | ✅ Complete | Track keep take participation with timeline |
| Relic Tracking | Feature | ✅ Complete | Relic raid sessions, carrier tracking, contribution metrics |
| Battleground Statistics | Feature | ✅ Complete | BG-specific performance tracking by type |
| Siege Tracking Tab | Feature | ✅ Complete | New tab in MainWindow with siege sessions and outcomes |
| Relic Tracking Tab | Feature | ✅ Complete | New tab in MainWindow with relic status grid |
| Battlegrounds Tab | Feature | ✅ Complete | New tab in MainWindow with BG statistics |

---

### v1.6.0 - Distribution & Auto-Update ✅ RELEASED

**Released:** January 2026
**Focus:** Improved installers and auto-update functionality

| Item | Type | Status | Description |
|------|------|--------|-------------|
| [Distribution Builds](future-enhancements/07-distribution-builds.md) | Feature | ✅ Complete | Windows MSI, macOS DMG, Linux packages |
| Windows MSI Installer | Feature | ✅ Complete | WiX Toolset v4-based installer with file associations |
| macOS DMG Distribution | Feature | ✅ Complete | Universal binary, code signing, notarization workflow |
| Linux Packages | Feature | ✅ Complete | AppImage, .deb, .rpm package support |
| Auto-Update System | Feature | ✅ Complete | Update checking, download, verification, installation |
| Update Dialog UI | Feature | ✅ Complete | Progress tracking, rollback support, version comparison |
| Release Feed | Feature | ✅ Complete | JSON-based update feed at `releases/latest.json` |

---

### v1.7.0 - Polish & Enhancement ✅ RELEASED

**Released:** January 2026
**Focus:** GUI polish, performance optimization, stability

| Item | Type | Status | Description |
|------|------|--------|-------------|
| Centralized Style Resources | Enhancement | ✅ Complete | Colors, Brushes, Styles, Icons in Resources folder |
| Theme Service | Enhancement | ✅ Complete | IThemeService with System/Light/Dark modes and persistence |
| Loading Indicators | Enhancement | ✅ Complete | LoadingSpinner, LoadingOverlay, ProgressCard controls |
| Keyboard Shortcuts | Enhancement | ✅ Complete | F1 help, Ctrl+T theme toggle, shortcuts window |
| Status Bar | Enhancement | ✅ Complete | Event count, theme status, version display |
| Async Parser | Performance | ✅ Complete | ParseAsync with IProgress support, cancellation |
| Statistics Caching | Performance | ✅ Complete | SHA256 hash validation, LRU eviction |
| DataGrid Virtualization | Performance | ✅ Complete | VirtualizingStackPanel for large lists |
| Memory Optimizations | Performance | ✅ Complete | StringPool, ObjectPool for reduced allocations |
| Performance Benchmarks | Performance | ✅ Complete | BenchmarkDotNet suite for parser, cache, pools |

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

### v1.7.0 (January 2026)

**Polish & Enhancement Release:**
- Centralized style resources in `src/CamelotCombatReporter.Gui/Resources/`
  - Colors.axaml with semantic color definitions
  - Brushes.axaml for light/dark theme brushes
  - Styles.axaml with reusable control styles
  - Icons.axaml with PathGeometry definitions
- ThemeService with System/Light/Dark mode support
  - JSON persistence at `AppData/CamelotCombatReporter/theme-settings.json`
  - ThemeChanged event for reactive UI updates
- Loading indicator controls
  - LoadingSpinner: Animated circular spinner with configurable size
  - LoadingOverlay: Full overlay with message and progress bar
  - ProgressCard: Card control with title, description, and progress
- Keyboard shortcuts system
  - F1 for keyboard shortcuts help window
  - Ctrl+T for theme toggle
  - KeyboardShortcutsWindow with organized shortcut display
- Status bar in MainWindow with event count, theme status, version
- Async log parser with ParseAsync() method
  - IProgress<ParseProgress> for progress reporting
  - CancellationToken support for cancellation
  - Task.Yield() for UI responsiveness every 1000 lines
- Statistics caching layer
  - IStatisticsCacheService interface with GetCachedAsync/CacheAsync
  - SHA256 file hash validation for cache integrity
  - LRU eviction when cache exceeds max entries (default 10)
- DataGrid virtualization with VirtualizingStackPanel
  - CombatSessions and TargetStatistics lists converted to ListBox
- Memory optimization utilities
  - StringPool for string interning with LRU eviction
  - ObjectPool<T> generic object pooling
  - StringBuilderPool and ListPool<T> specialized implementations
- BenchmarkDotNet performance benchmarks
  - LogParserBenchmarks: sync vs async parsing
  - StringPoolBenchmarks: unique vs duplicate string interning
  - CachingBenchmarks: parse vs cached retrieval
- 36 new unit tests for caching and optimization services
- 9 new ThemeService tests

### v1.6.0 (January 2026)

**Distribution & Auto-Update Release:**
- Windows MSI Installer with WiX Toolset v4
- File associations for `.combat` and `.log` files
- Start menu shortcuts and optional desktop shortcut
- macOS DMG with universal binary (x64 + ARM64) support
- Full code signing infrastructure and notarization workflow
- Linux packages: AppImage, .deb (Debian/Ubuntu), .rpm (Fedora/RHEL)
- Desktop entry integration with icons for all sizes
- Auto-Update System with update checking and version comparison
- Download progress tracking with speed and ETA
- SHA256 checksum verification before installation
- Platform-specific installer execution
- Version rollback support with backup management
- Update channels: Stable, Beta, Dev
- JSON release feed at `releases/latest.json`
- Update Dialog UI with progress, rollback, and version comparison
- GitHub Actions workflow for multi-platform installer builds
- 26 new unit tests for update services

### v1.5.0 (January 2026)

**RvR Features Release:**
- Keep and Siege Tracking with door damage, phase detection, and contribution scoring
- Siege session resolution with configurable time gap threshold
- Siege phases: Approach, Outer Siege, Inner Siege, Lord Fight, Capture
- Keep capture event tracking with realm and guild attribution
- Guard kill and lord kill detection
- Relic raid tracking with pickup, drop, capture, and return events
- Relic carrier statistics with delivery success rates
- Relic status tracking (Home, InTransit, Captured) for all 6 realm relics
- Battleground session detection for Thidranki, Molvik, Cathal Valley, Killaloe, Open RvR
- Per-session and aggregated battleground statistics
- Best performing and most played battleground tracking
- New Siege Tracking, Relic Tracking, and Battlegrounds tabs in main window
- 41 new unit tests for RvR services

### v1.4.0 (January 2026)

**Group Analysis Release:**
- Group Composition Analysis with automatic member detection from combat patterns
- Multi-strategy detection: healing received, buff sources, shared combat targets
- Manual member configuration with class/realm override capability
- 7 group roles: Tank, Healer, CrowdControl, MeleeDps, CasterDps, Support, Hybrid
- Dual-role system (primary + secondary) for all 48 character classes
- Standard DAoC group sizes: Solo, Small-Man, 8-Man, Battlegroup
- 6 pre-defined templates: 8-Man RvR, Small-Man, Zerg Support, Gank Group, Keep Defense, Duo
- Composition balance scoring (0-100) with role coverage analysis
- Priority-based recommendations for composition improvement
- Team performance metrics (DPS/HPS, K/D, contributions)
- New Group Analysis tab in main window with full dashboard

### v1.3.0 (January 2026)

**Alerts & Comparison Release:**
- Real-Time Combat Alerts with 6 built-in conditions (Health, Damage, KillStreak, EnemyClass, AbilityUsed, Debuff)
- 4 notification types: Sound, Screen Flash, Text-to-Speech, Discord Webhooks
- Alert engine with real-time event processing and rule evaluation
- Configurable rules with priority levels and cooldown management
- Session Comparison with delta calculations and change indicators
- Trend Analysis using linear regression and rolling averages
- Goal Tracking with progress monitoring and JSON persistence
- Personal Best Tracking with automatic detection and improvement tracking
- New Alerts and Session Comparison tabs in main window
- Trend visualization with LiveCharts2

### v1.2.0 (January 2026)

**Realm Abilities & Buffs Release:**
- Realm Ability Tracking with 100+ abilities organized by realm
- Era-based ability gating (Classic through Live)
- Activation tracking with cooldown management and effectiveness analysis
- Buff/Debuff Tracking with timer-based expiry estimation
- 40+ buff/debuff definitions across 9 categories
- Uptime calculations with gap detection for expected buffs
- New Realm Abilities and Buff Tracking tabs in main window
- Timeline and distribution visualizations for both features

### v1.1.0 (January 2025)

**Combat Intelligence Release:**
- Death Analysis with 15-second pre-death reconstruction
- Death categorization (9 types: Burst, Attrition, Execution)
- Crowd Control Analysis with Diminishing Returns tracking
- DR system: Full → Reduced → Minimal → Immune with 60s decay
- Server Profile system for DAoC eras (Classic through Live)
- Chat Filtering with presets and privacy mode
- Unified Settings window with tabbed configuration
- New Death Analysis and CC Analysis tabs in main window

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
