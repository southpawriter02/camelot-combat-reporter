using CamelotCombatReporter.Core.Models;

namespace CamelotCombatReporter.Core.Parsing;

/// <summary>
/// Represents the progress of a parsing operation.
/// </summary>
/// <param name="LinesProcessed">Number of lines processed so far.</param>
/// <param name="TotalLines">Total number of lines in the file (0 if unknown).</param>
/// <param name="EventsFound">Number of events parsed so far.</param>
/// <param name="PercentComplete">Completion percentage (0-100).</param>
/// <param name="Elapsed">Time elapsed since parsing started.</param>
/// <param name="EstimatedRemaining">Estimated time remaining (null if unknown).</param>
public record ParseProgress(
    long LinesProcessed,
    long TotalLines,
    int EventsFound,
    double PercentComplete,
    TimeSpan Elapsed,
    TimeSpan? EstimatedRemaining)
{
    /// <summary>
    /// Gets the lines processed per second.
    /// </summary>
    public double LinesPerSecond => Elapsed.TotalSeconds > 0
        ? LinesProcessed / Elapsed.TotalSeconds
        : 0;

    /// <summary>
    /// Gets a human-readable status message.
    /// </summary>
    public string StatusMessage
    {
        get
        {
            if (TotalLines > 0)
            {
                return $"Parsing... {PercentComplete:F1}% ({LinesProcessed:N0} / {TotalLines:N0} lines, {EventsFound:N0} events)";
            }
            return $"Parsing... ({LinesProcessed:N0} lines, {EventsFound:N0} events)";
        }
    }
}

/// <summary>
/// Result of a completed parse operation.
/// </summary>
/// <param name="Events">The parsed events.</param>
/// <param name="TotalLines">Total lines processed.</param>
/// <param name="ParseTime">Time taken to parse.</param>
/// <param name="WasCancelled">Whether the operation was cancelled.</param>
public record ParseResult(
    IReadOnlyList<LogEvent> Events,
    long TotalLines,
    TimeSpan ParseTime,
    bool WasCancelled = false);
