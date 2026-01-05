namespace CamelotCombatReporter.Core.BuffTracking.Models;

/// <summary>
/// Categories of buff/debuff effects.
/// </summary>
public enum BuffCategory
{
    // Beneficial effects
    /// <summary>Stat increases (STR, DEX, etc.).</summary>
    StatBuff,
    /// <summary>Armor factor increases.</summary>
    ArmorBuff,
    /// <summary>Resistance increases.</summary>
    ResistanceBuff,
    /// <summary>Damage add bonuses.</summary>
    DamageAddBuff,
    /// <summary>To-hit bonuses.</summary>
    ToHitBuff,
    /// <summary>Movement speed increases.</summary>
    SpeedBuff,
    /// <summary>Attack speed increases (haste/celerity).</summary>
    HasteBuff,
    /// <summary>HP/Power/Endurance regeneration.</summary>
    RegenerationBuff,
    /// <summary>Concentration slot buffs.</summary>
    ConcentrationBuff,
    /// <summary>Effects from realm abilities.</summary>
    RealmAbilityBuff,

    // Detrimental effects
    /// <summary>Stat decreases.</summary>
    StatDebuff,
    /// <summary>Resistance decreases.</summary>
    ResistDebuff,
    /// <summary>Movement speed decreases (snare).</summary>
    SpeedDebuff,
    /// <summary>Periodic damage effects (poison, DoT).</summary>
    DamageOverTime,
    /// <summary>Disease effects (HP debuff, heal reduction).</summary>
    Disease,
    /// <summary>Bleed effects.</summary>
    Bleed,
    /// <summary>Armor factor decreases.</summary>
    ArmorDebuff,

    // Other
    /// <summary>Utility effects.</summary>
    Utility,
    /// <summary>Unknown category.</summary>
    Unknown
}

/// <summary>
/// Type of buff event.
/// </summary>
public enum BuffEventType
{
    /// <summary>Buff was applied.</summary>
    Applied,
    /// <summary>Buff was refreshed (extended duration).</summary>
    Refreshed,
    /// <summary>Buff expired naturally.</summary>
    Expired,
    /// <summary>Buff was removed (dispelled or cancelled).</summary>
    Removed,
    /// <summary>Buff was resisted.</summary>
    Resisted
}

/// <summary>
/// Target type for buff effects.
/// </summary>
public enum BuffTargetType
{
    /// <summary>Applied to self.</summary>
    Self,
    /// <summary>Applied to an ally.</summary>
    Ally,
    /// <summary>Applied to an enemy.</summary>
    Enemy,
    /// <summary>Applied to a pet.</summary>
    Pet
}

/// <summary>
/// Stacking behavior for buffs.
/// </summary>
public enum BuffStackingRule
{
    /// <summary>Buffs don't stack - new application replaces old.</summary>
    NoStack,
    /// <summary>Buffs stack by extending duration.</summary>
    StackDuration,
    /// <summary>Buffs stack by increasing intensity.</summary>
    StackIntensity,
    /// <summary>Higher magnitude buff wins.</summary>
    HigherWins,
    /// <summary>Buffs occupy separate slots.</summary>
    SeparateSlot
}

/// <summary>
/// Concentration type for concentration-based buffs.
/// </summary>
public enum ConcentrationType
{
    /// <summary>Not a concentration buff.</summary>
    None,
    /// <summary>Self concentration slot.</summary>
    Self,
    /// <summary>Group concentration slot.</summary>
    GroupSlot,
    /// <summary>Realm ability concentration.</summary>
    RealmAbility
}
