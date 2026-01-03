# Combat Style Optimizer Plugin

## Plugin Type: Data Analysis

## Overview

Analyze combat style usage patterns, chain effectiveness, and provide recommendations for optimal style rotations. This plugin helps melee players understand which style chains work best and identifies missed opportunities for higher damage.

## Problem Statement

Melee combat in DAoC relies heavily on combat style chains for maximum damage. Players often:
- Miss chain opportunities by using the wrong opener
- Use suboptimal style sequences
- Don't realize which chains are most effective for their class
- Lack visibility into style hit rates and damage contribution

## Features

### Style Usage Statistics
- Count of each style used per session
- Hit rate per style (success vs. miss/evade/block/parry)
- Average damage per style
- Total damage contribution percentage

### Chain Analysis
- Detect successful style chains (opener → follow-up)
- Track chain completion rates
- Calculate damage bonus from chains vs. unchained styles
- Identify broken chains (follow-up without opener)

### Recommendations Engine
- Suggest optimal openers based on hit rate
- Recommend chain sequences with highest average damage
- Alert on underutilized high-damage chains
- Class-specific style recommendations

### Insights
- "Your Garrote → Achilles Heel chain averages 450 damage vs. 280 for unchained"
- "You missed 23 chain opportunities by using Hamstring after evade instead of Riposte"
- "Leaper has a 45% miss rate - consider Dragonfang as your opener"

## Technical Specification

### Plugin Manifest

```json
{
  "id": "combat-style-optimizer",
  "name": "Combat Style Optimizer",
  "version": "1.0.0",
  "author": "CCR Community",
  "description": "Analyzes combat style chains and recommends optimal rotations",
  "type": "DataAnalysis",
  "entryPoint": {
    "assembly": "CombatStyleOptimizer.dll",
    "typeName": "CombatStyleOptimizer.StyleOptimizerPlugin"
  },
  "compatibility": {
    "minAppVersion": "1.0.0"
  },
  "permissions": [
    "CombatDataAccess",
    "SettingsRead",
    "SettingsWrite"
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
| `style-count` | Style Usage Count | Dictionary | Count per style name |
| `style-hit-rate` | Style Hit Rate | Dictionary | Hit percentage per style |
| `style-avg-damage` | Average Damage | Dictionary | Average damage per style |
| `chain-completion-rate` | Chain Completion | double | Percentage of chains completed |
| `chain-damage-bonus` | Chain Damage Bonus | double | Extra damage from chaining |
| `optimal-opener` | Recommended Opener | string | Best opener based on data |

### Data Structures

```csharp
public record StyleUsage(
    string StyleName,
    int UseCount,
    int HitCount,
    int MissCount,
    int TotalDamage,
    double AverageDamage,
    double HitRate
);

public record ChainSequence(
    string OpenerStyle,
    string FollowUpStyle,
    int CompletionCount,
    int MissedCount,
    double AverageChainDamage,
    double AverageUnchainedDamage
);

public record StyleRecommendation(
    string CurrentStyle,
    string RecommendedStyle,
    string Reason,
    double ExpectedImprovement
);
```

### Implementation Outline

```csharp
public class StyleOptimizerPlugin : DataAnalysisPluginBase
{
    private readonly Dictionary<string, StyleUsage> _styleStats = new();
    private readonly Dictionary<string, ChainSequence> _chainStats = new();

    public override Task<AnalysisResult> AnalyzeAsync(
        IReadOnlyList<LogEvent> events,
        CombatStatistics? baseStatistics,
        AnalysisOptions options,
        CancellationToken ct = default)
    {
        // Reset stats
        _styleStats.Clear();
        _chainStats.Clear();

        // Get combat style events
        var styleEvents = events
            .OfType<CombatStyleEvent>()
            .Where(e => e.Source == options.CombatantName)
            .ToList();

        // Correlate with damage events
        var damageEvents = events.OfType<DamageEvent>().ToList();

        // Analyze each style
        foreach (var style in styleEvents)
        {
            AnalyzeStyleUsage(style, damageEvents);
        }

        // Detect chains
        DetectChains(styleEvents, damageEvents);

        // Generate recommendations
        var recommendations = GenerateRecommendations();

        return Task.FromResult(Success(BuildStatistics(), recommendations));
    }

    private void DetectChains(
        List<CombatStyleEvent> styles,
        List<DamageEvent> damages)
    {
        // Look for styles used within 3 seconds of each other
        for (int i = 1; i < styles.Count; i++)
        {
            var prev = styles[i - 1];
            var curr = styles[i];

            var timeDiff = CalculateTimeDiff(prev.Timestamp, curr.Timestamp);
            if (timeDiff.TotalSeconds <= 3)
            {
                // Potential chain
                RecordChain(prev.StyleName, curr.StyleName, damages);
            }
        }
    }

    private List<AnalysisInsight> GenerateRecommendations()
    {
        var insights = new List<AnalysisInsight>();

        // Find low hit-rate styles
        foreach (var style in _styleStats.Values.Where(s => s.HitRate < 0.5))
        {
            insights.Add(Insight(
                $"Low Hit Rate: {style.StyleName}",
                $"{style.StyleName} has only {style.HitRate:P0} hit rate. " +
                "Consider using a more reliable style as opener.",
                InsightSeverity.Suggestion
            ));
        }

        // Find missed chain opportunities
        foreach (var chain in _chainStats.Values.Where(c => c.MissedCount > 5))
        {
            insights.Add(Insight(
                $"Missed Chain Opportunities",
                $"You missed {chain.MissedCount} opportunities to follow " +
                $"{chain.OpenerStyle} with {chain.FollowUpStyle}",
                InsightSeverity.Info
            ));
        }

        return insights;
    }
}
```

## Class-Specific Data

The plugin should include style chain data for each melee class:

### Example: Mercenary Chains
| Opener | Follow-Up | Bonus |
|--------|-----------|-------|
| Garrote | Achilles Heel | +damage |
| Hamstring | Garrote | +damage |
| Dual Wield → any evade style | Riposte | +damage |

### Example: Shadowblade Chains
| Opener | Follow-Up | Bonus |
|--------|-----------|-------|
| Backstab opener | Perforate Artery | +damage |
| Evade style | Lunge | +damage |

## User Interface

The plugin could optionally provide a UI component showing:
- Style usage pie chart
- Chain success rate bar chart
- Recommendations panel

## Dependencies

- Core combat parsing (CombatStyleEvent support)
- Optional: Class database for chain recommendations

## Complexity

**Medium** - Requires understanding of style timing and chain mechanics, but uses standard analysis plugin patterns.

## Future Enhancements

- [ ] Import style databases per class
- [ ] Real-time chain tracking with overlay
- [ ] Style macro suggestions
- [ ] Compare to "optimal" rotation benchmarks
