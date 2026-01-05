using System.Security.Cryptography;
using System.Text;

namespace CamelotCombatReporter.Core.ChatFiltering;

/// <summary>
/// Anonymizes player names and other identifiers for privacy.
/// </summary>
public class PrivacyAnonymizer
{
    private readonly Dictionary<string, string> _nameMap = new(StringComparer.OrdinalIgnoreCase);
    private int _nextId = 1;
    private readonly PrivacySettings _settings;

    /// <summary>
    /// Creates a new PrivacyAnonymizer with specified settings.
    /// </summary>
    public PrivacyAnonymizer(PrivacySettings settings)
    {
        _settings = settings;
    }

    /// <summary>
    /// Creates a PrivacyAnonymizer with default settings.
    /// </summary>
    public PrivacyAnonymizer() : this(new PrivacySettings())
    {
    }

    /// <summary>
    /// Anonymizes a player name.
    /// </summary>
    /// <param name="playerName">The original player name.</param>
    /// <returns>An anonymized name (e.g., "Player1").</returns>
    public string AnonymizeName(string playerName)
    {
        if (!_settings.AnonymizePlayerNames)
            return playerName;

        if (string.IsNullOrEmpty(playerName))
            return playerName;

        if (!_nameMap.TryGetValue(playerName, out var anonymized))
        {
            if (_settings.HashIdentifiers)
            {
                // Use consistent hash for correlation
                anonymized = $"Player_{ComputeHash(playerName)}";
            }
            else
            {
                // Use sequential numbering
                anonymized = $"Player{_nextId++}";
            }
            _nameMap[playerName] = anonymized;
        }

        return anonymized;
    }

    /// <summary>
    /// Anonymizes all player names in a chat message content.
    /// </summary>
    /// <param name="content">The message content.</param>
    /// <param name="knownPlayers">List of player names that might appear in the content.</param>
    /// <returns>The content with anonymized names.</returns>
    public string AnonymizeContent(string content, IEnumerable<string> knownPlayers)
    {
        if (!_settings.AnonymizePlayerNames)
            return content;

        var result = content;
        foreach (var player in knownPlayers)
        {
            if (!string.IsNullOrEmpty(player) && result.Contains(player, StringComparison.OrdinalIgnoreCase))
            {
                var anonymized = AnonymizeName(player);
                result = result.Replace(player, anonymized, StringComparison.OrdinalIgnoreCase);
            }
        }

        return result;
    }

    /// <summary>
    /// Processes a chat message for privacy.
    /// </summary>
    /// <param name="message">The chat message to process.</param>
    /// <returns>The processed message, or null if it should be stripped.</returns>
    public ChatMessage? ProcessMessage(ChatMessage message)
    {
        // Strip private messages if configured
        if (_settings.StripPrivateMessages &&
            (message.Type == ChatMessageType.Send || message.Type == ChatMessageType.Tell))
        {
            return null;
        }

        if (!_settings.AnonymizePlayerNames)
            return message;

        // Anonymize sender name
        var anonymizedSender = message.SenderName != null
            ? AnonymizeName(message.SenderName)
            : null;

        // We can't easily anonymize names in content without knowing all players
        // So we just return with anonymized sender
        return message with
        {
            SenderName = anonymizedSender
        };
    }

    /// <summary>
    /// Gets the mapping of original names to anonymized names.
    /// </summary>
    public IReadOnlyDictionary<string, string> NameMapping => _nameMap;

    /// <summary>
    /// Resets the anonymizer state.
    /// </summary>
    public void Reset()
    {
        _nameMap.Clear();
        _nextId = 1;
    }

    private static string ComputeHash(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        // Use first 6 characters of hex for a short but reasonably unique ID
        return Convert.ToHexString(bytes)[..6];
    }
}
