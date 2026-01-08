# Enemy Encounter Database Plugin

A plugin for [Camelot Combat Reporter](https://github.com/your-repo/camelot-combat-reporter) that tracks and catalogs all enemy encounters with detailed statistics.

## Features

### Core Functionality
- **Automatic Enemy Detection** - Identifies enemies from combat logs
- **Smart Classification** - Distinguishes Mobs vs Players vs NPCs using naming heuristics
- **Per-Enemy Statistics** - Tracks damage, kills, deaths, win rate, DPS
- **Ability Breakdown** - Shows damage by ability/damage type
- **Personal Notes** - Add notes to any enemy
- **Favorites** - Bookmark important enemies

### v1.1.0 GUI Enhancements
- **Type Color Coding** - Visual badges: Red (Players), Green (Mobs), Blue (NPCs)
- **Win Rate Progress Bars** - Color-coded from red (0%) to green (100%)
- **Statistics Dashboard** - Summary panel showing totals across filtered enemies
- **Outcome Indicators** - Victory/Defeat/Escaped with colored text
- **Export** - Export filtered enemies to JSON

## Installation

```bash
cd plugins/EnemyEncounterDatabase
dotnet build -c Release
cp -r bin/Release/net9.0/* ../installed/EnemyEncounterDatabase/
```

Restart Camelot Combat Reporter after installation.

## Usage

### Enemy Browser Tab
- **Search**: Filter by name (real-time)
- **Type Filter**: All/Players/Mobs/NPCs dropdown
- **Sort By**: Last Seen, Encounters, Damage, Win Rate, Kills
- **Favorites Only**: Toggle to show bookmarked enemies
- **Export**: Export current filter to JSON

### Dashboard
Shows aggregate stats for current filter:
- Total unique enemies
- Total encounters
- Overall win rate

### Enemy Details
Select an enemy to view:
- **Win Rate Card**: Visual progress bar with kill/death counts
- **Combat Statistics**: Damage dealt/taken, average DPS, duration
- **Top Abilities**: Your most effective abilities with damage bars
- **Notes**: Editable personal notes
- **Recent Encounters**: Timestamped list with outcomes

## Technical Details

### Permissions
- `CombatDataAccess` - Read combat log events
- `FileRead/FileWrite` - Persist enemy database
- `UIModification` - Display enemy browser tab

### Data Storage
```
{PluginDataDirectory}/enemies.json
```

### Classification Heuristics
| Type | Pattern |
|------|---------|
| Player | Single capitalized word (e.g., "Nemesis") |
| Mob | Multi-word, articles, lowercase (e.g., "a goblin") |
| NPC | Explicitly marked NPCs |

## Development

### Building
```bash
dotnet build
```

### Testing
```bash
cd ../EnemyEncounterDatabase.Tests
dotnet test
```

### Project Structure
```
EnemyEncounterDatabase/
├── Models/           # Data models
├── Services/         # Database interface/implementation
├── Analysis/         # Combat log analysis
├── ViewModels/       # MVVM view models
├── Views/            # Avalonia XAML + Converters
└── plugin.json       # Plugin manifest
```

### Key Files
- `Views/Converters.cs` - Type/WinRate/Outcome value converters
- `ViewModels/EnemyBrowserViewModel.cs` - Main view model with dashboard stats
- `Views/EnemyBrowserView.axaml` - Enhanced UI with badges and progress bars

## Version History

### v1.1.0
- Added type color-coded badges
- Added win rate progress bars
- Added statistics dashboard
- Added outcome coloring
- Added export functionality
- Performance timing in logs

### v1.0.0
- Initial release
- Enemy detection and classification
- Per-enemy statistics
- Notes and favorites
- Searchable browser

## License

MIT License - See main project for details.
