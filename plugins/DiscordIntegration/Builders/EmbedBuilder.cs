using CamelotCombatReporter.Core.Models;
using DiscordIntegration.Models;

namespace DiscordIntegration.Builders;

/// <summary>
/// Builds Discord embeds for various combat events.
/// </summary>
/// <remarks>
/// <para>
/// Creates rich embeds with realm-specific colors:
/// <list type="bullet">
///   <item>Albion: Blue (#2196F3)</item>
///   <item>Midgard: Red (#F44336)</item>
///   <item>Hibernia: Green (#4CAF50)</item>
/// </list>
/// </para>
/// </remarks>
public class EmbedBuilder
{
    private readonly DiscordSettings _settings;

    public EmbedBuilder(DiscordSettings settings)
    {
        _settings = settings;
    }

    /// <summary>
    /// Builds a session summary embed.
    /// </summary>
    public DiscordEmbed BuildSessionSummary(
        CombatStatistics stats,
        string? characterName,
        Realm? realm,
        TimeSpan duration,
        int realmPoints = 0)
    {
        var embed = new DiscordEmbed
        {
            Title = "⚔️ Combat Session Complete",
            Color = GetRealmColor(realm),
            Timestamp = DateTime.UtcNow,
            Fields = []
        };

        if (_settings.IncludeCharacterName && !string.IsNullOrEmpty(characterName))
        {
            embed.Author = new EmbedAuthor
            {
                Name = characterName,
                IconUrl = GetRealmIconUrl(realm)
            };
        }

        // Duration
        embed.Fields.Add(new EmbedField("Duration", FormatDuration(duration), true));

        // DPS
        embed.Fields.Add(new EmbedField("DPS", $"{stats.Dps:F0}", true));

        // Total damage
        embed.Fields.Add(new EmbedField("Total Damage", $"{stats.TotalDamage:N0}", true));

        // Kills and deaths
        if (stats.Kills > 0 || stats.Deaths > 0)
        {
            embed.Fields.Add(new EmbedField("K/D", $"{stats.Kills}/{stats.Deaths}", true));
        }

        // Realm points
        if (_settings.IncludeRealmRank && realmPoints > 0)
        {
            embed.Fields.Add(new EmbedField("RP Earned", $"{realmPoints:N0}", true));
        }

        embed.Footer = new EmbedFooter
        {
            Text = "Camelot Combat Reporter"
        };

        return embed;
    }

    /// <summary>
    /// Builds a personal best achievement embed.
    /// </summary>
    public DiscordEmbed BuildPersonalBest(
        string metric,
        double newValue,
        double previousBest,
        string? characterName = null)
    {
        var improvement = newValue - previousBest;
        var percentImprove = previousBest > 0 ? (improvement / previousBest) * 100 : 100;

        var embed = new DiscordEmbed
        {
            Title = "🏆 New Personal Best!",
            Description = $"**{metric}**: {newValue:N0} (+{improvement:N0}, {percentImprove:F1}%)",
            Color = 0xFFD700, // Gold
            Timestamp = DateTime.UtcNow,
            Footer = new EmbedFooter
            {
                Text = $"Previous best: {previousBest:N0}"
            }
        };

        if (_settings.IncludeCharacterName && !string.IsNullOrEmpty(characterName))
        {
            embed.Author = new EmbedAuthor { Name = characterName };
        }

        return embed;
    }

    /// <summary>
    /// Builds a milestone achievement embed.
    /// </summary>
    public DiscordEmbed BuildMilestone(
        string milestoneType,
        string description,
        string? characterName = null,
        Realm? realm = null)
    {
        var embed = new DiscordEmbed
        {
            Title = $"🎉 {milestoneType}",
            Description = description,
            Color = GetRealmColor(realm) > 0 ? GetRealmColor(realm) : 0x9B59B6, // Purple default
            Timestamp = DateTime.UtcNow,
            Footer = new EmbedFooter { Text = "Camelot Combat Reporter" }
        };

        if (_settings.IncludeCharacterName && !string.IsNullOrEmpty(characterName))
        {
            embed.Author = new EmbedAuthor
            {
                Name = characterName,
                IconUrl = GetRealmIconUrl(realm)
            };
        }

        return embed;
    }

    /// <summary>
    /// Builds a major kill notification embed.
    /// </summary>
    public DiscordEmbed BuildMajorKill(
        int killCount,
        int? deathCount = null,
        string? characterName = null,
        Realm? realm = null)
    {
        var kdRatio = deathCount > 0 ? (double)killCount / deathCount : killCount;

        var embed = new DiscordEmbed
        {
            Title = "💀 Major Kill Streak!",
            Description = $"**{killCount}** kills this session",
            Color = 0xE74C3C, // Red
            Timestamp = DateTime.UtcNow,
            Fields =
            [
                new EmbedField("Kills", $"{killCount}", true),
                new EmbedField("Deaths", $"{deathCount ?? 0}", true),
                new EmbedField("K/D Ratio", $"{kdRatio:F2}", true)
            ],
            Footer = new EmbedFooter { Text = "Camelot Combat Reporter" }
        };

        if (_settings.IncludeCharacterName && !string.IsNullOrEmpty(characterName))
        {
            embed.Author = new EmbedAuthor
            {
                Name = characterName,
                IconUrl = GetRealmIconUrl(realm)
            };
        }

        return embed;
    }

    #region Helpers

    private static int GetRealmColor(Realm? realm) => realm switch
    {
        Realm.Albion => 0x2196F3,   // Blue
        Realm.Midgard => 0xF44336,  // Red
        Realm.Hibernia => 0x4CAF50, // Green
        _ => 0x757575               // Gray
    };

    private static string? GetRealmIconUrl(Realm? realm) => realm switch
    {
        // These would be actual icon URLs in production
        Realm.Albion => null,
        Realm.Midgard => null,
        Realm.Hibernia => null,
        _ => null
    };

    private static string FormatDuration(TimeSpan duration)
    {
        if (duration.TotalHours >= 1)
        {
            return $"{(int)duration.TotalHours}h {duration.Minutes}m";
        }
        return $"{duration.Minutes}m {duration.Seconds}s";
    }

    #endregion
}
