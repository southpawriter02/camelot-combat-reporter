using CamelotCombatReporter.Core.GroupAnalysis.Models;
using CamelotCombatReporter.Core.Models;

namespace CamelotCombatReporter.Core.GroupAnalysis;

/// <summary>
/// Service for detecting group members from combat log events.
/// Combines inference from combat patterns with manual member configuration.
/// </summary>
public interface IGroupDetectionService
{
    /// <summary>
    /// Time window for considering events as related (for proximity detection).
    /// Default: 10 seconds.
    /// </summary>
    TimeSpan ProximityWindow { get; set; }

    /// <summary>
    /// Minimum number of interactions required to infer a group member.
    /// Default: 3.
    /// </summary>
    int MinInteractions { get; set; }

    /// <summary>
    /// Detects group members from combat events.
    /// Combines inference with manually configured members.
    /// </summary>
    /// <param name="events">All combat log events.</param>
    /// <returns>List of detected group members.</returns>
    IReadOnlyList<GroupMember> DetectGroupMembers(IEnumerable<LogEvent> events);

    /// <summary>
    /// Builds a group composition from detected members.
    /// </summary>
    /// <param name="members">The group members.</param>
    /// <param name="timestamp">The timestamp for the composition.</param>
    /// <returns>The group composition.</returns>
    GroupComposition BuildComposition(IReadOnlyList<GroupMember> members, TimeOnly timestamp);

    /// <summary>
    /// Adds a manually configured group member.
    /// </summary>
    /// <param name="name">The member's name.</param>
    /// <param name="characterClass">The member's class, if known.</param>
    /// <param name="realm">The member's realm, if known.</param>
    void AddManualMember(string name, CharacterClass? characterClass = null, Realm? realm = null);

    /// <summary>
    /// Removes a manually configured group member.
    /// </summary>
    /// <param name="name">The member's name to remove.</param>
    /// <returns>True if removed, false if not found.</returns>
    bool RemoveManualMember(string name);

    /// <summary>
    /// Gets all manually configured group members.
    /// </summary>
    /// <returns>List of manual member names.</returns>
    IReadOnlyList<(string Name, CharacterClass? Class, Realm? Realm)> GetManualMembers();

    /// <summary>
    /// Clears all manually configured members.
    /// </summary>
    void ClearManualMembers();

    /// <summary>
    /// Resets the service state including inferred and manual members.
    /// </summary>
    void Reset();
}
