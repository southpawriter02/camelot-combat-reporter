namespace CamelotCombatReporter.Core.Filtering;

/// <summary>
/// Shared context for filter pipeline, tracking combat state and configuration.
/// </summary>
public class FilterContext
{
    /// <summary>
    /// Timestamp of the last combat event (damage, healing, death, etc.).
    /// Used for context-aware filtering (e.g., keeping group chat during combat).
    /// </summary>
    public TimeOnly? LastCombatEventTime { get; set; }

    /// <summary>
    /// Whether the player is currently considered "in combat".
    /// Determined by recent combat events within the combat window.
    /// </summary>
    public bool IsInCombat { get; set; }

    /// <summary>
    /// Duration after last combat event to still be considered "in combat".
    /// Default is 15 seconds.
    /// </summary>
    public TimeSpan CombatContextWindow { get; set; } = TimeSpan.FromSeconds(15);

    /// <summary>
    /// Current line being processed (for multi-line context).
    /// </summary>
    public string? CurrentLine { get; set; }

    /// <summary>
    /// Current line number being processed.
    /// </summary>
    public int CurrentLineNumber { get; set; }

    /// <summary>
    /// Updates combat state based on a combat event timestamp.
    /// </summary>
    /// <param name="eventTime">The timestamp of the combat event.</param>
    public void RegisterCombatEvent(TimeOnly eventTime)
    {
        LastCombatEventTime = eventTime;
        IsInCombat = true;
    }

    /// <summary>
    /// Updates combat state based on current time.
    /// Should be called periodically to decay combat state.
    /// </summary>
    /// <param name="currentTime">The current line timestamp.</param>
    public void UpdateCombatState(TimeOnly currentTime)
    {
        if (LastCombatEventTime.HasValue)
        {
            var elapsed = currentTime - LastCombatEventTime.Value;
            IsInCombat = elapsed <= CombatContextWindow;
        }
    }

    /// <summary>
    /// Resets context to initial state.
    /// </summary>
    public void Reset()
    {
        LastCombatEventTime = null;
        IsInCombat = false;
        CurrentLine = null;
        CurrentLineNumber = 0;
    }
}
