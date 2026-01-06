namespace CamelotCombatReporter.Core.Models;

/// <summary>
/// Represents a combat session - a continuous period of combat activity.
/// Sessions are bounded by combat mode enter/exit, rest periods, or significant time gaps.
/// Contains one or more encounters with individual mobs.
/// </summary>
/// <param name="Id">Unique identifier for this session.</param>
/// <param name="SessionNumber">Sequential session number within the log (1, 2, 3...).</param>
/// <param name="StartTime">First event timestamp in this session.</param>
/// <param name="EndTime">Last event timestamp in this session.</param>
/// <param name="EndReason">How this session ended.</param>
/// <param name="Encounters">All target encounters within this session.</param>
/// <param name="Events">All events in this session.</param>
public record CombatSession(
    Guid Id,
    int SessionNumber,
    TimeOnly StartTime,
    TimeOnly EndTime,
    SessionEndReason EndReason,
    IReadOnlyList<CombatEncounter> Encounters,
    IReadOnlyList<LogEvent> Events
)
{
    /// <summary>
    /// Duration of this session.
    /// </summary>
    public TimeSpan Duration => EndTime - StartTime;

    /// <summary>
    /// Total damage dealt across all encounters.
    /// </summary>
    public int TotalDamageDealt => Encounters.Sum(e => e.TotalDamageDealt);

    /// <summary>
    /// Total damage taken across all encounters.
    /// </summary>
    public int TotalDamageTaken => Encounters.Sum(e => e.TotalDamageTaken);

    /// <summary>
    /// Total healing done across all encounters.
    /// </summary>
    public int TotalHealingDone => Encounters.Sum(e => e.TotalHealingDone);

    /// <summary>
    /// Number of kills in this session.
    /// </summary>
    public int TotalKills => Encounters.Count(e => e.WasKilled);

    /// <summary>
    /// DPS for this session.
    /// </summary>
    public double Dps => Duration.TotalSeconds > 0
        ? TotalDamageDealt / Duration.TotalSeconds
        : TotalDamageDealt;

    /// <summary>
    /// Number of unique targets fought.
    /// </summary>
    public int UniqueTargetCount => Encounters
        .Select(e => e.Instance.TargetName)
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .Count();

    /// <summary>
    /// Display name for this session.
    /// </summary>
    public string DisplayName => $"Session {SessionNumber}";

    /// <summary>
    /// Creates a new session with a generated ID.
    /// </summary>
    public static CombatSession Create(
        int sessionNumber,
        TimeOnly startTime,
        TimeOnly endTime,
        SessionEndReason endReason,
        IReadOnlyList<CombatEncounter> encounters,
        IReadOnlyList<LogEvent> events) =>
        new(Guid.NewGuid(), sessionNumber, startTime, endTime, endReason, encounters, events);
}

/// <summary>
/// How a combat session ended.
/// </summary>
public enum SessionEndReason
{
    /// <summary>Explicit combat mode exit (if detected).</summary>
    CombatModeExit,

    /// <summary>Player started resting (sat down).</summary>
    Rest,

    /// <summary>Time gap exceeded session timeout threshold.</summary>
    Timeout,

    /// <summary>Log file boundary reached.</summary>
    LogBoundary,

    /// <summary>End of log file.</summary>
    EndOfLog,

    /// <summary>Session still in progress.</summary>
    InProgress
}

/// <summary>
/// Aggregated statistics across multiple sessions.
/// </summary>
/// <param name="Sessions">All sessions in the analysis.</param>
public record SessionStatistics(
    IReadOnlyList<CombatSession> Sessions
)
{
    /// <summary>
    /// Total number of sessions.
    /// </summary>
    public int TotalSessions => Sessions.Count;

    /// <summary>
    /// Total combat duration across all sessions.
    /// </summary>
    public TimeSpan TotalCombatDuration => TimeSpan.FromSeconds(
        Sessions.Sum(s => s.Duration.TotalSeconds));

    /// <summary>
    /// Total damage dealt across all sessions.
    /// </summary>
    public int TotalDamageDealt => Sessions.Sum(s => s.TotalDamageDealt);

    /// <summary>
    /// Total kills across all sessions.
    /// </summary>
    public int TotalKills => Sessions.Sum(s => s.TotalKills);

    /// <summary>
    /// Average DPS across all sessions.
    /// </summary>
    public double AverageDps => Sessions.Count > 0
        ? Sessions.Average(s => s.Dps)
        : 0;

    /// <summary>
    /// Best DPS in a single session.
    /// </summary>
    public double? BestSessionDps => Sessions.Count > 0
        ? Sessions.Max(s => s.Dps)
        : null;

    /// <summary>
    /// Longest session by duration.
    /// </summary>
    public CombatSession? LongestSession => Sessions
        .OrderByDescending(s => s.Duration)
        .FirstOrDefault();

    /// <summary>
    /// Most productive session by kills.
    /// </summary>
    public CombatSession? MostKillsSession => Sessions
        .OrderByDescending(s => s.TotalKills)
        .FirstOrDefault();
}

/// <summary>
/// State tracker for an active session being built.
/// </summary>
internal class ActiveSessionState
{
    public int SessionNumber { get; }
    public TimeOnly StartTime { get; }
    public TimeOnly LastEventTime { get; set; }
    public List<LogEvent> Events { get; } = new();
    public List<CombatEncounter> Encounters { get; } = new();
    public bool InCombatMode { get; set; }

    public ActiveSessionState(int sessionNumber, TimeOnly startTime)
    {
        SessionNumber = sessionNumber;
        StartTime = startTime;
        LastEventTime = startTime;
        InCombatMode = true;
    }

    /// <summary>
    /// Creates a completed CombatSession from this state.
    /// </summary>
    public CombatSession ToSession(SessionEndReason endReason, TimeOnly? endTime = null) =>
        CombatSession.Create(
            SessionNumber,
            StartTime,
            endTime ?? LastEventTime,
            endReason,
            Encounters.AsReadOnly(),
            Events.AsReadOnly()
        );
}
