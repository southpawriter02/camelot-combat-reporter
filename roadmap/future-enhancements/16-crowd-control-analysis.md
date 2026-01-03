# 16. Crowd Control Analysis

## Status: 📋 Planned

**Prerequisites:**
- ✅ Log parsing infrastructure
- ✅ Timeline view
- ⬚ CC event parsing
- ⬚ Diminishing returns tracking

---

## Description

Analyze crowd control (CC) effectiveness including mezzes, stuns, roots, and snares. Track CC chains, diminishing returns, CC breakers, and provide insights for improving CC usage and survival.

## Functionality

### Core Features

* **CC Detection:**
  * Parse CC application and breaks
  * Identify CC type and duration
  * Track CC source and target
  * Detect immunity periods

* **Diminishing Returns (DR):**
  * Track DR timers per target
  * Calculate effective CC duration
  * Alert when DR makes CC ineffective
  * DR decay monitoring

* **CC Breaking:**
  * Detect CC break events
  * Identify break source (ability, damage)
  * Track break timing
  * Calculate wasted CC duration

### CC Types

| Type | Examples | Duration | DR Category |
|------|----------|----------|-------------|
| **Mez** | Mesmerize, Hibernation | 30-60s | Mez |
| **Stun** | Slam, Stunning Shout | 3-9s | Stun |
| **Root** | Root, Tangler | 15-30s | Root |
| **Snare** | Snare, Cripple | 15-30s | Snare |
| **Silence** | Silence, Mute | 5-10s | Silence |
| **Disarm** | Disarm | 5-10s | Disarm |

### Diminishing Returns System

```
DR Levels:
Full Effect (100%) → First CC
Reduced (50%)     → Second CC within 60s
Minimal (25%)     → Third CC within 60s
Immune (0%)       → Fourth CC within 60s

DR Decay:
- Timer resets 60 seconds after last CC
- Each CC type has separate DR counter
- DR is per-target, not per-caster
```

### Analysis Views

* **CC Timeline:**
  * Visual CC duration bars
  * DR level indicators
  * Break markers
  * Chain visualization

* **CC Effectiveness:**
  * Average effective duration
  * DR-adjusted duration
  * Break rate percentage
  * Wasted CC time

* **CC Contribution:**
  * Damage dealt during CC
  * Kills enabled by CC
  * Peel effectiveness
  * Defensive CC saves

### CC Chain Analysis

* **Chain Detection:**
  * Identify overlapping CC
  * Calculate chain duration
  * Track chain participants
  * Measure chain gaps

* **Chain Optimization:**
  * Suggest optimal CC order
  * Identify wasted overlaps
  * Recommend timing improvements
  * DR-aware chain planning

### CC Statistics

| Metric | Description |
|--------|-------------|
| **CC Uptime** | Percentage of combat with active CC |
| **DR Efficiency** | CC applied at optimal DR level |
| **Break Rate** | Percentage of CC broken early |
| **Chain Success** | Successful chain completions |
| **Kill Participation** | Kills within 5s of your CC |
| **CC Contribution** | Team damage during your CC |

### Defensive CC Analysis

* **CC Received:**
  * Time spent CC'd
  * CC sources breakdown
  * Average CC duration received
  * CC chain frequency

* **CC Avoidance:**
  * Resist rate tracking
  * Purge/break ability usage
  * Immunity uptime
  * CC mitigation improvements

## Requirements

* **CC Parsing:** Detect all CC-related messages
* **DR System:** Implement DR tracking logic
* **Timing:** Precise timestamp handling for chains
* **UI:** CC timeline and statistics views

## Limitations

* Some CC applications may not log
* DR state not directly visible in logs
* Enemy CC tracking more limited
* Private server CC mechanics may vary

## Dependencies

* **01-log-parsing.md:** Core event parsing
* **04-timeline-view.md:** Timeline visualization
* **15-buff-debuff-tracking.md:** Effect duration tracking

## Implementation Phases

### Phase 1: CC Event Parsing
- [ ] Identify CC log message patterns
- [ ] Create CrowdControlEvent model
- [ ] Parse CC application and removal
- [ ] Detect CC breaks

### Phase 2: DR System
- [ ] Implement DR timer tracking
- [ ] Calculate effective durations
- [ ] Build DR state machine
- [ ] Add DR level indicators

### Phase 3: Chain Analysis
- [ ] Detect overlapping CC
- [ ] Calculate chain metrics
- [ ] Identify optimization opportunities
- [ ] Build chain visualization

### Phase 4: GUI Integration
- [ ] Design CC timeline widget
- [ ] Create CC statistics panel
- [ ] Implement chain analysis view
- [ ] Add DR monitoring display

## Technical Notes

### Data Structures

```csharp
public record CrowdControlEvent(
    DateTime Timestamp,
    CCEventType EventType,
    CCType CrowdControlType,
    string TargetName,
    string? SourceName,
    TimeSpan? Duration,
    DRLevel DRAtApplication,
    CCBreakReason? BreakReason
);

public enum CCEventType
{
    Applied,
    Expired,
    Broken,
    Resisted,
    Immune
}

public enum CCType
{
    Mez,
    Stun,
    Root,
    Snare,
    Silence,
    Disarm
}

public enum DRLevel
{
    Full,           // 100%
    Reduced,        // 50%
    Minimal,        // 25%
    Immune          // 0%
}

public enum CCBreakReason
{
    Damage,
    Purge,
    Ability,
    Duration,
    Unknown
}

public record CCChain(
    DateTime StartTime,
    DateTime EndTime,
    string TargetName,
    IReadOnlyList<CrowdControlEvent> Events,
    TimeSpan TotalDuration,
    TimeSpan GapTime,
    int ChainLength
);
```

### Diminishing Returns Tracker

```csharp
public class DRTracker
{
    private readonly Dictionary<(string Target, CCType Type), DRState> _drStates = new();
    private readonly TimeSpan _drDecayTime = TimeSpan.FromSeconds(60);

    public DRLevel GetCurrentDR(string target, CCType ccType)
    {
        var key = (target, ccType);

        if (!_drStates.TryGetValue(key, out var state))
            return DRLevel.Full;

        // Check if DR has decayed
        if (DateTime.Now - state.LastCCTime > _drDecayTime)
        {
            _drStates.Remove(key);
            return DRLevel.Full;
        }

        return state.Level;
    }

    public void ApplyCC(string target, CCType ccType)
    {
        var key = (target, ccType);
        var currentDR = GetCurrentDR(target, ccType);

        var newLevel = currentDR switch
        {
            DRLevel.Full => DRLevel.Reduced,
            DRLevel.Reduced => DRLevel.Minimal,
            DRLevel.Minimal => DRLevel.Immune,
            _ => DRLevel.Immune
        };

        _drStates[key] = new DRState(newLevel, DateTime.Now);
    }

    public TimeSpan CalculateEffectiveDuration(
        TimeSpan baseDuration,
        DRLevel drLevel)
    {
        var multiplier = drLevel switch
        {
            DRLevel.Full => 1.0,
            DRLevel.Reduced => 0.5,
            DRLevel.Minimal => 0.25,
            DRLevel.Immune => 0.0,
            _ => 0.0
        };

        return TimeSpan.FromTicks((long)(baseDuration.Ticks * multiplier));
    }

    public TimeSpan GetTimeUntilDRReset(string target, CCType ccType)
    {
        var key = (target, ccType);

        if (!_drStates.TryGetValue(key, out var state))
            return TimeSpan.Zero;

        var resetTime = state.LastCCTime + _drDecayTime;
        var remaining = resetTime - DateTime.Now;

        return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
    }

    private record DRState(DRLevel Level, DateTime LastCCTime);
}
```

### CC Chain Analyzer

```csharp
public class CCChainAnalyzer
{
    private readonly TimeSpan _chainGapThreshold = TimeSpan.FromSeconds(2);

    public IReadOnlyList<CCChain> DetectChains(IEnumerable<CrowdControlEvent> events)
    {
        var chains = new List<CCChain>();
        var eventsByTarget = events
            .Where(e => e.EventType == CCEventType.Applied)
            .GroupBy(e => e.TargetName);

        foreach (var targetEvents in eventsByTarget)
        {
            var orderedEvents = targetEvents.OrderBy(e => e.Timestamp).ToList();
            var currentChain = new List<CrowdControlEvent>();

            foreach (var evt in orderedEvents)
            {
                if (currentChain.Count == 0)
                {
                    currentChain.Add(evt);
                    continue;
                }

                var lastEvent = currentChain.Last();
                var gap = evt.Timestamp - (lastEvent.Timestamp + (lastEvent.Duration ?? TimeSpan.Zero));

                if (gap <= _chainGapThreshold)
                {
                    currentChain.Add(evt);
                }
                else
                {
                    if (currentChain.Count > 1)
                    {
                        chains.Add(BuildChain(targetEvents.Key, currentChain));
                    }
                    currentChain = new List<CrowdControlEvent> { evt };
                }
            }

            if (currentChain.Count > 1)
            {
                chains.Add(BuildChain(targetEvents.Key, currentChain));
            }
        }

        return chains;
    }

    private CCChain BuildChain(string target, List<CrowdControlEvent> events)
    {
        var start = events.First().Timestamp;
        var lastEvent = events.Last();
        var end = lastEvent.Timestamp + (lastEvent.Duration ?? TimeSpan.Zero);

        var totalDuration = TimeSpan.Zero;
        var gapTime = TimeSpan.Zero;

        for (int i = 0; i < events.Count; i++)
        {
            totalDuration += events[i].Duration ?? TimeSpan.Zero;

            if (i > 0)
            {
                var prevEnd = events[i - 1].Timestamp +
                    (events[i - 1].Duration ?? TimeSpan.Zero);
                var gap = events[i].Timestamp - prevEnd;
                if (gap > TimeSpan.Zero)
                    gapTime += gap;
            }
        }

        return new CCChain(
            start, end, target, events,
            totalDuration, gapTime, events.Count
        );
    }

    public ChainEfficiency AnalyzeChainEfficiency(CCChain chain)
    {
        var theoreticalMax = chain.Events
            .Sum(e => (e.Duration ?? TimeSpan.Zero).TotalSeconds);

        var actualDuration = (chain.EndTime - chain.StartTime).TotalSeconds;
        var efficiency = actualDuration / theoreticalMax * 100;

        var overlaps = new List<TimeSpan>();
        for (int i = 1; i < chain.Events.Count; i++)
        {
            var prevEnd = chain.Events[i - 1].Timestamp +
                (chain.Events[i - 1].Duration ?? TimeSpan.Zero);
            var overlap = prevEnd - chain.Events[i].Timestamp;
            if (overlap > TimeSpan.Zero)
                overlaps.Add(overlap);
        }

        return new ChainEfficiency(
            efficiency,
            chain.GapTime,
            overlaps.Any() ? overlaps.Aggregate((a, b) => a + b) : TimeSpan.Zero,
            overlaps.Count
        );
    }
}

public record ChainEfficiency(
    double EfficiencyPercent,
    TimeSpan TotalGapTime,
    TimeSpan TotalOverlapTime,
    int OverlapCount
);
```

### CC Statistics Service

```csharp
public class CCStatisticsService
{
    public CCStatistics CalculateStatistics(
        IEnumerable<CrowdControlEvent> events,
        TimeSpan combatDuration)
    {
        var applied = events.Where(e => e.EventType == CCEventType.Applied).ToList();
        var broken = events.Where(e => e.EventType == CCEventType.Broken).ToList();
        var resisted = events.Where(e => e.EventType == CCEventType.Resisted).ToList();

        var totalCCDuration = applied
            .Sum(e => (e.Duration ?? TimeSpan.Zero).TotalSeconds);

        var actualCCDuration = applied
            .Where(e => broken.All(b => b.Timestamp != e.Timestamp))
            .Sum(e => (e.Duration ?? TimeSpan.Zero).TotalSeconds);

        var drEfficiency = applied
            .Count(e => e.DRAtApplication == DRLevel.Full)
            / (double)applied.Count * 100;

        return new CCStatistics(
            TotalCCApplied: applied.Count,
            TotalCCResisted: resisted.Count,
            TotalCCBroken: broken.Count,
            CCUptime: totalCCDuration / combatDuration.TotalSeconds * 100,
            AverageDuration: TimeSpan.FromSeconds(
                applied.Average(e => (e.Duration ?? TimeSpan.Zero).TotalSeconds)),
            BreakRate: broken.Count / (double)applied.Count * 100,
            DREfficiency: drEfficiency,
            CCByType: applied.GroupBy(e => e.CrowdControlType)
                .ToDictionary(g => g.Key, g => g.Count())
        );
    }
}

public record CCStatistics(
    int TotalCCApplied,
    int TotalCCResisted,
    int TotalCCBroken,
    double CCUptime,
    TimeSpan AverageDuration,
    double BreakRate,
    double DREfficiency,
    IReadOnlyDictionary<CCType, int> CCByType
);
```

### CC Timeline Widget

```csharp
public class CCTimelineWidget : WidgetBase
{
    private readonly List<CrowdControlEvent> _events = new();
    private readonly DRTracker _drTracker = new();

    public override void Render(Graphics graphics)
    {
        var activeCC = GetActiveCC();
        var y = Bounds.Y;

        foreach (var cc in activeCC)
        {
            DrawCCBar(graphics, cc, y);
            y += 20;
        }
    }

    private void DrawCCBar(Graphics graphics, CrowdControlEvent cc, float y)
    {
        var remaining = (cc.Timestamp + (cc.Duration ?? TimeSpan.Zero)) - DateTime.Now;
        if (remaining <= TimeSpan.Zero) return;

        var totalDuration = cc.Duration ?? TimeSpan.FromSeconds(30);
        var fillPercent = remaining / totalDuration;

        // Background
        graphics.FillRectangle(_backgroundBrush, Bounds.X, y, Bounds.Width, 16);

        // CC bar with DR-based color
        var color = cc.DRAtApplication switch
        {
            DRLevel.Full => Color.Green,
            DRLevel.Reduced => Color.Yellow,
            DRLevel.Minimal => Color.Orange,
            _ => Color.Red
        };

        graphics.FillRectangle(
            new SolidBrush(color),
            Bounds.X, y,
            (float)(Bounds.Width * fillPercent), 16
        );

        // CC type label
        graphics.DrawText(
            _font, _textBrush,
            Bounds.X + 4, y + 2,
            $"{cc.CrowdControlType}: {remaining.TotalSeconds:F1}s"
        );

        // DR indicator
        var drText = cc.DRAtApplication switch
        {
            DRLevel.Full => "100%",
            DRLevel.Reduced => "50%",
            DRLevel.Minimal => "25%",
            _ => "IMMUNE"
        };

        graphics.DrawText(
            _smallFont, _textBrush,
            Bounds.X + Bounds.Width - 40, y + 2,
            drText
        );
    }
}
```
