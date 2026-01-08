# 8. Chat and Non-Combat Log Filtering

## Status: ✅ Complete (v1.1.0)

**Implementation Complete:**
- ✅ Log parsing infrastructure
- ✅ GUI filtering framework
- ✅ Chat message pattern recognition

---

## Description

Filter out non-combat content from log files, including player chat, system messages, trade spam, and other noise. This improves parsing performance, reduces storage requirements, and creates cleaner combat-focused analysis.

## Functionality

### Message Categories to Filter

| Category | Examples | Default |
|----------|----------|---------|
| **Say/Yell** | `[Say] Player: Hello!` | Filter out |
| **Group Chat** | `[Group] Player: inc enemy` | Keep (tactical) |
| **Guild Chat** | `[Guild] Player: anyone for RvR?` | Filter out |
| **Alliance Chat** | `[Alliance] Player: forming BG` | Filter out |
| **Broadcast** | `[Broadcast] Player: WTS items` | Filter out |
| **Private Messages** | `[Send/Reply] Player: message` | Filter out |
| **Region Chat** | Regional announcements | Filter out |
| **System Messages** | Server notifications, tips | Configurable |
| **Trade/LFG** | Trade and group finder | Filter out |
| **Emotes** | `/bow`, `/wave`, custom emotes | Filter out |
| **NPC Dialog** | Quest text, merchant interaction | Configurable |

### Core Features

* **Pre-Parse Filtering:**
  * Filter lines before detailed parsing
  * Significant performance improvement
  * Reduce memory usage for large logs
  * Configurable filter sets

* **Chat Channel Recognition:**
  * Parse chat channel prefixes
  * Handle multiple chat formats
  * Support for private server variations
  * Custom channel definitions

* **Smart Filtering:**
  * Keep tactical group messages
  * Preserve combat-relevant announcements
  * Detect combat callouts in chat
  * Configurable keyword whitelist

* **Chat Export (Optional):**
  * Separate export for filtered chat
  * Chat log with timestamps
  * Search within chat history
  * Cross-reference with combat events

### Filter Configuration

* **Global Presets:**
  * "Combat Only" - Maximum filtering
  * "Tactical" - Keep group/guild during combat
  * "All Messages" - No filtering
  * "Custom" - User-defined rules

* **Per-Channel Settings:**
  * Enable/disable each channel type
  * Whitelist specific senders
  * Keyword-based inclusion
  * Regex pattern matching

* **Context-Aware Filtering:**
  * Keep messages during active combat
  * Filter messages during downtime
  * Adjustable combat detection window

### Performance Benefits

| Scenario | Without Filtering | With Filtering |
|----------|-------------------|----------------|
| 100MB log file | 50,000 events | 12,000 events |
| Parse time | 8 seconds | 2 seconds |
| Memory usage | 400MB | 100MB |
| Storage (JSON) | 25MB | 6MB |

### Privacy Considerations

* **Automatic Privacy Mode:**
  * Strip player names from exports
  * Option to filter private messages before storage
  * Anonymize guild/alliance names
  * Hash or remove identifying information

* **Export Options:**
  * "Privacy-Safe Export" preset
  * Configurable anonymization levels
  * Preview before export

## Requirements

* **Pattern Library:** Regex patterns for all chat types
* **Settings UI:** Filter configuration interface
* **Performance:** Minimal overhead for filtering logic

## Limitations

* Chat format may vary between servers
* Some tactical messages may be incorrectly filtered
* Private server chat formats may differ
* Historical logs may have legacy formats

## Dependencies

* **01-log-parsing.md:** Core parsing infrastructure
* **06-server-type-filters.md:** Server-specific chat formats

## Implementation Phases

### Phase 1: Basic Filtering
- [ ] Identify all chat message patterns in DAoC logs
- [ ] Create ChatMessageType enum
- [ ] Implement pre-parse line filtering
- [ ] Add filter toggle to settings

### Phase 2: Channel Configuration
- [ ] Create filter configuration model
- [ ] Build settings UI for filter options
- [ ] Implement preset profiles
- [ ] Add per-channel enable/disable

### Phase 3: Smart Filtering
- [ ] Implement combat-context detection
- [ ] Add keyword whitelist system
- [ ] Create tactical message detection
- [ ] Build regex pattern customization

### Phase 4: Chat Export
- [ ] Create separate chat log storage
- [ ] Implement chat search functionality
- [ ] Add chat timeline view
- [ ] Cross-reference with combat events

## Technical Notes

### Message Pattern Recognition

```csharp
public enum ChatMessageType
{
    Unknown,
    Say,
    Yell,
    Group,
    Guild,
    Alliance,
    Broadcast,
    Send,          // Outgoing private message
    Reply,         // Incoming private message
    Region,
    Trade,
    LFG,
    Emote,
    System,
    NpcDialog,
    Combat         // Combat-related (keep)
}

public static class ChatPatterns
{
    // Pattern examples - actual patterns depend on DAoC log format
    public static readonly Regex SayPattern = new(@"^\[Say\] (\w+): (.+)$");
    public static readonly Regex GroupPattern = new(@"^\[Group\] (\w+): (.+)$");
    public static readonly Regex GuildPattern = new(@"^\[Guild\] (\w+): (.+)$");
    // ... etc
}
```

### Filter Configuration Model

```csharp
public record ChatFilterSettings(
    bool EnableFiltering,
    FilterPreset Preset,
    IReadOnlyDictionary<ChatMessageType, ChannelFilterConfig> ChannelSettings,
    IReadOnlyList<string> WhitelistKeywords,
    IReadOnlyList<string> WhitelistSenders,
    bool KeepDuringCombat,
    TimeSpan CombatContextWindow
);

public record ChannelFilterConfig(
    bool Enabled,
    bool KeepDuringCombat,
    IReadOnlyList<string> CustomPatterns
);

public enum FilterPreset
{
    CombatOnly,    // Maximum filtering
    Tactical,      // Keep group during combat
    AllMessages,   // No filtering
    Custom         // User-defined
}
```

### Pre-Parse Filter Implementation

```csharp
public interface IChatFilter
{
    bool ShouldProcess(string logLine);
    ChatMessageType ClassifyMessage(string logLine);
}

public class ChatFilter : IChatFilter
{
    private readonly ChatFilterSettings _settings;

    public bool ShouldProcess(string logLine)
    {
        if (!_settings.EnableFiltering)
            return true;

        var messageType = ClassifyMessage(logLine);

        if (messageType == ChatMessageType.Combat)
            return true;

        if (!_settings.ChannelSettings.TryGetValue(messageType, out var config))
            return false;

        return config.Enabled;
    }
}
```

### Performance Optimization

```csharp
// Fast prefix-based filtering before regex
private static readonly string[] ChatPrefixes =
{
    "[Say]", "[Yell]", "[Group]", "[Guild]",
    "[Alliance]", "[Broadcast]", "[Send]", "[Reply]"
};

public bool QuickFilter(ReadOnlySpan<char> line)
{
    foreach (var prefix in ChatPrefixes)
    {
        if (line.StartsWith(prefix))
            return false; // Filtered out
    }
    return true; // Process further
}
```
