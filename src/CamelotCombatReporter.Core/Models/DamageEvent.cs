namespace CamelotCombatReporter.Core.Models;

/// <summary>
/// Represents a damage event.
/// </summary>
/// <param name="Timestamp">The time the event occurred.</param>
/// <param name="Source">The source of the damage.</param>
/// <param name="Target">The target of the damage.</param>
/// <param name="DamageAmount">The amount of damage dealt.</param>
/// <param name="DamageType">The type of damage (e.g., Crush, Slash, Heat).</param>
/// <param name="Modifier">Optional damage modifier shown as (+N) or (-N) in logs.</param>
/// <param name="BodyPart">Optional body part hit (e.g., "torso", "head").</param>
/// <param name="WeaponUsed">Optional weapon used for the attack (e.g., "sword", "bow").</param>
public record DamageEvent(
    TimeOnly Timestamp,
    string Source,
    string Target,
    int DamageAmount,
    string DamageType,
    int? Modifier = null,
    string? BodyPart = null,
    string? WeaponUsed = null
) : LogEvent(Timestamp);
