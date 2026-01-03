# 3. Cross-Realm Analysis

## Status: ✅ Phase 1 Complete (Local-First Implementation)

**What's Implemented:**
- ✅ Game enums for all realms and classes (Albion, Midgard, Hibernia)
- ✅ Character configuration with manual realm/class selection
- ✅ Extended combat statistics with character context
- ✅ Local JSON storage for session persistence
- ✅ Cross-realm statistics aggregation service
- ✅ Local leaderboards (DPS, HPS, KDR, etc.)
- ✅ Export to JSON/CSV for community sharing
- ✅ New "Cross-Realm Analysis" tab in GUI
- ✅ Character configuration dialog

**Future Phases (Not Yet Implemented):**
- Central server for community-wide statistics
- Log-based auto-detection of class
- Public leaderboards
- Opt-in anonymous data sharing

---

## Description

This feature enables the aggregation of combat statistics by realm (Albion, Midgard, Hibernia) and character class, providing insights into personal performance across different play styles.

## Current Implementation

### Core Models
- **GameEnums.cs** - Realm, CharacterClass, and ClassArchetype enums with helper extensions
- **CharacterInfo.cs** - Character configuration record (name, realm, class, level, realm rank)
- **ExtendedCombatStatistics.cs** - Enhanced statistics with character context, K/D ratio, HPS

### Services
- **CrossRealmStatisticsService** - Local JSON storage and statistics aggregation
  - Saves sessions to `%APPDATA%/CamelotCombatReporter/cross-realm/sessions/`
  - Maintains an index file for fast queries
  - Calculates realm and class statistics (avg/median/max DPS, HPS, KDR)
  - Generates local leaderboards by metric

- **CrossRealmExporter** - Export functionality
  - JSON export with full session data or aggregates only
  - CSV export for spreadsheet analysis
  - Configurable filters (realm, class, date range)
  - Privacy-aware (optional character name inclusion)

### GUI Components
- **CrossRealmView** - New tab showing:
  - Character configuration card
  - Current session info with save button
  - Realm statistics table
  - Class statistics table (when realm is selected)
  - Top DPS and HPS leaderboards
  - Recent sessions list
  - Export buttons (JSON/CSV)

- **CharacterConfigDialog** - Modal dialog for:
  - Realm selection (Albion, Midgard, Hibernia)
  - Class selection (filtered by realm, 15-16 classes per realm)
  - Level (1-50)
  - Realm Rank (0-14)
  - Character name (optional)

## Usage

1. Open the "Cross-Realm Analysis" tab
2. Click "Configure" to set your character's realm and class
3. Analyze combat logs on the "Combat Analysis" tab
4. Return to "Cross-Realm Analysis" to save sessions
5. View aggregated statistics and personal leaderboards
6. Export data for community sharing

## Functionality

### Local Features (Implemented)
- **Character Configuration:** Manual setup for realm, class, level, and realm rank
- **Session Tracking:** Save analyzed combat sessions with character context
- **Personal Statistics:**
  - Realm-level aggregates (avg DPS, HPS, K/D across all your characters)
  - Class-level aggregates (per-class performance)
  - Local leaderboards (your personal bests by metric)
- **Export:** Share data in JSON/CSV format for community analysis

### Planned Future Features
- **Centralized Data Repository:** Central server for community-wide statistics
- **Public Leaderboards:** Server-based leaderboards across all users
- **Auto-Detection:** Automatic class detection from combat log patterns
- **Community Insights:** Aggregated meta-analysis across the player base

## Requirements

- .NET 9.0 runtime
- Local file system access for session storage

## Limitations

- Character configuration is manual (no auto-detection from logs yet)
- Statistics are local-only (no central server yet)
- K/D ratio requires kills/deaths to be manually tracked (not parsed from logs)

## Dependencies

- **Core Models:** CamelotCombatReporter.Core project
- **GUI Framework:** Avalonia UI with CommunityToolkit.MVVM

## File Structure

```
src/CamelotCombatReporter.Core/
├── Models/
│   ├── GameEnums.cs                    # Realm, CharacterClass, ClassArchetype
│   ├── CharacterInfo.cs                # Character configuration
│   └── ExtendedCombatStatistics.cs     # Stats with character context
└── CrossRealm/
    ├── CrossRealmTypes.cs              # Supporting types (RealmStatistics, etc.)
    ├── ICrossRealmStatisticsService.cs # Service interface
    ├── CrossRealmStatisticsService.cs  # Local JSON implementation
    └── CrossRealmExporter.cs           # JSON/CSV export

src/CamelotCombatReporter.Gui/CrossRealm/
├── Views/
│   ├── CrossRealmView.axaml(.cs)       # Main cross-realm tab
│   └── CharacterConfigDialog.axaml(.cs)# Character setup dialog
└── ViewModels/
    ├── CrossRealmViewModel.cs          # Tab logic and data
    └── CharacterConfigViewModel.cs     # Dialog logic
```
