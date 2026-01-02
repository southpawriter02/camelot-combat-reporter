namespace CamelotCombatReporter.Core.Models;

/// <summary>
/// Represents a healing event.
/// </summary>
/// <param name="Timestamp">The time the event occurred.</param>
/// <param name="Source">The source of the healing.</param>
/// <param name="Target">The target of the healing.</param>
/// <param name="HealingAmount">The amount of healing done.</param>
public record HealingEvent(
    TimeOnly Timestamp,
    string Source,
    string Target,
    int HealingAmount
) : LogEvent(Timestamp);
