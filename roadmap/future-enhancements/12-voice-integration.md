# 12. Voice Chat Integration

## Status: 📋 Planned

**Prerequisites:**
- ✅ Log parsing infrastructure
- ⬚ Voice platform API access
- ⬚ Audio processing libraries

---

## Description

Integrate with voice communication platforms (Discord, TeamSpeak, Mumble) to correlate voice activity with combat events. Provide audio cues, text-to-speech announcements, and voice-activated commands for hands-free operation during combat.

## Functionality

### Core Features

* **Voice Platform Integration:**
  * Discord Rich Presence and bot integration
  * TeamSpeak plugin support
  * Mumble positional audio plugin
  * Generic WebSocket bridge for other platforms

* **Audio Announcements:**
  * Text-to-speech for combat alerts
  * Custom sound effects for events
  * Configurable announcement priorities
  * Volume and voice customization

* **Voice Commands:**
  * Start/stop parsing via voice
  * Request statistics verbally
  * Trigger exports with voice
  * Quick lookups ("What's my DPS?")

### Platform Integration

| Platform | Features | Implementation |
|----------|----------|----------------|
| **Discord** | Rich Presence, Bot, Webhooks | Discord SDK, Bot API |
| **TeamSpeak** | Plugin, Overlay, Whispers | TS5 Plugin SDK |
| **Mumble** | Positional Audio, Overlay | Mumble Plugin |
| **Ventrilo** | Basic status updates | Status file bridge |
| **Generic** | WebSocket bridge | Custom protocol |

### Audio Announcements

* **Combat Events:**
  * "Combat started" / "Combat ended"
  * "Kill: [target name]"
  * "Death: You were killed by [enemy]"
  * "Low health warning"
  * "Healing received from [healer]"

* **Statistics Callouts:**
  * Periodic DPS updates during combat
  * Session summary at combat end
  * Personal best notifications
  * Milestone achievements

* **Alert Priorities:**
  * Critical: Deaths, low health, emergency
  * High: Kills, significant damage
  * Medium: Healing events, buffs
  * Low: General combat updates

### Discord Integration

* **Rich Presence:**
  * Show current combat status
  * Display DPS/HPS in real-time
  * Character and realm information
  * Session duration

* **Bot Commands:**
  * `/stats` - Request current statistics
  * `/session` - Session summary
  * `/leaderboard` - Personal bests
  * `/compare` - Compare with group

* **Webhooks:**
  * Post combat summaries to channel
  * Share notable achievements
  * Alert for realm events (keep captures)
  * Scheduled session reports

### Voice Commands

| Command | Action |
|---------|--------|
| "Start parsing" | Begin log monitoring |
| "Stop parsing" | End log monitoring |
| "What's my DPS?" | Announce current DPS |
| "Session stats" | Full session summary |
| "Save session" | Save current session |
| "Compare to last" | Compare with previous session |

### Text-to-Speech Configuration

* **Voice Selection:**
  * System TTS voices
  * Custom voice packs
  * Speed and pitch adjustment
  * Language support

* **Announcement Filtering:**
  * Enable/disable by event type
  * Minimum damage threshold
  * Cooldown between announcements
  * Combat-only or always active

## Requirements

* **Voice Platform SDKs:** Discord, TeamSpeak, Mumble
* **Audio Libraries:** NAudio, Azure Speech, or similar
* **Speech Recognition:** For voice commands (optional)
* **Permissions:** Microphone access, bot tokens

## Limitations

* Requires API keys/bot tokens for some features
* Voice recognition accuracy varies
* TTS quality depends on system voices
* Some platforms restrict third-party integration

## Dependencies

* **01-log-parsing.md:** Core event parsing
* **02-real-time-parsing.md:** Live event streaming
* **17-combat-alerts.md:** Alert system foundation

## Implementation Phases

### Phase 1: Audio Announcements
- [ ] Integrate TTS library (NAudio/System.Speech)
- [ ] Create announcement event system
- [ ] Add basic combat event announcements
- [ ] Implement volume and priority controls

### Phase 2: Discord Integration
- [ ] Implement Discord Rich Presence
- [ ] Create Discord bot framework
- [ ] Add webhook posting
- [ ] Build bot command handlers

### Phase 3: Voice Commands
- [ ] Integrate speech recognition library
- [ ] Define command grammar
- [ ] Implement command handlers
- [ ] Add wake word detection

### Phase 4: Extended Platform Support
- [ ] TeamSpeak plugin development
- [ ] Mumble integration
- [ ] Generic WebSocket bridge
- [ ] Platform auto-detection

## Technical Notes

### Text-to-Speech Implementation

```csharp
public interface ICombatAnnouncer
{
    Task AnnounceAsync(string message, AnnouncementPriority priority);
    void SetVolume(float volume);
    void SetVoice(string voiceId);
    void EnableAnnouncement(AnnouncementType type, bool enabled);
}

public class TextToSpeechAnnouncer : ICombatAnnouncer
{
    private readonly SpeechSynthesizer _synthesizer;
    private readonly Dictionary<AnnouncementType, bool> _enabledTypes;
    private readonly Queue<QueuedAnnouncement> _queue;
    private DateTime _lastAnnouncement;
    private readonly TimeSpan _cooldown = TimeSpan.FromSeconds(1);

    public async Task AnnounceAsync(string message, AnnouncementPriority priority)
    {
        if (DateTime.Now - _lastAnnouncement < _cooldown &&
            priority < AnnouncementPriority.Critical)
            return;

        _queue.Enqueue(new QueuedAnnouncement(message, priority));
        await ProcessQueueAsync();
    }

    private async Task ProcessQueueAsync()
    {
        while (_queue.TryDequeue(out var announcement))
        {
            await _synthesizer.SpeakTextAsync(announcement.Message);
            _lastAnnouncement = DateTime.Now;
        }
    }
}

public enum AnnouncementPriority
{
    Low,
    Medium,
    High,
    Critical
}

public enum AnnouncementType
{
    CombatStart,
    CombatEnd,
    Kill,
    Death,
    LowHealth,
    HealingReceived,
    DpsUpdate,
    SessionSummary
}
```

### Discord Integration

```csharp
public class DiscordIntegration : IDisposable
{
    private readonly DiscordRpcClient _rpcClient;
    private readonly DiscordSocketClient? _botClient;
    private readonly string? _webhookUrl;

    public async Task UpdatePresenceAsync(CombatSession session)
    {
        _rpcClient.SetPresence(new RichPresence
        {
            Details = $"DPS: {session.Statistics.Dps:F0}",
            State = $"{session.Character.Realm} - {session.Character.Class}",
            Timestamps = new Timestamps(session.StartTime),
            Assets = new Assets
            {
                LargeImageKey = session.Character.Realm.ToString().ToLower(),
                LargeImageText = session.Character.Class.ToString()
            }
        });
    }

    public async Task PostWebhookAsync(string content, Embed? embed = null)
    {
        if (string.IsNullOrEmpty(_webhookUrl)) return;

        var payload = new
        {
            content,
            embeds = embed != null ? new[] { embed } : null
        };

        using var client = new HttpClient();
        await client.PostAsJsonAsync(_webhookUrl, payload);
    }

    public async Task PostCombatSummaryAsync(CombatStatistics stats)
    {
        var embed = new EmbedBuilder()
            .WithTitle("Combat Summary")
            .WithColor(Color.Blue)
            .AddField("Duration", $"{stats.DurationMinutes:F1} min", true)
            .AddField("DPS", $"{stats.Dps:F0}", true)
            .AddField("Total Damage", $"{stats.TotalDamage:N0}", true)
            .WithFooter("Camelot Combat Reporter")
            .WithCurrentTimestamp()
            .Build();

        await PostWebhookAsync("", embed);
    }
}
```

### Voice Command Recognition

```csharp
public class VoiceCommandHandler : IDisposable
{
    private readonly SpeechRecognitionEngine _recognizer;
    private readonly Dictionary<string, Func<Task>> _commands;

    public VoiceCommandHandler()
    {
        _recognizer = new SpeechRecognitionEngine();

        var grammar = new GrammarBuilder();
        grammar.Append(new Choices(
            "start parsing",
            "stop parsing",
            "what's my DPS",
            "session stats",
            "save session",
            "compare to last"
        ));

        _recognizer.LoadGrammar(new Grammar(grammar));
        _recognizer.SpeechRecognized += OnSpeechRecognized;

        _commands = new Dictionary<string, Func<Task>>
        {
            ["start parsing"] = () => StartParsingAsync(),
            ["stop parsing"] = () => StopParsingAsync(),
            ["what's my DPS"] = () => AnnounceDpsAsync(),
            ["session stats"] = () => AnnounceSessionStatsAsync(),
            ["save session"] = () => SaveSessionAsync(),
            ["compare to last"] = () => CompareSessionsAsync()
        };
    }

    public void StartListening()
    {
        _recognizer.SetInputToDefaultAudioDevice();
        _recognizer.RecognizeAsync(RecognizeMode.Multiple);
    }

    private async void OnSpeechRecognized(object sender, SpeechRecognizedEventArgs e)
    {
        if (e.Result.Confidence < 0.7f) return;

        var command = e.Result.Text.ToLower();
        if (_commands.TryGetValue(command, out var handler))
        {
            await handler();
        }
    }
}
```

### Platform Bridge Protocol

```json
{
  "protocol": "ccr-voice-bridge",
  "version": "1.0",
  "messages": {
    "status_update": {
      "type": "status",
      "inCombat": true,
      "dps": 450.5,
      "character": {
        "name": "PlayerOne",
        "realm": "Albion",
        "class": "Armsman"
      }
    },
    "announcement": {
      "type": "announce",
      "message": "Kill: EnemyPlayer",
      "priority": "high",
      "tts": true
    },
    "command_response": {
      "type": "response",
      "command": "dps_query",
      "result": "Your current DPS is 450"
    }
  }
}
```
