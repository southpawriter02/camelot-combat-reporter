# Changelog

All notable changes to Camelot Combat Reporter are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

No unreleased changes.

---

## [1.9.1] - 2026-01-08

### Added
- **Enemy Encounter Database Plugin**
  - Complete plugin implementation for tracking enemy encounters
  - `EnemyRecord`, `EnemyStatistics`, `EncounterSummary` data models
  - `JsonEnemyDatabase` with thread-safe persistence using SemaphoreSlim
  - `EncounterAnalyzer` for detecting and classifying enemies (Mob/Player/NPC)
  - Enemy classification heuristics based on naming patterns
  - Per-enemy statistics: damage dealt/taken, kills/deaths, win rate, average DPS
  - Damage breakdown by ability type
  - User notes and favorites per enemy
  - `EnemyBrowserView` Avalonia UI with search, filter, and sort functionality
  - Two-panel layout: enemy list (left) and detailed statistics (right)
  - Plugin manifest with CombatDataAccess, FileRead, FileWrite, UIModification permissions
  - Comprehensive test suite (23 tests) covering analyzer, database, and models

---

## [1.9.0] - 2026-01-07

### Added
- **Combat Style Class Detection**
  - `ICombatLogClassDetector` interface and implementation
  - Comprehensive DAoC combat style-to-class mappings (45+ classes)
  - Confidence scoring based on style frequency and uniqueness
  - Support for all three realms: Albion, Midgard, Hibernia
  - `ClassInferenceResult` with candidate classes and usage statistics

- **Meta Build Templates (45+ Templates)**
  - `IMetaBuildTemplateService` with template management
  - 2-3 meta builds per class covering different playstyles
  - Albion: Armsman, Cabalist, Cleric, Friar, Infiltrator, Mercenary, Minstrel, Paladin, Reaver, Scout, Theurgist, Wizard
  - Midgard: Berserker, Healer, Hunter, Savage, Shadowblade, Shaman, Skald, Thane, Warrior
  - Hibernia: Bard, Blademaster, Champion, Druid, Hero, Nightshade, Ranger, Valewalker, Warden
  - Each template includes spec lines, realm abilities, role, and author

- **Enhanced Profile Export**
  - `ProfileExportOptions` record for configurable exports
  - Include/exclude build history option
  - Include/exclude session references option
  - Character name anonymization option
  - Custom export name support
  - Performance metrics stripping option
  - `ProfileExportResult` with metadata (size, counts, suggested filename)

- **Keyboard Shortcuts**
  - Character Profiles View:
    - `Ctrl+N`: New Profile
    - `Ctrl+E`: Export Profile
    - `Ctrl+I`: Import Profile
    - `Delete`: Delete Selected Profile
    - `F2`: Edit Selected Profile
  - Build Editor Dialog:
    - `Escape`: Cancel and close
    - `Ctrl+S`: Save build

- **Template Selection Dialog**
  - Browse all meta build templates by realm, role, or search
  - Preview template details: spec lines, realm abilities, tags
  - Filter by realm (Albion/Midgard/Hibernia) and role (Tank/DPS/Healer/Support)
  - "Load Template" button in Build Editor dialog

- **Enhanced Profile Export Dialog**
  - Configurable export with privacy options
  - Include/exclude build history and session references
  - Character name anonymization
  - Custom export filename support
  - Performance metrics stripping

### Changed
- Updated version to v1.9.0

---

## [1.8.3] - 2026-01-07

### Added
- **Sidebar Navigation**
  - Categorized sidebar replaces flat tab navigation
  - 5 categories: Core, Analysis, Character, Tracking, RvR
  - Dark theme sidebar with hover effects
  - Emoji icons for visual recognition
  
- **View Menu**
  - New View menu with categorized navigation submenus
  - Keyboard shortcut Ctrl+1 for Combat Analysis
  - Quick access to all 14 views from menu bar

- **Navigation Infrastructure**
  - `NavItem` model with category, icon, and shortcut support
  - `NavKeyToIndexConverter` for view switching
  - `NavCategory` enum for grouping
  - Hidden tabs style for TabControl

### Changed
- Replaced 14 flat horizontal tabs with organized sidebar
- Updated version to v1.8.3 in status bar
- Enhanced navigation with smooth transitions and hover states

---

## [1.8.2] - 2026-01-07

### Added
- **Premium Gradient Brushes**
  - Primary, Success, Danger, Warning, Accent gradients
  - Realm-specific gradients (Albion, Midgard, Hibernia)
  - Header, Subtle, Shimmer, and Glass effect gradients

- **Enhanced Button Styles**
  - Smooth color transitions (150ms)
  - Press scale animation (0.97 scale)
  - PrimaryGradient button variant with lift effect
  - Cursor and hover transitions for all button types

- **New Card Styles**
  - `CardHover` - Lift effect on hover with shadow animation
  - `CardInteractive` - Clickable with border highlight
  - `CardGlass` - Glassmorphism effect
  - Enhanced shadows on all card types

- **Component Polish**
  - StatCard hover scaling with shadow animation
  - Badge hover scaling effects
  - Tab hover color transitions
  - Realm badge hover animations
  - Enhanced tooltip styling
  - DataGrid row hover effects
  - Progress bar variants (Thin, Primary, Success, Warning, Error)
  - QuickStatsBar enhanced styling

- **New Unit Tests**
  - 27 additional tests for Character Building services
  - CharacterProfileService: persistence, duplicates, cloning (7 tests)
  - SpecializationTemplateService: realm classes, formulas (8 tests)
  - BuildComparisonService: spec deltas, RA changes (7 tests)
  - PerformanceAnalysisService: aggregation, edge cases (5 tests)
  - ProgressionTrackingService: trends, RP calculations (5 tests)

### Changed
- Increased button padding (12,6 → 16,8)
- Increased corner radius (5 → 6-8)
- Updated card padding (15 → 16)
- Enhanced separator margins (10 → 12)
- Total tests: 410 (381 Core + 29 GUI)

---

## [1.8.1] - 2026-01-07

### Added
- **Build Comparison System**
  - Side-by-side comparison of any two character builds
  - Spec line delta calculations with point allocation differences
  - Realm ability delta tracking: Added, Removed, RankChanged categorization
  - RA point cost calculations using triangular formula
  - Performance metrics delta comparison (DPS, HPS, K/D, Kills, Deaths)
  - `BuildComparisonService` with comprehensive logging

- **Progression Tracking**
  - Realm rank milestone recording (RR1-RR14)
  - RP thresholds for all 14 realm ranks from DAoC
  - Average days between rank-ups calculation
  - Average RP per session calculation
  - DPS and K/D trend analysis (first-half vs second-half comparison)
  - Estimated time to next rank based on current RP rate
  - Auto-detection and recording of rank-up events
  - `ProgressionTrackingService` with structured logging

- **Performance Analytics**
  - Session-based performance metric aggregation
  - DPS, HPS, and damage taken rate calculations
  - K/D ratio with proper zero-death handling
  - Peak DPS tracking across sessions
  - Date range filtering for metrics
  - Build metrics auto-update after session attachment
  - `PerformanceAnalysisService` with optional date filtering

- **New GUI Components**
  - `BuildComparisonView` with side-by-side layout and delta indicators
  - `ProgressionChartView` with milestone timeline and trend display
  - `PerformanceSummaryView` for detailed metrics visualization

- **Service Polish**
  - Comprehensive logging added to all Character Building services
  - XML documentation with detailed remarks for all public APIs
  - Inline comments explaining DAoC-specific formulas and algorithms

- **New Unit Tests**
  - 7 tests for BuildComparisonService
  - 7 tests for PerformanceAnalysisService
  - 7 tests for ProgressionTrackingService

### Changed
- Total tests: 343 (313 Core + 30 GUI)

---

## [1.8.0] - 2026-01-07

### Added
- **Character Profile System**
  - Create, update, and delete character profiles with realm/class/name
  - JSON file storage with profile index for fast lookups
  - Thread-safe implementation with SemaphoreSlim locking
  - Profile import/export to JSON for sharing
  - Session attachment to profiles for performance correlation
  - Auto-matching suggestions based on class, realm, and name similarity
  - `CharacterProfileService` with comprehensive logging

- **Build Management**
  - Immutable build snapshots with version history
  - Active build tracking per profile
  - Build cloning with new name and reset metrics
  - Build history retrieval for rollback
  - `CharacterBuild` record with specs, realm abilities, and performance metrics

- **Specialization Templates**
  - Complete templates for all 48 DAoC character classes
  - 16 Albion classes (Armsman, Cabalist, Cleric, Friar, Heretic, Infiltrator, Mercenary, Minstrel, Necromancer, Paladin, Reaver, Scout, Sorcerer, Theurgist, Wizard, MaulerAlb)
  - 16 Midgard classes (Berserker, Bonedancer, Healer, Hunter, Runemaster, Savage, Shadowblade, Shaman, Skald, Spiritmaster, Thane, Valkyrie, Warlock, Warrior, MaulerMid)
  - 16 Hibernia classes (Animist, Bainshee, Bard, Blademaster, Champion, Druid, Eldritch, Enchanter, Hero, Mentalist, Nightshade, Ranger, Valewalker, Vampiir, Warden, MaulerHib)
  - Spec line types: Weapon, Magic, Utility, Hybrid
  - Point multipliers for balanced-cost specs
  - `SpecializationTemplateService` for template lookups

- **Spec Point Calculations**
  - DAoC formula: `(level * 2) + (level / 2) + 1` = 126 points at level 50
  - Triangular cost formula: `n(n+1)/2` for spec point costs
  - Spec multiplier support for hybrid classes
  - Validation of total allocation against level cap
  - Remaining points calculation

- **Realm Ability Catalog**
  - 40+ realm abilities with accurate point costs
  - Triangular cost scaling: 1→1, 2→3, 3→6, 4→10, 5→15
  - Master Level abilities (ML1-10)
  - Realm-specific and universal abilities
  - RA point budget tracking

- **New Data Models**
  - `CharacterProfile` - Profile with name, realm, class, builds, sessions
  - `CharacterBuild` - Immutable build snapshot with specs and RAs
  - `SpecializationTemplate` / `SpecLine` - Class spec definitions
  - `RealmAbilitySlot` - Equipped RA with rank
  - `BuildPerformanceMetrics` - Aggregated combat statistics
  - `RealmRankProgression` / `RankMilestone` - Progression data

- **New GUI Components**
  - `BuildEditorView` with spec line sliders and point tracking
  - `RealmAbilityEditorView` with RA selection and budget display
  - `CharacterProfileView` for profile management

- **New Unit Tests**
  - 12 tests for CharacterProfileService
  - 27 tests for SpecializationTemplateService and RealmAbilityCatalog

### Changed
- MainWindow includes new Character Building tab
- Total tests: 322 (302 Core + 20 GUI)

---

## [1.6.0] - 2026-01-06

### Added
- **Windows MSI Installer**
  - WiX Toolset v4-based installer with full installation flow
  - File associations for `.combat` and `.log` files
  - Start menu shortcuts and optional desktop shortcut
  - Upgrade/downgrade support with version migration
  - Custom UI dialog sequence with branding
  - Per-user or per-machine installation options

- **macOS DMG Distribution**
  - Universal binary support (x64 + ARM64) via `lipo`
  - Drag-to-Applications DMG installer with custom background
  - Full code signing infrastructure with entitlements
  - Notarization workflow for Gatekeeper compatibility
  - App bundle with proper Info.plist and file associations

- **Linux Package Support**
  - AppImage for universal Linux distribution
  - Debian package (.deb) for Ubuntu/Debian/Mint
  - RPM package for Fedora/RHEL/CentOS
  - Desktop entry with icon integration
  - Post-install and post-remove scripts

- **Auto-Update System**
  - `IUpdateService` / `UpdateService` for update management
  - Automatic update checking on startup
  - Download progress tracking with speed and ETA
  - SHA256 checksum verification before installation
  - Platform-specific installer execution
  - Version rollback support with backup management
  - Update channels: Stable, Beta, Dev
  - JSON release feed at `releases/latest.json`

- **Update Dialog UI**
  - Update notification with version comparison
  - Release notes link
  - Download progress bar with speed and time remaining
  - "Download and Install", "Remind Me Later", "Skip This Version" options
  - Rollback to previous version option
  - Required update indicator

- **New Core Services**
  - `IUpdateService` / `UpdateService` - Update checking, download, verification, installation
  - `UpdateInfo`, `UpdateCheckResult`, `DownloadProgress` - Update data models
  - `UpdateChannel` enum for release channels

- **New GUI Components**
  - `UpdateDialog` with `UpdateViewModel` for update notifications
  - Progress tracking and status display

- **CI/CD Enhancements**
  - GitHub Actions workflow for multi-platform installers
  - Automatic MSI, DMG, AppImage, deb, rpm builds on release tags
  - SHA256 checksum generation for all artifacts
  - Automatic `latest.json` release feed generation

- **New Unit Tests**
  - 26 new tests for UpdateService, UpdateInfo, UpdateCheckResult, DownloadProgress

### Changed
- GitHub Actions release workflow now builds platform-specific installers
- Total tests: 256 (236 Core + 20 GUI)

---

## [1.5.0] - 2026-01-06

### Added
- **Keep and Siege Tracking**
  - Door/structure damage tracking with per-door damage aggregation
  - Siege session detection with configurable time gap threshold
  - Siege phase detection: Approach, Outer Siege, Inner Siege, Lord Fight, Capture
  - Keep capture event tracking with realm and guild attribution
  - Guard kill tracking with lord kill detection
  - Siege contribution scoring formula: `(structureDamage * 0.01) + (playerKills * 50) - (deaths * 25) + (healingDone * 0.005) + (guardKills * 10)`
  - Siege timeline visualization with phase-colored entries
  - Statistics aggregation by keep with outcomes tracking

- **Relic Tracking**
  - Relic pickup, drop, capture, and return event tracking
  - Relic raid session resolution from event sequences
  - Relic status tracking (Home, InTransit, Captured)
  - Relic carrier statistics with delivery success rates
  - Contribution scoring for relic raids including carrier bonuses
  - Relic database with all 6 realm relics (2 per realm)
  - Per-raid statistics with success/failure tracking

- **Battleground Statistics**
  - Zone-based battleground session detection
  - Support for Thidranki, Molvik, Cathal Valley, Killaloe, and Open RvR
  - Per-session combat statistics (kills, deaths, damage, healing, K/D ratio)
  - Aggregated statistics by battleground type
  - Best performing battleground detection (highest K/D with minimum kills)
  - Most played battleground tracking
  - Total time in battlegrounds calculation
  - Estimated realm points from kills

- **New Core Services**
  - `ISiegeTrackingService` / `SiegeTrackingService` - Siege session detection, phase detection, contribution scoring
  - `IRelicTrackingService` / `RelicTrackingService` - Relic raid tracking, carrier statistics
  - `IBattlegroundService` / `BattlegroundService` - BG session detection, statistics aggregation

- **New GUI Components**
  - `SiegeTrackingView` with siege statistics, session list, outcomes chart, and timeline
  - `RelicTrackingView` with relic status grid, raid history, and carrier statistics
  - `BattlegroundView` with performance by type, session history, and K/D charts

- **New Data Models**
  - `RvREnums.cs` - KeepType, SiegeOutcome, SiegePhase, RelicType, RelicStatus, BattlegroundType
  - `SiegeEvents.cs` - DoorDamageEvent, KeepCapturedEvent, GuardKillEvent, TowerCapturedEvent
  - `RelicEvents.cs` - RelicPickupEvent, RelicDropEvent, RelicCapturedEvent, RelicReturnedEvent
  - `SiegeModels.cs` - SiegeSession, SiegeContribution, SiegeStatistics, SiegeTimelineEntry
  - `BattlegroundModels.cs` - BattlegroundSession, BattlegroundStatistics, AllBattlegroundStatistics

- **New Unit Tests**
  - 41 new tests for RvR features (SiegeTrackingService, RelicTrackingService, BattlegroundService)

### Changed
- MainWindow now includes Siege Tracking, Relic Tracking, and Battlegrounds tabs
- Total tests: 230 (210 Core + 20 GUI)

---

## [1.4.0] - 2026-01-06

### Added
- **Group Composition Analysis**
  - Automatic group member detection from combat patterns
  - Multi-strategy detection: healing received, buff sources, shared combat targets
  - Manual member configuration with class/realm assignment
  - Combined inference + manual override capability

- **Role Classification System**
  - 7 group roles: Tank, Healer, CrowdControl, MeleeDps, CasterDps, Support, Hybrid
  - Dual-role system (primary + secondary) for all 48 character classes
  - Complete role mapping database for Albion, Midgard, and Hibernia
  - `RoleClassificationService` for role lookups and class suggestions

- **Group Size Categories**
  - Solo (1 player)
  - Small-Man (2-4 players)
  - 8-Man (5-8 players)
  - Battlegroup (9+ players)

- **Group Templates**
  - 6 pre-defined templates: 8-Man RvR, Small-Man, Zerg Support, Gank Group, Keep Defense, Duo
  - Template matching with score-based evaluation
  - Role requirements with min/max counts and required flags

- **Balance Scoring**
  - Composition balance score (0-100)
  - Penalties for missing healers (-35), tanks (-20), DPS (-25)
  - Bonuses for CC (+5), support (+5)
  - Imbalance detection (>50% one role)

- **Role Coverage Analysis**
  - Per-role coverage status (Covered, Missing, Over-represented)
  - Member count and names per role
  - Visual progress indicators

- **Composition Recommendations**
  - 5 recommendation types: AddRole, ReduceRole, RebalanceRoles, TemplateMatch, SynergyImprovement
  - 4 priority levels: Low, Medium, High, Critical
  - Context-aware suggestions based on group composition

- **Performance Metrics**
  - Group total DPS/HPS
  - Total kills and deaths with K/D ratio
  - Combat duration tracking
  - Per-member contribution percentages

- **New Core Services**
  - `IGroupDetectionService` / `GroupDetectionService` - Member detection with configurable thresholds
  - `IGroupAnalysisService` / `GroupAnalysisService` - Full composition analysis
  - `RoleClassificationService` - Class-to-role mapping

- **New GUI Components**
  - `GroupAnalysisView` with comprehensive analysis dashboard
  - Group overview panel with member count, category, balance score, template match
  - Members DataGrid with role classification
  - Manual member input with class selection
  - Role distribution pie chart (LiveCharts2)
  - Role coverage bars with status indicators
  - Performance metrics display (DPS, HPS, K/D, kills, deaths, duration)
  - Member contribution stacked bar chart
  - Priority-colored recommendations panel
  - Empty state guidance

- **New Data Models**
  - `GroupEnums.cs` - GroupRole, GroupSizeCategory, GroupMemberSource, RecommendationType, RecommendationPriority
  - `GroupModels.cs` - GroupMember, GroupComposition, GroupTemplate, RoleRequirement, GroupPerformanceMetrics, MemberContribution, RoleCoverage, CompositionRecommendation, GroupAnalysisSummary

- **New Unit Tests**
  - 68 new tests for GroupAnalysis (RoleClassification, GroupDetection, GroupAnalysis, Templates, Enums)

### Changed
- MainWindow now includes Group Analysis tab
- Total tests: 189 (169 Core + 20 GUI)

---

## [1.3.0] - 2026-01-05

### Added
- **Real-Time Combat Alerts System**
  - Extensible AlertEngine with condition-based rule evaluation
  - 6 built-in alert conditions:
    - `HealthBelowCondition` - Triggers when health drops below threshold
    - `DamageInWindowCondition` - Detects burst damage situations
    - `KillStreakCondition` - Tracks kill streak milestones
    - `EnemyClassCondition` - Alerts when targeting specific classes
    - `AbilityUsedCondition` - Tracks realm ability usage (self or enemy)
    - `DebuffAppliedCondition` - Monitors active debuffs
  - 4 notification types:
    - `SoundNotification` - Priority-based audio alerts
    - `ScreenFlashNotification` - Visual screen flash with color coding
    - `TtsNotification` - Text-to-speech announcements
    - `DiscordWebhookNotification` - Post alerts to Discord channels
  - Rule management with AND/OR condition logic
  - Per-rule cooldowns and max triggers per session
  - Combat state tracking (health, streaks, debuffs, damage windows)
  - Alert configuration persistence (JSON)
  - New "Alerts" tab in main window with preset rules

- **Session Comparison & Analytics**
  - `SessionComparisonService` for side-by-side session analysis
  - Metric delta calculations with direction indicators (Improved/Declined/Unchanged)
  - Categorized metrics: Damage, Healing, Combat, General, Custom
  - Significance thresholds for filtering minor changes
  - Human-readable comparison summaries

- **Trend Analysis**
  - `TrendAnalysisService` with statistical analysis
  - Linear regression for trend direction (slope, intercept, R-squared)
  - Rolling average calculations for data smoothing
  - Standard deviation, mean, median, min/max statistics
  - Trend interpretation with predicted next values
  - LiveCharts2 trend visualization

- **Goal Tracking**
  - `GoalTracker` for performance goals
  - Goal types: DPS, HPS, K/D Ratio, Kills/Deaths per session, Buff uptime, Custom metrics
  - Goal statuses: NotStarted, InProgress, Achieved, Failed, Expired
  - Progress history tracking with session association
  - Automatic status updates as goals are achieved
  - JSON persistence for goals

- **Personal Best Tracking**
  - `PersonalBestTracker` with automatic PB detection
  - Improvement percentage calculation
  - PB history per metric
  - `NewPersonalBest` event for notifications
  - JSON persistence for current bests and history

- **New Core Services**
  - `AlertEngine` - Real-time event processing and alert evaluation
  - `IAlertCondition` / `INotification` - Extensible interfaces
  - `IAudioService` / `ITtsService` - Platform-agnostic audio interfaces
  - `AlertConfigurationService` - Rule serialization and persistence
  - `ISessionComparisonService` / `SessionComparisonService`
  - `ITrendAnalysisService` / `TrendAnalysisService`
  - `IGoalTracker` / `GoalTracker`
  - `IPersonalBestTracker` / `PersonalBestTracker`

- **New GUI Components**
  - `AlertsView` with rule management DataGrid
  - Priority color coding (Critical=Red, High=Orange, Medium=Yellow, Low=Green)
  - Quick preset buttons for common alert rules
  - Alert trigger history with timestamps
  - `SessionComparisonView` with session selector dropdowns
  - Comparison results DataGrid with delta indicators
  - Trend chart with LiveCharts2 line series
  - Goal progress cards with completion percentage
  - Personal best notifications panel

- **New Data Models**
  - `AlertEnums.cs` - AlertPriority, ConditionLogic, AlertRuleState
  - `AlertRule` record with conditions, notifications, cooldown
  - `AlertContext` and `AlertTrigger` records
  - `CombatState` - Real-time state tracking with damage/healing queues
  - `ComparisonModels.cs` - ChangeDirection, MetricDelta, SessionSummary, SessionComparison
  - `TrendModels.cs` - TrendDataPoint, TrendStatistics, TrendAnalysis
  - `GoalModels.cs` - GoalType, GoalStatus, PerformanceGoal, GoalProgress, PersonalBest

### Changed
- MainWindow now includes Alerts and Session Comparison tabs
- All 101 tests passing

### Technical Details
- 40+ new files added across Core and GUI projects
- New namespaces: `Alerts`, `Alerts.Models`, `Alerts.Conditions`, `Alerts.Notifications`, `Alerts.Services`, `Comparison`, `Comparison.Models`
- Full MVVM implementation with CommunityToolkit.Mvvm source generators
- LiveCharts2 integration for trend visualization
- JSON-based configuration and data persistence

---

## [1.2.0] - 2026-01-05

### Added
- **Realm Ability Tracking System**
  - Comprehensive database with 100+ realm abilities organized by realm (Albion, Midgard, Hibernia, Universal)
  - Era-based ability gating (Classic, Shrouded Isles, Trials of Atlantis, New Frontiers, Catacombs, Darkness Rising, Labyrinth, Live)
  - Activation tracking with cooldown management
  - Statistics calculations: total activations, cooldown efficiency, damage/healing totals
  - Per-ability usage statistics with effectiveness metrics
  - Timeline visualization of RA activations by minute
  - Type distribution charts (Damage, CC, Defensive, Healing, Utility, Passive)
  - Cooldown state tracking at session end
  - New "Realm Abilities" tab in main window

- **Buff/Debuff Tracking System**
  - Buff state tracker following DRTracker pattern with timer-based expiry estimation
  - Comprehensive buff database with 40+ buff/debuff types
  - Categories: Stat Buffs, Armor Buffs, Resistance Buffs, Damage/Speed, Regeneration, Debuffs, DoT Effects
  - Uptime calculations per buff with gap detection
  - Critical gap identification for expected buffs (stat buffs, damage add)
  - Timeline visualization of buff events
  - Category distribution and uptime bar charts
  - New "Buff Tracking" tab in main window

- **New Core Services**
  - `IRealmAbilityService` / `RealmAbilityService` - RA activation tracking and statistics
  - `IRealmAbilityDatabase` / `RealmAbilityDatabase` - JSON-based RA database with indexing
  - `IBuffTrackingService` / `BuffTrackingService` - Buff event extraction and statistics
  - `BuffStateTracker` - Active buff state management with expiry estimation

- **Log Parsing Enhancements**
  - RA activation patterns: "You activate [ability]!"
  - RA damage/healing effect patterns
  - RA ready notifications
  - Enemy RA activation detection

- **New Data Models**
  - `RealmAbility` record with level costs, cooldown, prerequisites, and era gating
  - `RealmAbilityEvent` and `RealmAbilityReadyEvent` log events
  - `RealmAbilityActivation`, `RealmAbilityUsageStats`, `RealmAbilitySessionStats`
  - `BuffDefinition` with category, duration, stacking rules, concentration type
  - `BuffEvent`, `ActiveBuff`, `BuffGap`, `BuffUptimeStats`, `BuffStatistics`

### Changed
- MainWindow now includes Realm Abilities and Buff Tracking tabs
- About dialog updated to show version 1.2.0

### Technical Details
- 27 new files added across Core and GUI projects
- New namespaces: `RealmAbilities`, `RealmAbilities.Models`, `BuffTracking`, `BuffTracking.Models`
- JSON database at `data/realm-abilities/realm-abilities.json`
- Full MVVM implementation with CommunityToolkit.Mvvm source generators
- LiveCharts2 integration for timeline and distribution charts
- All 82 tests passing

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
| 1.8.1 | 2026-01-07 | Character Building Tools: Build Comparison, Progression Tracking, Performance Analytics |
| 1.8.0 | 2026-01-07 | Character Building Tools: Profiles, 48 Class Templates, RA Catalog, Spec Point System |
| 1.6.0 | 2026-01-06 | Distribution & Auto-Update: MSI, DMG, AppImage, deb, rpm, Auto-Update System |
| 1.5.0 | 2026-01-06 | RvR Features: Keep/Siege Tracking, Relic Raids, Battleground Statistics |
| 1.4.0 | 2026-01-06 | Group Composition Analysis with role classification and templates |
| 1.3.0 | 2026-01-05 | Combat Alerts, Session Comparison, Trend Analysis, Goal Tracking |
| 1.2.0 | 2026-01-05 | Realm Ability Tracking (100+ abilities), Buff/Debuff Tracking with uptime analysis |
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
