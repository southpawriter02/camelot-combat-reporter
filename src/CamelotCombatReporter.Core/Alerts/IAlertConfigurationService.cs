using CamelotCombatReporter.Core.Alerts.Models;

namespace CamelotCombatReporter.Core.Alerts;

/// <summary>
/// Service for loading and saving alert configurations.
/// </summary>
public interface IAlertConfigurationService
{
    /// <summary>
    /// Loads the alert configuration from storage.
    /// </summary>
    Task<AlertConfiguration> LoadAsync();

    /// <summary>
    /// Saves the alert configuration to storage.
    /// </summary>
    Task SaveAsync(AlertConfiguration config);

    /// <summary>
    /// Gets the default configuration.
    /// </summary>
    AlertConfiguration GetDefault();

    /// <summary>
    /// Deserializes rules from a configuration.
    /// </summary>
    IReadOnlyList<AlertRule> DeserializeRules(AlertConfiguration config);

    /// <summary>
    /// Serializes rules to a configuration.
    /// </summary>
    AlertConfiguration SerializeRules(
        IEnumerable<AlertRule> rules,
        bool globalMute,
        float masterVolume,
        bool ttsEnabled);
}
