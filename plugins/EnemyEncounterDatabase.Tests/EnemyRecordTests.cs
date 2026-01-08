using EnemyEncounterDatabase.Models;
using Xunit;

namespace EnemyEncounterDatabase.Tests;

/// <summary>
/// Tests for the EnemyRecord model.
/// </summary>
public class EnemyRecordTests
{
    [Fact]
    public void CreateNew_InitializesCorrectly()
    {
        // Act
        var enemy = EnemyRecord.CreateNew("Test Enemy", EnemyType.Mob);

        // Assert
        Assert.Equal("Test Enemy", enemy.Name);
        Assert.Equal(EnemyType.Mob, enemy.Type);
        Assert.Equal(0, enemy.EncounterCount);
        Assert.False(enemy.IsFavorite);
        Assert.Null(enemy.Notes);
        Assert.Empty(enemy.RecentEncounters);
        Assert.NotEmpty(enemy.Id);
    }

    [Fact]
    public void GenerateId_IsDeterministic()
    {
        // Act
        var id1 = EnemyRecord.GenerateId("Goblin", EnemyType.Mob);
        var id2 = EnemyRecord.GenerateId("Goblin", EnemyType.Mob);

        // Assert
        Assert.Equal(id1, id2);
    }

    [Fact]
    public void GenerateId_DiffersByType()
    {
        // Act
        var mobId = EnemyRecord.GenerateId("Target", EnemyType.Mob);
        var playerId = EnemyRecord.GenerateId("Target", EnemyType.Player);

        // Assert
        Assert.NotEqual(mobId, playerId);
    }

    [Fact]
    public void GenerateId_IsCaseInsensitive()
    {
        // Act
        var id1 = EnemyRecord.GenerateId("Goblin", EnemyType.Mob);
        var id2 = EnemyRecord.GenerateId("GOBLIN", EnemyType.Mob);
        var id3 = EnemyRecord.GenerateId("goblin", EnemyType.Mob);

        // Assert
        Assert.Equal(id1, id2);
        Assert.Equal(id2, id3);
    }

    [Fact]
    public void WinRate_CalculatesCorrectly()
    {
        // Arrange
        var stats = new EnemyStatistics(
            TotalDamageDealt: 1000,
            TotalDamageTaken: 500,
            TotalKills: 7,
            TotalDeaths: 3,
            AverageEncounterDuration: 30,
            AverageDps: 100,
            DamageByAbility: new Dictionary<string, long>(),
            DamageTakenByAbility: new Dictionary<string, long>());

        // Act
        var winRate = stats.WinRate;

        // Assert
        Assert.Equal(70, winRate); // 7/(7+3) = 70%
    }

    [Fact]
    public void WinRate_WithNoEncounters_ReturnsZero()
    {
        // Arrange
        var stats = EnemyStatistics.Empty;

        // Act
        var winRate = stats.WinRate;

        // Assert
        Assert.Equal(0, winRate);
    }
}
