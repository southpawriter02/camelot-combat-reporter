using CamelotCombatReporter.Core.Models;

namespace CamelotCombatReporter.Plugins.Abstractions;

/// <summary>
/// Interface for plugins that extend log parsing capabilities.
/// </summary>
public interface IParserPlugin : IPlugin
{
    /// <summary>
    /// Custom event types this parser can produce.
    /// </summary>
    IReadOnlyCollection<EventTypeDefinition> CustomEventTypes { get; }

    /// <summary>
    /// Parsing patterns registered by this plugin.
    /// </summary>
    IReadOnlyCollection<ParsingPatternDefinition> Patterns { get; }

    /// <summary>
    /// Priority for pattern matching (higher = checked first).
    /// Default is 0. Built-in patterns have priority -100.
    /// </summary>
    int Priority { get; }

    /// <summary>
    /// Attempts to parse a log line.
    /// </summary>
    /// <param name="line">The log line to parse.</param>
    /// <param name="context">Context about the parsing state.</param>
    /// <returns>Parse result indicating success, skip, or error.</returns>
    ParseResult TryParse(string line, ParsingContext context);
}

/// <summary>
/// Defines a custom event type provided by a parser plugin.
/// </summary>
/// <param name="TypeName">Unique name for the event type.</param>
/// <param name="EventType">The .NET type implementing LogEvent.</param>
/// <param name="Description">Description of what this event represents.</param>
public record EventTypeDefinition(
    string TypeName,
    Type EventType,
    string Description);

/// <summary>
/// Defines a parsing pattern registered by a plugin.
/// </summary>
/// <param name="Id">Unique identifier for the pattern.</param>
/// <param name="Description">Description of what this pattern matches.</param>
/// <param name="RegexPattern">The regex pattern used for matching.</param>
public record ParsingPatternDefinition(
    string Id,
    string Description,
    string RegexPattern);

/// <summary>
/// Context provided during parsing.
/// </summary>
/// <param name="LineNumber">Current line number in the file.</param>
/// <param name="PreviousLine">The previous log line, if any.</param>
/// <param name="RecentEvents">Recent events for context (limited to last 10).</param>
public record ParsingContext(
    int LineNumber,
    string? PreviousLine,
    IReadOnlyList<LogEvent> RecentEvents);

/// <summary>
/// Base class for parse results.
/// </summary>
public abstract record ParseResult;

/// <summary>
/// Indicates the line was successfully parsed into an event.
/// </summary>
/// <param name="Event">The parsed event.</param>
public sealed record ParseSuccess(LogEvent Event) : ParseResult;

/// <summary>
/// Indicates this plugin cannot parse this line (try next parser).
/// </summary>
public sealed record ParseSkip : ParseResult
{
    public static ParseSkip Instance { get; } = new();
}

/// <summary>
/// Indicates an error occurred during parsing.
/// </summary>
/// <param name="Message">Error message.</param>
public sealed record ParseError(string Message) : ParseResult;
