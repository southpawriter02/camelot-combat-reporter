using CamelotCombatReporter.Core.CharacterBuilding.Models;
using CamelotCombatReporter.Core.CharacterBuilding.Services;
using CamelotCombatReporter.Core.CharacterBuilding.Templates;
using CamelotCombatReporter.Core.Models;

namespace CamelotCombatReporter.Core.Tests.CharacterBuilding;

public class SpecializationTemplateServiceTests
{
    private readonly SpecializationTemplateService _service;

    public SpecializationTemplateServiceTests()
    {
        _service = new SpecializationTemplateService();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Template Retrieval Tests
    // ─────────────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(CharacterClass.Armsman, 8)]  // Crush, Slash, Thrust, Polearm, Two-Handed, Shield, Parry, Crossbow
    [InlineData(CharacterClass.Wizard, 3)]   // Earth, Cold, Fire
    [InlineData(CharacterClass.Cleric, 3)]   // Rejuvenation, Enhancement, Smiting
    [InlineData(CharacterClass.Infiltrator, 6)] // Slash, Thrust, Dual Wield, Critical Strike, Stealth, Envenom
    public void GetTemplateForClass_ReturnsCorrectSpecLineCount(CharacterClass charClass, int expectedCount)
    {
        // Act
        var template = _service.GetTemplateForClass(charClass);

        // Assert
        Assert.Equal(charClass, template.Class);
        Assert.Equal(expectedCount, template.SpecLines.Count);
    }

    [Fact]
    public void GetTemplateForClass_Armsman_ContainsExpectedSpecs()
    {
        // Act
        var template = _service.GetTemplateForClass(CharacterClass.Armsman);
        var specNames = template.SpecLines.Select(s => s.Name).ToList();

        // Assert
        Assert.Contains("Polearm", specNames);
        Assert.Contains("Shield", specNames);
        Assert.Contains("Parry", specNames);
        Assert.Contains("Two-Handed", specNames);
    }

    [Fact]
    public void GetTemplateForClass_AllRealms_HaveTemplates()
    {
        // Arrange
        var allClasses = Enum.GetValues<CharacterClass>();

        // Act & Assert
        foreach (var charClass in allClasses)
        {
            var template = _service.GetTemplateForClass(charClass);
            Assert.NotNull(template);
            // Most classes should have spec lines (some edge cases may not)
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Spec Point Calculation Tests
    // ─────────────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(50, 126)]  // Standard level 50: (50*2) + 25 + 1 = 126
    [InlineData(40, 101)]  // (40*2) + 20 + 1 = 101
    [InlineData(1, 3)]     // (1*2) + 0 + 1 = 3
    public void GetMaxSpecPoints_ReturnsCorrectPoints(int level, int expectedPoints)
    {
        // Act
        var points = _service.GetMaxSpecPoints(level);

        // Assert
        Assert.Equal(expectedPoints, points);
    }

    [Theory]
    [InlineData(1, 1)]     // Level 1 costs 1 point
    [InlineData(10, 55)]   // Sum 1-10 = 55
    [InlineData(50, 1275)] // Sum 1-50 = 1275
    public void CalculateSpecPointCost_ReturnsTriangularNumber(int specLevel, int expectedCost)
    {
        // Act
        var cost = _service.CalculateSpecPointCost(specLevel);

        // Assert
        Assert.Equal(expectedCost, cost);
    }

    [Fact]
    public void CalculateSpecPointCost_WithMultiplier_AppliesCorrectly()
    {
        // Act
        var fullCost = _service.CalculateSpecPointCost(10, 1.0);
        var halfCost = _service.CalculateSpecPointCost(10, 0.5);

        // Assert
        Assert.Equal(55, fullCost);
        Assert.Equal(27, halfCost); // 55 * 0.5 = 27.5, truncated to 27
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Build Validation Tests
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void GetAllocatedSpecPoints_EmptyBuild_ReturnsZero()
    {
        // Arrange
        var build = new CharacterBuild { Name = "Empty" };

        // Act
        var allocated = _service.GetAllocatedSpecPoints(build, CharacterClass.Armsman);

        // Assert
        Assert.Equal(0, allocated);
    }

    [Fact]
    public void GetAllocatedSpecPoints_WithSpecs_CalculatesCorrectly()
    {
        // Arrange
        var build = new CharacterBuild
        {
            Name = "Test",
            SpecLines = new Dictionary<string, int>
            {
                { "Polearm", 10 },  // Cost: 55
                { "Shield", 5 }     // Cost: 15
            }
        };

        // Act
        var allocated = _service.GetAllocatedSpecPoints(build, CharacterClass.Armsman);

        // Assert
        Assert.Equal(70, allocated); // 55 + 15
    }

    [Fact]
    public void ValidateSpecAllocation_UnderLimit_ReturnsTrue()
    {
        // Arrange
        var build = new CharacterBuild
        {
            Name = "Valid",
            SpecLines = new Dictionary<string, int>
            {
                { "Polearm", 5 },   // Cost: 15
                { "Shield", 5 }     // Cost: 15
            }
        };

        // Act
        var isValid = _service.ValidateSpecAllocation(build, CharacterClass.Armsman, 50);

        // Assert
        Assert.True(isValid); // 30 < 126
    }

    [Fact]
    public void ValidateSpecAllocation_OverLimit_ReturnsFalse()
    {
        // Arrange - way over limit
        var build = new CharacterBuild
        {
            Name = "Invalid",
            SpecLines = new Dictionary<string, int>
            {
                { "Polearm", 50 },  // Cost: 1275
                { "Shield", 50 }    // Cost: 1275
            }
        };

        // Act
        var isValid = _service.ValidateSpecAllocation(build, CharacterClass.Armsman, 50);

        // Assert
        Assert.False(isValid); // 2550 > 126
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Realm Class Tests
    // ─────────────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(Realm.Albion)]
    [InlineData(Realm.Midgard)]
    [InlineData(Realm.Hibernia)]
    public void GetClassesForRealm_ReturnsNonEmptyList(Realm realm)
    {
        // Act
        var classes = _service.GetClassesForRealm(realm);

        // Assert
        Assert.NotEmpty(classes);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Additional Tests (v1.8.2)
    // ─────────────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(Realm.Albion, 16)]
    [InlineData(Realm.Midgard, 15)]
    [InlineData(Realm.Hibernia, 16)]
    public void GetClassesForRealm_ReturnsCorrectCount(Realm realm, int expectedCount)
    {
        // Act
        var classes = _service.GetClassesForRealm(realm);

        // Assert
        Assert.Equal(expectedCount, classes.Count);
    }

    [Fact]
    public void GetRemainingSpecPoints_FullyAllocated_ReturnsZeroOrNegative()
    {
        // Arrange - Over-allocated build
        var build = new CharacterBuild
        {
            Name = "MaxedOut",
            SpecLines = new Dictionary<string, int>
            {
                { "Polearm", 50 } // 1275 points - way over limit
            }
        };

        // Act
        var remaining = _service.GetRemainingSpecPoints(build, CharacterClass.Armsman, 50);

        // Assert
        Assert.True(remaining < 0);
    }

    [Fact]
    public void GetRemainingSpecPoints_EmptyBuild_ReturnsMax()
    {
        // Arrange
        var build = new CharacterBuild { Name = "Empty" };

        // Act
        var remaining = _service.GetRemainingSpecPoints(build, CharacterClass.Armsman, 50);

        // Assert
        Assert.Equal(126, remaining); // Max points at level 50
    }

    [Theory]
    [InlineData(CharacterClass.Armsman)]
    [InlineData(CharacterClass.Paladin)]
    [InlineData(CharacterClass.Mercenary)]
    [InlineData(CharacterClass.Reaver)]
    public void GetTemplateForClass_AlbionMelee_HasPolearmOrSlash(CharacterClass charClass)
    {
        // Act
        var template = _service.GetTemplateForClass(charClass);
        var specNames = template.SpecLines.Select(s => s.Name).ToList();

        // Assert - Melee classes should have some weapon spec
        Assert.True(specNames.Any(s => 
            s.Contains("Slash") || s.Contains("Crush") || s.Contains("Thrust") || 
            s.Contains("Polearm") || s.Contains("Two-Handed")));
    }

    [Theory]
    [InlineData(CharacterClass.Wizard)]
    [InlineData(CharacterClass.Runemaster)]
    [InlineData(CharacterClass.Eldritch)]
    public void GetTemplateForClass_Casters_HasMagicSpecs(CharacterClass charClass)
    {
        // Act
        var template = _service.GetTemplateForClass(charClass);

        // Assert - All spec lines should be magic type for pure casters
        Assert.All(template.SpecLines, spec => 
            Assert.Equal(SpecLineType.Magic, spec.Type));
    }

    [Fact]
    public void CalculateSpecPointCost_ZeroLevel_ReturnsZero()
    {
        // Act
        var cost = _service.CalculateSpecPointCost(0);

        // Assert
        Assert.Equal(0, cost);
    }

    [Fact]
    public void GetMaxSpecPoints_Level25_ReturnsHalfOfLevel50()
    {
        // Act
        var level25Points = _service.GetMaxSpecPoints(25);
        var level50Points = _service.GetMaxSpecPoints(50);

        // Assert - Level 25 should be roughly half
        // (25*2) + 12 + 1 = 63
        Assert.Equal(63, level25Points);
        Assert.True(level25Points < level50Points / 2 + 5);
    }
}
