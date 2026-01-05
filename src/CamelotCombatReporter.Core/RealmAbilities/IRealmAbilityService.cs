using CamelotCombatReporter.Core.Models;
using CamelotCombatReporter.Core.RealmAbilities.Models;

namespace CamelotCombatReporter.Core.RealmAbilities;

/// <summary>
/// Service for tracking and analyzing realm ability usage.
/// </summary>
public interface IRealmAbilityService
{
    /// <summary>
    /// Gets the realm ability database.
    /// </summary>
    IRealmAbilityDatabase Database { get; }

    /// <summary>
    /// Extracts realm ability activations from log events.
    /// </summary>
    /// <param name="events">The log events to analyze.</param>
    /// <returns>List of realm ability activations.</returns>
    IReadOnlyList<RealmAbilityActivation> ExtractActivations(IEnumerable<LogEvent> events);

    /// <summary>
    /// Calculates statistics for realm ability usage.
    /// </summary>
    /// <param name="activations">The activations to analyze.</param>
    /// <param name="sessionDuration">The total session duration.</param>
    /// <returns>Session statistics.</returns>
    RealmAbilitySessionStats CalculateStatistics(IEnumerable<RealmAbilityActivation> activations, TimeSpan sessionDuration);

    /// <summary>
    /// Builds a timeline of realm ability events.
    /// </summary>
    /// <param name="activations">The activations to include.</param>
    /// <returns>Timeline entries for visualization.</returns>
    IReadOnlyList<RealmAbilityTimelineEntry> BuildTimeline(IEnumerable<RealmAbilityActivation> activations);

    /// <summary>
    /// Gets the current cooldown state of all tracked abilities.
    /// </summary>
    /// <param name="activations">Previous activations.</param>
    /// <param name="currentTime">Current time to check against.</param>
    /// <returns>Cooldown states for each used ability.</returns>
    IReadOnlyList<CooldownState> GetCooldownStates(IEnumerable<RealmAbilityActivation> activations, TimeOnly currentTime);

    /// <summary>
    /// Gets all abilities available in a specific era.
    /// </summary>
    /// <param name="era">The game era.</param>
    /// <returns>Available abilities.</returns>
    IReadOnlyList<RealmAbility> GetAbilitiesForEra(GameEra era);

    /// <summary>
    /// Gets all abilities available to a specific realm.
    /// </summary>
    /// <param name="realm">The realm.</param>
    /// <returns>Available abilities.</returns>
    IReadOnlyList<RealmAbility> GetAbilitiesForRealm(RealmAvailability realm);

    /// <summary>
    /// Tries to match an ability name from a log event to a known ability.
    /// </summary>
    /// <param name="abilityName">The ability name from the log.</param>
    /// <returns>The matched ability, or null if not found.</returns>
    RealmAbility? MatchAbility(string abilityName);

    /// <summary>
    /// Resets the service state.
    /// </summary>
    void Reset();
}
