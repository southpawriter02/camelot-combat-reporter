namespace CamelotCombatReporter.Core.Filtering;

/// <summary>
/// Result of a filter operation on a log line.
/// </summary>
public enum FilterAction
{
    /// <summary>Include the line in output.</summary>
    Keep,
    /// <summary>Exclude the line from output.</summary>
    Skip,
    /// <summary>Let the next filter in the chain decide.</summary>
    Pass
}

/// <summary>
/// Result from a log line filter.
/// </summary>
/// <param name="Action">The action to take for this line.</param>
/// <param name="Reason">Optional reason for the decision (for debugging/logging).</param>
public record FilterResult(FilterAction Action, string? Reason = null)
{
    /// <summary>Singleton for passing to next filter.</summary>
    public static FilterResult PassToNext { get; } = new(FilterAction.Pass);

    /// <summary>Creates a Keep result with optional reason.</summary>
    public static FilterResult KeepLine(string? reason = null) => new(FilterAction.Keep, reason);

    /// <summary>Creates a Skip result with optional reason.</summary>
    public static FilterResult SkipLine(string? reason = null) => new(FilterAction.Skip, reason);
}

/// <summary>
/// Interface for log line filters that can filter lines before or after parsing.
/// Filters are applied in priority order (lower priority number = earlier in pipeline).
/// </summary>
public interface ILogLineFilter
{
    /// <summary>
    /// Priority for filter ordering. Lower values are checked first.
    /// Recommended ranges:
    /// - 0-99: Pre-parse quick filters (chat, server context)
    /// - 100-199: Context-aware filters
    /// - 200+: Post-parse filters
    /// </summary>
    int Priority { get; }

    /// <summary>
    /// Display name for the filter (used in logging and UI).
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Determines whether the log line should be processed.
    /// </summary>
    /// <param name="line">The raw log line.</param>
    /// <param name="lineNumber">The line number in the file.</param>
    /// <param name="context">Shared context with combat state information.</param>
    /// <returns>A FilterResult indicating whether to keep, skip, or pass the line.</returns>
    FilterResult Filter(string line, int lineNumber, FilterContext context);
}
