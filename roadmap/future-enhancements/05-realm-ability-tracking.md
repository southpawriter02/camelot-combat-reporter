# 5. Realm Ability Tracking and Analysis

## Status: ✅ Complete (v1.2.0)

**Implementation Complete:**
- ✅ Log parsing infrastructure
- ✅ Cross-realm analysis (Phase 1)
- ✅ Realm ability event parsing patterns

---

## Description

Track and analyze Realm Ability (RA) usage, effectiveness, and cooldown management. This feature provides insights into RA utilization patterns, helps optimize RA specs, and tracks RA point expenditure over time.

## Functionality

### Core Features

* **RA Event Parsing:**
  * Parse RA activation messages from combat logs
  * Detect RA effects on targets (buffs, debuffs, damage)
  * Track RA cooldown timers
  * Capture realm point gains for RA progression

* **RA Database:**
  * Complete database of all realm abilities by realm
  * RA costs (realm points) and prerequisites
  * Base cooldowns and effects
  * Passive vs. active ability classification

* **Usage Statistics:**
  * RA usage frequency per session
  * Effectiveness metrics (damage dealt, healing done, CC duration)
  * Cooldown efficiency (actual vs. optimal usage)
  * RA contribution to overall performance

### Realm Ability Categories

| Category | Examples | Tracking Focus |
|----------|----------|----------------|
| **Damage** | Volcanic Pillar, Thornweed Field, Ichor of the Deep | DPS contribution, targets hit |
| **Crowd Control** | Purge, Static Tempest, Dazzling Array | Duration, targets affected, breaks |
| **Defensive** | Soldier's Barricade, Bunker of Faith, Barrier of Fortitude | Damage mitigated, uptime |
| **Healing** | Raging Power, Gift of Perizor | HPS contribution, overhealing |
| **Utility** | Mystic Crystal Lore, Speed of Sound | Usage frequency, tactical value |
| **Passives** | Mastery of Arms, Wild Power, Augmented Acuity | Stat contributions |

### Analysis Views

* **RA Usage Timeline:**
  * Visualize RA activations during combat
  * Show cooldown periods
  * Highlight missed opportunities (RA ready but not used)

* **RA Effectiveness Report:**
  * Per-RA breakdown of damage/healing contribution
  * Compare actual vs. expected effectiveness
  * Identify underutilized abilities

* **RA Spec Analysis:**
  * Input current RA spec for personalized analysis
  * Suggest RA improvements based on usage patterns
  * Calculate realm point investment efficiency

* **Cross-Session Trends:**
  * Track RA usage patterns over time
  * Identify playstyle changes
  * Monitor RA point accumulation rate

### Realm Point Tracking

* **RP Gain Analysis:**
  * Track realm points earned per session
  * Calculate RP/hour rates
  * Project time to next RA purchase
  * Breakdown by kill type (solo, group, keep)

* **RA Progression:**
  * Current total realm points spent
  * Realm rank progression
  * Next RA purchase recommendations

## Requirements

* **Log Parsing:** RA-specific regex patterns
* **RA Database:** Complete RA data for all three realms
* **UI:** New "Realm Abilities" section or tab

## Limitations

* Some RA effects may not appear in combat logs
* Passive RA contributions are indirect and harder to quantify
* RA data may need updates with game patches
* Private server RA variations may differ

## Dependencies

* **01-log-parsing.md:** Core parsing infrastructure
* **03-cross-realm-analysis.md:** Character and realm context
* **06-server-type-filters.md:** RA availability varies by server type

## Implementation Phases

### Phase 1: RA Event Parsing
- [ ] Identify RA activation patterns in DAoC logs
- [ ] Create RealmAbilityEvent model class
- [ ] Build RA database (JSON/embedded resource)
- [ ] Add RA parsing to LogParser

### Phase 2: Usage Statistics
- [ ] Create RealmAbilityStatisticsService
- [ ] Implement usage frequency calculations
- [ ] Add cooldown tracking logic
- [ ] Calculate effectiveness metrics

### Phase 3: GUI Integration
- [ ] Design RA tracking UI
- [ ] Implement RA timeline visualization
- [ ] Create RA spec input dialog
- [ ] Add RP progression display

### Phase 4: Advanced Analysis
- [ ] RA spec optimization suggestions
- [ ] Cross-session trend analysis
- [ ] RA comparison across classes/realms

## Technical Notes

* RA database should be version-controlled and updatable
* Consider RA cooldown sharing (some RAs share cooldowns)
* Store RA events with timestamps for timeline reconstruction
* Link RA events to combat events for effectiveness calculation

## Realm Ability Data Structure

```csharp
public record RealmAbility(
    string Name,
    Realm Realm,                    // Albion, Midgard, Hibernia, or All
    RealmAbilityType Type,          // Damage, CC, Defensive, Healing, Utility, Passive
    int[] RealmPointCosts,          // Cost per level [1, 3, 6, 10, 14]
    TimeSpan? Cooldown,             // null for passives
    string[] Prerequisites,         // Required RAs
    string Description
);

public enum RealmAbilityType
{
    Damage,
    CrowdControl,
    Defensive,
    Healing,
    Utility,
    Passive
}
```
