# Enemy Encounter Database Plugin

## Plugin Type: Data Analysis + UI Component

## Status: ✅ Implemented (v1.9.1)

## Overview

Build a personal database of enemy encounters, tracking statistics for each unique enemy you've fought. Useful for understanding mob patterns in PvE or tracking specific player nemeses in RvR.

## Problem Statement

Players want to remember and analyze:
- Which enemies they've fought before
- How much damage specific enemies deal
- Which enemies are easiest/hardest to kill
- Historical win/loss records against player enemies
- Patterns in enemy behavior

## Features

### Enemy Tracking
- Automatically catalog every unique enemy encountered
- Distinguish between mob types and player enemies
- Track encounter count per enemy
- Record first and last seen dates

### Per-Enemy Statistics
- Total damage dealt to enemy
- Total damage taken from enemy
- Average time to kill
- Win/loss ratio (for players)
- Most effective abilities against them

### Enemy Browser
- Searchable list of all enemies
- Sort by various metrics
- Filter by type (mob/player), realm, class
- Favorite/bookmark specific enemies
- Add personal notes

### Encounter History
- Log of all encounters with selected enemy
- Timeline view of engagements
- Performance trend over time

## Technical Specification

### Plugin Manifest

```json
{
  "id": "enemy-encounter-database",
  "name": "Enemy Encounter Database",
  "version": "1.0.0",
  "author": "CCR Community",
  "description": "Tracks and catalogs all enemy encounters with detailed statistics",
  "type": "DataAnalysis",
  "entryPoint": {
    "assembly": "EnemyEncounterDb.dll",
    "typeName": "EnemyEncounterDb.EncounterPlugin"
  },
  "compatibility": {
    "minAppVersion": "1.0.0"
  },
  "permissions": [
    "CombatDataAccess",
    "FileRead",
    "FileWrite",
    "UIModification"
  ],
  "resources": {
    "maxMemoryMb": 128,
    "maxCpuTimeSeconds": 30
  }
}
```

### Data Structures

```csharp
public record EnemyRecord(
    string Id,                      // Unique identifier (hash of name)
    string Name,                    // Enemy name
    EnemyType Type,                 // Mob, Player, NPC
    Realm? Realm,                   // For players
    CharacterClass? Class,          // If detectable
    int EncounterCount,             // How many times fought
    DateTime FirstSeen,             // First encounter date
    DateTime LastSeen,              // Most recent encounter
    EnemyStatistics Statistics,     // Aggregated stats
    IReadOnlyList<EncounterSummary> RecentEncounters,
    string? Notes,                  // User notes
    bool IsFavorite                 // Bookmarked
);

public record EnemyStatistics(
    int TotalDamageDealt,           // Damage you dealt to them
    int TotalDamageTaken,           // Damage they dealt to you
    int TotalKills,                 // Times you killed them
    int TotalDeaths,                // Times they killed you
    double AverageEncounterDuration,
    double AverageDps,
    Dictionary<string, int> DamageByAbility,
    Dictionary<string, int> DamageTakenByAbility
);

public record EncounterSummary(
    DateTime Timestamp,
    TimeSpan Duration,
    int DamageDealt,
    int DamageTaken,
    EncounterOutcome Outcome,
    string? Location
);

public enum EnemyType
{
    Mob,
    Player,
    NPC,
    Unknown
}

public enum EncounterOutcome
{
    Victory,    // You killed them
    Defeat,     // They killed you
    Escaped,    // Combat ended without death
    Unknown
}
```

### Database Storage

```csharp
public interface IEnemyDatabase
{
    Task<EnemyRecord?> GetEnemyAsync(string id);
    Task<IReadOnlyList<EnemyRecord>> SearchAsync(EnemySearchCriteria criteria);
    Task SaveEnemyAsync(EnemyRecord record);
    Task<EnemyRecord> GetOrCreateAsync(string name, EnemyType type);
    Task AddEncounterAsync(string enemyId, EncounterSummary encounter);
}

public class JsonEnemyDatabase : IEnemyDatabase
{
    private readonly string _databasePath;
    private Dictionary<string, EnemyRecord> _enemies = new();

    public JsonEnemyDatabase(string pluginDataDirectory)
    {
        _databasePath = Path.Combine(pluginDataDirectory, "enemies.json");
        LoadDatabase();
    }

    private void LoadDatabase()
    {
        if (File.Exists(_databasePath))
        {
            var json = File.ReadAllText(_databasePath);
            _enemies = JsonSerializer.Deserialize<Dictionary<string, EnemyRecord>>(json)
                ?? new();
        }
    }

    public async Task SaveAsync()
    {
        var json = JsonSerializer.Serialize(_enemies, new JsonSerializerOptions
        {
            WriteIndented = true
        });
        await File.WriteAllTextAsync(_databasePath, json);
    }
}
```

### Implementation Outline

```csharp
public class EncounterPlugin : DataAnalysisPluginBase
{
    private IEnemyDatabase _database = null!;

    public override async Task InitializeAsync(
        IPluginContext context,
        CancellationToken ct = default)
    {
        _database = new JsonEnemyDatabase(context.PluginDataDirectory);
        await base.InitializeAsync(context, ct);
    }

    public override async Task<AnalysisResult> AnalyzeAsync(
        IReadOnlyList<LogEvent> events,
        CombatStatistics? baseStatistics,
        AnalysisOptions options,
        CancellationToken ct = default)
    {
        var encounters = DetectEncounters(events, options.CombatantName);

        foreach (var encounter in encounters)
        {
            await ProcessEncounter(encounter);
        }

        var stats = BuildStatistics(encounters);
        return Success(stats);
    }

    private List<EnemyEncounter> DetectEncounters(
        IReadOnlyList<LogEvent> events,
        string combatantName)
    {
        var encounters = new List<EnemyEncounter>();

        // Group events by enemy
        var byEnemy = events
            .OfType<DamageEvent>()
            .Where(e => e.Source == combatantName || e.Target == combatantName)
            .GroupBy(e => e.Source == combatantName ? e.Target : e.Source);

        foreach (var group in byEnemy)
        {
            var enemyName = group.Key;
            var enemyEvents = group.ToList();

            var damageDealt = enemyEvents
                .Where(e => e.Source == combatantName)
                .Sum(e => e.DamageAmount);

            var damageTaken = enemyEvents
                .Where(e => e.Target == combatantName)
                .Sum(e => e.DamageAmount);

            var firstEvent = enemyEvents.First();
            var lastEvent = enemyEvents.Last();
            var duration = CalculateDuration(
                firstEvent.Timestamp,
                lastEvent.Timestamp
            );

            encounters.Add(new EnemyEncounter(
                enemyName,
                ClassifyEnemy(enemyName, enemyEvents),
                damageDealt,
                damageTaken,
                duration,
                DetermineOutcome(events, combatantName, enemyName)
            ));
        }

        return encounters;
    }

    private EnemyType ClassifyEnemy(string name, List<DamageEvent> events)
    {
        // Heuristics for classification:
        // - Mobs usually have generic names
        // - Players have varied damage types
        // - NPCs have specific patterns

        // Check for player-like names (capitalized, no spaces)
        if (Regex.IsMatch(name, @"^[A-Z][a-z]+$"))
            return EnemyType.Player;

        // Check for mob patterns (lowercase or generic)
        if (name.Contains(" ") || char.IsLower(name[0]))
            return EnemyType.Mob;

        return EnemyType.Unknown;
    }

    private async Task ProcessEncounter(EnemyEncounter encounter)
    {
        var record = await _database.GetOrCreateAsync(
            encounter.EnemyName,
            encounter.Type
        );

        var summary = new EncounterSummary(
            DateTime.Now,
            encounter.Duration,
            encounter.DamageDealt,
            encounter.DamageTaken,
            encounter.Outcome,
            null // Location if detectable
        );

        await _database.AddEncounterAsync(record.Id, summary);
    }
}
```

### UI Component

```csharp
public class EnemyBrowserView : UserControl
{
    private readonly EncounterPlugin _plugin;
    private ObservableCollection<EnemyRecord> _enemies = new();
    private EnemyRecord? _selectedEnemy;

    // Search and filter
    public string SearchText { get; set; } = "";
    public EnemyType? TypeFilter { get; set; }
    public string SortBy { get; set; } = "LastSeen";

    private async Task LoadEnemies()
    {
        var criteria = new EnemySearchCriteria
        {
            NameContains = SearchText,
            Type = TypeFilter,
            SortBy = SortBy
        };

        var results = await _plugin.SearchEnemiesAsync(criteria);
        _enemies.Clear();
        foreach (var enemy in results)
        {
            _enemies.Add(enemy);
        }
    }
}
```

## UI Layout

```
┌─────────────────────────────────────────────────────────────┐
│  Enemy Encounter Database                      [🔍] [⚙️]   │
├───────────────────────────┬─────────────────────────────────┤
│  Search: [              ] │  Selected: Dark Elf Assassin    │
│                           │                                 │
│  Filter: [All Types ▼]    │  Type: Player                   │
│  Sort:   [Last Seen ▼]    │  Realm: Hibernia                │
│                           │  Class: Nightshade (suspected)  │
│  ─────────────────────    │                                 │
│  ⭐ Dark Elf Assassin     │  ───────────────────────────    │
│     47 encounters         │  STATISTICS                     │
│     Last: 2h ago          │  Encounters: 47                 │
│                           │  Your Kills: 23                 │
│  ○ Goblin Scout           │  Your Deaths: 18                │
│     312 encounters        │  Win Rate: 56.1%                │
│     Last: 3h ago          │                                 │
│                           │  Damage Dealt: 145,320          │
│  ○ Svartalf Warrior       │  Damage Taken: 132,890          │
│     89 encounters         │  Avg Duration: 45s              │
│     Last: 1d ago          │                                 │
│                           │  ───────────────────────────    │
│  ○ Midgard Skald          │  TOP ABILITIES AGAINST          │
│     12 encounters         │  • Perforate Artery (34%)       │
│     Last: 3d ago          │  • Backstab II (22%)            │
│                           │  • Hamstring (18%)              │
│  [Load More...]           │                                 │
│                           │  ───────────────────────────    │
│                           │  NOTES                          │
│                           │  [Usually runs when low HP.    ]│
│                           │  [Watch for stealth opener.    ]│
│                           │                                 │
│                           │  [Save Notes]                   │
└───────────────────────────┴─────────────────────────────────┘
```

## Dependencies

- Core combat parsing
- Local file storage for database
- UI framework for browser component

## Complexity

**Medium** - Database management and UI are straightforward, but enemy classification heuristics need refinement.

## Future Enhancements

- [ ] Enemy ability database (common attacks)
- [ ] Threat level rating based on history
- [ ] Export enemy list as JSON
- [ ] Sync across characters
- [ ] Community enemy database contributions
- [ ] Integration with PvP Matchup Analyzer
