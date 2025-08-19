namespace CamelotCombatReporter.Core.Models;

/// <summary>
/// Represents a damage event.
/// </summary>
/// <param name="Timestamp">The time the event occurred.</param>
/// <param name="Source">The source of the damage.</param>
/// <param name="Target">The target of the damage.</param>
/// <param name="DamageAmount">The amount of damage dealt.</param>
/// <param name="DamageType">The type of damage (e.g., Crush, Slash, Heat).</param>
public record DamageEvent(
    TimeOnly Timestamp,
    string Source,
    string Target,
    int DamageAmount,
    string DamageType
) : LogEvent(Timestamp);
