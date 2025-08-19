namespace CamelotCombatReporter.Core.Models;

/// <summary>
/// Represents a spell cast event.
/// </summary>
/// <param name="Timestamp">The time the event occurred.</param>
/// <param name="Source">The source of the spell.</param>
/// <param name="Target">The target of the spell.</param>
/// <param name="SpellName">The name of the spell cast.</param>
public record SpellCastEvent(
    TimeOnly Timestamp,
    string Source,
    string Target,
    string SpellName
) : LogEvent(Timestamp);
