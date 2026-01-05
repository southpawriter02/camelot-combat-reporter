using System.Text;
using System.Text.Json;
using CamelotCombatReporter.Core.Models;

namespace CamelotCombatReporter.Core.ChatFiltering;

/// <summary>
/// Options for chat export.
/// </summary>
public record ChatExportOptions(
    bool IncludeTimestamps = true,
    bool IncludeChannelInfo = true,
    bool ApplyPrivacy = false,
    ChatMessageType? ChannelFilter = null
);

/// <summary>
/// Exports and searches chat messages.
/// </summary>
public class ChatExporter
{
    private readonly PrivacyAnonymizer _anonymizer;

    /// <summary>
    /// Creates a new ChatExporter.
    /// </summary>
    public ChatExporter(PrivacySettings? privacySettings = null)
    {
        _anonymizer = new PrivacyAnonymizer(privacySettings ?? new PrivacySettings());
    }

    /// <summary>
    /// Exports chat messages to a file.
    /// </summary>
    public async Task ExportToFileAsync(
        IEnumerable<ChatMessage> messages,
        string outputPath,
        ChatExportOptions options)
    {
        var extension = Path.GetExtension(outputPath).ToLowerInvariant();

        switch (extension)
        {
            case ".json":
                await ExportToJsonAsync(messages, outputPath, options);
                break;
            case ".csv":
                await ExportToCsvAsync(messages, outputPath, options);
                break;
            default:
                await ExportToTextAsync(messages, outputPath, options);
                break;
        }
    }

    /// <summary>
    /// Searches chat messages by query string.
    /// </summary>
    public IEnumerable<ChatMessage> Search(
        IEnumerable<ChatMessage> messages,
        string query,
        ChatMessageType? channelFilter = null)
    {
        var filtered = messages.AsEnumerable();

        // Apply channel filter
        if (channelFilter.HasValue)
        {
            filtered = filtered.Where(m => m.Type == channelFilter.Value);
        }

        // Apply text search
        if (!string.IsNullOrWhiteSpace(query))
        {
            filtered = filtered.Where(m =>
                m.Content.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                (m.SenderName?.Contains(query, StringComparison.OrdinalIgnoreCase) == true));
        }

        return filtered;
    }

    /// <summary>
    /// Cross-references chat messages with combat events.
    /// Finds messages that occurred within a time window of combat events.
    /// </summary>
    public IEnumerable<(ChatMessage Chat, LogEvent Combat)> CrossReference(
        IEnumerable<ChatMessage> chats,
        IEnumerable<LogEvent> combatEvents,
        TimeSpan windowSize)
    {
        var chatList = chats.ToList();
        var combatList = combatEvents.ToList();

        foreach (var chat in chatList)
        {
            // Find combat events within the window
            var nearbyEvents = combatList
                .Where(e =>
                {
                    var diff = chat.Timestamp - e.Timestamp;
                    return diff.Duration() <= windowSize;
                })
                .OrderBy(e => (chat.Timestamp - e.Timestamp).Duration());

            var nearestEvent = nearbyEvents.FirstOrDefault();
            if (nearestEvent != null)
            {
                yield return (chat, nearestEvent);
            }
        }
    }

    /// <summary>
    /// Gets chat statistics for a collection of messages.
    /// </summary>
    public ChatStatistics GetStatistics(IEnumerable<ChatMessage> messages)
    {
        var messageList = messages.ToList();

        var byChannel = messageList
            .GroupBy(m => m.Type)
            .ToDictionary(g => g.Key, g => g.Count());

        var bySender = messageList
            .Where(m => m.SenderName != null)
            .GroupBy(m => m.SenderName!)
            .ToDictionary(g => g.Key, g => g.Count());

        var topSenders = bySender
            .OrderByDescending(kvp => kvp.Value)
            .Take(10)
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

        return new ChatStatistics(
            TotalMessages: messageList.Count,
            MessagesByChannel: byChannel,
            TopSenders: topSenders,
            UniqueParticipants: bySender.Count
        );
    }

    private async Task ExportToJsonAsync(
        IEnumerable<ChatMessage> messages,
        string outputPath,
        ChatExportOptions options)
    {
        var processed = ProcessMessages(messages, options);

        var exportData = processed.Select(m => new
        {
            timestamp = m.Timestamp.ToString("HH:mm:ss"),
            channel = m.Type.ToString(),
            sender = m.SenderName,
            content = m.Content
        });

        var json = JsonSerializer.Serialize(exportData, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        await File.WriteAllTextAsync(outputPath, json);
    }

    private async Task ExportToCsvAsync(
        IEnumerable<ChatMessage> messages,
        string outputPath,
        ChatExportOptions options)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Timestamp,Channel,Sender,Content");

        foreach (var message in ProcessMessages(messages, options))
        {
            var content = message.Content.Replace("\"", "\"\"");
            sb.AppendLine($"{message.Timestamp:HH:mm:ss},{message.Type},{message.SenderName ?? ""},\"{content}\"");
        }

        await File.WriteAllTextAsync(outputPath, sb.ToString());
    }

    private async Task ExportToTextAsync(
        IEnumerable<ChatMessage> messages,
        string outputPath,
        ChatExportOptions options)
    {
        var sb = new StringBuilder();

        foreach (var message in ProcessMessages(messages, options))
        {
            var line = options.IncludeTimestamps
                ? $"[{message.Timestamp:HH:mm:ss}] "
                : "";

            if (options.IncludeChannelInfo)
            {
                line += $"[{message.Type}] ";
            }

            if (message.SenderName != null)
            {
                line += $"{message.SenderName}: ";
            }

            line += message.Content;
            sb.AppendLine(line);
        }

        await File.WriteAllTextAsync(outputPath, sb.ToString());
    }

    private IEnumerable<ChatMessage> ProcessMessages(
        IEnumerable<ChatMessage> messages,
        ChatExportOptions options)
    {
        var filtered = messages.AsEnumerable();

        // Apply channel filter
        if (options.ChannelFilter.HasValue)
        {
            filtered = filtered.Where(m => m.Type == options.ChannelFilter.Value);
        }

        // Apply privacy
        if (options.ApplyPrivacy)
        {
            filtered = filtered
                .Select(m => _anonymizer.ProcessMessage(m))
                .Where(m => m != null)
                .Cast<ChatMessage>();
        }

        return filtered;
    }
}

/// <summary>
/// Statistics about chat messages.
/// </summary>
public record ChatStatistics(
    int TotalMessages,
    IReadOnlyDictionary<ChatMessageType, int> MessagesByChannel,
    IReadOnlyDictionary<string, int> TopSenders,
    int UniqueParticipants
);
