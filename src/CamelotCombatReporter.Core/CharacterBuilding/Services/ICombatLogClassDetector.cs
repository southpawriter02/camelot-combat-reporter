using CamelotCombatReporter.Core.Models;

namespace CamelotCombatReporter.Core.CharacterBuilding.Services;

/// <summary>
/// Result of attempting to infer a character's class from combat styles.
/// </summary>
public record ClassInferenceResult(
    /// <summary>The inferred class, or null if inference failed.</summary>
    CharacterClass? InferredClass,
    
    /// <summary>Confidence score from 0.0 to 1.0.</summary>
    double Confidence,
    
    /// <summary>Combat styles detected in the log.</summary>
    IReadOnlyList<string> DetectedStyles,
    
    /// <summary>Usage count per style name.</summary>
    IReadOnlyDictionary<string, int> StyleUsage,
    
    /// <summary>All classes that matched at least one style.</summary>
    IReadOnlyList<CharacterClass> CandidateClasses
);

/// <summary>
/// Infers character class from combat styles used in combat logs.
/// </summary>
public interface ICombatLogClassDetector
{
    /// <summary>
    /// Infers the character class from combat style events in a log.
    /// </summary>
    /// <param name="styleNames">Collection of combat style names used.</param>
    /// <returns>Inference result with class and confidence.</returns>
    ClassInferenceResult InferClassFromStyles(IEnumerable<string> styleNames);

    /// <summary>
    /// Gets all known combat styles for a class.
    /// </summary>
    IReadOnlyList<string> GetStylesForClass(CharacterClass charClass);
    
    /// <summary>
    /// Gets all classes that can use a given style.
    /// </summary>
    IReadOnlyList<CharacterClass> GetClassesForStyle(string styleName);
}
