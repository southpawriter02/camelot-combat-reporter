# Damage Breakdown Chart Plugin

Interactive visualization for exploring damage breakdown by type, ability, and target.

## Features

- **Hierarchical Data** - Drill down from damage types to abilities to targets
- **Pie Chart Breakdown** - Visual percentage representation
- **Bar Chart Treemap** - Horizontal bars for comparison
- **Color-Coded Types** - DAoC damage types with distinct colors
- **Statistics Panel** - Details for selected segments

## Installation

```bash
cd plugins/DamageBreakdownChart
dotnet build -c Release
cp -r bin/Release/net9.0/* ../installed/DamageBreakdownChart/
```

## Usage

1. Load a combat log in the main application
2. Navigate to the "Damage Breakdown" or "Damage Treemap" tab
3. Click segments to drill down
4. Use "Back" button to navigate up

## Hierarchy Levels

1. **Damage Type** - Slash, Heat, Cold, etc.
2. **Ability Category** - Combat Style, Spell, DoT, Proc, Pet
3. **Source** - Specific ability or weapon
4. **Target** - Individual targets

## Color Scheme

| Damage Type | Color |
|-------------|-------|
| Slash | Red |
| Crush | Purple |
| Thrust | Blue |
| Heat | Orange |
| Cold | Cyan |
| Matter | Brown |
| Body | Green |
| Spirit | Violet |
| Energy | Yellow |

## Technical Details

### Dependencies
- LiveChartsCore.SkiaSharpView.Avalonia
- Avalonia 11.2.3

### Permissions
- `CombatDataAccess` - Read damage events
- `UIModification` - Add chart tabs

## Version History

### v1.0.0
- Initial release
- Pie and bar chart visualizations
- Hierarchical drill-down
- DAoC damage type colors

## License

MIT License - See main project for details.
