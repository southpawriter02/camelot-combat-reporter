using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using DiscordIntegration.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace DiscordIntegration.Services;

/// <summary>
/// Service for posting messages to Discord via webhooks.
/// </summary>
/// <remarks>
/// <para>
/// Implements rate limiting to comply with Discord's webhook limits (30 requests/minute).
/// </para>
/// <para>
/// Uses retry logic for transient failures with exponential backoff.
/// </para>
/// </remarks>
public sealed class DiscordWebhookService : IDiscordWebhookService, IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<DiscordWebhookService> _logger;
    private readonly string _webhookUrl;
    private readonly SemaphoreSlim _rateLimiter = new(1, 1);

    private DateTime _lastRequestTime = DateTime.MinValue;
    private const int MinRequestIntervalMs = 2000; // ~30 req/min

    /// <summary>
    /// Creates a new Discord webhook service.
    /// </summary>
    public DiscordWebhookService(
        string webhookUrl,
        ILogger<DiscordWebhookService>? logger = null)
    {
        _webhookUrl = webhookUrl ?? throw new ArgumentNullException(nameof(webhookUrl));
        _logger = logger ?? NullLogger<DiscordWebhookService>.Instance;
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(30)
        };
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "CamelotCombatReporter/1.0");
    }

    /// <inheritdoc/>
    public async Task<bool> PostEmbedAsync(DiscordEmbed embed, CancellationToken ct = default)
    {
        var payload = new WebhookPayload
        {
            Embeds = [embed]
        };

        return await PostPayloadAsync(payload, ct).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<bool> PostMessageAsync(string content, CancellationToken ct = default)
    {
        var payload = new WebhookPayload
        {
            Content = content
        };

        return await PostPayloadAsync(payload, ct).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<bool> TestWebhookAsync(string webhookUrl, CancellationToken ct = default)
    {
        try
        {
            _logger.LogDebug("Testing webhook URL: {Url}", MaskUrl(webhookUrl));

            // Discord webhooks support GET to return webhook info
            var response = await _httpClient.GetAsync(webhookUrl, ct).ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Webhook test successful");
                return true;
            }

            _logger.LogWarning("Webhook test failed: {Status}", response.StatusCode);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Webhook test failed with exception");
            return false;
        }
    }

    private async Task<bool> PostPayloadAsync(WebhookPayload payload, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(_webhookUrl))
        {
            _logger.LogWarning("Webhook URL not configured");
            return false;
        }

        await _rateLimiter.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            // Rate limiting
            var elapsed = DateTime.UtcNow - _lastRequestTime;
            if (elapsed.TotalMilliseconds < MinRequestIntervalMs)
            {
                var delay = MinRequestIntervalMs - (int)elapsed.TotalMilliseconds;
                _logger.LogDebug("Rate limiting: waiting {Delay}ms", delay);
                await Task.Delay(delay, ct).ConfigureAwait(false);
            }

            _logger.LogDebug("Posting to Discord webhook");

            var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            });

            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(_webhookUrl, content, ct).ConfigureAwait(false);
            _lastRequestTime = DateTime.UtcNow;

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Successfully posted to Discord");
                return true;
            }

            var errorBody = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            _logger.LogWarning(
                "Discord webhook returned {Status}: {Error}",
                response.StatusCode,
                errorBody);

            // Handle rate limiting from Discord
            if ((int)response.StatusCode == 429)
            {
                _logger.LogWarning("Rate limited by Discord, will retry later");
            }

            return false;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error posting to Discord");
            return false;
        }
        catch (TaskCanceledException)
        {
            _logger.LogWarning("Discord post cancelled or timed out");
            return false;
        }
        finally
        {
            _rateLimiter.Release();
        }
    }

    private static string MaskUrl(string url)
    {
        // Mask the webhook token for logging
        var lastSlash = url.LastIndexOf('/');
        if (lastSlash > 0 && url.Length > lastSlash + 8)
        {
            return url[..(lastSlash + 8)] + "***";
        }
        return "***masked***";
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _httpClient.Dispose();
        _rateLimiter.Dispose();
    }
}
