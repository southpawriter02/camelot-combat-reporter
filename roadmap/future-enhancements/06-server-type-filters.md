# 6. Server Type Filters

## Status: ✅ Complete (v1.1.0)

**Implementation Complete:**
- ✅ Log parsing infrastructure
- ✅ GUI filtering framework
- ✅ Server-specific configuration system

---

## Description

Apply filters and configurations based on DAoC server type (Classic, Shrouded Isles, Trials of Atlantis, Live). Different server rulesets affect available classes, abilities, items, and game mechanics, requiring context-aware parsing and analysis.

## Functionality

### Server Types

| Server Type | Era | Key Characteristics |
|-------------|-----|---------------------|
| **Classic** | Launch - SI | Original 7 classes per realm, no ToA abilities, classic RvR |
| **Shrouded Isles** | SI Expansion | +2 classes per realm (e.g., Necromancer, Savage, Animist) |
| **Trials of Atlantis** | ToA Expansion | Master Levels, Artifacts, Champion Levels |
| **New Frontiers** | NF Expansion | Updated RvR, new frontiers zones, Mauler class |
| **Live** | Current | All expansions, latest patches, all features |
| **Custom** | Private servers | User-defined ruleset combinations |

### Core Features

* **Server Profile Selection:**
  * Choose server type in settings
  * Auto-detect from log patterns when possible
  * Custom profile creation for private servers
  * Per-session server type override

* **Context-Aware Parsing:**
  * Filter class lists by server era
  * Adjust ability recognition by era
  * Handle era-specific log message formats
  * Validate data against server capabilities

* **Era-Specific Data:**
  * Class availability by server type
  * Realm ability pools by era
  * Item/artifact availability
  * Stat caps and mechanics differences

### Filtering Options

* **Class Filters:**
  * Hide classes not available on selected server
  * Example: No Necromancer/Savage on Classic servers

* **Ability Filters:**
  * Filter realm abilities by era availability
  * Hide Master Levels on pre-ToA servers
  * Adjust spell/style lists by expansion

* **Statistics Adjustments:**
  * Apply era-appropriate stat caps
  * Adjust calculations for mechanics changes
  * Handle pre/post-nerf ability values

### Server Profiles

```
Classic Profile:
- Classes: 7 per realm (no expansion classes)
- Realm Abilities: Limited RA pool
- No Master Levels or Artifacts
- Original damage formulas

Shrouded Isles Profile:
- Classes: 9 per realm
- Additional zones and content
- Expanded RA pool
- No ToA content

Trials of Atlantis Profile:
- Classes: 9 per realm
- Master Levels (1-10)
- Artifacts and artifact abilities
- Champion Levels
- Full RA pool (pre-Mauler)

Live Profile:
- All classes including Mauler
- All expansions enabled
- Current patch mechanics
- Full feature set
```

### Private Server Support

* **Custom Profile Builder:**
  * Toggle individual features on/off
  * Select class availability per realm
  * Enable/disable specific expansions
  * Set custom stat caps

* **Profile Import/Export:**
  * Share server profiles in JSON format
  * Import community profiles
  * Version control for profile updates

## Requirements

* **Settings UI:** Server type selection interface
* **Profile System:** Configurable server profiles
* **Data Files:** Era-specific game data

## Limitations

* Private servers may have unique modifications
* Log formats may vary between servers
* Some mechanics changes are undocumented
* Community data may be required for accuracy

## Dependencies

* **01-log-parsing.md:** Core parsing with context awareness
* **03-cross-realm-analysis.md:** Class/realm data integration
* **05-realm-ability-tracking.md:** RA availability by era

## Implementation Phases

### Phase 1: Server Profile System
- [ ] Create ServerType enum and profile model
- [ ] Build default profiles for each server type
- [ ] Add server selection to settings
- [ ] Store selected profile in preferences

### Phase 2: Class Filtering
- [ ] Create class availability matrix by server
- [ ] Filter character config dialog by server type
- [ ] Update cross-realm analysis for server context
- [ ] Add visual indicators for era restrictions

### Phase 3: Ability Filtering
- [ ] Create ability availability matrix by server
- [ ] Filter RA tracking by server type
- [ ] Handle Master Level abilities for ToA+
- [ ] Adjust spell/style parsing

### Phase 4: Custom Profiles
- [ ] Design custom profile builder UI
- [ ] Implement profile import/export
- [ ] Create community profile repository structure
- [ ] Add profile validation

## Technical Notes

* Use feature flags pattern for optional capabilities
* Store era data as embedded resources
* Support runtime profile switching
* Consider profile migration for updates

## Data Structures

```csharp
public enum ServerType
{
    Classic,
    ShroudedIsles,
    TrialsOfAtlantis,
    NewFrontiers,
    Live,
    Custom
}

public record ServerProfile(
    string Name,
    ServerType BaseType,
    IReadOnlySet<CharacterClass> AvailableClasses,
    IReadOnlySet<string> AvailableRealmAbilities,
    bool HasMasterLevels,
    bool HasArtifacts,
    bool HasChampionLevels,
    Dictionary<string, object> CustomSettings
);

public interface IServerContextProvider
{
    ServerProfile CurrentProfile { get; }
    bool IsClassAvailable(CharacterClass characterClass);
    bool IsAbilityAvailable(string abilityName);
    T GetMechanicValue<T>(string mechanicKey, T defaultValue);
}
```
