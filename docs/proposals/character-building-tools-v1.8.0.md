# Character Building Tools - Implementation Proposal

**Version:** 1.8.0+
**Status:** Proposed
**Created:** January 2026
**Author:** Combat Reporter Team

---

## Executive Summary

This proposal introduces **Character Building Tools** to Camelot Combat Reporter, enabling players to create persistent character profiles that attach combat logs to track performance across class, specialization, stats, and realm rank progression. Players will be able to trace how changes in their character build affect damage output, damage mitigation, healing effectiveness, and overall combat performance.

---

## Table of Contents

1. [Goals & Objectives](#goals--objectives)
2. [Core Features](#core-features)
3. [Data Models](#data-models)
4. [User Interface Design](#user-interface-design)
5. [Service Architecture](#service-architecture)
6. [Combat Log Integration](#combat-log-integration)
7. [Performance Analysis Engine](#performance-analysis-engine)
8. [Implementation Phases](#implementation-phases)
9. [Technical Considerations](#technical-considerations)
10. [Future Enhancements](#future-enhancements)

---

## Goals & Objectives

### Primary Goals

1. **Character Profile Management** - Create and manage detailed character profiles with class, specialization, stats, and realm rank
2. **Combat Log Attachment** - Associate combat sessions with specific character builds to track performance over time
3. **Build Comparison** - Compare performance metrics across different builds, gear configurations, and realm rank milestones
4. **Progression Tracking** - Visualize damage output/receipt changes as realm rank and gear improve

### Success Metrics

- Players can create character profiles in under 2 minutes
- Combat sessions auto-associate with correct character profile
- Build comparisons provide actionable insights on damage deltas
- Realm rank progression charts show clear performance trends

---

## Core Features

### 1. Character Profile System

- **Profile Creation** - Name, realm, class, level, realm rank
- **Specialization Tracking** - Class-specific spec lines and point allocations
- **Template Builds** - Pre-defined meta builds for each class archetype
- **Multi-Character Support** - Manage multiple characters across servers/realms

### 2. Build Configuration

- **Spec Line Allocation** - Track specialization points (1-50+ per line)
- **Realm Ability Selection** - Track purchased realm abilities and ranks
- **Stat Attributes** - STR, CON, DEX, QUI, INT, PIE, EMP, CHA tracking
- **Equipment Bonuses** - Aggregate stat bonuses from gear (manual or inferred)

### 3. Combat Log Attachment

- **Session Binding** - Attach parsed combat sessions to character builds
- **Build Versioning** - Create snapshots when build changes occur
- **Auto-Detection** - Suggest character matches based on class/abilities used

### 4. Performance Analytics

- **Damage Output Analysis** - DPS trends across builds/time
- **Damage Mitigation** - Damage taken patterns by build configuration
- **Healing Efficiency** - HPS metrics for healer/hybrid classes
- **Realm Rank Impact** - Correlate RR progression with performance gains

### 5. Build Comparison Engine

- **Side-by-Side Comparison** - Compare two builds with delta calculations
- **Trend Analysis** - Performance over time with build change markers
- **What-If Scenarios** - Project performance with spec/RA changes

---

## Data Models

### CharacterProfile

```csharp
/// <summary>
/// Persistent character profile with associated combat history.
/// </summary>
public record CharacterProfile
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string Name { get; init; }
    public required Realm Realm { get; init; }
    public required CharacterClass Class { get; init; }
    public int Level { get; init; } = 50;
    public string? ServerName { get; init; }
    public DateTime CreatedUtc { get; init; } = DateTime.UtcNow;
    public DateTime LastUpdatedUtc { get; set; }

    // Active build configuration
    public CharacterBuild? ActiveBuild { get; set; }

    // Build history for comparison
    public IReadOnlyList<CharacterBuild> BuildHistory { get; init; } = [];

    // Associated combat sessions
    public IReadOnlyList<Guid> AttachedSessionIds { get; init; } = [];

    // Computed progression data
    public RealmRankProgression? RankProgression { get; set; }
}
```

### CharacterBuild

```csharp
/// <summary>
/// Snapshot of a character's build at a point in time.
/// </summary>
public record CharacterBuild
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string Name { get; init; }  // e.g., "RR5 Caster Nuke Build"
    public DateTime CreatedUtc { get; init; } = DateTime.UtcNow;

    // Realm rank at time of build
    public int RealmRank { get; init; }
    public long RealmPoints { get; init; }

    // Specialization allocations (class-specific)
    public IReadOnlyDictionary<string, int> SpecLines { get; init; } =
        new Dictionary<string, int>();

    // Realm abilities with ranks
    public IReadOnlyList<RealmAbilitySelection> RealmAbilities { get; init; } = [];

    // Character stats (base + bonuses)
    public CharacterStats Stats { get; init; } = new();

    // Optional notes
    public string? Notes { get; init; }

    // Performance metrics snapshot (calculated from attached sessions)
    public BuildPerformanceMetrics? PerformanceMetrics { get; set; }
}
```

### CharacterStats

```csharp
/// <summary>
/// Character attribute stats with base and bonus values.
/// </summary>
public record CharacterStats
{
    // Primary stats (base value, bonus from gear/buffs)
    public StatValue Strength { get; init; } = new(60, 0);
    public StatValue Constitution { get; init; } = new(60, 0);
    public StatValue Dexterity { get; init; } = new(60, 0);
    public StatValue Quickness { get; init; } = new(60, 0);
    public StatValue Intelligence { get; init; } = new(60, 0);
    public StatValue Piety { get; init; } = new(60, 0);
    public StatValue Empathy { get; init; } = new(60, 0);
    public StatValue Charisma { get; init; } = new(60, 0);

    // Derived combat stats
    public int HitPoints { get; init; }
    public int Power { get; init; }
    public int ArmorFactor { get; init; }
    public int AbsorptionPercent { get; init; }

    // Resists (by damage type)
    public IReadOnlyDictionary<DamageType, int> Resistances { get; init; } =
        new Dictionary<DamageType, int>();
}

public record StatValue(int Base, int Bonus)
{
    public int Total => Base + Bonus;
}
```

### RealmAbilitySelection

```csharp
/// <summary>
/// A realm ability and its trained rank.
/// </summary>
public record RealmAbilitySelection
{
    public required string AbilityName { get; init; }
    public int Rank { get; init; } = 1;
    public int PointCost { get; init; }
    public RealmAbilityCategory Category { get; init; }
}

public enum RealmAbilityCategory
{
    Passive,
    Active,
    Mastery
}
```

### RealmRankProgression

```csharp
/// <summary>
/// Tracks realm rank progression over time with performance metrics.
/// </summary>
public record RealmRankProgression
{
    public IReadOnlyList<RankMilestone> Milestones { get; init; } = [];
}

public record RankMilestone
{
    public int RealmRank { get; init; }
    public long RealmPoints { get; init; }
    public DateTime AchievedUtc { get; init; }

    // Performance at this rank
    public double AverageDps { get; init; }
    public double AverageHps { get; init; }
    public double KillDeathRatio { get; init; }
    public int SessionCount { get; init; }
}
```

### BuildPerformanceMetrics

```csharp
/// <summary>
/// Aggregated performance metrics for a specific build.
/// </summary>
public record BuildPerformanceMetrics
{
    public int SessionCount { get; init; }
    public TimeSpan TotalCombatTime { get; init; }

    // Damage output
    public long TotalDamageDealt { get; init; }
    public double AverageDps { get; init; }
    public double PeakDps { get; init; }
    public double MedianDamagePerHit { get; init; }
    public double CriticalHitRate { get; init; }

    // Damage mitigation
    public long TotalDamageTaken { get; init; }
    public double AverageDamageTakenPerSecond { get; init; }
    public double AvoidanceRate { get; init; }  // Blocks, parries, evades

    // Healing (for healers/hybrids)
    public long TotalHealingDone { get; init; }
    public double AverageHps { get; init; }
    public double OverhealPercent { get; init; }

    // Combat effectiveness
    public int Kills { get; init; }
    public int Deaths { get; init; }
    public int Assists { get; init; }
    public double KillDeathRatio { get; init; }

    // Style/ability breakdown
    public IReadOnlyDictionary<string, DamageBreakdown> TopDamageSources { get; init; } =
        new Dictionary<string, DamageBreakdown>();
}

public record DamageBreakdown(
    string SourceName,
    long TotalDamage,
    int HitCount,
    double AverageDamage,
    double PercentOfTotal
);
```

### SpecializationTemplate (Class-Specific)

```csharp
/// <summary>
/// Defines available specialization lines for a class.
/// </summary>
public record SpecializationTemplate
{
    public required CharacterClass Class { get; init; }
    public IReadOnlyList<SpecLine> SpecLines { get; init; } = [];
}

public record SpecLine
{
    public required string Name { get; init; }
    public int MaxLevel { get; init; } = 50;
    public SpecLineType Type { get; init; }
    public string? Description { get; init; }
}

public enum SpecLineType
{
    Weapon,
    Magic,
    Utility,
    Hybrid
}
```

---

## User Interface Design

### New Tab: "Character Profiles"

The Character Profiles tab will be the 14th tab in the main window, positioned after Battlegrounds.

#### Tab Layout Structure

```
┌─────────────────────────────────────────────────────────────────────────────┐
│  Character Profiles                                                          │
├─────────────────────────────────────────────────────────────────────────────┤
│ ┌───────────────────┐  ┌──────────────────────────────────────────────────┐ │
│ │ PROFILES LIST     │  │ PROFILE DETAILS                                  │ │
│ │                   │  │                                                  │ │
│ │ [+] New Profile   │  │ ┌────────────────────────────────────────────┐  │ │
│ │                   │  │ │ Character: Thorin                          │  │ │
│ │ ▼ Albion          │  │ │ Class: Armsman  |  Realm: Albion           │  │ │
│ │   ● Thorin (Arms) │  │ │ Level: 50  |  Realm Rank: 8L4              │  │ │
│ │   ○ Merlyn (Wiz)  │  │ │ Server: Ywain                              │  │ │
│ │                   │  │ └────────────────────────────────────────────┘  │ │
│ │ ▼ Midgard         │  │                                                  │ │
│ │   ○ Ragnar (War)  │  │ ┌─────────────────────────────────────────────┐ │ │
│ │                   │  │ │ ACTIVE BUILD: "RR8 Polearm Tank"           │ │ │
│ │ ▼ Hibernia        │  │ │                                             │ │ │
│ │   ○ Fionn (Hero)  │  │ │ Spec Lines:                                 │ │ │
│ │                   │  │ │   Polearm: 50  |  Shield: 42  |  Parry: 35  │ │ │
│ │                   │  │ │                                             │ │ │
│ │ ─────────────────│  │ │ Realm Abilities:                            │ │ │
│ │                   │  │ │   ● Purge (3)  ● DT (5)  ● IP (3)          │ │ │
│ │ Filter: [______]  │  │ │   ● MoP (3)  ● Avoid Pain (3)              │ │ │
│ │                   │  │ └─────────────────────────────────────────────┘ │ │
│ └───────────────────┘  │                                                  │ │
│                        │ ┌─────────────────────────────────────────────┐ │ │
│                        │ │ PERFORMANCE SUMMARY                         │ │ │
│                        │ │                                             │ │ │
│                        │ │ Sessions: 47  |  Combat Time: 12h 34m      │ │ │
│                        │ │ Avg DPS: 342  |  Peak DPS: 891             │ │ │
│                        │ │ K/D Ratio: 2.4  |  Kills: 156              │ │ │
│                        │ │                                             │ │ │
│                        │ │ [View Full Analytics]  [Compare Builds]    │ │ │
│                        │ └─────────────────────────────────────────────┘ │ │
│                        └──────────────────────────────────────────────────┘ │
│                                                                             │
│ ┌─────────────────────────────────────────────────────────────────────────┐ │
│ │ ATTACHED COMBAT SESSIONS                                      [Attach] │ │
│ ├─────────────────────────────────────────────────────────────────────────┤ │
│ │ Date       │ Duration │ DPS  │ K/D  │ Build            │ RR    │ Notes │ │
│ │──────────────────────────────────────────────────────────────────────── │ │
│ │ 2026-01-07 │ 45:23    │ 367  │ 3.2  │ RR8 Polearm Tank │ 8L4   │       │ │
│ │ 2026-01-06 │ 1:12:05  │ 341  │ 2.1  │ RR8 Polearm Tank │ 8L3   │       │ │
│ │ 2026-01-05 │ 32:11    │ 298  │ 1.8  │ RR7 Sword/Shield │ 7L9   │       │ │
│ │ ...        │          │      │      │                  │       │       │ │
│ └─────────────────────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────────────────────┘
```

#### Build Editor Dialog

```
┌─────────────────────────────────────────────────────────────────────────────┐
│ Edit Build: "RR8 Polearm Tank"                                    [X]      │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│ Build Name: [RR8 Polearm Tank_____________]                                 │
│                                                                             │
│ ┌─ Realm Rank ────────────────────────────────────────────────────────────┐ │
│ │  Rank: [8__]  Level: [L4]   Realm Points: [4,250,000______]             │ │
│ └─────────────────────────────────────────────────────────────────────────┘ │
│                                                                             │
│ ┌─ Specializations ───────────────────────────────────────────────────────┐ │
│ │                                                                         │ │
│ │  Polearm:    [====================================] 50                  │ │
│ │  Shield:     [==============================    ] 42                    │ │
│ │  Parry:      [========================          ] 35                    │ │
│ │  Slash:      [                                  ] 1                     │ │
│ │  Thrust:     [                                  ] 1                     │ │
│ │  Crossbow:   [                                  ] 1                     │ │
│ │                                                                         │ │
│ │  Total Points: 130 / 130                                                │ │
│ └─────────────────────────────────────────────────────────────────────────┘ │
│                                                                             │
│ ┌─ Realm Abilities ───────────────────────────────────────────────────────┐ │
│ │                                                                         │ │
│ │  [+] Add Realm Ability                    Points Used: 42 / 45          │ │
│ │                                                                         │ │
│ │  ┌───────────────────┬───────┬────────┐                                 │ │
│ │  │ Ability           │ Rank  │ Cost   │                                 │ │
│ │  ├───────────────────┼───────┼────────┤                                 │ │
│ │  │ Determination     │ 5     │ 14     │  [−]                            │ │
│ │  │ Purge             │ 3     │ 10     │  [−]                            │ │
│ │  │ Ignore Pain       │ 3     │ 6      │  [−]                            │ │
│ │  │ Mastery of Pain   │ 3     │ 6      │  [−]                            │ │
│ │  │ Avoid Pain        │ 3     │ 6      │  [−]                            │ │
│ │  └───────────────────┴───────┴────────┘                                 │ │
│ └─────────────────────────────────────────────────────────────────────────┘ │
│                                                                             │
│ ┌─ Stats (Optional) ──────────────────────────────────────────────────────┐ │
│ │                                                                         │ │
│ │  STR: [75] +[101]    CON: [75] +[101]    DEX: [60] +[80]               │ │
│ │  QUI: [60] +[26]     INT: [--] +[--]     PIE: [--] +[--]               │ │
│ │  EMP: [--] +[--]     CHA: [--] +[--]                                    │ │
│ │                                                                         │ │
│ │  Hits: [+400]   AF: [--]   Abs: [27%]                                   │ │
│ └─────────────────────────────────────────────────────────────────────────┘ │
│                                                                             │
│ Notes: [________________________________________________]                   │
│                                                                             │
│                                          [Cancel]  [Save as New]  [Save]   │
└─────────────────────────────────────────────────────────────────────────────┘
```

#### Build Comparison View

```
┌─────────────────────────────────────────────────────────────────────────────┐
│ Build Comparison: Thorin                                          [X]      │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│ ┌─────────────────────────┐    ┌─────────────────────────┐                 │
│ │ BUILD A                 │    │ BUILD B                 │                 │
│ │ [RR8 Polearm Tank    ▼] │    │ [RR7 Sword/Shield   ▼]  │                 │
│ │ Sessions: 23            │    │ Sessions: 18            │                 │
│ └─────────────────────────┘    └─────────────────────────┘                 │
│                                                                             │
│ ┌─ Performance Delta ─────────────────────────────────────────────────────┐ │
│ │                                                                         │ │
│ │              Build A          Build B          Delta                    │ │
│ │  ─────────────────────────────────────────────────────                  │ │
│ │  Avg DPS:    342              287              +55 (+19.2%) ▲           │ │
│ │  Peak DPS:   891              743              +148 (+19.9%) ▲          │ │
│ │  K/D Ratio:  2.4              1.9              +0.5 (+26.3%) ▲          │ │
│ │  Avg Dmg:    1,247            1,089            +158 (+14.5%) ▲          │ │
│ │  Dmg Taken:  234/s            312/s            -78 (-25.0%) ▲           │ │
│ │  Kills:      156              98               +58 (+59.2%) ▲           │ │
│ │  Deaths:     65               52               +13 (+25.0%) ▼           │ │
│ │                                                                         │ │
│ └─────────────────────────────────────────────────────────────────────────┘ │
│                                                                             │
│ ┌─ Spec Differences ──────────────────────────────────────────────────────┐ │
│ │                                                                         │ │
│ │  Spec Line      Build A     Build B     Delta                           │ │
│ │  ────────────────────────────────────────────                           │ │
│ │  Polearm        50          1           +49                             │ │
│ │  Sword          1           44          -43                             │ │
│ │  Shield         42          50          -8                              │ │
│ │  Parry          35          35          0                               │ │
│ │                                                                         │ │
│ └─────────────────────────────────────────────────────────────────────────┘ │
│                                                                             │
│ ┌─ Damage Source Comparison ──────────────────────────────────────────────┐ │
│ │                                                                         │ │
│ │  ████████████████████████  Polearm Styles (Build A: 67%)                │ │
│ │  ██████████████            Sword Styles (Build B: 58%)                  │ │
│ │  ████████                  Shield Slam (Both: ~20%)                     │ │
│ │  ████                      Other (Both: ~15%)                           │ │
│ │                                                                         │ │
│ └─────────────────────────────────────────────────────────────────────────┘ │
│                                                                             │
│                                                    [Export Report]  [Close] │
└─────────────────────────────────────────────────────────────────────────────┘
```

#### Realm Rank Progression Chart

```
┌─────────────────────────────────────────────────────────────────────────────┐
│ Realm Rank Progression: Thorin                                              │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  DPS                                                                        │
│   │                                                    ●─────● RR8L4       │
│ 400├                                          ●───────●                     │
│   │                              ●────●──────●   Build Change               │
│ 350├                    ●───────●                (Polearm)                  │
│   │          ●─────────●                                                    │
│ 300├    ●────●                                                              │
│   │   ●                                                                     │
│ 250├──●                                                                     │
│   │                                                                         │
│ 200├──┬────┬────┬────┬────┬────┬────┬────┬────┬────┬────┬────┬────┬────    │
│      RR1  RR2  RR3  RR4  RR5  RR6  RR7  RR8  RR9 RR10 RR11 RR12 RR13       │
│                                                                             │
│  Legend:  ● Session Average   ─── Trend Line   │ Build Change              │
│                                                                             │
│  [DPS ▼]  [K/D Ratio]  [Damage Taken]  [Healing]                           │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## Service Architecture

### New Services

#### ICharacterProfileService

```csharp
/// <summary>
/// Manages character profiles and associated builds.
/// </summary>
public interface ICharacterProfileService
{
    // Profile CRUD
    Task<CharacterProfile> CreateProfileAsync(CharacterProfile profile);
    Task<CharacterProfile?> GetProfileAsync(Guid profileId);
    Task<IReadOnlyList<CharacterProfile>> GetAllProfilesAsync();
    Task<IReadOnlyList<CharacterProfile>> GetProfilesByRealmAsync(Realm realm);
    Task UpdateProfileAsync(CharacterProfile profile);
    Task DeleteProfileAsync(Guid profileId);

    // Build management
    Task<CharacterBuild> CreateBuildAsync(Guid profileId, CharacterBuild build);
    Task<CharacterBuild> CloneBuildAsync(Guid buildId, string newName);
    Task UpdateBuildAsync(CharacterBuild build);
    Task SetActiveBuildAsync(Guid profileId, Guid buildId);
    Task<IReadOnlyList<CharacterBuild>> GetBuildHistoryAsync(Guid profileId);

    // Session attachment
    Task AttachSessionAsync(Guid profileId, Guid sessionId, Guid? buildId = null);
    Task DetachSessionAsync(Guid profileId, Guid sessionId);
    Task<IReadOnlyList<ExtendedCombatStatistics>> GetAttachedSessionsAsync(Guid profileId);

    // Auto-matching
    Task<CharacterProfile?> SuggestProfileForSessionAsync(ExtendedCombatStatistics session);
}
```

#### IBuildComparisonService

```csharp
/// <summary>
/// Compares performance metrics between builds.
/// </summary>
public interface IBuildComparisonService
{
    Task<BuildComparisonResult> CompareBuildsAsync(Guid buildIdA, Guid buildIdB);
    Task<BuildComparisonResult> CompareBuildToBaselineAsync(Guid buildId, Guid profileId);
    Task<TrendAnalysis> AnalyzeTrendAsync(Guid profileId, DateRange range);
}

public record BuildComparisonResult(
    CharacterBuild BuildA,
    CharacterBuild BuildB,
    PerformanceDelta PerformanceDelta,
    IReadOnlyDictionary<string, int> SpecDelta,
    IReadOnlyList<string> RealmAbilityDelta
);

public record PerformanceDelta(
    double DpsDelta,
    double DpsPercentChange,
    double KdDelta,
    double KdPercentChange,
    double DamageTakenDelta,
    double DamageTakenPercentChange,
    // ... additional metrics
);
```

#### IRealmRankProgressionService

```csharp
/// <summary>
/// Tracks realm rank progression and correlates with performance.
/// </summary>
public interface IRealmRankProgressionService
{
    Task<RealmRankProgression> GetProgressionAsync(Guid profileId);
    Task RecordMilestoneAsync(Guid profileId, RankMilestone milestone);
    Task<IReadOnlyList<RankMilestone>> GetMilestonesBetweenAsync(
        Guid profileId, int startRank, int endRank);
    Task<PerformanceProjection> ProjectPerformanceAtRankAsync(
        Guid profileId, int targetRank);
}
```

#### ISpecializationTemplateService

```csharp
/// <summary>
/// Provides class-specific specialization templates and validation.
/// </summary>
public interface ISpecializationTemplateService
{
    SpecializationTemplate GetTemplateForClass(CharacterClass charClass);
    int GetMaxSpecPoints(int level, int realmRank);
    bool ValidateSpecAllocation(CharacterBuild build);
    IReadOnlyList<CharacterBuild> GetMetaBuildsForClass(CharacterClass charClass);
}
```

### Storage

```
%APPDATA%/CamelotCombatReporter/
├── profiles/
│   ├── profiles-index.json        # Profile manifest
│   ├── {profileId}.json           # Individual profile data
│   └── builds/
│       └── {buildId}.json         # Build snapshots
├── templates/
│   ├── spec-templates.json        # Class spec line definitions
│   └── meta-builds/               # Community meta build templates
│       ├── albion/
│       ├── midgard/
│       └── hibernia/
└── cross-realm/                   # Existing session storage
    └── sessions/
```

---

## Combat Log Integration

### Auto-Detection Algorithm

When a new combat session is parsed, the system will attempt to match it to an existing character profile:

```csharp
public async Task<CharacterProfile?> SuggestProfileForSessionAsync(
    ExtendedCombatStatistics session)
{
    var candidates = await GetAllProfilesAsync();

    // Score each profile based on matching criteria
    var scores = candidates.Select(profile => new
    {
        Profile = profile,
        Score = CalculateMatchScore(profile, session)
    })
    .Where(x => x.Score > 0.5)  // Minimum threshold
    .OrderByDescending(x => x.Score)
    .ToList();

    return scores.FirstOrDefault()?.Profile;
}

private double CalculateMatchScore(CharacterProfile profile, ExtendedCombatStatistics session)
{
    double score = 0;

    // Exact class match: +0.4
    if (profile.Class == session.Character.Class)
        score += 0.4;

    // Realm match: +0.2
    if (profile.Realm == session.Character.Realm)
        score += 0.2;

    // Name similarity (fuzzy match): +0.3
    score += StringSimilarity(profile.Name, session.Character.Name) * 0.3;

    // Recent activity on this profile: +0.1
    if (HasRecentSessions(profile, TimeSpan.FromDays(7)))
        score += 0.1;

    return score;
}
```

### Session Attachment Workflow

1. User parses combat log file
2. System detects character class/realm from combat events
3. Auto-suggest matching profile (if exists)
4. User confirms or selects different profile
5. User optionally assigns to specific build version
6. Session metadata linked to profile

---

## Performance Analysis Engine

### Damage Output Correlation

The system will correlate spec line investments with damage output:

```csharp
public record SpecDamageCorrelation
{
    public string SpecLine { get; init; }
    public int SpecLevel { get; init; }
    public double AverageDamagePerPoint { get; init; }
    public double CorrelationCoefficient { get; init; }  // -1 to +1
}

public async Task<IReadOnlyList<SpecDamageCorrelation>> AnalyzeSpecImpactAsync(
    Guid profileId)
{
    var builds = await GetBuildHistoryAsync(profileId);
    var correlations = new List<SpecDamageCorrelation>();

    foreach (var specLine in builds.SelectMany(b => b.SpecLines.Keys).Distinct())
    {
        var dataPoints = builds
            .Where(b => b.PerformanceMetrics != null)
            .Select(b => (
                SpecLevel: b.SpecLines.GetValueOrDefault(specLine, 0),
                Dps: b.PerformanceMetrics!.AverageDps
            ))
            .ToList();

        if (dataPoints.Count >= 3)
        {
            var correlation = CalculatePearsonCorrelation(
                dataPoints.Select(d => (double)d.SpecLevel),
                dataPoints.Select(d => d.Dps)
            );

            correlations.Add(new SpecDamageCorrelation
            {
                SpecLine = specLine,
                SpecLevel = dataPoints.Max(d => d.SpecLevel),
                CorrelationCoefficient = correlation
            });
        }
    }

    return correlations;
}
```

### Realm Rank Impact Analysis

```csharp
public record RealmRankImpact
{
    public int FromRank { get; init; }
    public int ToRank { get; init; }
    public double DpsIncrease { get; init; }
    public double DpsPercentIncrease { get; init; }
    public IReadOnlyList<string> NewAbilitiesUnlocked { get; init; }
    public string PrimaryContributor { get; init; }  // e.g., "Mastery of Pain 3"
}
```

---

## Implementation Phases

### Phase 1: Core Profile System (v1.8.0)

**Duration:** Foundation release

**Features:**
- [ ] CharacterProfile and CharacterBuild data models
- [ ] ICharacterProfileService implementation with JSON storage
- [ ] Profile creation/editing dialog
- [ ] Basic profile list view in new "Character Profiles" tab
- [ ] Manual session attachment to profiles
- [ ] Profile import/export (JSON format)

**Files to Create/Modify:**
- `src/CamelotCombatReporter.Core/CharacterBuilding/Models/`
- `src/CamelotCombatReporter.Core/CharacterBuilding/Services/`
- `src/CamelotCombatReporter.Gui/CharacterBuilding/`

### Phase 2: Build Configuration (v1.8.1)

**Duration:** Spec and RA tracking

**Features:**
- [ ] Specialization template definitions for all 48 classes
- [ ] Build editor with spec line sliders
- [ ] Realm ability selection with point validation
- [ ] Build history and versioning
- [ ] "Save as New Build" workflow

**Files to Create/Modify:**
- `src/CamelotCombatReporter.Core/CharacterBuilding/Templates/`
- `src/CamelotCombatReporter.Gui/CharacterBuilding/BuildEditorDialog.axaml`

### Phase 3: Performance Analytics (v1.9.0)

**Duration:** Analysis engine

**Features:**
- [ ] BuildPerformanceMetrics calculation from attached sessions
- [ ] Performance summary cards in profile view
- [ ] Top damage sources breakdown
- [ ] Combat time and efficiency metrics
- [ ] Filtering by date range and build

**Files to Create/Modify:**
- `src/CamelotCombatReporter.Core/CharacterBuilding/Analysis/`
- `src/CamelotCombatReporter.Gui/CharacterBuilding/PerformanceSummaryView.axaml`

### Phase 4: Build Comparison (v1.9.1)

**Duration:** Comparison tools

**Features:**
- [ ] IBuildComparisonService implementation
- [ ] Side-by-side build comparison dialog
- [ ] Delta calculations with percentage changes
- [ ] Spec difference visualization
- [ ] Damage source comparison charts

**Files to Create/Modify:**
- `src/CamelotCombatReporter.Core/CharacterBuilding/Services/BuildComparisonService.cs`
- `src/CamelotCombatReporter.Gui/CharacterBuilding/BuildComparisonDialog.axaml`

### Phase 5: Progression Tracking (v1.10.0)

**Duration:** Realm rank progression

**Features:**
- [ ] IRealmRankProgressionService implementation
- [ ] Realm rank milestone recording
- [ ] Progression chart visualization (LiveCharts2)
- [ ] Build change markers on timeline
- [ ] Performance trend analysis

**Files to Create/Modify:**
- `src/CamelotCombatReporter.Core/CharacterBuilding/Services/RealmRankProgressionService.cs`
- `src/CamelotCombatReporter.Gui/CharacterBuilding/ProgressionChartView.axaml`

### Phase 6: Auto-Detection & Polish (v1.10.1)

**Duration:** Intelligence and UX

**Features:**
- [ ] Auto-suggest profile for new sessions
- [ ] Infer class/spec from combat style usage
- [ ] Community meta build templates
- [ ] Profile sharing (export with sessions)
- [ ] Keyboard shortcuts and accessibility

---

## Technical Considerations

### Performance

- **Lazy Loading:** Build history and session metrics loaded on-demand
- **Caching:** Aggregated performance metrics cached per build
- **Indexing:** Profile index file for fast lookup without loading full profiles
- **Background Calculation:** Performance metrics calculated on background thread

### Data Integrity

- **Build Immutability:** Builds are snapshots; edits create new versions
- **Session Orphans:** Detaching sessions doesn't delete combat data
- **Migration Path:** Version field in profile JSON for future schema changes

### Privacy

- **Local Storage Only:** No cloud sync without explicit user action
- **Export Sanitization:** Option to strip character names on export
- **No Telemetry:** Profile data never leaves user's machine

---

## Future Enhancements

### v1.11.0+ Potential Features

1. **Template Sharing Platform** - Community-contributed meta builds
2. **Guild Roster Integration** - Link profiles to guild members
3. **Equipment Slot Tracking** - Detailed gear loadout management
4. **Buff Template Integration** - Link builds to buff configurations
5. **Training Progression** - Track spec points spent while leveling
6. **Multi-Account Support** - Manage characters across DAoC accounts
7. **Performance Predictions** - ML-based damage projections for build changes
8. **Combat Replay Integration** - Link builds to specific combat moments

---

## Appendix A: Class Specialization Lines

### Albion

| Class | Spec Lines |
|-------|-----------|
| Armsman | Crush, Slash, Thrust, Polearm, Two-Handed, Shield, Parry, Crossbow |
| Cabalist | Body, Matter, Spirit, Focus |
| Cleric | Rejuvenation, Enhancement, Smiting |
| Friar | Rejuvenation, Enhancement, Staff, Parry |
| Heretic | Rejuvenation, Enhancement, Crush, Flexible |
| Infiltrator | Slash, Thrust, Dual Wield, Critical Strike, Stealth, Envenom |
| Mercenary | Slash, Thrust, Crush, Dual Wield, Parry, Shield |
| Minstrel | Instruments, Slash, Thrust, Stealth |
| Necromancer | Deathsight, Painworking, Death Servant, Focus |
| Paladin | Slash, Thrust, Crush, Two-Handed, Shield, Parry, Chants |
| Reaver | Slash, Thrust, Crush, Flexible, Soulrending, Parry, Shield |
| Scout | Archery, Shield, Slash, Thrust, Stealth |
| Sorcerer | Body, Mind, Matter, Focus |
| Theurgist | Earth, Ice, Wind, Focus |
| Wizard | Earth, Fire, Ice, Focus |
| Mauler | Fist, Magnetism, Power Strikes, Aura Manipulation |

### Midgard

| Class | Spec Lines |
|-------|-----------|
| Berserker | Sword, Axe, Hammer, Left Axe, Parry |
| Bonedancer | Darkness, Suppression, Bone Army, Focus |
| Healer | Mending, Augmentation, Pacification |
| Hunter | Beastcraft, Spear, Composite Bow, Stealth |
| Runemaster | Darkness, Suppression, Runecarving, Focus |
| Savage | Sword, Axe, Hammer, Hand-to-Hand, Savagery, Parry |
| Shadowblade | Sword, Axe, Left Axe, Critical Strike, Stealth, Envenom |
| Shaman | Mending, Augmentation, Subterranean, Cave Magic |
| Skald | Sword, Axe, Hammer, Parry, Battlesongs |
| Spiritmaster | Darkness, Suppression, Summoning, Focus |
| Thane | Sword, Axe, Hammer, Shield, Parry, Stormcalling |
| Valkyrie | Sword, Spear, Shield, Parry, Odin's Will, Mending |
| Warlock | Cursing, Hexing, Witchcraft, Focus |
| Warrior | Sword, Axe, Hammer, Shield, Parry |
| Mauler | Fist, Magnetism, Power Strikes, Aura Manipulation |

### Hibernia

| Class | Spec Lines |
|-------|-----------|
| Animist | Arboreal Path, Creeping Path, Verdant Path, Focus |
| Bainshee | Ethereal Shriek, Phantasmal Wail, Spectral Guard, Focus |
| Bard | Music, Nurture, Regrowth, Blades |
| Blademaster | Blades, Piercing, Blunt, Celtic Dual, Parry, Shield |
| Champion | Blades, Piercing, Blunt, Large Weapons, Shield, Parry, Valor |
| Druid | Nurture, Regrowth, Nature |
| Eldritch | Light, Mana, Void, Focus |
| Enchanter | Light, Mana, Enchantments, Focus |
| Hero | Blades, Piercing, Blunt, Large Weapons, Celtic Spear, Shield, Parry |
| Mentalist | Light, Mana, Mentalism, Focus |
| Nightshade | Blades, Piercing, Celtic Dual, Critical Strike, Stealth, Envenom |
| Ranger | Archery, Piercing, Blades, Celtic Dual, Stealth, Pathfinding |
| Valewalker | Arboreal Path, Scythe, Parry |
| Vampiir | Piercing, Dementia, Shadow Mastery, Vampiiric Embrace |
| Warden | Nurture, Regrowth, Blades, Blunt, Parry, Shield |
| Mauler | Fist, Magnetism, Power Strikes, Aura Manipulation |

---

## Appendix B: Realm Rank Reference

| Rank | Title | Realm Points Required |
|------|-------|----------------------|
| RR1 | - | 0 |
| RR2 | - | 7,125 |
| RR3 | - | 61,750 |
| RR4 | - | 213,875 |
| RR5 | - | 513,500 |
| RR6 | - | 1,010,625 |
| RR7 | - | 1,755,250 |
| RR8 | - | 2,797,375 |
| RR9 | - | 4,187,000 |
| RR10 | - | 5,974,125 |
| RR11 | - | 8,208,750 |
| RR12 | - | 10,940,875 |
| RR13 | - | 14,220,500 |
| RR14 | - | 18,097,625 |

Each realm rank has 10 levels (L0-L9), with L10 advancing to the next rank.

---

*This document is a living proposal and will be updated as implementation progresses.*
