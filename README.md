# camelot-combat-reporter
A cross-platform tool that allows players to analyze their `chat.log` file and receive a detailed, visualized breakdown of their combat encounters. It parses the log, identifies individual fights, and presents statistics like damage dealt, healing done, crowd control used, and other key metrics for performance analysis.

## Available Interfaces

### GUI Application (NEW!)
A user-friendly graphical interface built with Avalonia UI for Windows, macOS, and Linux.

```bash
dotnet run --project src/CamelotCombatReporter.Gui
```

Features:
- File picker for easy log selection
- Real-time combat statistics
- Colorful, visual display of metrics
- Cross-platform support

See [GUI README](src/CamelotCombatReporter.Gui/README.md) for more details.

### Command-Line Interface
A simple CLI for quick analysis from the terminal.

```bash
dotnet run --project src/CamelotCombatReporter.Cli -- <path_to_log_file> [combatant_name]
```

Example:
```bash
dotnet run --project src/CamelotCombatReporter.Cli -- data/sample.log
```

## Project Structure

- `src/CamelotCombatReporter.Core` - Core parsing and analysis library
- `src/CamelotCombatReporter.Cli` - Command-line interface
- `src/CamelotCombatReporter.Gui` - Graphical user interface (Avalonia UI)
- `tests/CamelotCombatReporter.Core.Tests` - Unit tests

## Requirements

- .NET 9.0 SDK or later

## Building

```bash
dotnet build
```

## Testing

```bash
dotnet test
```

