namespace DiscordIntegration.Models;

/// <summary>
/// Configuration settings for Discord integration.
/// </summary>
/// <param name="WebhookUrl">Discord webhook URL for posting.</param>
/// <param name="Enabled">Whether Discord posting is enabled.</param>
/// <param name="EnabledTriggers">Which events trigger automatic posts.</param>
/// <param name="MinKillsToPost">Minimum kills for MajorKill trigger.</param>
/// <param name="MinRpToPost">Minimum realm points earned to include in posts.</param>
/// <param name="IncludeCharacterName">Show character name in posts.</param>
/// <param name="IncludeRealmRank">Show realm rank in posts.</param>
/// <param name="Privacy">Privacy level for enemy information.</param>
public record DiscordSettings(
    string WebhookUrl,
    bool Enabled,
    PostTrigger EnabledTriggers,
    int MinKillsToPost,
    int MinRpToPost,
    bool IncludeCharacterName,
    bool IncludeRealmRank,
    PrivacyLevel Privacy)
{
    /// <summary>
    /// Default settings with Discord disabled.
    /// </summary>
    public static DiscordSettings Default => new(
        WebhookUrl: string.Empty,
        Enabled: false,
        EnabledTriggers: PostTrigger.SessionEnd | PostTrigger.PersonalBest,
        MinKillsToPost: 5,
        MinRpToPost: 1000,
        IncludeCharacterName: true,
        IncludeRealmRank: true,
        Privacy: PrivacyLevel.Full);
}
