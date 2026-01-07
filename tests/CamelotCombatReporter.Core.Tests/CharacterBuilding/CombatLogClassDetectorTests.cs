using CamelotCombatReporter.Core.CharacterBuilding.Services;
using CamelotCombatReporter.Core.Models;

namespace CamelotCombatReporter.Core.Tests.CharacterBuilding;

/// <summary>
/// Tests for the CombatLogClassDetector service.
/// </summary>
public class CombatLogClassDetectorTests
{
    private readonly CombatLogClassDetector _detector = new();

    [Fact]
    public void InferClassFromStyles_WithNoStyles_ReturnsNullClass()
    {
        // Arrange
        var styles = Array.Empty<string>();

        // Act
        var result = _detector.InferClassFromStyles(styles);

        // Assert
        Assert.Null(result.InferredClass);
        Assert.Equal(0, result.Confidence);
        Assert.Empty(result.DetectedStyles);
    }

    [Fact]
    public void InferClassFromStyles_WithArmsmanStyles_ReturnsArmsman()
    {
        // Arrange
        var styles = new[] { "Defender's Rage", "Phalanx", "Slam", "Defender's Fury" };

        // Act
        var result = _detector.InferClassFromStyles(styles);

        // Assert
        Assert.Equal(CharacterClass.Armsman, result.InferredClass);
        Assert.True(result.Confidence > 0);
        Assert.Equal(4, result.DetectedStyles.Count);
    }

    [Fact]
    public void InferClassFromStyles_WithInfiltratorStyles_ReturnsAssassinClass()
    {
        // Arrange - Critical Strike styles shared by assassins
        var styles = new[] { "Perforate Artery", "Leaper", "Creeping Death", "Hamstring" };

        // Act
        var result = _detector.InferClassFromStyles(styles);

        // Assert - Should be one of the three assassins
        Assert.NotNull(result.InferredClass);
        Assert.Contains(result.InferredClass!.Value, new[] 
        { 
            CharacterClass.Infiltrator, 
            CharacterClass.Shadowblade, 
            CharacterClass.Nightshade 
        });
        Assert.True(result.Confidence > 0);
    }

    [Fact]
    public void InferClassFromStyles_WithMidgardWarriorStyles_ReturnsWarrior()
    {
        // Arrange
        var styles = new[] { "Evernight", "Arctic Rift", "Frost's Fury", "Thor's Hammer" };

        // Act
        var result = _detector.InferClassFromStyles(styles);

        // Assert
        Assert.Equal(CharacterClass.Warrior, result.InferredClass);
        Assert.True(result.Confidence > 0);
    }

    [Fact]
    public void InferClassFromStyles_WithHiberniaHeroStyles_ReturnsHero()
    {
        // Arrange
        var styles = new[] { "Ancient Spear", "Culainn's Thrust", "Spear of Kings" };

        // Act
        var result = _detector.InferClassFromStyles(styles);

        // Assert
        Assert.Equal(CharacterClass.Hero, result.InferredClass);
        Assert.True(result.Confidence > 0);
    }

    [Fact]
    public void InferClassFromStyles_WithSharedStyles_ReturnsMultipleCandidates()
    {
        // Arrange - Shield styles are shared by many classes
        var styles = new[] { "Slam", "Numb", "Shield Bash" };

        // Act
        var result = _detector.InferClassFromStyles(styles);

        // Assert
        Assert.True(result.CandidateClasses.Count > 1);
    }

    [Fact]
    public void InferClassFromStyles_WithUnknownStyles_ReturnsNoMatch()
    {
        // Arrange
        var styles = new[] { "Unknown Style", "Made Up Attack", "Fake Move" };

        // Act
        var result = _detector.InferClassFromStyles(styles);

        // Assert
        Assert.Null(result.InferredClass);
        Assert.Empty(result.CandidateClasses);
    }

    [Fact]
    public void InferClassFromStyles_TracksStyleUsage()
    {
        // Arrange
        var styles = new[] { "Slam", "Slam", "Slam", "Engage" };

        // Act
        var result = _detector.InferClassFromStyles(styles);

        // Assert
        Assert.True(result.StyleUsage.ContainsKey("Slam"));
        Assert.Equal(3, result.StyleUsage["Slam"]);
        Assert.Equal(1, result.StyleUsage["Engage"]);
    }

    [Fact]
    public void GetStylesForClass_ReturnsStylesForKnownClass()
    {
        // Act
        var styles = _detector.GetStylesForClass(CharacterClass.Armsman);

        // Assert
        Assert.NotEmpty(styles);
        Assert.Contains("Defender's Rage", styles);
    }

    [Fact]
    public void GetStylesForClass_ReturnsStylesForCaster()
    {
        // Act - Casters typically have Focus Staff Strike
        var styles = _detector.GetStylesForClass(CharacterClass.Wizard);

        // Assert
        Assert.NotEmpty(styles);
    }

    [Fact]
    public void GetClassesForStyle_ReturnsBerserkerForBerserkStrike()
    {
        // Arrange - Berserker-specific style
        var style = "Berserk Strike";

        // Act
        var classes = _detector.GetClassesForStyle(style);

        // Assert
        Assert.Contains(CharacterClass.Berserker, classes);
    }

    [Fact]
    public void GetClassesForStyle_ReturnsMultipleClassesForSharedStyle()
    {
        // Arrange
        var style = "Slam";

        // Act
        var classes = _detector.GetClassesForStyle(style);

        // Assert
        Assert.True(classes.Count > 1);
    }

    [Fact]
    public void GetClassesForStyle_ReturnEmptyForUnknownStyle()
    {
        // Arrange
        var style = "Totally Made Up Style";

        // Act
        var classes = _detector.GetClassesForStyle(style);

        // Assert
        Assert.Empty(classes);
    }
}
