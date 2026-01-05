using System.Text;
using System.Text.Json;
using CamelotCombatReporter.Core.Alerts.Models;

namespace CamelotCombatReporter.Core.Alerts.Notifications;

/// <summary>
/// Notification that posts to a Discord webhook when triggered.
/// </summary>
public class DiscordWebhookNotification : INotification
{
    private readonly HttpClient _httpClient;

    /// <inheritdoc />
    public string NotificationType => "DiscordWebhook";

    /// <inheritdoc />
    public bool IsEnabled { get; set; } = false;

    /// <summary>
    /// Discord webhook URL.
    /// </summary>
    public string WebhookUrl { get; set; } = string.Empty;

    /// <summary>
    /// Whether to include timestamp in the embed.
    /// </summary>
    public bool IncludeTimestamp { get; set; } = true;

    /// <summary>
    /// Bot username to display.
    /// </summary>
    public string BotUsername { get; set; } = "Camelot Combat Reporter";

    /// <summary>
    /// Custom colors by priority level.
    /// </summary>
    public Dictionary<AlertPriority, int> PriorityColors { get; set; } = new()
    {
        { AlertPriority.Critical, 0xFF0000 },  // Red
        { AlertPriority.High, 0xFF6600 },       // Orange
        { AlertPriority.Medium, 0xFFFF00 },     // Yellow
        { AlertPriority.Low, 0x00FF00 }         // Green
    };

    /// <summary>
    /// Creates a new Discord webhook notification.
    /// </summary>
    /// <param name="httpClient">HTTP client for making requests.</param>
    public DiscordWebhookNotification(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    /// <inheritdoc />
    public async Task ExecuteAsync(AlertContext context, CancellationToken cancellationToken = default)
    {
        if (!IsEnabled || string.IsNullOrWhiteSpace(WebhookUrl))
            return;

        try
        {
            var embed = CreateEmbed(context);
            var json = JsonSerializer.Serialize(embed);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            await _httpClient.PostAsync(WebhookUrl, content, cancellationToken);
        }
        catch
        {
            // Log but don't throw - webhook failures shouldn't crash the app
        }
    }

    private object CreateEmbed(AlertContext context)
    {
        var color = PriorityColors.TryGetValue(context.Rule.Priority, out var c) ? c : 0x808080;

        var fields = new List<object>
        {
            new { name = "Priority", value = context.Rule.Priority.ToString(), inline = true },
            new { name = "Time", value = context.Timestamp.ToString("HH:mm:ss"), inline = true }
        };

        // Add condition data as fields
        foreach (var kvp in context.ConditionData.Take(3))
        {
            fields.Add(new
            {
                name = kvp.Key,
                value = kvp.Value?.ToString() ?? "N/A",
                inline = true
            });
        }

        return new
        {
            username = BotUsername,
            embeds = new[]
            {
                new
                {
                    title = $"Alert: {context.Rule.Name}",
                    description = context.TriggerReason,
                    color,
                    timestamp = IncludeTimestamp ? DateTime.UtcNow.ToString("o") : null,
                    fields,
                    footer = new
                    {
                        text = "Camelot Combat Reporter"
                    }
                }
            }
        };
    }
}
