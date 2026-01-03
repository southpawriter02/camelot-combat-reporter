# 11. Combat Replay System

## Status: 📋 Planned

**Prerequisites:**
- ✅ Log parsing infrastructure
- ✅ Timeline view
- ⬚ Event sequencing and playback engine

---

## Description

Replay combat encounters in a visual timeline format, allowing players to review fights frame-by-frame, analyze decision points, and identify mistakes. The replay system reconstructs combat from log data and presents it as an interactive visualization.

## Functionality

### Core Features

* **Combat Reconstruction:**
  * Parse combat logs into discrete events
  * Order events by timestamp with millisecond precision
  * Interpolate gaps in event data
  * Handle simultaneous events

* **Playback Controls:**
  * Play/pause/stop functionality
  * Variable playback speed (0.25x to 4x)
  * Step forward/backward by event
  * Jump to specific timestamps
  * Loop specific segments

* **Visual Timeline:**
  * Horizontal scrolling timeline
  * Event markers with icons
  * Health/resource bars over time
  * Damage/healing number overlays
  * Ability cooldown indicators

### Replay Modes

| Mode | Description | Use Case |
|------|-------------|----------|
| **Full Combat** | Complete fight from start to finish | Post-fight review |
| **Death Analysis** | Focus on moments before death | Learn from mistakes |
| **Kill Highlights** | Jump between kill moments | Celebrate victories |
| **Burst Windows** | Highlight high-damage periods | Optimize rotations |
| **Heal Checks** | Focus on healing events | Healer improvement |

### Visualization Elements

* **Combat Stage:**
  * Abstract representation of combat space
  * Participant positioning (inferred from targeting)
  * Effect animations for major abilities
  * Health bar overlays

* **Event Feed:**
  * Scrolling event log synchronized with playback
  * Color-coded by event type
  * Expandable event details
  * Search within replay

* **Statistics Panel:**
  * Live-updating DPS/HPS during playback
  * Cumulative damage tracking
  * Ability usage counts
  * Resource expenditure

### Analysis Tools

* **Annotation System:**
  * Add notes at specific timestamps
  * Mark important moments
  * Create highlight clips
  * Share annotations with group

* **Comparison View:**
  * Side-by-side replay of two encounters
  * Overlay multiple attempts
  * Compare personal vs. group performance
  * Before/after improvement tracking

* **Slow Motion Analysis:**
  * Frame-by-frame advancement
  * Pause on specific events
  * Analyze ability chains
  * Study enemy patterns

### Export Features

* **Video Export:**
  * Export replay as video file
  * Configurable resolution and framerate
  * Include statistics overlay
  * Add custom watermark

* **Clip Creation:**
  * Select time range for clip
  * Export as standalone replay file
  * Share via link (with hosting)
  * Embed in external tools

## Requirements

* **Event Sequencing:** Precise timestamp ordering
* **Rendering Engine:** Canvas or WebGL for smooth playback
* **Storage:** Efficient replay data format

## Limitations

* Log data may have timestamp gaps
* Position data must be inferred
* Large fights generate large replay files
* Real-time capture not supported (post-combat only)

## Dependencies

* **01-log-parsing.md:** Core event parsing
* **04-timeline-view.md:** Timeline visualization foundation
* **02-real-time-parsing.md:** Near-real-time capture

## Implementation Phases

### Phase 1: Replay Engine Core
- [ ] Create ReplayCombat model
- [ ] Implement event sequencing logic
- [ ] Build playback state machine
- [ ] Add basic play/pause controls

### Phase 2: Visual Timeline
- [ ] Design timeline component
- [ ] Implement event markers
- [ ] Add health bar visualization
- [ ] Create damage number overlays

### Phase 3: Advanced Playback
- [ ] Variable speed controls
- [ ] Event stepping
- [ ] Segment looping
- [ ] Bookmark system

### Phase 4: Analysis Features
- [ ] Annotation system
- [ ] Comparison view
- [ ] Statistics integration
- [ ] Export functionality

## Technical Notes

### Data Structures

```csharp
public record ReplayCombat(
    Guid Id,
    string Name,
    DateTime StartTime,
    DateTime EndTime,
    IReadOnlyList<ReplayParticipant> Participants,
    IReadOnlyList<ReplayEvent> Events,
    ReplayMetadata Metadata
);

public record ReplayParticipant(
    string Name,
    ParticipantType Type,
    CharacterClass? Class,
    int MaxHealth,
    Realm? Realm
);

public record ReplayEvent(
    long TickOffset,         // Milliseconds from start
    ReplayEventType Type,
    string Source,
    string? Target,
    int? Value,
    string? AbilityName,
    Dictionary<string, object>? ExtraData
);

public record ReplayMetadata(
    TimeSpan Duration,
    int TotalEvents,
    int ParticipantCount,
    string? Location,
    ReplayCombatType CombatType
);

public enum ReplayEventType
{
    DamageDealt,
    DamageTaken,
    HealingDone,
    HealingReceived,
    AbilityUsed,
    BuffApplied,
    BuffRemoved,
    Death,
    Resurrection,
    CrowdControl
}
```

### Playback State Machine

```csharp
public class ReplayController
{
    private ReplayCombat _replay;
    private int _currentEventIndex;
    private double _playbackSpeed = 1.0;
    private PlaybackState _state = PlaybackState.Stopped;
    private long _currentTick;

    public event Action<ReplayEvent>? OnEventReached;
    public event Action<long>? OnTickChanged;
    public event Action<PlaybackState>? OnStateChanged;

    public void Play()
    {
        _state = PlaybackState.Playing;
        OnStateChanged?.Invoke(_state);
        StartPlaybackLoop();
    }

    public void Pause() => _state = PlaybackState.Paused;
    public void Stop() { _state = PlaybackState.Stopped; _currentTick = 0; }

    public void SetSpeed(double speed) => _playbackSpeed = Math.Clamp(speed, 0.25, 4.0);

    public void SeekToTick(long tick)
    {
        _currentTick = tick;
        _currentEventIndex = FindEventIndexForTick(tick);
        OnTickChanged?.Invoke(_currentTick);
    }

    public void StepForward()
    {
        if (_currentEventIndex < _replay.Events.Count - 1)
        {
            _currentEventIndex++;
            var evt = _replay.Events[_currentEventIndex];
            _currentTick = evt.TickOffset;
            OnEventReached?.Invoke(evt);
            OnTickChanged?.Invoke(_currentTick);
        }
    }

    public void StepBackward()
    {
        if (_currentEventIndex > 0)
        {
            _currentEventIndex--;
            var evt = _replay.Events[_currentEventIndex];
            _currentTick = evt.TickOffset;
            OnEventReached?.Invoke(evt);
            OnTickChanged?.Invoke(_currentTick);
        }
    }

    private async void StartPlaybackLoop()
    {
        var stopwatch = Stopwatch.StartNew();
        var lastFrameTime = stopwatch.ElapsedMilliseconds;

        while (_state == PlaybackState.Playing)
        {
            var currentFrameTime = stopwatch.ElapsedMilliseconds;
            var deltaTime = (currentFrameTime - lastFrameTime) * _playbackSpeed;
            lastFrameTime = currentFrameTime;

            _currentTick += (long)deltaTime;

            // Process events up to current tick
            while (_currentEventIndex < _replay.Events.Count &&
                   _replay.Events[_currentEventIndex].TickOffset <= _currentTick)
            {
                OnEventReached?.Invoke(_replay.Events[_currentEventIndex]);
                _currentEventIndex++;
            }

            OnTickChanged?.Invoke(_currentTick);

            // Check for end of replay
            if (_currentEventIndex >= _replay.Events.Count)
            {
                _state = PlaybackState.Stopped;
                OnStateChanged?.Invoke(_state);
                break;
            }

            await Task.Delay(16); // ~60 FPS
        }
    }

    private int FindEventIndexForTick(long tick)
    {
        return _replay.Events
            .TakeWhile(e => e.TickOffset <= tick)
            .Count();
    }
}

public enum PlaybackState
{
    Stopped,
    Playing,
    Paused
}
```

### Timeline Rendering

```csharp
public class TimelineRenderer
{
    private readonly ReplayCombat _replay;
    private readonly ICanvas _canvas;

    public void Render(long currentTick, double viewportStart, double viewportEnd)
    {
        // Draw timeline background
        DrawTimelineBackground();

        // Draw time markers
        DrawTimeMarkers(viewportStart, viewportEnd);

        // Draw event markers
        foreach (var evt in GetVisibleEvents(viewportStart, viewportEnd))
        {
            DrawEventMarker(evt);
        }

        // Draw playhead
        DrawPlayhead(currentTick);

        // Draw participant health bars
        foreach (var participant in _replay.Participants)
        {
            DrawHealthBar(participant, currentTick);
        }
    }

    private IEnumerable<ReplayEvent> GetVisibleEvents(double start, double end)
    {
        return _replay.Events
            .Where(e => e.TickOffset >= start && e.TickOffset <= end);
    }
}
```

### Replay File Format

```json
{
  "version": "1.0",
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "name": "Keep Siege - Snowdonia",
  "startTime": "2025-01-02T14:30:00Z",
  "duration": 180000,
  "participants": [
    {
      "name": "PlayerOne",
      "type": "Player",
      "class": "Armsman",
      "maxHealth": 2500,
      "realm": "Albion"
    }
  ],
  "events": [
    {
      "tick": 0,
      "type": "AbilityUsed",
      "source": "PlayerOne",
      "abilityName": "Slam"
    },
    {
      "tick": 150,
      "type": "DamageDealt",
      "source": "PlayerOne",
      "target": "Enemy",
      "value": 450,
      "abilityName": "Slam"
    }
  ],
  "annotations": [
    {
      "tick": 45000,
      "author": "PlayerOne",
      "text": "Should have used defensive cooldown here"
    }
  ]
}
```
