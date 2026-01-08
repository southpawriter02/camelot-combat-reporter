using EnemyEncounterDatabase.Models;
using EnemyEncounterDatabase.Services;
using Xunit;

namespace EnemyEncounterDatabase.Tests;

/// <summary>
/// Tests for the EnemyBrowserViewModel.
/// </summary>
public class EnemyBrowserViewModelTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly JsonEnemyDatabase _database;

    public EnemyBrowserViewModelTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), $"ViewModelTest_{Guid.NewGuid():N}");
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
    public async Task LoadAsync_PopulatesEnemyList()
    {
        // Arrange
        await _database.GetOrCreateAsync("Wolf", EnemyType.Mob);
        await _database.GetOrCreateAsync("Goblin", EnemyType.Mob);
        var viewModel = new ViewModels.EnemyBrowserViewModel(_database);

        // Act
        await viewModel.LoadAsync();

        // Assert
        Assert.Equal(2, viewModel.Enemies.Count);
    }

    [Fact]
    public async Task SearchText_FiltersEnemies()
    {
        // Arrange
        await _database.GetOrCreateAsync("Forest Wolf", EnemyType.Mob);
        await _database.GetOrCreateAsync("Dire Wolf", EnemyType.Mob);
        await _database.GetOrCreateAsync("Goblin", EnemyType.Mob);
        var viewModel = new ViewModels.EnemyBrowserViewModel(_database);
        await viewModel.LoadAsync();

        // Act - Set search text and reload
        viewModel.SearchText = "Wolf";
        // Wait a moment for auto-load to trigger
        await Task.Delay(100);
        await viewModel.LoadAsync();

        // Assert
        Assert.Equal(2, viewModel.Enemies.Count);
        Assert.All(viewModel.Enemies, e => Assert.Contains("Wolf", e.Name));
    }

    [Fact]
    public async Task TypeFilter_FiltersByType()
    {
        // Arrange
        await _database.GetOrCreateAsync("Wolf", EnemyType.Mob);
        await _database.GetOrCreateAsync("Nemesis", EnemyType.Player);
        var viewModel = new ViewModels.EnemyBrowserViewModel(_database);
        await viewModel.LoadAsync();

        // Act
        viewModel.TypeFilter = EnemyType.Mob;
        await viewModel.LoadAsync();

        // Assert
        Assert.Single(viewModel.Enemies);
        Assert.Equal("Wolf", viewModel.Enemies[0].Name);
    }

    [Fact]
    public async Task ToggleFavoriteCommand_UpdatesFavoriteStatus()
    {
        // Arrange
        var enemy = await _database.GetOrCreateAsync("Boss", EnemyType.Mob);
        var viewModel = new ViewModels.EnemyBrowserViewModel(_database);
        await viewModel.LoadAsync();
        viewModel.SelectedEnemy = viewModel.Enemies.First();

        // Act - Execute the toggle favorite command
        viewModel.ToggleFavoriteCommand.Execute(null);
        await Task.Delay(100); // Wait for async operation

        // Assert
        var updated = await _database.GetEnemyAsync(enemy.Id);
        Assert.True(updated?.IsFavorite);
    }

    [Fact]
    public async Task SaveNotesCommand_UpdatesEnemyNotes()
    {
        // Arrange
        var enemy = await _database.GetOrCreateAsync("Boss", EnemyType.Mob);
        var viewModel = new ViewModels.EnemyBrowserViewModel(_database);
        await viewModel.LoadAsync();
        viewModel.SelectedEnemy = viewModel.Enemies.First();

        // Act
        viewModel.EditingNotes = "Watch out for fire attacks!";
        viewModel.SaveNotesCommand.Execute(null);
        await Task.Delay(100); // Wait for async operation

        // Assert
        var updated = await _database.GetEnemyAsync(enemy.Id);
        Assert.Equal("Watch out for fire attacks!", updated?.Notes);
    }

    [Fact]
    public async Task SelectedEnemy_PopulatesEditingNotes()
    {
        // Arrange
        var enemy = await _database.GetOrCreateAsync("Boss", EnemyType.Mob);
        await _database.UpdateNotesAsync(enemy.Id, "Original notes");
        var viewModel = new ViewModels.EnemyBrowserViewModel(_database);
        await viewModel.LoadAsync();

        // Act
        viewModel.SelectedEnemy = viewModel.Enemies.First();

        // Assert
        Assert.Equal("Original notes", viewModel.EditingNotes);
    }

    [Fact]
    public async Task HasUnsavedNotes_DetectsChanges()
    {
        // Arrange
        var enemy = await _database.GetOrCreateAsync("Boss", EnemyType.Mob);
        await _database.UpdateNotesAsync(enemy.Id, "Original");
        var viewModel = new ViewModels.EnemyBrowserViewModel(_database);
        await viewModel.LoadAsync();
        viewModel.SelectedEnemy = viewModel.Enemies.First();

        // Initially no unsaved changes
        Assert.False(viewModel.HasUnsavedNotes);

        // Act - Modify notes
        viewModel.EditingNotes = "Modified";

        // Assert
        Assert.True(viewModel.HasUnsavedNotes);
    }

    [Fact]
    public async Task FavoritesOnly_OnlyShowsFavorites()
    {
        // Arrange
        var enemy = await _database.GetOrCreateAsync("FavEnemy", EnemyType.Mob);
        await _database.GetOrCreateAsync("RegularEnemy", EnemyType.Mob);
        await _database.SetFavoriteAsync(enemy.Id, true);

        var viewModel = new ViewModels.EnemyBrowserViewModel(_database);

        // Act
        viewModel.FavoritesOnly = true;
        await viewModel.LoadAsync();

        // Assert
        Assert.Single(viewModel.Enemies);
        Assert.Equal("FavEnemy", viewModel.Enemies[0].Name);
    }

    [Fact]
    public async Task SortBy_ChangesResultOrder()
    {
        // Arrange
        var wolf = await _database.GetOrCreateAsync("Wolf", EnemyType.Mob);
        var goblin = await _database.GetOrCreateAsync("Goblin", EnemyType.Mob);

        // Add more encounters to goblin
        await _database.AddEncounterAsync(goblin.Id, CreateEncounter());
        await _database.AddEncounterAsync(goblin.Id, CreateEncounter());

        var viewModel = new ViewModels.EnemyBrowserViewModel(_database);

        // Act
        viewModel.SortBy = EnemySortBy.EncounterCount;
        viewModel.SortDescending = true;
        await viewModel.LoadAsync();

        // Assert - Goblin should be first (has 2 encounters vs 0)
        Assert.Equal("Goblin", viewModel.Enemies.First().Name);
    }

    [Fact]
    public async Task TopAbilities_ReturnsTopFive()
    {
        // Arrange
        var enemy = await _database.GetOrCreateAsync("Boss", EnemyType.Mob);
        var abilities = new Dictionary<string, long>
        {
            ["Slash"] = 1000,
            ["Thrust"] = 800,
            ["Crush"] = 600,
            ["Pierce"] = 400,
            ["Fire"] = 200,
            ["Ice"] = 100
        };
        await _database.AddEncounterAsync(enemy.Id, CreateEncounter(), abilities);

        var viewModel = new ViewModels.EnemyBrowserViewModel(_database);
        await viewModel.LoadAsync();

        // Act
        viewModel.SelectedEnemy = viewModel.Enemies.First();

        // Assert - Should only show top 5
        Assert.Equal(5, viewModel.TopAbilities.Count);
        Assert.Equal("Slash", viewModel.TopAbilities[0].Name);
    }

    private static EncounterSummary CreateEncounter() => new(
        Timestamp: DateTime.UtcNow,
        Duration: TimeSpan.FromSeconds(10),
        DamageDealt: 100,
        DamageTaken: 50,
        Outcome: EncounterOutcome.Victory);
}
