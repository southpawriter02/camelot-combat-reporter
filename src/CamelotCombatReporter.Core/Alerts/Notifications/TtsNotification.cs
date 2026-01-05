using CamelotCombatReporter.Core.Alerts.Models;
using CamelotCombatReporter.Core.Alerts.Services;

namespace CamelotCombatReporter.Core.Alerts.Notifications;

/// <summary>
/// Notification that speaks text using text-to-speech when triggered.
/// </summary>
public class TtsNotification : INotification
{
    private readonly ITtsService _ttsService;

    /// <inheritdoc />
    public string NotificationType => "TTS";

    /// <inheritdoc />
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Message template to speak. Supports placeholders:
    /// {RuleName}, {TriggerReason}, {Priority}
    /// </summary>
    public string MessageTemplate { get; set; } = "{RuleName}: {TriggerReason}";

    /// <summary>
    /// Speech rate multiplier (0.5 to 2.0).
    /// </summary>
    public float SpeechRate { get; set; } = 1.0f;

    /// <summary>
    /// Whether to prefix with "Alert:" for context.
    /// </summary>
    public bool PrefixWithAlert { get; set; } = true;

    /// <summary>
    /// Creates a new TTS notification.
    /// </summary>
    /// <param name="ttsService">TTS service for speech synthesis.</param>
    public TtsNotification(ITtsService ttsService)
    {
        _ttsService = ttsService ?? throw new ArgumentNullException(nameof(ttsService));
    }

    /// <inheritdoc />
    public async Task ExecuteAsync(AlertContext context, CancellationToken cancellationToken = default)
    {
        if (!IsEnabled || !_ttsService.IsAvailable)
            return;

        var message = FormatMessage(context);
        await _ttsService.SpeakAsync(message, SpeechRate, cancellationToken);
    }

    private string FormatMessage(AlertContext context)
    {
        var message = MessageTemplate
            .Replace("{RuleName}", context.Rule.Name)
            .Replace("{TriggerReason}", context.TriggerReason)
            .Replace("{Priority}", context.Rule.Priority.ToString());

        if (PrefixWithAlert)
            message = $"Alert: {message}";

        return message;
    }
}
