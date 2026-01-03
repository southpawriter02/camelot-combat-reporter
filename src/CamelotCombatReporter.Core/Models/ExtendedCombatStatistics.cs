using System.Text.Json.Serialization;

namespace CamelotCombatReporter.Core.Models;

/// <summary>
/// Extended combat statistics with character context for cross-realm analysis.
/// Builds on the base CombatStatistics with additional metrics.
/// </summary>
public record ExtendedCombatStatistics
{
    /// <summary>
    /// Unique identifier for this session.
    /// </summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    /// Base combat statistics from the core analysis.
    /// </summary>
    public CombatStatistics BaseStats { get; init; }

    /// <summary>
    /// Character information for realm/class context.
    /// </summary>
    public CharacterInfo Character { get; init; }

    /// <summary>
    /// Total damage dealt during the session.
    /// </summary>
    public int TotalDamageDealt { get; init; }

    /// <summary>
    /// Total damage taken during the session.
    /// </summary>
    public int TotalDamageTaken { get; init; }

    /// <summary>
    /// Total healing done during the session.
    /// </summary>
    public int TotalHealingDone { get; init; }

    /// <summary>
    /// Total healing received during the session.
    /// </summary>
    public int TotalHealingReceived { get; init; }

    /// <summary>
    /// Healing per second.
    /// </summary>
    public double Hps { get; init; }

    /// <summary>
    /// Number of kills during the session.
    /// </summary>
    public int KillCount { get; init; }

    /// <summary>
    /// Number of deaths during the session.
    /// </summary>
    public int DeathCount { get; init; }

    /// <summary>
    /// Number of assists during the session.
    /// </summary>
    public int AssistCount { get; init; }

    /// <summary>
    /// Session start time (UTC).
    /// </summary>
    public DateTime SessionStartUtc { get; init; }

    /// <summary>
    /// Session end time (UTC).
    /// </summary>
    public DateTime SessionEndUtc { get; init; }

    /// <summary>
    /// Original log file name (for reference).
    /// </summary>
    public string? LogFileName { get; init; }

    /// <summary>
    /// Creates a new ExtendedCombatStatistics instance.
    /// </summary>
    public ExtendedCombatStatistics(
        CombatStatistics baseStats,
        CharacterInfo character,
        int totalDamageDealt,
        int totalDamageTaken,
        int totalHealingDone,
        int totalHealingReceived,
        double hps,
        int killCount,
        int deathCount,
        int assistCount,
        DateTime sessionStartUtc,
        DateTime sessionEndUtc,
        string? logFileName = null)
    {
        BaseStats = baseStats;
        Character = character;
        TotalDamageDealt = totalDamageDealt;
        TotalDamageTaken = totalDamageTaken;
        TotalHealingDone = totalHealingDone;
        TotalHealingReceived = totalHealingReceived;
        Hps = hps;
        KillCount = killCount;
        DeathCount = deathCount;
        AssistCount = assistCount;
        SessionStartUtc = sessionStartUtc;
        SessionEndUtc = sessionEndUtc;
        LogFileName = logFileName;
    }

    /// <summary>
    /// Kill/Death ratio. Returns kill count if no deaths.
    /// </summary>
    [JsonIgnore]
    public double Kdr => DeathCount > 0 ? (double)KillCount / DeathCount : KillCount;

    /// <summary>
    /// Session duration.
    /// </summary>
    [JsonIgnore]
    public TimeSpan Duration => SessionEndUtc - SessionStartUtc;

    /// <summary>
    /// Net damage (dealt - taken).
    /// </summary>
    [JsonIgnore]
    public int NetDamage => TotalDamageDealt - TotalDamageTaken;

    /// <summary>
    /// Net healing (done - received).
    /// </summary>
    [JsonIgnore]
    public int NetHealing => TotalHealingDone - TotalHealingReceived;

    /// <summary>
    /// Creates extended statistics from base stats with character context.
    /// </summary>
    public static ExtendedCombatStatistics FromBaseStats(
        CombatStatistics baseStats,
        CharacterInfo character,
        int damageTaken = 0,
        int healingDone = 0,
        int healingReceived = 0,
        int kills = 0,
        int deaths = 0,
        int assists = 0,
        DateTime? sessionStart = null,
        DateTime? sessionEnd = null,
        string? logFileName = null)
    {
        var start = sessionStart ?? DateTime.UtcNow.AddMinutes(-baseStats.DurationMinutes);
        var end = sessionEnd ?? DateTime.UtcNow;
        var durationSeconds = baseStats.DurationMinutes * 60;
        var hps = durationSeconds > 0 ? healingDone / durationSeconds : 0;

        return new ExtendedCombatStatistics(
            baseStats,
            character,
            baseStats.TotalDamage,
            damageTaken,
            healingDone,
            healingReceived,
            hps,
            kills,
            deaths,
            assists,
            start,
            end,
            logFileName);
    }
}

/// <summary>
/// Summary of a combat session for listing/display.
/// </summary>
public record CombatSessionSummary(
    Guid Id,
    DateTime SessionStartUtc,
    CharacterInfo Character,
    double DurationMinutes,
    double Dps,
    double Hps,
    int Kills,
    int Deaths,
    string? LogFileName
)
{
    /// <summary>
    /// Creates a summary from extended statistics.
    /// </summary>
    public static CombatSessionSummary FromExtended(ExtendedCombatStatistics stats) =>
        new(
            stats.Id,
            stats.SessionStartUtc,
            stats.Character,
            stats.BaseStats.DurationMinutes,
            stats.BaseStats.Dps,
            stats.Hps,
            stats.KillCount,
            stats.DeathCount,
            stats.LogFileName);
}
