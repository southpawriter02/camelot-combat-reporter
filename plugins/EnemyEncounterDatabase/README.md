# Enemy Encounter Database Plugin

A plugin for [Camelot Combat Reporter](https://github.com/your-repo/camelot-combat-reporter) that tracks and catalogs all enemy encounters with detailed statistics.

## Features

- **Automatic Enemy Detection** - Automatically detects and catalogs enemies from combat logs
- **Mob vs Player Classification** - Uses naming heuristics to classify enemies
- **Per-Enemy Statistics** - Tracks damage dealt/taken, kills/deaths, win rate, DPS
- **Ability Breakdown** - Damage breakdown by ability/damage type
- **Personal Notes** - Add notes to any enemy for personal reference
- **Favorites** - Bookmark important enemies for quick access
- **Searchable Browser** - Filter by name, type, realm; sort by various metrics

## Installation

1. Build the plugin:
   ```bash
   cd plugins/EnemyEncounterDatabase
   dotnet build -c Release
   ```

2. Copy output to plugins directory:
   ```bash
   cp -r bin/Release/net9.0/* ../installed/EnemyEncounterDatabase/
   ```

3. Restart Camelot Combat Reporter

## Usage

After installation, an **Enemy Database** tab appears in the main application.

### Browsing Enemies
- Use the search box to filter by name
- Filter by type (Players, Mobs, NPCs)
- Sort by various metrics (encounters, damage, win rate)
- Toggle "Favorites Only" to show bookmarked enemies

### Enemy Details
Select an enemy to view:
- **Statistics**: Encounters, kills, deaths, win rate, total damage
- **Top Abilities**: Your most effective abilities against this enemy
- **Notes**: Personal notes (click Save to persist)
- **Recent Encounters**: Last 50 encounters with timestamps

## Technical Details

### Permissions Required
- `CombatDataAccess` - Read combat log events
- `FileRead` / `FileWrite` - Persist enemy database
- `UIModification` - Display enemy browser tab

### Data Storage
Enemy data is stored in:
```
{PluginDataDirectory}/enemies.json
```

### Enemy Classification
The plugin uses naming heuristics:
- **Players**: Single capitalized word (e.g., "Nemesis")
- **Mobs**: Multi-word, articles, lowercase (e.g., "a skeletal warrior")

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
├── Models/           # Data models (EnemyRecord, etc.)
├── Services/         # Database interface and implementation
├── Analysis/         # Combat log analysis logic
├── ViewModels/       # MVVM view models
├── Views/            # Avalonia XAML views
└── plugin.json       # Plugin manifest
```

## License

MIT License - See main project for details.
