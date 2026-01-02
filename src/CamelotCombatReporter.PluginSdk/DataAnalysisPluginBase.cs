using CamelotCombatReporter.Core.Models;
using CamelotCombatReporter.Plugins.Abstractions;

namespace CamelotCombatReporter.PluginSdk;

/// <summary>
/// Base class for data analysis plugins.
/// Extend this class to create custom statistics and metrics analysis.
/// </summary>
public abstract class DataAnalysisPluginBase : PluginBase, IDataAnalysisPlugin
{
    /// <inheritdoc/>
    public sealed override PluginType Type => PluginType.DataAnalysis;

    /// <summary>
    /// Statistics provided by this analysis plugin.
    /// </summary>
    public abstract IReadOnlyCollection<StatisticDefinition> ProvidedStatistics { get; }

    /// <summary>
    /// Performs the analysis on combat data.
    /// </summary>
    public abstract Task<AnalysisResult> AnalyzeAsync(
        IReadOnlyList<LogEvent> events,
        CombatStatistics? baseStatistics,
        AnalysisOptions options,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a successful analysis result.
    /// </summary>
    protected AnalysisResult Success(
        Dictionary<string, object> statistics,
        IEnumerable<AnalysisInsight>? insights = null)
    {
        return new AnalysisResult(
            statistics.AsReadOnly(),
            insights?.ToList() ?? new List<AnalysisInsight>());
    }

    /// <summary>
    /// Creates an empty analysis result.
    /// </summary>
    protected AnalysisResult Empty()
    {
        return new AnalysisResult(
            new Dictionary<string, object>().AsReadOnly(),
            new List<AnalysisInsight>());
    }

    /// <summary>
    /// Creates a statistic definition.
    /// </summary>
    protected StatisticDefinition DefineStatistic(
        string id,
        string name,
        string description,
        string category,
        Type valueType)
    {
        return new StatisticDefinition(id, name, description, category, valueType);
    }

    /// <summary>
    /// Creates a statistic definition for numeric values.
    /// </summary>
    protected StatisticDefinition DefineNumericStatistic(
        string id,
        string name,
        string description,
        string category)
    {
        return new StatisticDefinition(id, name, description, category, typeof(double));
    }

    /// <summary>
    /// Creates an analysis insight.
    /// </summary>
    protected AnalysisInsight Insight(
        string title,
        string description,
        InsightSeverity severity = InsightSeverity.Info)
    {
        return new AnalysisInsight(title, description, severity);
    }

    /// <summary>
    /// Filters events to damage events dealt by the combatant.
    /// </summary>
    protected IEnumerable<DamageEvent> GetDamageDealt(
        IReadOnlyList<LogEvent> events,
        string combatantName)
    {
        return events.OfType<DamageEvent>()
            .Where(e => e.Source == combatantName);
    }

    /// <summary>
    /// Filters events to damage events taken by the combatant.
    /// </summary>
    protected IEnumerable<DamageEvent> GetDamageTaken(
        IReadOnlyList<LogEvent> events,
        string combatantName)
    {
        return events.OfType<DamageEvent>()
            .Where(e => e.Target == combatantName);
    }

    /// <summary>
    /// Filters events to healing events done by the combatant.
    /// </summary>
    protected IEnumerable<HealingEvent> GetHealingDone(
        IReadOnlyList<LogEvent> events,
        string combatantName)
    {
        return events.OfType<HealingEvent>()
            .Where(e => e.Source == combatantName);
    }
}
