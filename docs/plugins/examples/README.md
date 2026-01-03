# Example Plugins

This directory contains complete example plugins demonstrating each plugin type. Use these as templates for building your own plugins.

## Available Examples

| Example | Type | Description |
|---------|------|-------------|
| [DPS Calculator](dps-calculator.md) | Data Analysis | Calculates DPS, burst damage, and efficiency metrics |
| [HTML Report Exporter](html-exporter.md) | Export Format | Exports combat data to styled HTML reports |
| [Damage Timeline](damage-timeline.md) | UI Component | Interactive chart showing damage over time |
| [Critical Hit Parser](critical-hit-parser.md) | Custom Parser | Parses critical hit and special damage messages |

## Quick Start

Each example includes:
- Complete source code
- Plugin manifest (`plugin.json`)
- Build instructions
- Explanation of key concepts

## Project Structure

A typical plugin project looks like this:

```
MyPlugin/
├── MyPlugin.csproj          # Project file
├── plugin.json              # Plugin manifest
├── MyPlugin.cs              # Main plugin class
├── Models/                  # Optional: data models
│   └── CustomModels.cs
├── Services/                # Optional: helper services
│   └── Calculator.cs
└── Views/                   # Optional: UI components (for UI plugins)
    └── MyView.axaml
```

## Building Examples

### Prerequisites

- .NET 9.0 SDK
- Reference to CamelotCombatReporter.PluginSdk

### Build Steps

```bash
# Clone or copy the example
cd examples/dps-calculator

# Restore and build
dotnet restore
dotnet build -c Release

# Output is in bin/Release/net9.0/
```

### Installing

Copy the entire output folder to your plugins directory:

- **Windows**: `%APPDATA%\CamelotCombatReporter\plugins\installed\{plugin-id}\`
- **macOS**: `~/Library/Application Support/CamelotCombatReporter/plugins/installed/{plugin-id}/`
- **Linux**: `~/.config/CamelotCombatReporter/plugins/installed/{plugin-id}/`
