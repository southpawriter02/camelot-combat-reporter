using CamelotCombatReporter.Core.Models;
using EnemyEncounterDatabase.Analysis;
using EnemyEncounterDatabase.Models;
using Xunit;

namespace EnemyEncounterDatabase.Tests;

/// <summary>
/// Tests for the EncounterAnalyzer class.
/// </summary>
public class EncounterAnalyzerTests
{
    [Fact]
    public void DetectEncounters_WithDamageEvents_DetectsEnemy()
    {
        // Arrange
        var events = new List<LogEvent>
        {
            new DamageEvent(
                Timestamp: new TimeOnly(12, 0, 0),
                Source: "Testplayer",
                Target: "forest wolf",
                DamageAmount: 100,
                DamageType: "Slash"),
            new DamageEvent(
                Timestamp: new TimeOnly(12, 0, 5),
                Source: "Testplayer",
                Target: "forest wolf",
                DamageAmount: 150,
                DamageType: "Slash"),
            new DamageEvent(
                Timestamp: new TimeOnly(12, 0, 3),
                Source: "forest wolf",
                Target: "Testplayer",
                DamageAmount: 50,
                DamageType: "Crush")
        };

        // Act
        var encounters = EncounterAnalyzer.DetectEncounters(events, "Testplayer");

        // Assert
        Assert.Single(encounters);
        var encounter = encounters[0];
        Assert.Equal("forest wolf", encounter.EnemyName);
        Assert.Equal(EnemyType.Mob, encounter.Type);
        Assert.Equal(250, encounter.DamageDealt);
        Assert.Equal(50, encounter.DamageTaken);
    }

    [Fact]
    public void DetectEncounters_WithMultipleEnemies_DetectsAll()
    {
        // Arrange
        var events = new List<LogEvent>
        {
            new DamageEvent(new TimeOnly(12, 0, 0), "Player", "Goblin", 100, "Slash"),
            new DamageEvent(new TimeOnly(12, 0, 1), "Player", "Orc", 200, "Thrust"),
            new DamageEvent(new TimeOnly(12, 0, 2), "Goblin", "Player", 30, "Crush"),
            new DamageEvent(new TimeOnly(12, 0, 3), "Player", "Goblin", 100, "Slash")
        };

        // Act
        var encounters = EncounterAnalyzer.DetectEncounters(events, "Player");

        // Assert
        Assert.Equal(2, encounters.Count);
        Assert.Contains(encounters, e => e.EnemyName == "Goblin" && e.DamageDealt == 200);
        Assert.Contains(encounters, e => e.EnemyName == "Orc" && e.DamageDealt == 200);
    }

    [Fact]
    public void DetectEncounters_WithPlayerName_ClassifiesAsPlayer()
    {
        // Arrange - Player names are typically single capitalized words
        var events = new List<LogEvent>
        {
            new DamageEvent(new TimeOnly(12, 0, 0), "Testchar", "Nemesis", 100, "Slash")
        };

        // Act
        var encounters = EncounterAnalyzer.DetectEncounters(events, "Testchar");

        // Assert
        Assert.Single(encounters);
        Assert.Equal(EnemyType.Player, encounters[0].Type);
    }

    [Fact]
    public void DetectEncounters_WithMobName_ClassifiesAsMob()
    {
        // Arrange - Mobs often have lowercase or multi-word names
        var events = new List<LogEvent>
        {
            new DamageEvent(new TimeOnly(12, 0, 0), "Player", "a skeletal warrior", 100, "Slash")
        };

        // Act
        var encounters = EncounterAnalyzer.DetectEncounters(events, "Player");

        // Assert
        Assert.Single(encounters);
        Assert.Equal(EnemyType.Mob, encounters[0].Type);
    }

    [Fact]
    public void DetectEncounters_CalculatesDamageByAbility()
    {
        // Arrange
        var events = new List<LogEvent>
        {
            new DamageEvent(new TimeOnly(12, 0, 0), "Player", "Enemy", 100, "Slash"),
            new DamageEvent(new TimeOnly(12, 0, 1), "Player", "Enemy", 200, "Slash"),
            new DamageEvent(new TimeOnly(12, 0, 2), "Player", "Enemy", 150, "Thrust")
        };

        // Act
        var encounters = EncounterAnalyzer.DetectEncounters(events, "Player");

        // Assert
        Assert.Single(encounters);
        var encounter = encounters[0];
        Assert.Equal(300, encounter.DamageByAbility["Slash"]);
        Assert.Equal(150, encounter.DamageByAbility["Thrust"]);
    }

    [Fact]
    public void DetectEncounters_WithEmptyEvents_ReturnsEmpty()
    {
        // Arrange
        var events = new List<LogEvent>();

        // Act
        var encounters = EncounterAnalyzer.DetectEncounters(events, "Player");

        // Assert
        Assert.Empty(encounters);
    }

    [Fact]
    public void DetectEncounters_IgnoresSelfDamage()
    {
        // Arrange
        var events = new List<LogEvent>
        {
            new DamageEvent(new TimeOnly(12, 0, 0), "Player", "Player", 100, "Slash")
        };

        // Act
        var encounters = EncounterAnalyzer.DetectEncounters(events, "Player");

        // Assert
        Assert.Empty(encounters);
    }
}
