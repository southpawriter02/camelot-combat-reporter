using CamelotCombatReporter.Core.Models;
using CamelotCombatReporter.Core.RvR;
using CamelotCombatReporter.Core.RvR.Models;
using Xunit;

namespace CamelotCombatReporter.Core.Tests.RvR;

public class RelicTrackingServiceTests
{
    private readonly RelicTrackingService _service = new();

    [Fact]
    public void ExtractRelicEvents_WithMixedEvents_ReturnsOnlyRelicEvents()
    {
        // Arrange
        var events = new List<LogEvent>
        {
            new DamageEvent(new TimeOnly(12, 0, 0), "You", "Enemy", 100, "Crush"),
            new RelicPickupEvent(new TimeOnly(12, 0, 10), "Thor's Hammer", RelicType.Strength, "PlayerName", Realm.Midgard),
            new HealingEvent(new TimeOnly(12, 0, 20), "Healer", "You", 200)
        };

        // Act
        var relicEvents = _service.ExtractRelicEvents(events);

        // Assert
        Assert.Single(relicEvents);
        Assert.IsType<RelicPickupEvent>(relicEvents[0]);
    }

    [Fact]
    public void ResolveSessions_WithSingleRelic_CreatesOneSession()
    {
        // Arrange
        var events = new List<LogEvent>
        {
            new RelicPickupEvent(new TimeOnly(12, 0, 0), "Thor's Hammer", RelicType.Strength, "Player1", Realm.Midgard),
            new RelicCapturedEvent(new TimeOnly(12, 5, 0), "Thor's Hammer", RelicType.Strength, Realm.Albion, Realm.Midgard)
        };

        // Act
        var sessions = _service.ResolveSessions(events);

        // Assert
        Assert.Single(sessions);
        Assert.Equal("Thor's Hammer", sessions[0].RelicName);
        Assert.True(sessions[0].WasSuccessful);
    }

    [Fact]
    public void ResolveSessions_WithDifferentRelics_CreatesSeparateSessions()
    {
        // Arrange
        var events = new List<LogEvent>
        {
            new RelicPickupEvent(new TimeOnly(12, 0, 0), "Thor's Hammer", RelicType.Strength, "Player1", Realm.Midgard),
            new RelicPickupEvent(new TimeOnly(12, 1, 0), "Merlin's Staff", RelicType.Power, "Player2", Realm.Albion)
        };

        // Act
        var sessions = _service.ResolveSessions(events);

        // Assert
        Assert.Equal(2, sessions.Count);
    }

    [Fact]
    public void GetRelicStatuses_InitializesAllRelicsAsHome()
    {
        // Arrange
        var events = new List<LogEvent>();

        // Act
        var statuses = _service.GetRelicStatuses(events);

        // Assert
        Assert.Equal(6, statuses.Count); // 6 relics in the database
        Assert.All(statuses.Values, s => Assert.Equal(RelicStatus.Home, s));
    }

    [Fact]
    public void GetRelicStatuses_WithPickup_SetsInTransit()
    {
        // Arrange
        var events = new List<LogEvent>
        {
            new RelicPickupEvent(new TimeOnly(12, 0, 0), "Thor's Hammer", RelicType.Strength, "Player1", Realm.Midgard)
        };

        // Act
        var statuses = _service.GetRelicStatuses(events);

        // Assert
        Assert.Equal(RelicStatus.InTransit, statuses["Thor's Hammer"]);
    }

    [Fact]
    public void GetRelicStatuses_WithCapture_SetsCaptured()
    {
        // Arrange
        var events = new List<LogEvent>
        {
            new RelicCapturedEvent(new TimeOnly(12, 0, 0), "Thor's Hammer", RelicType.Strength, Realm.Albion, Realm.Midgard)
        };

        // Act
        var statuses = _service.GetRelicStatuses(events);

        // Assert
        Assert.Equal(RelicStatus.Captured, statuses["Thor's Hammer"]);
    }

    [Fact]
    public void GetRelicStatuses_WithReturn_SetsHome()
    {
        // Arrange
        var events = new List<LogEvent>
        {
            new RelicCapturedEvent(new TimeOnly(12, 0, 0), "Thor's Hammer", RelicType.Strength, Realm.Albion, Realm.Midgard),
            new RelicReturnedEvent(new TimeOnly(12, 5, 0), "Thor's Hammer", RelicType.Strength, Realm.Midgard)
        };

        // Act
        var statuses = _service.GetRelicStatuses(events);

        // Assert
        Assert.Equal(RelicStatus.Home, statuses["Thor's Hammer"]);
    }

    [Fact]
    public void CalculateContribution_WithPlayerAsCarrier_IncludesCarrierBonus()
    {
        // Arrange
        var events = new List<LogEvent>
        {
            new RelicPickupEvent(new TimeOnly(12, 0, 0), "Thor's Hammer", RelicType.Strength, "You", Realm.Midgard),
            new RelicCapturedEvent(new TimeOnly(12, 5, 0), "Thor's Hammer", RelicType.Strength, Realm.Albion, Realm.Midgard)
        };

        // Act
        var contribution = _service.CalculateContribution(events);

        // Assert
        Assert.True(contribution.WasCarrier);
        Assert.True(contribution.DeliveredRelic);
        Assert.True(contribution.ContributionScore > 0);
    }

    [Fact]
    public void CalculateStatistics_WithMultipleSessions_AggregatesCorrectly()
    {
        // Arrange
        var events = new List<LogEvent>
        {
            new RelicPickupEvent(new TimeOnly(12, 0, 0), "Thor's Hammer", RelicType.Strength, "Player1", Realm.Midgard),
            new RelicCapturedEvent(new TimeOnly(12, 5, 0), "Thor's Hammer", RelicType.Strength, Realm.Albion, Realm.Midgard),
            new RelicPickupEvent(new TimeOnly(13, 0, 0), "Merlin's Staff", RelicType.Power, "Player2", Realm.Albion),
            new RelicDropEvent(new TimeOnly(13, 2, 0), "Merlin's Staff", RelicType.Power, "Player2", "Enemy")
        };
        var sessions = _service.ResolveSessions(events);

        // Act
        var stats = _service.CalculateStatistics(sessions);

        // Assert
        Assert.Equal(2, stats.TotalRaidsParticipated);
        Assert.Equal(1, stats.SuccessfulRaids);
        Assert.Equal(1, stats.FailedRaids);
    }

    [Fact]
    public void GetCarrierStatistics_TracksCarrierPerformance()
    {
        // Arrange
        var events = new List<LogEvent>
        {
            new RelicPickupEvent(new TimeOnly(12, 0, 0), "Thor's Hammer", RelicType.Strength, "Player1", Realm.Midgard),
            new RelicCapturedEvent(new TimeOnly(12, 5, 0), "Thor's Hammer", RelicType.Strength, Realm.Albion, Realm.Midgard)
        };

        // Act
        var carrierStats = _service.GetCarrierStatistics(events);

        // Assert
        Assert.True(carrierStats.ContainsKey("Player1"));
        Assert.Equal(1, carrierStats["Player1"].RelicsCarried);
        Assert.Equal(1, carrierStats["Player1"].SuccessfulDeliveries);
    }
}
