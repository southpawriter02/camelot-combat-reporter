namespace CamelotCombatReporter.Core.RvR.Models;

/// <summary>
/// Type of keep structure in DAoC RvR.
/// </summary>
public enum KeepType
{
    /// <summary>Standard border keep with 2 doors.</summary>
    BorderKeep,

    /// <summary>Relic keep housing realm relics, has 3 doors.</summary>
    RelicKeep,

    /// <summary>Tower structure, single door.</summary>
    Tower,

    /// <summary>Milegate chokepoint structure.</summary>
    Milegate,

    /// <summary>Dock access point.</summary>
    Dock
}

/// <summary>
/// Outcome of a siege engagement.
/// </summary>
public enum SiegeOutcome
{
    /// <summary>Siege is still ongoing.</summary>
    InProgress,

    /// <summary>Attackers successfully captured the keep.</summary>
    AttackSuccess,

    /// <summary>Defenders successfully repelled the attack.</summary>
    DefenseSuccess,

    /// <summary>Siege was abandoned without resolution.</summary>
    Abandoned,

    /// <summary>Outcome could not be determined.</summary>
    Unknown
}

/// <summary>
/// Current phase of a siege engagement.
/// </summary>
public enum SiegePhase
{
    /// <summary>Initial approach, no doors breached.</summary>
    Approach,

    /// <summary>Outer door has been breached.</summary>
    OuterSiege,

    /// <summary>Inner door has been breached.</summary>
    InnerSiege,

    /// <summary>Lord room engaged.</summary>
    LordFight,

    /// <summary>Keep has been captured.</summary>
    Capture
}

/// <summary>
/// Type of realm relic.
/// </summary>
public enum RelicType
{
    /// <summary>Strength relic (melee bonus).</summary>
    Strength,

    /// <summary>Power relic (magic bonus).</summary>
    Power
}

/// <summary>
/// Current status of a realm relic.
/// </summary>
public enum RelicStatus
{
    /// <summary>Relic is at its home keep.</summary>
    Home,

    /// <summary>Relic has been captured by another realm.</summary>
    Captured,

    /// <summary>Relic is being carried (in transit).</summary>
    InTransit,

    /// <summary>Relic status is unknown.</summary>
    Unknown
}

/// <summary>
/// Type of battleground zone.
/// </summary>
public enum BattlegroundType
{
    /// <summary>Thidranki (Level 20-24).</summary>
    Thidranki,

    /// <summary>Molvik (Level 35-39).</summary>
    Molvik,

    /// <summary>Cathal Valley (Level 45-49).</summary>
    CathalValley,

    /// <summary>Killaloe (Level 40-44).</summary>
    Killaloe,

    /// <summary>Open RvR in frontier zones.</summary>
    OpenRvR
}

/// <summary>
/// Type of siege weapon.
/// </summary>
public enum SiegeWeaponType
{
    /// <summary>Battering ram for door destruction.</summary>
    Ram,

    /// <summary>Trebuchet for long-range siege.</summary>
    Trebuchet,

    /// <summary>Ballista for anti-personnel.</summary>
    Ballista,

    /// <summary>Catapult for area damage.</summary>
    Catapult,

    /// <summary>Boiling oil for keep defense.</summary>
    BoilingOil
}

/// <summary>
/// Extension methods for RvR enums.
/// </summary>
public static class RvREnumExtensions
{
    /// <summary>
    /// Gets a display-friendly name for the keep type.
    /// </summary>
    public static string GetDisplayName(this KeepType type) => type switch
    {
        KeepType.BorderKeep => "Border Keep",
        KeepType.RelicKeep => "Relic Keep",
        KeepType.Tower => "Tower",
        KeepType.Milegate => "Milegate",
        KeepType.Dock => "Dock",
        _ => type.ToString()
    };

    /// <summary>
    /// Gets a display-friendly name for the siege outcome.
    /// </summary>
    public static string GetDisplayName(this SiegeOutcome outcome) => outcome switch
    {
        SiegeOutcome.InProgress => "In Progress",
        SiegeOutcome.AttackSuccess => "Attack Victory",
        SiegeOutcome.DefenseSuccess => "Defense Victory",
        SiegeOutcome.Abandoned => "Abandoned",
        SiegeOutcome.Unknown => "Unknown",
        _ => outcome.ToString()
    };

    /// <summary>
    /// Gets a display-friendly name for the siege phase.
    /// </summary>
    public static string GetDisplayName(this SiegePhase phase) => phase switch
    {
        SiegePhase.Approach => "Approach",
        SiegePhase.OuterSiege => "Outer Door Siege",
        SiegePhase.InnerSiege => "Inner Door Siege",
        SiegePhase.LordFight => "Lord Fight",
        SiegePhase.Capture => "Captured",
        _ => phase.ToString()
    };

    /// <summary>
    /// Gets a display-friendly name for the battleground type.
    /// </summary>
    public static string GetDisplayName(this BattlegroundType type) => type switch
    {
        BattlegroundType.Thidranki => "Thidranki (20-24)",
        BattlegroundType.Molvik => "Molvik (35-39)",
        BattlegroundType.CathalValley => "Cathal Valley (45-49)",
        BattlegroundType.Killaloe => "Killaloe (40-44)",
        BattlegroundType.OpenRvR => "Open RvR",
        _ => type.ToString()
    };

    /// <summary>
    /// Gets the level range for a battleground.
    /// </summary>
    public static (int Min, int Max) GetLevelRange(this BattlegroundType type) => type switch
    {
        BattlegroundType.Thidranki => (20, 24),
        BattlegroundType.Molvik => (35, 39),
        BattlegroundType.CathalValley => (45, 49),
        BattlegroundType.Killaloe => (40, 44),
        BattlegroundType.OpenRvR => (1, 50),
        _ => (1, 50)
    };
}
