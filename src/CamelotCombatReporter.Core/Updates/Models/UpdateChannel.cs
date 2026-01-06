namespace CamelotCombatReporter.Core.Updates.Models;

/// <summary>
/// Represents the update release channel.
/// </summary>
public enum UpdateChannel
{
    /// <summary>
    /// Stable release channel with fully tested versions.
    /// </summary>
    Stable,

    /// <summary>
    /// Beta channel with pre-release versions for testing.
    /// </summary>
    Beta,

    /// <summary>
    /// Development channel with latest builds (may be unstable).
    /// </summary>
    Dev
}
