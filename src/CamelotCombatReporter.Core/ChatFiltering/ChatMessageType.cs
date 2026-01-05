namespace CamelotCombatReporter.Core.ChatFiltering;

/// <summary>
/// Types of chat messages in DAoC logs.
/// </summary>
public enum ChatMessageType
{
    /// <summary>Unknown message type.</summary>
    Unknown = 0,

    /// <summary>Combat-related messages (always kept).</summary>
    Combat = 1,

    // Social channels

    /// <summary>Local say chat.</summary>
    Say = 10,
    /// <summary>Yell/shout chat.</summary>
    Yell = 11,
    /// <summary>Group/party chat (tactical).</summary>
    Group = 12,
    /// <summary>Guild chat.</summary>
    Guild = 13,
    /// <summary>Alliance chat.</summary>
    Alliance = 14,
    /// <summary>Broadcast/region-wide chat.</summary>
    Broadcast = 15,

    // Private messaging

    /// <summary>Outgoing private message (send).</summary>
    Send = 20,
    /// <summary>Incoming private message (tell).</summary>
    Tell = 21,

    // Regional/System

    /// <summary>Region chat.</summary>
    Region = 30,
    /// <summary>Trade chat.</summary>
    Trade = 31,
    /// <summary>Looking for group.</summary>
    LFG = 32,
    /// <summary>System messages.</summary>
    System = 33,
    /// <summary>Advice/help channel.</summary>
    Advice = 34,

    // Other

    /// <summary>Emotes.</summary>
    Emote = 40,
    /// <summary>NPC dialog.</summary>
    NpcDialog = 41
}

/// <summary>
/// Filter presets for quick configuration.
/// </summary>
public enum FilterPreset
{
    /// <summary>Filter all non-combat messages.</summary>
    CombatOnly,
    /// <summary>Keep group chat during combat.</summary>
    Tactical,
    /// <summary>No filtering - show all messages.</summary>
    AllMessages,
    /// <summary>Custom per-channel configuration.</summary>
    Custom
}
