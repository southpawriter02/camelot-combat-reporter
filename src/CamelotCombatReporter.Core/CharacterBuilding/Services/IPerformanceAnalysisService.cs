using CamelotCombatReporter.Core.CharacterBuilding.Models;
using CamelotCombatReporter.Core.Models;

namespace CamelotCombatReporter.Core.CharacterBuilding.Services;

/// <summary>
/// Analyzes combat sessions to calculate performance metrics for character builds.
/// </summary>
public interface IPerformanceAnalysisService
{
    /// <summary>
    /// Calculates aggregated performance metrics from a collection of combat sessions.
    /// </summary>
    Task<BuildPerformanceMetrics> CalculateMetricsAsync(IEnumerable<ExtendedCombatStatistics> sessions);

    /// <summary>
    /// Calculates metrics for all sessions attached to a profile.
    /// </summary>
    Task<BuildPerformanceMetrics> CalculateMetricsForProfileAsync(
        Guid profileId, 
        DateRange? dateRange = null,
        Guid? buildId = null);

    /// <summary>
    /// Gets the top damage sources across all attached sessions.
    /// </summary>
    Task<IReadOnlyList<DamageBreakdown>> GetTopDamageSourcesAsync(
        Guid profileId, 
        int topN = 10,
        DateRange? dateRange = null);

    /// <summary>
    /// Recalculates and updates the performance metrics for a profile's active build.
    /// </summary>
    Task UpdateBuildMetricsAsync(Guid profileId);
}

/// <summary>
/// Represents a date range for filtering.
/// </summary>
public record DateRange(DateTime StartUtc, DateTime EndUtc)
{
    public bool Contains(DateTime dateUtc) => dateUtc >= StartUtc && dateUtc <= EndUtc;
    
    public static DateRange LastDays(int days) => 
        new(DateTime.UtcNow.AddDays(-days), DateTime.UtcNow);
    
    public static DateRange LastWeek => LastDays(7);
    public static DateRange LastMonth => LastDays(30);
    public static DateRange All => new(DateTime.MinValue, DateTime.MaxValue);
}
