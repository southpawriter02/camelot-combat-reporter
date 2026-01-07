using CamelotCombatReporter.Core.CharacterBuilding.Models;
using CamelotCombatReporter.Core.CharacterBuilding.Templates;

namespace CamelotCombatReporter.Core.Tests.CharacterBuilding;

public class RealmAbilityCatalogTests
{
    // ─────────────────────────────────────────────────────────────────────────
    // Catalog Retrieval Tests
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void GetAllAbilities_ReturnsNonEmptyList()
    {
        // Act
        var abilities = RealmAbilityCatalog.GetAllAbilities();

        // Assert
        Assert.NotEmpty(abilities);
        Assert.True(abilities.Count > 30); // At least 30 abilities defined
    }

    [Fact]
    public void GetActiveAbilities_ReturnsOnlyActive()
    {
        // Act
        var active = RealmAbilityCatalog.GetActiveAbilities();

        // Assert
        Assert.NotEmpty(active);
        Assert.All(active, a => Assert.Equal(RealmAbilityCategory.Active, a.Category));
    }

    [Fact]
    public void GetPassiveAbilities_ReturnsOnlyPassive()
    {
        // Act
        var passive = RealmAbilityCatalog.GetPassiveAbilities();

        // Assert
        Assert.NotEmpty(passive);
        Assert.All(passive, a => Assert.Equal(RealmAbilityCategory.Passive, a.Category));
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Point Cost Tests
    // ─────────────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("Purge", 1, 6)]
    [InlineData("Purge", 2, 10)]
    [InlineData("Purge", 3, 14)]
    [InlineData("Determination", 5, 14)]
    public void GetPointCost_ReturnsCorrectCost(string abilityName, int rank, int expectedCost)
    {
        // Act
        var cost = RealmAbilityCatalog.GetPointCost(abilityName, rank);

        // Assert
        Assert.Equal(expectedCost, cost);
    }

    [Fact]
    public void GetPointCost_InvalidAbility_ReturnsZero()
    {
        // Act
        var cost = RealmAbilityCatalog.GetPointCost("NonExistentAbility", 1);

        // Assert
        Assert.Equal(0, cost);
    }

    [Fact]
    public void GetPointCost_InvalidRank_ReturnsZero()
    {
        // Act
        var costZero = RealmAbilityCatalog.GetPointCost("Purge", 0);
        var costOver = RealmAbilityCatalog.GetPointCost("Purge", 10);

        // Assert
        Assert.Equal(0, costZero);
        Assert.Equal(0, costOver);
    }

    [Fact]
    public void GetTotalPointsSpent_CalculatesCorrectly()
    {
        // Arrange
        var selections = new[]
        {
            new RealmAbilitySelection { AbilityName = "Purge", Rank = 3, PointCost = 14 },
            new RealmAbilitySelection { AbilityName = "Determination", Rank = 5, PointCost = 14 }
        };

        // Act
        var total = RealmAbilityCatalog.GetTotalPointsSpent(selections);

        // Assert
        Assert.Equal(28, total); // 14 + 14
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Max RA Points Tests
    // ─────────────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(1, 0, 0)]
    [InlineData(2, 0, 2)]
    [InlineData(5, 0, 14)]
    [InlineData(8, 0, 35)]
    [InlineData(10, 0, 54)]
    public void GetMaxRealmAbilityPoints_ReturnsCorrectAmoutn(int rr, int rl, int expectedPoints)
    {
        // Act
        var points = RealmAbilityCatalog.GetMaxRealmAbilityPoints(rr, rl);

        // Assert
        Assert.Equal(expectedPoints, points);
    }

    [Fact]
    public void RealmAbilityDefinition_TotalCost_SumsCorrectly()
    {
        // Arrange
        var purge = RealmAbilityCatalog.GetAllAbilities()
            .First(a => a.Name == "Purge");

        // Act
        var totalCost = purge.TotalCost;

        // Assert
        Assert.Equal(30, totalCost); // 6 + 10 + 14 = 30
    }
}
