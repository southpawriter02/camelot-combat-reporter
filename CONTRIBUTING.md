# Contributing to Camelot Combat Reporter

Thank you for your interest in contributing to Camelot Combat Reporter! This document provides guidelines and instructions for contributing to the project.

## Table of Contents

- [Code of Conduct](#code-of-conduct)
- [Getting Started](#getting-started)
- [Development Environment](#development-environment)
- [Project Structure](#project-structure)
- [Coding Standards](#coding-standards)
- [Making Changes](#making-changes)
- [Testing](#testing)
- [Pull Request Process](#pull-request-process)
- [Documentation](#documentation)

---

## Code of Conduct

This project follows a simple code of conduct:

- Be respectful and inclusive
- Focus on constructive feedback
- Help maintain a welcoming environment for all contributors

---

## Getting Started

### Prerequisites

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download) or later
- [Node.js 18+](https://nodejs.org/) (for TypeScript development)
- Git
- IDE: Visual Studio, VS Code, Rider, or similar

### Clone the Repository

```bash
git clone https://github.com/southpawriter02/camelot-combat-reporter.git
cd camelot-combat-reporter
```

### Build the Project

```bash
# Build all C# projects
dotnet build

# Build specific project
dotnet build src/CamelotCombatReporter.Gui
```

### Run the Application

```bash
# Run GUI application
dotnet run --project src/CamelotCombatReporter.Gui

# Run CLI
dotnet run --project src/CamelotCombatReporter.Cli -- data/sample.log
```

### Run Tests

```bash
dotnet test
```

---

## Development Environment

### Recommended VS Code Extensions

- C# Dev Kit
- Avalonia for VSCode
- TypeScript and JavaScript Language Features
- EditorConfig for VS Code

### Recommended Rider/Visual Studio Settings

- Enable nullable reference types warnings
- Use file-scoped namespaces
- Enable XML documentation warnings for public APIs

### IDE Configuration

The project includes:
- `.editorconfig` - Code style settings
- `.idea/` - JetBrains IDE settings (Rider)

---

## Project Structure

```
src/
├── CamelotCombatReporter.Core/       # Core library (no dependencies)
├── CamelotCombatReporter.Cli/        # Command-line interface
├── CamelotCombatReporter.Gui/        # Avalonia desktop application
├── CamelotCombatReporter.Plugins/    # Plugin host infrastructure
├── CamelotCombatReporter.PluginSdk/  # Plugin development SDK
│
│   # TypeScript Implementation
├── index.ts                          # TypeScript entry point
├── parser/                           # TS log parsing
├── analysis/                         # TS combat analysis
└── ...                               # Other TS modules
```

### Project Dependencies

```
Gui ──────┬──▶ Core
          ├──▶ Plugins ──▶ PluginSdk ──▶ Core (models only)
          └──▶ PluginSdk

Cli ──────────▶ Core
```

### Key Files

| File | Purpose |
|------|---------|
| `src/CamelotCombatReporter.Core/Parsing/LogParser.cs` | Main log parsing logic |
| `src/CamelotCombatReporter.Core/Models/*.cs` | Data models and enums |
| `src/CamelotCombatReporter.Gui/Views/MainWindow.axaml` | Main UI layout |
| `src/CamelotCombatReporter.Gui/ViewModels/MainWindowViewModel.cs` | Main UI logic |

---

## Coding Standards

### C# Style Guide

#### Naming Conventions

```csharp
// PascalCase for public members
public string PlayerName { get; set; }
public void CalculateDamage() { }

// camelCase for private fields with underscore prefix
private readonly string _playerName;
private int _damageTotal;

// PascalCase for constants
public const int MaxDamage = 9999;

// Interfaces prefixed with I
public interface ICombatAnalyzer { }
```

#### File Organization

```csharp
// File-scoped namespace (preferred)
namespace CamelotCombatReporter.Core.Models;

// Order: usings, namespace, type declaration
using System;
using System.Collections.Generic;

/// <summary>
/// XML documentation for public types.
/// </summary>
public record DamageEvent(
    TimeOnly Timestamp,
    string Source,
    string Target,
    int Amount,
    string DamageType
) : LogEvent(Timestamp, Source, Target);
```

#### Records vs Classes

- Use `record` for immutable data transfer objects
- Use `class` for services with behavior and state
- Use `record struct` for small, frequently allocated value types

```csharp
// Good: Immutable data
public record CombatStatistics(double Dps, int TotalDamage);

// Good: Service with behavior
public class LogParser
{
    public List<LogEvent> Parse(string logContent) { ... }
}
```

#### Async/Await

```csharp
// Use Async suffix for async methods
public async Task<AnalysisResult> AnalyzeAsync(CancellationToken ct = default)
{
    ct.ThrowIfCancellationRequested();
    // ...
}

// Always pass CancellationToken through
public async Task ProcessAsync(CancellationToken ct)
{
    await SomeOperationAsync(ct);
}
```

### XAML Style Guide

```xml
<!-- Consistent attribute ordering: x:Name, x:DataType, properties, events -->
<Button x:Name="AnalyzeButton"
        Content="Analyze"
        Command="{Binding AnalyzeCommand}"
        Padding="15,8"
        Click="OnAnalyzeClick"/>

<!-- Use Grid for complex layouts, StackPanel for simple lists -->
<Grid ColumnDefinitions="*,Auto,*" RowDefinitions="Auto,*">
    <TextBlock Grid.Column="0" Text="Label"/>
    <Button Grid.Column="2" Content="Action"/>
</Grid>
```

### TypeScript Style Guide

```typescript
// Use explicit types for function parameters and returns
function parseLine(line: string): LogEvent | null {
    // ...
}

// Use interfaces for object shapes
interface DamageEvent extends LogEvent {
    amount: number;
    damageType: DamageType;
}

// Use const for immutable bindings
const parser = new LogParser();
```

---

## Making Changes

### Branching Strategy

```
main                 # Stable release branch
  └── feature/*      # New features
  └── fix/*          # Bug fixes
  └── docs/*         # Documentation updates
```

### Creating a Feature Branch

```bash
git checkout main
git pull origin main
git checkout -b feature/my-new-feature
```

### Commit Messages

Follow conventional commit format:

```
<type>(<scope>): <description>

[optional body]

[optional footer]
```

Types:
- `feat`: New feature
- `fix`: Bug fix
- `docs`: Documentation changes
- `style`: Code style changes (formatting, etc.)
- `refactor`: Code refactoring
- `test`: Adding or updating tests
- `chore`: Build process, dependencies, etc.

Examples:
```
feat(parser): add support for crowd control events

fix(gui): correct chart rendering on dark theme

docs(readme): update installation instructions
```

### Keeping Your Branch Updated

```bash
git fetch origin
git rebase origin/main
```

---

## Testing

### Running Tests

```bash
# Run all tests
dotnet test

# Run specific test project
dotnet test tests/CamelotCombatReporter.Core.Tests

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"
```

### Writing Tests

```csharp
using Xunit;

public class LogParserTests
{
    [Fact]
    public void Parse_DamageEvent_ExtractsCorrectAmount()
    {
        // Arrange
        var parser = new LogParser();
        var line = "[12:34:56] You hit the goblin for 150 damage!";

        // Act
        var events = parser.ParseLine(line);

        // Assert
        var damage = Assert.Single(events);
        Assert.Equal(150, ((DamageEvent)damage).Amount);
    }

    [Theory]
    [InlineData("goblin", 100)]
    [InlineData("dragon", 500)]
    public void Parse_DifferentTargets_ExtractsCorrectly(string target, int amount)
    {
        // ...
    }
}
```

### Test Organization

```
tests/
└── CamelotCombatReporter.Core.Tests/
    ├── Parsing/
    │   └── LogParserTests.cs
    ├── Models/
    │   └── CombatStatisticsTests.cs
    └── CrossRealm/
        └── CrossRealmStatisticsServiceTests.cs
```

---

## Pull Request Process

### Before Submitting

1. **Build succeeds**: `dotnet build` completes without errors
2. **Tests pass**: `dotnet test` passes all tests
3. **No warnings**: Address or document any new warnings
4. **Documentation**: Update docs for user-facing changes
5. **Changelog**: Add entry to CHANGELOG.md for notable changes

### PR Template

```markdown
## Description
Brief description of the changes.

## Type of Change
- [ ] Bug fix
- [ ] New feature
- [ ] Breaking change
- [ ] Documentation update

## Testing
Describe how you tested the changes.

## Checklist
- [ ] Code follows project style guidelines
- [ ] Tests added/updated
- [ ] Documentation updated
- [ ] CHANGELOG.md updated (if applicable)
```

### Review Process

1. Submit PR against `main` branch
2. Automated checks must pass (build, tests)
3. At least one maintainer review required
4. Address review feedback
5. Maintainer merges when approved

---

## Documentation

### XML Documentation

Add XML documentation to all public APIs:

```csharp
/// <summary>
/// Parses a combat log file and extracts events.
/// </summary>
/// <param name="filePath">Path to the log file.</param>
/// <param name="combatantName">
/// Name of the combatant to filter events for. Defaults to "You".
/// </param>
/// <returns>A list of parsed combat events.</returns>
/// <exception cref="FileNotFoundException">
/// Thrown when the specified file does not exist.
/// </exception>
public List<LogEvent> ParseFile(string filePath, string combatantName = "You")
{
    // ...
}
```

### README Updates

- Update feature lists when adding functionality
- Keep installation instructions current
- Add examples for new features

### Roadmap Updates

When completing roadmap items:

1. Update the item's status in its markdown file
2. Update the summary table in `roadmap/README.md`
3. Add implementation notes and file locations

---

## Getting Help

- **Issues**: Open a GitHub issue for bugs or feature requests
- **Discussions**: Use GitHub Discussions for questions
- **Documentation**: Check [ARCHITECTURE.md](ARCHITECTURE.md) for design details

---

## Recognition

Contributors will be recognized in:
- Git commit history
- Release notes for significant contributions
- Special thanks section for major features

Thank you for contributing to Camelot Combat Reporter!
