# PvP Matchup Analyzer Plugin

## Plugin Type: Data Analysis

## Overview

Track win/loss records against specific enemy players and classes, identify favorable and unfavorable matchups, and provide tactical insights for improving PvP performance in 1v1 and small-scale encounters.

## Problem Statement

PvP players want to understand:
- Which classes do I consistently beat or lose to?
- Are there specific players I struggle against?
- What's my overall win rate in different matchup scenarios?
- How can I improve against my weak matchups?
- What patterns lead to my victories vs. defeats?

## Features

### Win/Loss Tracking
- Track outcomes by enemy class
- Track outcomes by specific enemy player
- Solo vs. group fight categorization
- First strike vs. reactive engagement
- Fight duration correlation with outcomes

### Matchup Analysis
- Win rate by class matchup
- Damage dealt vs. taken in wins vs. losses
- Time-to-kill comparisons
- Opening move success rates
- Finishing blow patterns

### Trend Detection
- Improvement/decline over time
- Session-by-session performance
- Learning curve visualization
- Comeback patterns (losing streak to winning)

### Tactical Insights
- Successful tactics against each class
- Styles/spells that work best per matchup
- Defensive vs. aggressive strategy outcomes
- Positioning and timing patterns

## Technical Specification

### Plugin Manifest

```json
{
  "id": "pvp-matchup-analyzer",
  "name": "PvP Matchup Analyzer",
  "version": "1.0.0",
  "author": "CCR Community",
  "description": "Tracks win/loss records and analyzes PvP matchups",
  "type": "DataAnalysis",
  "entryPoint": {
    "assembly": "PvPMatchupAnalyzer.dll",
    "typeName": "PvPMatchupAnalyzer.MatchupPlugin"
  },
  "compatibility": {
    "minAppVersion": "1.0.0"
  },
  "permissions": [
    "CombatDataAccess",
    "FileRead",
    "FileWrite"
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
| `total-fights` | Total Fights | int | Total PvP encounters |
| `wins` | Wins | int | Fights where you killed enemy |
| `losses` | Losses | int | Fights where enemy killed you |
| `win-rate` | Win Rate | double | Overall win percentage |
| `best-matchup` | Best Matchup | string | Class with highest win rate |
| `worst-matchup` | Worst Matchup | string | Class with lowest win rate |
| `nemesis` | Nemesis | string | Player you lose to most |
| `victim` | Favorite Target | string | Player you beat most |

### Data Structures

```csharp
public record PvPFight(
    Guid Id,
    DateTime Timestamp,
    FightOutcome Outcome,
    string EnemyName,
    CharacterClass EnemyClass,
    Realm EnemyRealm,
    TimeSpan Duration,
    int DamageDealt,
    int DamageTaken,
    int HealingDone,
    bool WasFirstStrike,
    bool WasGroupFight,
    IReadOnlyList<string> StylesUsed,
    IReadOnlyList<string> SpellsUsed,
    string? KillingBlow
);

public enum FightOutcome
{
    Win,        // You killed them
    Loss,       // They killed you
    Draw,       // Both died or neither died
    Escaped,    // One party fled
    Interrupted // Fight interrupted by third party
}

public record ClassMatchupStats(
    CharacterClass EnemyClass,
    int TotalFights,
    int Wins,
    int Losses,
    int Draws,
    double WinRate,
    double AvgDamageDealt,
    double AvgDamageTaken,
    TimeSpan AvgFightDuration,
    double FirstStrikeWinRate,
    IReadOnlyList<string> TopWinningStyles,
    IReadOnlyList<string> TopLosingStyles
);

public record PlayerMatchupStats(
    string PlayerName,
    CharacterClass PlayerClass,
    int TotalFights,
    int Wins,
    int Losses,
    double WinRate,
    DateTime FirstEncounter,
    DateTime LastEncounter,
    TrendDirection Trend  // Improving, Declining, Stable
);

public record MatchupInsight(
    InsightType Type,
    string Title,
    string Description,
    string? RecommendedAction,
    InsightSeverity Severity
);

public enum TrendDirection
{
    Improving,
    Declining,
    Stable,
    Insufficient  // Not enough data
}
```

### Implementation Outline

```csharp
public class MatchupPlugin : DataAnalysisPluginBase
{
    private readonly Dictionary<string, PlayerMatchupStats> _playerStats = new();
    private readonly Dictionary<CharacterClass, ClassMatchupStats> _classStats = new();
    private IDataStore? _dataStore;

    public override async Task InitializeAsync(
        IPluginContext context,
        CancellationToken ct = default)
    {
        _dataStore = context.GetService<IDataStore>();
        if (_dataStore != null)
        {
            await LoadHistoricalDataAsync(ct);
        }
    }

    public override Task<AnalysisResult> AnalyzeAsync(
        IReadOnlyList<LogEvent> events,
        CombatStatistics? baseStatistics,
        AnalysisOptions options,
        CancellationToken ct = default)
    {
        // Identify PvP fights from events
        var fights = IdentifyFights(events, options.CombatantName);

        if (!fights.Any())
        {
            return Task.FromResult(Empty());
        }

        // Update statistics
        foreach (var fight in fights)
        {
            UpdatePlayerStats(fight);
            UpdateClassStats(fight);
        }

        // Calculate session stats
        var sessionWins = fights.Count(f => f.Outcome == FightOutcome.Win);
        var sessionLosses = fights.Count(f => f.Outcome == FightOutcome.Loss);
        var sessionWinRate = fights.Count > 0
            ? (double)sessionWins / fights.Count * 100
            : 0;

        // Find best/worst matchups
        var classMatchups = CalculateClassMatchups(fights);
        var bestMatchup = classMatchups
            .Where(m => m.TotalFights >= 3)
            .OrderByDescending(m => m.WinRate)
            .FirstOrDefault();
        var worstMatchup = classMatchups
            .Where(m => m.TotalFights >= 3)
            .OrderBy(m => m.WinRate)
            .FirstOrDefault();

        // Find nemesis and victim
        var playerMatchups = CalculatePlayerMatchups(fights);
        var nemesis = playerMatchups
            .Where(p => p.TotalFights >= 2)
            .OrderBy(p => p.WinRate)
            .FirstOrDefault();
        var victim = playerMatchups
            .Where(p => p.TotalFights >= 2)
            .OrderByDescending(p => p.WinRate)
            .FirstOrDefault();

        // Generate insights
        var insights = GenerateInsights(
            classMatchups,
            playerMatchups,
            sessionWinRate
        );

        return Task.FromResult(Success(new Dictionary<string, object>
        {
            ["total-fights"] = fights.Count,
            ["wins"] = sessionWins,
            ["losses"] = sessionLosses,
            ["win-rate"] = Math.Round(sessionWinRate, 1),
            ["best-matchup"] = bestMatchup?.EnemyClass.ToString() ?? "N/A",
            ["worst-matchup"] = worstMatchup?.EnemyClass.ToString() ?? "N/A",
            ["nemesis"] = nemesis?.PlayerName ?? "N/A",
            ["victim"] = victim?.PlayerName ?? "N/A",
            ["class-matchups"] = classMatchups,
            ["player-matchups"] = playerMatchups
        }, insights));
    }

    private List<PvPFight> IdentifyFights(
        IReadOnlyList<LogEvent> events,
        string combatantName)
    {
        var fights = new List<PvPFight>();
        var damageEvents = events.OfType<DamageEvent>().ToList();
        var killEvents = events.OfType<KillEvent>().ToList();

        // Group damage events by target to identify separate fights
        var engagements = damageEvents
            .Where(e => IsPlayerVsPlayer(e))
            .GroupBy(e => GetEngagementKey(e, combatantName))
            .ToList();

        foreach (var engagement in engagements)
        {
            var fight = AnalyzeEngagement(
                engagement.ToList(),
                killEvents,
                combatantName
            );
            if (fight != null)
            {
                fights.Add(fight);
            }
        }

        return fights;
    }

    private bool IsPlayerVsPlayer(DamageEvent e)
    {
        // Heuristic: Player names typically have specific patterns
        // and don't match mob naming conventions
        return !IsMobName(e.Source) && !IsMobName(e.Target);
    }

    private FightOutcome DetermineOutcome(
        List<DamageEvent> engagement,
        List<KillEvent> kills,
        string combatantName)
    {
        var enemyName = engagement
            .Select(e => e.Source == combatantName ? e.Target : e.Source)
            .First();

        var youKilledThem = kills.Any(k =>
            k.Killer == combatantName && k.Victim == enemyName);
        var theyKilledYou = kills.Any(k =>
            k.Killer == enemyName && k.Victim == combatantName);

        return (youKilledThem, theyKilledYou) switch
        {
            (true, false) => FightOutcome.Win,
            (false, true) => FightOutcome.Loss,
            (true, true) => FightOutcome.Draw,
            _ => FightOutcome.Escaped
        };
    }

    private List<MatchupInsight> GenerateInsights(
        List<ClassMatchupStats> classMatchups,
        List<PlayerMatchupStats> playerMatchups,
        double sessionWinRate)
    {
        var insights = new List<MatchupInsight>();

        // Session performance
        if (sessionWinRate >= 70)
        {
            insights.Add(new MatchupInsight(
                InsightType.Positive,
                "Dominant Session",
                $"Excellent {sessionWinRate:F0}% win rate this session!",
                null,
                InsightSeverity.Info
            ));
        }
        else if (sessionWinRate < 40)
        {
            insights.Add(new MatchupInsight(
                InsightType.Efficiency,
                "Tough Session",
                $"Win rate of {sessionWinRate:F0}% this session.",
                "Consider reviewing fight logs to identify patterns",
                InsightSeverity.Suggestion
            ));
        }

        // Class-specific insights
        var problemClasses = classMatchups
            .Where(m => m.TotalFights >= 5 && m.WinRate < 30)
            .ToList();

        foreach (var problem in problemClasses)
        {
            insights.Add(new MatchupInsight(
                InsightType.SpellChoice,
                $"Struggling vs {problem.EnemyClass}",
                $"Only {problem.WinRate:F0}% win rate in {problem.TotalFights} fights",
                $"Average fight lasts {problem.AvgFightDuration.TotalSeconds:F0}s - " +
                "consider adjusting opener tactics",
                InsightSeverity.Warning
            ));
        }

        // First strike analysis
        var firstStrikeClasses = classMatchups
            .Where(m => m.TotalFights >= 5)
            .Where(m => Math.Abs(m.FirstStrikeWinRate - m.WinRate) > 20)
            .ToList();

        foreach (var fsClass in firstStrikeClasses)
        {
            if (fsClass.FirstStrikeWinRate > fsClass.WinRate)
            {
                insights.Add(new MatchupInsight(
                    InsightType.ReactionTime,
                    $"First Strike Matters vs {fsClass.EnemyClass}",
                    $"{fsClass.FirstStrikeWinRate:F0}% win rate with first strike " +
                    $"vs {fsClass.WinRate:F0}% overall",
                    "Try to initiate fights against this class",
                    InsightSeverity.Suggestion
                ));
            }
        }

        // Nemesis alert
        var nemeses = playerMatchups
            .Where(p => p.TotalFights >= 3 && p.WinRate < 25)
            .OrderBy(p => p.WinRate)
            .Take(1)
            .ToList();

        foreach (var nemesis in nemeses)
        {
            insights.Add(new MatchupInsight(
                InsightType.TargetNeglect,
                $"Nemesis: {nemesis.PlayerName}",
                $"Lost {nemesis.Losses} of {nemesis.TotalFights} fights " +
                $"({nemesis.PlayerClass})",
                nemesis.Trend == TrendDirection.Improving
                    ? "You're improving against them!"
                    : "Study their patterns and adapt",
                InsightSeverity.Warning
            ));
        }

        // Improvement tracking
        var improvingMatchups = playerMatchups
            .Where(p => p.Trend == TrendDirection.Improving && p.TotalFights >= 5)
            .ToList();

        if (improvingMatchups.Any())
        {
            insights.Add(new MatchupInsight(
                InsightType.Positive,
                "Improvement Detected",
                $"Your performance is improving against: " +
                string.Join(", ", improvingMatchups.Select(p => p.PlayerName)),
                null,
                InsightSeverity.Info
            ));
        }

        return insights;
    }

    private TrendDirection CalculateTrend(
        List<PvPFight> recentFights,
        string playerName)
    {
        var fights = recentFights
            .Where(f => f.EnemyName == playerName)
            .OrderBy(f => f.Timestamp)
            .ToList();

        if (fights.Count < 4)
            return TrendDirection.Insufficient;

        var half = fights.Count / 2;
        var firstHalfWinRate = fights.Take(half)
            .Count(f => f.Outcome == FightOutcome.Win) / (double)half;
        var secondHalfWinRate = fights.Skip(half)
            .Count(f => f.Outcome == FightOutcome.Win) / (double)(fights.Count - half);

        var improvement = secondHalfWinRate - firstHalfWinRate;

        return improvement switch
        {
            > 0.15 => TrendDirection.Improving,
            < -0.15 => TrendDirection.Declining,
            _ => TrendDirection.Stable
        };
    }
}
```

## Output Summary

```
PVP MATCHUP ANALYSIS
====================

Session: 47 fights | 28 wins | 17 losses | 2 draws
Win Rate: 59.6%

CLASS MATCHUPS
─────────────────────────────────────────────────────────
Class           | Fights | Wins | Losses | Win Rate | Trend
Infiltrator     | 12     | 9    | 3      | 75.0%    | ↑
Armsman         | 8      | 5    | 3      | 62.5%    | →
Mercenary       | 6      | 3    | 3      | 50.0%    | ↓
Minstrel        | 5      | 1    | 4      | 20.0%    | →
Scout           | 4      | 3    | 1      | 75.0%    | →
Other           | 12     | 7    | 3      | 63.6%    | →

NOTABLE OPPONENTS
─────────────────────────────────────────────────────────
Player          | Class      | Record | Win Rate | Status
Shadowstrike    | Infiltrator| 5-1    | 83.3%    | Victim ✓
ArrowRain       | Scout      | 3-0    | 100%     | Victim ✓
PaladinKnight   | Armsman    | 1-4    | 20.0%    | Nemesis ⚠
BardMaster      | Minstrel   | 0-3    | 0.0%     | Nemesis ⚠

INSIGHTS
• ⚠️ Struggling vs Minstrel: 20% win rate in 5 fights
• ⚠️ Nemesis: PaladinKnight (Armsman) - lost 4 of 5 fights
• 💡 First Strike Matters vs Armsman: 80% win rate when initiating
• ✓ Improving against Infiltrators - up 25% from last week
```

## Configuration Options

- **Minimum fights for stats**: Threshold before showing matchup data
- **Player name tracking**: Enable/disable specific player tracking
- **Historical window**: How far back to include in lifetime stats
- **Class detection method**: Heuristic-based or manual tagging
- **Group fight handling**: Include, exclude, or separate category

## Persistence

Store in plugin data directory:
- `matchups.json` - All fight records
- `class-stats.json` - Aggregated class statistics
- `player-stats.json` - Per-player statistics
- `config.json` - User preferences

## Data Privacy

- Player names are stored locally only
- Export functions anonymize by default
- Option to clear history for specific players
- No automatic sharing of matchup data

## Dependencies

- Core combat parsing
- Kill/death detection
- Character class detection (manual or heuristic)
- Optional: Integration with Cross-Realm Analysis for class data

## Complexity

**Medium-High** - Requires accurate fight identification, outcome determination, and class detection. Historical data management adds complexity.

## Future Enhancements

- [ ] Integration with replay system for fight review
- [ ] Suggested counter-strategies by class
- [ ] Heat map of fight locations
- [ ] Time-of-day performance patterns
- [ ] Guild/alliance matchup tracking
- [ ] Automated class detection from combat patterns
- [ ] Export for community tier list contributions
