# v1.4.0 Group Analysis Implementation Plan

## Overview

Implement comprehensive group composition analysis for Dark Age of Camelot combat logs. This feature will detect group members, classify roles, analyze team performance, and provide composition recommendations.

---

## Design Decisions (Based on User Input)

1. **Group Detection**: Combined approach - inference from combat events + manual configuration
2. **Role System**: Dual-role (primary + secondary) with 7 role types
3. **Group Sizes**: Standard DAoC categories (Solo, Small-man, 8-man, Battlegroup)

---

## Phase 1: Core Models & Enums

### Files to Create

**`src/CamelotCombatReporter.Core/GroupAnalysis/Models/GroupEnums.cs`**
```csharp
namespace CamelotCombatReporter.Core.GroupAnalysis.Models;

public enum GroupRole
{
    Unknown = 0,
    Tank = 1,
    Healer = 2,
    CrowdControl = 3,
    MeleeDps = 4,
    CasterDps = 5,
    Support = 6,
    Hybrid = 7
}

public enum GroupSizeCategory
{
    Solo = 1,        // 1 player
    SmallMan = 2,    // 2-4 players
    EightMan = 3,    // 5-8 players
    Battlegroup = 4  // 9+ players
}

public enum GroupMemberSource
{
    Inferred = 1,    // Detected from combat events
    Manual = 2,      // User-configured
    Both = 3         // Inferred and confirmed manually
}
```

**`src/CamelotCombatReporter.Core/GroupAnalysis/Models/GroupModels.cs`**
```csharp
namespace CamelotCombatReporter.Core.GroupAnalysis.Models;

public record GroupMember(
    string Name,
    CharacterClass? Class,
    Realm? Realm,
    GroupRole PrimaryRole,
    GroupRole? SecondaryRole,
    GroupMemberSource Source,
    TimeOnly FirstSeen,
    TimeOnly? LastSeen,
    bool IsPlayer  // true if "You"
);

public record GroupComposition(
    Guid Id,
    IReadOnlyList<GroupMember> Members,
    GroupSizeCategory SizeCategory,
    GroupTemplate? MatchedTemplate,
    double BalanceScore,
    TimeOnly FormationTime,
    TimeOnly? DisbandTime
);

public record GroupTemplate(
    string Name,
    string Description,
    IReadOnlyDictionary<GroupRole, RoleRequirement> RoleRequirements,
    int MinSize,
    int MaxSize
);

public record RoleRequirement(
    int MinCount,
    int MaxCount,
    bool IsRequired
);

public record GroupPerformanceMetrics(
    Guid CompositionId,
    double TotalDps,
    double TotalHps,
    int TotalKills,
    int TotalDeaths,
    double KillDeathRatio,
    double AverageMemberDps,
    double AverageMemberHps,
    TimeSpan CombatDuration,
    IReadOnlyDictionary<string, MemberContribution> MemberContributions
);

public record MemberContribution(
    string MemberName,
    GroupRole Role,
    int DamageDealt,
    int HealingDone,
    int Kills,
    int Deaths,
    double DpsContributionPercent,
    double HpsContributionPercent
);

public record RoleCoverage(
    GroupRole Role,
    int MemberCount,
    bool IsCovered,
    bool IsOverRepresented,
    string[] MemberNames
);

public record CompositionRecommendation(
    RecommendationType Type,
    GroupRole? TargetRole,
    string Message,
    RecommendationPriority Priority
);

public enum RecommendationType
{
    AddRole,
    RemoveRole,
    RebalanceRoles,
    TemplateMatch,
    SynergyImprovement
}

public enum RecommendationPriority
{
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4
}
```

---

## Phase 2: Role Classification Service

### Files to Create

**`src/CamelotCombatReporter.Core/GroupAnalysis/RoleClassificationService.cs`**

Role mapping database for all 48 classes:

| Realm | Class | Primary Role | Secondary Role |
|-------|-------|--------------|----------------|
| **Albion** | Armsman | Tank | MeleeDps |
| | Paladin | Tank | Healer |
| | Mercenary | MeleeDps | Tank |
| | Reaver | Hybrid | Tank |
| | Cleric | Healer | Support |
| | Friar | Healer | MeleeDps |
| | Heretic | Hybrid | CasterDps |
| | Infiltrator | MeleeDps | — |
| | Scout | MeleeDps | — |
| | Minstrel | Support | MeleeDps |
| | Cabalist | CasterDps | CrowdControl |
| | Necromancer | CasterDps | — |
| | Sorcerer | CrowdControl | CasterDps |
| | Theurgist | CasterDps | Support |
| | Wizard | CasterDps | — |
| | MaulerAlb | Hybrid | MeleeDps |
| **Midgard** | Warrior | Tank | MeleeDps |
| | Thane | Tank | CasterDps |
| | Berserker | MeleeDps | Tank |
| | Savage | MeleeDps | — |
| | Valkyrie | Hybrid | Healer |
| | Healer | Healer | CrowdControl |
| | Shaman | Healer | Support |
| | Skald | Support | MeleeDps |
| | Hunter | MeleeDps | — |
| | Shadowblade | MeleeDps | — |
| | Runemaster | CrowdControl | CasterDps |
| | Spiritmaster | CasterDps | CrowdControl |
| | Bonedancer | CasterDps | — |
| | Warlock | CasterDps | Support |
| | MaulerMid | Hybrid | MeleeDps |
| **Hibernia** | Hero | Tank | MeleeDps |
| | Champion | Tank | Support |
| | Blademaster | MeleeDps | Tank |
| | Valewalker | Hybrid | MeleeDps |
| | Vampiir | Hybrid | MeleeDps |
| | Warden | Healer | Tank |
| | Druid | Healer | Support |
| | Bard | Support | Healer |
| | Ranger | MeleeDps | — |
| | Nightshade | MeleeDps | — |
| | Mentalist | CrowdControl | CasterDps |
| | Enchanter | Support | CrowdControl |
| | Eldritch | CasterDps | — |
| | Animist | CasterDps | Support |
| | Bainshee | CasterDps | — |
| | MaulerHib | Hybrid | MeleeDps |

Service methods:
- `GetPrimaryRole(CharacterClass class)` → GroupRole
- `GetSecondaryRole(CharacterClass class)` → GroupRole?
- `GetRolesForClass(CharacterClass class)` → (GroupRole primary, GroupRole? secondary)
- `GetClassesForRole(GroupRole role, bool includePrimary, bool includeSecondary)` → IEnumerable<CharacterClass>

---

## Phase 3: Group Detection Service

### Files to Create

**`src/CamelotCombatReporter.Core/GroupAnalysis/IGroupDetectionService.cs`**
**`src/CamelotCombatReporter.Core/GroupAnalysis/GroupDetectionService.cs`**

Detection strategies:
1. **Healing Received** - Entities that heal "You" are likely group members
2. **Shared Targets** - Entities attacking the same targets in proximity
3. **Buff Sources** - Entities providing buffs to "You"
4. **Death Proximity** - Deaths occurring near each other in time
5. **Manual Override** - User-added names

Interface:
```csharp
public interface IGroupDetectionService
{
    TimeSpan ProximityWindow { get; set; }  // Default: 10 seconds
    int MinInteractions { get; set; }       // Default: 3 interactions to infer

    IReadOnlyList<GroupMember> DetectGroupMembers(IEnumerable<LogEvent> events);
    GroupComposition BuildComposition(IReadOnlyList<GroupMember> members, TimeOnly timestamp);

    void AddManualMember(string name, CharacterClass? @class);
    void RemoveManualMember(string name);
    IReadOnlyList<string> GetManualMembers();

    void Reset();
}
```

Detection algorithm:
1. Scan healing events where Source != "You" and Target == "You"
2. Scan buff events applied to "You" by others
3. Track damage dealt to same targets within ProximityWindow
4. Score each potential member by interaction count
5. Filter to members with MinInteractions or more
6. Merge with manual member list
7. Attempt class detection from ability names or spell patterns

---

## Phase 4: Group Analysis Service

### Files to Create

**`src/CamelotCombatReporter.Core/GroupAnalysis/IGroupAnalysisService.cs`**
**`src/CamelotCombatReporter.Core/GroupAnalysis/GroupAnalysisService.cs`**

Interface:
```csharp
public interface IGroupAnalysisService
{
    // Analysis
    GroupComposition AnalyzeComposition(IEnumerable<LogEvent> events);
    GroupPerformanceMetrics CalculateMetrics(GroupComposition composition, IEnumerable<LogEvent> events);
    IReadOnlyList<RoleCoverage> AnalyzeRoleCoverage(GroupComposition composition);

    // Scoring
    double CalculateBalanceScore(GroupComposition composition);
    GroupTemplate? MatchTemplate(GroupComposition composition);

    // Recommendations
    IReadOnlyList<CompositionRecommendation> GenerateRecommendations(GroupComposition composition);

    // Templates
    IReadOnlyList<GroupTemplate> GetAvailableTemplates();
}
```

Built-in templates:
- **8-Man RvR**: 2 Tank, 2 Healer, 2 CC, 2 DPS (5-8 players)
- **Small-Man**: 1 Tank, 1 Healer, 2+ DPS (2-4 players)
- **Zerg Support**: 3+ Healer, 2+ Support (6-8 players)
- **Gank Group**: 4+ MeleeDps, 0-1 Healer (3-5 players)
- **Keep Defense**: 2+ CC, 2+ Healer, 2+ Tank (6-8 players)

Balance scoring algorithm:
- Base score: 100
- Missing healer: -30
- Missing tank: -20
- No DPS: -25
- No CC: -10
- Over 50% one role: -15
- Matches template: +10
- Has support: +5

---

## Phase 5: GUI Implementation

### Files to Create

**`src/CamelotCombatReporter.Gui/GroupAnalysis/ViewModels/GroupAnalysisViewModel.cs`**

Observable properties:
- `GroupMembers` - ObservableCollection<GroupMemberViewModel>
- `CurrentComposition` - GroupCompositionViewModel
- `PerformanceMetrics` - GroupPerformanceMetricsViewModel
- `RoleCoverage` - ObservableCollection<RoleCoverageViewModel>
- `Recommendations` - ObservableCollection<RecommendationViewModel>
- `RoleDistributionSeries` - ISeries[] (Pie chart)
- `ContributionSeries` - ISeries[] (Bar chart)
- `HasData` - bool
- `BalanceScore` - double
- `MatchedTemplate` - string

Commands:
- `AnalyzeFromFileCommand` - Load and analyze log file
- `AddManualMemberCommand` - Add member manually
- `RemoveManualMemberCommand` - Remove selected member
- `RefreshAnalysisCommand` - Re-analyze with current settings

**`src/CamelotCombatReporter.Gui/GroupAnalysis/Views/GroupAnalysisView.axaml`**

Layout sections:
1. **Header** - Title + Analyze button
2. **Group Overview Panel**
   - Size category badge
   - Balance score gauge (0-100)
   - Matched template indicator
3. **Members List**
   - DataGrid with Name, Class, Primary Role, Secondary Role, Source
   - Add/Remove buttons for manual members
4. **Role Coverage Panel**
   - Horizontal bars showing role distribution
   - Coverage indicators (✓ Covered, ⚠ Missing, ⬆ Over)
5. **Performance Metrics Panel**
   - Total DPS/HPS cards
   - K/D ratio
   - Combat duration
6. **Contribution Chart**
   - Stacked bar chart showing member contributions
7. **Recommendations Panel**
   - Priority-sorted list of suggestions
   - Color-coded by priority

**`src/CamelotCombatReporter.Gui/GroupAnalysis/Views/GroupAnalysisView.axaml.cs`**
- Standard code-behind with InitializeComponent()

---

## Phase 6: MainWindow Integration

### Files to Modify

**`src/CamelotCombatReporter.Gui/Views/MainWindow.axaml`**
- Add xmlns for GroupAnalysis namespace
- Add new TabItem for Group Analysis tab

---

## Phase 7: Unit Tests

### Files to Create

**`tests/CamelotCombatReporter.Core.Tests/GroupAnalysisTests.cs`**

Test cases:
1. RoleClassificationService
   - All classes have primary role assigned
   - Secondary roles are correct for hybrid classes
   - GetClassesForRole returns correct classes

2. GroupDetectionService
   - Detects healer from healing events
   - Detects group from shared target attacks
   - Manual members are included
   - Respects MinInteractions threshold

3. GroupAnalysisService
   - Balance score calculation is correct
   - Template matching works for 8-man
   - Recommendations generated for missing roles
   - Role coverage analysis is accurate

4. Integration tests
   - Full pipeline from events to analysis
   - GUI ViewModel updates correctly

---

## Implementation Order

| Phase | Components | Estimated Files |
|-------|------------|-----------------|
| 1 | Models & Enums | 2 |
| 2 | RoleClassificationService | 1 |
| 3 | GroupDetectionService (interface + impl) | 2 |
| 4 | GroupAnalysisService (interface + impl) | 2 |
| 5 | GUI (ViewModel + View) | 3 |
| 6 | MainWindow integration | 1 (modify) |
| 7 | Unit tests | 1 |

**Total: ~12 new files + 1 modification**

---

## Dependencies

- Existing: `CharacterClass`, `Realm`, `ClassArchetype` enums from GameEnums.cs
- Existing: `CharacterInfo` model for character context
- Existing: `LogParser` and all `LogEvent` types
- New: LiveCharts2 for visualizations (already in project)
- New: CommunityToolkit.Mvvm for MVVM patterns (already in project)

---

## Success Criteria

1. Group members detected from healing/combat patterns
2. Manual member configuration working
3. Role classification accurate for all 48 classes
4. Balance score reflects composition quality
5. Template matching identifies common setups
6. Recommendations help improve composition
7. All visualizations render correctly
8. Unit tests pass (target: 15+ tests)
9. Integration with MainWindow complete
