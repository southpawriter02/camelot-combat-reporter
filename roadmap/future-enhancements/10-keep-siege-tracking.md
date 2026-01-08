# 10. Keep and Siege Tracking

## Status: ✅ Complete (v1.5.0)

**Implementation Complete:**
- ✅ Log parsing infrastructure
- ✅ Cross-realm analysis
- ✅ Keep/siege event parsing patterns

---

## Description

Track keep sieges, relic raids, and objective-based RvR combat. Analyze siege effectiveness, defensive performance, and contribute to realm-wide siege statistics. Provide insights for improving keep attack and defense strategies.

## Functionality

### Core Features

* **Keep Event Parsing:**
  * Keep attack initiation and conclusion
  * Door/wall damage and destruction
  * Lord and guard kills
  * Keep claim and ownership changes
  * Tower captures

* **Siege Weapon Tracking:**
  * Siege engine deployment (rams, trebs, ballistas, catapults)
  * Siege damage dealt to structures
  * Oil/boiling water usage
  * Siege equipment destruction

* **Defensive Tracking:**
  * Guard NPC kills
  * Door repair contributions
  * Defensive kills
  * Time defending before fall

### Keep Types

| Type | Description | Tracking Focus |
|------|-------------|----------------|
| **Border Keeps** | Standard RvR keeps | Attack/defense metrics |
| **Relic Keeps** | Houses realm relics | High-value events |
| **Towers** | Smaller objectives | Capture frequency |
| **Milegates** | Chokepoint structures | Traffic/conflict |
| **Docks** | Boat access points | Strategic captures |

### Siege Phases

```
Phase 1: Approach
├── Group assembly
├── Ram deployment
└── Initial engagement

Phase 2: Outer Siege
├── Door assault
├── Defensive response
└── Guard clearing

Phase 3: Inner Siege
├── Inner door breach
├── Lord room assault
└── Lord engagement

Phase 4: Capture
├── Lord kill
├── Keep claim
└── Upgrade initiation
```

### Analysis Views

* **Siege Timeline:**
  * Visual timeline of siege events
  * Phase markers and duration
  * Key moments (door down, lord engaged)
  * Participant contribution bars

* **Personal Contribution:**
  * Damage dealt to structures
  * Player kills during siege
  * Healing output in siege context
  * Realm point contribution

* **Siege Statistics:**
  * Average siege duration
  * Success rate by keep type
  * Best performing siege compositions
  * Optimal attack times

* **Realm Overview:**
  * Keep ownership map
  * Recent capture history
  * Realm point hotspots
  * Active siege alerts

### Siege Metrics

| Metric | Description |
|--------|-------------|
| **Siege DPS** | Damage per second to structures |
| **Door Time** | Time to breach each door |
| **Kill Efficiency** | Player kills vs. deaths during siege |
| **Contribution Score** | Combined damage, healing, kills weight |
| **Defense Rating** | How long keep held under attack |

### Relic Tracking

* **Relic Raid Events:**
  * Relic pickup detection
  * Carrier tracking
  * Relic return/capture
  * Escort performance

* **Relic Benefits:**
  * Track active relic bonuses
  * Correlate with performance changes
  * Realm relic history

## Requirements

* **Log Parsing:** Keep and siege-specific patterns
* **Zone Data:** Keep locations and types
* **UI:** Siege timeline and contribution views

## Limitations

* Some siege events may not appear in personal logs
* Keep ownership requires realm context
* Large-scale sieges generate massive log data
* Real-time tracking requires streaming parsing

## Dependencies

* **01-log-parsing.md:** Core parsing infrastructure
* **02-real-time-parsing.md:** Live siege tracking
* **03-cross-realm-analysis.md:** Realm context

## Implementation Phases

### Phase 1: Siege Event Parsing
- [ ] Identify keep/siege log message patterns
- [ ] Create SiegeEvent model hierarchy
- [ ] Parse door/structure damage
- [ ] Detect keep capture events

### Phase 2: Siege Analysis
- [ ] Create SiegeStatisticsService
- [ ] Implement siege phase detection
- [ ] Calculate contribution scores
- [ ] Build siege duration metrics

### Phase 3: GUI Integration
- [ ] Design siege timeline component
- [ ] Create contribution breakdown view
- [ ] Build keep status display
- [ ] Add siege summary panel

### Phase 4: Realm Integration
- [ ] Track keep ownership state
- [ ] Implement relic tracking
- [ ] Create realm statistics aggregation
- [ ] Add siege history log

## Technical Notes

### Data Structures

```csharp
public abstract record SiegeEvent(DateTime Timestamp, string KeepName);

public record DoorDamageEvent(
    DateTime Timestamp,
    string KeepName,
    string DoorName,
    int DamageAmount,
    string Source,
    bool IsDestroyed
) : SiegeEvent(Timestamp, KeepName);

public record KeepCapturedEvent(
    DateTime Timestamp,
    string KeepName,
    Realm NewOwner,
    Realm PreviousOwner,
    string ClaimingGuild
) : SiegeEvent(Timestamp, KeepName);

public record SiegeSession(
    Guid Id,
    string KeepName,
    DateTime StartTime,
    DateTime? EndTime,
    SiegeOutcome Outcome,
    Realm AttackingRealm,
    Realm DefendingRealm,
    IReadOnlyList<SiegeEvent> Events,
    SiegeContribution PlayerContribution
);

public record SiegeContribution(
    int StructureDamage,
    int PlayerKills,
    int Deaths,
    int HealingDone,
    int GuardKills,
    double ContributionScore
);

public enum SiegeOutcome
{
    InProgress,
    AttackSuccess,
    DefenseSuccess,
    Abandoned,
    Unknown
}
```

### Keep Database

```csharp
public record KeepInfo(
    string Name,
    KeepType Type,
    Realm HomeRealm,
    string Zone,
    int BaseLevel,
    int DoorCount
);

public enum KeepType
{
    BorderKeep,
    RelicKeep,
    Tower,
    Milegate,
    Dock
}

public static class KeepDatabase
{
    public static readonly IReadOnlyList<KeepInfo> Keeps = new[]
    {
        // Albion
        new KeepInfo("Castle Sauvage", KeepType.BorderKeep, Realm.Albion, "Hadrian's Wall", 5, 2),
        new KeepInfo("Snowdonia Fortress", KeepType.BorderKeep, Realm.Albion, "Snowdonia", 5, 2),

        // Midgard
        new KeepInfo("Svasud Faste", KeepType.BorderKeep, Realm.Midgard, "Odin's Gate", 5, 2),
        new KeepInfo("Vindsaul Faste", KeepType.BorderKeep, Realm.Midgard, "Jamtland Mountains", 5, 2),

        // Hibernia
        new KeepInfo("Druim Ligen", KeepType.BorderKeep, Realm.Hibernia, "Emain Macha", 5, 2),
        new KeepInfo("Druim Cain", KeepType.BorderKeep, Realm.Hibernia, "Cruachan Gorge", 5, 2),

        // ... complete keep database
    };
}
```

### Contribution Scoring

```csharp
public double CalculateContributionScore(SiegeContribution contribution)
{
    const double StructureDamageWeight = 0.01;
    const double PlayerKillWeight = 50.0;
    const double DeathPenalty = 25.0;
    const double HealingWeight = 0.005;
    const double GuardKillWeight = 10.0;

    return (contribution.StructureDamage * StructureDamageWeight) +
           (contribution.PlayerKills * PlayerKillWeight) -
           (contribution.Deaths * DeathPenalty) +
           (contribution.HealingDone * HealingWeight) +
           (contribution.GuardKills * GuardKillWeight);
}
```

### Siege Phase Detection

```csharp
public SiegePhase DetectCurrentPhase(SiegeSession session)
{
    var events = session.Events.OrderBy(e => e.Timestamp).ToList();

    bool outerDoorDown = events.OfType<DoorDamageEvent>()
        .Any(e => e.DoorName.Contains("Outer") && e.IsDestroyed);

    bool innerDoorDown = events.OfType<DoorDamageEvent>()
        .Any(e => e.DoorName.Contains("Inner") && e.IsDestroyed);

    bool lordEngaged = events.OfType<DamageEvent>()
        .Any(e => e.Target.Contains("Lord"));

    if (lordEngaged) return SiegePhase.LordFight;
    if (innerDoorDown) return SiegePhase.InnerSiege;
    if (outerDoorDown) return SiegePhase.OuterSiege;
    return SiegePhase.Approach;
}
```
