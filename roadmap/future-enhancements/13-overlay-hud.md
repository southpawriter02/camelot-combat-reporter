# 13. In-Game Overlay HUD

## Status: 📋 Planned

**Prerequisites:**
- ✅ Real-time log parsing
- ⬚ Overlay rendering framework
- ⬚ Window injection/composition

---

## Description

Display real-time combat statistics as a transparent overlay on top of the game window. The HUD provides instant feedback on DPS, healing, and other metrics without requiring alt-tabbing out of the game.

## Functionality

### Core Features

* **Transparent Overlay:**
  * Always-on-top window over game
  * Configurable transparency level
  * Click-through mode for unobstructed gameplay
  * Auto-hide when not in combat

* **Real-Time Metrics:**
  * Live DPS counter
  * Healing per second (HPS)
  * Damage taken
  * Combat duration timer
  * Kill/death counter

* **Customizable Layout:**
  * Drag-and-drop widget positioning
  * Resize individual elements
  * Multiple preset layouts
  * Save/load layout profiles

### HUD Elements

| Widget | Content | Customization |
|--------|---------|---------------|
| **DPS Meter** | Current and average DPS | Size, color, history length |
| **HPS Meter** | Healing output | Size, color, threshold alerts |
| **Damage Taken** | Incoming damage rate | Size, color, source breakdown |
| **Combat Timer** | Duration since combat start | Format (mm:ss or seconds) |
| **K/D Counter** | Kills and deaths this session | Reset button, session vs. all-time |
| **Target Info** | Last target damage dealt | Auto-clear timeout |
| **Mini Timeline** | Compact event ticker | Event count, scroll speed |
| **Alert Box** | Important notifications | Position, duration, sound |

### Display Modes

* **Combat Mode:**
  * Full HUD visible during combat
  * Real-time updating metrics
  * Alert notifications active
  * Maximum information density

* **Idle Mode:**
  * Minimal display when not fighting
  * Session summary visible
  * Reduced screen clutter
  * Optional complete hide

* **Compact Mode:**
  * Single-line essential stats
  * Minimal screen footprint
  * Expandable on hover
  * Ideal for streaming

* **Streamer Mode:**
  * OBS-friendly capture
  * Green screen background option
  * Custom branding area
  * Chat integration display

### Overlay Positioning

```
┌────────────────────────────────────────────┐
│  Game Window                               │
│                                            │
│  ┌──────────┐              ┌─────────────┐ │
│  │ DPS: 450 │              │ K: 5  D: 1  │ │
│  │ HPS: 120 │              │ Duration: 3m│ │
│  └──────────┘              └─────────────┘ │
│                                            │
│                                            │
│  ┌───────────────────────────────────────┐ │
│  │ [!] Low health warning                │ │
│  └───────────────────────────────────────┘ │
│                                            │
│                  ┌─────────────────────┐   │
│                  │ Combat Log Ticker   │   │
│                  │ > 450 dmg to Enemy  │   │
│                  │ > Healed for 200    │   │
│                  └─────────────────────┘   │
└────────────────────────────────────────────┘
```

### Configuration Options

* **Visual Settings:**
  * Transparency (0-100%)
  * Background color/opacity
  * Font size and family
  * Color themes (realm-based, custom)
  * Border styles

* **Behavior Settings:**
  * Auto-hide delay
  * Combat detection sensitivity
  * Click-through toggle
  * Global hotkey for show/hide
  * Lock position toggle

* **Performance Settings:**
  * Update frequency (10-60 FPS)
  * Hardware acceleration toggle
  * Memory usage limits
  * Low-power mode

## Requirements

* **Overlay Framework:** GameOverlay.NET or custom DirectX/OpenGL
* **Window Management:** Win32 API for positioning
* **Game Detection:** Process monitoring for game window
* **Real-Time Data:** Sub-second log parsing

## Limitations

* Windows-only initially (overlay APIs)
* Anti-cheat software may flag overlay
* Performance impact on lower-end systems
* Some games block overlays
* macOS/Linux require different approach

## Dependencies

* **01-log-parsing.md:** Core parsing engine
* **02-real-time-parsing.md:** Live log streaming
* **17-combat-alerts.md:** Alert system

## Implementation Phases

### Phase 1: Basic Overlay
- [ ] Set up overlay window framework
- [ ] Implement game window detection
- [ ] Create basic DPS widget
- [ ] Add transparency controls

### Phase 2: Widget System
- [ ] Design widget architecture
- [ ] Implement all core widgets
- [ ] Add drag-and-drop positioning
- [ ] Create layout save/load

### Phase 3: Advanced Features
- [ ] Combat detection auto-show/hide
- [ ] Click-through mode
- [ ] Global hotkeys
- [ ] Multiple profiles

### Phase 4: Polish
- [ ] Performance optimization
- [ ] Streamer mode
- [ ] Theme system
- [ ] OBS integration

## Technical Notes

### Overlay Window Setup

```csharp
public class GameOverlay : IDisposable
{
    private readonly GraphicsWindow _window;
    private readonly Graphics _graphics;
    private readonly Process _gameProcess;
    private readonly List<IWidget> _widgets = new();

    public GameOverlay(string gameName)
    {
        _gameProcess = FindGameProcess(gameName);

        var gfx = new Graphics
        {
            MeasureFPS = true,
            PerPrimitiveAntiAliasing = true,
            TextAntiAliasing = true
        };

        _window = new GraphicsWindow(gfx)
        {
            IsTopmost = true,
            IsVisible = true,
            FPS = 60,
            X = 0,
            Y = 0,
            Width = 1920,
            Height = 1080
        };

        _window.SetupGraphics += OnSetupGraphics;
        _window.DrawGraphics += OnDrawGraphics;
    }

    public void AddWidget(IWidget widget) => _widgets.Add(widget);

    private void OnDrawGraphics(object sender, DrawGraphicsEventArgs e)
    {
        e.Graphics.ClearScene();

        foreach (var widget in _widgets.Where(w => w.IsVisible))
        {
            widget.Render(e.Graphics);
        }
    }

    public void UpdatePosition()
    {
        if (!GetWindowRect(_gameProcess.MainWindowHandle, out var rect))
            return;

        _window.X = rect.Left;
        _window.Y = rect.Top;
        _window.Width = rect.Right - rect.Left;
        _window.Height = rect.Bottom - rect.Top;
    }
}
```

### Widget Interface

```csharp
public interface IWidget
{
    string Id { get; }
    bool IsVisible { get; set; }
    Rectangle Bounds { get; set; }
    void Update(CombatState state);
    void Render(Graphics graphics);
    void OnClick(Point position);
    void OnDrag(Point delta);
}

public abstract class WidgetBase : IWidget
{
    public string Id { get; protected set; }
    public bool IsVisible { get; set; } = true;
    public Rectangle Bounds { get; set; }
    public bool IsLocked { get; set; }

    protected Font TitleFont { get; set; }
    protected Font ValueFont { get; set; }
    protected SolidBrush BackgroundBrush { get; set; }
    protected SolidBrush TextBrush { get; set; }

    public abstract void Update(CombatState state);
    public abstract void Render(Graphics graphics);

    public virtual void OnDrag(Point delta)
    {
        if (IsLocked) return;
        Bounds = new Rectangle(
            Bounds.X + delta.X,
            Bounds.Y + delta.Y,
            Bounds.Width,
            Bounds.Height
        );
    }
}
```

### DPS Widget Implementation

```csharp
public class DpsWidget : WidgetBase
{
    private double _currentDps;
    private double _averageDps;
    private readonly Queue<double> _dpsHistory = new();
    private const int HistoryLength = 30;

    public DpsWidget()
    {
        Id = "dps_meter";
        Bounds = new Rectangle(10, 10, 150, 80);
    }

    public override void Update(CombatState state)
    {
        _currentDps = state.CurrentDps;

        _dpsHistory.Enqueue(_currentDps);
        while (_dpsHistory.Count > HistoryLength)
            _dpsHistory.Dequeue();

        _averageDps = _dpsHistory.Average();
    }

    public override void Render(Graphics graphics)
    {
        // Background
        graphics.FillRoundedRectangle(
            BackgroundBrush,
            Bounds.X, Bounds.Y,
            Bounds.Width, Bounds.Height,
            5
        );

        // Title
        graphics.DrawText(
            TitleFont, TextBrush,
            Bounds.X + 10, Bounds.Y + 5,
            "DPS"
        );

        // Current DPS (large)
        graphics.DrawText(
            ValueFont, TextBrush,
            Bounds.X + 10, Bounds.Y + 25,
            $"{_currentDps:F0}"
        );

        // Average DPS (smaller)
        graphics.DrawText(
            TitleFont, TextBrush,
            Bounds.X + 10, Bounds.Y + 55,
            $"Avg: {_averageDps:F0}"
        );

        // Mini graph
        DrawDpsGraph(graphics);
    }

    private void DrawDpsGraph(Graphics graphics)
    {
        if (_dpsHistory.Count < 2) return;

        var graphX = Bounds.X + 90;
        var graphY = Bounds.Y + 25;
        var graphW = 50;
        var graphH = 40;

        var max = _dpsHistory.Max();
        if (max == 0) max = 1;

        var points = _dpsHistory
            .Select((dps, i) => new Point(
                graphX + (i * graphW / HistoryLength),
                graphY + graphH - (int)(dps / max * graphH)
            ))
            .ToArray();

        for (int i = 1; i < points.Length; i++)
        {
            graphics.DrawLine(
                TextBrush,
                points[i - 1].X, points[i - 1].Y,
                points[i].X, points[i].Y,
                1
            );
        }
    }
}
```

### Layout Configuration

```json
{
  "name": "Default RvR Layout",
  "version": "1.0",
  "widgets": [
    {
      "id": "dps_meter",
      "visible": true,
      "position": { "x": 10, "y": 10 },
      "size": { "width": 150, "height": 80 },
      "settings": {
        "showGraph": true,
        "historyLength": 30
      }
    },
    {
      "id": "hps_meter",
      "visible": true,
      "position": { "x": 10, "y": 100 },
      "size": { "width": 150, "height": 60 }
    },
    {
      "id": "kd_counter",
      "visible": true,
      "position": { "x": -160, "y": 10 },
      "anchor": "top-right"
    },
    {
      "id": "combat_timer",
      "visible": true,
      "position": { "x": 0, "y": 10 },
      "anchor": "top-center"
    },
    {
      "id": "alert_box",
      "visible": true,
      "position": { "x": 0, "y": -100 },
      "anchor": "bottom-center"
    }
  ],
  "global": {
    "transparency": 0.8,
    "fontSize": 14,
    "colorScheme": "realm_albion",
    "autoHide": true,
    "autoHideDelay": 5000
  }
}
```

### Game Detection

```csharp
public class GameDetector
{
    private static readonly string[] SupportedGames =
    {
        "game.dll",      // Live DAoC
        "camelot",       // Classic client
    };

    public Process? FindGameProcess()
    {
        return Process.GetProcesses()
            .FirstOrDefault(p => SupportedGames
                .Any(name => p.ProcessName.Contains(name,
                    StringComparison.OrdinalIgnoreCase)));
    }

    public bool IsGameFocused(Process gameProcess)
    {
        var foregroundWindow = GetForegroundWindow();
        return foregroundWindow == gameProcess.MainWindowHandle;
    }

    public Rectangle GetGameBounds(Process gameProcess)
    {
        GetWindowRect(gameProcess.MainWindowHandle, out var rect);
        return new Rectangle(
            rect.Left, rect.Top,
            rect.Right - rect.Left,
            rect.Bottom - rect.Top
        );
    }
}
```
