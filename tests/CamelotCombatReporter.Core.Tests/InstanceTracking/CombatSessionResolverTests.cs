using CamelotCombatReporter.Core.InstanceTracking;
using CamelotCombatReporter.Core.Models;
using Xunit;

namespace CamelotCombatReporter.Core.Tests.InstanceTracking;

public class CombatSessionResolverTests
{
    private readonly CombatSessionResolver _resolver;
    private const string PlayerName = "TestPlayer";

    public CombatSessionResolverTests()
    {
        _resolver = new CombatSessionResolver();
    }

    [Fact]
    public void SingleCombatSequence_ReturnsOneSession()
    {
        // Arrange
        var events = new List<LogEvent>
        {
            new DamageEvent(new TimeOnly(10, 0, 0), PlayerName, "mob1", 100, "Crush"),
            new DamageEvent(new TimeOnly(10, 0, 5), PlayerName, "mob1", 100, "Crush"),
            new DeathEvent(new TimeOnly(10, 0, 10), "mob1", PlayerName)
        };

        // Act
        var sessions = _resolver.ResolveSessions(events, PlayerName);

        // Assert
        Assert.Single(sessions);
        Assert.Equal(1, sessions[0].SessionNumber);
        Assert.Equal(1, sessions[0].TotalKills);
        Assert.Equal(200, sessions[0].TotalDamageDealt);
        Assert.Equal(SessionEndReason.EndOfLog, sessions[0].EndReason);
    }

    [Fact]
    public void LargeTimeGap_CreatesSeparateSessions()
    {
        // Arrange - 2 minute gap (exceeds 60s default timeout)
        _resolver.SessionTimeoutThreshold = TimeSpan.FromSeconds(60);

        var events = new List<LogEvent>
        {
            // Session 1
            new DamageEvent(new TimeOnly(10, 0, 0), PlayerName, "mob1", 100, "Crush"),
            new DeathEvent(new TimeOnly(10, 0, 10), "mob1", PlayerName),

            // 2 minute gap
            // Session 2
            new DamageEvent(new TimeOnly(10, 2, 10), PlayerName, "mob2", 150, "Slash"),
            new DeathEvent(new TimeOnly(10, 2, 20), "mob2", PlayerName)
        };

        // Act
        var sessions = _resolver.ResolveSessions(events, PlayerName);

        // Assert
        Assert.Equal(2, sessions.Count);
        Assert.Equal(1, sessions[0].SessionNumber);
        Assert.Equal(2, sessions[1].SessionNumber);
        Assert.Equal(SessionEndReason.Timeout, sessions[0].EndReason);
        Assert.Equal(SessionEndReason.EndOfLog, sessions[1].EndReason);
    }

    [Fact]
    public void RestEvent_SplitsSession()
    {
        // Arrange
        var events = new List<LogEvent>
        {
            // Session 1
            new DamageEvent(new TimeOnly(10, 0, 0), PlayerName, "mob1", 100, "Crush"),
            new DeathEvent(new TimeOnly(10, 0, 10), "mob1", PlayerName),

            // Player rests
            new RestStartEvent(new TimeOnly(10, 0, 15)),

            // Session 2 starts after rest
            new DamageEvent(new TimeOnly(10, 0, 30), PlayerName, "mob2", 150, "Slash"),
            new DeathEvent(new TimeOnly(10, 0, 40), "mob2", PlayerName)
        };

        // Act
        var sessions = _resolver.ResolveSessions(events, PlayerName);

        // Assert
        Assert.Equal(2, sessions.Count);
        Assert.Equal(SessionEndReason.Rest, sessions[0].EndReason);
    }

    [Fact]
    public void RestEvent_DisableSplitting_SingleSession()
    {
        // Arrange
        _resolver.SplitOnRest = false;

        var events = new List<LogEvent>
        {
            new DamageEvent(new TimeOnly(10, 0, 0), PlayerName, "mob1", 100, "Crush"),
            new DeathEvent(new TimeOnly(10, 0, 10), "mob1", PlayerName),
            new RestStartEvent(new TimeOnly(10, 0, 15)),
            new DamageEvent(new TimeOnly(10, 0, 30), PlayerName, "mob2", 150, "Slash"),
            new DeathEvent(new TimeOnly(10, 0, 40), "mob2", PlayerName)
        };

        // Act
        var sessions = _resolver.ResolveSessions(events, PlayerName);

        // Assert
        Assert.Single(sessions);
        Assert.Equal(2, sessions[0].TotalKills);
    }

    [Fact]
    public void CombatModeEnter_AfterGap_SplitsSession()
    {
        // Arrange
        var events = new List<LogEvent>
        {
            // Session 1
            new DamageEvent(new TimeOnly(10, 0, 0), PlayerName, "mob1", 100, "Crush"),
            new DeathEvent(new TimeOnly(10, 0, 10), "mob1", PlayerName),

            // Gap then combat mode enter for session 2
            new CombatModeEnterEvent(new TimeOnly(10, 0, 20), "mob2"),
            new DamageEvent(new TimeOnly(10, 0, 21), PlayerName, "mob2", 150, "Slash"),
            new DeathEvent(new TimeOnly(10, 0, 30), "mob2", PlayerName)
        };

        // Act
        var sessions = _resolver.ResolveSessions(events, PlayerName);

        // Assert
        Assert.Equal(2, sessions.Count);
    }

    [Fact]
    public void ChatLogClose_EndsSession()
    {
        // Arrange
        var events = new List<LogEvent>
        {
            new DamageEvent(new TimeOnly(10, 0, 0), PlayerName, "mob1", 100, "Crush"),
            new DeathEvent(new TimeOnly(10, 0, 10), "mob1", PlayerName),
            new ChatLogBoundaryEvent(new TimeOnly(10, 0, 15), false, new DateTime(2024, 1, 1, 10, 0, 15))
        };

        // Act
        var sessions = _resolver.ResolveSessions(events, PlayerName);

        // Assert
        Assert.Single(sessions);
        Assert.Equal(SessionEndReason.LogBoundary, sessions[0].EndReason);
    }

    [Fact]
    public void SessionContainsEncounters()
    {
        // Arrange
        var events = new List<LogEvent>
        {
            new DamageEvent(new TimeOnly(10, 0, 0), PlayerName, "mob1", 100, "Crush"),
            new DeathEvent(new TimeOnly(10, 0, 5), "mob1", PlayerName),
            new DamageEvent(new TimeOnly(10, 0, 10), PlayerName, "mob2", 150, "Slash"),
            new DeathEvent(new TimeOnly(10, 0, 15), "mob2", PlayerName),
            new DamageEvent(new TimeOnly(10, 0, 20), PlayerName, "mob1", 120, "Crush"),
            new DeathEvent(new TimeOnly(10, 0, 25), "mob1", PlayerName)
        };

        // Act
        var sessions = _resolver.ResolveSessions(events, PlayerName);

        // Assert
        Assert.Single(sessions);
        Assert.Equal(3, sessions[0].Encounters.Count);
        Assert.Equal(3, sessions[0].TotalKills);
    }

    [Fact]
    public void SessionStatistics_AggregatesCorrectly()
    {
        // Arrange
        var events = new List<LogEvent>
        {
            // Session 1: 200 damage, 1 kill
            new DamageEvent(new TimeOnly(10, 0, 0), PlayerName, "mob1", 200, "Crush"),
            new DeathEvent(new TimeOnly(10, 0, 10), "mob1", PlayerName),

            // 2 minute gap
            // Session 2: 300 damage, 2 kills
            new DamageEvent(new TimeOnly(10, 2, 10), PlayerName, "mob2", 150, "Slash"),
            new DeathEvent(new TimeOnly(10, 2, 15), "mob2", PlayerName),
            new DamageEvent(new TimeOnly(10, 2, 20), PlayerName, "mob3", 150, "Slash"),
            new DeathEvent(new TimeOnly(10, 2, 25), "mob3", PlayerName)
        };

        // Act
        var stats = _resolver.GetSessionStatistics(events, PlayerName);

        // Assert
        Assert.Equal(2, stats.TotalSessions);
        Assert.Equal(3, stats.TotalKills);
        Assert.Equal(500, stats.TotalDamageDealt);
    }

    [Fact]
    public void SessionDuration_CalculatedCorrectly()
    {
        // Arrange
        var events = new List<LogEvent>
        {
            new DamageEvent(new TimeOnly(10, 0, 0), PlayerName, "mob1", 100, "Crush"),
            new DamageEvent(new TimeOnly(10, 0, 30), PlayerName, "mob1", 100, "Crush"),
            new DeathEvent(new TimeOnly(10, 1, 0), "mob1", PlayerName) // 1 minute total
        };

        // Act
        var sessions = _resolver.ResolveSessions(events, PlayerName);

        // Assert
        Assert.Single(sessions);
        Assert.Equal(TimeSpan.FromMinutes(1), sessions[0].Duration);
    }

    [Fact]
    public void SessionDps_CalculatedCorrectly()
    {
        // Arrange - 200 damage over 20 seconds = 10 DPS
        var events = new List<LogEvent>
        {
            new DamageEvent(new TimeOnly(10, 0, 0), PlayerName, "mob1", 100, "Crush"),
            new DamageEvent(new TimeOnly(10, 0, 10), PlayerName, "mob1", 100, "Crush"),
            new DeathEvent(new TimeOnly(10, 0, 20), "mob1", PlayerName)
        };

        // Act
        var sessions = _resolver.ResolveSessions(events, PlayerName);

        // Assert
        Assert.Single(sessions);
        Assert.Equal(10.0, sessions[0].Dps, 0.01);
    }

    [Fact]
    public void UniqueTargetCount_CalculatedCorrectly()
    {
        // Arrange - 3 encounters with 2 unique targets
        var events = new List<LogEvent>
        {
            new DamageEvent(new TimeOnly(10, 0, 0), PlayerName, "mob1", 100, "Crush"),
            new DeathEvent(new TimeOnly(10, 0, 5), "mob1", PlayerName),
            new DamageEvent(new TimeOnly(10, 0, 10), PlayerName, "mob2", 100, "Slash"),
            new DeathEvent(new TimeOnly(10, 0, 15), "mob2", PlayerName),
            new DamageEvent(new TimeOnly(10, 0, 20), PlayerName, "mob1", 100, "Crush"),
            new DeathEvent(new TimeOnly(10, 0, 25), "mob1", PlayerName)
        };

        // Act
        var sessions = _resolver.ResolveSessions(events, PlayerName);

        // Assert
        Assert.Single(sessions);
        Assert.Equal(2, sessions[0].UniqueTargetCount);
    }

    [Fact]
    public void EmptyEvents_ReturnsEmptySessions()
    {
        // Act
        var sessions = _resolver.ResolveSessions(new List<LogEvent>(), PlayerName);

        // Assert
        Assert.Empty(sessions);
    }

    [Fact]
    public void CustomTimeoutThreshold_Respected()
    {
        // Arrange - 30 second timeout
        _resolver.SessionTimeoutThreshold = TimeSpan.FromSeconds(30);

        var events = new List<LogEvent>
        {
            new DamageEvent(new TimeOnly(10, 0, 0), PlayerName, "mob1", 100, "Crush"),
            new DeathEvent(new TimeOnly(10, 0, 10), "mob1", PlayerName),
            // 35 second gap - should split with 30s timeout
            new DamageEvent(new TimeOnly(10, 0, 45), PlayerName, "mob2", 100, "Slash"),
            new DeathEvent(new TimeOnly(10, 0, 50), "mob2", PlayerName)
        };

        // Act
        var sessions = _resolver.ResolveSessions(events, PlayerName);

        // Assert
        Assert.Equal(2, sessions.Count);
    }

    [Fact]
    public void HealingEvents_IncludedInSession()
    {
        // Arrange
        var events = new List<LogEvent>
        {
            new DamageEvent(new TimeOnly(10, 0, 0), PlayerName, "mob1", 100, "Crush"),
            new HealingEvent(new TimeOnly(10, 0, 5), PlayerName, PlayerName, 50),
            new DamageEvent(new TimeOnly(10, 0, 10), PlayerName, "mob1", 100, "Crush"),
            new DeathEvent(new TimeOnly(10, 0, 15), "mob1", PlayerName)
        };

        // Act
        var sessions = _resolver.ResolveSessions(events, PlayerName);

        // Assert
        Assert.Single(sessions);
        Assert.Equal(4, sessions[0].Events.Count);
    }
}
