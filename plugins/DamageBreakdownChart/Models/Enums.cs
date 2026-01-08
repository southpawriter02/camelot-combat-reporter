namespace DamageBreakdownChart.Models;

/// <summary>
/// Type of node in the damage hierarchy.
/// </summary>
public enum DamageNodeType
{
    /// <summary>Root node containing all damage.</summary>
    Root,

    /// <summary>Damage type (Slash, Crush, Heat, etc.).</summary>
    DamageType,

    /// <summary>Ability category (Combat Style, Spell, Auto-attack).</summary>
    AbilityCategory,

    /// <summary>Specific ability name.</summary>
    AbilityName,

    /// <summary>Target entity.</summary>
    Target,

    /// <summary>Individual damage event.</summary>
    Event
}
