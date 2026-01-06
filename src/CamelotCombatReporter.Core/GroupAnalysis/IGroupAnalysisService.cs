using CamelotCombatReporter.Core.GroupAnalysis.Models;
using CamelotCombatReporter.Core.Models;

namespace CamelotCombatReporter.Core.GroupAnalysis;

/// <summary>
/// Service for analyzing group composition, performance, and providing recommendations.
/// </summary>
public interface IGroupAnalysisService
{
    /// <summary>
    /// Analyzes group composition from combat events.
    /// </summary>
    /// <param name="events">All combat log events.</param>
    /// <returns>The analyzed group composition.</returns>
    GroupComposition AnalyzeComposition(IEnumerable<LogEvent> events);

    /// <summary>
    /// Calculates performance metrics for a group composition.
    /// </summary>
    /// <param name="composition">The group composition.</param>
    /// <param name="events">All combat log events.</param>
    /// <returns>Performance metrics for the group.</returns>
    GroupPerformanceMetrics CalculateMetrics(GroupComposition composition, IEnumerable<LogEvent> events);

    /// <summary>
    /// Analyzes how well each role is covered in the composition.
    /// </summary>
    /// <param name="composition">The group composition.</param>
    /// <returns>Coverage analysis for each role.</returns>
    IReadOnlyList<RoleCoverage> AnalyzeRoleCoverage(GroupComposition composition);

    /// <summary>
    /// Calculates the balance score for a composition.
    /// </summary>
    /// <param name="composition">The group composition.</param>
    /// <returns>Balance score from 0-100.</returns>
    double CalculateBalanceScore(GroupComposition composition);

    /// <summary>
    /// Finds the best matching template for the composition.
    /// </summary>
    /// <param name="composition">The group composition.</param>
    /// <returns>Best matching template, or null if no good match.</returns>
    GroupTemplate? MatchTemplate(GroupComposition composition);

    /// <summary>
    /// Generates recommendations for improving the composition.
    /// </summary>
    /// <param name="composition">The group composition.</param>
    /// <returns>List of recommendations sorted by priority.</returns>
    IReadOnlyList<CompositionRecommendation> GenerateRecommendations(GroupComposition composition);

    /// <summary>
    /// Gets all available group templates.
    /// </summary>
    /// <returns>List of available templates.</returns>
    IReadOnlyList<GroupTemplate> GetAvailableTemplates();

    /// <summary>
    /// Performs a full analysis and returns a summary.
    /// </summary>
    /// <param name="events">All combat log events.</param>
    /// <returns>Complete analysis summary.</returns>
    GroupAnalysisSummary PerformFullAnalysis(IEnumerable<LogEvent> events);

    /// <summary>
    /// Adds a manual group member for analysis.
    /// </summary>
    void AddManualMember(string name, CharacterClass? characterClass = null, Realm? realm = null);

    /// <summary>
    /// Removes a manual group member.
    /// </summary>
    bool RemoveManualMember(string name);

    /// <summary>
    /// Gets all manual group members.
    /// </summary>
    IReadOnlyList<(string Name, CharacterClass? Class, Realm? Realm)> GetManualMembers();

    /// <summary>
    /// Clears all manual members.
    /// </summary>
    void ClearManualMembers();

    /// <summary>
    /// Resets the service state.
    /// </summary>
    void Reset();
}
