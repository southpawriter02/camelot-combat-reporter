# Camelot Combat Reporter

[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![.NET 9.0](https://img.shields.io/badge/.NET-9.0-purple.svg)](https://dotnet.microsoft.com/download/dotnet/9.0)

A cross-platform tool that allows players to analyze their `chat.log` file and receive a detailed, visualized breakdown of their combat encounters. It parses the log, identifies individual fights, and presents statistics like damage dealt, healing done, crowd control used, and other key metrics for performance analysis.

## Features

- **Combat Log Parsing** — Parse and analyze Dark Age of Camelot combat logs
- **Cross-Platform GUI** — Desktop application for Windows, macOS, and Linux
- **Real-Time Statistics** — DPS, HPS, damage breakdowns, and more
- **Charts & Visualization** — Timeline charts, damage distribution, and trends
- **Cross-Realm Analysis** — Track statistics by realm and character class
- **Plugin System** — Extend functionality with custom plugins
- **Export Options** — Export to JSON and CSV formats

## Installation

### Download Pre-Built Releases

Download the latest release for your platform from the [Releases](https://github.com/southpawriter02/camelot-combat-reporter/releases) page:

- **Windows**: `CamelotCombatReporter-win-x64.zip`
- **macOS (Intel)**: `CamelotCombatReporter-osx-x64.zip`
- **macOS (Apple Silicon)**: `CamelotCombatReporter-osx-arm64.zip`
- **Linux**: `CamelotCombatReporter-linux-x64.zip`

Extract and run the executable — no .NET runtime installation required.

### Build from Source

Requires [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) or later.

```bash
git clone https://github.com/southpawriter02/camelot-combat-reporter.git
cd camelot-combat-reporter
dotnet build
```

## Usage

### GUI Application

```bash
dotnet run --project src/CamelotCombatReporter.Gui
```

### Command-Line Interface

```bash
dotnet run --project src/CamelotCombatReporter.Cli -- <path_to_log_file> [combatant_name]

# Example:
dotnet run --project src/CamelotCombatReporter.Cli -- data/sample.log
```

## Project Structure

| Directory | Description |
|-----------|-------------|
| `src/CamelotCombatReporter.Core` | Core parsing and analysis library |
| `src/CamelotCombatReporter.Cli` | Command-line interface |
| `src/CamelotCombatReporter.Gui` | Graphical user interface (Avalonia UI) |
| `src/CamelotCombatReporter.Plugins` | Plugin host infrastructure |
| `src/CamelotCombatReporter.PluginSdk` | Plugin development SDK |
| `tests/` | Unit tests |

## Documentation

- [Architecture Overview](ARCHITECTURE.md) — System design and data flow
- [Contributing Guide](CONTRIBUTING.md) — Development setup and guidelines
- [Changelog](CHANGELOG.md) — Version history and release notes
- [Plugin Development](docs/plugins/README.md) — Build custom plugins
- [Feature Roadmap](roadmap/README.md) — Planned features and status

## Testing

```bash
dotnet test
```

## License

This project is licensed under the MIT License — see the [LICENSE](LICENSE) file for details.
