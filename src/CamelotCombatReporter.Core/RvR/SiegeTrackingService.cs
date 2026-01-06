using CamelotCombatReporter.Core.Models;
using CamelotCombatReporter.Core.RvR.Models;
using Microsoft.Extensions.Logging;

namespace CamelotCombatReporter.Core.RvR;

/// <summary>
/// Service for tracking and analyzing keep sieges.
/// </summary>
public class SiegeTrackingService : ISiegeTrackingService
{
    private readonly ILogger<SiegeTrackingService>? _logger;

    /// <inheritdoc />
    public TimeSpan SessionGapThreshold { get; set; } = TimeSpan.FromMinutes(5);

    public SiegeTrackingService(ILogger<SiegeTrackingService>? logger = null)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public IReadOnlyList<SiegeEvent> ExtractSiegeEvents(IEnumerable<LogEvent> events)
    {
        return events
            .OfType<SiegeEvent>()
            .OrderBy(e => e.Timestamp)
            .ToList();
    }

    /// <inheritdoc />
    public IReadOnlyList<SiegeSession> ResolveSessions(IEnumerable<LogEvent> events, string playerName = "You")
    {
        var allEvents = events.ToList();
        var siegeEvents = ExtractSiegeEvents(allEvents);

        if (siegeEvents.Count == 0)
        {
            _logger?.LogDebug("No siege events found");
            return Array.Empty<SiegeSession>();
        }

        var sessions = new List<SiegeSession>();
        var currentSessionEvents = new List<LogEvent>();
        string? currentKeepName = null;
        TimeOnly? lastEventTime = null;

        foreach (var evt in siegeEvents)
        {
            var startNewSession = false;

            // Check if we should start a new session
            if (currentKeepName == null)
            {
                startNewSession = true;
            }
            else if (evt.KeepName != currentKeepName)
            {
                // Different keep = new session
                startNewSession = true;
            }
            else if (lastEventTime.HasValue)
            {
                // Check time gap
                var gap = evt.Timestamp - lastEventTime.Value;
                if (gap > SessionGapThreshold)
                {
                    startNewSession = true;
                }
            }

            if (startNewSession && currentSessionEvents.Count > 0 && currentKeepName != null)
            {
                // Save the current session
                var session = CreateSession(currentSessionEvents, currentKeepName, playerName, allEvents);
                sessions.Add(session);
                currentSessionEvents.Clear();
            }

            if (startNewSession)
            {
                currentKeepName = evt.KeepName;
            }

            currentSessionEvents.Add(evt);
            lastEventTime = evt.Timestamp;
        }

        // Save final session
        if (currentSessionEvents.Count > 0 && currentKeepName != null)
        {
            var session = CreateSession(currentSessionEvents, currentKeepName, playerName, allEvents);
            sessions.Add(session);
        }

        _logger?.LogInformation("Resolved {Count} siege sessions", sessions.Count);
        return sessions;
    }

    private SiegeSession CreateSession(
        List<LogEvent> sessionEvents,
        string keepName,
        string playerName,
        List<LogEvent> allEvents)
    {
        var startTime = sessionEvents.First().Timestamp;
        var endTime = sessionEvents.Last().Timestamp;
        var duration = endTime - startTime;

        // Get keep info
        var keepInfo = KeepDatabase.GetByName(keepName);
        var keepType = keepInfo?.Type ?? KeepType.BorderKeep;

        // Determine outcome
        var outcome = DetermineOutcome(sessionEvents);

        // Determine realms (simplified - would need more context in real scenarios)
        var attackingRealm = Realm.Albion;
        var defendingRealm = keepInfo?.HomeRealm ?? Realm.Midgard;

        // Check if player was attacker (if they did damage to doors)
        var playerWasAttacker = sessionEvents
            .OfType<DoorDamageEvent>()
            .Any(e => e.Source.Equals(playerName, StringComparison.OrdinalIgnoreCase));

        // Get combat events from the same time window for contribution calculation
        var windowStart = startTime.Add(-TimeSpan.FromSeconds(30));
        var windowEnd = endTime.Add(TimeSpan.FromSeconds(30));
        var windowEvents = allEvents
            .Where(e => e.Timestamp >= windowStart && e.Timestamp <= windowEnd)
            .ToList();

        var contribution = CalculateContribution(windowEvents, playerName);
        var finalPhase = DetectPhaseFromEvents(sessionEvents);

        return new SiegeSession(
            Id: Guid.NewGuid(),
            KeepName: keepName,
            KeepType: keepType,
            StartTime: startTime,
            EndTime: endTime,
            Outcome: outcome,
            AttackingRealm: attackingRealm,
            DefendingRealm: defendingRealm,
            Events: sessionEvents.AsReadOnly(),
            PlayerContribution: contribution,
            FinalPhase: finalPhase,
            Duration: duration,
            PlayerWasAttacker: playerWasAttacker
        );
    }

    private SiegeOutcome DetermineOutcome(List<LogEvent> events)
    {
        // Check for keep captured event
        if (events.OfType<KeepCapturedEvent>().Any())
        {
            return SiegeOutcome.AttackSuccess;
        }

        // Check if lord was killed
        if (events.OfType<GuardKillEvent>().Any(e => e.IsLordKill))
        {
            return SiegeOutcome.AttackSuccess;
        }

        // If only outer door was breached but siege ended, likely defense success
        var outerDoorDown = events.OfType<DoorDamageEvent>()
            .Any(e => e.IsDestroyed && e.DoorName.Contains("Outer", StringComparison.OrdinalIgnoreCase));
        var innerDoorDown = events.OfType<DoorDamageEvent>()
            .Any(e => e.IsDestroyed && e.DoorName.Contains("Inner", StringComparison.OrdinalIgnoreCase));

        if (!outerDoorDown)
        {
            return SiegeOutcome.DefenseSuccess;
        }

        // Can't determine for sure
        return SiegeOutcome.Unknown;
    }

    private SiegePhase DetectPhaseFromEvents(List<LogEvent> events)
    {
        // Check for capture
        if (events.OfType<KeepCapturedEvent>().Any())
        {
            return SiegePhase.Capture;
        }

        // Check for lord kill
        if (events.OfType<GuardKillEvent>().Any(e => e.IsLordKill))
        {
            return SiegePhase.LordFight;
        }

        // Check door states
        var innerDoorDown = events.OfType<DoorDamageEvent>()
            .Any(e => e.IsDestroyed && e.DoorName.Contains("Inner", StringComparison.OrdinalIgnoreCase));
        if (innerDoorDown)
        {
            return SiegePhase.InnerSiege;
        }

        var outerDoorDown = events.OfType<DoorDamageEvent>()
            .Any(e => e.IsDestroyed && e.DoorName.Contains("Outer", StringComparison.OrdinalIgnoreCase));
        if (outerDoorDown)
        {
            return SiegePhase.OuterSiege;
        }

        return SiegePhase.Approach;
    }

    /// <inheritdoc />
    public SiegePhase DetectPhase(SiegeSession session)
    {
        return DetectPhaseFromEvents(session.Events.ToList());
    }

    /// <inheritdoc />
    public SiegeContribution CalculateContribution(IEnumerable<LogEvent> events, string playerName = "You")
    {
        var eventList = events.ToList();
        var isPlayer = playerName.Equals("You", StringComparison.OrdinalIgnoreCase);

        // Structure damage
        var structureDamage = eventList
            .OfType<DoorDamageEvent>()
            .Where(e => isPlayer
                ? e.Source.Equals("You", StringComparison.OrdinalIgnoreCase)
                : e.Source.Equals(playerName, StringComparison.OrdinalIgnoreCase))
            .Sum(e => e.DamageAmount);

        // Player kills
        var playerKills = eventList
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

        // Guard kills
        var guardKills = eventList
            .OfType<GuardKillEvent>()
            .Count(e => isPlayer
                ? e.Killer.Equals("You", StringComparison.OrdinalIgnoreCase)
                : e.Killer.Equals(playerName, StringComparison.OrdinalIgnoreCase));

        return SiegeContribution.Create(structureDamage, playerKills, deaths, healingDone, guardKills);
    }

    /// <inheritdoc />
    public SiegeStatistics CalculateStatistics(IEnumerable<SiegeSession> sessions)
    {
        var sessionList = sessions.ToList();

        if (sessionList.Count == 0)
        {
            return new SiegeStatistics(
                TotalSiegesParticipated: 0,
                AttackVictories: 0,
                DefenseVictories: 0,
                TotalStructureDamage: 0,
                TotalPlayerKills: 0,
                TotalDeaths: 0,
                TotalGuardKills: 0,
                TotalContributionScore: 0,
                AverageSiegeDuration: TimeSpan.Zero,
                SiegesByKeep: new Dictionary<string, int>(),
                SiegesByFinalPhase: new Dictionary<SiegePhase, int>()
            );
        }

        var attackVictories = sessionList.Count(s =>
            s.Outcome == SiegeOutcome.AttackSuccess && s.PlayerWasAttacker);
        var defenseVictories = sessionList.Count(s =>
            s.Outcome == SiegeOutcome.DefenseSuccess && !s.PlayerWasAttacker);

        var totalDamage = sessionList.Sum(s => s.PlayerContribution.StructureDamage);
        var totalKills = sessionList.Sum(s => s.PlayerContribution.PlayerKills);
        var totalDeaths = sessionList.Sum(s => s.PlayerContribution.Deaths);
        var totalGuardKills = sessionList.Sum(s => s.PlayerContribution.GuardKills);
        var totalScore = sessionList.Sum(s => s.PlayerContribution.ContributionScore);

        var avgDuration = TimeSpan.FromTicks((long)sessionList.Average(s => s.Duration.Ticks));

        var byKeep = sessionList
            .GroupBy(s => s.KeepName)
            .ToDictionary(g => g.Key, g => g.Count());

        var byPhase = sessionList
            .GroupBy(s => s.FinalPhase)
            .ToDictionary(g => g.Key, g => g.Count());

        return new SiegeStatistics(
            TotalSiegesParticipated: sessionList.Count,
            AttackVictories: attackVictories,
            DefenseVictories: defenseVictories,
            TotalStructureDamage: totalDamage,
            TotalPlayerKills: totalKills,
            TotalDeaths: totalDeaths,
            TotalGuardKills: totalGuardKills,
            TotalContributionScore: totalScore,
            AverageSiegeDuration: avgDuration,
            SiegesByKeep: byKeep,
            SiegesByFinalPhase: byPhase
        );
    }

    /// <inheritdoc />
    public IReadOnlyList<SiegeTimelineEntry> BuildTimeline(SiegeSession session)
    {
        var entries = new List<SiegeTimelineEntry>();
        var currentPhase = SiegePhase.Approach;

        foreach (var evt in session.Events.OrderBy(e => e.Timestamp))
        {
            string eventType;
            string description;
            var isPlayerAction = false;

            switch (evt)
            {
                case DoorDamageEvent door when door.IsDestroyed:
                    eventType = "Door Destroyed";
                    description = $"{door.DoorName} of {door.KeepName} destroyed";
                    // Update phase
                    if (door.DoorName.Contains("Inner", StringComparison.OrdinalIgnoreCase))
                        currentPhase = SiegePhase.InnerSiege;
                    else if (door.DoorName.Contains("Outer", StringComparison.OrdinalIgnoreCase))
                        currentPhase = SiegePhase.OuterSiege;
                    break;

                case DoorDamageEvent door:
                    eventType = "Door Damage";
                    description = $"{door.Source} hit {door.DoorName} for {door.DamageAmount}";
                    isPlayerAction = door.Source.Equals("You", StringComparison.OrdinalIgnoreCase);
                    break;

                case GuardKillEvent guard when guard.IsLordKill:
                    eventType = "Lord Killed";
                    description = $"{guard.GuardName} slain";
                    currentPhase = SiegePhase.LordFight;
                    isPlayerAction = guard.Killer.Equals("You", StringComparison.OrdinalIgnoreCase);
                    break;

                case GuardKillEvent guard:
                    eventType = "Guard Killed";
                    description = $"{guard.Killer} killed {guard.GuardName}";
                    isPlayerAction = guard.Killer.Equals("You", StringComparison.OrdinalIgnoreCase);
                    break;

                case KeepCapturedEvent capture:
                    eventType = "Keep Captured";
                    description = $"{capture.KeepName} captured by {capture.NewOwner}";
                    if (capture.ClaimingGuild != null)
                        description += $" ({capture.ClaimingGuild})";
                    currentPhase = SiegePhase.Capture;
                    break;

                case SiegeWeaponEvent weapon when weapon.IsDeployed:
                    eventType = "Siege Deployed";
                    description = $"{weapon.PlayerName} deployed {weapon.WeaponType}";
                    isPlayerAction = weapon.PlayerName.Equals("You", StringComparison.OrdinalIgnoreCase);
                    break;

                case SiegeWeaponEvent weapon:
                    eventType = "Siege Destroyed";
                    description = $"{weapon.WeaponType} destroyed";
                    break;

                default:
                    eventType = "Event";
                    description = evt.GetType().Name;
                    break;
            }

            entries.Add(new SiegeTimelineEntry(
                Timestamp: evt.Timestamp,
                EventType: eventType,
                Description: description,
                Phase: currentPhase,
                IsPlayerAction: isPlayerAction
            ));
        }

        return entries;
    }
}
