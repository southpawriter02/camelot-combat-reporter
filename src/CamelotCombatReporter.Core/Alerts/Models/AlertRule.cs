using CamelotCombatReporter.Core.Alerts.Conditions;
using CamelotCombatReporter.Core.Alerts.Notifications;
using CamelotCombatReporter.Core.Models;

namespace CamelotCombatReporter.Core.Alerts.Models;

/// <summary>
/// Defines a configurable alert rule with conditions and notifications.
/// </summary>
/// <param name="Id">Unique identifier for the rule.</param>
/// <param name="Name">Display name for the rule.</param>
/// <param name="Description">Description of what this rule alerts on.</param>
/// <param name="Priority">Priority level for this alert.</param>
/// <param name="Logic">How to combine multiple conditions (AND/OR).</param>
/// <param name="Conditions">List of conditions that must be met.</param>
/// <param name="Notifications">List of notifications to trigger when conditions are met.</param>
/// <param name="Cooldown">Minimum time between triggers of this rule.</param>
/// <param name="State">Current state of the rule (Active/Paused/Disabled).</param>
/// <param name="MaxTriggersPerSession">Optional limit on triggers per session.</param>
/// <param name="RequiresCombat">Whether the rule only triggers during combat.</param>
public record AlertRule(
    Guid Id,
    string Name,
    string Description,
    AlertPriority Priority,
    ConditionLogic Logic,
    IReadOnlyList<IAlertCondition> Conditions,
    IReadOnlyList<INotification> Notifications,
    TimeSpan Cooldown,
    AlertRuleState State = AlertRuleState.Active,
    int? MaxTriggersPerSession = null,
    bool RequiresCombat = true
);

/// <summary>
/// Context provided to notifications when an alert is triggered.
/// </summary>
/// <param name="Rule">The rule that triggered.</param>
/// <param name="Timestamp">When the alert triggered.</param>
/// <param name="TriggerReason">Human-readable explanation of why the alert triggered.</param>
/// <param name="ConditionData">Data from condition evaluations.</param>
/// <param name="TriggeringEvent">The log event that caused the trigger, if applicable.</param>
public record AlertContext(
    AlertRule Rule,
    TimeOnly Timestamp,
    string TriggerReason,
    IReadOnlyDictionary<string, object> ConditionData,
    LogEvent? TriggeringEvent = null
);

/// <summary>
/// Record of an alert trigger for history tracking.
/// </summary>
/// <param name="Id">Unique identifier for this trigger instance.</param>
/// <param name="RuleId">The rule that triggered.</param>
/// <param name="Timestamp">When the alert triggered.</param>
/// <param name="TriggerReason">Human-readable explanation of why the alert triggered.</param>
/// <param name="Priority">Priority of the triggered alert.</param>
/// <param name="WasNotified">Whether notifications were successfully sent.</param>
public record AlertTrigger(
    Guid Id,
    Guid RuleId,
    TimeOnly Timestamp,
    string TriggerReason,
    AlertPriority Priority,
    bool WasNotified
);
