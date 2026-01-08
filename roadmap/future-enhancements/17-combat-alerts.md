# 17. Combat Alerts and Notifications

## Status: ✅ Complete (v1.3.0)

**Implementation Complete:**
- ✅ Log parsing infrastructure
- ✅ Real-time parsing
- ✅ Alert rule engine
- ✅ Notification system

---

## Description

Provide real-time alerts and notifications for important combat events. Configure custom triggers based on damage thresholds, ability usage, enemy presence, and other conditions. Supports visual, audio, and external notification methods.

## Functionality

### Core Features

* **Alert Engine:**
  * Rule-based trigger system
  * Condition evaluation in real-time
  * Priority-based alert queuing
  * Cooldown management

* **Notification Types:**
  * Visual alerts (screen flash, popup)
  * Audio alerts (sound effects, TTS)
  * External notifications (Discord, webhook)
  * Overlay integration

* **Built-in Alerts:**
  * Low health warning
  * Kill/death notifications
  * Incoming burst damage
  * Ability cooldown ready
  * Enemy detected (by name/class)

### Alert Categories

| Category | Examples | Default Priority |
|----------|----------|------------------|
| **Survival** | Low health, high damage incoming | Critical |
| **Combat** | Kill, death, combat start/end | High |
| **Ability** | Cooldown ready, ability used on you | Medium |
| **Detection** | Enemy spotted, group member low | Low |
| **Statistics** | DPS milestone, personal best | Info |

### Alert Conditions

* **Health Triggers:**
  * Health below percentage
  * Health below absolute value
  * Rapid health loss (X damage in Y seconds)
  * Healing received above threshold

* **Damage Triggers:**
  * Damage dealt above threshold
  * Damage taken above threshold
  * Burst damage window detection
  * Specific damage type received

* **Entity Triggers:**
  * Specific enemy name detected
  * Enemy class detected
  * Group member health low
  * Friendly death

* **Ability Triggers:**
  * Specific ability used by enemy
  * Your ability ready (cooldown)
  * Buff expired
  * CC applied to you

### Alert Configuration

```yaml
# Example alert configuration
alerts:
  - name: "Low Health Warning"
    enabled: true
    priority: critical
    conditions:
      - type: health_below_percent
        value: 30
    cooldown: 5s
    notifications:
      - type: sound
        sound: "alert_critical.wav"
      - type: screen_flash
        color: red
        duration: 200ms

  - name: "Enemy Healer Spotted"
    enabled: true
    priority: high
    conditions:
      - type: enemy_class
        classes: [Cleric, Healer, Druid]
    cooldown: 30s
    notifications:
      - type: tts
        message: "Enemy healer detected"
      - type: visual
        text: "HEALER: {target_name}"

  - name: "Burst Damage Incoming"
    enabled: true
    priority: critical
    conditions:
      - type: damage_in_window
        damage: 1000
        window: 2s
    notifications:
      - type: overlay
        widget: "alert_box"
        message: "BURST: {damage_amount} in {window}s"
```

### Notification Methods

* **Visual Alerts:**
  * Screen border flash
  * Popup notifications
  * Overlay text/icons
  * Status bar updates

* **Audio Alerts:**
  * Sound effects library
  * Custom sound upload
  * Text-to-speech
  * Volume by priority

* **External Alerts:**
  * Discord webhook
  * Windows toast notifications
  * Mobile push (via service)
  * Custom webhook

### Alert History

* **Log all triggered alerts**
* **Statistics on alert frequency**
* **False positive tracking**
* **Alert effectiveness metrics**

## Requirements

* **Rule Engine:** Flexible condition evaluation
* **Audio Library:** Sound playback capability
* **Notification API:** System and external notifications
* **UI:** Alert configuration interface

## Limitations

* Real-time alerts require streaming parsing
* Audio may conflict with game sounds
* Too many alerts can be distracting
* Some conditions may produce false positives

## Dependencies

* **01-log-parsing.md:** Core event parsing
* **02-real-time-parsing.md:** Live event streaming
* **12-voice-integration.md:** TTS integration
* **13-overlay-hud.md:** Overlay alerts

## Implementation Phases

### Phase 1: Alert Engine Core
- [ ] Design alert rule model
- [ ] Implement condition evaluator
- [ ] Build alert queue system
- [ ] Add cooldown management

### Phase 2: Visual Notifications
- [ ] Create screen flash effect
- [ ] Implement popup notifications
- [ ] Add overlay alert widget
- [ ] Build alert history log

### Phase 3: Audio Notifications
- [ ] Integrate audio playback
- [ ] Add TTS support
- [ ] Create sound effect library
- [ ] Implement volume controls

### Phase 4: External Notifications
- [ ] Discord webhook integration
- [ ] Windows toast notifications
- [ ] Custom webhook support
- [ ] Mobile push (optional)

### Phase 5: Configuration UI
- [ ] Design alert builder interface
- [ ] Create condition editor
- [ ] Add notification preview
- [ ] Implement import/export

## Technical Notes

### Data Structures

```csharp
public record AlertRule(
    string Id,
    string Name,
    bool Enabled,
    AlertPriority Priority,
    IReadOnlyList<IAlertCondition> Conditions,
    ConditionLogic Logic,
    TimeSpan Cooldown,
    IReadOnlyList<INotification> Notifications
);

public enum AlertPriority
{
    Info,
    Low,
    Medium,
    High,
    Critical
}

public enum ConditionLogic
{
    All,    // AND
    Any     // OR
}

public interface IAlertCondition
{
    string Type { get; }
    bool Evaluate(CombatState state, LogEvent? triggerEvent);
}

public interface INotification
{
    string Type { get; }
    Task ExecuteAsync(AlertContext context);
}

public record AlertContext(
    AlertRule Rule,
    DateTime Timestamp,
    LogEvent? TriggerEvent,
    CombatState State,
    Dictionary<string, object> Variables
);
```

### Condition Implementations

```csharp
public class HealthBelowCondition : IAlertCondition
{
    public string Type => "health_below_percent";
    public int Threshold { get; init; }

    public bool Evaluate(CombatState state, LogEvent? evt)
    {
        if (state.PlayerMaxHealth == 0) return false;
        var healthPercent = state.PlayerHealth / (double)state.PlayerMaxHealth * 100;
        return healthPercent <= Threshold;
    }
}

public class DamageInWindowCondition : IAlertCondition
{
    public string Type => "damage_in_window";
    public int DamageThreshold { get; init; }
    public TimeSpan Window { get; init; }

    private readonly Queue<(DateTime Time, int Damage)> _recentDamage = new();

    public bool Evaluate(CombatState state, LogEvent? evt)
    {
        if (evt is not DamageEvent dmg) return false;

        var now = DateTime.Now;
        _recentDamage.Enqueue((now, dmg.DamageAmount));

        // Remove old entries
        while (_recentDamage.Any() &&
               now - _recentDamage.Peek().Time > Window)
        {
            _recentDamage.Dequeue();
        }

        var totalDamage = _recentDamage.Sum(d => d.Damage);
        return totalDamage >= DamageThreshold;
    }
}

public class EnemyClassCondition : IAlertCondition
{
    public string Type => "enemy_class";
    public HashSet<CharacterClass> TargetClasses { get; init; } = new();

    public bool Evaluate(CombatState state, LogEvent? evt)
    {
        if (evt is not DamageEvent dmg) return false;

        var enemyClass = state.GetEnemyClass(dmg.Source);
        return enemyClass.HasValue && TargetClasses.Contains(enemyClass.Value);
    }
}

public class AbilityUsedCondition : IAlertCondition
{
    public string Type => "ability_used";
    public HashSet<string> AbilityNames { get; init; } = new();
    public bool OnPlayer { get; init; } = true;

    public bool Evaluate(CombatState state, LogEvent? evt)
    {
        if (evt is not AbilityEvent ability) return false;

        if (OnPlayer && ability.Target != state.PlayerName)
            return false;

        return AbilityNames.Contains(ability.AbilityName);
    }
}
```

### Alert Engine

```csharp
public class AlertEngine : IDisposable
{
    private readonly List<AlertRule> _rules = new();
    private readonly Dictionary<string, DateTime> _lastTriggered = new();
    private readonly PriorityQueue<AlertContext, int> _alertQueue = new();

    public event Action<AlertContext>? OnAlertTriggered;

    public void AddRule(AlertRule rule) => _rules.Add(rule);
    public void RemoveRule(string ruleId) =>
        _rules.RemoveAll(r => r.Id == ruleId);

    public void ProcessEvent(LogEvent evt, CombatState state)
    {
        foreach (var rule in _rules.Where(r => r.Enabled))
        {
            if (IsOnCooldown(rule)) continue;

            var triggered = rule.Logic switch
            {
                ConditionLogic.All => rule.Conditions.All(c => c.Evaluate(state, evt)),
                ConditionLogic.Any => rule.Conditions.Any(c => c.Evaluate(state, evt)),
                _ => false
            };

            if (triggered)
            {
                TriggerAlert(rule, evt, state);
            }
        }
    }

    private bool IsOnCooldown(AlertRule rule)
    {
        if (!_lastTriggered.TryGetValue(rule.Id, out var lastTime))
            return false;

        return DateTime.Now - lastTime < rule.Cooldown;
    }

    private void TriggerAlert(AlertRule rule, LogEvent? evt, CombatState state)
    {
        _lastTriggered[rule.Id] = DateTime.Now;

        var context = new AlertContext(
            rule,
            DateTime.Now,
            evt,
            state,
            BuildVariables(rule, evt, state)
        );

        var priority = (int)rule.Priority;
        _alertQueue.Enqueue(context, -priority); // Negative for max-priority

        OnAlertTriggered?.Invoke(context);

        _ = ExecuteNotificationsAsync(context);
    }

    private async Task ExecuteNotificationsAsync(AlertContext context)
    {
        foreach (var notification in context.Rule.Notifications)
        {
            try
            {
                await notification.ExecuteAsync(context);
            }
            catch (Exception ex)
            {
                // Log notification failure
            }
        }
    }

    private Dictionary<string, object> BuildVariables(
        AlertRule rule,
        LogEvent? evt,
        CombatState state)
    {
        var vars = new Dictionary<string, object>
        {
            ["player_name"] = state.PlayerName,
            ["player_health"] = state.PlayerHealth,
            ["player_health_percent"] = state.PlayerHealthPercent,
            ["current_dps"] = state.CurrentDps,
        };

        if (evt is DamageEvent dmg)
        {
            vars["damage_amount"] = dmg.DamageAmount;
            vars["damage_source"] = dmg.Source;
            vars["damage_type"] = dmg.DamageType;
        }

        return vars;
    }
}
```

### Notification Implementations

```csharp
public class SoundNotification : INotification
{
    public string Type => "sound";
    public string SoundFile { get; init; } = "alert.wav";
    public float Volume { get; init; } = 1.0f;

    private static readonly AudioPlayer _player = new();

    public async Task ExecuteAsync(AlertContext context)
    {
        await _player.PlayAsync(SoundFile, Volume);
    }
}

public class ScreenFlashNotification : INotification
{
    public string Type => "screen_flash";
    public string Color { get; init; } = "red";
    public int DurationMs { get; init; } = 200;

    public async Task ExecuteAsync(AlertContext context)
    {
        var overlay = ServiceLocator.Get<IOverlayService>();
        await overlay.FlashScreenAsync(
            ParseColor(Color),
            TimeSpan.FromMilliseconds(DurationMs)
        );
    }
}

public class TtsNotification : INotification
{
    public string Type => "tts";
    public string Message { get; init; } = "";

    public async Task ExecuteAsync(AlertContext context)
    {
        var message = InterpolateVariables(Message, context.Variables);
        var tts = ServiceLocator.Get<ITextToSpeech>();
        await tts.SpeakAsync(message);
    }

    private string InterpolateVariables(
        string template,
        Dictionary<string, object> vars)
    {
        var result = template;
        foreach (var (key, value) in vars)
        {
            result = result.Replace($"{{{key}}}", value.ToString());
        }
        return result;
    }
}

public class DiscordWebhookNotification : INotification
{
    public string Type => "discord_webhook";
    public string WebhookUrl { get; init; } = "";
    public string Message { get; init; } = "";
    public bool IncludeEmbed { get; init; } = false;

    public async Task ExecuteAsync(AlertContext context)
    {
        var message = InterpolateVariables(Message, context.Variables);

        var payload = new
        {
            content = message,
            embeds = IncludeEmbed ? BuildEmbed(context) : null
        };

        using var client = new HttpClient();
        await client.PostAsJsonAsync(WebhookUrl, payload);
    }

    private object[]? BuildEmbed(AlertContext context)
    {
        return new[]
        {
            new
            {
                title = context.Rule.Name,
                color = GetPriorityColor(context.Rule.Priority),
                fields = context.Variables
                    .Take(5)
                    .Select(v => new { name = v.Key, value = v.Value.ToString(), inline = true })
                    .ToArray(),
                timestamp = context.Timestamp.ToString("o")
            }
        };
    }
}
```

### Alert Configuration UI Model

```csharp
public class AlertBuilderViewModel : ViewModelBase
{
    [ObservableProperty] private string _name = "";
    [ObservableProperty] private bool _enabled = true;
    [ObservableProperty] private AlertPriority _priority = AlertPriority.Medium;
    [ObservableProperty] private ConditionLogic _logic = ConditionLogic.All;
    [ObservableProperty] private int _cooldownSeconds = 5;

    public ObservableCollection<ConditionViewModel> Conditions { get; } = new();
    public ObservableCollection<NotificationViewModel> Notifications { get; } = new();

    [RelayCommand]
    private void AddCondition(string conditionType)
    {
        var condition = ConditionFactory.Create(conditionType);
        Conditions.Add(new ConditionViewModel(condition));
    }

    [RelayCommand]
    private void AddNotification(string notificationType)
    {
        var notification = NotificationFactory.Create(notificationType);
        Notifications.Add(new NotificationViewModel(notification));
    }

    [RelayCommand]
    private void TestAlert()
    {
        var rule = BuildRule();
        var context = new AlertContext(rule, DateTime.Now, null, new CombatState(), new());

        foreach (var notification in rule.Notifications)
        {
            _ = notification.ExecuteAsync(context);
        }
    }

    public AlertRule BuildRule()
    {
        return new AlertRule(
            Guid.NewGuid().ToString(),
            Name,
            Enabled,
            Priority,
            Conditions.Select(c => c.Build()).ToList(),
            Logic,
            TimeSpan.FromSeconds(CooldownSeconds),
            Notifications.Select(n => n.Build()).ToList()
        );
    }
}
```
