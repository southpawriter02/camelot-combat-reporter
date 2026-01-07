using CamelotCombatReporter.Core.CharacterBuilding.Models;
using CamelotCombatReporter.Core.CharacterBuilding.Templates;
using CamelotCombatReporter.Core.Models;

namespace CamelotCombatReporter.Core.CharacterBuilding.Services;

/// <summary>
/// Provides class-specific specialization templates and validation.
/// </summary>
public interface ISpecializationTemplateService
{
    /// <summary>
    /// Gets the specialization template for a given class.
    /// </summary>
    SpecializationTemplate GetTemplateForClass(CharacterClass charClass);

    /// <summary>
    /// Gets the maximum spec points available for a character.
    /// Formula: (level * 2) + (level / 2) + 1
    /// At level 50: (50 * 2) + 25 + 1 = 126 points
    /// </summary>
    int GetMaxSpecPoints(int level);

    /// <summary>
    /// Gets the total spec points currently allocated in a build.
    /// </summary>
    int GetAllocatedSpecPoints(CharacterBuild build, CharacterClass charClass);

    /// <summary>
    /// Gets the remaining spec points available.
    /// </summary>
    int GetRemainingSpecPoints(CharacterBuild build, CharacterClass charClass, int level);

    /// <summary>
    /// Validates that a build's spec allocation does not exceed limits.
    /// </summary>
    bool ValidateSpecAllocation(CharacterBuild build, CharacterClass charClass, int level);

    /// <summary>
    /// Gets all available classes for a realm.
    /// </summary>
    IReadOnlyList<CharacterClass> GetClassesForRealm(Realm realm);

    /// <summary>
    /// Calculates the point cost for a given spec level.
    /// Standard formula: sum of 1 to specLevel.
    /// </summary>
    int CalculateSpecPointCost(int specLevel, double multiplier = 1.0);
}
