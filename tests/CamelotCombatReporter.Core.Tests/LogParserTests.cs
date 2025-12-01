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

    [Fact]
    public void Parse_ShouldCorrectlyParseHealingDoneEvent()
    {
        // Arrange
        var logContent = "[16:45:30] You heal Merlin for 250 hit points!";
        var logFilePath = Path.GetTempFileName();
        File.WriteAllText(logFilePath, logContent);

        var parser = new LogParser(logFilePath);

        // Act
        var events = parser.Parse().ToList();

        // Assert
        Assert.Single(events);
        var healingEvent = Assert.IsType<HealingEvent>(events.First());

        Assert.Equal(new TimeOnly(16, 45, 30), healingEvent.Timestamp);
        Assert.Equal("You", healingEvent.Source);
        Assert.Equal("Merlin", healingEvent.Target);
        Assert.Equal(250, healingEvent.HealingAmount);

        // Clean up
        File.Delete(logFilePath);
    }

    [Fact]
    public void Parse_ShouldCorrectlyParseHealingReceivedEvent()
    {
        // Arrange
        var logContent = "[17:00:15] A cleric heals you for 175 hit points.";
        var logFilePath = Path.GetTempFileName();
        File.WriteAllText(logFilePath, logContent);

        var parser = new LogParser(logFilePath);

        // Act
        var events = parser.Parse().ToList();

        // Assert
        Assert.Single(events);
        var healingEvent = Assert.IsType<HealingEvent>(events.First());

        Assert.Equal(new TimeOnly(17, 0, 15), healingEvent.Timestamp);
        Assert.Equal("A cleric", healingEvent.Source);
        Assert.Equal("You", healingEvent.Target);
        Assert.Equal(175, healingEvent.HealingAmount);

        // Clean up
        File.Delete(logFilePath);
    }

    [Fact]
    public void Parse_ShouldHandleMixedCombatAndHealingEvents()
    {
        // Arrange
        var logContent =
            "[15:30:10] You hit the goblin for 100 points of crush damage!\n" +
            "[15:30:12] The goblin hits you for 50 points of slash damage!\n" +
            "[15:30:14] You heal yourself for 75 hit points.\n" +
            "[15:30:16] A healer heals you for 120 hit points!\n" +
            "[15:30:18] You use Backstab on the goblin!\n" +
            "[15:30:20] You cast Fireball on the goblin.";

        var logFilePath = Path.GetTempFileName();
        File.WriteAllText(logFilePath, logContent);

        var parser = new LogParser(logFilePath);

        // Act
        var events = parser.Parse().ToList();

        // Assert
        Assert.Equal(6, events.Count);

        // Event 1: Damage Dealt
        var event1 = Assert.IsType<DamageEvent>(events[0]);
        Assert.Equal("You", event1.Source);
        Assert.Equal(100, event1.DamageAmount);

        // Event 2: Damage Taken
        var event2 = Assert.IsType<DamageEvent>(events[1]);
        Assert.Equal("You", event2.Target);
        Assert.Equal(50, event2.DamageAmount);

        // Event 3: Healing Done
        var event3 = Assert.IsType<HealingEvent>(events[2]);
        Assert.Equal("You", event3.Source);
        Assert.Equal("yourself", event3.Target);
        Assert.Equal(75, event3.HealingAmount);

        // Event 4: Healing Received
        var event4 = Assert.IsType<HealingEvent>(events[3]);
        Assert.Equal("A healer", event4.Source);
        Assert.Equal("You", event4.Target);
        Assert.Equal(120, event4.HealingAmount);

        // Event 5: Combat Style
        var event5 = Assert.IsType<CombatStyleEvent>(events[4]);
        Assert.Equal("Backstab", event5.StyleName);

        // Event 6: Spell Cast
        var event6 = Assert.IsType<SpellCastEvent>(events[5]);
        Assert.Equal("Fireball", event6.SpellName);

        // Clean up
        File.Delete(logFilePath);
    }

    [Fact]
    public void Parse_ShouldCorrectlyParseCombatStyleEvent()
    {
        // Arrange
        var logContent = "[10:15:30] You use Evade on the training dummy!";
        var logFilePath = Path.GetTempFileName();
        File.WriteAllText(logFilePath, logContent);

        var parser = new LogParser(logFilePath);

        // Act
        var events = parser.Parse().ToList();

        // Assert
        Assert.Single(events);
        var styleEvent = Assert.IsType<CombatStyleEvent>(events.First());

        Assert.Equal(new TimeOnly(10, 15, 30), styleEvent.Timestamp);
        Assert.Equal("You", styleEvent.Source);
        Assert.Equal("training dummy", styleEvent.Target);
        Assert.Equal("Evade", styleEvent.StyleName);

        // Clean up
        File.Delete(logFilePath);
    }

    [Fact]
    public void Parse_ShouldCorrectlyParseSpellCastEvent()
    {
        // Arrange
        var logContent = "[11:20:45] You cast Greater Heal on Merlin.";
        var logFilePath = Path.GetTempFileName();
        File.WriteAllText(logFilePath, logContent);

        var parser = new LogParser(logFilePath);

        // Act
        var events = parser.Parse().ToList();

        // Assert
        Assert.Single(events);
        var spellEvent = Assert.IsType<SpellCastEvent>(events.First());

        Assert.Equal(new TimeOnly(11, 20, 45), spellEvent.Timestamp);
        Assert.Equal("You", spellEvent.Source);
        Assert.Equal("Merlin", spellEvent.Target);
        Assert.Equal("Greater Heal", spellEvent.SpellName);

        // Clean up
        File.Delete(logFilePath);
    }
}
