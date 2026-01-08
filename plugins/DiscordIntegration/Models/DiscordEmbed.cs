using System.Text.Json.Serialization;

namespace DiscordIntegration.Models;

/// <summary>
/// Discord embed for rich message formatting.
/// </summary>
/// <remarks>
/// Follows the Discord embed structure:
/// https://discord.com/developers/docs/resources/channel#embed-object
/// </remarks>
public class DiscordEmbed
{
    /// <summary>Title of the embed.</summary>
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    /// <summary>Description text.</summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>Color code (decimal format).</summary>
    [JsonPropertyName("color")]
    public int Color { get; set; }

    /// <summary>ISO8601 timestamp.</summary>
    [JsonPropertyName("timestamp")]
    public DateTime? Timestamp { get; set; }

    /// <summary>Author information.</summary>
    [JsonPropertyName("author")]
    public EmbedAuthor? Author { get; set; }

    /// <summary>Thumbnail image.</summary>
    [JsonPropertyName("thumbnail")]
    public EmbedThumbnail? Thumbnail { get; set; }

    /// <summary>Fields in the embed.</summary>
    [JsonPropertyName("fields")]
    public List<EmbedField>? Fields { get; set; }

    /// <summary>Footer information.</summary>
    [JsonPropertyName("footer")]
    public EmbedFooter? Footer { get; set; }
}

/// <summary>
/// Author section of a Discord embed.
/// </summary>
public class EmbedAuthor
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("icon_url")]
    public string? IconUrl { get; set; }

    [JsonPropertyName("url")]
    public string? Url { get; set; }
}

/// <summary>
/// Thumbnail image for a Discord embed.
/// </summary>
public class EmbedThumbnail
{
    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;
}

/// <summary>
/// Field within a Discord embed.
/// </summary>
public class EmbedField
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("value")]
    public string Value { get; set; } = string.Empty;

    [JsonPropertyName("inline")]
    public bool Inline { get; set; }

    public EmbedField() { }

    public EmbedField(string name, string value, bool inline = false)
    {
        Name = name;
        Value = value;
        Inline = inline;
    }
}

/// <summary>
/// Footer section of a Discord embed.
/// </summary>
public class EmbedFooter
{
    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;

    [JsonPropertyName("icon_url")]
    public string? IconUrl { get; set; }
}

/// <summary>
/// Webhook payload for Discord API.
/// </summary>
public class WebhookPayload
{
    [JsonPropertyName("content")]
    public string? Content { get; set; }

    [JsonPropertyName("username")]
    public string? Username { get; set; }

    [JsonPropertyName("avatar_url")]
    public string? AvatarUrl { get; set; }

    [JsonPropertyName("embeds")]
    public List<DiscordEmbed>? Embeds { get; set; }
}
