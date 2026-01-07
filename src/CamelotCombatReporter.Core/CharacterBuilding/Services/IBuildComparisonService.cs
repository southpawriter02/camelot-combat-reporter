using CamelotCombatReporter.Core.CharacterBuilding.Models;

namespace CamelotCombatReporter.Core.CharacterBuilding.Services;

/// <summary>
/// Service for comparing character builds.
/// </summary>
public interface IBuildComparisonService
{
    /// <summary>
    /// Compares two builds and returns detailed deltas.
    /// </summary>
    /// <param name="buildA">First build (baseline).</param>
    /// <param name="buildB">Second build to compare against baseline.</param>
    BuildComparisonResult CompareBuilds(CharacterBuild buildA, CharacterBuild buildB);
}
