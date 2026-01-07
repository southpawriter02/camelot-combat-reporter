using CamelotCombatReporter.Core.Models;

namespace CamelotCombatReporter.Core.CharacterBuilding.Templates;

/// <summary>
/// Defines available specialization lines for a character class.
/// </summary>
public record SpecializationTemplate
{
    /// <summary>
    /// The character class this template applies to.
    /// </summary>
    public required CharacterClass Class { get; init; }

    /// <summary>
    /// Available specialization lines for this class.
    /// </summary>
    public IReadOnlyList<SpecLine> SpecLines { get; init; } = [];
}

/// <summary>
/// A single specialization line within a class template.
/// </summary>
public record SpecLine
{
    /// <summary>
    /// Name of the specialization line (e.g., "Polearm", "Rejuvenation").
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Maximum level this spec can be trained to.
    /// </summary>
    public int MaxLevel { get; init; } = 50;

    /// <summary>
    /// Type of specialization line.
    /// </summary>
    public SpecLineType Type { get; init; } = SpecLineType.Weapon;

    /// <summary>
    /// Optional description of the spec line.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Whether this is an auto-trained spec line.
    /// </summary>
    public bool IsAutoTrain { get; init; }

    /// <summary>
    /// Multiplier for point cost (1.0 = normal, 0.5 = half cost auto-train).
    /// </summary>
    public double PointMultiplier { get; init; } = 1.0;
}

/// <summary>
/// Type of specialization line.
/// </summary>
public enum SpecLineType
{
    /// <summary>Weapon-based combat spec (Slash, Thrust, Polearm, etc.)</summary>
    Weapon,

    /// <summary>Magic-based spec (Rejuvenation, Fire, etc.)</summary>
    Magic,

    /// <summary>Utility spec (Stealth, Parry, Shield, etc.)</summary>
    Utility,

    /// <summary>Hybrid spec combining multiple types.</summary>
    Hybrid
}
