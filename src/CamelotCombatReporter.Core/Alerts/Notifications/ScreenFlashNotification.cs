using CamelotCombatReporter.Core.Alerts.Models;

namespace CamelotCombatReporter.Core.Alerts.Notifications;

/// <summary>
/// Event args for screen flash requests.
/// </summary>
public record ScreenFlashEventArgs(string Color, int DurationMs, int FlashCount);

/// <summary>
/// Notification that flashes the screen when triggered.
/// </summary>
public class ScreenFlashNotification : INotification
{
    /// <inheritdoc />
    public string NotificationType => "ScreenFlash";

    /// <inheritdoc />
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Default flash color in hex format (e.g., "#FF0000").
    /// </summary>
    public string FlashColor { get; set; } = "#FF0000";

    /// <summary>
    /// Duration of each flash in milliseconds.
    /// </summary>
    public int FlashDurationMs { get; set; } = 200;

    /// <summary>
    /// Number of times to flash.
    /// </summary>
    public int FlashCount { get; set; } = 2;

    /// <summary>
    /// Whether to use priority-based color selection.
    /// </summary>
    public bool UsePriorityColors { get; set; } = true;

    /// <summary>
    /// Custom colors by priority level.
    /// </summary>
    public Dictionary<AlertPriority, string> PriorityColors { get; set; } = new()
    {
        { AlertPriority.Critical, "#FF0000" },  // Red
        { AlertPriority.High, "#FF6600" },       // Orange
        { AlertPriority.Medium, "#FFFF00" },     // Yellow
        { AlertPriority.Low, "#00FF00" }         // Green
    };

    /// <summary>
    /// Event raised when a screen flash is requested.
    /// </summary>
    public event EventHandler<ScreenFlashEventArgs>? FlashRequested;

    /// <inheritdoc />
    public Task ExecuteAsync(AlertContext context, CancellationToken cancellationToken = default)
    {
        if (!IsEnabled)
            return Task.CompletedTask;

        var color = GetColorForPriority(context.Rule.Priority);
        var flashCount = GetFlashCountForPriority(context.Rule.Priority);

        FlashRequested?.Invoke(this, new ScreenFlashEventArgs(color, FlashDurationMs, flashCount));

        return Task.CompletedTask;
    }

    private string GetColorForPriority(AlertPriority priority)
    {
        if (!UsePriorityColors)
            return FlashColor;

        return PriorityColors.TryGetValue(priority, out var color)
            ? color
            : FlashColor;
    }

    private int GetFlashCountForPriority(AlertPriority priority)
    {
        // Higher priority alerts flash more times
        return priority switch
        {
            AlertPriority.Critical => FlashCount + 2,
            AlertPriority.High => FlashCount + 1,
            _ => FlashCount
        };
    }
}
