using CamelotCombatReporter.Core.CharacterBuilding.Models;
using CamelotCombatReporter.Core.CharacterBuilding.Services;

namespace CamelotCombatReporter.Core.Tests.CharacterBuilding;

public class BuildComparisonServiceTests
{
    private readonly BuildComparisonService _service = new();

    [Fact]
    public void CompareBuilds_IdenticalBuilds_ReturnsNoDeltas()
    {
        // Arrange
        var build = CreateBuild(
            specs: [("Sword", 50), ("Shield", 42)],
            ras: [("Purge", 1)]);

        // Act
        var result = _service.CompareBuilds(build, build);

        // Assert
        Assert.True(result.AreIdentical);
        Assert.All(result.SpecDeltas, s => Assert.Equal(0, s.Delta));
        Assert.Empty(result.RealmAbilityDeltas);
    }

    [Fact]
    public void CompareBuilds_DifferentSpecs_CalculatesDeltas()
    {
        // Arrange
        var buildA = CreateBuild(specs: [("Sword", 50), ("Shield", 30)]);
        var buildB = CreateBuild(specs: [("Sword", 45), ("Shield", 42)]);

        // Act
        var result = _service.CompareBuilds(buildA, buildB);

        // Assert
        Assert.False(result.AreIdentical);
        
        var swordDelta = result.SpecDeltas.First(s => s.SpecName == "Sword");
        Assert.Equal(-5, swordDelta.Delta);
        
        var shieldDelta = result.SpecDeltas.First(s => s.SpecName == "Shield");
        Assert.Equal(12, shieldDelta.Delta);
    }

    [Fact]
    public void CompareBuilds_AddedRealmAbility_DetectsAddition()
    {
        // Arrange
        var buildA = CreateBuild(ras: [("Purge", 1)]);
        var buildB = CreateBuild(ras: [("Purge", 1), ("Determination", 3)]);

        // Act
        var result = _service.CompareBuilds(buildA, buildB);

        // Assert
        var addedRA = result.RealmAbilityDeltas.First(ra => ra.AbilityName == "Determination");
        Assert.Equal(RealmAbilityChangeType.Added, addedRA.ChangeType);
        Assert.Equal(0, addedRA.RankA);
        Assert.Equal(3, addedRA.RankB);
    }

    [Fact]
    public void CompareBuilds_RemovedRealmAbility_DetectsRemoval()
    {
        // Arrange
        var buildA = CreateBuild(ras: [("Purge", 1), ("Determination", 3)]);
        var buildB = CreateBuild(ras: [("Purge", 1)]);

        // Act
        var result = _service.CompareBuilds(buildA, buildB);

        // Assert
        var removedRA = result.RealmAbilityDeltas.First(ra => ra.AbilityName == "Determination");
        Assert.Equal(RealmAbilityChangeType.Removed, removedRA.ChangeType);
    }

    [Fact]
    public void CompareBuilds_RankChangedRealmAbility_DetectsChange()
    {
        // Arrange
        var buildA = CreateBuild(ras: [("Purge", 1)]);
        var buildB = CreateBuild(ras: [("Purge", 3)]);

        // Act
        var result = _service.CompareBuilds(buildA, buildB);

        // Assert
        var changedRA = result.RealmAbilityDeltas.First(ra => ra.AbilityName == "Purge");
        Assert.Equal(RealmAbilityChangeType.RankChanged, changedRA.ChangeType);
        Assert.Equal(1, changedRA.RankA);
        Assert.Equal(3, changedRA.RankB);
    }

    [Fact]
    public void CompareBuilds_WithPerformanceMetrics_CalculatesPerformanceDeltas()
    {
        // Arrange
        var metricsA = new BuildPerformanceMetrics { AverageDps = 100, Kills = 10, Deaths = 2 };
        var metricsB = new BuildPerformanceMetrics { AverageDps = 150, Kills = 15, Deaths = 3 };
        
        var buildA = CreateBuild() with { PerformanceMetrics = metricsA };
        var buildB = CreateBuild() with { PerformanceMetrics = metricsB };

        // Act
        var result = _service.CompareBuilds(buildA, buildB);

        // Assert
        Assert.NotNull(result.PerformanceDeltas);
        Assert.Equal(50, result.PerformanceDeltas.DpsDelta);
        Assert.Equal(5, result.PerformanceDeltas.KillsDelta);
    }

    [Fact]
    public void CompareBuilds_NewSpecInBuildB_ShowsAsIncrease()
    {
        // Arrange
        var buildA = CreateBuild(specs: [("Sword", 50)]);
        var buildB = CreateBuild(specs: [("Sword", 50), ("Parry", 20)]);

        // Act
        var result = _service.CompareBuilds(buildA, buildB);

        // Assert
        var parryDelta = result.SpecDeltas.First(s => s.SpecName == "Parry");
        Assert.Equal(0, parryDelta.ValueA);
        Assert.Equal(20, parryDelta.ValueB);
        Assert.Equal(20, parryDelta.Delta);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Additional Tests (v1.8.2)
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void CompareBuilds_SpecRemovedInBuildB_ShowsAsDecrease()
    {
        // Arrange
        var buildA = CreateBuild(specs: [("Sword", 50), ("Parry", 20)]);
        var buildB = CreateBuild(specs: [("Sword", 50)]);

        // Act
        var result = _service.CompareBuilds(buildA, buildB);

        // Assert
        var parryDelta = result.SpecDeltas.First(s => s.SpecName == "Parry");
        Assert.Equal(20, parryDelta.ValueA);
        Assert.Equal(0, parryDelta.ValueB);
        Assert.Equal(-20, parryDelta.Delta);
        Assert.True(parryDelta.IsDecrease);
    }

    [Fact]
    public void CompareBuilds_TotalSpecPointsDelta_Calculates()
    {
        // Arrange
        var buildA = CreateBuild(specs: [("Sword", 50), ("Shield", 30)]);
        var buildB = CreateBuild(specs: [("Sword", 45), ("Shield", 40)]);

        // Act
        var result = _service.CompareBuilds(buildA, buildB);

        // Assert - Net change: -5 + 10 = 5
        Assert.Equal(5, result.TotalSpecPointsDelta);
    }

    [Fact]
    public void CompareBuilds_NoPerformanceMetrics_PerformanceDeltaIsNull()
    {
        // Arrange
        var buildA = CreateBuild();
        var buildB = CreateBuild();

        // Act
        var result = _service.CompareBuilds(buildA, buildB);

        // Assert
        Assert.Null(result.PerformanceDeltas);
    }

    [Fact]
    public void CompareBuilds_OnlyBuildAHasMetrics_PerformanceDeltaIsNull()
    {
        // Arrange
        var metricsA = new BuildPerformanceMetrics { AverageDps = 100 };
        var buildA = CreateBuild() with { PerformanceMetrics = metricsA };
        var buildB = CreateBuild();

        // Act
        var result = _service.CompareBuilds(buildA, buildB);

        // Assert
        Assert.Null(result.PerformanceDeltas);
    }

    [Fact]
    public void CompareBuilds_RAPointsDelta_Calculates()
    {
        // Arrange - Purge rank 1 = ~2 points, rank 3 = ~6 points (test uses simplified cost)
        var buildA = CreateBuild(ras: [("Purge", 1)]);
        var buildB = CreateBuild(ras: [("Purge", 3)]);

        // Act
        var result = _service.CompareBuilds(buildA, buildB);

        // Assert
        Assert.True(result.TotalRAPointsDelta > 0);
    }

    [Fact]
    public void PerformanceDelta_IsDpsImproved_True_WhenHigherDps()
    {
        // Arrange
        var delta = new PerformanceDelta { DpsDelta = 50 };

        // Assert
        Assert.True(delta.IsDpsImproved);
    }

    [Fact]
    public void PerformanceDelta_IsKdImproved_False_WhenLowerKd()
    {
        // Arrange
        var delta = new PerformanceDelta { KdRatioDelta = -0.5 };

        // Assert
        Assert.False(delta.IsKdImproved);
    }

    private static CharacterBuild CreateBuild(
        (string name, int value)[]? specs = null,
        (string name, int rank)[]? ras = null)
    {
        var specDict = specs?.ToDictionary(s => s.name, s => s.value)
            ?? new Dictionary<string, int>();
        
        var raList = ras?.Select(ra => new RealmAbilitySelection
        {
            AbilityName = ra.name,
            Rank = ra.rank,
            PointCost = ra.rank * 2  // Simplified cost for testing
        }).ToList() ?? [];

        return new CharacterBuild
        {
            Id = Guid.NewGuid(),
            Name = "Test Build",
            CreatedUtc = DateTime.UtcNow,
            SpecLines = specDict,
            RealmAbilities = raList
        };
    }
}

