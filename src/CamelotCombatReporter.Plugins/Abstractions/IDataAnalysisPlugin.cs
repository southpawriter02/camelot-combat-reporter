using CamelotCombatReporter.Core.Models;

namespace CamelotCombatReporter.Plugins.Abstractions;

/// <summary>
/// Interface for plugins that provide custom data analysis and statistics.
/// </summary>
public interface IDataAnalysisPlugin : IPlugin
{
    /// <summary>
    /// Gets the custom statistics this plugin provides.
    /// </summary>
    IReadOnlyCollection<StatisticDefinition> ProvidedStatistics { get; }

    /// <summary>
    /// Analyzes the given combat events and returns custom statistics.
    /// </summary>
    Task<AnalysisResult> AnalyzeAsync(
        IReadOnlyList<LogEvent> events,
        CombatStatistics? baseStatistics,
        AnalysisOptions options,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Defines a custom statistic provided by an analysis plugin.
/// </summary>
/// <param name="Id">Unique identifier for the statistic.</param>
/// <param name="Name">Display name.</param>
/// <param name="Description">Description of what this statistic measures.</param>
/// <param name="Category">Category for grouping (e.g., "Damage", "Healing", "Performance").</param>
/// <param name="ValueType">Type of the statistic value.</param>
public record StatisticDefinition(
    string Id,
    string Name,
    string Description,
    string Category,
    Type ValueType);

/// <summary>
/// Result from an analysis plugin.
/// </summary>
/// <param name="Statistics">Dictionary of statistic ID to computed value.</param>
/// <param name="Insights">Optional insights derived from the analysis.</param>
public record AnalysisResult(
    IReadOnlyDictionary<string, object> Statistics,
    IReadOnlyList<AnalysisInsight> Insights);

/// <summary>
/// An insight or recommendation from the analysis.
/// </summary>
/// <param name="Title">Short title for the insight.</param>
/// <param name="Description">Detailed description.</param>
/// <param name="Severity">Importance level.</param>
public record AnalysisInsight(
    string Title,
    string Description,
    InsightSeverity Severity);

/// <summary>
/// Severity levels for analysis insights.
/// </summary>
public enum InsightSeverity
{
    Info,
    Suggestion,
    Warning,
    Critical
}

/// <summary>
/// Options for analysis plugins.
/// </summary>
/// <param name="StartTime">Optional start time filter.</param>
/// <param name="EndTime">Optional end time filter.</param>
/// <param name="TargetFilter">Optional target name filter.</param>
/// <param name="DamageTypeFilter">Optional damage type filter.</param>
/// <param name="CombatantName">Name of the player/combatant.</param>
public record AnalysisOptions(
    TimeOnly? StartTime,
    TimeOnly? EndTime,
    string? TargetFilter,
    string? DamageTypeFilter,
    string CombatantName);
