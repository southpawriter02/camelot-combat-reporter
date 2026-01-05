using System.Text.Json;
using CamelotCombatReporter.Core.Alerts.Conditions;
using CamelotCombatReporter.Core.Alerts.Models;
using CamelotCombatReporter.Core.Alerts.Notifications;

namespace CamelotCombatReporter.Core.Alerts;

/// <summary>
/// Service for loading and saving alert configurations.
/// </summary>
public class AlertConfigurationService : IAlertConfigurationService
{
    private readonly string _configPath;

    /// <summary>
    /// Creates a new alert configuration service.
    /// </summary>
    /// <param name="configPath">Path to the configuration file.</param>
    public AlertConfigurationService(string configPath)
    {
        _configPath = configPath ?? throw new ArgumentNullException(nameof(configPath));
    }

    /// <inheritdoc />
    public async Task<AlertConfiguration> LoadAsync()
    {
        if (!File.Exists(_configPath))
            return GetDefault();

        try
        {
            var json = await File.ReadAllTextAsync(_configPath);
            var config = JsonSerializer.Deserialize<AlertConfiguration>(json, GetJsonOptions());
            return config ?? GetDefault();
        }
        catch
        {
            return GetDefault();
        }
    }

    /// <inheritdoc />
    public async Task SaveAsync(AlertConfiguration config)
    {
        var directory = Path.GetDirectoryName(_configPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        var json = JsonSerializer.Serialize(config, GetJsonOptions());
        await File.WriteAllTextAsync(_configPath, json);
    }

    /// <inheritdoc />
    public AlertConfiguration GetDefault()
    {
        return new AlertConfiguration(
            Rules: new List<AlertRuleDto>(),
            GlobalMute: false,
            MasterVolume: 1.0f,
            TtsEnabled: true);
    }

    /// <inheritdoc />
    public IReadOnlyList<AlertRule> DeserializeRules(AlertConfiguration config)
    {
        var rules = new List<AlertRule>();

        foreach (var dto in config.Rules)
        {
            var conditions = dto.Conditions
                .Select(DeserializeCondition)
                .Where(c => c != null)
                .Cast<IAlertCondition>()
                .ToList();

            var notifications = dto.Notifications
                .Select(DeserializeNotification)
                .Where(n => n != null)
                .Cast<INotification>()
                .ToList();

            var rule = new AlertRule(
                Id: dto.Id,
                Name: dto.Name,
                Description: dto.Description,
                Priority: dto.Priority,
                Logic: dto.Logic,
                Conditions: conditions,
                Notifications: notifications,
                Cooldown: TimeSpan.FromSeconds(dto.CooldownSeconds),
                State: dto.State,
                MaxTriggersPerSession: dto.MaxTriggersPerSession,
                RequiresCombat: dto.RequiresCombat);

            rules.Add(rule);
        }

        return rules;
    }

    /// <inheritdoc />
    public AlertConfiguration SerializeRules(
        IEnumerable<AlertRule> rules,
        bool globalMute,
        float masterVolume,
        bool ttsEnabled)
    {
        var dtos = rules.Select(r => new AlertRuleDto(
            Id: r.Id,
            Name: r.Name,
            Description: r.Description,
            Priority: r.Priority,
            Logic: r.Logic,
            Conditions: r.Conditions.Select(SerializeCondition).ToList(),
            Notifications: r.Notifications.Select(SerializeNotification).ToList(),
            CooldownSeconds: (int)r.Cooldown.TotalSeconds,
            State: r.State,
            MaxTriggersPerSession: r.MaxTriggersPerSession,
            RequiresCombat: r.RequiresCombat
        )).ToList();

        return new AlertConfiguration(dtos, globalMute, masterVolume, ttsEnabled);
    }

    private IAlertCondition? DeserializeCondition(ConditionDto dto)
    {
        return dto.Type switch
        {
            "HealthBelow" => new HealthBelowCondition(
                dto.Parameters.TryGetValue("ThresholdPercent", out var t)
                    ? Convert.ToDouble(t)
                    : 30),

            "DamageInWindow" => new DamageInWindowCondition(
                dto.Parameters.TryGetValue("DamageThreshold", out var d)
                    ? Convert.ToInt32(d)
                    : 500,
                dto.Parameters.TryGetValue("WindowSeconds", out var w)
                    ? TimeSpan.FromSeconds(Convert.ToDouble(w))
                    : TimeSpan.FromSeconds(3)),

            "KillStreak" => new KillStreakCondition(
                dto.Parameters.TryGetValue("StreakThreshold", out var k)
                    ? Convert.ToInt32(k)
                    : 3),

            "EnemyClass" => new EnemyClassCondition(
                dto.Parameters.TryGetValue("Classes", out var c) && c is JsonElement je
                    ? je.EnumerateArray().Select(e => e.GetString() ?? "").ToList()
                    : new List<string>()),

            "AbilityUsed" => new AbilityUsedCondition(
                dto.Parameters.TryGetValue("AbilityName", out var a)
                    ? a?.ToString() ?? ""
                    : "",
                dto.Parameters.TryGetValue("ByEnemy", out var te) && te is JsonElement bee
                    && bee.GetBoolean()),

            "DebuffApplied" => new DebuffAppliedCondition(
                dto.Parameters.TryGetValue("DebuffNames", out var dn) && dn is JsonElement de
                    ? de.EnumerateArray().Select(e => e.GetString() ?? "").ToList()
                    : new List<string>(),
                dto.Parameters.TryGetValue("MatchAny", out var ma) && ma is JsonElement mae
                    && mae.GetBoolean()),

            _ => null
        };
    }

    private ConditionDto SerializeCondition(IAlertCondition condition)
    {
        var parameters = new Dictionary<string, object>();

        switch (condition)
        {
            case HealthBelowCondition hbc:
                parameters["ThresholdPercent"] = hbc.ThresholdPercent;
                break;
            case DamageInWindowCondition dwc:
                parameters["DamageThreshold"] = dwc.DamageThreshold;
                parameters["WindowSeconds"] = dwc.Window.TotalSeconds;
                break;
            case KillStreakCondition ksc:
                parameters["StreakThreshold"] = ksc.StreakThreshold;
                break;
            case EnemyClassCondition ecc:
                parameters["Classes"] = ecc.TargetClasses.ToList();
                break;
            case AbilityUsedCondition auc:
                parameters["AbilityName"] = auc.AbilityName;
                parameters["ByEnemy"] = auc.ByEnemy;
                break;
            case DebuffAppliedCondition dac:
                parameters["DebuffNames"] = dac.DebuffNames.ToList();
                parameters["MatchAny"] = dac.MatchAny;
                break;
        }

        return new ConditionDto(condition.ConditionType, parameters);
    }

    private INotification? DeserializeNotification(NotificationDto dto)
    {
        // Note: In a real implementation, these would be fully configured
        // For now, we create placeholder instances
        return dto.Type switch
        {
            "Sound" => new SoundNotification(null!)
            {
                IsEnabled = dto.IsEnabled,
                SoundFile = dto.Settings.TryGetValue("SoundFile", out var sf)
                    ? sf?.ToString() ?? ""
                    : ""
            },
            "ScreenFlash" => new ScreenFlashNotification()
            {
                IsEnabled = dto.IsEnabled
            },
            "Tts" => new TtsNotification(null!)
            {
                IsEnabled = dto.IsEnabled,
                MessageTemplate = dto.Settings.TryGetValue("MessageTemplate", out var mt)
                    ? mt?.ToString() ?? "{RuleName} triggered"
                    : "{RuleName} triggered"
            },
            "DiscordWebhook" => new DiscordWebhookNotification(new HttpClient())
            {
                IsEnabled = dto.IsEnabled,
                WebhookUrl = dto.Settings.TryGetValue("WebhookUrl", out var wu)
                    ? wu?.ToString() ?? ""
                    : ""
            },
            _ => null
        };
    }

    private NotificationDto SerializeNotification(INotification notification)
    {
        var settings = new Dictionary<string, object>();

        switch (notification)
        {
            case SoundNotification sn:
                settings["SoundFile"] = sn.SoundFile;
                break;
            case TtsNotification tn:
                settings["MessageTemplate"] = tn.MessageTemplate;
                break;
            case DiscordWebhookNotification dwn:
                settings["WebhookUrl"] = dwn.WebhookUrl;
                break;
        }

        return new NotificationDto(notification.NotificationType, notification.IsEnabled, settings);
    }

    private static JsonSerializerOptions GetJsonOptions() => new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };
}
