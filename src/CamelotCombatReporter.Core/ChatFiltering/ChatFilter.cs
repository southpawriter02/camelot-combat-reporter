using CamelotCombatReporter.Core.Filtering;

namespace CamelotCombatReporter.Core.ChatFiltering;

/// <summary>
/// Log line filter that filters chat messages based on configuration.
/// </summary>
public class ChatFilter : ILogLineFilter
{
    private readonly ChatFilterSettings _settings;
    private readonly ChatPatternMatcher _matcher;

    /// <inheritdoc />
    public int Priority => 10; // Early in pipeline

    /// <inheritdoc />
    public string Name => "ChatFilter";

    /// <summary>
    /// Creates a new ChatFilter with specified settings.
    /// </summary>
    public ChatFilter(ChatFilterSettings settings)
    {
        _settings = settings;
        _matcher = new ChatPatternMatcher();
    }

    /// <summary>
    /// Creates a ChatFilter with default tactical preset.
    /// </summary>
    public ChatFilter() : this(ChatFilterSettings.CreateFromPreset(FilterPreset.Tactical))
    {
    }

    /// <inheritdoc />
    public FilterResult Filter(string line, int lineNumber, FilterContext context)
    {
        // If filtering is disabled, pass through
        if (!_settings.Enabled)
        {
            return FilterResult.PassToNext;
        }

        // Quick check: if it doesn't look like a chat message, pass through
        if (!_matcher.MightBeChatMessage(line))
        {
            return FilterResult.PassToNext;
        }

        // Try to parse the chat message
        var chatMessage = _matcher.TryParse(line, lineNumber);
        if (chatMessage == null)
        {
            return FilterResult.PassToNext; // Not a recognized chat format
        }

        // Combat messages always pass
        if (chatMessage.Type == ChatMessageType.Combat)
        {
            return FilterResult.KeepLine("Combat message");
        }

        // Check sender whitelist
        if (chatMessage.SenderName != null &&
            _settings.SenderWhitelist.Contains(chatMessage.SenderName))
        {
            return FilterResult.KeepLine($"Sender {chatMessage.SenderName} is whitelisted");
        }

        // Check keyword whitelist
        if (ContainsWhitelistedKeyword(chatMessage.Content))
        {
            return FilterResult.KeepLine("Contains whitelisted keyword");
        }

        // Get channel configuration
        var channelConfig = _settings.GetChannelConfig(chatMessage.Type);

        // Check if channel is enabled
        if (channelConfig.Enabled)
        {
            return FilterResult.KeepLine($"Channel {chatMessage.Type} is enabled");
        }

        // Check if we should keep during combat
        if (channelConfig.KeepDuringCombat && context.IsInCombat)
        {
            return FilterResult.KeepLine($"Channel {chatMessage.Type} kept during combat");
        }

        // Skip this message
        return FilterResult.SkipLine($"Channel {chatMessage.Type} is disabled");
    }

    private bool ContainsWhitelistedKeyword(string content)
    {
        foreach (var keyword in _settings.KeywordWhitelist)
        {
            if (content.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }
}
