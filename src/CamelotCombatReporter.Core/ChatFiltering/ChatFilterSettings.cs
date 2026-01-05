namespace CamelotCombatReporter.Core.ChatFiltering;

/// <summary>
/// Configuration for a single chat channel.
/// </summary>
/// <param name="Enabled">Whether this channel is shown.</param>
/// <param name="KeepDuringCombat">Keep messages during active combat even if disabled.</param>
/// <param name="CustomRegexPattern">Optional custom regex pattern override.</param>
public record ChannelConfig(
    bool Enabled,
    bool KeepDuringCombat,
    string? CustomRegexPattern = null
);

/// <summary>
/// Privacy settings for chat export.
/// </summary>
/// <param name="AnonymizePlayerNames">Replace player names with Player1, Player2, etc.</param>
/// <param name="StripPrivateMessages">Remove PMs before storage/export.</param>
/// <param name="HashIdentifiers">Use consistent hashes for correlation.</param>
public record PrivacySettings(
    bool AnonymizePlayerNames = false,
    bool StripPrivateMessages = false,
    bool HashIdentifiers = false
);

/// <summary>
/// Complete chat filter configuration.
/// </summary>
public class ChatFilterSettings
{
    /// <summary>
    /// Whether chat filtering is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// The active filter preset.
    /// </summary>
    public FilterPreset Preset { get; set; } = FilterPreset.Tactical;

    /// <summary>
    /// Per-channel configuration.
    /// </summary>
    public Dictionary<ChatMessageType, ChannelConfig> ChannelSettings { get; set; } = new();

    /// <summary>
    /// Keywords that always pass the filter (e.g., "inc", "assist", "target").
    /// </summary>
    public HashSet<string> KeywordWhitelist { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Player names whose messages always pass the filter.
    /// </summary>
    public HashSet<string> SenderWhitelist { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Duration in seconds to keep tactical messages after last combat event.
    /// </summary>
    public int CombatContextWindowSeconds { get; set; } = 15;

    /// <summary>
    /// Privacy settings for export.
    /// </summary>
    public PrivacySettings PrivacySettings { get; set; } = new();

    /// <summary>
    /// Creates default settings for a preset.
    /// </summary>
    public static ChatFilterSettings CreateFromPreset(FilterPreset preset)
    {
        var settings = new ChatFilterSettings { Preset = preset };

        switch (preset)
        {
            case FilterPreset.CombatOnly:
                settings.SetAllChannels(false, false);
                break;

            case FilterPreset.Tactical:
                settings.SetAllChannels(false, false);
                settings.ChannelSettings[ChatMessageType.Group] = new ChannelConfig(true, true);
                settings.KeywordWhitelist.Add("inc");
                settings.KeywordWhitelist.Add("incoming");
                settings.KeywordWhitelist.Add("assist");
                settings.KeywordWhitelist.Add("target");
                settings.KeywordWhitelist.Add("ot");
                settings.KeywordWhitelist.Add("on target");
                break;

            case FilterPreset.AllMessages:
                settings.Enabled = false;
                break;

            case FilterPreset.Custom:
                settings.SetAllChannels(true, false);
                break;
        }

        return settings;
    }

    /// <summary>
    /// Sets all channels to the same configuration.
    /// </summary>
    public void SetAllChannels(bool enabled, bool keepDuringCombat)
    {
        foreach (ChatMessageType type in Enum.GetValues<ChatMessageType>())
        {
            if (type != ChatMessageType.Combat && type != ChatMessageType.Unknown)
            {
                ChannelSettings[type] = new ChannelConfig(enabled, keepDuringCombat);
            }
        }
    }

    /// <summary>
    /// Gets the configuration for a channel, with defaults.
    /// </summary>
    public ChannelConfig GetChannelConfig(ChatMessageType channelType)
    {
        if (ChannelSettings.TryGetValue(channelType, out var config))
            return config;

        // Default: disabled, but keep during combat for tactical channels
        var keepDuringCombat = channelType is ChatMessageType.Group or ChatMessageType.Alliance;
        return new ChannelConfig(false, keepDuringCombat);
    }
}
