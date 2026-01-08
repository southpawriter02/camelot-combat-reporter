# Discord Bot Integration Plugin

A plugin for [Camelot Combat Reporter](https://github.com/your-repo/camelot-combat-reporter) that posts combat stats and achievements to Discord via webhooks.

## Features

- **Session Summaries** - Auto-post stats when sessions end
- **Personal Bests** - Announce new DPS/damage records
- **Milestone Celebrations** - Realm rank ups and achievements
- **Major Kills** - Configurable kill streak notifications
- **Privacy Controls** - Full/partial/anonymous posting
- **Rate Limiting** - Compliant with Discord API limits

## Installation

```bash
cd plugins/DiscordIntegration
dotnet build -c Release
cp -r bin/Release/net9.0/* ../installed/DiscordIntegration/
```

## Configuration

1. Create a Discord webhook in your server (Server Settings → Integrations → Webhooks)
2. Copy the webhook URL
3. Open plugin settings in Camelot Combat Reporter
4. Paste webhook URL and configure triggers

### Settings

| Setting | Description |
|---------|-------------|
| Webhook URL | Discord webhook endpoint |
| Enabled | Master on/off switch |
| Session End | Post when combat ends |
| Personal Best | Announce new records |
| Major Kill | Post for kill streaks |
| Min Kills | Threshold for kill notifications |
| Include Name | Show character name |
| Include Rank | Show realm rank |
| Privacy | Full/Partial/StatsOnly |

## Discord Embeds

### Session Summary
```
⚔️ Combat Session Complete
─────────────────────────
👤 Warrior1 (Albion)

Duration: 45m 23s    DPS: 892
Total Damage: 2,423,450
K/D: 12/3    RP Earned: 12,450
─────────────────────────
Camelot Combat Reporter
```

### Personal Best
```
🏆 New Personal Best!
DPS: 1,245 (+123, +10.9%)
Previous best: 1,122
```

## Technical Details

### Permissions
- `CombatDataAccess` - Read combat statistics
- `NetworkAccess` - POST to Discord webhooks
- `SettingsRead/Write` - Persist configuration

### Rate Limiting
Discord webhooks allow ~30 requests/minute. The plugin enforces 2-second minimum intervals between posts.

### Webhook Security
Webhook URLs are:
- Stored locally (never transmitted except to Discord)
- Masked in logs
- Validated before saving

## Development

```bash
dotnet build
```

## Version History

### v1.0.0
- Initial release
- Webhook integration
- Session summaries
- Personal best tracking
- Major kill notifications
- Privacy controls

## License

MIT License - See main project for details.
