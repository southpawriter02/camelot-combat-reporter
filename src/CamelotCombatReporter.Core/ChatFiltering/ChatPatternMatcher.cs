using System.Globalization;
using System.Text.RegularExpressions;
using CamelotCombatReporter.Core.Models;

namespace CamelotCombatReporter.Core.ChatFiltering;

/// <summary>
/// Represents a parsed chat message.
/// </summary>
/// <param name="Timestamp">When the message was sent.</param>
/// <param name="Type">The type of chat channel.</param>
/// <param name="SenderName">The sender's name, if applicable.</param>
/// <param name="Content">The message content.</param>
/// <param name="RawLine">The original log line.</param>
/// <param name="LineNumber">The line number in the log file.</param>
public record ChatMessage(
    TimeOnly Timestamp,
    ChatMessageType Type,
    string? SenderName,
    string Content,
    string RawLine,
    int LineNumber
) : LogEvent(Timestamp);

/// <summary>
/// Parses chat messages from log lines and classifies them by channel.
/// </summary>
public class ChatPatternMatcher
{
    // Timestamp pattern for all messages
    private static readonly Regex TimestampPattern = new(
        @"^\[(?<ts>\d{2}:\d{2}:\d{2})\]",
        RegexOptions.Compiled);

    // Chat channel patterns (DAoC format: [HH:mm:ss] [Channel] Player: message)
    private static readonly Regex SayPattern = new(
        @"^\[(?<ts>\d{2}:\d{2}:\d{2})\]\s+\[Say\]\s+(?<sender>\w+):\s*(?<msg>.*)$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex YellPattern = new(
        @"^\[(?<ts>\d{2}:\d{2}:\d{2})\]\s+\[Yell\]\s+(?<sender>\w+):\s*(?<msg>.*)$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex GroupPattern = new(
        @"^\[(?<ts>\d{2}:\d{2}:\d{2})\]\s+\[Group\]\s+(?<sender>\w+):\s*(?<msg>.*)$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex GuildPattern = new(
        @"^\[(?<ts>\d{2}:\d{2}:\d{2})\]\s+\[Guild\]\s+(?<sender>\w+):\s*(?<msg>.*)$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex AlliancePattern = new(
        @"^\[(?<ts>\d{2}:\d{2}:\d{2})\]\s+\[Alliance\]\s+(?<sender>\w+):\s*(?<msg>.*)$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex BroadcastPattern = new(
        @"^\[(?<ts>\d{2}:\d{2}:\d{2})\]\s+\[Broadcast\]\s+(?<sender>\w+):\s*(?<msg>.*)$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex SendPattern = new(
        @"^\[(?<ts>\d{2}:\d{2}:\d{2})\]\s+You send,?\s*""(?<msg>.*)""\s+to\s+(?<sender>\w+)\.$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex TellPattern = new(
        @"^\[(?<ts>\d{2}:\d{2}:\d{2})\]\s+(?<sender>\w+)\s+sends,?\s*""(?<msg>.*)""\s*$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex RegionPattern = new(
        @"^\[(?<ts>\d{2}:\d{2}:\d{2})\]\s+\[Region\]\s+(?<sender>\w+):\s*(?<msg>.*)$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex TradePattern = new(
        @"^\[(?<ts>\d{2}:\d{2}:\d{2})\]\s+\[Trade\]\s+(?<sender>\w+):\s*(?<msg>.*)$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex LFGPattern = new(
        @"^\[(?<ts>\d{2}:\d{2}:\d{2})\]\s+\[LFG\]\s+(?<sender>\w+):\s*(?<msg>.*)$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex AdvicePattern = new(
        @"^\[(?<ts>\d{2}:\d{2}:\d{2})\]\s+\[Advice\]\s+(?<sender>\w+):\s*(?<msg>.*)$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex EmotePattern = new(
        @"^\[(?<ts>\d{2}:\d{2}:\d{2})\]\s+(?<sender>\w+)\s+(?<msg>(?:bows|waves|dances|laughs|cheers|cries|salutes|grins|shrugs|nods|sighs).*)$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    // Pattern registry for extensibility
    private readonly List<(Regex Pattern, ChatMessageType Type)> _patterns = new()
    {
        (SayPattern, ChatMessageType.Say),
        (YellPattern, ChatMessageType.Yell),
        (GroupPattern, ChatMessageType.Group),
        (GuildPattern, ChatMessageType.Guild),
        (AlliancePattern, ChatMessageType.Alliance),
        (BroadcastPattern, ChatMessageType.Broadcast),
        (SendPattern, ChatMessageType.Send),
        (TellPattern, ChatMessageType.Tell),
        (RegionPattern, ChatMessageType.Region),
        (TradePattern, ChatMessageType.Trade),
        (LFGPattern, ChatMessageType.LFG),
        (AdvicePattern, ChatMessageType.Advice),
        (EmotePattern, ChatMessageType.Emote)
    };

    // Quick prefixes for fast rejection
    private static readonly string[] ChatPrefixes = new[]
    {
        "[Say]", "[Yell]", "[Group]", "[Guild]", "[Alliance]", "[Broadcast]",
        "[Region]", "[Trade]", "[LFG]", "[Advice]",
        "You send", "sends,"
    };

    /// <summary>
    /// Checks if a line might be a chat message (quick prefix check).
    /// </summary>
    public bool MightBeChatMessage(string line)
    {
        foreach (var prefix in ChatPrefixes)
        {
            if (line.Contains(prefix, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    /// <summary>
    /// Tries to parse a log line as a chat message.
    /// </summary>
    public ChatMessage? TryParse(string line, int lineNumber)
    {
        foreach (var (pattern, type) in _patterns)
        {
            var match = pattern.Match(line);
            if (match.Success)
            {
                var timestamp = TimeOnly.ParseExact(
                    match.Groups["ts"].Value,
                    "HH:mm:ss",
                    CultureInfo.InvariantCulture);

                var sender = match.Groups["sender"].Success
                    ? match.Groups["sender"].Value
                    : null;

                var content = match.Groups["msg"].Success
                    ? match.Groups["msg"].Value
                    : string.Empty;

                return new ChatMessage(
                    Timestamp: timestamp,
                    Type: type,
                    SenderName: sender,
                    Content: content,
                    RawLine: line,
                    LineNumber: lineNumber
                );
            }
        }

        return null;
    }

    /// <summary>
    /// Classifies a message without full parsing.
    /// </summary>
    public ChatMessageType ClassifyMessage(string line)
    {
        foreach (var (pattern, type) in _patterns)
        {
            if (pattern.IsMatch(line))
                return type;
        }

        return ChatMessageType.Unknown;
    }

    /// <summary>
    /// Adds a custom pattern for a channel type.
    /// </summary>
    public void AddCustomPattern(Regex pattern, ChatMessageType type)
    {
        _patterns.Insert(0, (pattern, type)); // Custom patterns checked first
    }
}
