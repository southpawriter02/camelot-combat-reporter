# Damage Breakdown Chart Plugin

## Plugin Type: UI Component

## Overview

Add an interactive sunburst or treemap visualization that breaks down damage by multiple dimensions: damage type, target, ability, and time period. Users can drill down from high-level totals to individual events.

## Problem Statement

The existing statistics show totals, but players want to:
- Understand where their damage comes from at a glance
- See the contribution of each ability visually
- Compare damage across targets
- Identify which abilities need improvement
- Explore data interactively without exporting

## Features

### Visualization Types
- **Sunburst Chart**: Hierarchical radial chart for drilling down
- **Treemap**: Rectangle-based area representation
- **Stacked Bar**: Time-based breakdown
- **Sankey Diagram**: Flow from source to target

### Drill-Down Dimensions
1. **Level 1**: Damage Type (Physical, Magical, etc.)
2. **Level 2**: Ability Category (Combat Style, Spell, Auto-attack)
3. **Level 3**: Specific Ability Name
4. **Level 4**: Target
5. **Level 5**: Individual Events (optional)

### Interactive Features
- Click to zoom into segment
- Hover for detailed tooltip
- Breadcrumb navigation back to parent
- Filter by time range
- Toggle dimensions on/off
- Animate transitions

### Statistics Panel
- Selected segment statistics
- Comparison to total
- DPS contribution
- Hit count and average

## Technical Specification

### Plugin Manifest

```json
{
  "id": "damage-breakdown-chart",
  "name": "Damage Breakdown Chart",
  "version": "1.0.0",
  "author": "CCR Community",
  "description": "Interactive visualization for exploring damage breakdown",
  "type": "UIComponent",
  "entryPoint": {
    "assembly": "DamageBreakdownChart.dll",
    "typeName": "DamageBreakdownChart.DamageChartPlugin"
  },
  "compatibility": {
    "minAppVersion": "1.0.0"
  },
  "permissions": [
    "CombatDataAccess",
    "UIModification"
  ],
  "resources": {
    "maxMemoryMb": 64,
    "maxCpuTimeSeconds": 20
  }
}
```

### UI Components Provided

| Component ID | Location | Description |
|--------------|----------|-------------|
| `damage-sunburst` | MainTab | Sunburst chart tab |
| `damage-treemap` | MainTab | Treemap chart tab |
| `damage-stats-card` | StatisticsCard | Mini breakdown widget |

### Data Structures

```csharp
public record DamageNode(
    string Id,
    string Name,
    DamageNodeType Type,
    int TotalDamage,
    int HitCount,
    double Percentage,
    IReadOnlyList<DamageNode> Children
);

public enum DamageNodeType
{
    Root,
    DamageType,
    AbilityCategory,
    AbilityName,
    Target,
    Event
}

public record ChartDataPoint(
    string Label,
    double Value,
    string Color,
    string ParentId,
    Dictionary<string, object> Metadata
);
```

### Implementation Outline

```csharp
public class DamageChartPlugin : UIPluginBase
{
    public override string Id => "damage-breakdown-chart";
    public override string Name => "Damage Breakdown Chart";
    public override Version Version => new(1, 0, 0);
    public override string Author => "CCR Community";
    public override string Description =>
        "Interactive visualization for exploring damage breakdown";

    public override IReadOnlyCollection<UIComponentDefinition> Components =>
        new[]
        {
            Tab("damage-sunburst", "Damage Sunburst",
                "Interactive sunburst damage breakdown", 50),
            Tab("damage-treemap", "Damage Treemap",
                "Treemap damage breakdown", 51),
            StatisticsCard("damage-mini", "Damage Overview",
                "Quick damage breakdown widget", 20)
        };

    private IUIComponentContext? _context;
    private DamageNode? _rootNode;

    public override Task<object> CreateComponentAsync(
        string componentId,
        IUIComponentContext context,
        CancellationToken ct = default)
    {
        _context = context;

        return componentId switch
        {
            "damage-sunburst" => Task.FromResult<object>(
                new SunburstView(this)),
            "damage-treemap" => Task.FromResult<object>(
                new TreemapView(this)),
            "damage-mini" => Task.FromResult<object>(
                new MiniBreakdownCard(this)),
            _ => throw new ArgumentException($"Unknown component: {componentId}")
        };
    }

    public override Task OnDataChangedAsync(
        IReadOnlyList<LogEvent> events,
        CombatStatistics? statistics,
        CancellationToken ct = default)
    {
        _rootNode = BuildDamageTree(events);
        _context?.RequestRefresh();
        return Task.CompletedTask;
    }

    public DamageNode? GetDamageTree() => _rootNode;

    private DamageNode BuildDamageTree(IReadOnlyList<LogEvent> events)
    {
        var damageEvents = events.OfType<DamageEvent>().ToList();
        var totalDamage = damageEvents.Sum(e => e.DamageAmount);

        // Group by damage type
        var byType = damageEvents
            .GroupBy(e => e.DamageType)
            .Select(g => BuildTypeNode(g.Key, g.ToList(), totalDamage))
            .ToList();

        return new DamageNode(
            "root",
            "All Damage",
            DamageNodeType.Root,
            totalDamage,
            damageEvents.Count,
            100.0,
            byType
        );
    }

    private DamageNode BuildTypeNode(
        string damageType,
        List<DamageEvent> events,
        int totalDamage)
    {
        var typeDamage = events.Sum(e => e.DamageAmount);

        // Group by ability category
        var byCategory = events
            .GroupBy(e => ClassifyAbility(e))
            .Select(g => BuildCategoryNode(g.Key, g.ToList(), totalDamage))
            .ToList();

        return new DamageNode(
            $"type-{damageType}",
            damageType,
            DamageNodeType.DamageType,
            typeDamage,
            events.Count,
            (double)typeDamage / totalDamage * 100,
            byCategory
        );
    }

    private string ClassifyAbility(DamageEvent evt)
    {
        // Classify based on patterns in the event
        // This is a simplified example
        if (evt.Source.Contains("style", StringComparison.OrdinalIgnoreCase))
            return "Combat Style";
        if (evt.Source.Contains("spell", StringComparison.OrdinalIgnoreCase))
            return "Spell";
        return "Auto-attack";
    }
}
```

### Sunburst View (Avalonia)

```csharp
public class SunburstView : UserControl
{
    private readonly DamageChartPlugin _plugin;
    private DamageNode? _currentRoot;
    private Stack<DamageNode> _navigationStack = new();

    public SunburstView(DamageChartPlugin plugin)
    {
        _plugin = plugin;
        InitializeComponent();
    }

    private void RenderChart()
    {
        var data = _plugin.GetDamageTree();
        if (data == null) return;

        _currentRoot = data;

        // Using a custom drawing approach or Avalonia canvas
        var canvas = this.FindControl<Canvas>("ChartCanvas");
        canvas.Children.Clear();

        DrawSunburstRing(canvas, _currentRoot, 0, 360, 0);
    }

    private void DrawSunburstRing(
        Canvas canvas,
        DamageNode node,
        double startAngle,
        double endAngle,
        int level)
    {
        if (level > 3) return; // Max depth

        var ringWidth = 60;
        var innerRadius = 50 + (level * ringWidth);
        var outerRadius = innerRadius + ringWidth;

        var anglePerDamage = (endAngle - startAngle) / node.TotalDamage;
        var currentAngle = startAngle;

        foreach (var child in node.Children)
        {
            var childAngle = child.TotalDamage * anglePerDamage;

            // Draw arc segment
            var arc = CreateArcSegment(
                innerRadius,
                outerRadius,
                currentAngle,
                currentAngle + childAngle,
                GetColorForNode(child)
            );

            arc.PointerPressed += (s, e) => OnSegmentClicked(child);
            arc.PointerEntered += (s, e) => OnSegmentHovered(child);

            canvas.Children.Add(arc);

            // Recurse for children
            if (child.Children.Any())
            {
                DrawSunburstRing(
                    canvas,
                    child,
                    currentAngle,
                    currentAngle + childAngle,
                    level + 1
                );
            }

            currentAngle += childAngle;
        }
    }

    private void OnSegmentClicked(DamageNode node)
    {
        if (!node.Children.Any()) return;

        _navigationStack.Push(_currentRoot!);
        _currentRoot = node;
        RenderChart();
    }

    private void OnNavigateBack()
    {
        if (_navigationStack.TryPop(out var parent))
        {
            _currentRoot = parent;
            RenderChart();
        }
    }
}
```

### Color Schemes

```csharp
public static class ChartColors
{
    public static readonly Dictionary<string, string> DamageTypeColors = new()
    {
        ["Slash"] = "#e53935",
        ["Crush"] = "#8e24aa",
        ["Thrust"] = "#1e88e5",
        ["Heat"] = "#ff5722",
        ["Cold"] = "#00bcd4",
        ["Matter"] = "#795548",
        ["Body"] = "#4caf50",
        ["Spirit"] = "#9c27b0",
        ["Energy"] = "#ffeb3b"
    };

    public static readonly string[] CategoryColors =
    {
        "#2196F3", "#4CAF50", "#FF9800", "#9C27B0",
        "#00BCD4", "#F44336", "#3F51B5", "#FFEB3B"
    };

    public static string GetColor(DamageNode node, int index)
    {
        if (node.Type == DamageNodeType.DamageType &&
            DamageTypeColors.TryGetValue(node.Name, out var color))
        {
            return color;
        }

        return CategoryColors[index % CategoryColors.Length];
    }
}
```

## UI Layout

```
┌─────────────────────────────────────────────────────────────┐
│  Damage Breakdown                              [⊕] [≡] [⟳] │
├─────────────────────────────────────────────────────────────┤
│  Breadcrumb: All Damage > Slash > Combat Styles             │
├──────────────────────────────────┬──────────────────────────┤
│                                  │  Selected: Garrote       │
│                                  │                          │
│         ┌───────────┐            │  Total Damage: 12,450    │
│       ╱             ╲            │  Hits: 47                │
│      ╱   SUNBURST    ╲           │  Average: 265            │
│     │     CHART       │          │  DPS Contribution: 23.4% │
│      ╲               ╱           │                          │
│       ╲             ╱            │  Targets:                │
│         └───────────┘            │  • Goblin (8,200)        │
│                                  │  • Troll (4,250)         │
│                                  │                          │
├──────────────────────────────────┴──────────────────────────┤
│  [Sunburst] [Treemap] [Stacked Bar]     Filter: [All Time▼] │
└─────────────────────────────────────────────────────────────┘
```

## Dependencies

- Avalonia UI framework
- Optional: LiveCharts2 or custom drawing
- Core combat event data

## Complexity

**High** - Custom drawing/rendering of interactive charts requires significant UI development effort.

## Future Enhancements

- [ ] Animation on data updates
- [ ] Comparison mode (two logs side by side)
- [ ] Export chart as image
- [ ] Customizable color schemes
- [ ] Save favorite views/filters
- [ ] Integration with timeline for time-filtered breakdowns
