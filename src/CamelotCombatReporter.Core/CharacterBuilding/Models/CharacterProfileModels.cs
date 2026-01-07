using CamelotCombatReporter.Core.Models;

namespace CamelotCombatReporter.Core.CharacterBuilding.Models;

/// <summary>
/// Persistent character profile with associated combat history.
/// </summary>
public record CharacterProfile
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string Name { get; init; }
    public required Realm Realm { get; init; }
    public required CharacterClass Class { get; init; }
    public int Level { get; init; } = 50;
    public string? ServerName { get; init; }
    public DateTime CreatedUtc { get; init; } = DateTime.UtcNow;
    public DateTime LastUpdatedUtc { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// The currently active build configuration.
    /// </summary>
    public CharacterBuild? ActiveBuild { get; set; }

    /// <summary>
    /// Historical builds for comparison and versioning.
    /// </summary>
    public IReadOnlyList<CharacterBuild> BuildHistory { get; init; } = [];

    /// <summary>
    /// Combat session IDs attached to this profile.
    /// </summary>
    public IReadOnlyList<Guid> AttachedSessionIds { get; init; } = [];

    /// <summary>
    /// Realm rank progression data (calculated from attached sessions).
    /// </summary>
    public RealmRankProgression? RankProgression { get; set; }
}

/// <summary>
/// Snapshot of a character's build at a point in time.
/// Builds are immutable - edits create new versions.
/// </summary>
public record CharacterBuild
{
    public Guid Id { get; init; } = Guid.NewGuid();
    
    /// <summary>
    /// Descriptive name for this build (e.g., "RR5 Caster Nuke Build").
    /// </summary>
    public required string Name { get; init; }
    
    public DateTime CreatedUtc { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Realm rank at time of build (1-14).
    /// </summary>
    public int RealmRank { get; init; } = 1;
    
    /// <summary>
    /// Realm rank sub-level (0-9, where 10 advances to next rank).
    /// </summary>
    public int RealmRankLevel { get; init; } = 0;
    
    /// <summary>
    /// Total realm points accumulated.
    /// </summary>
    public long RealmPoints { get; init; }

    /// <summary>
    /// Specialization line allocations (e.g., {"Polearm": 50, "Shield": 42}).
    /// </summary>
    public IReadOnlyDictionary<string, int> SpecLines { get; init; } =
        new Dictionary<string, int>();

    /// <summary>
    /// Selected realm abilities with their trained ranks.
    /// </summary>
    public IReadOnlyList<RealmAbilitySelection> RealmAbilities { get; init; } = [];

    /// <summary>
    /// Character stats (base + bonuses from gear/buffs).
    /// </summary>
    public CharacterStats Stats { get; init; } = new();

    /// <summary>
    /// Optional notes about this build.
    /// </summary>
    public string? Notes { get; init; }

    /// <summary>
    /// Aggregated performance metrics from attached sessions with this build.
    /// </summary>
    public BuildPerformanceMetrics? PerformanceMetrics { get; set; }
    
    /// <summary>
    /// Display string for realm rank (e.g., "RR8L4").
    /// </summary>
    public string RealmRankDisplay => $"RR{RealmRank}L{RealmRankLevel}";
}

/// <summary>
/// Character attribute stats with base and bonus values.
/// </summary>
public record CharacterStats
{
    // Primary stats (base value from race/class, bonus from gear/buffs)
    public StatValue Strength { get; init; } = new(60, 0);
    public StatValue Constitution { get; init; } = new(60, 0);
    public StatValue Dexterity { get; init; } = new(60, 0);
    public StatValue Quickness { get; init; } = new(60, 0);
    public StatValue Intelligence { get; init; } = new(60, 0);
    public StatValue Piety { get; init; } = new(60, 0);
    public StatValue Empathy { get; init; } = new(60, 0);
    public StatValue Charisma { get; init; } = new(60, 0);

    // Derived combat stats
    public int HitPoints { get; init; }
    public int Power { get; init; }
    public int ArmorFactor { get; init; }
    public int AbsorptionPercent { get; init; }

    /// <summary>
    /// Resistance values by damage type.
    /// </summary>
    public IReadOnlyDictionary<string, int> Resistances { get; init; } =
        new Dictionary<string, int>();
}

/// <summary>
/// Represents a stat with base value and equipment/buff bonus.
/// </summary>
/// <param name="Base">Base stat value from race and class.</param>
/// <param name="Bonus">Bonus from gear, buffs, and realm abilities.</param>
public record StatValue(int Base, int Bonus)
{
    /// <summary>
    /// Total stat value (Base + Bonus).
    /// </summary>
    public int Total => Base + Bonus;
}

// ─────────────────────────────────────────────────────────────────────────────
// Export Models
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Options for profile export.
/// </summary>
public record ProfileExportOptions
{
    /// <summary>Whether to include build history in the export.</summary>
    public bool IncludeBuildHistory { get; init; } = true;
    
    /// <summary>Whether to include session IDs (not full session data).</summary>
    public bool IncludeSessionReferences { get; init; } = false;
    
    /// <summary>Whether to replace the character name with a placeholder.</summary>
    public bool AnonymizeCharacterName { get; init; } = false;
    
    /// <summary>Optional custom name for the exported profile.</summary>
    public string? CustomExportName { get; init; }
    
    /// <summary>Whether to include performance metrics in builds.</summary>
    public bool IncludePerformanceMetrics { get; init; } = false;
}

/// <summary>
/// Result of a profile export operation.
/// </summary>
public record ProfileExportResult
{
    /// <summary>The exported JSON content.</summary>
    public required string Json { get; init; }
    
    /// <summary>Suggested filename for the export.</summary>
    public required string SuggestedFileName { get; init; }
    
    /// <summary>Size of the export in bytes.</summary>
    public int SizeBytes { get; init; }
    
    /// <summary>Number of builds included in the export.</summary>
    public int BuildCount { get; init; }
    
    /// <summary>Number of session references included.</summary>
    public int SessionReferenceCount { get; init; }
    
    /// <summary>Whether the profile name was anonymized.</summary>
    public bool WasAnonymized { get; init; }
}
