using CamelotCombatReporter.Core.Alerts.Conditions;
using CamelotCombatReporter.Core.Alerts.Models;
using CamelotCombatReporter.Core.Alerts.Notifications;
using CamelotCombatReporter.Core.BuffTracking.Models;
using CamelotCombatReporter.Core.Models;

namespace CamelotCombatReporter.Core.Alerts;

/// <summary>
/// Event args for alert triggered events.
/// </summary>
public record AlertTriggeredEventArgs(AlertContext Context);

/// <summary>
/// Event args for combat state changed events.
/// </summary>
public record CombatStateChangedEventArgs(CombatState State, LogEvent TriggeringEvent);

/// <summary>
/// Core engine for processing events and triggering alerts based on configurable rules.
/// </summary>
public class AlertEngine : IDisposable
{
    private readonly List<AlertRule> _rules = new();
    private readonly Dictionary<Guid, TimeOnly> _lastTriggered = new();
    private readonly Dictionary<Guid, int> _triggerCounts = new();
    private readonly CombatState _state = new();
    private readonly List<AlertTrigger> _triggerHistory = new();
    private readonly object _lock = new();
    private bool _disposed;

    /// <summary>
    /// Raised when an alert is triggered.
    /// </summary>
    public event EventHandler<AlertTriggeredEventArgs>? AlertTriggered;

    /// <summary>
    /// Raised when the combat state changes.
    /// </summary>
    public event EventHandler<CombatStateChangedEventArgs>? StateChanged;

    /// <summary>
    /// Gets all configured rules.
    /// </summary>
    public IReadOnlyList<AlertRule> Rules
    {
        get
        {
            lock (_lock)
            {
                return _rules.ToList();
            }
        }
    }

    /// <summary>
    /// Gets the current combat state.
    /// </summary>
    public CombatState CurrentState => _state;

    /// <summary>
    /// Gets the trigger history.
    /// </summary>
    public IReadOnlyList<AlertTrigger> TriggerHistory
    {
        get
        {
            lock (_lock)
            {
                return _triggerHistory.ToList();
            }
        }
    }

    /// <summary>
    /// Whether the engine is currently processing events.
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    #region Rule Management

    /// <summary>
    /// Adds a rule to the engine.
    /// </summary>
    public void AddRule(AlertRule rule)
    {
        lock (_lock)
        {
            _rules.Add(rule);
        }
    }

    /// <summary>
    /// Removes a rule from the engine.
    /// </summary>
    public void RemoveRule(Guid ruleId)
    {
        lock (_lock)
        {
            _rules.RemoveAll(r => r.Id == ruleId);
            _lastTriggered.Remove(ruleId);
            _triggerCounts.Remove(ruleId);
        }
    }

    /// <summary>
    /// Updates an existing rule.
    /// </summary>
    public void UpdateRule(AlertRule updatedRule)
    {
        lock (_lock)
        {
            var index = _rules.FindIndex(r => r.Id == updatedRule.Id);
            if (index >= 0)
            {
                _rules[index] = updatedRule;
            }
        }
    }

    /// <summary>
    /// Sets the state of a rule.
    /// </summary>
    public void SetRuleState(Guid ruleId, AlertRuleState state)
    {
        lock (_lock)
        {
            var index = _rules.FindIndex(r => r.Id == ruleId);
            if (index >= 0)
            {
                var rule = _rules[index];
                _rules[index] = rule with { State = state };
            }
        }
    }

    /// <summary>
    /// Clears all rules.
    /// </summary>
    public void ClearRules()
    {
        lock (_lock)
        {
            _rules.Clear();
            _lastTriggered.Clear();
            _triggerCounts.Clear();
        }
    }

    #endregion

    #region Event Processing

    /// <summary>
    /// Processes a log event, updating state and evaluating rules.
    /// </summary>
    public async Task ProcessEventAsync(LogEvent logEvent, CancellationToken cancellationToken = default)
    {
        if (!IsEnabled || _disposed)
            return;

        // Update combat state based on the event
        UpdateCombatState(logEvent);
        StateChanged?.Invoke(this, new CombatStateChangedEventArgs(_state, logEvent));

        // Evaluate rules and collect triggered ones
        var triggeredRules = EvaluateRules(logEvent);

        // Process triggered rules in priority order
        foreach (var (rule, reason, data) in triggeredRules.OrderByDescending(r => r.Rule.Priority))
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            var context = new AlertContext(
                rule,
                logEvent.Timestamp,
                reason,
                data,
                logEvent);

            var trigger = new AlertTrigger(
                Guid.NewGuid(),
                rule.Id,
                logEvent.Timestamp,
                reason,
                rule.Priority,
                WasNotified: true);

            lock (_lock)
            {
                _triggerHistory.Add(trigger);
                _lastTriggered[rule.Id] = logEvent.Timestamp;
                _triggerCounts[rule.Id] = _triggerCounts.GetValueOrDefault(rule.Id) + 1;

                // Keep history bounded
                if (_triggerHistory.Count > 1000)
                    _triggerHistory.RemoveRange(0, 100);
            }

            AlertTriggered?.Invoke(this, new AlertTriggeredEventArgs(context));

            // Execute notifications
            await ExecuteNotificationsAsync(rule, context, cancellationToken);
        }
    }

    private void UpdateCombatState(LogEvent logEvent)
    {
        _state.LastEventTime = logEvent.Timestamp;

        switch (logEvent)
        {
            case DamageEvent damage:
                HandleDamageEvent(damage);
                break;

            case HealingEvent healing:
                HandleHealingEvent(healing);
                break;

            case DeathEvent death:
                HandleDeathEvent(death);
                break;

            case BuffEvent buff:
                HandleBuffEvent(buff);
                break;
        }
    }

    private void HandleDamageEvent(DamageEvent damage)
    {
        // Track if player receives damage
        if (IsPlayerTarget(damage.Target))
        {
            _state.RecordDamageReceived(damage.Timestamp, damage.DamageAmount);

            // Estimate health reduction (rough heuristic)
            // In a real implementation, this would track actual health values
            _state.CurrentHealthPercent = Math.Max(0, _state.CurrentHealthPercent - (damage.DamageAmount / 50.0));
        }

        // Track if player deals damage
        if (IsPlayerSource(damage.Source))
        {
            _state.RecordDamageDealt(damage.Timestamp, damage.DamageAmount);

            // Update target info
            if (!IsPlayerTarget(damage.Target))
            {
                _state.CurrentTargetName = damage.Target;
            }
        }

        // Combat started
        if (!_state.IsInCombat)
        {
            _state.IsInCombat = true;
            _state.CombatStartTime = damage.Timestamp;
        }
    }

    private void HandleHealingEvent(HealingEvent healing)
    {
        if (IsPlayerTarget(healing.Target))
        {
            _state.RecordHealingReceived(healing.Timestamp, healing.HealingAmount);

            // Estimate health increase
            _state.CurrentHealthPercent = Math.Min(100, _state.CurrentHealthPercent + (healing.HealingAmount / 50.0));
        }
    }

    private void HandleDeathEvent(DeathEvent death)
    {
        if (IsPlayerTarget(death.Target))
        {
            _state.RecordDeath();
            _state.CurrentHealthPercent = 0;
            _state.IsInCombat = false;
            _state.CombatStartTime = null;
        }
        else if (IsPlayerSource(death.Killer))
        {
            _state.RecordKill();
        }
    }

    private void HandleBuffEvent(BuffEvent buff)
    {
        if (!IsPlayerTarget(buff.TargetName))
            return;

        var buffName = buff.BuffDefinition.Name;
        var isDebuff = buff.BuffDefinition.Category >= BuffCategory.StatDebuff;

        if (buff.EventType == BuffEventType.Applied)
        {
            if (isDebuff)
            {
                _state.ActiveDebuffs = _state.ActiveDebuffs.Append(buffName).Distinct().ToList();
            }
            else
            {
                _state.ActiveBuffs = _state.ActiveBuffs.Append(buffName).Distinct().ToList();
            }
        }
        else if (buff.EventType is BuffEventType.Expired or BuffEventType.Removed)
        {
            if (isDebuff)
            {
                _state.ActiveDebuffs = _state.ActiveDebuffs.Where(b => b != buffName).ToList();
            }
            else
            {
                _state.ActiveBuffs = _state.ActiveBuffs.Where(b => b != buffName).ToList();
            }
        }
    }

    private static bool IsPlayerTarget(string? name) =>
        string.IsNullOrEmpty(name) || name.Equals("You", StringComparison.OrdinalIgnoreCase);

    private static bool IsPlayerSource(string? name) =>
        !string.IsNullOrEmpty(name) && name.Equals("You", StringComparison.OrdinalIgnoreCase);

    #endregion

    #region Rule Evaluation

    private List<(AlertRule Rule, string Reason, Dictionary<string, object> Data)> EvaluateRules(LogEvent logEvent)
    {
        var triggered = new List<(AlertRule, string, Dictionary<string, object>)>();

        lock (_lock)
        {
            foreach (var rule in _rules.Where(r => r.State == AlertRuleState.Active))
            {
                if (!ShouldEvaluateRule(rule, logEvent))
                    continue;

                var (isMet, reason, data) = EvaluateConditions(rule, logEvent);
                if (isMet)
                {
                    triggered.Add((rule, reason, data));
                }
            }
        }

        return triggered;
    }

    private bool ShouldEvaluateRule(AlertRule rule, LogEvent logEvent)
    {
        // Check cooldown
        if (_lastTriggered.TryGetValue(rule.Id, out var lastTime))
        {
            var elapsed = logEvent.Timestamp.ToTimeSpan() - lastTime.ToTimeSpan();

            // Handle day rollover
            if (elapsed < TimeSpan.Zero)
                elapsed += TimeSpan.FromHours(24);

            if (elapsed < rule.Cooldown)
                return false;
        }

        // Check max triggers per session
        if (rule.MaxTriggersPerSession.HasValue &&
            _triggerCounts.GetValueOrDefault(rule.Id) >= rule.MaxTriggersPerSession.Value)
        {
            return false;
        }

        // Check combat requirement
        if (rule.RequiresCombat && !_state.IsInCombat)
            return false;

        return true;
    }

    private (bool IsMet, string Reason, Dictionary<string, object> Data) EvaluateConditions(
        AlertRule rule,
        LogEvent logEvent)
    {
        var allData = new Dictionary<string, object>();
        var reasons = new List<string>();
        var results = new List<bool>();

        foreach (var condition in rule.Conditions)
        {
            var (isMet, reason, data) = condition.Evaluate(_state, logEvent);
            results.Add(isMet);

            // Namespace the data to avoid collisions
            foreach (var kvp in data)
            {
                allData[$"{condition.ConditionType}_{kvp.Key}"] = kvp.Value;
            }

            if (isMet && !string.IsNullOrEmpty(reason))
            {
                reasons.Add(reason);
            }
        }

        if (results.Count == 0)
            return (false, string.Empty, allData);

        bool finalResult = rule.Logic switch
        {
            ConditionLogic.And => results.All(r => r),
            ConditionLogic.Or => results.Any(r => r),
            _ => false
        };

        return (finalResult, string.Join("; ", reasons), allData);
    }

    #endregion

    #region Notification Execution

    private static async Task ExecuteNotificationsAsync(
        AlertRule rule,
        AlertContext context,
        CancellationToken cancellationToken)
    {
        foreach (var notification in rule.Notifications.Where(n => n.IsEnabled))
        {
            try
            {
                await notification.ExecuteAsync(context, cancellationToken);
            }
            catch
            {
                // Log but continue with other notifications
            }
        }
    }

    #endregion

    #region Session Management

    /// <summary>
    /// Resets the engine for a new session.
    /// </summary>
    public void ResetSession()
    {
        lock (_lock)
        {
            _state.Reset();
            _lastTriggered.Clear();
            _triggerCounts.Clear();
            _triggerHistory.Clear();
        }
    }

    /// <summary>
    /// Clears trigger counts and history while keeping state.
    /// </summary>
    public void ClearTriggerHistory()
    {
        lock (_lock)
        {
            _triggerHistory.Clear();
            _triggerCounts.Clear();
        }
    }

    #endregion

    #region IDisposable

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _rules.Clear();
        _lastTriggered.Clear();
        _triggerCounts.Clear();
        _triggerHistory.Clear();

        GC.SuppressFinalize(this);
    }

    #endregion
}
