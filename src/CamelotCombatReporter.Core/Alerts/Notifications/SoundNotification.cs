using CamelotCombatReporter.Core.Alerts.Models;
using CamelotCombatReporter.Core.Alerts.Services;

namespace CamelotCombatReporter.Core.Alerts.Notifications;

/// <summary>
/// Notification that plays a sound when triggered.
/// </summary>
public class SoundNotification : INotification
{
    private readonly IAudioService _audioService;

    /// <inheritdoc />
    public string NotificationType => "Sound";

    /// <inheritdoc />
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Default sound file to play.
    /// </summary>
    public string SoundFile { get; set; } = "alert.wav";

    /// <summary>
    /// Volume level for this notification (0.0 to 1.0).
    /// </summary>
    public float Volume { get; set; } = 0.8f;

    /// <summary>
    /// Whether to use priority-based sound selection.
    /// </summary>
    public bool UsePrioritySounds { get; set; } = true;

    /// <summary>
    /// Custom sound files by priority level.
    /// </summary>
    public Dictionary<AlertPriority, string> PrioritySounds { get; set; } = new()
    {
        { AlertPriority.Critical, "critical_alert.wav" },
        { AlertPriority.High, "high_alert.wav" },
        { AlertPriority.Medium, "medium_alert.wav" },
        { AlertPriority.Low, "alert.wav" }
    };

    /// <summary>
    /// Creates a new sound notification.
    /// </summary>
    /// <param name="audioService">Audio service for playback.</param>
    public SoundNotification(IAudioService audioService)
    {
        _audioService = audioService ?? throw new ArgumentNullException(nameof(audioService));
    }

    /// <inheritdoc />
    public async Task ExecuteAsync(AlertContext context, CancellationToken cancellationToken = default)
    {
        if (!IsEnabled)
            return;

        var soundPath = GetSoundPathForPriority(context.Rule.Priority);
        await _audioService.PlaySoundAsync(soundPath, Volume, cancellationToken);
    }

    private string GetSoundPathForPriority(AlertPriority priority)
    {
        if (!UsePrioritySounds)
            return SoundFile;

        return PrioritySounds.TryGetValue(priority, out var sound)
            ? sound
            : SoundFile;
    }
}
