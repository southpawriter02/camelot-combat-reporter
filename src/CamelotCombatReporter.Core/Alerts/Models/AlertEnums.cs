namespace CamelotCombatReporter.Core.Alerts.Models;

/// <summary>
/// Priority level for an alert rule.
/// Higher priority alerts are processed and notified first.
/// </summary>
public enum AlertPriority
{
    Low = 0,
    Medium = 1,
    High = 2,
    Critical = 3
}

/// <summary>
/// Logic for combining multiple conditions in an alert rule.
/// </summary>
public enum ConditionLogic
{
    /// <summary>All conditions must be true for the alert to trigger.</summary>
    And,
    /// <summary>Any condition being true will trigger the alert.</summary>
    Or
}

/// <summary>
/// State of an alert rule.
/// </summary>
public enum AlertRuleState
{
    /// <summary>Rule is active and will be evaluated.</summary>
    Active,
    /// <summary>Rule is temporarily paused (can be resumed).</summary>
    Paused,
    /// <summary>Rule is disabled and will not be evaluated.</summary>
    Disabled
}
