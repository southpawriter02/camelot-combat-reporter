# Discord Bot Integration Plugin

## Plugin Type: Data Analysis + External Integration

## Overview

Post combat summaries, statistics, and achievements to Discord channels via webhooks or a companion bot. Share your best fights, track guild-wide performance, and celebrate milestones with your community.

## Problem Statement

Players want to share combat achievements with their guild/community:
- Post session summaries to a Discord channel
- Celebrate kills and personal bests
- Track realm point progress publicly
- Create guild leaderboards
- Automate reporting

## Features

### Webhook Integration
- Configure Discord webhook URL
- Customizable message templates
- Rich embeds with statistics
- Automatic triggers (session end, milestone)

### Message Types
- Session summary posts
- Personal best announcements
- Kill notifications (configurable threshold)
- Milestone celebrations (realm rank up)
- Daily/weekly reports

### Bot Commands (Optional)
- `/stats` - Request current session stats
- `/leaderboard` - Guild damage/RP leaderboard
- `/compare @user` - Compare with another player
- `/session start/stop` - Announce session status

### Privacy Controls
- Opt-in for all features
- Anonymization options
- Exclude specific data types
- Personal vs. guild channels

## Technical Specification

### Plugin Manifest

```json
{
  "id": "discord-bot-integration",
  "name": "Discord Bot Integration",
  "version": "1.0.0",
  "author": "CCR Community",
  "description": "Posts combat summaries and achievements to Discord",
  "type": "DataAnalysis",
  "entryPoint": {
    "assembly": "DiscordIntegration.dll",
    "typeName": "DiscordIntegration.DiscordPlugin"
  },
  "compatibility": {
    "minAppVersion": "1.0.0"
  },
  "permissions": [
    "CombatDataAccess",
    "NetworkAccess",
    "SettingsRead",
    "SettingsWrite",
    "UINotifications"
  ],
  "resources": {
    "maxMemoryMb": 32,
    "maxCpuTimeSeconds": 15
  },
  "network": {
    "allowedDomains": [
      "discord.com",
      "discordapp.com"
    ]
  }
}
```

### Configuration

```csharp
public record DiscordSettings(
    string WebhookUrl,
    bool Enabled,
    PostTrigger EnabledTriggers,
    MessageTemplate SessionSummaryTemplate,
    MessageTemplate PersonalBestTemplate,
    MessageTemplate MilestoneTemplate,
    int MinKillsToPost,
    int MinRpToPost,
    bool IncludeCharacterName,
    bool IncludeRealmRank,
    PrivacyLevel Privacy
);

[Flags]
public enum PostTrigger
{
    None = 0,
    SessionEnd = 1,
    PersonalBest = 2,
    MajorKill = 4,
    Milestone = 8,
    Manual = 16
}

public enum PrivacyLevel
{
    Full,           // Show all details
    Partial,        // Hide player names
    StatsOnly       // Only aggregate numbers
}
```

### Discord Embed Builder

```csharp
public class DiscordEmbedBuilder
{
    public DiscordEmbed BuildSessionSummary(
        CombatStatistics stats,
        SessionInfo session,
        DiscordSettings settings)
    {
        var embed = new DiscordEmbed
        {
            Title = $"⚔️ Combat Session Complete",
            Color = GetRealmColor(session.Character?.Realm),
            Timestamp = DateTime.UtcNow
        };

        if (settings.IncludeCharacterName)
        {
            embed.Author = new EmbedAuthor
            {
                Name = session.Character?.Name ?? "Unknown",
                IconUrl = GetRealmIcon(session.Character?.Realm)
            };
        }

        embed.Fields = new List<EmbedField>
        {
            new("Duration", FormatDuration(stats.DurationMinutes), true),
            new("DPS", $"{stats.Dps:F0}", true),
            new("Total Damage", $"{stats.TotalDamage:N0}", true),
            new("Combat Styles", $"{stats.CombatStylesCount}", true),
            new("Spells Cast", $"{stats.SpellsCastCount}", true)
        };

        if (settings.IncludeRealmRank && session.RealmPoints > 0)
        {
            embed.Fields.Add(new("RP Earned", $"{session.RealmPoints:N0}", true));
        }

        embed.Footer = new EmbedFooter
        {
            Text = "Camelot Combat Reporter",
            IconUrl = "https://example.com/ccr-icon.png"
        };

        return embed;
    }

    public DiscordEmbed BuildPersonalBest(
        string metric,
        double value,
        double previousBest,
        SessionInfo session)
    {
        var improvement = value - previousBest;
        var percentImprove = (improvement / previousBest) * 100;

        return new DiscordEmbed
        {
            Title = "🏆 New Personal Best!",
            Description = $"**{metric}**: {value:N0} (+{improvement:N0}, {percentImprove:F1}%)",
            Color = 0xFFD700, // Gold
            Thumbnail = new EmbedThumbnail
            {
                Url = "https://example.com/trophy.png"
            },
            Footer = new EmbedFooter
            {
                Text = $"Previous best: {previousBest:N0}"
            }
        };
    }

    private int GetRealmColor(Realm? realm) => realm switch
    {
        Realm.Albion => 0x2196F3,   // Blue
        Realm.Midgard => 0xF44336,  // Red
        Realm.Hibernia => 0x4CAF50, // Green
        _ => 0x757575               // Gray
    };
}
```

### Implementation Outline

```csharp
public class DiscordPlugin : DataAnalysisPluginBase
{
    private DiscordSettings _settings = null!;
    private readonly HttpClient _httpClient = new();
    private CombatStatistics? _lastSessionStats;

    public override async Task InitializeAsync(
        IPluginContext context,
        CancellationToken ct = default)
    {
        var prefs = context.GetService<IPreferencesAccess>();
        if (prefs != null)
        {
            _settings = await prefs.GetAsync<DiscordSettings>("discord-settings", ct)
                ?? new DiscordSettings(
                    WebhookUrl: "",
                    Enabled: false,
                    EnabledTriggers: PostTrigger.SessionEnd,
                    SessionSummaryTemplate: MessageTemplate.Default,
                    PersonalBestTemplate: MessageTemplate.Default,
                    MilestoneTemplate: MessageTemplate.Default,
                    MinKillsToPost: 5,
                    MinRpToPost: 1000,
                    IncludeCharacterName: true,
                    IncludeRealmRank: true,
                    Privacy: PrivacyLevel.Full
                );
        }
    }

    public override Task<AnalysisResult> AnalyzeAsync(
        IReadOnlyList<LogEvent> events,
        CombatStatistics? baseStatistics,
        AnalysisOptions options,
        CancellationToken ct = default)
    {
        _lastSessionStats = baseStatistics;

        // Check for personal bests
        if (_settings.EnabledTriggers.HasFlag(PostTrigger.PersonalBest))
        {
            CheckForPersonalBests(baseStatistics);
        }

        return Task.FromResult(Success(new Dictionary<string, object>
        {
            ["discord-enabled"] = _settings.Enabled,
            ["last-post-status"] = "Ready"
        }));
    }

    public async Task PostSessionSummaryAsync(CancellationToken ct = default)
    {
        if (!_settings.Enabled || string.IsNullOrEmpty(_settings.WebhookUrl))
        {
            LogWarning("Discord posting is disabled or webhook not configured");
            return;
        }

        if (_lastSessionStats == null)
        {
            LogWarning("No session data to post");
            return;
        }

        var embed = new DiscordEmbedBuilder()
            .BuildSessionSummary(_lastSessionStats, GetCurrentSession(), _settings);

        await PostWebhookAsync(embed, ct);
    }

    private async Task PostWebhookAsync(DiscordEmbed embed, CancellationToken ct)
    {
        try
        {
            var payload = new
            {
                embeds = new[] { embed }
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(_settings.WebhookUrl, content, ct);
            response.EnsureSuccessStatusCode();

            LogInfo("Successfully posted to Discord");
        }
        catch (Exception ex)
        {
            LogError("Failed to post to Discord", ex);
        }
    }

    private async void CheckForPersonalBests(CombatStatistics? stats)
    {
        if (stats == null) return;

        var prefs = Context.GetService<IPreferencesAccess>();
        if (prefs == null) return;

        // Check DPS personal best
        var bestDps = await prefs.GetAsync<double>("best-dps");
        if (stats.Dps > bestDps)
        {
            await prefs.SetAsync("best-dps", stats.Dps);

            if (_settings.EnabledTriggers.HasFlag(PostTrigger.PersonalBest))
            {
                var embed = new DiscordEmbedBuilder()
                    .BuildPersonalBest("DPS", stats.Dps, bestDps, GetCurrentSession());
                await PostWebhookAsync(embed, CancellationToken.None);
            }
        }
    }
}
```

### Embed Data Types

```csharp
public class DiscordEmbed
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public int Color { get; set; }
    public DateTime? Timestamp { get; set; }
    public EmbedAuthor? Author { get; set; }
    public EmbedThumbnail? Thumbnail { get; set; }
    public List<EmbedField>? Fields { get; set; }
    public EmbedFooter? Footer { get; set; }
}

public record EmbedAuthor(string Name, string? IconUrl = null, string? Url = null);
public record EmbedThumbnail(string Url);
public record EmbedField(string Name, string Value, bool Inline = false);
public record EmbedFooter(string Text, string? IconUrl = null);
```

## Example Discord Posts

### Session Summary
```
┌──────────────────────────────────────┐
│ ⚔️ Combat Session Complete           │
│ ──────────────────────────────────── │
│ 👤 Warrior1 (Albion)                 │
│                                      │
│ Duration: 45m 23s    DPS: 892        │
│ Total Damage: 2,423,450              │
│ Combat Styles: 156   Spells: 0       │
│ RP Earned: 12,450                    │
│                                      │
│ ────────────────────────────────     │
│ Camelot Combat Reporter • Today 8:45│
└──────────────────────────────────────┘
```

### Personal Best
```
┌──────────────────────────────────────┐
│ 🏆 New Personal Best!                │
│                                      │
│ DPS: 1,245 (+123, +10.9%)           │
│                                      │
│ Previous best: 1,122                 │
└──────────────────────────────────────┘
```

## Settings UI

```
┌─────────────────────────────────────────────────────────────┐
│  Discord Integration Settings                               │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  ☑ Enable Discord Integration                               │
│                                                             │
│  Webhook URL:                                               │
│  [https://discord.com/api/webhooks/...                   ] │
│  [Test Webhook]                                             │
│                                                             │
│  POST TRIGGERS                                              │
│  ☑ Session end summaries                                    │
│  ☑ Personal best achievements                               │
│  ☐ Major kills (min: [10] kills)                           │
│  ☑ Milestones (realm rank up, etc.)                        │
│                                                             │
│  PRIVACY                                                    │
│  ☑ Include character name                                   │
│  ☑ Include realm rank                                       │
│  ☐ Anonymize enemy names                                    │
│                                                             │
│  [Save Settings]  [Reset to Defaults]                       │
└─────────────────────────────────────────────────────────────┘
```

## Dependencies

- Network access (HTTPS to Discord)
- Settings storage
- Combat statistics

## Complexity

**Low-Medium** - Discord webhook API is simple, main complexity is in configuration and user preferences.

## Future Enhancements

- [ ] Full Discord bot with slash commands
- [ ] Guild leaderboard tracking
- [ ] Scheduled reports (daily/weekly)
- [ ] Reaction-based triggers
- [ ] Multiple webhook support (different channels)
- [ ] Custom embed templates with variables
- [ ] Integration with guild management bots
