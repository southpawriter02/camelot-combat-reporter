namespace CamelotCombatReporter.Core.CrowdControlAnalysis.Models;

/// <summary>
/// Types of crowd control effects in DAoC.
/// </summary>
public enum CCType
{
    /// <summary>Mesmerize - target cannot act, breaks on damage.</summary>
    Mez,
    /// <summary>Stun - target cannot act, does not break on damage.</summary>
    Stun,
    /// <summary>Root - target cannot move but can attack/cast.</summary>
    Root,
    /// <summary>Snare - target movement speed reduced.</summary>
    Snare,
    /// <summary>Silence - target cannot cast spells.</summary>
    Silence,
    /// <summary>Disarm - target cannot use melee attacks.</summary>
    Disarm
}

/// <summary>
/// Diminishing Returns level for crowd control effects.
/// </summary>
public enum DRLevel
{
    /// <summary>Full duration (100%).</summary>
    Full = 100,
    /// <summary>Reduced duration (50%).</summary>
    Reduced = 50,
    /// <summary>Minimal duration (25%).</summary>
    Minimal = 25,
    /// <summary>Immune (0% - CC has no effect).</summary>
    Immune = 0
}

/// <summary>
/// Type of crowd control event.
/// </summary>
public enum CCEventType
{
    /// <summary>CC effect was applied to target.</summary>
    Applied,
    /// <summary>CC effect expired naturally.</summary>
    Expired,
    /// <summary>CC effect was broken (e.g., by damage).</summary>
    Broken,
    /// <summary>Target resisted the CC effect.</summary>
    Resisted,
    /// <summary>Target was immune to the CC effect (DR at 0%).</summary>
    Immune
}

/// <summary>
/// Reason a CC effect was broken.
/// </summary>
public enum CCBreakReason
{
    /// <summary>Unknown reason.</summary>
    Unknown,
    /// <summary>Broken by damage.</summary>
    Damage,
    /// <summary>Broken by purge/cleanse ability.</summary>
    Purge,
    /// <summary>Broken by using an ability.</summary>
    Ability,
    /// <summary>Expired due to duration.</summary>
    Duration
}

/// <summary>
/// Represents a CC application with DR information.
/// </summary>
/// <param name="Id">Unique identifier for this CC application.</param>
/// <param name="Timestamp">When the CC was applied.</param>
/// <param name="CrowdControlType">The type of CC effect.</param>
/// <param name="TargetName">The target that was CC'd.</param>
/// <param name="SourceName">The source that applied the CC, if known.</param>
/// <param name="BaseDuration">The base duration before DR is applied.</param>
/// <param name="DRAtApplication">The DR level when this CC was applied.</param>
/// <param name="EffectiveDuration">The actual duration after DR reduction.</param>
public record CCApplication(
    Guid Id,
    TimeOnly Timestamp,
    CCType CrowdControlType,
    string TargetName,
    string? SourceName,
    TimeSpan BaseDuration,
    DRLevel DRAtApplication,
    TimeSpan EffectiveDuration
);

/// <summary>
/// Represents a CC chain (multiple CC effects applied consecutively).
/// </summary>
/// <param name="Id">Unique identifier for this chain.</param>
/// <param name="StartTime">When the chain started.</param>
/// <param name="EndTime">When the chain ended.</param>
/// <param name="TargetName">The target of the chain.</param>
/// <param name="Applications">The CC applications in the chain.</param>
/// <param name="TotalDuration">Total duration the target was CC'd.</param>
/// <param name="GapTime">Total time between CC effects (gaps in the chain).</param>
/// <param name="OverlapTime">Total time where CC effects overlapped.</param>
/// <param name="ChainLength">Number of CC effects in the chain.</param>
/// <param name="EfficiencyPercent">Chain efficiency (less gaps = higher efficiency).</param>
public record CCChain(
    Guid Id,
    TimeOnly StartTime,
    TimeOnly EndTime,
    string TargetName,
    IReadOnlyList<CCApplication> Applications,
    TimeSpan TotalDuration,
    TimeSpan GapTime,
    TimeSpan OverlapTime,
    int ChainLength,
    double EfficiencyPercent
);

/// <summary>
/// Current DR state for a target/CC-type combination.
/// </summary>
/// <param name="TargetName">The target.</param>
/// <param name="CrowdControlType">The type of CC.</param>
/// <param name="CurrentLevel">The current DR level.</param>
/// <param name="LastCCTime">When the last CC was applied.</param>
/// <param name="TimeUntilReset">Time remaining until DR resets to Full.</param>
public record DRState(
    string TargetName,
    CCType CrowdControlType,
    DRLevel CurrentLevel,
    TimeOnly LastCCTime,
    TimeSpan TimeUntilReset
);

/// <summary>
/// Aggregated CC statistics for a combat session.
/// </summary>
/// <param name="TotalCCApplied">Total CC effects successfully applied.</param>
/// <param name="TotalCCResisted">Total CC effects that were resisted.</param>
/// <param name="TotalCCBroken">Total CC effects that were broken early.</param>
/// <param name="CCUptimePercent">Percentage of combat time targets were CC'd.</param>
/// <param name="AverageDuration">Average CC duration.</param>
/// <param name="BreakRatePercent">Percentage of CC effects broken early.</param>
/// <param name="DREfficiencyPercent">How efficiently DR was managed (higher = better).</param>
/// <param name="CCByType">Breakdown of CC applications by type.</param>
/// <param name="KillsWithinCCWindow">Kills that occurred within 5s of CC ending.</param>
/// <param name="TotalDamageDuringCC">Total damage dealt while targets were CC'd.</param>
public record CCStatistics(
    int TotalCCApplied,
    int TotalCCResisted,
    int TotalCCBroken,
    double CCUptimePercent,
    TimeSpan AverageDuration,
    double BreakRatePercent,
    double DREfficiencyPercent,
    IReadOnlyDictionary<CCType, int> CCByType,
    int KillsWithinCCWindow,
    int TotalDamageDuringCC
);

/// <summary>
/// Entry for CC timeline visualization.
/// </summary>
/// <param name="Timestamp">When this entry occurred.</param>
/// <param name="CrowdControlType">The type of CC.</param>
/// <param name="TargetName">The target.</param>
/// <param name="SourceName">The source, if known.</param>
/// <param name="EventType">The type of CC event.</param>
/// <param name="DRLevel">The DR level at this event.</param>
/// <param name="Duration">Duration of the effect, if applicable.</param>
/// <param name="DisplayColor">Color for UI display based on DR level.</param>
public record CCTimelineEntry(
    TimeOnly Timestamp,
    CCType CrowdControlType,
    string TargetName,
    string? SourceName,
    CCEventType EventType,
    DRLevel DRLevel,
    TimeSpan? Duration,
    string DisplayColor
)
{
    /// <summary>
    /// Gets the display color based on DR level.
    /// </summary>
    public static string GetColorForDRLevel(DRLevel level) => level switch
    {
        DRLevel.Full => "#4CAF50",      // Green
        DRLevel.Reduced => "#FFEB3B",   // Yellow
        DRLevel.Minimal => "#FF9800",   // Orange
        DRLevel.Immune => "#F44336",    // Red
        _ => "#9E9E9E"                  // Gray
    };
}
