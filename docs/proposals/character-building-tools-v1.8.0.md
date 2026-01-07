# Character Building Tools - Implementation Proposal

**Version**: 1.8.0+
**Status**: Proposed
**Author**: Combat Reporter Team
**Date**: January 2026

---

## Executive Summary

This proposal outlines the implementation of **Character Building Tools** for Camelot Combat Reporter, enabling players to create, manage, and analyze character builds in correlation with combat performance data. Players will be able to attach combat logs to their character's class, specialization, stats, and realm ranks, then trace differences in damage output and receipt across different configurations.

---

## Table of Contents

1. [Goals & Objectives](#goals--objectives)
2. [Feature Overview](#feature-overview)
3. [Architecture Design](#architecture-design)
4. [Data Models](#data-models)
5. [Service Layer](#service-layer)
6. [User Interface](#user-interface)
7. [Performance Correlation Engine](#performance-correlation-engine)
8. [Implementation Phases](#implementation-phases)
9. [Testing Strategy](#testing-strategy)
10. [Future Enhancements](#future-enhancements)

---

## Goals & Objectives

### Primary Goals

1. **Build Management**: Allow players to define and save character builds including class, specializations, stats, and realm abilities
2. **Log Association**: Link combat log sessions to specific character builds for performance tracking
3. **Performance Correlation**: Analyze how build changes affect damage output, damage mitigation, healing effectiveness, and survivability
4. **Build Comparison**: Compare performance metrics across different builds to identify optimal configurations
5. **Progression Tracking**: Track character progression through realm ranks and correlate with combat effectiveness

### Success Metrics

- Players can create and manage multiple builds per character
- Combat sessions are automatically or manually linked to builds
- Clear visualization of performance differences between builds
- Actionable insights for build optimization

---

## Feature Overview

### Core Capabilities

```
┌─────────────────────────────────────────────────────────────────┐
│                    CHARACTER BUILDING TOOLS                      │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  ┌──────────────┐    ┌──────────────┐    ┌──────────────┐       │
│  │    BUILD     │    │   SESSION    │    │ PERFORMANCE  │       │
│  │   MANAGER    │───▶│   LINKER     │───▶│   ANALYZER   │       │
│  └──────────────┘    └──────────────┘    └──────────────┘       │
│         │                   │                    │               │
│         ▼                   ▼                    ▼               │
│  ┌──────────────┐    ┌──────────────┐    ┌──────────────┐       │
│  │    SPEC      │    │   COMBAT     │    │    BUILD     │       │
│  │  CALCULATOR  │    │    LOGS      │    │  OPTIMIZER   │       │
│  └──────────────┘    └──────────────┘    └──────────────┘       │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

### Key Features

| Feature | Description | Priority |
|---------|-------------|----------|
| Build Profiles | Create/edit/delete character builds with full spec details | P0 |
| Specialization Editor | Configure weapon skills, magic lines, and realm abilities | P0 |
| Stat Configuration | Define character stats (STR, CON, DEX, etc.) and derived values | P0 |
| Realm Rank Tracking | Track RR progression and earned realm ability points | P0 |
| Session Linking | Associate combat sessions with specific builds | P0 |
| Performance Dashboard | View aggregated metrics per build | P1 |
| Build Comparison | Side-by-side comparison of build performance | P1 |
| Correlation Analysis | Statistical analysis of build changes vs performance | P1 |
| Build Templates | Pre-defined templates for common class builds | P2 |
| Import/Export | Share builds via JSON/clipboard | P2 |
| Optimization Suggestions | AI-driven recommendations based on combat data | P2 |

---

## Architecture Design

### Module Structure

```
src/CamelotCombatReporter.Core/
├── CharacterBuilding/
│   ├── Models/
│   │   ├── CharacterBuild.cs
│   │   ├── SpecializationConfig.cs
│   │   ├── StatBlock.cs
│   │   ├── RealmRankInfo.cs
│   │   ├── BuildPerformanceMetrics.cs
│   │   └── BuildComparison.cs
│   ├── Services/
│   │   ├── ICharacterBuildService.cs
│   │   ├── CharacterBuildService.cs
│   │   ├── ISpecializationService.cs
│   │   ├── SpecializationService.cs
│   │   ├── IBuildPerformanceService.cs
│   │   ├── BuildPerformanceService.cs
│   │   ├── IBuildCorrelationService.cs
│   │   └── BuildCorrelationService.cs
│   ├── Calculations/
│   │   ├── StatCalculator.cs
│   │   ├── SpecPointCalculator.cs
│   │   └── RealmAbilityPointCalculator.cs
│   └── Persistence/
│       ├── IBuildRepository.cs
│       └── JsonBuildRepository.cs

src/CamelotCombatReporter.Gui/
├── CharacterBuilding/
│   ├── Views/
│   │   ├── CharacterBuildingView.axaml
│   │   ├── BuildEditorView.axaml
│   │   ├── SpecializationEditorView.axaml
│   │   ├── BuildComparisonView.axaml
│   │   └── PerformanceDashboardView.axaml
│   └── ViewModels/
│       ├── CharacterBuildingViewModel.cs
│       ├── BuildEditorViewModel.cs
│       ├── SpecializationEditorViewModel.cs
│       ├── BuildComparisonViewModel.cs
│       └── PerformanceDashboardViewModel.cs
```

### Integration Points

```
┌─────────────────────┐     ┌─────────────────────┐
│   Existing Models   │     │   New Build Models  │
├─────────────────────┤     ├─────────────────────┤
│ CharacterInfo       │◄───▶│ CharacterBuild      │
│ CharacterClass      │     │ SpecializationConfig│
│ Realm               │     │ StatBlock           │
│ ExtendedCombatStats │◄───▶│ BuildPerformance    │
│ CombatSession       │◄───▶│ SessionBuildLink    │
│ RealmAbilityInfo    │◄───▶│ RealmRankInfo       │
└─────────────────────┘     └─────────────────────┘
```

---

## Data Models

### CharacterBuild

The core model representing a complete character configuration.

```csharp
/// <summary>
/// Represents a complete character build configuration including
/// class, specializations, stats, and realm abilities.
/// </summary>
public record CharacterBuild
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public CharacterInfo Character { get; init; } = default!;
    public SpecializationConfig Specializations { get; init; } = default!;
    public StatBlock Stats { get; init; } = default!;
    public RealmRankInfo RealmRank { get; init; } = default!;
    public IReadOnlyList<EquippedRealmAbility> RealmAbilities { get; init; } = [];
    public DateTime CreatedUtc { get; init; } = DateTime.UtcNow;
    public DateTime ModifiedUtc { get; init; } = DateTime.UtcNow;
    public bool IsActive { get; init; } = true;
    public IReadOnlyDictionary<string, string> CustomTags { get; init; } =
        new Dictionary<string, string>();
}
```

### SpecializationConfig

Defines the character's skill point allocation.

```csharp
/// <summary>
/// Configuration of specialization points across skill lines.
/// </summary>
public record SpecializationConfig
{
    public CharacterClass Class { get; init; }
    public int TotalSpecPoints { get; init; }
    public int SpentSpecPoints => SkillAllocations.Values.Sum();
    public int RemainingSpecPoints => TotalSpecPoints - SpentSpecPoints;

    /// <summary>
    /// Skill line name to allocated points mapping.
    /// Example: { "Sword": 50, "Shield": 42, "Parry": 35 }
    /// </summary>
    public IReadOnlyDictionary<string, int> SkillAllocations { get; init; } =
        new Dictionary<string, int>();

    /// <summary>
    /// Composite spec level for weapon skills (e.g., "50+16" for base+items).
    /// </summary>
    public IReadOnlyDictionary<string, CompositeSkillLevel> EffectiveSkillLevels { get; init; } =
        new Dictionary<string, CompositeSkillLevel>();
}

public record CompositeSkillLevel(int BaseLevel, int ItemBonus, int RealmBonus)
{
    public int Total => BaseLevel + ItemBonus + RealmBonus;
    public override string ToString() => $"{BaseLevel}+{ItemBonus + RealmBonus}";
}
```

### StatBlock

Character base and derived statistics.

```csharp
/// <summary>
/// Character statistics including base stats, caps, and derived values.
/// </summary>
public record StatBlock
{
    // Base Attributes (10-100+ scale)
    public int Strength { get; init; }
    public int Constitution { get; init; }
    public int Dexterity { get; init; }
    public int Quickness { get; init; }
    public int Intelligence { get; init; }
    public int Piety { get; init; }
    public int Empathy { get; init; }
    public int Charisma { get; init; }

    // Stat Caps (typically 75 base + 26 item cap = 101)
    public int StrengthCap { get; init; } = 101;
    public int ConstitutionCap { get; init; } = 101;
    public int DexterityCap { get; init; } = 101;
    public int QuicknessCap { get; init; } = 101;

    // Derived Combat Stats
    public int HitPoints { get; init; }
    public int Power { get; init; }
    public int ArmorFactor { get; init; }
    public int AbsorptionPercent { get; init; }

    // Resistances (0-26% typical cap)
    public ResistanceBlock Resistances { get; init; } = new();

    // Melee Stats
    public int MeleeDamageBonus { get; init; }
    public int MeleeSpeedBonus { get; init; }
    public int StyleDamageBonus { get; init; }

    // Caster Stats
    public int SpellDamageBonus { get; init; }
    public int SpellDurationBonus { get; init; }
    public int SpellRangeBonus { get; init; }
    public int HealingBonus { get; init; }
    public int PowerPoolBonus { get; init; }

    // Utility Stats
    public int SpellPiercing { get; init; }
    public int DefensePenetration { get; init; }
}

public record ResistanceBlock
{
    public int Crush { get; init; }
    public int Slash { get; init; }
    public int Thrust { get; init; }
    public int Heat { get; init; }
    public int Cold { get; init; }
    public int Matter { get; init; }
    public int Body { get; init; }
    public int Spirit { get; init; }
    public int Energy { get; init; }
}
```

### RealmRankInfo

Tracks realm rank progression and ability points.

```csharp
/// <summary>
/// Realm rank information including earned and spent ability points.
/// </summary>
public record RealmRankInfo
{
    public int RealmRank { get; init; }           // 1-14 (RR1-RR14)
    public int RealmLevel { get; init; }          // 0-9 within each rank
    public long RealmPoints { get; init; }        // Total earned RP
    public long RealmPointsToNextLevel { get; init; }

    public int TotalRealmAbilityPoints { get; init; }
    public int SpentRealmAbilityPoints { get; init; }
    public int AvailableRealmAbilityPoints => TotalRealmAbilityPoints - SpentRealmAbilityPoints;

    /// <summary>
    /// Display string like "RR5L4" or "RR12L0"
    /// </summary>
    public string DisplayRank => $"RR{RealmRank}L{RealmLevel}";

    /// <summary>
    /// Numeric representation (e.g., RR5L4 = 5.4)
    /// </summary>
    public double NumericRank => RealmRank + (RealmLevel / 10.0);
}

public record EquippedRealmAbility
{
    public string AbilityName { get; init; } = string.Empty;
    public int Level { get; init; }              // 1-5 typically
    public int PointCost { get; init; }          // Cost at current level
    public int TotalPointsInvested { get; init; } // Cumulative cost
    public bool IsPassive { get; init; }
    public RealmAbilityCategory Category { get; init; }
}

public enum RealmAbilityCategory
{
    Offensive,
    Defensive,
    Utility,
    Passive,
    MasterLevel
}
```

### BuildPerformanceMetrics

Aggregated performance data for a build.

```csharp
/// <summary>
/// Aggregated combat performance metrics for a specific build.
/// </summary>
public record BuildPerformanceMetrics
{
    public Guid BuildId { get; init; }
    public int SessionCount { get; init; }
    public TimeSpan TotalCombatTime { get; init; }

    // Damage Metrics
    public double AverageDps { get; init; }
    public double MedianDps { get; init; }
    public double MaxDps { get; init; }
    public double DpsStandardDeviation { get; init; }
    public long TotalDamageDealt { get; init; }

    // Damage Taken Metrics
    public double AverageDamageTakenPerSecond { get; init; }
    public long TotalDamageTaken { get; init; }
    public double AverageMitigationPercent { get; init; }

    // Healing Metrics (if applicable)
    public double AverageHps { get; init; }
    public long TotalHealingDone { get; init; }

    // Survivability Metrics
    public double AverageDeathsPerHour { get; init; }
    public double KillDeathRatio { get; init; }
    public int TotalKills { get; init; }
    public int TotalDeaths { get; init; }

    // Style/Ability Effectiveness
    public IReadOnlyDictionary<string, StylePerformance> StyleBreakdown { get; init; } =
        new Dictionary<string, StylePerformance>();
    public IReadOnlyDictionary<string, double> RealmAbilityUsageRate { get; init; } =
        new Dictionary<string, double>();

    // Time-based tracking
    public DateTime FirstSessionUtc { get; init; }
    public DateTime LastSessionUtc { get; init; }
}

public record StylePerformance
{
    public string StyleName { get; init; } = string.Empty;
    public int UseCount { get; init; }
    public long TotalDamage { get; init; }
    public double AverageDamage { get; init; }
    public double HitRate { get; init; }
    public double CritRate { get; init; }
}
```

### SessionBuildLink

Associates combat sessions with builds.

```csharp
/// <summary>
/// Links a combat session to a specific character build for analysis.
/// </summary>
public record SessionBuildLink
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid SessionId { get; init; }
    public Guid BuildId { get; init; }
    public LinkSource Source { get; init; }
    public DateTime LinkedAtUtc { get; init; } = DateTime.UtcNow;
    public string? Notes { get; init; }
}

public enum LinkSource
{
    /// <summary>Automatically detected from session metadata.</summary>
    Automatic,
    /// <summary>Manually assigned by user.</summary>
    Manual,
    /// <summary>Inferred from realm rank changes.</summary>
    Inferred
}
```

### BuildComparison

Results of comparing two or more builds.

```csharp
/// <summary>
/// Comparison analysis between multiple builds.
/// </summary>
public record BuildComparison
{
    public IReadOnlyList<Guid> BuildIds { get; init; } = [];
    public IReadOnlyList<BuildComparisonEntry> Entries { get; init; } = [];
    public DateTime GeneratedAtUtc { get; init; } = DateTime.UtcNow;
    public ComparisonConfidence Confidence { get; init; }
}

public record BuildComparisonEntry
{
    public string MetricName { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty; // Damage, Survivability, etc.
    public IReadOnlyDictionary<Guid, double> ValuesByBuild { get; init; } =
        new Dictionary<Guid, double>();
    public Guid? BestBuildId { get; init; }
    public double MaxDifferencePercent { get; init; }
    public bool IsStatisticallySignificant { get; init; }
}

public record ComparisonConfidence
{
    public double OverallConfidence { get; init; }  // 0.0 - 1.0
    public int MinSessionsRecommended { get; init; }
    public string? ConfidenceWarning { get; init; }
}
```

---

## Service Layer

### ICharacterBuildService

Core build management operations.

```csharp
public interface ICharacterBuildService
{
    // CRUD Operations
    Task<CharacterBuild> CreateBuildAsync(CharacterBuild build);
    Task<CharacterBuild?> GetBuildAsync(Guid buildId);
    Task<IReadOnlyList<CharacterBuild>> GetBuildsForCharacterAsync(string characterName);
    Task<IReadOnlyList<CharacterBuild>> GetAllBuildsAsync();
    Task<CharacterBuild> UpdateBuildAsync(CharacterBuild build);
    Task<bool> DeleteBuildAsync(Guid buildId);

    // Build Operations
    Task<CharacterBuild> CloneBuildAsync(Guid buildId, string newName);
    Task<CharacterBuild> SetActiveBuildAsync(Guid buildId);
    Task<CharacterBuild?> GetActiveBuildForCharacterAsync(string characterName);

    // Import/Export
    Task<string> ExportBuildAsync(Guid buildId);
    Task<CharacterBuild> ImportBuildAsync(string jsonData);

    // Templates
    Task<IReadOnlyList<CharacterBuild>> GetTemplatesForClassAsync(CharacterClass characterClass);
}
```

### ISpecializationService

Specialization validation and calculation.

```csharp
public interface ISpecializationService
{
    // Skill Line Data
    IReadOnlyList<SkillLineDefinition> GetSkillLinesForClass(CharacterClass characterClass);
    int GetMaxSpecLevel(int characterLevel);
    int GetTotalSpecPoints(int characterLevel);

    // Validation
    SpecValidationResult ValidateSpecialization(SpecializationConfig config);
    bool CanTrainSkill(SpecializationConfig config, string skillLine, int targetLevel);

    // Calculations
    int CalculateSpecCost(string skillLine, int fromLevel, int toLevel);
    double CalculateWeaponSkillDamageModifier(int specLevel);
    double CalculateMagicSkillEffectiveness(int specLevel);
}

public record SkillLineDefinition
{
    public string Name { get; init; } = string.Empty;
    public SkillLineType Type { get; init; }
    public int MaxLevel { get; init; }
    public bool IsPrimary { get; init; }
    public IReadOnlyList<string> StylesGranted { get; init; } = [];
}

public enum SkillLineType
{
    Weapon,
    Magic,
    Hybrid,
    Utility
}
```

### IBuildPerformanceService

Performance tracking and aggregation.

```csharp
public interface IBuildPerformanceService
{
    // Session Linking
    Task<SessionBuildLink> LinkSessionToBuildAsync(Guid sessionId, Guid buildId,
        LinkSource source = LinkSource.Manual, string? notes = null);
    Task<bool> UnlinkSessionAsync(Guid sessionId);
    Task<Guid?> GetLinkedBuildAsync(Guid sessionId);
    Task<IReadOnlyList<Guid>> GetLinkedSessionsAsync(Guid buildId);

    // Automatic Detection
    Task<Guid?> DetectBuildForSessionAsync(Guid sessionId);
    Task<int> AutoLinkSessionsAsync(string characterName);

    // Performance Metrics
    Task<BuildPerformanceMetrics> CalculateMetricsAsync(Guid buildId);
    Task<BuildPerformanceMetrics> CalculateMetricsAsync(Guid buildId,
        DateTime fromUtc, DateTime toUtc);
    Task RefreshMetricsCacheAsync(Guid buildId);

    // Trend Analysis
    Task<PerformanceTrend> GetPerformanceTrendAsync(Guid buildId, int sessionCount = 20);
}

public record PerformanceTrend
{
    public Guid BuildId { get; init; }
    public TrendDirection DpsTrend { get; init; }
    public TrendDirection SurvivabilityTrend { get; init; }
    public double DpsChangePercent { get; init; }
    public double SurvivabilityChangePercent { get; init; }
    public IReadOnlyList<TrendDataPoint> DataPoints { get; init; } = [];
}

public enum TrendDirection { Improving, Declining, Stable, InsufficientData }

public record TrendDataPoint(DateTime Timestamp, double Dps, double DamageTaken, double Kdr);
```

### IBuildCorrelationService

Statistical analysis of build changes.

```csharp
public interface IBuildCorrelationService
{
    // Build Comparison
    Task<BuildComparison> CompareBuildPerformanceAsync(params Guid[] buildIds);
    Task<BuildComparison> CompareBuildPerformanceAsync(IEnumerable<Guid> buildIds,
        ComparisonOptions options);

    // Correlation Analysis
    Task<CorrelationResult> AnalyzeStatCorrelationAsync(Guid buildId, string statName);
    Task<IReadOnlyList<CorrelationResult>> FindSignificantCorrelationsAsync(Guid buildId);

    // Optimization Suggestions
    Task<IReadOnlyList<OptimizationSuggestion>> GetOptimizationSuggestionsAsync(Guid buildId);

    // What-If Analysis
    Task<PredictedPerformance> PredictPerformanceChangeAsync(Guid buildId,
        StatBlock proposedStats);
    Task<PredictedPerformance> PredictPerformanceChangeAsync(Guid buildId,
        SpecializationConfig proposedSpec);
}

public record CorrelationResult
{
    public string FactorName { get; init; } = string.Empty;
    public string MetricName { get; init; } = string.Empty;
    public double CorrelationCoefficient { get; init; }  // -1.0 to 1.0
    public double PValue { get; init; }
    public bool IsStatisticallySignificant { get; init; }
    public CorrelationDirection Direction { get; init; }
}

public enum CorrelationDirection { Positive, Negative, None }

public record OptimizationSuggestion
{
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public OptimizationCategory Category { get; init; }
    public double ConfidenceScore { get; init; }
    public double EstimatedImpactPercent { get; init; }
    public IReadOnlyDictionary<string, object> SuggestedChanges { get; init; } =
        new Dictionary<string, object>();
}

public enum OptimizationCategory
{
    SpecReallocation,
    StatOptimization,
    RealmAbilitySelection,
    PlaystyleAdjustment
}

public record PredictedPerformance
{
    public double PredictedDps { get; init; }
    public double DpsChangePercent { get; init; }
    public double ConfidenceInterval { get; init; }
    public string Explanation { get; init; } = string.Empty;
}
```

---

## User Interface

### Main Tab: Character Building

New tab added to MainWindow for build management.

```
┌─────────────────────────────────────────────────────────────────────┐
│ Character Building                                                   │
├─────────────────────────────────────────────────────────────────────┤
│ ┌─────────────────┐  ┌────────────────────────────────────────────┐ │
│ │ My Builds       │  │ Build Details: "RR10 Melee Cleric"         │ │
│ │ ───────────────│  │ ──────────────────────────────────────────│ │
│ │ ► Cleric        │  │                                            │ │
│ │   • RR10 Melee ◄│  │ Character: Healbot (Albion Cleric L50)     │ │
│ │   • RR5 Smite   │  │ Realm Rank: RR10L5 (1,245,000 RP)          │ │
│ │   • Template A  │  │                                            │ │
│ │                 │  │ [Specializations] [Stats] [RAs] [Sessions] │ │
│ │ ► Armsman       │  │ ┌──────────────────────────────────────┐   │ │
│ │   • RR8 Pole    │  │ │ Rejuvenation: 47 (+11) ████████████  │   │ │
│ │   • RR6 S&S     │  │ │ Enhancement:  35 (+8)  ████████░░░░  │   │ │
│ │                 │  │ │ Smite:        26       ██████░░░░░░  │   │ │
│ │ + New Build     │  │ │ Crush:        39 (+7)  █████████░░░  │   │ │
│ │                 │  │ │ Shield:       42 (+6)  ██████████░░  │   │ │
│ └─────────────────┘  │ └──────────────────────────────────────┘   │ │
│                      │                                            │ │
│                      │ Performance Summary (47 sessions)          │ │
│                      │ ─────────────────────────────────────────  │ │
│                      │ Avg DPS: 245.3  │  K/D: 2.4  │  Deaths/hr: │ │
│                      │ [Compare Builds]  [Link Sessions]  [Export]│ │
│                      └────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────────────┘
```

### Build Editor Dialog

Modal dialog for creating/editing builds.

```
┌─────────────────────────────────────────────────────────────────┐
│ Edit Build: RR10 Melee Cleric                            [X]    │
├─────────────────────────────────────────────────────────────────┤
│ Name: [RR10 Melee Cleric                    ]                   │
│ Description: [Frontline healing with melee capability    ]      │
│                                                                  │
│ ┌─ Character ────────────────────────────────────────────────┐  │
│ │ Class: [Cleric ▼]     Realm: [Albion ▼]    Level: [50]     │  │
│ │ Realm Rank: [10] L[5]    Total RP: [1,245,000]             │  │
│ └────────────────────────────────────────────────────────────┘  │
│                                                                  │
│ ┌─ Specializations (78/78 points) ───────────────────────────┐  │
│ │ Rejuvenation [47 ▼]  ████████████████████░░  Primary Heal  │  │
│ │ Enhancement  [35 ▼]  ██████████████░░░░░░░░  Buffs         │  │
│ │ Smite        [26 ▼]  ██████████░░░░░░░░░░░░  Damage        │  │
│ │ Crush        [39 ▼]  ███████████████░░░░░░░  Melee         │  │
│ │ Shield       [42 ▼]  ████████████████░░░░░░  Block/Styles  │  │
│ │                                                             │  │
│ │ Remaining Points: 0    [Reset] [Optimize]                  │  │
│ └────────────────────────────────────────────────────────────┘  │
│                                                                  │
│ ┌─ Realm Abilities (87/87 points) ───────────────────────────┐  │
│ │ [+] Mastery of Healing 5      (14 pts)  Passive            │  │
│ │ [+] Augmented Constitution 5  (10 pts)  Passive            │  │
│ │ [+] Purge 3                   (10 pts)  Active             │  │
│ │ [+] Divine Intervention 1     (14 pts)  Active             │  │
│ │ [+] Ignore Pain 3             (6 pts)   Active             │  │
│ │ ...                                                         │  │
│ │ Available: 0 points    [Add RA] [View All]                 │  │
│ └────────────────────────────────────────────────────────────┘  │
│                                                                  │
│                              [Cancel]  [Save Build]              │
└─────────────────────────────────────────────────────────────────┘
```

### Build Comparison View

Side-by-side performance comparison.

```
┌─────────────────────────────────────────────────────────────────────┐
│ Build Comparison                                                     │
├─────────────────────────────────────────────────────────────────────┤
│ Comparing: [RR10 Melee ▼] vs [RR5 Smite ▼]    [+ Add Build]         │
│                                                                      │
│ ┌─ Performance Metrics ─────────────────────────────────────────┐   │
│ │ Metric              │ RR10 Melee  │ RR5 Smite   │ Difference  │   │
│ │ ────────────────────┼─────────────┼─────────────┼─────────────│   │
│ │ Average DPS         │    245.3    │    312.7    │  +27.5% ▲   │   │
│ │ Average HPS         │    189.4    │     42.1    │  -77.8% ▼   │   │
│ │ Kill/Death Ratio    │      2.4    │      1.8    │  -25.0% ▼   │   │
│ │ Deaths per Hour     │      1.2    │      2.1    │  +75.0% ▼   │   │
│ │ Avg Dmg Mitigation  │     34.2%   │     28.1%   │   -6.1% ▼   │   │
│ │ Sessions Analyzed   │       47    │       23    │             │   │
│ └───────────────────────────────────────────────────────────────┘   │
│                                                                      │
│ ┌─ Insights ────────────────────────────────────────────────────┐   │
│ │ • RR5 Smite deals 27.5% more damage but dies 75% more often   │   │
│ │ • RR10 Melee provides significantly better group healing      │   │
│ │ • Statistical confidence: HIGH (70 combined sessions)         │   │
│ │ • Recommendation: Use RR10 Melee for group play, RR5 Smite   │   │
│ │   for solo/small-man where healing is less critical           │   │
│ └───────────────────────────────────────────────────────────────┘   │
│                                                                      │
│ [Export Comparison]  [View Trend Charts]  [Close]                    │
└─────────────────────────────────────────────────────────────────────┘
```

### Performance Dashboard

Detailed metrics visualization.

```
┌─────────────────────────────────────────────────────────────────────┐
│ Performance Dashboard: RR10 Melee Cleric                            │
├─────────────────────────────────────────────────────────────────────┤
│ Time Range: [Last 30 Days ▼]    Sessions: 47    Combat Time: 18.5h  │
│                                                                      │
│ ┌─ DPS Over Time ───────────────────────────────────────────────┐   │
│ │     ▲                                                          │   │
│ │ 300 │        ╭─╮    ╭──╮                                       │   │
│ │     │   ╭──╮ │ │╭──╮│  │ ╭─╮                                   │   │
│ │ 200 │ ╭─╯  ╰─╯ ╰╯  ╰╯  ╰─╯ ╰──╮                               │   │
│ │     │╭╯                       ╰──                              │   │
│ │ 100 │                              Trend: +5.2% ▲              │   │
│ │     └──────────────────────────────────────────────────▶       │   │
│ │       Dec 8    Dec 15    Dec 22    Dec 29    Jan 5             │   │
│ └───────────────────────────────────────────────────────────────┘   │
│                                                                      │
│ ┌─ Key Metrics ─────────┐  ┌─ Style Breakdown ──────────────────┐   │
│ │ DPS        245.3 ▲+5% │  │ Banish:     32% ████████           │   │
│ │ HPS        189.4 ▼-2% │  │ Smite:      28% ███████            │   │
│ │ K/D          2.4 ═    │  │ Crush:      24% ██████             │   │
│ │ Deaths/hr    1.2 ▲+8% │  │ Holy Strike: 16% ████              │   │
│ └───────────────────────┘  └────────────────────────────────────┘   │
│                                                                      │
│ ┌─ Correlations Found ──────────────────────────────────────────┐   │
│ │ • Higher Rejuv spec correlates with +12% survival (p<0.05)    │   │
│ │ • Shield skill shows strong correlation with K/D ratio        │   │
│ │ • Purge usage increases by 40% in 8-man groups                │   │
│ └───────────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────────┘
```

---

## Performance Correlation Engine

### Correlation Analysis Algorithm

```csharp
public class CorrelationEngine
{
    /// <summary>
    /// Analyzes correlations between build factors and performance metrics.
    /// Uses Pearson correlation coefficient with significance testing.
    /// </summary>
    public async Task<IReadOnlyList<CorrelationResult>> AnalyzeCorrelationsAsync(
        IReadOnlyList<BuildPerformanceSnapshot> snapshots)
    {
        var results = new List<CorrelationResult>();

        // Factors to analyze
        var factors = new[]
        {
            ("RealmRank", snapshots.Select(s => (double)s.Build.RealmRank.RealmRank)),
            ("PrimarySpecLevel", snapshots.Select(s => GetPrimarySpecLevel(s.Build))),
            ("TotalStatPoints", snapshots.Select(s => GetTotalStats(s.Build))),
            // ... more factors
        };

        // Metrics to correlate against
        var metrics = new[]
        {
            ("DPS", snapshots.Select(s => s.Metrics.AverageDps)),
            ("Survivability", snapshots.Select(s => 1.0 / s.Metrics.AverageDeathsPerHour)),
            ("KDR", snapshots.Select(s => s.Metrics.KillDeathRatio)),
            // ... more metrics
        };

        foreach (var (factorName, factorValues) in factors)
        {
            foreach (var (metricName, metricValues) in metrics)
            {
                var correlation = CalculatePearsonCorrelation(
                    factorValues.ToArray(),
                    metricValues.ToArray());

                var pValue = CalculatePValue(correlation, snapshots.Count);

                results.Add(new CorrelationResult
                {
                    FactorName = factorName,
                    MetricName = metricName,
                    CorrelationCoefficient = correlation,
                    PValue = pValue,
                    IsStatisticallySignificant = pValue < 0.05,
                    Direction = correlation > 0.1 ? CorrelationDirection.Positive
                              : correlation < -0.1 ? CorrelationDirection.Negative
                              : CorrelationDirection.None
                });
            }
        }

        return results.OrderByDescending(r => Math.Abs(r.CorrelationCoefficient)).ToList();
    }
}
```

### Build Change Detection

```csharp
public class BuildChangeDetector
{
    /// <summary>
    /// Detects when a player has changed their build based on combat patterns.
    /// </summary>
    public async Task<IReadOnlyList<DetectedBuildChange>> DetectBuildChangesAsync(
        string characterName,
        IReadOnlyList<CombatSession> sessions)
    {
        var changes = new List<DetectedBuildChange>();

        for (int i = 1; i < sessions.Count; i++)
        {
            var prev = sessions[i - 1];
            var curr = sessions[i];

            // Detect realm rank changes from RP gain events
            var rrChange = DetectRealmRankChange(prev, curr);

            // Detect spec changes from ability usage patterns
            var specChange = DetectSpecializationChange(prev, curr);

            // Detect RA changes from ability availability
            var raChange = DetectRealmAbilityChange(prev, curr);

            if (rrChange != null || specChange != null || raChange != null)
            {
                changes.Add(new DetectedBuildChange
                {
                    SessionBeforeId = prev.Id,
                    SessionAfterId = curr.Id,
                    DetectedAt = curr.StartTime,
                    RealmRankChange = rrChange,
                    SpecializationChange = specChange,
                    RealmAbilityChange = raChange,
                    Confidence = CalculateChangeConfidence(rrChange, specChange, raChange)
                });
            }
        }

        return changes;
    }
}
```

---

## Implementation Phases

### Phase 1: Foundation (v1.8.0)

**Goal**: Core build management and storage

| Task | Description | Estimate |
|------|-------------|----------|
| Data Models | Implement all core record types | Medium |
| Build Repository | JSON-based persistence layer | Medium |
| Build Service | CRUD operations for builds | Medium |
| Specialization Service | Spec validation and calculations | Large |
| Basic UI | Build list and editor views | Large |
| Unit Tests | 80%+ coverage on services | Medium |

**Deliverables**:
- Create, edit, delete character builds
- Configure specializations with validation
- Configure realm rank and abilities
- Persist builds to JSON storage
- Basic build list view in GUI

### Phase 2: Session Linking (v1.8.1)

**Goal**: Connect combat sessions to builds

| Task | Description | Estimate |
|------|-------------|----------|
| Session Link Model | SessionBuildLink implementation | Small |
| Manual Linking | UI for linking sessions to builds | Medium |
| Auto-Detection | Algorithm to suggest build matches | Large |
| Link Management UI | View/edit/remove session links | Medium |
| Bulk Operations | Link multiple sessions at once | Small |

**Deliverables**:
- Manually link any session to a build
- Auto-suggest builds for unlinked sessions
- View all sessions linked to a build
- Bulk link/unlink operations

### Phase 3: Performance Metrics (v1.8.2)

**Goal**: Calculate and display build performance

| Task | Description | Estimate |
|------|-------------|----------|
| Metrics Aggregation | BuildPerformanceMetrics calculation | Large |
| Performance Service | IBuildPerformanceService implementation | Medium |
| Metrics Caching | Cache computed metrics for speed | Medium |
| Performance Dashboard | Rich visualization of metrics | Large |
| Trend Analysis | Performance over time charts | Medium |

**Deliverables**:
- Aggregated metrics per build (DPS, HPS, K/D, etc.)
- Performance dashboard with visualizations
- Trend charts showing improvement over time
- Style/ability breakdown analysis

### Phase 4: Comparison & Correlation (v1.9.0)

**Goal**: Compare builds and find correlations

| Task | Description | Estimate |
|------|-------------|----------|
| Comparison Service | Multi-build comparison logic | Large |
| Correlation Engine | Statistical correlation analysis | Large |
| Comparison UI | Side-by-side comparison view | Medium |
| Insights Generation | Auto-generated comparison insights | Medium |
| Statistical Tests | Significance testing for differences | Medium |

**Deliverables**:
- Compare 2+ builds side-by-side
- Statistical significance indicators
- Auto-generated insights and recommendations
- Correlation coefficients for build factors

### Phase 5: Optimization & Advanced (v1.9.1+)

**Goal**: AI-driven suggestions and advanced features

| Task | Description | Estimate |
|------|-------------|----------|
| Optimization Suggestions | Rule-based recommendations | Large |
| What-If Analysis | Predict impact of changes | Large |
| Build Templates | Pre-built configurations | Medium |
| Import/Export | JSON/clipboard sharing | Small |
| Community Integration | Optional build sharing | Large |

**Deliverables**:
- Optimization suggestions based on data
- "What if I changed X?" predictions
- Template library for common builds
- Import/export for build sharing

---

## Testing Strategy

### Unit Tests

```csharp
public class CharacterBuildServiceTests
{
    [Fact]
    public async Task CreateBuild_WithValidData_ReturnsSavedBuild()
    {
        // Arrange
        var repository = new InMemoryBuildRepository();
        var service = new CharacterBuildService(repository);
        var build = CreateTestBuild();

        // Act
        var result = await service.CreateBuildAsync(build);

        // Assert
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal(build.Name, result.Name);
    }

    [Theory]
    [InlineData(CharacterClass.Cleric, 50, 78)]  // Level 50 = 78 spec points
    [InlineData(CharacterClass.Cleric, 40, 58)]  // Level 40 = 58 spec points
    public void CalculateTotalSpecPoints_ReturnsCorrectValue(
        CharacterClass cls, int level, int expected)
    {
        var service = new SpecializationService();
        var result = service.GetTotalSpecPoints(level);
        Assert.Equal(expected, result);
    }
}

public class BuildCorrelationServiceTests
{
    [Fact]
    public async Task AnalyzeCorrelation_WithSufficientData_FindsSignificantCorrelations()
    {
        // Arrange
        var service = new BuildCorrelationService();
        var snapshots = GenerateTestSnapshots(count: 50);

        // Act
        var correlations = await service.FindSignificantCorrelationsAsync(
            snapshots.First().Build.Id);

        // Assert
        Assert.NotEmpty(correlations);
        Assert.All(correlations, c => Assert.True(c.IsStatisticallySignificant));
    }
}
```

### Integration Tests

```csharp
public class BuildPerformanceIntegrationTests
{
    [Fact]
    public async Task FullWorkflow_CreateBuildLinkSessionsAnalyze_ProducesMetrics()
    {
        // Arrange
        var buildService = new CharacterBuildService(new JsonBuildRepository(testPath));
        var performanceService = new BuildPerformanceService(buildService, sessionRepo);

        // Create build
        var build = await buildService.CreateBuildAsync(CreateTestBuild());

        // Parse and link sessions
        var sessions = await ParseTestLogFile("test-combat.log");
        foreach (var session in sessions)
        {
            await performanceService.LinkSessionToBuildAsync(session.Id, build.Id);
        }

        // Act
        var metrics = await performanceService.CalculateMetricsAsync(build.Id);

        // Assert
        Assert.True(metrics.SessionCount > 0);
        Assert.True(metrics.AverageDps > 0);
        Assert.True(metrics.TotalDamageDealt > 0);
    }
}
```

### UI Tests

```csharp
public class BuildEditorViewModelTests
{
    [Fact]
    public void AddSpecPoints_ExceedingLimit_ShowsValidationError()
    {
        // Arrange
        var vm = new BuildEditorViewModel(mockBuildService, mockSpecService);
        vm.TotalSpecPoints = 78;
        vm.SkillAllocations["Rejuvenation"] = 50;
        vm.SkillAllocations["Enhancement"] = 28;

        // Act
        vm.SkillAllocations["Smite"] = 10; // Would exceed 78

        // Assert
        Assert.True(vm.HasValidationErrors);
        Assert.Contains("exceeds available", vm.ValidationMessage);
    }
}
```

---

## Future Enhancements

### v2.0.0+ Potential Features

| Feature | Description | Complexity |
|---------|-------------|------------|
| **Live Build Sync** | Real-time updates from game memory | Very High |
| **Community Builds** | Shared build repository with ratings | High |
| **ML Optimization** | Machine learning for build recommendations | Very High |
| **Gear Integration** | Track equipped items and bonuses | High |
| **Template Marketplace** | User-submitted build templates | Medium |
| **Mobile Companion** | View builds and stats on mobile | High |
| **Overlay Integration** | Show build info in-game overlay | High |
| **API Exposure** | REST API for build data | Medium |

### Integration Opportunities

- **Cross-Realm Leaderboards**: Compare builds across the player base
- **Guild Build Sharing**: Share optimized builds within guilds
- **Streaming Integration**: Display build info on Twitch/YouTube
- **Discord Bot**: Query build stats via Discord commands

---

## Appendix A: Realm-Specific Skill Lines

### Albion Classes

| Class | Primary Skills | Secondary Skills |
|-------|---------------|------------------|
| Armsman | Polearm, Crush, Slash, Thrust | Shield, Parry, Crossbow |
| Cleric | Rejuvenation, Enhancement, Smite | Crush, Shield |
| Friar | Staff, Rejuvenation, Enhancement | Parry |
| Heretic | Crush, Flexible, Rejuvenation | Shield |
| ... | ... | ... |

### Midgard Classes

| Class | Primary Skills | Secondary Skills |
|-------|---------------|------------------|
| Warrior | Sword, Axe, Hammer | Shield, Parry |
| Healer | Mending, Augmentation, Pacification | Crush |
| Shaman | Mending, Augmentation, Subterranean | Crush |
| ... | ... | ... |

### Hibernia Classes

| Class | Primary Skills | Secondary Skills |
|-------|---------------|------------------|
| Hero | Large Weapons, Blades, Blunt, Piercing | Shield, Parry |
| Druid | Nature, Regrowth, Nurture | Blunt |
| Warden | Regrowth, Nurture, Blades | Shield, Parry |
| ... | ... | ... |

---

## Appendix B: Realm Ability Point Costs

| Level | Cost | Cumulative |
|-------|------|------------|
| 1 | 1 | 1 |
| 2 | 2 | 3 |
| 3 | 3 | 6 |
| 4 | 5 | 11 |
| 5 | 8 | 19 |
| Master | 14 | 33 |

---

## Appendix C: Stat Cap Reference

| Stat Type | Base Cap | Item Cap | Total Cap |
|-----------|----------|----------|-----------|
| Primary Stats | 75 | 101 | 101 |
| Resistances | 0% | 26% | 26% |
| Melee Damage | 0% | 10% | 10% |
| Spell Damage | 0% | 10% | 10% |
| Healing Bonus | 0% | 25% | 25% |

---

## Summary

The Character Building Tools feature will provide DAoC players with comprehensive tools to:

1. **Document** their character builds with full specialization and stat details
2. **Link** combat performance data to specific build configurations
3. **Analyze** how build changes affect combat effectiveness
4. **Compare** different builds to identify optimal configurations
5. **Optimize** based on statistical analysis and correlation insights

This feature aligns with the Combat Reporter's mission of providing actionable insights from combat data, extending the analysis from "what happened" to "why it happened" based on character configuration.

**Estimated Timeline**:
- Phase 1-2 (v1.8.x): Foundation and session linking
- Phase 3-4 (v1.9.x): Performance metrics and correlation
- Phase 5 (v1.9.1+): Optimization and advanced features

**Dependencies**:
- Existing `CharacterInfo` and `ExtendedCombatStatistics` models
- Cross-realm persistence infrastructure
- Session tracking and comparison services
