using CamelotCombatReporter.Core.Models;
using CamelotCombatReporter.Core.RvR.Models;
using Microsoft.Extensions.Logging;

namespace CamelotCombatReporter.Core.RvR;

/// <summary>
/// Service for tracking and analyzing relic raids.
/// </summary>
public class RelicTrackingService : IRelicTrackingService
{
    private readonly ILogger<RelicTrackingService>? _logger;

    /// <inheritdoc />
    public TimeSpan SessionGapThreshold { get; set; } = TimeSpan.FromMinutes(10);

    public RelicTrackingService(ILogger<RelicTrackingService>? logger = null)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public IReadOnlyList<RelicEvent> ExtractRelicEvents(IEnumerable<LogEvent> events)
    {
        return events
            .OfType<RelicEvent>()
            .OrderBy(e => e.Timestamp)
            .ToList();
    }

    /// <inheritdoc />
    public IReadOnlyList<RelicRaidSession> ResolveSessions(IEnumerable<LogEvent> events, string playerName = "You")
    {
        var allEvents = events.ToList();
        var relicEvents = ExtractRelicEvents(allEvents);

        if (relicEvents.Count == 0)
        {
            _logger?.LogDebug("No relic events found");
            return Array.Empty<RelicRaidSession>();
        }

        var sessions = new List<RelicRaidSession>();
        var currentSessionEvents = new List<LogEvent>();
        string? currentRelicName = null;
        TimeOnly? lastEventTime = null;

        foreach (var evt in relicEvents)
        {
            var startNewSession = false;

            if (currentRelicName == null)
            {
                startNewSession = true;
            }
            else if (evt.RelicName != currentRelicName)
            {
                // Different relic = new session
                startNewSession = true;
            }
            else if (lastEventTime.HasValue)
            {
                var gap = evt.Timestamp - lastEventTime.Value;
                if (gap > SessionGapThreshold)
                {
                    startNewSession = true;
                }
            }

            if (startNewSession && currentSessionEvents.Count > 0 && currentRelicName != null)
            {
                var session = CreateSession(currentSessionEvents, currentRelicName, playerName, allEvents);
                sessions.Add(session);
                currentSessionEvents.Clear();
            }

            if (startNewSession)
            {
                currentRelicName = evt.RelicName;
            }

            currentSessionEvents.Add(evt);
            lastEventTime = evt.Timestamp;
        }

        // Save final session
        if (currentSessionEvents.Count > 0 && currentRelicName != null)
        {
            var session = CreateSession(currentSessionEvents, currentRelicName, playerName, allEvents);
            sessions.Add(session);
        }

        _logger?.LogInformation("Resolved {Count} relic raid sessions", sessions.Count);
        return sessions;
    }

    private RelicRaidSession CreateSession(
        List<LogEvent> sessionEvents,
        string relicName,
        string playerName,
        List<LogEvent> allEvents)
    {
        var startTime = sessionEvents.First().Timestamp;
        var endTime = sessionEvents.Last().Timestamp;
        var duration = endTime - startTime;

        // Get relic info
        var relicInfo = RelicDatabase.GetByName(relicName);
        var relicType = relicInfo?.Type ?? RelicType.Strength;
        var originRealm = relicInfo?.HomeRealm ?? Realm.Albion;

        // Determine outcome
        var outcome = DetermineOutcome(sessionEvents);

        // Determine capturing realm
        var capturingRealm = sessionEvents
            .OfType<RelicCapturedEvent>()
            .LastOrDefault()?.CapturingRealm ?? Realm.Albion;

        // Get carrier names
        var carriers = sessionEvents
            .OfType<RelicPickupEvent>()
            .Select(e => e.CarrierName)
            .Distinct()
            .ToList();

        // Get combat events from the same time window
        var windowStart = startTime.Add(-TimeSpan.FromSeconds(30));
        var windowEnd = endTime.Add(TimeSpan.FromSeconds(30));
        var windowEvents = allEvents
            .Where(e => e.Timestamp >= windowStart && e.Timestamp <= windowEnd)
            .ToList();

        var contribution = CalculateContribution(windowEvents, playerName);

        // Check if player was carrier
        var playerWasCarrier = sessionEvents
            .OfType<RelicPickupEvent>()
            .Any(e => e.CarrierName.Equals(playerName, StringComparison.OrdinalIgnoreCase) ||
                      (playerName.Equals("You", StringComparison.OrdinalIgnoreCase) &&
                       e.CarrierName.Equals("You", StringComparison.OrdinalIgnoreCase)));

        return new RelicRaidSession(
            Id: Guid.NewGuid(),
            RelicName: relicName,
            RelicType: relicType,
            OriginRealm: originRealm,
            CapturingRealm: capturingRealm,
            StartTime: startTime,
            EndTime: endTime,
            Duration: duration,
            WasSuccessful: outcome == RelicRaidOutcome.Captured,
            Outcome: outcome,
            Carriers: carriers.AsReadOnly(),
            PlayerWasCarrier: playerWasCarrier,
            PlayerContribution: contribution,
            Events: sessionEvents.AsReadOnly()
        );
    }

    private RelicRaidOutcome DetermineOutcome(List<LogEvent> events)
    {
        // Check if relic was captured
        if (events.OfType<RelicCapturedEvent>().Any())
        {
            return RelicRaidOutcome.Captured;
        }

        // Check if relic was returned
        if (events.OfType<RelicReturnedEvent>().Any())
        {
            return RelicRaidOutcome.Returned;
        }

        // Check if carrier died (relic dropped)
        var drops = events.OfType<RelicDropEvent>().ToList();
        if (drops.Any(d => d.KillerName != null))
        {
            return RelicRaidOutcome.CarrierKilled;
        }

        return RelicRaidOutcome.Unknown;
    }

    /// <inheritdoc />
    public IReadOnlyDictionary<string, RelicStatus> GetRelicStatuses(IEnumerable<LogEvent> events)
    {
        var statuses = new Dictionary<string, RelicStatus>();

        // Initialize all relics as home
        foreach (var relic in RelicDatabase.Relics)
        {
            statuses[relic.Name] = RelicStatus.Home;
        }

        // Process events in order to determine final status
        foreach (var evt in events.OfType<RelicEvent>().OrderBy(e => e.Timestamp))
        {
            switch (evt)
            {
                case RelicPickupEvent:
                    statuses[evt.RelicName] = RelicStatus.InTransit;
                    break;
                case RelicDropEvent:
                    // Dropped relic reverts to unknown status until picked up or returned
                    statuses[evt.RelicName] = RelicStatus.Unknown;
                    break;
                case RelicCapturedEvent captured:
                    statuses[evt.RelicName] = captured.CapturingRealm == captured.OriginRealm
                        ? RelicStatus.Home
                        : RelicStatus.Captured;
                    break;
                case RelicReturnedEvent:
                    statuses[evt.RelicName] = RelicStatus.Home;
                    break;
            }
        }

        return statuses;
    }

    /// <inheritdoc />
    public RelicContribution CalculateContribution(IEnumerable<LogEvent> events, string playerName = "You")
    {
        var eventList = events.ToList();
        var isPlayer = playerName.Equals("You", StringComparison.OrdinalIgnoreCase);

        // Escort kills (kills while escorting carrier)
        var escortKills = eventList
            .OfType<DeathEvent>()
            .Count(e => isPlayer
                ? e.Killer?.Equals("You", StringComparison.OrdinalIgnoreCase) == true
                : e.Killer?.Equals(playerName, StringComparison.OrdinalIgnoreCase) == true);

        // Deaths
        var deaths = eventList
            .OfType<DeathEvent>()
            .Count(e => isPlayer
                ? e.Target.Equals("You", StringComparison.OrdinalIgnoreCase)
                : e.Target.Equals(playerName, StringComparison.OrdinalIgnoreCase));

        // Healing done
        var healingDone = eventList
            .OfType<HealingEvent>()
            .Where(e => isPlayer
                ? e.Source.Equals("You", StringComparison.OrdinalIgnoreCase)
                : e.Source.Equals(playerName, StringComparison.OrdinalIgnoreCase))
            .Sum(e => e.HealingAmount);

        // Check if player was carrier
        var wasCarrier = eventList
            .OfType<RelicPickupEvent>()
            .Any(e => isPlayer
                ? e.CarrierName.Equals("You", StringComparison.OrdinalIgnoreCase)
                : e.CarrierName.Equals(playerName, StringComparison.OrdinalIgnoreCase));

        // Check if player delivered the relic
        var deliveredRelic = wasCarrier && eventList.OfType<RelicCapturedEvent>().Any();

        return RelicContribution.Create(escortKills, deaths, healingDone, wasCarrier, deliveredRelic);
    }

    /// <inheritdoc />
    public RelicRaidStatistics CalculateStatistics(IEnumerable<RelicRaidSession> sessions)
    {
        var sessionList = sessions.ToList();

        if (sessionList.Count == 0)
        {
            return new RelicRaidStatistics(
                TotalRaidsParticipated: 0,
                SuccessfulRaids: 0,
                FailedRaids: 0,
                TimesAsCarrier: 0,
                SuccessfulDeliveries: 0,
                TotalEscortKills: 0,
                TotalDeaths: 0,
                TotalHealingDone: 0,
                TotalContributionScore: 0,
                AverageRaidDuration: TimeSpan.Zero,
                RaidsByRelic: new Dictionary<string, int>(),
                RaidsByOutcome: new Dictionary<RelicRaidOutcome, int>()
            );
        }

        var successfulRaids = sessionList.Count(s => s.WasSuccessful);
        var failedRaids = sessionList.Count(s => !s.WasSuccessful);
        var timesAsCarrier = sessionList.Count(s => s.PlayerWasCarrier);
        var successfulDeliveries = sessionList.Count(s => s.PlayerWasCarrier && s.WasSuccessful);

        var totalEscortKills = sessionList.Sum(s => s.PlayerContribution.EscortKills);
        var totalDeaths = sessionList.Sum(s => s.PlayerContribution.Deaths);
        var totalHealing = sessionList.Sum(s => s.PlayerContribution.HealingDone);
        var totalScore = sessionList.Sum(s => s.PlayerContribution.ContributionScore);

        var avgDuration = TimeSpan.FromTicks((long)sessionList.Average(s => s.Duration.Ticks));

        var byRelic = sessionList
            .GroupBy(s => s.RelicName)
            .ToDictionary(g => g.Key, g => g.Count());

        var byOutcome = sessionList
            .GroupBy(s => s.Outcome)
            .ToDictionary(g => g.Key, g => g.Count());

        return new RelicRaidStatistics(
            TotalRaidsParticipated: sessionList.Count,
            SuccessfulRaids: successfulRaids,
            FailedRaids: failedRaids,
            TimesAsCarrier: timesAsCarrier,
            SuccessfulDeliveries: successfulDeliveries,
            TotalEscortKills: totalEscortKills,
            TotalDeaths: totalDeaths,
            TotalHealingDone: totalHealing,
            TotalContributionScore: totalScore,
            AverageRaidDuration: avgDuration,
            RaidsByRelic: byRelic,
            RaidsByOutcome: byOutcome
        );
    }

    /// <inheritdoc />
    public IReadOnlyDictionary<string, CarrierStatistics> GetCarrierStatistics(IEnumerable<LogEvent> events)
    {
        var eventList = events.OfType<RelicEvent>().OrderBy(e => e.Timestamp).ToList();
        var carrierStats = new Dictionary<string, (int carried, int delivered, int dropped, TimeSpan carryTime)>();

        string? currentCarrier = null;
        TimeOnly? pickupTime = null;

        foreach (var evt in eventList)
        {
            switch (evt)
            {
                case RelicPickupEvent pickup:
                    currentCarrier = pickup.CarrierName;
                    pickupTime = pickup.Timestamp;

                    if (!carrierStats.ContainsKey(currentCarrier))
                    {
                        carrierStats[currentCarrier] = (0, 0, 0, TimeSpan.Zero);
                    }

                    var stats = carrierStats[currentCarrier];
                    carrierStats[currentCarrier] = (stats.carried + 1, stats.delivered, stats.dropped, stats.carryTime);
                    break;

                case RelicDropEvent drop:
                    if (currentCarrier != null && pickupTime.HasValue)
                    {
                        var carryDuration = drop.Timestamp - pickupTime.Value;
                        var s = carrierStats[currentCarrier];

                        if (drop.KillerName != null)
                        {
                            // Dropped due to death
                            carrierStats[currentCarrier] = (s.carried, s.delivered, s.dropped + 1, s.carryTime + carryDuration);
                        }
                        else
                        {
                            carrierStats[currentCarrier] = (s.carried, s.delivered, s.dropped, s.carryTime + carryDuration);
                        }
                    }
                    currentCarrier = null;
                    pickupTime = null;
                    break;

                case RelicCapturedEvent:
                    if (currentCarrier != null && pickupTime.HasValue)
                    {
                        var carryDuration = evt.Timestamp - pickupTime.Value;
                        var s = carrierStats[currentCarrier];
                        carrierStats[currentCarrier] = (s.carried, s.delivered + 1, s.dropped, s.carryTime + carryDuration);
                    }
                    currentCarrier = null;
                    pickupTime = null;
                    break;
            }
        }

        return carrierStats.ToDictionary(
            kvp => kvp.Key,
            kvp => new CarrierStatistics(
                CarrierName: kvp.Key,
                RelicsCarried: kvp.Value.carried,
                SuccessfulDeliveries: kvp.Value.delivered,
                DropsFromDeath: kvp.Value.dropped,
                TotalCarryTime: kvp.Value.carryTime
            )
        );
    }
}

