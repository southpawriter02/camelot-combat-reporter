# Heal Efficiency Tracker Plugin

## Plugin Type: Data Analysis

## Overview

Comprehensive healing analysis for healers, tracking effective healing, overhealing, heal targets, reaction times, and providing insights for improving healing performance in group content.

## Problem Statement

Healers want to understand:
- How much of their healing was actually effective (not overhealing)?
- Who are they healing the most/least?
- How quickly do they react to damage?
- Are they using the right heal for the situation?
- How does their HPS compare to damage intake?

## Features

### Healing Metrics
- Total healing done
- Effective healing (excluding overheal)
- Overheal amount and percentage
- HPS (Heals per Second)
- Effective HPS

### Target Analysis
- Healing per target breakdown
- Target priority analysis
- Under-healed targets identification
- Tank vs. DPS healing ratio

### Spell Analysis
- Healing by spell type
- Spell efficiency (effective vs. total)
- Optimal spell selection feedback
- Mana efficiency tracking

### Reaction Time
- Time between damage and heal
- Average reaction to critical health
- Preemptive vs. reactive healing ratio

## Technical Specification

### Plugin Manifest

```json
{
  "id": "heal-efficiency-tracker",
  "name": "Heal Efficiency Tracker",
  "version": "1.0.0",
  "author": "CCR Community",
  "description": "Comprehensive healing analysis for healers",
  "type": "DataAnalysis",
  "entryPoint": {
    "assembly": "HealEfficiencyTracker.dll",
    "typeName": "HealEfficiencyTracker.HealTrackerPlugin"
  },
  "compatibility": {
    "minAppVersion": "1.0.0"
  },
  "permissions": [
    "CombatDataAccess"
  ],
  "resources": {
    "maxMemoryMb": 64,
    "maxCpuTimeSeconds": 30
  }
}
```

### Provided Statistics

| Statistic ID | Name | Type | Description |
|--------------|------|------|-------------|
| `total-healing` | Total Healing | int | Raw healing amount |
| `effective-healing` | Effective Healing | int | Healing that wasn't overheal |
| `overheal-amount` | Overheal Amount | int | Healing lost to full health |
| `overheal-percent` | Overheal % | double | Percentage overhealed |
| `hps` | HPS | double | Raw heals per second |
| `effective-hps` | Effective HPS | double | Effective heals per second |
| `avg-reaction-time` | Avg Reaction | TimeSpan | Average time to heal after damage |
| `heal-efficiency` | Heal Efficiency | double | Overall efficiency score |

### Data Structures

```csharp
public record HealingAnalysis(
    int TotalHealing,
    int EffectiveHealing,
    int OverhealAmount,
    double OverhealPercent,
    double Hps,
    double EffectiveHps,
    TimeSpan AverageReactionTime,
    double EfficiencyScore,
    IReadOnlyList<TargetHealingStats> ByTarget,
    IReadOnlyList<SpellHealingStats> BySpell,
    IReadOnlyList<HealingInsight> Insights
);

public record TargetHealingStats(
    string TargetName,
    int TotalHealing,
    int EffectiveHealing,
    int OverhealAmount,
    double OverhealPercent,
    int HealCount,
    double AverageHeal,
    TimeSpan AverageReactionTime
);

public record SpellHealingStats(
    string SpellName,
    int TotalHealing,
    int EffectiveHealing,
    double OverhealPercent,
    int CastCount,
    double AverageHeal,
    double ManaEfficiency  // Healing per mana if trackable
);

public record HealingInsight(
    InsightType Type,
    string Title,
    string Description,
    InsightSeverity Severity
);

public enum InsightType
{
    OverhealWarning,
    TargetNeglect,
    SpellChoice,
    ReactionTime,
    Efficiency,
    Positive
}
```

### Implementation Outline

```csharp
public class HealTrackerPlugin : DataAnalysisPluginBase
{
    public override Task<AnalysisResult> AnalyzeAsync(
        IReadOnlyList<LogEvent> events,
        CombatStatistics? baseStatistics,
        AnalysisOptions options,
        CancellationToken ct = default)
    {
        var healEvents = GetHealingDone(events, options.CombatantName).ToList();

        if (!healEvents.Any())
        {
            return Task.FromResult(Empty());
        }

        var damageEvents = events.OfType<DamageEvent>().ToList();

        // Calculate basic metrics
        var totalHealing = healEvents.Sum(h => h.HealingAmount);
        var effectiveHealing = CalculateEffectiveHealing(healEvents, damageEvents);
        var overheal = totalHealing - effectiveHealing;
        var overhealPercent = (double)overheal / totalHealing * 100;

        // Calculate HPS
        var duration = CalculateDuration(events);
        var hps = totalHealing / Math.Max(1, duration.TotalSeconds);
        var effectiveHps = effectiveHealing / Math.Max(1, duration.TotalSeconds);

        // Analyze by target
        var byTarget = AnalyzeByTarget(healEvents, damageEvents);

        // Analyze by spell
        var bySpell = AnalyzeBySpell(healEvents);

        // Calculate reaction times
        var reactionTime = CalculateAverageReactionTime(healEvents, damageEvents);

        // Generate insights
        var insights = GenerateInsights(
            overhealPercent,
            byTarget,
            bySpell,
            reactionTime
        );

        // Calculate overall efficiency score
        var efficiency = CalculateEfficiencyScore(
            overhealPercent,
            reactionTime,
            byTarget
        );

        return Task.FromResult(Success(new Dictionary<string, object>
        {
            ["total-healing"] = totalHealing,
            ["effective-healing"] = effectiveHealing,
            ["overheal-amount"] = overheal,
            ["overheal-percent"] = Math.Round(overhealPercent, 1),
            ["hps"] = Math.Round(hps, 1),
            ["effective-hps"] = Math.Round(effectiveHps, 1),
            ["avg-reaction-time"] = reactionTime,
            ["heal-efficiency"] = Math.Round(efficiency, 1),
            ["by-target"] = byTarget,
            ["by-spell"] = bySpell
        }, insights));
    }

    private int CalculateEffectiveHealing(
        List<HealingEvent> heals,
        List<DamageEvent> damage)
    {
        // For accurate calculation, we'd need to know target health
        // This is an estimation based on damage taken correlation

        var effectiveHealing = 0;

        foreach (var heal in heals)
        {
            // Find damage to this target in the window before the heal
            var windowStart = heal.Timestamp.Add(TimeSpan.FromSeconds(-5));
            var recentDamage = damage
                .Where(d => d.Target == heal.Target &&
                            d.Timestamp >= windowStart &&
                            d.Timestamp <= heal.Timestamp)
                .Sum(d => d.DamageAmount);

            // Effective healing is min of heal amount and recent damage
            var effective = Math.Min(heal.HealingAmount, recentDamage);
            effectiveHealing += effective;
        }

        return effectiveHealing;
    }

    private TimeSpan CalculateAverageReactionTime(
        List<HealingEvent> heals,
        List<DamageEvent> damage)
    {
        var reactionTimes = new List<TimeSpan>();

        foreach (var heal in heals)
        {
            // Find the most recent damage to this target before the heal
            var lastDamage = damage
                .Where(d => d.Target == heal.Target && d.Timestamp < heal.Timestamp)
                .OrderByDescending(d => d.Timestamp)
                .FirstOrDefault();

            if (lastDamage != null)
            {
                var reaction = heal.Timestamp.ToTimeSpan() -
                    lastDamage.Timestamp.ToTimeSpan();

                if (reaction.TotalSeconds <= 10) // Reasonable window
                {
                    reactionTimes.Add(reaction);
                }
            }
        }

        if (!reactionTimes.Any())
            return TimeSpan.Zero;

        var avgTicks = reactionTimes.Average(t => t.Ticks);
        return TimeSpan.FromTicks((long)avgTicks);
    }

    private List<TargetHealingStats> AnalyzeByTarget(
        List<HealingEvent> heals,
        List<DamageEvent> damage)
    {
        return heals
            .GroupBy(h => h.Target)
            .Select(g =>
            {
                var targetHeals = g.ToList();
                var total = targetHeals.Sum(h => h.HealingAmount);
                var effective = CalculateEffectiveHealingForTarget(
                    targetHeals,
                    damage.Where(d => d.Target == g.Key).ToList()
                );
                var overheal = total - effective;

                return new TargetHealingStats(
                    g.Key,
                    total,
                    effective,
                    overheal,
                    total > 0 ? (double)overheal / total * 100 : 0,
                    targetHeals.Count,
                    targetHeals.Average(h => h.HealingAmount),
                    CalculateReactionTimeForTarget(targetHeals, damage)
                );
            })
            .OrderByDescending(t => t.TotalHealing)
            .ToList();
    }

    private List<HealingInsight> GenerateInsights(
        double overhealPercent,
        List<TargetHealingStats> byTarget,
        List<SpellHealingStats> bySpell,
        TimeSpan reactionTime)
    {
        var insights = new List<HealingInsight>();

        // Overheal warning
        if (overhealPercent > 40)
        {
            insights.Add(new HealingInsight(
                InsightType.OverhealWarning,
                "High Overhealing",
                $"{overhealPercent:F0}% of your healing was overheal. " +
                "Consider using smaller heals or waiting longer between casts.",
                InsightSeverity.Warning
            ));
        }
        else if (overhealPercent < 15)
        {
            insights.Add(new HealingInsight(
                InsightType.Positive,
                "Excellent Efficiency",
                $"Only {overhealPercent:F0}% overheal - great target selection!",
                InsightSeverity.Info
            ));
        }

        // Target neglect warning
        var totalHealing = byTarget.Sum(t => t.TotalHealing);
        var neglectedTargets = byTarget
            .Where(t => t.TotalHealing < totalHealing * 0.05 &&
                        t.OverhealPercent < 10) // Low healing AND not overhealing
            .ToList();

        if (neglectedTargets.Any())
        {
            insights.Add(new HealingInsight(
                InsightType.TargetNeglect,
                "Under-Healed Targets",
                $"{string.Join(", ", neglectedTargets.Select(t => t.TargetName))} " +
                "received very little healing. Ensure they're getting adequate support.",
                InsightSeverity.Suggestion
            ));
        }

        // Reaction time feedback
        if (reactionTime.TotalSeconds > 3)
        {
            insights.Add(new HealingInsight(
                InsightType.ReactionTime,
                "Slow Reaction Time",
                $"Average {reactionTime.TotalSeconds:F1}s between damage and heal. " +
                "Try to anticipate damage or use faster heals.",
                InsightSeverity.Suggestion
            ));
        }
        else if (reactionTime.TotalSeconds < 1.5)
        {
            insights.Add(new HealingInsight(
                InsightType.Positive,
                "Quick Reactions",
                $"Average {reactionTime.TotalSeconds:F1}s reaction time - excellent awareness!",
                InsightSeverity.Info
            ));
        }

        return insights;
    }

    private double CalculateEfficiencyScore(
        double overhealPercent,
        TimeSpan reactionTime,
        List<TargetHealingStats> byTarget)
    {
        // Composite score from 0-100
        var overhealScore = Math.Max(0, 100 - overhealPercent * 2);
        var reactionScore = Math.Max(0, 100 - reactionTime.TotalSeconds * 20);
        var coverageScore = byTarget.Count > 1 ? 100 : 50; // Bonus for healing multiple targets

        return (overhealScore * 0.5) + (reactionScore * 0.3) + (coverageScore * 0.2);
    }
}
```

## Output Summary

```
HEALING EFFICIENCY REPORT
=========================

Total Healing:     48,320
Effective Healing: 32,450 (67.2%)
Overhealing:       15,870 (32.8%)

HPS:               142.3
Effective HPS:     95.5

Avg Reaction Time: 1.8s
Efficiency Score:  78.5 / 100

BY TARGET
─────────────────────────────────────────────
Target          | Heals  | Effective | Overheal
Warrior1        | 24,500 | 18,200    | 25.7%
Caster2         | 12,400 | 8,900     | 28.2%
Support3        | 11,420 | 5,350     | 53.1%

BY SPELL
─────────────────────────────────────────────
Spell           | Total  | Effective | Casts
Greater Heal    | 28,000 | 19,500    | 42
Minor Heal      | 12,320 | 8,950     | 89
Group Heal      | 8,000  | 4,000     | 12

INSIGHTS
• ⚠️ High Overhealing: 32.8% - consider timing heals better
• ✓ Quick Reactions: 1.8s average - great awareness!
• 💡 Support3 has 53% overheal - they may have self-healing
```

## Dependencies

- Core healing event parsing
- Damage event parsing (for reaction time)
- Optional: Target health tracking

## Complexity

**Medium** - Requires correlation between damage and healing events, overheal calculation is approximate without actual health data.

## Future Enhancements

- [ ] Per-encounter breakdown (boss fights)
- [ ] Heal priority recommendations
- [ ] Mana efficiency tracking (if parseable)
- [ ] Comparison with other healers
- [ ] Integration with group analyzer
- [ ] Heal timing visualization
