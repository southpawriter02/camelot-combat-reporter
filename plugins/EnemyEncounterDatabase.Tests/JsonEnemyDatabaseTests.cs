using EnemyEncounterDatabase.Models;
using EnemyEncounterDatabase.Services;
using Xunit;

namespace EnemyEncounterDatabase.Tests;

/// <summary>
/// Tests for the JsonEnemyDatabase class.
/// </summary>
public class JsonEnemyDatabaseTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly JsonEnemyDatabase _database;

    public JsonEnemyDatabaseTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), $"EnemyDbTest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testDirectory);
        _database = new JsonEnemyDatabase(_testDirectory);
    }

    public void Dispose()
    {
        _database.Dispose();
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, recursive: true);
        }
    }

    [Fact]
    public async Task GetOrCreateAsync_CreatesNewEnemy()
    {
        // Act
        var enemy = await _database.GetOrCreateAsync("Test Enemy", EnemyType.Mob);

        // Assert
        Assert.NotNull(enemy);
        Assert.Equal("Test Enemy", enemy.Name);
        Assert.Equal(EnemyType.Mob, enemy.Type);
        Assert.Equal(0, enemy.EncounterCount);
    }

    [Fact]
    public async Task GetOrCreateAsync_ReturnsExistingEnemy()
    {
        // Arrange
        var first = await _database.GetOrCreateAsync("Goblin", EnemyType.Mob);

        // Act
        var second = await _database.GetOrCreateAsync("Goblin", EnemyType.Mob);

        // Assert
        Assert.Equal(first.Id, second.Id);
    }

    [Fact]
    public async Task AddEncounterAsync_UpdatesStatistics()
    {
        // Arrange
        var enemy = await _database.GetOrCreateAsync("Wolf", EnemyType.Mob);
        var encounter = new EncounterSummary(
            Timestamp: DateTime.UtcNow,
            Duration: TimeSpan.FromSeconds(10),
            DamageDealt: 500,
            DamageTaken: 100,
            Outcome: EncounterOutcome.Victory);

        // Act
        await _database.AddEncounterAsync(
            enemy.Id,
            encounter,
            new Dictionary<string, long> { ["Slash"] = 500 });

        var updated = await _database.GetEnemyAsync(enemy.Id);

        // Assert
        Assert.NotNull(updated);
        Assert.Equal(1, updated.EncounterCount);
        Assert.Equal(500, updated.Statistics.TotalDamageDealt);
        Assert.Equal(100, updated.Statistics.TotalDamageTaken);
        Assert.Equal(1, updated.Statistics.TotalKills);
        Assert.Single(updated.RecentEncounters);
    }

    [Fact]
    public async Task SearchAsync_FiltersByName()
    {
        // Arrange
        await _database.GetOrCreateAsync("Forest Wolf", EnemyType.Mob);
        await _database.GetOrCreateAsync("Dire Wolf", EnemyType.Mob);
        await _database.GetOrCreateAsync("Goblin", EnemyType.Mob);

        // Act
        var results = await _database.SearchAsync(new EnemySearchCriteria(NameContains: "Wolf"));

        // Assert
        Assert.Equal(2, results.Count);
        Assert.All(results, e => Assert.Contains("Wolf", e.Name));
    }

    [Fact]
    public async Task SearchAsync_FiltersByType()
    {
        // Arrange
        await _database.GetOrCreateAsync("Wolf", EnemyType.Mob);
        await _database.GetOrCreateAsync("Badguy", EnemyType.Player);

        // Act
        var mobs = await _database.SearchAsync(new EnemySearchCriteria(Type: EnemyType.Mob));
        var players = await _database.SearchAsync(new EnemySearchCriteria(Type: EnemyType.Player));

        // Assert
        Assert.Single(mobs);
        Assert.Single(players);
        Assert.Equal("Wolf", mobs[0].Name);
        Assert.Equal("Badguy", players[0].Name);
    }

    [Fact]
    public async Task SearchAsync_SortsByEncounterCount()
    {
        // Arrange
        var wolf = await _database.GetOrCreateAsync("Wolf", EnemyType.Mob);
        var goblin = await _database.GetOrCreateAsync("Goblin", EnemyType.Mob);

        // Add 3 encounters to wolf, 1 to goblin
        for (int i = 0; i < 3; i++)
        {
            await _database.AddEncounterAsync(wolf.Id, CreateEncounter());
        }
        await _database.AddEncounterAsync(goblin.Id, CreateEncounter());

        // Act
        var results = await _database.SearchAsync(new EnemySearchCriteria(
            SortBy: EnemySortBy.EncounterCount,
            SortDescending: true));

        // Assert
        Assert.Equal(2, results.Count);
        Assert.Equal("Wolf", results[0].Name);
        Assert.Equal(3, results[0].EncounterCount);
    }

    [Fact]
    public async Task SetFavoriteAsync_UpdatesFavoriteStatus()
    {
        // Arrange
        var enemy = await _database.GetOrCreateAsync("Nemesis", EnemyType.Player);

        // Act
        await _database.SetFavoriteAsync(enemy.Id, true);
        var updated = await _database.GetEnemyAsync(enemy.Id);

        // Assert
        Assert.NotNull(updated);
        Assert.True(updated.IsFavorite);
    }

    [Fact]
    public async Task UpdateNotesAsync_UpdatesNotes()
    {
        // Arrange
        var enemy = await _database.GetOrCreateAsync("Boss", EnemyType.Mob);

        // Act
        await _database.UpdateNotesAsync(enemy.Id, "Watch out for AoE attacks!");
        var updated = await _database.GetEnemyAsync(enemy.Id);

        // Assert
        Assert.NotNull(updated);
        Assert.Equal("Watch out for AoE attacks!", updated.Notes);
    }

    [Fact]
    public async Task SaveChangesAsync_PersistsData()
    {
        // Arrange
        await _database.GetOrCreateAsync("PersistTest", EnemyType.Mob);
        await _database.SaveChangesAsync();

        // Act - Create new database instance to verify persistence
        using var newDatabase = new JsonEnemyDatabase(_testDirectory);
        var enemy = await newDatabase.GetEnemyAsync(
            EnemyRecord.GenerateId("PersistTest", EnemyType.Mob));

        // Assert
        Assert.NotNull(enemy);
        Assert.Equal("PersistTest", enemy.Name);
    }

    [Fact]
    public async Task GetTotalCountAsync_ReturnsCorrectCount()
    {
        // Arrange
        await _database.GetOrCreateAsync("Enemy1", EnemyType.Mob);
        await _database.GetOrCreateAsync("Enemy2", EnemyType.Player);
        await _database.GetOrCreateAsync("Enemy3", EnemyType.NPC);

        // Act
        var count = await _database.GetTotalCountAsync();

        // Assert
        Assert.Equal(3, count);
    }

    private static EncounterSummary CreateEncounter() => new(
        Timestamp: DateTime.UtcNow,
        Duration: TimeSpan.FromSeconds(10),
        DamageDealt: 100,
        DamageTaken: 50,
        Outcome: EncounterOutcome.Victory);
}
