namespace CamelotCombatReporter.Core.Models;

/// <summary>
/// Represents a combat style event.
/// </summary>
/// <param name="Timestamp">The time the event occurred.</param>
/// <param name="Source">The source of the combat style.</param>
/// <param name="Target">The target of the combat style.</param>
/// <param name="StyleName">The name of the combat style used.</param>
public record CombatStyleEvent(
    TimeOnly Timestamp,
    string Source,
    string Target,
    string StyleName
) : LogEvent(Timestamp);
