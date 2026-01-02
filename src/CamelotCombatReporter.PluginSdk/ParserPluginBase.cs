using CamelotCombatReporter.Core.Models;
using CamelotCombatReporter.Plugins.Abstractions;

namespace CamelotCombatReporter.PluginSdk;

/// <summary>
/// Base class for custom parser plugins.
/// Extend this class to add new log parsing patterns or event types.
/// </summary>
public abstract class ParserPluginBase : PluginBase, IParserPlugin
{
    /// <inheritdoc/>
    public sealed override PluginType Type => PluginType.CustomParser;

    /// <summary>
    /// Priority for this parser. Higher values are checked first.
    /// Built-in patterns have priority -100.
    /// </summary>
    public virtual int Priority => 0;

    /// <summary>
    /// Custom event types defined by this parser.
    /// </summary>
    public abstract IReadOnlyCollection<EventTypeDefinition> CustomEventTypes { get; }

    /// <summary>
    /// Parsing patterns used by this parser.
    /// </summary>
    public abstract IReadOnlyCollection<ParsingPatternDefinition> Patterns { get; }

    /// <summary>
    /// Attempts to parse a log line.
    /// </summary>
    public abstract ParseResult TryParse(string line, ParsingContext context);

    /// <summary>
    /// Creates a successful parse result.
    /// </summary>
    protected ParseSuccess Parsed(LogEvent logEvent)
    {
        return new ParseSuccess(logEvent);
    }

    /// <summary>
    /// Creates a result indicating the line was not recognized by this parser.
    /// </summary>
    protected ParseSkip Skip()
    {
        return ParseSkip.Instance;
    }

    /// <summary>
    /// Creates a parse error result.
    /// </summary>
    protected ParseError Error(string message)
    {
        return new ParseError(message);
    }

    /// <summary>
    /// Creates an event type definition.
    /// </summary>
    protected EventTypeDefinition DefineEventType(
        string typeName,
        Type eventType,
        string description)
    {
        return new EventTypeDefinition(typeName, eventType, description);
    }

    /// <summary>
    /// Creates a parsing pattern definition.
    /// </summary>
    protected ParsingPatternDefinition DefinePattern(
        string id,
        string description,
        string regexPattern)
    {
        return new ParsingPatternDefinition(id, description, regexPattern);
    }

    /// <summary>
    /// Tries to extract the timestamp from the beginning of a log line.
    /// Returns null if no timestamp found.
    /// </summary>
    protected TimeOnly? TryExtractTimestamp(string line)
    {
        // Common timestamp format: [HH:mm:ss] or HH:mm:ss
        if (line.Length >= 8)
        {
            var timePart = line.StartsWith('[') ? line.Substring(1, 8) : line.Substring(0, 8);
            if (TimeOnly.TryParse(timePart, out var timestamp))
            {
                return timestamp;
            }
        }
        return null;
    }
}
