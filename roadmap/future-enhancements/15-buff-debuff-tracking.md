# 15. Buff and Debuff Tracking

## Status: 📋 Planned

**Prerequisites:**
- ✅ Log parsing infrastructure
- ✅ Timeline view
- ⬚ Buff/debuff event parsing

---

## Description

Track active buffs, debuffs, and effects on players and enemies. Monitor buff uptime, debuff effectiveness, and provide insights for optimizing buff maintenance and debuff application.

## Functionality

### Core Features

* **Buff Detection:**
  * Parse buff application messages
  * Track buff duration and expiration
  * Identify buff sources
  * Distinguish between buff types

* **Debuff Tracking:**
  * Detect debuff application on enemies
  * Monitor debuff resistance and immunity
  * Track debuff duration
  * Calculate debuff uptime on targets

* **Effect Categories:**
  * Damage buffs (damage add, damage multiplier)
  * Defensive buffs (armor, absorption, resistances)
  * Speed buffs (movement, attack speed)
  * Regeneration (health, mana, endurance)
  * Crowd control effects
  * Special abilities (stealth, immunity)

### Buff Types

| Category | Examples | Tracking Focus |
|----------|----------|----------------|
| **Concentration Buffs** | Str/Con buffs, armor factor | Uptime, coverage |
| **Self Buffs** | Damage add, shield | Application timing |
| **Group Buffs** | Speed, power regen | Group coverage |
| **Chants/Songs** | Bard songs, skald chants | Pulse consistency |
| **Realm Abilities** | Purge effects, RA buffs | Usage efficiency |
| **Procs** | Weapon procs, reactive | Proc frequency |

### Debuff Types

| Category | Examples | Tracking Focus |
|----------|----------|----------------|
| **Stat Debuffs** | Strength/Con debuff | Uptime on targets |
| **Resistance Debuffs** | Resist debuff | Pre-damage application |
| **Attack Speed** | Snare, attack speed debuff | Duration effectiveness |
| **Movement** | Root, snare | CC contribution |
| **Disease** | Disease effects | Healing reduction |
| **DoTs** | Poison, bleed | Damage contribution |

### Analysis Views

* **Buff Uptime Dashboard:**
  * Per-buff uptime percentages
  * Gap analysis (when buffs fell off)
  * Buff overlap visualization
  * Priority ranking by importance

* **Debuff Effectiveness:**
  * Debuff application success rate
  * Resistance rate tracking
  * Damage dealt while debuffed
  * Debuff duration consistency

* **Buff Timeline:**
  * Visual buff bar similar to in-game
  * Historical buff state
  * Correlation with combat events
  * Expiration warnings

### Buff Optimization

* **Uptime Targets:**
  * Define target uptime percentages
  * Alert when uptime drops below threshold
  * Track improvement over sessions
  * Compare to class benchmarks

* **Buff Priorities:**
  * Rank buffs by combat impact
  * Identify critical buffs
  * Suggest rebuff timing
  * Calculate buff value per second

* **Gap Analysis:**
  * Find periods without key buffs
  * Correlate with damage taken
  * Identify rebuff delays
  * Track buff management skill

## Requirements

* **Buff Parsing:** Detect all buff/debuff messages
* **Duration Tracking:** Calculate buff remaining time
* **Database:** Buff information (name, type, base duration)
* **UI:** Buff bar and uptime displays

## Limitations

* Not all buffs appear in combat logs
* Buff duration may vary by spec/RA
* Enemy debuff resistance not always visible
* Buff overwriting not always detectable

## Dependencies

* **01-log-parsing.md:** Core event parsing
* **04-timeline-view.md:** Timeline visualization
* **14-death-analysis.md:** Correlate buffs with survival

## Implementation Phases

### Phase 1: Buff Detection
- [ ] Identify buff-related log patterns
- [ ] Create Buff and Debuff event models
- [ ] Parse buff application and removal
- [ ] Build buff state tracker

### Phase 2: Duration Tracking
- [ ] Create buff database with durations
- [ ] Implement expiration estimation
- [ ] Track actual vs. expected duration
- [ ] Handle buff refresh logic

### Phase 3: Analysis Engine
- [ ] Calculate uptime percentages
- [ ] Implement gap detection
- [ ] Build debuff effectiveness metrics
- [ ] Create buff priority system

### Phase 4: GUI Integration
- [ ] Design buff bar widget
- [ ] Create uptime dashboard
- [ ] Implement buff timeline
- [ ] Add configuration options

## Technical Notes

### Data Structures

```csharp
public record BuffEvent(
    DateTime Timestamp,
    BuffEventType EventType,
    string BuffName,
    string? TargetName,
    string? SourceName,
    TimeSpan? Duration,
    BuffCategory Category
);

public enum BuffEventType
{
    Applied,
    Refreshed,
    Expired,
    Removed,
    Resisted
}

public enum BuffCategory
{
    // Beneficial
    StatBuff,
    DefensiveBuff,
    OffensiveBuff,
    SpeedBuff,
    Regeneration,
    Concentration,
    RealmAbility,

    // Detrimental
    StatDebuff,
    ResistDebuff,
    SpeedDebuff,
    DamageOverTime,
    CrowdControl,
    Disease
}

public record BuffState(
    string BuffName,
    BuffCategory Category,
    DateTime AppliedAt,
    DateTime? ExpiresAt,
    string? Source,
    bool IsActive
)
{
    public TimeSpan? RemainingDuration => IsActive && ExpiresAt.HasValue
        ? ExpiresAt.Value - DateTime.Now
        : null;
}

public record BuffUptimeStats(
    string BuffName,
    BuffCategory Category,
    TimeSpan TotalUptime,
    TimeSpan TotalCombatTime,
    double UptimePercentage,
    int ApplicationCount,
    int GapCount,
    TimeSpan AverageGapDuration
);
```

### Buff State Tracker

```csharp
public class BuffStateTracker
{
    private readonly Dictionary<string, BuffState> _activeBuffs = new();
    private readonly List<BuffEvent> _history = new();
    private readonly BuffDatabase _database;

    public IReadOnlyDictionary<string, BuffState> ActiveBuffs => _activeBuffs;

    public void ProcessEvent(BuffEvent evt)
    {
        _history.Add(evt);

        switch (evt.EventType)
        {
            case BuffEventType.Applied:
            case BuffEventType.Refreshed:
                ApplyBuff(evt);
                break;

            case BuffEventType.Expired:
            case BuffEventType.Removed:
                RemoveBuff(evt.BuffName);
                break;
        }
    }

    private void ApplyBuff(BuffEvent evt)
    {
        var duration = evt.Duration ?? _database.GetDefaultDuration(evt.BuffName);

        _activeBuffs[evt.BuffName] = new BuffState(
            evt.BuffName,
            evt.Category,
            evt.Timestamp,
            duration.HasValue ? evt.Timestamp + duration.Value : null,
            evt.SourceName,
            true
        );
    }

    private void RemoveBuff(string buffName)
    {
        if (_activeBuffs.TryGetValue(buffName, out var buff))
        {
            _activeBuffs[buffName] = buff with { IsActive = false };
        }
    }

    public IReadOnlyList<BuffState> GetBuffsByCategory(BuffCategory category)
    {
        return _activeBuffs.Values
            .Where(b => b.Category == category && b.IsActive)
            .ToList();
    }

    public bool HasBuff(string buffName)
    {
        return _activeBuffs.TryGetValue(buffName, out var buff) && buff.IsActive;
    }
}
```

### Uptime Calculator

```csharp
public class BuffUptimeCalculator
{
    public BuffUptimeStats CalculateUptime(
        string buffName,
        IEnumerable<BuffEvent> events,
        DateTime combatStart,
        DateTime combatEnd)
    {
        var buffEvents = events
            .Where(e => e.BuffName == buffName)
            .OrderBy(e => e.Timestamp)
            .ToList();

        if (!buffEvents.Any())
        {
            return new BuffUptimeStats(
                buffName, BuffCategory.StatBuff,
                TimeSpan.Zero, combatEnd - combatStart,
                0, 0, 0, TimeSpan.Zero
            );
        }

        var totalUptime = TimeSpan.Zero;
        var gaps = new List<TimeSpan>();
        DateTime? lastActive = null;
        DateTime? lastEnd = null;
        int applications = 0;

        foreach (var evt in buffEvents)
        {
            switch (evt.EventType)
            {
                case BuffEventType.Applied:
                case BuffEventType.Refreshed:
                    if (lastEnd.HasValue && !lastActive.HasValue)
                    {
                        gaps.Add(evt.Timestamp - lastEnd.Value);
                    }
                    lastActive = evt.Timestamp;
                    applications++;
                    break;

                case BuffEventType.Expired:
                case BuffEventType.Removed:
                    if (lastActive.HasValue)
                    {
                        totalUptime += evt.Timestamp - lastActive.Value;
                        lastEnd = evt.Timestamp;
                        lastActive = null;
                    }
                    break;
            }
        }

        // If buff still active at combat end
        if (lastActive.HasValue)
        {
            totalUptime += combatEnd - lastActive.Value;
        }

        var combatDuration = combatEnd - combatStart;

        return new BuffUptimeStats(
            buffName,
            buffEvents.First().Category,
            totalUptime,
            combatDuration,
            totalUptime / combatDuration * 100,
            applications,
            gaps.Count,
            gaps.Any() ? TimeSpan.FromTicks((long)gaps.Average(g => g.Ticks)) : TimeSpan.Zero
        );
    }
}
```

### Buff Database

```csharp
public class BuffDatabase
{
    private readonly Dictionary<string, BuffInfo> _buffs = new()
    {
        // Concentration Buffs
        ["Strength/Constitution"] = new BuffInfo(
            "Strength/Constitution", BuffCategory.Concentration,
            TimeSpan.FromMinutes(10), true),

        ["Armor Factor"] = new BuffInfo(
            "Armor Factor", BuffCategory.Concentration,
            TimeSpan.FromMinutes(10), true),

        // Self Buffs
        ["Damage Add"] = new BuffInfo(
            "Damage Add", BuffCategory.OffensiveBuff,
            TimeSpan.FromSeconds(30), false),

        // Speed Buffs
        ["Speed of the Realm"] = new BuffInfo(
            "Speed of the Realm", BuffCategory.SpeedBuff,
            TimeSpan.FromMinutes(5), true),

        // Debuffs
        ["Disease"] = new BuffInfo(
            "Disease", BuffCategory.Disease,
            TimeSpan.FromSeconds(30), false),
    };

    public TimeSpan? GetDefaultDuration(string buffName)
    {
        return _buffs.TryGetValue(buffName, out var info)
            ? info.DefaultDuration
            : null;
    }

    public BuffInfo? GetBuffInfo(string buffName)
    {
        return _buffs.TryGetValue(buffName, out var info) ? info : null;
    }
}

public record BuffInfo(
    string Name,
    BuffCategory Category,
    TimeSpan? DefaultDuration,
    bool IsConcentration
);
```

### Buff Bar Widget

```csharp
public class BuffBarWidget : WidgetBase
{
    private readonly BuffStateTracker _tracker;
    private readonly int _maxVisibleBuffs = 10;

    public override void Render(Graphics graphics)
    {
        var buffs = _tracker.ActiveBuffs.Values
            .Where(b => b.IsActive)
            .OrderByDescending(b => GetBuffPriority(b))
            .Take(_maxVisibleBuffs)
            .ToList();

        var x = Bounds.X;
        var y = Bounds.Y;
        var iconSize = 24;
        var spacing = 4;

        foreach (var buff in buffs)
        {
            // Draw buff icon
            DrawBuffIcon(graphics, buff, x, y, iconSize);

            // Draw duration bar
            if (buff.ExpiresAt.HasValue)
            {
                var remaining = buff.RemainingDuration ?? TimeSpan.Zero;
                var maxDuration = buff.ExpiresAt.Value - buff.AppliedAt;
                var fillPercent = remaining / maxDuration;

                DrawDurationBar(graphics, x, y + iconSize, iconSize, 3, fillPercent);
            }

            x += iconSize + spacing;
        }
    }

    private int GetBuffPriority(BuffState buff)
    {
        return buff.Category switch
        {
            BuffCategory.CrowdControl => 100,
            BuffCategory.OffensiveBuff => 90,
            BuffCategory.DefensiveBuff => 80,
            BuffCategory.SpeedBuff => 70,
            _ => 50
        };
    }

    private void DrawDurationBar(
        Graphics graphics, float x, float y,
        float width, float height, double fillPercent)
    {
        // Background
        graphics.FillRectangle(_backgroundBrush, x, y, width, height);

        // Fill based on remaining time
        var fillColor = fillPercent switch
        {
            < 0.2 => Color.Red,
            < 0.5 => Color.Yellow,
            _ => Color.Green
        };

        graphics.FillRectangle(
            new SolidBrush(fillColor),
            x, y, (float)(width * fillPercent), height
        );
    }
}
```
