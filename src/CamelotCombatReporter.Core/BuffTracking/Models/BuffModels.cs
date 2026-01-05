using CamelotCombatReporter.Core.Models;

namespace CamelotCombatReporter.Core.BuffTracking.Models;

/// <summary>
/// Definition of a known buff or debuff type.
/// </summary>
/// <param name="BuffId">Unique identifier for this buff type.</param>
/// <param name="Name">Display name.</param>
/// <param name="Category">Category of the buff.</param>
/// <param name="DefaultDuration">Default duration in seconds.</param>
/// <param name="StackingRule">How the buff stacks with itself.</param>
/// <param name="ConcentrationType">Whether it uses concentration.</param>
/// <param name="IsExpectedBuff">Whether gaps should be tracked.</param>
/// <param name="LogPatterns">Log message patterns that indicate this buff.</param>
/// <param name="Description">Description of the buff effect.</param>
public record BuffDefinition(
    string BuffId,
    string Name,
    BuffCategory Category,
    int DefaultDuration,
    BuffStackingRule StackingRule,
    ConcentrationType ConcentrationType,
    bool IsExpectedBuff,
    IReadOnlyList<string> LogPatterns,
    string? Description = null
)
{
    /// <summary>
    /// Whether this is a beneficial effect.
    /// </summary>
    public bool IsBeneficial => Category switch
    {
        BuffCategory.StatBuff => true,
        BuffCategory.ArmorBuff => true,
        BuffCategory.ResistanceBuff => true,
        BuffCategory.DamageAddBuff => true,
        BuffCategory.ToHitBuff => true,
        BuffCategory.SpeedBuff => true,
        BuffCategory.HasteBuff => true,
        BuffCategory.RegenerationBuff => true,
        BuffCategory.ConcentrationBuff => true,
        BuffCategory.RealmAbilityBuff => true,
        BuffCategory.Utility => true,
        _ => false
    };
}

/// <summary>
/// A buff event parsed from combat logs.
/// </summary>
/// <param name="Timestamp">When the event occurred.</param>
/// <param name="BuffDefinition">The buff type.</param>
/// <param name="EventType">Type of buff event.</param>
/// <param name="TargetType">Whether applied to self, ally, enemy, or pet.</param>
/// <param name="TargetName">Name of the target.</param>
/// <param name="SourceName">Name of the buff source, if known.</param>
/// <param name="Duration">Duration if specified in the log.</param>
/// <param name="Magnitude">Magnitude if specified in the log.</param>
public record BuffEvent(
    TimeOnly Timestamp,
    BuffDefinition BuffDefinition,
    BuffEventType EventType,
    BuffTargetType TargetType,
    string TargetName,
    string? SourceName,
    int? Duration,
    int? Magnitude
) : LogEvent(Timestamp)
{
    /// <summary>
    /// Unique ID for this event instance.
    /// </summary>
    public Guid Id { get; } = Guid.NewGuid();
}

/// <summary>
/// Represents a currently active buff.
/// </summary>
/// <param name="BuffDefinition">The buff type.</param>
/// <param name="TargetName">Who has the buff.</param>
/// <param name="TargetType">Target type.</param>
/// <param name="AppliedAt">When it was applied.</param>
/// <param name="ExpiresAt">When it is estimated to expire.</param>
/// <param name="SourceName">Who applied it.</param>
/// <param name="RefreshCount">Number of times refreshed.</param>
/// <param name="Magnitude">Current magnitude.</param>
public record ActiveBuff(
    BuffDefinition BuffDefinition,
    string TargetName,
    BuffTargetType TargetType,
    TimeOnly AppliedAt,
    TimeOnly ExpiresAt,
    string? SourceName,
    int RefreshCount,
    int? Magnitude
);

/// <summary>
/// Represents a gap in expected buff uptime.
/// </summary>
/// <param name="BuffDefinition">The buff that was missing.</param>
/// <param name="TargetName">Who was missing the buff.</param>
/// <param name="GapStart">When the gap started.</param>
/// <param name="GapEnd">When the gap ended (null if still ongoing).</param>
/// <param name="GapDuration">Duration of the gap.</param>
/// <param name="Context">Context about what happened during the gap.</param>
public record BuffGap(
    BuffDefinition BuffDefinition,
    string TargetName,
    TimeOnly GapStart,
    TimeOnly? GapEnd,
    TimeSpan GapDuration,
    string? Context
);

/// <summary>
/// Statistics for buff uptime.
/// </summary>
/// <param name="BuffDefinition">The buff being tracked.</param>
/// <param name="TargetName">Who was tracked.</param>
/// <param name="TotalCombatTime">Total combat duration.</param>
/// <param name="TotalBuffedTime">Time with buff active.</param>
/// <param name="UptimePercent">Percentage of time buffed.</param>
/// <param name="ApplicationCount">Number of times applied.</param>
/// <param name="RefreshCount">Number of refreshes.</param>
/// <param name="AverageGapDuration">Average gap duration.</param>
/// <param name="LongestGap">Longest gap.</param>
/// <param name="Gaps">All gaps.</param>
public record BuffUptimeStats(
    BuffDefinition BuffDefinition,
    string TargetName,
    TimeSpan TotalCombatTime,
    TimeSpan TotalBuffedTime,
    double UptimePercent,
    int ApplicationCount,
    int RefreshCount,
    TimeSpan AverageGapDuration,
    BuffGap? LongestGap,
    IReadOnlyList<BuffGap> Gaps
);

/// <summary>
/// Overall buff statistics for a session.
/// </summary>
/// <param name="TotalBuffsApplied">Total beneficial buffs applied.</param>
/// <param name="TotalDebuffsApplied">Total debuffs applied to enemies.</param>
/// <param name="TotalDebuffsReceived">Total debuffs received.</param>
/// <param name="OverallBuffUptime">Average uptime across expected buffs.</param>
/// <param name="UptimeByBuff">Uptime stats per buff.</param>
/// <param name="BuffsByCategory">Count by category for beneficial.</param>
/// <param name="DebuffsByCategory">Count by category for debuffs.</param>
/// <param name="CriticalGaps">Gaps that may have impacted performance.</param>
/// <param name="MostUsedBuffs">Most frequently applied buffs.</param>
/// <param name="MostUsedDebuffs">Most frequently applied debuffs.</param>
public record BuffStatistics(
    int TotalBuffsApplied,
    int TotalDebuffsApplied,
    int TotalDebuffsReceived,
    double OverallBuffUptime,
    IReadOnlyDictionary<string, BuffUptimeStats> UptimeByBuff,
    IReadOnlyDictionary<BuffCategory, int> BuffsByCategory,
    IReadOnlyDictionary<BuffCategory, int> DebuffsByCategory,
    IReadOnlyList<BuffGap> CriticalGaps,
    IReadOnlyList<(BuffDefinition Buff, int Count)> MostUsedBuffs,
    IReadOnlyList<(BuffDefinition Debuff, int Count)> MostUsedDebuffs
);

/// <summary>
/// Timeline entry for buff visualization.
/// </summary>
/// <param name="Timestamp">When the event occurred.</param>
/// <param name="BuffName">Name of the buff.</param>
/// <param name="Category">Buff category.</param>
/// <param name="EventType">Type of event.</param>
/// <param name="TargetName">Target of the buff.</param>
/// <param name="TargetType">Target type.</param>
/// <param name="SourceName">Source if known.</param>
/// <param name="Duration">Duration if known.</param>
/// <param name="DisplayColor">Color for display.</param>
/// <param name="IsBeneficial">Whether the effect is beneficial.</param>
public record BuffTimelineEntry(
    TimeOnly Timestamp,
    string BuffName,
    BuffCategory Category,
    BuffEventType EventType,
    string TargetName,
    BuffTargetType TargetType,
    string? SourceName,
    TimeSpan? Duration,
    string DisplayColor,
    bool IsBeneficial
)
{
    /// <summary>
    /// Gets a display color based on category and whether beneficial.
    /// </summary>
    public static string GetColorForCategory(BuffCategory category, bool isBeneficial)
    {
        if (!isBeneficial)
        {
            return category switch
            {
                BuffCategory.DamageOverTime => "#FF0000",  // Red
                BuffCategory.Disease => "#8B4513",         // Saddle brown
                BuffCategory.Bleed => "#DC143C",           // Crimson
                BuffCategory.StatDebuff => "#FF6600",      // Orange
                BuffCategory.ResistDebuff => "#FF9900",    // Lighter orange
                BuffCategory.ArmorDebuff => "#CC6600",     // Darker orange
                BuffCategory.SpeedDebuff => "#9966FF",     // Purple
                _ => "#FF6666"                             // Light red
            };
        }

        return category switch
        {
            BuffCategory.StatBuff => "#00CC00",            // Green
            BuffCategory.ArmorBuff => "#0066CC",           // Blue
            BuffCategory.ResistanceBuff => "#6600CC",      // Purple
            BuffCategory.DamageAddBuff => "#CC0000",       // Dark red
            BuffCategory.SpeedBuff => "#00CCCC",           // Cyan
            BuffCategory.HasteBuff => "#CCCC00",           // Yellow
            BuffCategory.RegenerationBuff => "#00FF66",    // Bright green
            BuffCategory.RealmAbilityBuff => "#FF00FF",    // Magenta
            _ => "#FFFFFF"                                 // White
        };
    }
}
