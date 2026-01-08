# 14. Death Analysis and Prevention

## Status: ✅ Complete (v1.1.0)

**Implementation Complete:**
- ✅ Log parsing infrastructure
- ✅ Timeline view
- ✅ Death event detection
- ✅ Pre-death state reconstruction

---

## Description

Analyze deaths in detail to understand what went wrong and how to prevent future deaths. Reconstruct the seconds before death, identify killing blows, missed defensive opportunities, and provide actionable feedback for survival improvement.

## Functionality

### Core Features

* **Death Detection:**
  * Parse death messages from combat logs
  * Capture killing blow information
  * Record timestamp and location context
  * Track death frequency and patterns

* **Pre-Death Analysis:**
  * Reconstruct last 10-30 seconds before death
  * Track incoming damage by source
  * Monitor health trajectory
  * Identify burst damage windows

* **Killing Blow Breakdown:**
  * Final damage source and amount
  * Ability/attack that killed you
  * Killer's class and name
  * Overkill damage calculation

### Death Report Sections

| Section | Content |
|---------|---------|
| **Summary** | When, where, who killed you, total damage taken |
| **Damage Timeline** | Second-by-second breakdown of incoming damage |
| **Top Damagers** | Ranked list of damage sources |
| **Killing Blow** | Final hit details with overkill |
| **Missed Opportunities** | Defensive abilities available but not used |
| **Recommendations** | AI-generated survival tips |

### Analysis Metrics

* **Time-to-Death (TTD):**
  * How long from first hit to death
  * TTD trends over sessions
  * Compare to class averages

* **Damage Composition:**
  * Physical vs. magical damage
  * Direct vs. DoT damage
  * Single source vs. multi-target

* **Survival Windows:**
  * Points where death was preventable
  * Healing deficit calculations
  * CC break opportunities

### Death Categories

```
Burst Death (< 3 seconds)
├── Alpha Strike - Single massive hit
├── Coordinated Burst - Multiple attackers simultaneously
└── CC Chain Death - Killed while crowd controlled

Attrition Death (> 10 seconds)
├── Healing Deficit - Damage exceeded healing capacity
├── Resource Exhaustion - Ran out of abilities/mana
└── Positional Failure - Poor positioning led to death

Execution Death
├── Low Health Finish - Killed at low HP by execute
├── DoT Finish - Killed by damage over time
└── Environmental - Fall damage, hazards
```

### Death Prevention Tips

* **Class-Specific Advice:**
  * Defensive cooldown recommendations
  * Optimal ability usage patterns
  * Positioning guidelines

* **Matchup Analysis:**
  * Common killer classes
  * Counter-strategies per class
  * Ability timing suggestions

* **Situational Awareness:**
  * When to disengage
  * Group positioning tips
  * Escape route suggestions

### Death Statistics

* **Death Frequency:**
  * Deaths per hour
  * Deaths per combat encounter
  * Session death trends

* **Death Causes:**
  * Top killing classes
  * Most deadly abilities
  * Common death scenarios

* **Improvement Tracking:**
  * Survival rate changes
  * Average TTD improvements
  * Defensive cooldown usage

## Requirements

* **Death Parsing:** Detect all death-related log messages
* **Event Correlation:** Link death to preceding events
* **Analysis Engine:** Calculate metrics and generate tips
* **UI:** Death report view with timeline

## Limitations

* Cannot detect deaths not logged
* Pre-death events limited by log detail
* AI recommendations may not apply to all situations
* Requires sufficient death data for patterns

## Dependencies

* **01-log-parsing.md:** Core event parsing
* **04-timeline-view.md:** Timeline visualization
* **11-combat-replay.md:** Event reconstruction
* **15-buff-debuff-tracking.md:** Defensive buff status

## Implementation Phases

### Phase 1: Death Detection
- [ ] Create DeathEvent model
- [ ] Parse death messages from logs
- [ ] Capture killing blow information
- [ ] Build death event storage

### Phase 2: Pre-Death Analysis
- [ ] Implement event windowing (before death)
- [ ] Calculate damage timeline
- [ ] Identify damage sources
- [ ] Build health trajectory model

### Phase 3: Death Reports
- [ ] Design death report UI
- [ ] Create damage breakdown charts
- [ ] Implement killing blow display
- [ ] Add timeline visualization

### Phase 4: Recommendations Engine
- [ ] Build missed opportunity detector
- [ ] Create class-specific advice database
- [ ] Implement pattern recognition
- [ ] Generate actionable tips

## Technical Notes

### Data Structures

```csharp
public record DeathEvent(
    DateTime Timestamp,
    string VictimName,
    CharacterClass? VictimClass,
    DeathCause Cause,
    KillingBlow? KillingBlow,
    IReadOnlyList<DamageSource> DamageSources,
    TimeSpan TimeToDeath,
    int TotalDamageTaken,
    DeathCategory Category
);

public record KillingBlow(
    string AttackerName,
    CharacterClass? AttackerClass,
    string AbilityName,
    int DamageAmount,
    int OverkillAmount,
    DamageType DamageType
);

public record DamageSource(
    string AttackerName,
    CharacterClass? AttackerClass,
    int TotalDamage,
    double PercentOfTotal,
    IReadOnlyList<DamageEvent> Events
);

public enum DeathCategory
{
    BurstAlphaStrike,
    BurstCoordinated,
    BurstCCChain,
    AttritionHealingDeficit,
    AttritionResourceExhaustion,
    AttritionPositional,
    ExecutionLowHealth,
    ExecutionDoT,
    Environmental
}
```

### Death Report Generator

```csharp
public class DeathAnalyzer
{
    private readonly TimeSpan _preDeathWindow = TimeSpan.FromSeconds(30);

    public DeathReport Analyze(DeathEvent death, IEnumerable<LogEvent> allEvents)
    {
        var preDeathEvents = GetPreDeathEvents(death, allEvents);
        var damageTimeline = BuildDamageTimeline(preDeathEvents);
        var damageSources = CalculateDamageSources(preDeathEvents);
        var missedOpportunities = FindMissedOpportunities(preDeathEvents, death);
        var recommendations = GenerateRecommendations(death, missedOpportunities);

        return new DeathReport(
            death,
            damageTimeline,
            damageSources,
            missedOpportunities,
            recommendations,
            CalculateCategory(death, damageTimeline)
        );
    }

    private IReadOnlyList<LogEvent> GetPreDeathEvents(
        DeathEvent death,
        IEnumerable<LogEvent> allEvents)
    {
        var windowStart = death.Timestamp - _preDeathWindow;
        return allEvents
            .Where(e => e.Timestamp >= windowStart && e.Timestamp <= death.Timestamp)
            .OrderBy(e => e.Timestamp)
            .ToList();
    }

    private DamageTimeline BuildDamageTimeline(IReadOnlyList<LogEvent> events)
    {
        var buckets = new Dictionary<int, int>(); // second offset -> damage

        foreach (var evt in events.OfType<DamageEvent>())
        {
            var offset = (int)(evt.Timestamp - events.First().Timestamp).TotalSeconds;
            buckets.TryGetValue(offset, out var current);
            buckets[offset] = current + evt.DamageAmount;
        }

        return new DamageTimeline(buckets);
    }

    private DeathCategory CalculateCategory(
        DeathEvent death,
        DamageTimeline timeline)
    {
        if (death.TimeToDeath < TimeSpan.FromSeconds(3))
        {
            if (death.DamageSources.Count == 1)
                return DeathCategory.BurstAlphaStrike;
            return DeathCategory.BurstCoordinated;
        }

        if (death.TimeToDeath > TimeSpan.FromSeconds(10))
        {
            return DeathCategory.AttritionHealingDeficit;
        }

        return DeathCategory.BurstCoordinated;
    }
}
```

### Missed Opportunity Detection

```csharp
public class MissedOpportunityDetector
{
    private readonly Dictionary<CharacterClass, List<DefensiveAbility>> _defensives;

    public IReadOnlyList<MissedOpportunity> FindMissedOpportunities(
        IReadOnlyList<LogEvent> preDeathEvents,
        DeathEvent death,
        CharacterClass victimClass)
    {
        var opportunities = new List<MissedOpportunity>();

        if (!_defensives.TryGetValue(victimClass, out var classDefensives))
            return opportunities;

        var usedAbilities = preDeathEvents
            .OfType<AbilityEvent>()
            .Select(a => a.AbilityName)
            .ToHashSet();

        foreach (var defensive in classDefensives)
        {
            if (!usedAbilities.Contains(defensive.Name))
            {
                var optimalUseTime = FindOptimalUseTime(preDeathEvents, defensive);
                opportunities.Add(new MissedOpportunity(
                    defensive.Name,
                    defensive.Description,
                    optimalUseTime,
                    defensive.ExpectedBenefit
                ));
            }
        }

        return opportunities;
    }

    private DateTime? FindOptimalUseTime(
        IReadOnlyList<LogEvent> events,
        DefensiveAbility defensive)
    {
        // Find the moment of highest incoming damage
        var burstWindow = events
            .OfType<DamageEvent>()
            .GroupBy(e => (int)(e.Timestamp - events.First().Timestamp).TotalSeconds)
            .OrderByDescending(g => g.Sum(e => e.DamageAmount))
            .FirstOrDefault();

        return burstWindow?.First().Timestamp;
    }
}

public record MissedOpportunity(
    string AbilityName,
    string Description,
    DateTime? OptimalUseTime,
    string ExpectedBenefit
);

public record DefensiveAbility(
    string Name,
    string Description,
    TimeSpan Cooldown,
    string ExpectedBenefit
);
```

### Recommendation Engine

```csharp
public class DeathRecommendationEngine
{
    public IReadOnlyList<Recommendation> GenerateRecommendations(
        DeathReport report)
    {
        var recommendations = new List<Recommendation>();

        // Missed defensive recommendations
        foreach (var missed in report.MissedOpportunities)
        {
            recommendations.Add(new Recommendation(
                RecommendationType.AbilityUsage,
                $"Consider using {missed.AbilityName} when taking heavy damage",
                missed.Description,
                RecommendationPriority.High
            ));
        }

        // Category-specific recommendations
        switch (report.Category)
        {
            case DeathCategory.BurstAlphaStrike:
                recommendations.Add(new Recommendation(
                    RecommendationType.Positioning,
                    "Stay aware of stealthers and high-burst classes",
                    "You died to a single large hit. Watch for assassination classes.",
                    RecommendationPriority.Medium
                ));
                break;

            case DeathCategory.BurstCoordinated:
                recommendations.Add(new Recommendation(
                    RecommendationType.Awareness,
                    "Watch for coordinated enemy attacks",
                    "Multiple enemies focused you simultaneously. Stay with your group.",
                    RecommendationPriority.High
                ));
                break;

            case DeathCategory.AttritionHealingDeficit:
                recommendations.Add(new Recommendation(
                    RecommendationType.Disengagement,
                    "Consider disengaging when healing can't keep up",
                    "The fight lasted too long without sufficient healing. Know when to retreat.",
                    RecommendationPriority.Medium
                ));
                break;
        }

        // Killer class-specific recommendations
        if (report.Death.KillingBlow?.AttackerClass != null)
        {
            var classAdvice = GetClassCounterAdvice(report.Death.KillingBlow.AttackerClass.Value);
            recommendations.AddRange(classAdvice);
        }

        return recommendations.OrderByDescending(r => r.Priority).ToList();
    }
}

public record Recommendation(
    RecommendationType Type,
    string Title,
    string Description,
    RecommendationPriority Priority
);

public enum RecommendationType
{
    AbilityUsage,
    Positioning,
    Awareness,
    Disengagement,
    ClassCounter
}

public enum RecommendationPriority
{
    Low,
    Medium,
    High,
    Critical
}
```

### Death Statistics Tracker

```csharp
public class DeathStatisticsService
{
    private readonly List<DeathEvent> _deaths = new();

    public void RecordDeath(DeathEvent death) => _deaths.Add(death);

    public DeathStatistics GetStatistics(DateTime? since = null)
    {
        var deaths = since.HasValue
            ? _deaths.Where(d => d.Timestamp >= since.Value)
            : _deaths;

        return new DeathStatistics(
            TotalDeaths: deaths.Count(),
            DeathsPerHour: CalculateDeathsPerHour(deaths),
            AverageTimeToDeath: TimeSpan.FromSeconds(
                deaths.Average(d => d.TimeToDeath.TotalSeconds)),
            TopKillerClasses: GetTopKillerClasses(deaths),
            TopKillingAbilities: GetTopKillingAbilities(deaths),
            DeathsByCategory: deaths.GroupBy(d => d.Category)
                .ToDictionary(g => g.Key, g => g.Count())
        );
    }
}
```
