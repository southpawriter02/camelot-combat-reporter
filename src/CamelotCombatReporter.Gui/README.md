# Camelot Combat Reporter GUI

A cross-platform graphical user interface for the Camelot Combat Reporter application built with Avalonia UI.

## Features

- **File Selection**: Easy-to-use file picker for selecting combat log files
- **Combatant Name**: Specify the combatant name to track (defaults to "You")
- **Real-time Analysis**: Analyze combat logs with a single click
- **Visual Statistics Display**: View key combat metrics including:
  - Log Duration
  - Total Damage Dealt
  - Damage Per Second (DPS)
  - Average Damage
  - Median Damage
  - Combat Styles Used
  - Spells Cast

## Running the Application

### Prerequisites

- .NET 9.0 SDK or later

### Launch the GUI

```bash
dotnet run --project src/CamelotCombatReporter.Gui
```

Or build and run the executable:

```bash
dotnet build
cd src/CamelotCombatReporter.Gui/bin/Debug/net9.0
./CamelotCombatReporter.Gui
```

## How to Use

1. **Select Log File**: Click the "Select Log File" button to open a file picker and choose your combat log file
2. **Enter Combatant Name**: Type the name of the combatant you want to analyze (default is "You")
3. **Analyze**: Click the "Analyze" button to process the log and view statistics
4. **View Results**: The combat statistics will appear in a colorful, easy-to-read format

## Platform Support

The GUI runs on:
- Windows
- macOS
- Linux

## Technology

Built with:
- [Avalonia UI](https://avaloniaui.net/) - Cross-platform XAML-based UI framework
- [CommunityToolkit.Mvvm](https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/) - MVVM pattern helpers
- .NET 9.0
