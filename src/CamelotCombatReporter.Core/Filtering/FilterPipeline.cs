using Microsoft.Extensions.Logging;

namespace CamelotCombatReporter.Core.Filtering;

/// <summary>
/// Pipeline that chains multiple log line filters together.
/// Filters are executed in priority order until one returns Keep or Skip.
/// </summary>
public class FilterPipeline
{
    private readonly List<ILogLineFilter> _filters = new();
    private readonly ILogger<FilterPipeline>? _logger;
    private readonly FilterContext _context = new();

    /// <summary>
    /// Gets the shared filter context.
    /// </summary>
    public FilterContext Context => _context;

    /// <summary>
    /// Gets the number of registered filters.
    /// </summary>
    public int FilterCount => _filters.Count;

    /// <summary>
    /// Gets the names of all registered filters in priority order.
    /// </summary>
    public IReadOnlyList<string> FilterNames => _filters.Select(f => f.Name).ToList();

    /// <summary>
    /// Creates a new filter pipeline.
    /// </summary>
    /// <param name="logger">Optional logger for filter decisions.</param>
    public FilterPipeline(ILogger<FilterPipeline>? logger = null)
    {
        _logger = logger;
    }

    /// <summary>
    /// Adds a filter to the pipeline. Filters are automatically sorted by priority.
    /// </summary>
    /// <param name="filter">The filter to add.</param>
    public void AddFilter(ILogLineFilter filter)
    {
        _filters.Add(filter);
        _filters.Sort((a, b) => a.Priority.CompareTo(b.Priority));
        _logger?.LogDebug("Added filter '{FilterName}' with priority {Priority}", filter.Name, filter.Priority);
    }

    /// <summary>
    /// Removes a filter by name.
    /// </summary>
    /// <param name="filterName">The name of the filter to remove.</param>
    /// <returns>True if the filter was found and removed.</returns>
    public bool RemoveFilter(string filterName)
    {
        var filter = _filters.FirstOrDefault(f => f.Name == filterName);
        if (filter != null)
        {
            _filters.Remove(filter);
            _logger?.LogDebug("Removed filter '{FilterName}'", filterName);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Clears all filters from the pipeline.
    /// </summary>
    public void ClearFilters()
    {
        _filters.Clear();
        _logger?.LogDebug("Cleared all filters");
    }

    /// <summary>
    /// Determines whether a log line should be processed.
    /// </summary>
    /// <param name="line">The raw log line.</param>
    /// <param name="lineNumber">The line number in the file.</param>
    /// <returns>True if the line should be processed, false if it should be skipped.</returns>
    public bool ShouldProcess(string line, int lineNumber)
    {
        if (_filters.Count == 0)
        {
            return true; // No filters = process everything
        }

        _context.CurrentLine = line;
        _context.CurrentLineNumber = lineNumber;

        foreach (var filter in _filters)
        {
            var result = filter.Filter(line, lineNumber, _context);

            switch (result.Action)
            {
                case FilterAction.Keep:
                    _logger?.LogTrace("Line {LineNumber} kept by filter '{FilterName}': {Reason}",
                        lineNumber, filter.Name, result.Reason ?? "no reason");
                    return true;

                case FilterAction.Skip:
                    _logger?.LogTrace("Line {LineNumber} skipped by filter '{FilterName}': {Reason}",
                        lineNumber, filter.Name, result.Reason ?? "no reason");
                    return false;

                case FilterAction.Pass:
                    // Continue to next filter
                    continue;
            }
        }

        // No filter made a decision = default to keep
        return true;
    }

    /// <summary>
    /// Determines whether a log line should be processed and returns detailed result.
    /// </summary>
    /// <param name="line">The raw log line.</param>
    /// <param name="lineNumber">The line number in the file.</param>
    /// <returns>Tuple of (shouldProcess, filterName, reason).</returns>
    public (bool ShouldProcess, string? FilterName, string? Reason) ShouldProcessWithDetails(string line, int lineNumber)
    {
        if (_filters.Count == 0)
        {
            return (true, null, "No filters configured");
        }

        _context.CurrentLine = line;
        _context.CurrentLineNumber = lineNumber;

        foreach (var filter in _filters)
        {
            var result = filter.Filter(line, lineNumber, _context);

            switch (result.Action)
            {
                case FilterAction.Keep:
                    return (true, filter.Name, result.Reason);

                case FilterAction.Skip:
                    return (false, filter.Name, result.Reason);

                case FilterAction.Pass:
                    continue;
            }
        }

        return (true, null, "No filter made a decision");
    }

    /// <summary>
    /// Resets the pipeline context.
    /// </summary>
    public void Reset()
    {
        _context.Reset();
    }
}
