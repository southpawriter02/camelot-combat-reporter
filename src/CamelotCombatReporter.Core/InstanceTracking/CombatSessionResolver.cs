using CamelotCombatReporter.Core.Models;

namespace CamelotCombatReporter.Core.InstanceTracking;

/// <summary>
/// Resolves combat events into distinct combat sessions based on combat mode,
/// rest periods, and time gaps. Uses CombatInstanceResolver for per-target tracking
/// within each session.
/// </summary>
public class CombatSessionResolver : ICombatSessionResolver
{
    private readonly ICombatInstanceResolver _instanceResolver;

    /// <inheritdoc />
    public TimeSpan SessionTimeoutThreshold { get; set; } = TimeSpan.FromSeconds(60);

    /// <inheritdoc />
    public bool SplitOnRest { get; set; } = true;

    /// <inheritdoc />
    public bool SplitOnCombatModeEnter { get; set; } = true;

    public CombatSessionResolver()
    {
        _instanceResolver = new CombatInstanceResolver();
    }

    public CombatSessionResolver(ICombatInstanceResolver instanceResolver)
    {
        _instanceResolver = instanceResolver;
    }

    /// <inheritdoc />
    public IReadOnlyList<CombatSession> ResolveSessions(
        IReadOnlyList<LogEvent> events,
        string? playerName = null)
    {
        var sessions = new List<CombatSession>();
        ActiveSessionState? activeSession = null;
        var sessionNumber = 0;

        foreach (var evt in events.OrderBy(e => e.Timestamp))
        {
            // Check for session boundaries
            var boundaryReason = CheckSessionBoundary(evt, activeSession);

            if (boundaryReason != null && activeSession != null)
            {
                // Close current session
                var encounters = _instanceResolver.GetAllEncounters(activeSession.Events, playerName);
                activeSession.Encounters.AddRange(encounters);
                sessions.Add(activeSession.ToSession(boundaryReason.Value));
                activeSession = null;
            }

            // Determine if this event should start/continue a session
            if (IsCombatEvent(evt))
            {
                // Check for timeout since last event
                if (activeSession != null &&
                    (evt.Timestamp - activeSession.LastEventTime) > SessionTimeoutThreshold)
                {
                    // Close due to timeout
                    var encounters = _instanceResolver.GetAllEncounters(activeSession.Events, playerName);
                    activeSession.Encounters.AddRange(encounters);
                    sessions.Add(activeSession.ToSession(SessionEndReason.Timeout));
                    activeSession = null;
                }

                // Start new session if needed
                if (activeSession == null)
                {
                    sessionNumber++;
                    activeSession = new ActiveSessionState(sessionNumber, evt.Timestamp);
                }

                activeSession.Events.Add(evt);
                activeSession.LastEventTime = evt.Timestamp;
            }
            else if (activeSession != null)
            {
                // Non-combat event during active session - still track it
                activeSession.Events.Add(evt);
                activeSession.LastEventTime = evt.Timestamp;
            }
        }

        // Close any remaining active session
        if (activeSession != null)
        {
            var encounters = _instanceResolver.GetAllEncounters(activeSession.Events, playerName);
            activeSession.Encounters.AddRange(encounters);
            sessions.Add(activeSession.ToSession(SessionEndReason.EndOfLog));
        }

        return sessions;
    }

    /// <inheritdoc />
    public SessionStatistics GetSessionStatistics(
        IReadOnlyList<LogEvent> events,
        string? playerName = null)
    {
        var sessions = ResolveSessions(events, playerName);
        return new SessionStatistics(sessions);
    }

    /// <summary>
    /// Checks if the event represents a session boundary.
    /// </summary>
    private SessionEndReason? CheckSessionBoundary(LogEvent evt, ActiveSessionState? activeSession)
    {
        if (activeSession == null)
            return null;

        return evt switch
        {
            RestStartEvent when SplitOnRest => SessionEndReason.Rest,
            ChatLogBoundaryEvent boundary when !boundary.IsOpened => SessionEndReason.LogBoundary,
            CombatModeEnterEvent when SplitOnCombatModeEnter &&
                activeSession.Events.Any() &&
                (evt.Timestamp - activeSession.LastEventTime).TotalSeconds > 5 =>
                    SessionEndReason.CombatModeExit, // Previous session ended, new one starting
            _ => null
        };
    }

    /// <summary>
    /// Determines if an event is a combat-related event that should be tracked in sessions.
    /// </summary>
    private static bool IsCombatEvent(LogEvent evt)
    {
        return evt switch
        {
            DamageEvent => true,
            HealingEvent => true,
            DeathEvent => true,
            CriticalHitEvent => true,
            PetDamageEvent => true,
            CombatStyleEvent => true,
            SpellCastEvent => true,
            CrowdControlEvent => true,
            ResistEvent => true,
            CombatModeEnterEvent => true,
            _ => false
        };
    }
}
