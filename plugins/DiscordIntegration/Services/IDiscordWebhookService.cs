using DiscordIntegration.Models;

namespace DiscordIntegration.Services;

/// <summary>
/// Service for posting messages to Discord via webhooks.
/// </summary>
public interface IDiscordWebhookService
{
    /// <summary>
    /// Posts an embed to the configured Discord webhook.
    /// </summary>
    /// <param name="embed">The embed to post.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if successful, false otherwise.</returns>
    Task<bool> PostEmbedAsync(DiscordEmbed embed, CancellationToken ct = default);

    /// <summary>
    /// Posts a simple text message to Discord.
    /// </summary>
    /// <param name="content">The message content.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if successful, false otherwise.</returns>
    Task<bool> PostMessageAsync(string content, CancellationToken ct = default);

    /// <summary>
    /// Tests if the webhook URL is valid and accessible.
    /// </summary>
    /// <param name="webhookUrl">The webhook URL to test.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if valid, false otherwise.</returns>
    Task<bool> TestWebhookAsync(string webhookUrl, CancellationToken ct = default);
}
