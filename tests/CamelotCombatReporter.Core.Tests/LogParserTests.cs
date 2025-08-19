using CamelotCombatReporter.Core.Models;
using CamelotCombatReporter.Core.Parsing;
using Xunit;

namespace CamelotCombatReporter.Core.Tests;

public class LogParserTests
{
    [Fact]
    public void Parse_ShouldCorrectlyParsePlayerDamageEvent()
    {
        // Arrange
        var logContent = "[15:30:10] You hit the training dummy for 123 points of crush damage!";
        var logFilePath = Path.GetTempFileName();
        File.WriteAllText(logFilePath, logContent);

        var parser = new LogParser(logFilePath);

        // Act
        var events = parser.Parse().ToList();

        // Assert
        Assert.Single(events);
        var damageEvent = Assert.IsType<DamageEvent>(events.First());

        Assert.Equal(new TimeOnly(15, 30, 10), damageEvent.Timestamp);
        Assert.Equal("You", damageEvent.Source);
        Assert.Equal("training dummy", damageEvent.Target);
        Assert.Equal(123, damageEvent.DamageAmount);
        Assert.Equal("crush", damageEvent.DamageType);

        // Clean up the temporary file
        File.Delete(logFilePath);
    }

    [Fact]
    public void Parse_ShouldReturnUnknownDamageType_WhenTypeIsMissing()
    {
        // Arrange
        var logContent = "[12:00:00] You hit the training dummy for 50 points of damage.";
        var logFilePath = Path.GetTempFileName();
        File.WriteAllText(logFilePath, logContent);

        var parser = new LogParser(logFilePath);

        // Act
        var events = parser.Parse().ToList();

        // Assert
        Assert.Single(events);
        var damageEvent = Assert.IsType<DamageEvent>(events.First());
        Assert.Equal("Unknown", damageEvent.DamageType);

        // Clean up
        File.Delete(logFilePath);
    }

    [Fact]
    public void Parse_ShouldCorrectlyParsePlayerDamageTakenEvent()
    {
        // Arrange
        var logContent = "[08:45:15] A goblin hits you for 78 points of slash damage!";
        var logFilePath = Path.GetTempFileName();
        File.WriteAllText(logFilePath, logContent);

        var parser = new LogParser(logFilePath);

        // Act
        var events = parser.Parse().ToList();

        // Assert
        Assert.Single(events);
        var damageEvent = Assert.IsType<DamageEvent>(events.First());

        Assert.Equal(new TimeOnly(8, 45, 15), damageEvent.Timestamp);
        Assert.Equal("A goblin", damageEvent.Source);
        Assert.Equal("You", damageEvent.Target);
        Assert.Equal(78, damageEvent.DamageAmount);
        Assert.Equal("slash", damageEvent.DamageType);

        // Clean up
        File.Delete(logFilePath);
    }

    [Fact]
    public void Parse_ShouldHandleMixedDamageEvents()
    {
        // Arrange
        var logContent =
            "[15:30:10] You hit the training dummy for 123 points of crush damage!\n" +
            "[15:30:11] The training dummy hits you for 10 points of damage.\n" +
            "[15:30:12] You hit a goblin for 55 points of heat damage.";

        var logFilePath = Path.GetTempFileName();
        File.WriteAllText(logFilePath, logContent);

        var parser = new LogParser(logFilePath);

        // Act
        var events = parser.Parse().ToList();

        // Assert
        Assert.Equal(3, events.Count);

        // Event 1: Damage Dealt
        var event1 = Assert.IsType<DamageEvent>(events[0]);
        Assert.Equal("You", event1.Source);
        Assert.Equal("training dummy", event1.Target);
        Assert.Equal(123, event1.DamageAmount);

        // Event 2: Damage Taken
        var event2 = Assert.IsType<DamageEvent>(events[1]);
        Assert.Equal("The training dummy", event2.Source);
        Assert.Equal("You", event2.Target);
        Assert.Equal(10, event2.DamageAmount);
        Assert.Equal("Unknown", event2.DamageType);

        // Event 3: Damage Dealt
        var event3 = Assert.IsType<DamageEvent>(events[2]);
        Assert.Equal("You", event3.Source);
        Assert.Equal("a goblin", event3.Target);
        Assert.Equal(55, event3.DamageAmount);


        // Clean up
        File.Delete(logFilePath);
    }

    [Fact]
    public void Parse_ShouldHandleEmptyFile()
    {
        // Arrange
        var logFilePath = Path.GetTempFileName();
        // The file is empty by default

        var parser = new LogParser(logFilePath);

        // Act
        var events = parser.Parse().ToList();

        // Assert
        Assert.Empty(events);

        // Clean up
        File.Delete(logFilePath);
    }
}
