# 9. Group Composition Analysis

## Status: 📋 Planned

**Prerequisites:**
- ✅ Log parsing infrastructure
- ✅ Cross-realm analysis (character/class data)
- ⬚ Group member detection from logs

---

## Description

Analyze group compositions to understand how party makeup affects combat performance. Track which class combinations work well together, identify optimal group templates, and provide insights for building effective RvR groups.

## Functionality

### Core Features

* **Group Detection:**
  * Identify group members from combat logs
  * Track group formation and changes over time
  * Detect group size (solo, small-man, 8-man, battlegroup)
  * Associate combat events with group context

* **Composition Tracking:**
  * Record class composition per encounter
  * Track role distribution (tank, healer, CC, DPS, support)
  * Identify missing roles or imbalances
  * Compare against "meta" compositions

* **Performance Correlation:**
  * Correlate group composition with success metrics
  * Analyze DPS/HPS by group makeup
  * Track K/D ratios for different compositions
  * Identify synergies between specific classes

### Group Templates

| Template | Composition | Use Case |
|----------|-------------|----------|
| **8-Man RvR** | 2 tanks, 2 healers, 2 CC, 2 DPS | Organized group RvR |
| **Small-Man** | 1 tank, 1 healer, 2-3 DPS | Roaming 4-5 person group |
| **Zerg Support** | Heavy healer/support focus | Battlegroup warfare |
| **Gank Group** | High burst DPS, minimal heals | Quick assassinations |
| **Keep Defense** | CC-heavy, sustainable healing | Defensive siege |

### Analysis Views

* **Composition Overview:**
  * Visual representation of current group
  * Role icons and class distribution
  * Health/balance indicators
  * Missing role warnings

* **Historical Analysis:**
  * Performance trends by composition type
  * Best performing group setups
  * Time spent in different compositions
  * Win/loss records per template

* **Synergy Matrix:**
  * Class pairing effectiveness scores
  * Buff/ability overlap detection
  * Recommended partner classes
  * Anti-synergy warnings

* **Role Coverage:**
  * Healing capacity assessment
  * Crowd control coverage
  * Damage output potential
  * Utility/support availability

### Group Roles

```
Tank:       Armsman, Warrior, Hero, Paladin, Champion, Thane, Valkyrie
Healer:     Cleric, Healer, Druid, Friar, Shaman, Warden, Bard
CC:         Sorcerer, Runemaster, Mentalist, Bard, Healer, Shaman
DPS Melee:  Mercenary, Savage, Blademaster, Berserker, Infiltrator, Shadowblade, Nightshade
DPS Caster: Wizard, Eldritch, Spiritmaster, Cabalist, Necromancer, Animist, Bonedancer
Support:    Minstrel, Skald, Bard, Enchanter, Theurgist, Warlock
Hybrid:     Reaver, Valewalker, Vampiir, Heretic, Valkyrie
```

### Recommendations Engine

* **Composition Suggestions:**
  * Recommend classes to fill gaps
  * Suggest role swaps for better balance
  * Identify over-represented roles
  * Propose template-based improvements

* **Recruitment Assistance:**
  * "Looking for" class suggestions
  * Priority ranking for missing roles
  * Flexibility analysis (who can respec)

## Requirements

* **Group Detection:** Parse group-related log messages
* **Class Database:** Complete role assignments per class
* **UI:** Group composition visualization panel

## Limitations

* Group membership not always visible in logs
* Spec/role may differ from class default
* Battlegroup compositions harder to track
* Private server class balance may vary

## Dependencies

* **01-log-parsing.md:** Core parsing infrastructure
* **03-cross-realm-analysis.md:** Character and class data
* **06-server-type-filters.md:** Class availability by era

## Implementation Phases

### Phase 1: Group Detection
- [ ] Identify group-related log patterns
- [ ] Create GroupMember and GroupComposition models
- [ ] Parse group join/leave events
- [ ] Track group size changes

### Phase 2: Role Classification
- [ ] Build class-to-role mapping database
- [ ] Implement role detection logic
- [ ] Create role coverage calculator
- [ ] Add role balance scoring

### Phase 3: Performance Correlation
- [ ] Link combat statistics to group context
- [ ] Calculate per-composition metrics
- [ ] Build composition comparison tools
- [ ] Implement synergy scoring

### Phase 4: GUI Integration
- [ ] Design group visualization widget
- [ ] Create composition history view
- [ ] Build recommendations panel
- [ ] Add template matching display

## Technical Notes

### Data Structures

```csharp
public record GroupMember(
    string Name,
    CharacterClass Class,
    GroupRole PrimaryRole,
    GroupRole? SecondaryRole,
    DateTime JoinedAt,
    DateTime? LeftAt
);

public record GroupComposition(
    Guid SessionId,
    DateTime Timestamp,
    IReadOnlyList<GroupMember> Members,
    GroupTemplate? MatchedTemplate,
    double BalanceScore
);

public enum GroupRole
{
    Tank,
    Healer,
    CrowdControl,
    MeleeDps,
    CasterDps,
    Support,
    Hybrid
}

public record GroupTemplate(
    string Name,
    IReadOnlyDictionary<GroupRole, int> RequiredRoles,
    int MinSize,
    int MaxSize,
    string Description
);
```

### Role Assignment Matrix

```csharp
public static class RoleAssignments
{
    public static readonly Dictionary<CharacterClass, GroupRole[]> ClassRoles = new()
    {
        [CharacterClass.Cleric] = [GroupRole.Healer],
        [CharacterClass.Armsman] = [GroupRole.Tank, GroupRole.MeleeDps],
        [CharacterClass.Sorcerer] = [GroupRole.CrowdControl, GroupRole.CasterDps],
        [CharacterClass.Minstrel] = [GroupRole.Support, GroupRole.MeleeDps],
        // ... complete mapping for all classes
    };
}
```

### Balance Scoring Algorithm

```csharp
public double CalculateBalanceScore(GroupComposition composition)
{
    var roleCount = composition.Members
        .GroupBy(m => m.PrimaryRole)
        .ToDictionary(g => g.Key, g => g.Count());

    double score = 100.0;

    // Penalize missing essential roles
    if (!roleCount.ContainsKey(GroupRole.Healer))
        score -= 30;
    if (!roleCount.ContainsKey(GroupRole.Tank))
        score -= 20;

    // Penalize imbalance
    var dpsCount = roleCount.GetValueOrDefault(GroupRole.MeleeDps) +
                   roleCount.GetValueOrDefault(GroupRole.CasterDps);
    if (dpsCount < 2)
        score -= 15;

    // Bonus for CC coverage
    if (roleCount.ContainsKey(GroupRole.CrowdControl))
        score += 10;

    return Math.Max(0, Math.Min(100, score));
}
```
