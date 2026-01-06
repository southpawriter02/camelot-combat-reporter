namespace CamelotCombatReporter.Core.Models;

/// <summary>
/// Unique identifier for a combat target instance.
/// Allows differentiating between multiple mobs with the same name.
/// </summary>
/// <param name="TargetName">The display name of the target (e.g., "spraggonet").</param>
/// <param name="InstanceNumber">Sequential instance number for this target name (1, 2, 3...).</param>
/// <param name="InternalId">GUID for internal tracking.</param>
public record CombatTargetInstance(
    string TargetName,
    int InstanceNumber,
    Guid InternalId
)
{
    /// <summary>
    /// Display name with instance suffix (e.g., "spraggonet #2").
    /// Only shows suffix for instance 2+.
    /// </summary>
    public string DisplayName => InstanceNumber > 1
        ? $"{TargetName} #{InstanceNumber}"
        : TargetName;

    /// <summary>
    /// Creates a new instance with the next sequential number.
    /// </summary>
    public static CombatTargetInstance Create(string targetName, int instanceNumber) =>
        new(targetName, instanceNumber, Guid.NewGuid());
}

/// <summary>
/// Represents combat with a single target instance from first damage to death/timeout.
/// </summary>
/// <param name="Instance">The target instance.</param>
/// <param name="StartTime">First event timestamp.</param>
/// <param name="EndTime">Death or last event timestamp.</param>
/// <param name="EndReason">How this instance ended.</param>
/// <param name="Events">All events associated with this instance.</param>
/// <param name="TotalDamageDealt">Sum of player damage dealt to this target.</param>
/// <param name="TotalDamageTaken">Sum of damage received from this target.</param>
/// <param name="TotalHealingDone">Sum of healing done during this encounter.</param>
public record CombatEncounter(
    CombatTargetInstance Instance,
    TimeOnly StartTime,
    TimeOnly EndTime,
    EncounterEndReason EndReason,
    IReadOnlyList<LogEvent> Events,
    int TotalDamageDealt,
    int TotalDamageTaken,
    int TotalHealingDone = 0
)
{
    /// <summary>
    /// Duration of this encounter.
    /// </summary>
    public TimeSpan Duration => EndTime - StartTime;

    /// <summary>
    /// Damage per second for this encounter.
    /// </summary>
    public double Dps => Duration.TotalSeconds > 0
        ? TotalDamageDealt / Duration.TotalSeconds
        : TotalDamageDealt;

    /// <summary>
    /// Number of damage events in this encounter.
    /// </summary>
    public int DamageEventCount => Events.Count(e => e is DamageEvent);

    /// <summary>
    /// Whether this encounter resulted in a kill.
    /// </summary>
    public bool WasKilled => EndReason == EncounterEndReason.Death;
}

/// <summary>
/// How a combat encounter ended.
/// </summary>
public enum EncounterEndReason
{
    /// <summary>Target died (death event received).</summary>
    Death,

    /// <summary>Combat ended due to time gap (mob fled/despawned/evaded).</summary>
    Timeout,

    /// <summary>Session ended before encounter resolved.</summary>
    SessionEnd,

    /// <summary>Still in progress (live parsing).</summary>
    InProgress
}

/// <summary>
/// Statistics aggregated across all instances of a target type.
/// </summary>
/// <param name="TargetName">The target name (without instance suffix).</param>
/// <param name="Encounters">All encounters with this target type.</param>
public record TargetTypeStatistics(
    string TargetName,
    IReadOnlyList<CombatEncounter> Encounters
)
{
    /// <summary>
    /// Number of kills (encounters ending in death).
    /// </summary>
    public int TotalKills => Encounters.Count(e => e.EndReason == EncounterEndReason.Death);

    /// <summary>
    /// Total instances encountered (kills + timeouts + in progress).
    /// </summary>
    public int TotalEncounters => Encounters.Count;

    /// <summary>
    /// Total damage dealt across all encounters.
    /// </summary>
    public int TotalDamageDealt => Encounters.Sum(e => e.TotalDamageDealt);

    /// <summary>
    /// Total damage taken across all encounters.
    /// </summary>
    public int TotalDamageTaken => Encounters.Sum(e => e.TotalDamageTaken);

    /// <summary>
    /// Average damage per kill.
    /// </summary>
    public double AverageDamagePerKill => TotalKills > 0
        ? (double)TotalDamageDealt / TotalKills
        : 0;

    /// <summary>
    /// Average time to kill in seconds.
    /// </summary>
    public double AverageTimeToKill => TotalKills > 0
        ? Encounters
            .Where(e => e.EndReason == EncounterEndReason.Death)
            .Average(e => e.Duration.TotalSeconds)
        : 0;

    /// <summary>
    /// Average DPS across all kills.
    /// </summary>
    public double AverageDps => TotalKills > 0
        ? Encounters
            .Where(e => e.EndReason == EncounterEndReason.Death)
            .Average(e => e.Dps)
        : 0;

    /// <summary>
    /// Best (fastest) kill time in seconds.
    /// </summary>
    public double? FastestKill => TotalKills > 0
        ? Encounters
            .Where(e => e.EndReason == EncounterEndReason.Death)
            .Min(e => e.Duration.TotalSeconds)
        : null;

    /// <summary>
    /// Highest DPS achieved in a single encounter.
    /// </summary>
    public double? HighestDps => TotalKills > 0
        ? Encounters
            .Where(e => e.EndReason == EncounterEndReason.Death)
            .Max(e => e.Dps)
        : null;
}

/// <summary>
/// State tracker for an active combat instance being built.
/// </summary>
internal class ActiveInstanceState
{
    public CombatTargetInstance Instance { get; }
    public TimeOnly StartTime { get; }
    public TimeOnly LastEventTime { get; set; }
    public List<LogEvent> Events { get; } = new();
    public int DamageDealt { get; set; }
    public int DamageTaken { get; set; }
    public int HealingDone { get; set; }

    public ActiveInstanceState(CombatTargetInstance instance, TimeOnly startTime)
    {
        Instance = instance;
        StartTime = startTime;
        LastEventTime = startTime;
    }

    /// <summary>
    /// Creates a completed CombatEncounter from this state.
    /// </summary>
    public CombatEncounter ToEncounter(EncounterEndReason endReason, TimeOnly? endTime = null) =>
        new(
            Instance,
            StartTime,
            endTime ?? LastEventTime,
            endReason,
            Events.AsReadOnly(),
            DamageDealt,
            DamageTaken,
            HealingDone
        );
}
