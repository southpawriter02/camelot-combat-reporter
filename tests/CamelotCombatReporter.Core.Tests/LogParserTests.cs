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
    public void Parse_ShouldNotParseUnrelatedLines()
    {
        // Arrange
        var logContent =
            "[12:00:01] The training dummy hits you for 10 points of damage.\n" +
            "[12:00:02] You cast a healing spell on yourself.\n" +
            "[12:00:03] You have been stunned for 5 seconds.";
        var logFilePath = Path.GetTempFileName();
        File.WriteAllText(logFilePath, logContent);

        var parser = new LogParser(logFilePath);

        // Act
        var events = parser.Parse().ToList();

        // Assert
        Assert.Empty(events);

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
