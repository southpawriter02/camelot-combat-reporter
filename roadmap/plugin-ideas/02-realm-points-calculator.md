# Realm Points Calculator Plugin

## Plugin Type: Data Analysis + UI Component

## Overview

Calculate realm point (RP) gains from combat logs, track progression toward realm ranks, and project time to reach realm rank milestones. Essential for RvR-focused players tracking their advancement.

## Problem Statement

Players engaged in RvR want to:
- Know exactly how many RPs they earned in a session
- Understand their RP/hour rate
- Project how long until they reach the next realm rank
- Compare RP efficiency across different play styles
- Track lifetime RP progression

## Features

### Realm Points Calculation
- Parse kill messages to calculate RP earned
- Apply realm rank and group modifiers
- Track RP from different sources (kills, keep takes, tasks)
- Session totals and historical tracking

### Rate Analysis
- RP per hour calculation
- RP per kill average
- Peak earning periods identification
- Efficiency by activity type (solo, group, zerg)

### Rank Progression
- Current realm rank and points
- Progress to next rank (percentage)
- Time-to-rank projection based on current rate
- Rank milestones and dates achieved

### Goal Setting
- Set target realm rank
- Track progress toward goal
- Estimated completion date
- Required RP/hour to meet deadlines

## Technical Specification

### Plugin Manifest

```json
{
  "id": "realm-points-calculator",
  "name": "Realm Points Calculator",
  "version": "1.0.0",
  "author": "CCR Community",
  "description": "Calculates RP gains, tracks realm rank progression, and projects advancement",
  "type": "DataAnalysis",
  "entryPoint": {
    "assembly": "RealmPointsCalculator.dll",
    "typeName": "RealmPointsCalculator.RpCalculatorPlugin"
  },
  "compatibility": {
    "minAppVersion": "1.0.0"
  },
  "permissions": [
    "CombatDataAccess",
    "SettingsRead",
    "SettingsWrite",
    "FileRead",
    "FileWrite"
  ],
  "resources": {
    "maxMemoryMb": 32,
    "maxCpuTimeSeconds": 15
  }
}
```

### Provided Statistics

| Statistic ID | Name | Type | Description |
|--------------|------|------|-------------|
| `session-rp` | Session RP | int | Total RP earned this session |
| `rp-per-hour` | RP/Hour | double | Earning rate |
| `rp-per-kill` | RP/Kill | double | Average RP per kill |
| `kill-count` | Kill Count | int | Enemy players killed |
| `current-rr` | Current RR | string | Realm rank (e.g., "5L4") |
| `rp-to-next` | RP to Next Rank | int | Points needed for next rank |
| `time-to-next` | Time to Next Rank | TimeSpan | Projected time at current rate |

### Realm Rank Data

```csharp
public static class RealmRankData
{
    // Cumulative RP required for each realm rank
    public static readonly Dictionary<int, long> RpForRank = new()
    {
        [1] = 0,           // RR1L0
        [2] = 25,          // RR1L1
        [3] = 125,         // RR1L2
        [4] = 350,         // RR1L3
        // ... continues to RR14
        [100] = 9_111_713,  // RR10L0
        [121] = 66_181_501, // RR12L1
        [141] = 513_549_165 // RR14L1
    };

    public static (int Rank, int Level) GetRealmRank(long totalRp)
    {
        // Calculate RR from total RP
        // Returns tuple like (5, 4) for RR5L4
    }

    public static long GetRpForRank(int rank, int level)
    {
        // Get cumulative RP needed for specific rank
    }

    public static string FormatRealmRank(int rank, int level)
    {
        // Format as "RR5L4" or "5L4"
    }
}
```

### Data Structures

```csharp
public record RealmPointsSession(
    DateTime SessionStart,
    DateTime SessionEnd,
    int TotalRpEarned,
    int KillCount,
    int DeathCount,
    double RpPerHour,
    double RpPerKill,
    IReadOnlyList<RpEvent> Events
);

public record RpEvent(
    DateTime Timestamp,
    RpEventType Type,
    int RpAmount,
    string? TargetName,
    string? TargetRank
);

public enum RpEventType
{
    SoloKill,
    GroupKill,
    KeepCapture,
    TowerCapture,
    RelicCapture,
    Task,
    Other
}

public record RankProgression(
    int CurrentRank,
    int CurrentLevel,
    long TotalRp,
    long RpToNextLevel,
    double ProgressPercent,
    TimeSpan? EstimatedTimeToNext
);
```

### Implementation Outline

```csharp
public class RpCalculatorPlugin : DataAnalysisPluginBase
{
    private List<RpEvent> _sessionEvents = new();
    private long _startingRp;

    public override async Task InitializeAsync(
        IPluginContext context,
        CancellationToken ct = default)
    {
        // Load saved state (current RP, history)
        var prefs = context.GetService<IPreferencesAccess>();
        if (prefs != null)
        {
            _startingRp = await prefs.GetAsync<long>("current-total-rp", ct);
        }
    }

    public override Task<AnalysisResult> AnalyzeAsync(
        IReadOnlyList<LogEvent> events,
        CombatStatistics? baseStatistics,
        AnalysisOptions options,
        CancellationToken ct = default)
    {
        _sessionEvents.Clear();

        // Find kill events
        var kills = events
            .OfType<DamageEvent>()
            .Where(e => IsKillingBlow(e, events))
            .ToList();

        // Calculate RP for each kill
        foreach (var kill in kills)
        {
            var rp = CalculateRpForKill(kill, options);
            _sessionEvents.Add(new RpEvent(
                kill.Timestamp.ToDateTime(DateOnly.FromDateTime(DateTime.Today)),
                DetermineKillType(kill, events),
                rp,
                kill.Target,
                null // Target rank if detectable
            ));
        }

        var totalRp = _sessionEvents.Sum(e => e.RpAmount);
        var duration = CalculateSessionDuration(events);
        var rpPerHour = duration.TotalHours > 0
            ? totalRp / duration.TotalHours
            : 0;

        var newTotalRp = _startingRp + totalRp;
        var (rank, level) = RealmRankData.GetRealmRank(newTotalRp);
        var rpToNext = RealmRankData.GetRpForRank(rank, level + 1) - newTotalRp;
        var timeToNext = rpPerHour > 0
            ? TimeSpan.FromHours(rpToNext / rpPerHour)
            : (TimeSpan?)null;

        return Task.FromResult(Success(new Dictionary<string, object>
        {
            ["session-rp"] = totalRp,
            ["rp-per-hour"] = Math.Round(rpPerHour, 0),
            ["rp-per-kill"] = kills.Count > 0 ? totalRp / (double)kills.Count : 0,
            ["kill-count"] = kills.Count,
            ["current-rr"] = RealmRankData.FormatRealmRank(rank, level),
            ["rp-to-next"] = rpToNext,
            ["time-to-next"] = timeToNext
        }));
    }

    private int CalculateRpForKill(DamageEvent kill, AnalysisOptions options)
    {
        // Base RP calculation
        // Would need target realm rank for accurate calculation
        // Simplified version uses flat estimates

        // Solo kill vs group kill modifier
        // Realm rank difference modifier
        // Bonus RP weekends, etc.

        return 100; // Placeholder - real calculation is complex
    }
}
```

### RP Calculation Formula

The actual RP calculation in DAoC is:

```
Base RP = Target RR value × (1 + (your RR - target RR) × 0.05)
Group modifier = Base RP × (1 - (group size - 1) × 0.1)
Solo bonus = Base RP × 1.35 (if solo)
```

The plugin should allow configuration for:
- Solo vs. group play
- Bonus RP events (weekends, events)
- Server-specific modifiers

## User Interface Component

Optionally provide a statistics card showing:

```
┌─────────────────────────────────────┐
│ Realm Points                        │
├─────────────────────────────────────┤
│ Session RP:     12,450              │
│ RP/Hour:        8,300               │
│ Kills:          47                  │
│                                     │
│ Current Rank:   RR5L4               │
│ Progress:       ▓▓▓▓▓▓░░░░ 62%     │
│ RP to RR5L5:    4,250               │
│ Time at rate:   ~31 minutes         │
└─────────────────────────────────────┘
```

## Configuration Options

- Starting realm points (manual entry)
- Solo/group mode toggle
- Bonus RP multiplier
- Target realm rank goal
- Include/exclude specific kill types

## Persistence

Store in plugin data directory:
- Current total RP
- Session history
- Goal settings
- Rank achievement dates

## Dependencies

- Core combat parsing
- Kill detection (damage leading to death)
- Optional: Character configuration (for realm rank input)

## Complexity

**Medium** - RP calculation has nuances but follows documented formulas. Kill detection is the main challenge.

## Future Enhancements

- [ ] Auto-detect realm rank from log messages
- [ ] Integration with cross-realm character profiles
- [ ] Leaderboard submissions
- [ ] Weekly/monthly RP graphs
- [ ] Compare RP rates by class/spec
