namespace CamelotCombatReporter.Core.Models;

/// <summary>
/// Represents a critical hit event that follows a damage event.
/// </summary>
/// <param name="Timestamp">The time the event occurred.</param>
/// <param name="Target">The target of the critical hit, or null if not specified.</param>
/// <param name="DamageAmount">The additional damage from the critical hit.</param>
/// <param name="CritPercent">The critical hit percentage, if displayed (e.g., 20%).</param>
public record CriticalHitEvent(
    TimeOnly Timestamp,
    string? Target,
    int DamageAmount,
    int? CritPercent = null
) : LogEvent(Timestamp);

/// <summary>
/// Represents damage dealt by a player's pet.
/// </summary>
/// <param name="Timestamp">The time the event occurred.</param>
/// <param name="PetName">The name of the pet (e.g., "spirit warrior", "wolf sage").</param>
/// <param name="Target">The target of the attack.</param>
/// <param name="DamageAmount">The amount of damage dealt.</param>
/// <param name="Modifier">Optional damage modifier shown as (+N) or (-N).</param>
public record PetDamageEvent(
    TimeOnly Timestamp,
    string PetName,
    string Target,
    int DamageAmount,
    int? Modifier = null
) : LogEvent(Timestamp);

/// <summary>
/// Represents a death event when a mob or entity dies.
/// </summary>
/// <param name="Timestamp">The time the event occurred.</param>
/// <param name="Target">The name of the entity that died.</param>
/// <param name="Killer">The name of the killer, if specified.</param>
public record DeathEvent(
    TimeOnly Timestamp,
    string Target,
    string? Killer = null
) : LogEvent(Timestamp);

/// <summary>
/// Represents a resist event when an effect is resisted.
/// </summary>
/// <param name="Timestamp">The time the event occurred.</param>
/// <param name="Target">The name of the entity that resisted.</param>
public record ResistEvent(
    TimeOnly Timestamp,
    string Target
) : LogEvent(Timestamp);

/// <summary>
/// Represents a crowd control event (stun, mez, root, snare, silence, disarm).
/// </summary>
/// <param name="Timestamp">The time the event occurred.</param>
/// <param name="Target">The target of the crowd control effect.</param>
/// <param name="EffectType">The type of effect (e.g., "stun", "mez", "root", "snare", "silence", "disarm").</param>
/// <param name="IsApplied">True if the effect was applied, false if it wore off/was removed.</param>
/// <param name="Source">The source that applied the effect, if known.</param>
/// <param name="Duration">The duration of the effect in seconds, if known from the log message.</param>
public record CrowdControlEvent(
    TimeOnly Timestamp,
    string Target,
    string EffectType,
    bool IsApplied,
    string? Source = null,
    int? Duration = null
) : LogEvent(Timestamp);
