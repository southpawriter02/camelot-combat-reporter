using CamelotCombatReporter.Core.Models;

namespace CamelotCombatReporter.Core.RealmAbilities.Models;

/// <summary>
/// Represents a realm ability activation or effect event parsed from logs.
/// </summary>
/// <param name="Timestamp">The time the event occurred.</param>
/// <param name="AbilityName">The name of the realm ability.</param>
/// <param name="SourceName">The name of the entity that activated the ability.</param>
/// <param name="TargetName">The target of the ability effect, if applicable.</param>
/// <param name="EffectValue">The numeric effect value (damage, healing), if applicable.</param>
/// <param name="EffectType">The type of effect (damage type, heal), if applicable.</param>
/// <param name="IsActivation">True if this is the activation event, false if it's an effect.</param>
public record RealmAbilityEvent(
    TimeOnly Timestamp,
    string AbilityName,
    string SourceName,
    string? TargetName = null,
    int? EffectValue = null,
    string? EffectType = null,
    bool IsActivation = true
) : LogEvent(Timestamp);

/// <summary>
/// Represents a realm ability becoming ready (off cooldown).
/// </summary>
/// <param name="Timestamp">The time the ability became ready.</param>
/// <param name="AbilityName">The name of the realm ability.</param>
public record RealmAbilityReadyEvent(
    TimeOnly Timestamp,
    string AbilityName
) : LogEvent(Timestamp);
