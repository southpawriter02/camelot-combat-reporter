namespace CamelotCombatReporter.Core.RealmAbilities.Models;

/// <summary>
/// Type classification for realm abilities.
/// </summary>
public enum RealmAbilityType
{
    /// <summary>Direct damage abilities (nukes, PBAoE).</summary>
    Damage,

    /// <summary>Crowd control abilities (stun, mez, root).</summary>
    CrowdControl,

    /// <summary>Protection and defensive abilities.</summary>
    Defensive,

    /// <summary>Health restoration abilities.</summary>
    Healing,

    /// <summary>Utility effects (purge, speed, stealth).</summary>
    Utility,

    /// <summary>Passive stat bonuses (no activation required).</summary>
    Passive
}

/// <summary>
/// Realm availability for realm abilities.
/// </summary>
public enum RealmAvailability
{
    /// <summary>Available only to Albion.</summary>
    Albion,

    /// <summary>Available only to Midgard.</summary>
    Midgard,

    /// <summary>Available only to Hibernia.</summary>
    Hibernia,

    /// <summary>Available to all realms (universal abilities).</summary>
    All
}

/// <summary>
/// Game era for era-based ability gating.
/// </summary>
public enum GameEra
{
    /// <summary>Classic DAoC (release).</summary>
    Classic,

    /// <summary>Shrouded Isles expansion.</summary>
    ShroudedIsles,

    /// <summary>Trials of Atlantis expansion.</summary>
    TrialsOfAtlantis,

    /// <summary>New Frontiers expansion.</summary>
    NewFrontiers,

    /// <summary>Catacombs expansion.</summary>
    Catacombs,

    /// <summary>Darkness Rising expansion.</summary>
    DarknessRising,

    /// <summary>Labyrinth of the Minotaur expansion.</summary>
    Labyrinth,

    /// <summary>Live servers (current).</summary>
    Live
}
