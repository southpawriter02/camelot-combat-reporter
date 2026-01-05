using CamelotCombatReporter.Core.DeathAnalysis.Models;
using CamelotCombatReporter.Core.Models;

namespace CamelotCombatReporter.Core.DeathAnalysis;

/// <summary>
/// Service for analyzing player deaths and generating reports.
/// </summary>
public interface IDeathAnalysisService
{
    /// <summary>
    /// Gets or sets the pre-death window duration (default: 15 seconds).
    /// This is how far back to look for damage events before death.
    /// </summary>
    TimeSpan PreDeathWindow { get; set; }

    /// <summary>
    /// Analyzes a single death event.
    /// </summary>
    /// <param name="death">The death event to analyze.</param>
    /// <param name="allEvents">All combat events from the session.</param>
    /// <param name="victimClass">The class of the victim, if known.</param>
    /// <returns>A detailed death report.</returns>
    DeathReport AnalyzeDeath(
        DeathEvent death,
        IEnumerable<LogEvent> allEvents,
        CharacterClass? victimClass = null);

    /// <summary>
    /// Analyzes all player deaths in a collection of events.
    /// </summary>
    /// <param name="allEvents">All combat events from the session.</param>
    /// <param name="playerClass">The player's class, if known.</param>
    /// <returns>Reports for all player deaths.</returns>
    IReadOnlyList<DeathReport> AnalyzeAllDeaths(
        IEnumerable<LogEvent> allEvents,
        CharacterClass? playerClass = null);

    /// <summary>
    /// Calculates aggregate statistics from a collection of death reports.
    /// </summary>
    /// <param name="deaths">The death reports to analyze.</param>
    /// <param name="sessionDuration">The total session duration, if known.</param>
    /// <returns>Aggregated death statistics.</returns>
    DeathStatistics GetStatistics(
        IEnumerable<DeathReport> deaths,
        TimeSpan? sessionDuration = null);

    /// <summary>
    /// Generates recommendations based on a death report.
    /// </summary>
    /// <param name="report">The death report.</param>
    /// <param name="playerClass">The player's class for class-specific recommendations.</param>
    /// <returns>List of recommendations.</returns>
    IReadOnlyList<Recommendation> GenerateRecommendations(
        DeathReport report,
        CharacterClass? playerClass = null);
}
