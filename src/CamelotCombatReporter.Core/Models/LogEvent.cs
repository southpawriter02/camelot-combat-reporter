namespace CamelotCombatReporter.Core.Models;

/// <summary>
/// Base record for all log events.
/// </summary>
/// <param name="Timestamp">The time the event occurred.</param>
public abstract record LogEvent(TimeOnly Timestamp);
