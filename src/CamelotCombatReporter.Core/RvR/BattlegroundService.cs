using CamelotCombatReporter.Core.Models;
using CamelotCombatReporter.Core.RvR.Models;
using Microsoft.Extensions.Logging;

namespace CamelotCombatReporter.Core.RvR;

/// <summary>
/// Service for tracking and analyzing battleground performance.
/// </summary>
public class BattlegroundService : IBattlegroundService
{
    private readonly ILogger<BattlegroundService>? _logger;

    /// <inheritdoc />
    public TimeSpan SessionGapThreshold { get; set; } = TimeSpan.FromMinutes(5);

    public BattlegroundService(ILogger<BattlegroundService>? logger = null)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public IReadOnlyList<ZoneEntryEvent> ExtractZoneEntries(IEnumerable<LogEvent> events)
    {
        return events
            .OfType<ZoneEntryEvent>()
            .OrderBy(e => e.Timestamp)
            .ToList();
    }

    /// <inheritdoc />
    public BattlegroundType? GetBattlegroundType(string zoneName)
    {
        return BattlegroundZones.GetBattlegroundType(zoneName);
    }

    /// <inheritdoc />
    public IReadOnlyList<BattlegroundSession> ResolveSessions(IEnumerable<LogEvent> events, string playerName = "You")
    {
        var allEvents = events.ToList();
        var zoneEntries = ExtractZoneEntries(allEvents);

        var sessions = new List<BattlegroundSession>();
        BattlegroundType? currentBgType = null;
        string? currentZoneName = null;
        TimeOnly? sessionStart = null;
        var sessionEvents = new List<LogEvent>();

        foreach (var entry in zoneEntries)
        {
            var bgType = GetBattlegroundType(entry.ZoneName);

            if (bgType.HasValue)
            {
                // Entering a battleground
                if (currentBgType.HasValue && sessionEvents.Count > 0)
                {
                    // Close previous session
                    var session = CreateSession(sessionEvents, currentBgType.Value, currentZoneName!, sessionStart!.Value, playerName);
                    sessions.Add(session);
                    sessionEvents.Clear();
                }

                currentBgType = bgType;
                currentZoneName = entry.ZoneName;
                sessionStart = entry.Timestamp;
                sessionEvents.Add(entry);
            }
            else if (currentBgType.HasValue)
            {
                // Left the battleground (entered a non-BG zone)
                if (sessionEvents.Count > 0)
                {
                    var session = CreateSession(sessionEvents, currentBgType.Value, currentZoneName!, sessionStart!.Value, playerName);
                    sessions.Add(session);
                    sessionEvents.Clear();
                }

                currentBgType = null;
                currentZoneName = null;
                sessionStart = null;
            }
        }

        // Handle remaining session
        if (currentBgType.HasValue && sessionEvents.Count > 0)
        {
            var session = CreateSession(sessionEvents, currentBgType.Value, currentZoneName!, sessionStart!.Value, playerName);
            sessions.Add(session);
        }

        // Now we need to associate combat events with sessions based on time windows
        sessions = EnrichSessionsWithCombatEvents(sessions, allEvents, playerName);

        _logger?.LogInformation("Resolved {Count} battleground sessions", sessions.Count);
        return sessions;
    }

    private List<BattlegroundSession> EnrichSessionsWithCombatEvents(
        List<BattlegroundSession> sessions,
        List<LogEvent> allEvents,
        string playerName)
    {
        var enrichedSessions = new List<BattlegroundSession>();

        for (var i = 0; i < sessions.Count; i++)
        {
            var session = sessions[i];
            var nextSessionStart = i + 1 < sessions.Count
                ? sessions[i + 1].StartTime
                : (TimeOnly?)null;

            // Get all events between this session start and next session start (or end of log)
            var sessionEvents = allEvents
                .Where(e => e.Timestamp >= session.StartTime &&
                            (!nextSessionStart.HasValue || e.Timestamp < nextSessionStart.Value))
                .ToList();

            var endTime = sessionEvents.Count > 0
                ? sessionEvents.Max(e => e.Timestamp)
                : session.StartTime;

            var stats = CalculateStatisticsFromEvents(sessionEvents, playerName);

            enrichedSessions.Add(new BattlegroundSession(
                Id: session.Id,
                BattlegroundType: session.BattlegroundType,
                ZoneName: session.ZoneName,
                StartTime: session.StartTime,
                EndTime: endTime,
                Duration: endTime - session.StartTime,
                Events: sessionEvents.AsReadOnly(),
                Statistics: stats
            ));
        }

        return enrichedSessions;
    }

    private BattlegroundSession CreateSession(
        List<LogEvent> sessionEvents,
        BattlegroundType bgType,
        string zoneName,
        TimeOnly startTime,
        string playerName)
    {
        var endTime = sessionEvents.Count > 0
            ? sessionEvents.Max(e => e.Timestamp)
            : startTime;

        return new BattlegroundSession(
            Id: Guid.NewGuid(),
            BattlegroundType: bgType,
            ZoneName: zoneName,
            StartTime: startTime,
            EndTime: endTime,
            Duration: endTime - startTime,
            Events: sessionEvents.AsReadOnly(),
            Statistics: BattlegroundStatistics.Empty
        );
    }

    private BattlegroundStatistics CalculateStatisticsFromEvents(List<LogEvent> events, string playerName)
    {
        var isPlayer = playerName.Equals("You", StringComparison.OrdinalIgnoreCase);

        // Kills
        var kills = events
            .OfType<DeathEvent>()
            .Count(e => isPlayer
                ? e.Killer?.Equals("You", StringComparison.OrdinalIgnoreCase) == true
                : e.Killer?.Equals(playerName, StringComparison.OrdinalIgnoreCase) == true);

        // Deaths
        var deaths = events
            .OfType<DeathEvent>()
            .Count(e => isPlayer
                ? e.Target.Equals("You", StringComparison.OrdinalIgnoreCase)
                : e.Target.Equals(playerName, StringComparison.OrdinalIgnoreCase));

        // Death blows and solo kills aren't tracked in DeathEvent currently, so we use 0
        var deathBlows = 0;
        var soloKills = 0;

        // Damage dealt
        var damageDealt = events
            .OfType<DamageEvent>()
            .Where(e => isPlayer
                ? e.Source.Equals("You", StringComparison.OrdinalIgnoreCase)
                : e.Source.Equals(playerName, StringComparison.OrdinalIgnoreCase))
            .Sum(e => e.DamageAmount);

        // Damage received
        var damageReceived = events
            .OfType<DamageEvent>()
            .Where(e => isPlayer
                ? e.Target.Equals("You", StringComparison.OrdinalIgnoreCase)
                : e.Target.Equals(playerName, StringComparison.OrdinalIgnoreCase))
            .Sum(e => e.DamageAmount);

        // Healing done
        var healingDone = events
            .OfType<HealingEvent>()
            .Where(e => isPlayer
                ? e.Source.Equals("You", StringComparison.OrdinalIgnoreCase)
                : e.Source.Equals(playerName, StringComparison.OrdinalIgnoreCase))
            .Sum(e => e.HealingAmount);

        // Healing received
        var healingReceived = events
            .OfType<HealingEvent>()
            .Where(e => isPlayer
                ? e.Target.Equals("You", StringComparison.OrdinalIgnoreCase)
                : e.Target.Equals(playerName, StringComparison.OrdinalIgnoreCase))
            .Sum(e => e.HealingAmount);

        // RP from kills (simplified - based on kill count)
        var estimatedRp = kills * 100;

        // KDR
        var kdr = deaths > 0 ? (double)kills / deaths : kills;

        return new BattlegroundStatistics(
            Kills: kills,
            Deaths: deaths,
            DeathBlows: deathBlows,
            SoloKills: soloKills,
            DamageDealt: damageDealt,
            DamageReceived: damageReceived,
            HealingDone: healingDone,
            HealingReceived: healingReceived,
            RealmPointsEarned: estimatedRp,
            KillDeathRatio: kdr
        );
    }

    /// <inheritdoc />
    public BattlegroundStatistics CalculateSessionStatistics(BattlegroundSession session)
    {
        return session.Statistics;
    }

    /// <inheritdoc />
    public AllBattlegroundStatistics CalculateAllStatistics(IEnumerable<BattlegroundSession> sessions)
    {
        var sessionList = sessions.ToList();

        if (sessionList.Count == 0)
        {
            return new AllBattlegroundStatistics(
                TotalSessions: 0,
                TotalTimeInBattlegrounds: TimeSpan.Zero,
                StatsByType: new Dictionary<BattlegroundType, BattlegroundStatistics>(),
                SessionCountByType: new Dictionary<BattlegroundType, int>(),
                OverallStatistics: BattlegroundStatistics.Empty,
                BestPerformingBattleground: null,
                MostPlayedBattleground: null
            );
        }

        var totalTime = TimeSpan.FromTicks(sessionList.Sum(s => s.Duration.Ticks));

        // Aggregate stats by type
        var statsByType = new Dictionary<BattlegroundType, BattlegroundStatistics>();
        var sessionCountByType = new Dictionary<BattlegroundType, int>();

        foreach (var bgType in Enum.GetValues<BattlegroundType>())
        {
            var typeSessions = sessionList.Where(s => s.BattlegroundType == bgType).ToList();
            if (typeSessions.Count > 0)
            {
                sessionCountByType[bgType] = typeSessions.Count;
                statsByType[bgType] = AggregateStatistics(typeSessions.Select(s => s.Statistics));
            }
        }

        // Overall stats
        var overallStats = AggregateStatistics(sessionList.Select(s => s.Statistics));

        // Best performing (highest KDR with at least 5 kills)
        BattlegroundType? bestPerforming = null;
        var bestKdr = 0.0;
        foreach (var (bgType, stats) in statsByType)
        {
            if (stats.Kills >= 5 && stats.KillDeathRatio > bestKdr)
            {
                bestKdr = stats.KillDeathRatio;
                bestPerforming = bgType;
            }
        }

        // Most played
        BattlegroundType? mostPlayed = null;
        var maxSessions = 0;
        foreach (var (bgType, count) in sessionCountByType)
        {
            if (count > maxSessions)
            {
                maxSessions = count;
                mostPlayed = bgType;
            }
        }

        return new AllBattlegroundStatistics(
            TotalSessions: sessionList.Count,
            TotalTimeInBattlegrounds: totalTime,
            StatsByType: statsByType,
            SessionCountByType: sessionCountByType,
            OverallStatistics: overallStats,
            BestPerformingBattleground: bestPerforming,
            MostPlayedBattleground: mostPlayed
        );
    }

    private BattlegroundStatistics AggregateStatistics(IEnumerable<BattlegroundStatistics> stats)
    {
        var statsList = stats.ToList();

        if (statsList.Count == 0)
        {
            return BattlegroundStatistics.Empty;
        }

        var kills = statsList.Sum(s => s.Kills);
        var deaths = statsList.Sum(s => s.Deaths);

        return new BattlegroundStatistics(
            Kills: kills,
            Deaths: deaths,
            DeathBlows: statsList.Sum(s => s.DeathBlows),
            SoloKills: statsList.Sum(s => s.SoloKills),
            DamageDealt: statsList.Sum(s => s.DamageDealt),
            DamageReceived: statsList.Sum(s => s.DamageReceived),
            HealingDone: statsList.Sum(s => s.HealingDone),
            HealingReceived: statsList.Sum(s => s.HealingReceived),
            RealmPointsEarned: statsList.Sum(s => s.RealmPointsEarned),
            KillDeathRatio: deaths > 0 ? (double)kills / deaths : kills
        );
    }
}
