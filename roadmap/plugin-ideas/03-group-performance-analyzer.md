# Group Performance Analyzer Plugin

## Plugin Type: Data Analysis

## Overview

Analyze individual player contributions within a group context, measuring not just raw damage/healing but relative performance, coordination, and role fulfillment. Helps raid leaders and group members understand who's carrying their weight.

## Problem Statement

In group RvR or PvE, players need to understand:
- How does my performance compare to others in the group?
- Who is contributing the most to kills?
- Are healers keeping up with damage?
- Who died first/most often?
- Is the group composition working effectively?

## Features

### Individual Contribution Metrics
- Damage dealt as percentage of group total
- Healing done as percentage of total damage taken
- Kill participation (assists + kills)
- Death count and survival rate
- Crowd control contribution

### Role Performance
- Tank: Damage taken, threat generation, survival
- Healer: Effective healing, overhealing, reaction time
- DPS: Damage output, burst windows, target switching
- Support: Buff uptime, CC duration, utility usage

### Comparative Analysis
- Performance percentile within role
- Above/below group average indicators
- Trend over session (improving/declining)
- Highlight standout performances

### Group Synergy
- Kill correlation (who kills together)
- Heal targeting analysis
- CC chain participation
- Damage spikes during coordinated bursts

## Technical Specification

### Plugin Manifest

```json
{
  "id": "group-performance-analyzer",
  "name": "Group Performance Analyzer",
  "version": "1.0.0",
  "author": "CCR Community",
  "description": "Analyzes individual contributions within group combat",
  "type": "DataAnalysis",
  "entryPoint": {
    "assembly": "GroupPerformanceAnalyzer.dll",
    "typeName": "GroupPerformanceAnalyzer.GroupAnalyzerPlugin"
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
| `group-members` | Group Members | List | Detected group member names |
| `damage-share` | Damage Share | Dictionary | Percentage of group damage per member |
| `healing-share` | Healing Share | Dictionary | Percentage of group healing per member |
| `kill-participation` | Kill Participation | Dictionary | Kills + assists per member |
| `survival-rate` | Survival Rate | Dictionary | Percentage of fight time alive |
| `mvp-score` | MVP Score | Dictionary | Composite performance score |
| `group-dps` | Group DPS | double | Combined group damage per second |

### Data Structures

```csharp
public record GroupMemberStats(
    string Name,
    int DamageDealt,
    int DamageTaken,
    int HealingDone,
    int HealingReceived,
    int Kills,
    int Assists,
    int Deaths,
    TimeSpan TimeAlive,
    TimeSpan TotalCombatTime,
    double DamageShare,
    double HealingShare,
    double SurvivalRate,
    double MvpScore
);

public record GroupSummary(
    int TotalDamage,
    int TotalHealing,
    int TotalKills,
    int TotalDeaths,
    TimeSpan CombatDuration,
    double GroupDps,
    double GroupHps,
    IReadOnlyList<GroupMemberStats> Members
);

public record RolePerformance(
    string MemberName,
    GroupRole Role,
    double RoleScore,
    IReadOnlyList<string> Strengths,
    IReadOnlyList<string> Improvements
);
```

### Implementation Outline

```csharp
public class GroupAnalyzerPlugin : DataAnalysisPluginBase
{
    public override Task<AnalysisResult> AnalyzeAsync(
        IReadOnlyList<LogEvent> events,
        CombatStatistics? baseStatistics,
        AnalysisOptions options,
        CancellationToken ct = default)
    {
        // Step 1: Identify group members
        var members = IdentifyGroupMembers(events);

        if (members.Count < 2)
        {
            return Task.FromResult(Empty()); // Not enough for group analysis
        }

        // Step 2: Calculate per-member stats
        var memberStats = new Dictionary<string, GroupMemberStats>();
        foreach (var member in members)
        {
            memberStats[member] = CalculateMemberStats(member, events);
        }

        // Step 3: Calculate group totals
        var groupSummary = CalculateGroupSummary(memberStats.Values.ToList());

        // Step 4: Calculate relative metrics
        foreach (var member in memberStats.Keys.ToList())
        {
            memberStats[member] = EnrichWithRelativeMetrics(
                memberStats[member],
                groupSummary
            );
        }

        // Step 5: Generate insights
        var insights = GenerateInsights(memberStats.Values.ToList(), groupSummary);

        return Task.FromResult(Success(
            BuildStatistics(groupSummary, memberStats),
            insights
        ));
    }

    private HashSet<string> IdentifyGroupMembers(IReadOnlyList<LogEvent> events)
    {
        // Heuristics for group detection:
        // 1. Healing targets (if you heal them, they're in group)
        // 2. Damage to same targets within short window
        // 3. Buff sharing patterns

        var members = new HashSet<string>();

        // Find healing targets
        var healTargets = events
            .OfType<HealingEvent>()
            .Select(h => h.Target)
            .Distinct();

        members.UnionWith(healTargets);

        // Find coordinated attackers
        var attackWindows = events
            .OfType<DamageEvent>()
            .GroupBy(d => new {
                Target = d.Target,
                Window = (int)(d.Timestamp.ToTimeSpan().TotalSeconds / 3)
            })
            .Where(g => g.Select(e => e.Source).Distinct().Count() > 1);

        foreach (var window in attackWindows)
        {
            members.UnionWith(window.Select(e => e.Source));
        }

        return members;
    }

    private double CalculateMvpScore(GroupMemberStats stats, GroupSummary group)
    {
        // Weighted composite score
        var damageScore = stats.DamageShare * 30;
        var healingScore = stats.HealingShare * 30;
        var survivalScore = stats.SurvivalRate * 20;
        var killScore = (stats.Kills + stats.Assists * 0.5) /
            Math.Max(1, group.TotalKills) * 20;

        return damageScore + healingScore + survivalScore + killScore;
    }

    private List<AnalysisInsight> GenerateInsights(
        List<GroupMemberStats> members,
        GroupSummary group)
    {
        var insights = new List<AnalysisInsight>();

        // Top performer
        var mvp = members.OrderByDescending(m => m.MvpScore).First();
        insights.Add(Insight(
            "MVP",
            $"{mvp.Name} had the highest contribution with {mvp.DamageShare:P0} " +
            $"of damage and {mvp.Kills} kills",
            InsightSeverity.Info
        ));

        // Underperformer warning
        var avgDamageShare = 1.0 / members.Count;
        var underperformers = members
            .Where(m => m.DamageShare < avgDamageShare * 0.5)
            .ToList();

        if (underperformers.Any())
        {
            insights.Add(Insight(
                "Low Contribution",
                $"{string.Join(", ", underperformers.Select(u => u.Name))} " +
                "contributed less than half the average damage",
                InsightSeverity.Warning
            ));
        }

        // Survival issues
        var deaths = members.Where(m => m.Deaths > 2).ToList();
        if (deaths.Any())
        {
            insights.Add(Insight(
                "High Deaths",
                $"{string.Join(", ", deaths.Select(d => d.Name))} died frequently. " +
                "Consider defensive adjustments.",
                InsightSeverity.Suggestion
            ));
        }

        return insights;
    }
}
```

## Output Format

The plugin produces a summary like:

```
GROUP PERFORMANCE SUMMARY
========================
Duration: 15m 32s | Total Damage: 145,320 | Group DPS: 156

Member         | Damage   | Share  | Healing | Kills | Deaths | MVP Score
---------------|----------|--------|---------|-------|--------|----------
Warrior1       | 52,400   | 36.1%  | 0       | 12    | 1      | 84.2
Caster2        | 38,200   | 26.3%  | 0       | 8     | 2      | 68.5
Healer3        | 4,800    | 3.3%   | 48,500  | 2     | 0      | 72.1
Support4       | 49,920   | 34.4%  | 12,300  | 9     | 3      | 71.8

INSIGHTS:
• MVP: Warrior1 led the group with highest damage and 12 kills
• Healer3 provided strong healing support with 100% survival
• Support4 died 3 times - consider better positioning
```

## Configuration Options

- Minimum group size for analysis (default: 2)
- Include self in group (default: true)
- Weight adjustments for MVP calculation
- Role detection mode (auto, manual assignment)

## Dependencies

- Core combat parsing
- Healing event support
- Kill/death detection

## Complexity

**Medium-High** - Group member detection requires heuristics that may not be 100% accurate without explicit group data in logs.

## Future Enhancements

- [ ] Manual group member input
- [ ] Role assignment override
- [ ] Historical group performance tracking
- [ ] Integration with voice chat activity
- [ ] Comparison across multiple sessions
- [ ] Export group report as image for sharing
