using CamelotCombatReporter.Core.Alerts.Models;

namespace CamelotCombatReporter.Core.Alerts;

/// <summary>
/// Root configuration for the alert system.
/// </summary>
/// <param name="Rules">Configured alert rules.</param>
/// <param name="GlobalMute">Whether all sounds are muted.</param>
/// <param name="MasterVolume">Master volume level (0.0 to 1.0).</param>
/// <param name="TtsEnabled">Whether text-to-speech is enabled.</param>
public record AlertConfiguration(
    IReadOnlyList<AlertRuleDto> Rules,
    bool GlobalMute,
    float MasterVolume,
    bool TtsEnabled
);

/// <summary>
/// Data transfer object for serializing an alert rule.
/// </summary>
/// <param name="Id">Unique identifier for the rule.</param>
/// <param name="Name">Display name for the rule.</param>
/// <param name="Description">Description of what this rule alerts on.</param>
/// <param name="Priority">Priority level for this alert.</param>
/// <param name="Logic">How to combine multiple conditions (AND/OR).</param>
/// <param name="Conditions">List of condition configurations.</param>
/// <param name="Notifications">List of notification configurations.</param>
/// <param name="CooldownSeconds">Minimum seconds between triggers of this rule.</param>
/// <param name="State">Current state of the rule (Active/Paused/Disabled).</param>
/// <param name="MaxTriggersPerSession">Optional limit on triggers per session.</param>
/// <param name="RequiresCombat">Whether the rule only triggers during combat.</param>
public record AlertRuleDto(
    Guid Id,
    string Name,
    string Description,
    AlertPriority Priority,
    ConditionLogic Logic,
    IReadOnlyList<ConditionDto> Conditions,
    IReadOnlyList<NotificationDto> Notifications,
    int CooldownSeconds,
    AlertRuleState State,
    int? MaxTriggersPerSession,
    bool RequiresCombat
);

/// <summary>
/// Data transfer object for serializing a condition.
/// </summary>
/// <param name="Type">Type identifier for the condition (e.g., "HealthBelow").</param>
/// <param name="Parameters">Condition-specific parameters.</param>
public record ConditionDto(
    string Type,
    Dictionary<string, object> Parameters
);

/// <summary>
/// Data transfer object for serializing a notification.
/// </summary>
/// <param name="Type">Type identifier for the notification (e.g., "Sound").</param>
/// <param name="IsEnabled">Whether this notification is enabled.</param>
/// <param name="Settings">Notification-specific settings.</param>
public record NotificationDto(
    string Type,
    bool IsEnabled,
    Dictionary<string, object> Settings
);
