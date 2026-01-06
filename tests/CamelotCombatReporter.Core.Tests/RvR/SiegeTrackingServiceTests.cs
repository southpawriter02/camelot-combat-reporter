using CamelotCombatReporter.Core.Models;
using CamelotCombatReporter.Core.RvR;
using CamelotCombatReporter.Core.RvR.Models;
using Xunit;

namespace CamelotCombatReporter.Core.Tests.RvR;

public class SiegeTrackingServiceTests
{
    private readonly SiegeTrackingService _service = new();

    [Fact]
    public void ExtractSiegeEvents_WithMixedEvents_ReturnsOnlySiegeEvents()
    {
        // Arrange
        var events = new List<LogEvent>
        {
            new DamageEvent(new TimeOnly(12, 0, 0), "You", "Enemy", 100, "Crush"),
            new DoorDamageEvent(new TimeOnly(12, 0, 10), "Castle Sauvage", "Outer Door", 500, "You", false),
            new HealingEvent(new TimeOnly(12, 0, 20), "Healer", "You", 200),
            new GuardKillEvent(new TimeOnly(12, 0, 30), "Castle Sauvage", "Keep Guard", "You", false)
        };

        // Act
        var siegeEvents = _service.ExtractSiegeEvents(events);

        // Assert
        Assert.Equal(2, siegeEvents.Count);
        Assert.IsType<DoorDamageEvent>(siegeEvents[0]);
        Assert.IsType<GuardKillEvent>(siegeEvents[1]);
    }

    [Fact]
    public void ExtractSiegeEvents_WithNoSiegeEvents_ReturnsEmptyList()
    {
        // Arrange
        var events = new List<LogEvent>
        {
            new DamageEvent(new TimeOnly(12, 0, 0), "You", "Enemy", 100, "Crush"),
            new HealingEvent(new TimeOnly(12, 0, 20), "Healer", "You", 200)
        };

        // Act
        var siegeEvents = _service.ExtractSiegeEvents(events);

        // Assert
        Assert.Empty(siegeEvents);
    }

    [Fact]
    public void ResolveSessions_WithSingleKeep_CreatesOneSession()
    {
        // Arrange
        var events = new List<LogEvent>
        {
            new DoorDamageEvent(new TimeOnly(12, 0, 0), "Castle Sauvage", "Outer Door", 500, "You", false),
            new DoorDamageEvent(new TimeOnly(12, 0, 30), "Castle Sauvage", "Outer Door", 500, "You", false),
            new DoorDamageEvent(new TimeOnly(12, 1, 0), "Castle Sauvage", "Outer Door", 500, "You", true)
        };

        // Act
        var sessions = _service.ResolveSessions(events);

        // Assert
        Assert.Single(sessions);
        Assert.Equal("Castle Sauvage", sessions[0].KeepName);
    }

    [Fact]
    public void ResolveSessions_WithDifferentKeeps_CreatesSeparateSessions()
    {
        // Arrange
        var events = new List<LogEvent>
        {
            new DoorDamageEvent(new TimeOnly(12, 0, 0), "Castle Sauvage", "Outer Door", 500, "You", false),
            new DoorDamageEvent(new TimeOnly(12, 1, 0), "Bledmeer Faste", "Outer Door", 500, "You", false)
        };

        // Act
        var sessions = _service.ResolveSessions(events);

        // Assert
        Assert.Equal(2, sessions.Count);
        Assert.Contains(sessions, s => s.KeepName == "Castle Sauvage");
        Assert.Contains(sessions, s => s.KeepName == "Bledmeer Faste");
    }

    [Fact]
    public void ResolveSessions_WithLargeTimeGap_CreatesSeparateSessions()
    {
        // Arrange
        _service.SessionGapThreshold = TimeSpan.FromMinutes(5);
        var events = new List<LogEvent>
        {
            new DoorDamageEvent(new TimeOnly(12, 0, 0), "Castle Sauvage", "Outer Door", 500, "You", false),
            new DoorDamageEvent(new TimeOnly(12, 10, 0), "Castle Sauvage", "Outer Door", 500, "You", false)
        };

        // Act
        var sessions = _service.ResolveSessions(events);

        // Assert
        Assert.Equal(2, sessions.Count);
    }

    [Fact]
    public void DetectPhase_WithNoDoorsDown_ReturnsApproach()
    {
        // Arrange
        var events = new List<LogEvent>
        {
            new DoorDamageEvent(new TimeOnly(12, 0, 0), "Castle Sauvage", "Outer Door", 500, "You", false)
        };
        var sessions = _service.ResolveSessions(events);

        // Act
        var phase = _service.DetectPhase(sessions[0]);

        // Assert
        Assert.Equal(SiegePhase.Approach, phase);
    }

    [Fact]
    public void DetectPhase_WithOuterDoorDestroyed_ReturnsOuterSiege()
    {
        // Arrange
        var events = new List<LogEvent>
        {
            new DoorDamageEvent(new TimeOnly(12, 0, 0), "Castle Sauvage", "Outer Door", 500, "You", true)
        };
        var sessions = _service.ResolveSessions(events);

        // Act
        var phase = _service.DetectPhase(sessions[0]);

        // Assert
        Assert.Equal(SiegePhase.OuterSiege, phase);
    }

    [Fact]
    public void DetectPhase_WithInnerDoorDestroyed_ReturnsInnerSiege()
    {
        // Arrange
        var events = new List<LogEvent>
        {
            new DoorDamageEvent(new TimeOnly(12, 0, 0), "Castle Sauvage", "Inner Door", 500, "You", true)
        };
        var sessions = _service.ResolveSessions(events);

        // Act
        var phase = _service.DetectPhase(sessions[0]);

        // Assert
        Assert.Equal(SiegePhase.InnerSiege, phase);
    }

    [Fact]
    public void DetectPhase_WithLordKilled_ReturnsLordFight()
    {
        // Arrange
        var events = new List<LogEvent>
        {
            new GuardKillEvent(new TimeOnly(12, 0, 0), "Castle Sauvage", "Lord Arawn", "You", true)
        };
        var sessions = _service.ResolveSessions(events);

        // Act
        var phase = _service.DetectPhase(sessions[0]);

        // Assert
        Assert.Equal(SiegePhase.LordFight, phase);
    }

    [Fact]
    public void DetectPhase_WithKeepCaptured_ReturnsCapture()
    {
        // Arrange
        var events = new List<LogEvent>
        {
            new KeepCapturedEvent(new TimeOnly(12, 0, 0), "Castle Sauvage", Realm.Albion, null, "Test Guild")
        };
        var sessions = _service.ResolveSessions(events);

        // Act
        var phase = _service.DetectPhase(sessions[0]);

        // Assert
        Assert.Equal(SiegePhase.Capture, phase);
    }

    [Fact]
    public void CalculateContribution_WithPlayerActions_CalculatesCorrectScore()
    {
        // Arrange
        var events = new List<LogEvent>
        {
            new DoorDamageEvent(new TimeOnly(12, 0, 0), "Castle Sauvage", "Outer Door", 1000, "You", false),
            new DeathEvent(new TimeOnly(12, 0, 30), "Enemy", "You"),
            new GuardKillEvent(new TimeOnly(12, 1, 0), "Castle Sauvage", "Keep Guard", "You", false)
        };

        // Act
        var contribution = _service.CalculateContribution(events);

        // Assert
        Assert.Equal(1000, contribution.StructureDamage);
        Assert.Equal(1, contribution.PlayerKills);
        Assert.Equal(1, contribution.GuardKills);
        Assert.True(contribution.ContributionScore > 0);
    }

    [Fact]
    public void CalculateStatistics_WithMultipleSessions_AggregatesCorrectly()
    {
        // Arrange
        var events = new List<LogEvent>
        {
            new DoorDamageEvent(new TimeOnly(12, 0, 0), "Castle Sauvage", "Outer Door", 500, "You", false),
            new KeepCapturedEvent(new TimeOnly(12, 1, 0), "Castle Sauvage", Realm.Albion, null, null),
            new DoorDamageEvent(new TimeOnly(13, 0, 0), "Bledmeer Faste", "Outer Door", 500, "You", false)
        };
        var sessions = _service.ResolveSessions(events);

        // Act
        var stats = _service.CalculateStatistics(sessions);

        // Assert
        Assert.Equal(2, stats.TotalSiegesParticipated);
        Assert.True(stats.SiegesByKeep.ContainsKey("Castle Sauvage"));
        Assert.True(stats.SiegesByKeep.ContainsKey("Bledmeer Faste"));
    }

    [Fact]
    public void BuildTimeline_CreatesEntriesForEachEvent()
    {
        // Arrange
        var events = new List<LogEvent>
        {
            new DoorDamageEvent(new TimeOnly(12, 0, 0), "Castle Sauvage", "Outer Door", 500, "You", false),
            new DoorDamageEvent(new TimeOnly(12, 1, 0), "Castle Sauvage", "Outer Door", 0, "Ram", true),
            new GuardKillEvent(new TimeOnly(12, 2, 0), "Castle Sauvage", "Keep Guard", "You", false)
        };
        var sessions = _service.ResolveSessions(events);

        // Act
        var timeline = _service.BuildTimeline(sessions[0]);

        // Assert
        Assert.Equal(3, timeline.Count);
        Assert.Equal("Door Damage", timeline[0].EventType);
        Assert.Equal("Door Destroyed", timeline[1].EventType);
        Assert.Equal("Guard Killed", timeline[2].EventType);
    }
}
