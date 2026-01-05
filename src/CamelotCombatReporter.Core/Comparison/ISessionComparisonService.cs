using CamelotCombatReporter.Core.Comparison.Models;

namespace CamelotCombatReporter.Core.Comparison;

/// <summary>
/// Service for comparing combat sessions.
/// </summary>
public interface ISessionComparisonService
{
    /// <summary>
    /// Compares two sessions and returns detailed delta information.
    /// </summary>
    /// <param name="baseSession">The baseline/older session.</param>
    /// <param name="compareSession">The comparison/newer session.</param>
    /// <returns>A complete comparison between the two sessions.</returns>
    SessionComparison Compare(SessionSummary baseSession, SessionSummary compareSession);

    /// <summary>
    /// Calculates deltas between two sessions.
    /// </summary>
    /// <param name="baseSession">The baseline/older session.</param>
    /// <param name="compareSession">The comparison/newer session.</param>
    /// <returns>List of metric deltas.</returns>
    IReadOnlyList<MetricDelta> CalculateDeltas(SessionSummary baseSession, SessionSummary compareSession);

    /// <summary>
    /// Generates a human-readable summary of a comparison.
    /// </summary>
    /// <param name="comparison">The comparison to summarize.</param>
    /// <returns>Summary text.</returns>
    string GenerateComparisonSummary(SessionComparison comparison);

    /// <summary>
    /// Loads recent session summaries for comparison.
    /// </summary>
    /// <param name="count">Number of recent sessions to load.</param>
    /// <returns>List of session summaries.</returns>
    IReadOnlyList<SessionSummary> LoadSessionHistory(int count = 10);

    /// <summary>
    /// Creates a summary from a session ID.
    /// </summary>
    /// <param name="sessionId">The session ID to summarize.</param>
    /// <returns>Session summary.</returns>
    Task<SessionSummary> CreateSummaryFromSessionAsync(Guid sessionId);
}
