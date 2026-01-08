# AI Development Rules for Camelot Combat Reporter

This document defines the mandatory rules AI assistants must follow when developing features, fixing bugs, or modifying any part of the Camelot Combat Reporter codebase. These rules ensure consistency, quality, and documentation completeness across all contributions.

---

## 1. Architecture Rules

### 1.1 Project Structure

| Project | Purpose | Dependencies |
|---------|---------|--------------|
| `CamelotCombatReporter.Core` | Core library (parsing, models, services) | None (pure .NET) |
| `CamelotCombatReporter.Cli` | Command-line interface | Core only |
| `CamelotCombatReporter.Gui` | Avalonia desktop application | Core, Plugins, PluginSdk |
| `CamelotCombatReporter.Plugins` | Plugin host infrastructure | Core, PluginSdk |
| `CamelotCombatReporter.PluginSdk` | Plugin development SDK | Core (models only) |

**Rules:**
- **R-ARCH-01**: Core MUST have zero external dependencies (pure .NET Standard).
- **R-ARCH-02**: GUI MUST NOT contain business logic—delegate to Core services.
- **R-ARCH-03**: New features MUST be organized in dedicated feature folders (e.g., `Core/FeatureName/`, `Gui/FeatureName/`).
- **R-ARCH-04**: Feature folders SHOULD contain `Models/`, `Services/`, and optionally `Views/`, `ViewModels/`.

### 1.2 Interface-First Design

**Rules:**
- **R-INTF-01**: All services MUST have an `I{ServiceName}` interface defined in Core.
- **R-INTF-02**: Interfaces MUST be async-first using `Task<T>` with `CancellationToken ct = default`.
- **R-INTF-03**: Service implementations MUST accept `ILogger<T>` via constructor.
- **R-INTF-04**: Use `NullLogger<T>.Instance` as default for test compatibility.

```csharp
// CORRECT
public interface IFeatureService
{
    Task<Result> ProcessAsync(Input data, CancellationToken ct = default);
}

public class FeatureService : IFeatureService
{
    private readonly ILogger<FeatureService> _logger;
    
    public FeatureService(ILogger<FeatureService>? logger = null)
    {
        _logger = logger ?? NullLogger<FeatureService>.Instance;
    }
}
```

### 1.3 Thread Safety

**Rules:**
- **R-THRD-01**: Services with file I/O MUST use `SemaphoreSlim(1, 1)` for synchronization.
- **R-THRD-02**: Services using semaphores MUST implement `IDisposable`.
- **R-THRD-03**: All semaphore usage MUST follow try/finally pattern.

```csharp
private readonly SemaphoreSlim _semaphore = new(1, 1);

public async Task SaveAsync(Data data, CancellationToken ct)
{
    await _semaphore.WaitAsync(ct);
    try
    {
        // File operations here
    }
    finally
    {
        _semaphore.Release();
    }
}

public void Dispose() => _semaphore.Dispose();
```

### 1.4 Data Storage

**Rules:**
- **R-DATA-01**: Persistent data MUST be stored in `%APPDATA%/CamelotCombatReporter/`.
- **R-DATA-02**: Use index files for fast listing (e.g., `profiles-index.json`).
- **R-DATA-03**: Use individual JSON files for data objects (e.g., `profiles/{guid}.json`).
- **R-DATA-04**: Use `System.Text.Json` with `JsonSerializerOptions { WriteIndented = true }`.

---

## 2. Coding Standards

### 2.1 Naming Conventions

| Element | Convention | Example |
|---------|------------|---------|
| Public members | PascalCase | `PlayerName`, `CalculateDamage()` |
| Private fields | _camelCase | `_playerName`, `_damageTotal` |
| Constants | PascalCase | `MaxDamage` |
| Interfaces | I-prefix | `ICombatAnalyzer` |
| Async methods | Async suffix | `AnalyzeAsync()` |

**Rules:**
- **R-NAME-01**: Private fields MUST use underscore prefix.
- **R-NAME-02**: Async methods MUST end with `Async` suffix.
- **R-NAME-03**: Boolean properties SHOULD use Is/Has/Can prefix (`IsValid`, `HasData`).

### 2.2 Records vs Classes

**Rules:**
- **R-TYPE-01**: Use `record` for immutable DTOs and value objects.
- **R-TYPE-02**: Use `class` for services with behavior and state.
- **R-TYPE-03**: Use `record struct` for small, frequently allocated value types.

```csharp
// Immutable data
public record CombatStatistics(double Dps, int TotalDamage);

// Service with behavior
public class LogParser
{
    public List<LogEvent> Parse(string logContent) { ... }
}
```

### 2.3 File Organization

**Rules:**
- **R-FILE-01**: Use file-scoped namespaces (`namespace X.Y;`).
- **R-FILE-02**: Order: usings → namespace → type declaration.
- **R-FILE-03**: One primary type per file (exceptions: nested/helper classes).

### 2.4 MVVM Pattern (GUI)

**Rules:**
- **R-MVVM-01**: Views MUST NOT contain business logic.
- **R-MVVM-02**: ViewModels MUST use `CommunityToolkit.Mvvm` source generators.
- **R-MVVM-03**: Use `[ObservableProperty]` for bindable properties.
- **R-MVVM-04**: Use `[RelayCommand]` for command bindings.

```csharp
public partial class FeatureViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _searchText = "";
    
    [RelayCommand]
    private async Task SearchAsync()
    {
        // Implementation
    }
}
```

---

## 3. Documentation Requirements

### 3.1 XML Documentation

**Rules:**
- **R-DOC-01**: ALL public types and members MUST have XML documentation.
- **R-DOC-02**: Use `<summary>` for brief descriptions.
- **R-DOC-03**: Use `<remarks>` for implementation details, limitations, formulas.
- **R-DOC-04**: Use `<inheritdoc/>` for interface implementations.
- **R-DOC-05**: Document exceptions with `<exception cref="T">`.

```csharp
/// <summary>
/// Parses a combat log file and extracts events.
/// </summary>
/// <param name="filePath">Path to the log file.</param>
/// <returns>A list of parsed combat events.</returns>
/// <exception cref="FileNotFoundException">
/// Thrown when the specified file does not exist.
/// </exception>
/// <remarks>
/// Uses the DAoC formula: cost = level × (level + 1) ÷ 2.
/// </remarks>
public List<LogEvent> ParseFile(string filePath) { ... }
```

### 3.2 Inline Comments

**Rules:**
- **R-CMT-01**: Add comments for complex domain logic (DAoC formulas, RP calculations).
- **R-CMT-02**: Explain "why" not "what" for non-obvious code.
- **R-CMT-03**: Use section separators for logical groupings in large files.

```csharp
// ─────────────────────────────────────────────────────────────────────────
// Realm Point Calculations (DAoC Formula)
// ─────────────────────────────────────────────────────────────────────────

// Triangular number formula: n(n+1)/2 gives the base cost.
// This is multiplied by the spec line's cost multiplier.
public int CalculateCost(int level) => level * (level + 1) / 2;
```

### 3.3 Logging Standards

**Rules:**
- **R-LOG-01**: Log entry/exit for public methods at Debug level.
- **R-LOG-02**: Log method results/summaries at Information level.
- **R-LOG-03**: Log exceptions at Error level with full context.
- **R-LOG-04**: Use structured logging with named parameters.

```csharp
_logger.LogDebug("Calculating progression for profile {ProfileId}", profileId);
_logger.LogInformation("Calculated {Count} milestones with trend {Trend:F2}", count, trend);
_logger.LogError(ex, "Failed to save profile {ProfileId}", profileId);
```

---

## 4. Testing Requirements

### 4.1 Test Organization

```
tests/
├── CamelotCombatReporter.Core.Tests/
│   ├── FeatureName/
│   │   ├── FeatureServiceTests.cs
│   │   └── FeatureModelsTests.cs
├── CamelotCombatReporter.Gui.Tests/
├── integration/
└── fixtures/
```

**Rules:**
- **R-TEST-01**: Tests MUST mirror source folder structure.
- **R-TEST-02**: Test classes MUST match `{ClassName}Tests.cs` naming.
- **R-TEST-03**: Use xUnit framework with `[Fact]` and `[Theory]`.

### 4.2 Test Naming

**Pattern:** `MethodName_Condition_ExpectedResult`

```csharp
[Fact]
public void Parse_DamageEvent_ExtractsCorrectAmount() { ... }

[Fact]
public void CalculateMetrics_ZeroDeaths_KdEqualsKills() { ... }

[Theory]
[InlineData(1, 0)]
[InlineData(50, 126)]
public void GetMaxSpecPoints_AtLevel_ReturnsCorrectPoints(int level, int expected) { ... }
```

**Rules:**
- **R-TNAM-01**: Use descriptive method names following the pattern above.
- **R-TNAM-02**: Avoid abbreviations—clarity over brevity.

### 4.3 Test Structure

**Rules:**
- **R-TSTC-01**: Use Arrange/Act/Assert pattern with comments.
- **R-TSTC-02**: Create helper methods for common test data (`CreateTestSession()`).
- **R-TSTC-03**: Use testable subclasses to avoid external dependencies.

```csharp
[Fact]
public void CalculateProgressionSummary_EmptyProgression_ReturnsEmptySummary()
{
    // Arrange
    var service = new TestableProgressionService();
    var progression = new RealmRankProgression();

    // Act
    var summary = service.CalculateProgressionSummary(progression);

    // Assert
    Assert.Equal(0, summary.CurrentRank);
    Assert.Equal(0, summary.TotalRealmPoints);
}
```

### 4.4 Test Coverage Requirements

**Rules:**
- **R-COV-01**: New services MUST have at least 5-10 unit tests covering:
  - Happy path scenarios
  - Edge cases (empty inputs, zero values)
  - Error conditions
  - Boundary values
- **R-COV-02**: Tests MUST run via `dotnet test`.
- **R-COV-03**: All tests MUST pass before submitting changes.

---

## 5. Changelog and Versioning

### 5.1 Semantic Versioning

Format: `MAJOR.MINOR.PATCH`

| Change Type | Version Bump | Example |
|-------------|--------------|---------|
| Breaking changes | MAJOR | 1.x.x → 2.0.0 |
| New features | MINOR | 1.8.x → 1.9.0 |
| Bug fixes | PATCH | 1.8.0 → 1.8.1 |

### 5.2 CHANGELOG.md Format

**Rules:**
- **R-CLOG-01**: Follow [Keep a Changelog](https://keepachangelog.com/) format.
- **R-CLOG-02**: Group changes by: Added, Changed, Deprecated, Removed, Fixed, Security.
- **R-CLOG-03**: Include version number and date: `## [1.9.0] - 2026-01-07`.
- **R-CLOG-04**: List new services, models, tests, and GUI components.
- **R-CLOG-05**: Update test counts in Changed section.

```markdown
## [1.9.0] - 2026-01-07

### Added
- **Feature Name**
  - `INewService` interface and implementation
  - `NewModel` record for data representation
  - New "Feature" tab in main window

### Changed
- Total tests: 410 (381 Core + 29 GUI)
```

---

## 6. Pull Request Checklist

Before submitting any changes, verify:

- [ ] **Build**: `dotnet build` completes without errors
- [ ] **Tests**: `dotnet test` passes all tests
- [ ] **Documentation**: XML docs on all public APIs
- [ ] **Logging**: ILogger injected and used appropriately
- [ ] **Changelog**: CHANGELOG.md updated for notable changes
- [ ] **Architecture**: Feature follows folder structure conventions
- [ ] **Naming**: All conventions followed

---

## 7. File References

| Document | Path | Purpose |
|----------|------|---------|
| Architecture | `ARCHITECTURE.md` | System overview and design decisions |
| Contributing | `CONTRIBUTING.md` | Development setup and guidelines |
| Changelog | `CHANGELOG.md` | Version history and release notes |
| Roadmap | `roadmap/README.md` | Feature planning and status |

---

## Verification

### How to Verify Compliance

1. **Run build**: `dotnet build` from repository root
2. **Run tests**: `dotnet test` from repository root
3. **Check warnings**: Address or document any new build warnings
4. **Review manually**: Compare changes against rules in this document

### Commands

```bash
# Build all projects
dotnet build

# Run all tests
dotnet test

# Run specific test project
dotnet test tests/CamelotCombatReporter.Core.Tests
```
