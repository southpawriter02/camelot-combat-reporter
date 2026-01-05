namespace CamelotCombatReporter.Core.ServerProfiles;

/// <summary>
/// DAoC server era/type for context-aware filtering.
/// </summary>
public enum ServerType
{
    /// <summary>
    /// Classic DAoC (Launch to Shrouded Isles).
    /// Original 7 classes per realm.
    /// </summary>
    Classic,

    /// <summary>
    /// Shrouded Isles expansion.
    /// Adds 2 classes per realm (Necromancer, Reaver, Savage, etc.).
    /// </summary>
    ShroudedIsles,

    /// <summary>
    /// Trials of Atlantis expansion.
    /// Adds Master Levels, Artifacts, and Champion Levels.
    /// </summary>
    TrialsOfAtlantis,

    /// <summary>
    /// New Frontiers expansion.
    /// Adds Mauler class and updated RvR system.
    /// </summary>
    NewFrontiers,

    /// <summary>
    /// Current live servers with all features enabled.
    /// </summary>
    Live,

    /// <summary>
    /// Custom server configuration for private servers.
    /// </summary>
    Custom
}
