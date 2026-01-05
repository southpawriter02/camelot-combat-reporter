using CamelotCombatReporter.Core.Alerts.Models;

namespace CamelotCombatReporter.Core.Alerts.Notifications;

/// <summary>
/// Interface for alert notifications that execute when an alert triggers.
/// </summary>
public interface INotification
{
    /// <summary>
    /// Unique type identifier for this notification (e.g., "Sound", "ScreenFlash").
    /// </summary>
    string NotificationType { get; }

    /// <summary>
    /// Whether this notification is currently enabled.
    /// </summary>
    bool IsEnabled { get; set; }

    /// <summary>
    /// Executes the notification.
    /// </summary>
    /// <param name="context">Context information about the triggered alert.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task ExecuteAsync(AlertContext context, CancellationToken cancellationToken = default);
}
