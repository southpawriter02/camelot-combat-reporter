using CamelotCombatReporter.Core.Models;
using CamelotCombatReporter.Plugins.Abstractions;
using CamelotCombatReporter.Plugins.Permissions;
using CamelotCombatReporter.PluginSdk;
using DiscordIntegration.Builders;
using DiscordIntegration.Models;
using DiscordIntegration.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace DiscordIntegration;

/// <summary>
/// Discord Bot Integration plugin - posts combat stats to Discord via webhooks.
/// </summary>
/// <remarks>
/// <para>
/// Posts session summaries, personal bests, and milestones to a Discord channel
/// using webhook integration. Supports configurable triggers and privacy settings.
/// </para>
/// <para>
/// <strong>Features</strong>:
/// <list type="bullet">
///   <item><description>Post session summaries automatically or manually</description></item>
///   <item><description>Personal best announcements</description></item>
///   <item><description>Milestone celebrations (realm rank ups)</description></item>
///   <item><description>Configurable privacy controls</description></item>
///   <item><description>Rate-limited webhook posting</description></item>
/// </list>
/// </para>
/// </remarks>
public sealed class DiscordIntegrationPlugin : DataAnalysisPluginBase
{
    private DiscordSettings _settings = DiscordSettings.Default;
    private DiscordWebhookService? _webhookService;
    private CombatStatistics? _lastSessionStats;
    private ILogger<DiscordIntegrationPlugin>? _logger;

    // Personal best tracking
    private double _bestDps;
    private int _bestKillStreak;
    private long _bestDamage;

    #region Plugin Metadata

    /// <inheritdoc/>
    public override string Id => "discord-bot-integration";

    /// <inheritdoc/>
    public override string Name => "Discord Bot Integration";

    /// <inheritdoc/>
    public override Version Version => new(1, 0, 0);

    /// <inheritdoc/>
    public override string Author => "CCR Community";

    /// <inheritdoc/>
    public override string Description =>
        "Posts combat summaries and achievements to Discord via webhooks. " +
        "Share your battles with your guild and celebrate milestones.";

    /// <inheritdoc/>
    public override IReadOnlyList<PluginPermission> RequiredPermissions =>
    [
        new PluginPermission("CombatDataAccess", "Read combat statistics"),
        new PluginPermission("NetworkAccess", "POST to Discord webhooks"),
        new PluginPermission("SettingsRead", "Load Discord settings"),
        new PluginPermission("SettingsWrite", "Save Discord settings"),
        new PluginPermission("UINotifications", "Show post status")
    ];

    #endregion

    #region Lifecycle

    /// <inheritdoc/>
    public override async Task InitializeAsync(
        IPluginContext context,
        CancellationToken ct = default)
    {
        await base.InitializeAsync(context, ct).ConfigureAwait(false);

        _logger = context.GetService<ILoggerFactory>()
            ?.CreateLogger<DiscordIntegrationPlugin>()
            ?? NullLogger<DiscordIntegrationPlugin>.Instance;

        _logger.LogDebug("Discord Integration plugin initializing");

        // Load settings
        var prefs = context.GetService<IPreferencesAccess>();
        if (prefs != null)
        {
            _settings = await prefs.GetAsync<DiscordSettings>("discord-settings", ct)
                ?? DiscordSettings.Default;

            // Load personal bests
            _bestDps = await prefs.GetAsync<double>("discord-best-dps", ct);
            _bestKillStreak = await prefs.GetAsync<int>("discord-best-kills", ct);
            _bestDamage = await prefs.GetAsync<long>("discord-best-damage", ct);
        }

        // Initialize webhook service if configured
        if (!string.IsNullOrEmpty(_settings.WebhookUrl))
        {
            _webhookService = new DiscordWebhookService(
                _settings.WebhookUrl,
                context.GetService<ILoggerFactory>()?.CreateLogger<DiscordWebhookService>());
        }

        _logger.LogInformation(
            "Discord Integration initialized. Enabled={Enabled}, Triggers={Triggers}",
            _settings.Enabled,
            _settings.EnabledTriggers);
    }

    /// <inheritdoc/>
    public override async Task OnUnloadAsync(CancellationToken ct = default)
    {
        _logger?.LogDebug("Discord Integration plugin unloading");

        if (_webhookService != null)
        {
            _webhookService.Dispose();
            _webhookService = null;
        }

        await base.OnUnloadAsync(ct).ConfigureAwait(false);
    }

    #endregion

    #region Analysis

    /// <inheritdoc/>
    public override async Task<AnalysisResult> AnalyzeAsync(
        IReadOnlyList<LogEvent> events,
        CombatStatistics? baseStatistics,
        AnalysisOptions options,
        CancellationToken ct = default)
    {
        _lastSessionStats = baseStatistics;

        if (!_settings.Enabled || _webhookService == null)
        {
            return Success(new Dictionary<string, object>
            {
                ["discord-enabled"] = false,
                ["status"] = "Discord integration disabled"
            });
        }

        // Check for personal bests
        if (_settings.EnabledTriggers.HasFlag(PostTrigger.PersonalBest) && baseStatistics != null)
        {
            await CheckPersonalBestsAsync(baseStatistics, ct).ConfigureAwait(false);
        }

        // Check for major kills
        if (_settings.EnabledTriggers.HasFlag(PostTrigger.MajorKill) && baseStatistics != null)
        {
            if (baseStatistics.Kills >= _settings.MinKillsToPost)
            {
                await PostMajorKillAsync(baseStatistics, ct).ConfigureAwait(false);
            }
        }

        return Success(new Dictionary<string, object>
        {
            ["discord-enabled"] = true,
            ["webhook-configured"] = !string.IsNullOrEmpty(_settings.WebhookUrl),
            ["status"] = "Ready"
        });
    }

    #endregion

    #region Discord Posting

    /// <summary>
    /// Posts a session summary to Discord.
    /// </summary>
    public async Task<bool> PostSessionSummaryAsync(
        string? characterName = null,
        Realm? realm = null,
        TimeSpan? duration = null,
        int realmPoints = 0,
        CancellationToken ct = default)
    {
        if (!ValidateWebhook())
            return false;

        if (_lastSessionStats == null)
        {
            _logger?.LogWarning("No session statistics available to post");
            return false;
        }

        var builder = new EmbedBuilder(_settings);
        var embed = builder.BuildSessionSummary(
            _lastSessionStats,
            characterName,
            realm,
            duration ?? TimeSpan.Zero,
            realmPoints);

        var success = await _webhookService!.PostEmbedAsync(embed, ct).ConfigureAwait(false);

        _logger?.Log(
            success ? LogLevel.Information : LogLevel.Warning,
            "Session summary post {Result}",
            success ? "succeeded" : "failed");

        return success;
    }

    /// <summary>
    /// Posts a manual message to Discord.
    /// </summary>
    public async Task<bool> PostManualAsync(
        string title,
        string description,
        CancellationToken ct = default)
    {
        if (!ValidateWebhook())
            return false;

        var embed = new DiscordEmbed
        {
            Title = title,
            Description = description,
            Color = 0x3498DB, // Blue
            Timestamp = DateTime.UtcNow,
            Footer = new EmbedFooter { Text = "Camelot Combat Reporter" }
        };

        return await _webhookService!.PostEmbedAsync(embed, ct).ConfigureAwait(false);
    }

    private async Task CheckPersonalBestsAsync(CombatStatistics stats, CancellationToken ct)
    {
        var prefs = Context.GetService<IPreferencesAccess>();
        var builder = new EmbedBuilder(_settings);

        // Check DPS
        if (stats.Dps > _bestDps && stats.Dps > 0)
        {
            var embed = builder.BuildPersonalBest("DPS", stats.Dps, _bestDps);
            await _webhookService!.PostEmbedAsync(embed, ct).ConfigureAwait(false);

            _bestDps = stats.Dps;
            if (prefs != null)
                await prefs.SetAsync("discord-best-dps", _bestDps, ct).ConfigureAwait(false);

            _logger?.LogInformation("New DPS personal best: {Dps}", stats.Dps);
        }

        // Check total damage
        if (stats.TotalDamage > _bestDamage && stats.TotalDamage > 0)
        {
            var embed = builder.BuildPersonalBest("Total Damage", stats.TotalDamage, _bestDamage);
            await _webhookService!.PostEmbedAsync(embed, ct).ConfigureAwait(false);

            _bestDamage = stats.TotalDamage;
            if (prefs != null)
                await prefs.SetAsync("discord-best-damage", _bestDamage, ct).ConfigureAwait(false);

            _logger?.LogInformation("New damage personal best: {Damage}", stats.TotalDamage);
        }
    }

    private async Task PostMajorKillAsync(CombatStatistics stats, CancellationToken ct)
    {
        var builder = new EmbedBuilder(_settings);
        var embed = builder.BuildMajorKill(stats.Kills, stats.Deaths);

        await _webhookService!.PostEmbedAsync(embed, ct).ConfigureAwait(false);
        _logger?.LogInformation("Posted major kill notification: {Kills} kills", stats.Kills);
    }

    private bool ValidateWebhook()
    {
        if (!_settings.Enabled)
        {
            _logger?.LogDebug("Discord posting disabled in settings");
            return false;
        }

        if (_webhookService == null || string.IsNullOrEmpty(_settings.WebhookUrl))
        {
            _logger?.LogWarning("Webhook not configured");
            return false;
        }

        return true;
    }

    #endregion

    #region Settings

    /// <summary>
    /// Updates the plugin settings.
    /// </summary>
    public async Task UpdateSettingsAsync(DiscordSettings newSettings, CancellationToken ct = default)
    {
        _settings = newSettings;

        // Recreate webhook service if URL changed
        if (_webhookService != null)
        {
            _webhookService.Dispose();
        }

        if (!string.IsNullOrEmpty(newSettings.WebhookUrl))
        {
            _webhookService = new DiscordWebhookService(
                newSettings.WebhookUrl,
                Context.GetService<ILoggerFactory>()?.CreateLogger<DiscordWebhookService>());
        }

        // Persist settings
        var prefs = Context.GetService<IPreferencesAccess>();
        if (prefs != null)
        {
            await prefs.SetAsync("discord-settings", newSettings, ct).ConfigureAwait(false);
        }

        _logger?.LogInformation("Discord settings updated. Enabled={Enabled}", newSettings.Enabled);
    }

    /// <summary>
    /// Gets the current settings.
    /// </summary>
    public DiscordSettings GetSettings() => _settings;

    /// <summary>
    /// Tests the current webhook configuration.
    /// </summary>
    public async Task<bool> TestWebhookAsync(CancellationToken ct = default)
    {
        if (_webhookService == null)
            return false;

        return await _webhookService.TestWebhookAsync(_settings.WebhookUrl, ct).ConfigureAwait(false);
    }

    #endregion
}
