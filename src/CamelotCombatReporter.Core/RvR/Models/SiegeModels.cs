using CamelotCombatReporter.Core.Models;

namespace CamelotCombatReporter.Core.RvR.Models;

/// <summary>
/// Contribution metrics for a player during a siege.
/// </summary>
public record SiegeContribution(
    int StructureDamage,
    int PlayerKills,
    int Deaths,
    int HealingDone,
    int GuardKills,
    double ContributionScore
)
{
    /// <summary>
    /// Weight for structure damage in contribution scoring.
    /// </summary>
    public const double StructureDamageWeight = 0.01;

    /// <summary>
    /// Weight for player kills in contribution scoring.
    /// </summary>
    public const double PlayerKillWeight = 50.0;

    /// <summary>
    /// Penalty for deaths in contribution scoring.
    /// </summary>
    public const double DeathPenalty = 25.0;

    /// <summary>
    /// Weight for healing done in contribution scoring.
    /// </summary>
    public const double HealingWeight = 0.005;

    /// <summary>
    /// Weight for guard kills in contribution scoring.
    /// </summary>
    public const double GuardKillWeight = 10.0;

    /// <summary>
    /// Calculates the contribution score based on weighted metrics.
    /// </summary>
    public static double CalculateScore(int structureDamage, int playerKills,
        int deaths, int healingDone, int guardKills)
    {
        return (structureDamage * StructureDamageWeight) +
               (playerKills * PlayerKillWeight) -
               (deaths * DeathPenalty) +
               (healingDone * HealingWeight) +
               (guardKills * GuardKillWeight);
    }

    /// <summary>
    /// Creates a SiegeContribution with automatically calculated score.
    /// </summary>
    public static SiegeContribution Create(int structureDamage, int playerKills,
        int deaths, int healingDone, int guardKills)
    {
        var score = CalculateScore(structureDamage, playerKills, deaths, healingDone, guardKills);
        return new SiegeContribution(structureDamage, playerKills, deaths, healingDone, guardKills, score);
    }
}

/// <summary>
/// A complete siege session tracking attack or defense.
/// </summary>
public record SiegeSession(
    Guid Id,
    string KeepName,
    KeepType KeepType,
    TimeOnly StartTime,
    TimeOnly EndTime,
    SiegeOutcome Outcome,
    Realm AttackingRealm,
    Realm DefendingRealm,
    IReadOnlyList<LogEvent> Events,
    SiegeContribution PlayerContribution,
    SiegePhase FinalPhase,
    TimeSpan Duration,
    bool PlayerWasAttacker
);

/// <summary>
/// Aggregate statistics across multiple sieges.
/// </summary>
public record SiegeStatistics(
    int TotalSiegesParticipated,
    int AttackVictories,
    int DefenseVictories,
    int TotalStructureDamage,
    int TotalPlayerKills,
    int TotalDeaths,
    int TotalGuardKills,
    double TotalContributionScore,
    TimeSpan AverageSiegeDuration,
    IReadOnlyDictionary<string, int> SiegesByKeep,
    IReadOnlyDictionary<SiegePhase, int> SiegesByFinalPhase
);

/// <summary>
/// A single entry in the siege timeline for visualization.
/// </summary>
public record SiegeTimelineEntry(
    TimeOnly Timestamp,
    string EventType,
    string Description,
    SiegePhase Phase,
    bool IsPlayerAction
);

/// <summary>
/// Contribution metrics for a player during a relic raid.
/// </summary>
public record RelicContribution(
    int EscortKills,
    int Deaths,
    int HealingDone,
    bool WasCarrier,
    bool DeliveredRelic,
    double ContributionScore
)
{
    /// <summary>
    /// Calculates relic raid contribution score.
    /// </summary>
    public static double CalculateScore(int escortKills, int deaths,
        int healingDone, bool wasCarrier, bool deliveredRelic)
    {
        const double KillWeight = 30.0;
        const double DeathPenalty = 20.0;
        const double HealingWeight = 0.005;
        const double CarrierBonus = 100.0;
        const double DeliveryBonus = 500.0;

        var score = (escortKills * KillWeight) -
                   (deaths * DeathPenalty) +
                   (healingDone * HealingWeight);

        if (wasCarrier)
            score += CarrierBonus;
        if (deliveredRelic)
            score += DeliveryBonus;

        return score;
    }

    /// <summary>
    /// Creates a RelicContribution with automatically calculated score.
    /// </summary>
    public static RelicContribution Create(int escortKills, int deaths,
        int healingDone, bool wasCarrier, bool deliveredRelic)
    {
        var score = CalculateScore(escortKills, deaths, healingDone, wasCarrier, deliveredRelic);
        return new RelicContribution(escortKills, deaths, healingDone, wasCarrier, deliveredRelic, score);
    }
}

/// <summary>
/// A relic raid session from start to completion.
/// </summary>
public record RelicRaidSession(
    Guid Id,
    string RelicName,
    RelicType RelicType,
    Realm OriginRealm,
    Realm CapturingRealm,
    TimeOnly StartTime,
    TimeOnly EndTime,
    TimeSpan Duration,
    bool WasSuccessful,
    RelicRaidOutcome Outcome,
    IReadOnlyList<string> Carriers,
    bool PlayerWasCarrier,
    RelicContribution PlayerContribution,
    IReadOnlyList<LogEvent> Events
);

/// <summary>
/// Outcome of a relic raid.
/// </summary>
public enum RelicRaidOutcome
{
    /// <summary>Relic was successfully captured.</summary>
    Captured,

    /// <summary>Relic was returned to its home temple.</summary>
    Returned,

    /// <summary>Carrier was killed and relic was dropped.</summary>
    CarrierKilled,

    /// <summary>Outcome could not be determined.</summary>
    Unknown
}

/// <summary>
/// Aggregate statistics for relic raids.
/// </summary>
public record RelicRaidStatistics(
    int TotalRaidsParticipated,
    int SuccessfulRaids,
    int FailedRaids,
    int TimesAsCarrier,
    int SuccessfulDeliveries,
    int TotalEscortKills,
    int TotalDeaths,
    int TotalHealingDone,
    double TotalContributionScore,
    TimeSpan AverageRaidDuration,
    IReadOnlyDictionary<string, int> RaidsByRelic,
    IReadOnlyDictionary<RelicRaidOutcome, int> RaidsByOutcome
);
