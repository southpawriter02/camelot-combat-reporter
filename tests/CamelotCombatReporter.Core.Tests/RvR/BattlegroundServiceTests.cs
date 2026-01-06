using CamelotCombatReporter.Core.Models;
using CamelotCombatReporter.Core.RvR;
using CamelotCombatReporter.Core.RvR.Models;
using Xunit;

namespace CamelotCombatReporter.Core.Tests.RvR;

public class BattlegroundServiceTests
{
    private readonly BattlegroundService _service = new();

    [Fact]
    public void ExtractZoneEntries_WithMixedEvents_ReturnsOnlyZoneEntries()
    {
        // Arrange
        var events = new List<LogEvent>
        {
            new DamageEvent(new TimeOnly(12, 0, 0), "You", "Enemy", 100, "Crush"),
            new ZoneEntryEvent(new TimeOnly(12, 0, 10), "Thidranki"),
            new HealingEvent(new TimeOnly(12, 0, 20), "Healer", "You", 200)
        };

        // Act
        var zoneEntries = _service.ExtractZoneEntries(events);

        // Assert
        Assert.Single(zoneEntries);
        Assert.Equal("Thidranki", zoneEntries[0].ZoneName);
    }

    [Fact]
    public void GetBattlegroundType_WithThidranki_ReturnsCorrectType()
    {
        // Act
        var result = _service.GetBattlegroundType("Thidranki");

        // Assert
        Assert.Equal(BattlegroundType.Thidranki, result);
    }

    [Fact]
    public void GetBattlegroundType_WithMolvik_ReturnsCorrectType()
    {
        // Act
        var result = _service.GetBattlegroundType("Molvik");

        // Assert
        Assert.Equal(BattlegroundType.Molvik, result);
    }

    [Fact]
    public void GetBattlegroundType_WithCathalValley_ReturnsCorrectType()
    {
        // Act
        var result = _service.GetBattlegroundType("Cathal Valley");

        // Assert
        Assert.Equal(BattlegroundType.CathalValley, result);
    }

    [Fact]
    public void GetBattlegroundType_WithKillaloe_ReturnsCorrectType()
    {
        // Act
        var result = _service.GetBattlegroundType("Killaloe");

        // Assert
        Assert.Equal(BattlegroundType.Killaloe, result);
    }

    [Fact]
    public void GetBattlegroundType_WithNonBgZone_ReturnsNull()
    {
        // Act
        var result = _service.GetBattlegroundType("Camelot");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void ResolveSessions_WithSingleBgEntry_CreatesOneSession()
    {
        // Arrange
        var events = new List<LogEvent>
        {
            new ZoneEntryEvent(new TimeOnly(12, 0, 0), "Thidranki"),
            new DamageEvent(new TimeOnly(12, 1, 0), "You", "Enemy", 100, "Crush"),
            new DeathEvent(new TimeOnly(12, 2, 0), "Enemy", "You")
        };

        // Act
        var sessions = _service.ResolveSessions(events);

        // Assert
        Assert.Single(sessions);
        Assert.Equal(BattlegroundType.Thidranki, sessions[0].BattlegroundType);
        Assert.Equal("Thidranki", sessions[0].ZoneName);
    }

    [Fact]
    public void ResolveSessions_WithMultipleBgEntries_CreatesMultipleSessions()
    {
        // Arrange
        var events = new List<LogEvent>
        {
            new ZoneEntryEvent(new TimeOnly(12, 0, 0), "Thidranki"),
            new DamageEvent(new TimeOnly(12, 1, 0), "You", "Enemy", 100, "Crush"),
            new ZoneEntryEvent(new TimeOnly(13, 0, 0), "Molvik"),
            new DamageEvent(new TimeOnly(13, 1, 0), "You", "Enemy2", 200, "Slash")
        };

        // Act
        var sessions = _service.ResolveSessions(events);

        // Assert
        Assert.Equal(2, sessions.Count);
        Assert.Equal(BattlegroundType.Thidranki, sessions[0].BattlegroundType);
        Assert.Equal(BattlegroundType.Molvik, sessions[1].BattlegroundType);
    }

    [Fact]
    public void ResolveSessions_WithExitToNonBg_EndsSession()
    {
        // Arrange
        var events = new List<LogEvent>
        {
            new ZoneEntryEvent(new TimeOnly(12, 0, 0), "Thidranki"),
            new DamageEvent(new TimeOnly(12, 1, 0), "You", "Enemy", 100, "Crush"),
            new ZoneEntryEvent(new TimeOnly(12, 30, 0), "Camelot")
        };

        // Act
        var sessions = _service.ResolveSessions(events);

        // Assert
        Assert.Single(sessions);
        Assert.Equal(BattlegroundType.Thidranki, sessions[0].BattlegroundType);
    }

    [Fact]
    public void ResolveSessions_CalculatesStatistics()
    {
        // Arrange
        var events = new List<LogEvent>
        {
            new ZoneEntryEvent(new TimeOnly(12, 0, 0), "Thidranki"),
            new DamageEvent(new TimeOnly(12, 1, 0), "You", "Enemy", 100, "Crush"),
            new DamageEvent(new TimeOnly(12, 1, 30), "You", "Enemy", 150, "Crush"),
            new DeathEvent(new TimeOnly(12, 2, 0), "Enemy", "You"),
            new DeathEvent(new TimeOnly(12, 3, 0), "You", "Enemy2")
        };

        // Act
        var sessions = _service.ResolveSessions(events);

        // Assert
        Assert.Single(sessions);
        Assert.Equal(1, sessions[0].Statistics.Kills);
        Assert.Equal(1, sessions[0].Statistics.Deaths);
        Assert.Equal(250, sessions[0].Statistics.DamageDealt);
    }

    [Fact]
    public void CalculateAllStatistics_WithMultipleSessions_AggregatesCorrectly()
    {
        // Arrange
        var sessions = new List<BattlegroundSession>
        {
            new BattlegroundSession(
                Guid.NewGuid(),
                BattlegroundType.Thidranki,
                "Thidranki",
                new TimeOnly(12, 0, 0),
                new TimeOnly(12, 30, 0),
                TimeSpan.FromMinutes(30),
                new List<LogEvent>().AsReadOnly(),
                new BattlegroundStatistics(5, 2, 0, 0, 1000, 500, 200, 100, 500, 2.5)
            ),
            new BattlegroundSession(
                Guid.NewGuid(),
                BattlegroundType.Molvik,
                "Molvik",
                new TimeOnly(14, 0, 0),
                new TimeOnly(14, 45, 0),
                TimeSpan.FromMinutes(45),
                new List<LogEvent>().AsReadOnly(),
                new BattlegroundStatistics(3, 3, 0, 0, 800, 600, 150, 80, 300, 1.0)
            )
        };

        // Act
        var stats = _service.CalculateAllStatistics(sessions);

        // Assert
        Assert.Equal(2, stats.TotalSessions);
        Assert.Equal(8, stats.OverallStatistics.Kills);
        Assert.Equal(5, stats.OverallStatistics.Deaths);
        Assert.Equal(1800, stats.OverallStatistics.DamageDealt);
        Assert.True(stats.StatsByType.ContainsKey(BattlegroundType.Thidranki));
        Assert.True(stats.StatsByType.ContainsKey(BattlegroundType.Molvik));
    }

    [Fact]
    public void CalculateAllStatistics_WithEmptySessions_ReturnsEmptyStats()
    {
        // Arrange
        var sessions = new List<BattlegroundSession>();

        // Act
        var stats = _service.CalculateAllStatistics(sessions);

        // Assert
        Assert.Equal(0, stats.TotalSessions);
        Assert.Equal(0, stats.OverallStatistics.Kills);
        Assert.Null(stats.BestPerformingBattleground);
        Assert.Null(stats.MostPlayedBattleground);
    }

    [Fact]
    public void CalculateAllStatistics_FindsBestPerformingBattleground()
    {
        // Arrange
        var sessions = new List<BattlegroundSession>
        {
            new BattlegroundSession(
                Guid.NewGuid(),
                BattlegroundType.Thidranki,
                "Thidranki",
                new TimeOnly(12, 0, 0),
                new TimeOnly(12, 30, 0),
                TimeSpan.FromMinutes(30),
                new List<LogEvent>().AsReadOnly(),
                new BattlegroundStatistics(10, 2, 0, 0, 1000, 500, 200, 100, 1000, 5.0)
            ),
            new BattlegroundSession(
                Guid.NewGuid(),
                BattlegroundType.Molvik,
                "Molvik",
                new TimeOnly(14, 0, 0),
                new TimeOnly(14, 30, 0),
                TimeSpan.FromMinutes(30),
                new List<LogEvent>().AsReadOnly(),
                new BattlegroundStatistics(5, 5, 0, 0, 800, 600, 150, 80, 500, 1.0)
            )
        };

        // Act
        var stats = _service.CalculateAllStatistics(sessions);

        // Assert
        Assert.Equal(BattlegroundType.Thidranki, stats.BestPerformingBattleground);
    }

    [Fact]
    public void CalculateAllStatistics_FindsMostPlayedBattleground()
    {
        // Arrange
        var sessions = new List<BattlegroundSession>
        {
            new BattlegroundSession(
                Guid.NewGuid(),
                BattlegroundType.Thidranki,
                "Thidranki",
                new TimeOnly(12, 0, 0),
                new TimeOnly(12, 30, 0),
                TimeSpan.FromMinutes(30),
                new List<LogEvent>().AsReadOnly(),
                new BattlegroundStatistics(5, 2, 0, 0, 1000, 500, 200, 100, 500, 2.5)
            ),
            new BattlegroundSession(
                Guid.NewGuid(),
                BattlegroundType.Thidranki,
                "Thidranki",
                new TimeOnly(13, 0, 0),
                new TimeOnly(13, 30, 0),
                TimeSpan.FromMinutes(30),
                new List<LogEvent>().AsReadOnly(),
                new BattlegroundStatistics(5, 2, 0, 0, 1000, 500, 200, 100, 500, 2.5)
            ),
            new BattlegroundSession(
                Guid.NewGuid(),
                BattlegroundType.Molvik,
                "Molvik",
                new TimeOnly(14, 0, 0),
                new TimeOnly(14, 30, 0),
                TimeSpan.FromMinutes(30),
                new List<LogEvent>().AsReadOnly(),
                new BattlegroundStatistics(5, 2, 0, 0, 1000, 500, 200, 100, 500, 2.5)
            )
        };

        // Act
        var stats = _service.CalculateAllStatistics(sessions);

        // Assert
        Assert.Equal(BattlegroundType.Thidranki, stats.MostPlayedBattleground);
        Assert.Equal(2, stats.SessionCountByType[BattlegroundType.Thidranki]);
        Assert.Equal(1, stats.SessionCountByType[BattlegroundType.Molvik]);
    }

    [Fact]
    public void CalculateAllStatistics_CalculatesTotalTime()
    {
        // Arrange
        var sessions = new List<BattlegroundSession>
        {
            new BattlegroundSession(
                Guid.NewGuid(),
                BattlegroundType.Thidranki,
                "Thidranki",
                new TimeOnly(12, 0, 0),
                new TimeOnly(12, 30, 0),
                TimeSpan.FromMinutes(30),
                new List<LogEvent>().AsReadOnly(),
                BattlegroundStatistics.Empty
            ),
            new BattlegroundSession(
                Guid.NewGuid(),
                BattlegroundType.Molvik,
                "Molvik",
                new TimeOnly(14, 0, 0),
                new TimeOnly(14, 45, 0),
                TimeSpan.FromMinutes(45),
                new List<LogEvent>().AsReadOnly(),
                BattlegroundStatistics.Empty
            )
        };

        // Act
        var stats = _service.CalculateAllStatistics(sessions);

        // Assert
        Assert.Equal(TimeSpan.FromMinutes(75), stats.TotalTimeInBattlegrounds);
    }

    [Fact]
    public void CalculateSessionStatistics_ReturnsSessionStats()
    {
        // Arrange
        var expectedStats = new BattlegroundStatistics(5, 2, 0, 0, 1000, 500, 200, 100, 500, 2.5);
        var session = new BattlegroundSession(
            Guid.NewGuid(),
            BattlegroundType.Thidranki,
            "Thidranki",
            new TimeOnly(12, 0, 0),
            new TimeOnly(12, 30, 0),
            TimeSpan.FromMinutes(30),
            new List<LogEvent>().AsReadOnly(),
            expectedStats
        );

        // Act
        var stats = _service.CalculateSessionStatistics(session);

        // Assert
        Assert.Equal(expectedStats, stats);
    }

    [Fact]
    public void ResolveSessions_WithNoZoneEntries_ReturnsEmpty()
    {
        // Arrange
        var events = new List<LogEvent>
        {
            new DamageEvent(new TimeOnly(12, 0, 0), "You", "Enemy", 100, "Crush"),
            new HealingEvent(new TimeOnly(12, 0, 20), "Healer", "You", 200)
        };

        // Act
        var sessions = _service.ResolveSessions(events);

        // Assert
        Assert.Empty(sessions);
    }

    [Fact]
    public void ResolveSessions_WithOnlyNonBgZones_ReturnsEmpty()
    {
        // Arrange
        var events = new List<LogEvent>
        {
            new ZoneEntryEvent(new TimeOnly(12, 0, 0), "Camelot"),
            new ZoneEntryEvent(new TimeOnly(13, 0, 0), "Jordheim")
        };

        // Act
        var sessions = _service.ResolveSessions(events);

        // Assert
        Assert.Empty(sessions);
    }
}
