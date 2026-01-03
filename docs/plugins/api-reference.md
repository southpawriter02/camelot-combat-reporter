# Plugin SDK API Reference

This reference documents all public APIs available to plugin developers in the Camelot Combat Reporter Plugin SDK.

## Table of Contents

- [Base Classes](#base-classes)
- [Interfaces](#interfaces)
- [Data Types](#data-types)
- [Context and Services](#context-and-services)
- [Event Types](#event-types)

---

## Base Classes

### PluginBase

The abstract base class for all plugins. Extend this class or one of its specialized subclasses.

**Namespace:** `CamelotCombatReporter.PluginSdk`

#### Abstract Properties (Required)

| Property | Type | Description |
|----------|------|-------------|
| `Id` | `string` | Unique plugin identifier (must match manifest) |
| `Name` | `string` | Display name for the plugin |
| `Version` | `Version` | Plugin version |
| `Author` | `string` | Plugin author name |
| `Description` | `string` | Brief description |
| `Type` | `PluginType` | Type of plugin (set by subclass) |

#### Virtual Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `RequiredPermissions` | `IReadOnlyCollection<PluginPermission>` | Empty | Permissions required by the plugin |

#### Read-Only Properties

| Property | Type | Description |
|----------|------|-------------|
| `State` | `PluginState` | Current plugin lifecycle state |
| `Context` | `IPluginContext` | Access to application services (after load) |

#### Lifecycle Methods

```csharp
// Called when the plugin is being loaded
public virtual Task OnLoadAsync(IPluginContext context, CancellationToken ct = default);

// Called to initialize the plugin after loading
public virtual Task InitializeAsync(IPluginContext context, CancellationToken ct = default);

// Called when the plugin is being enabled
public virtual Task OnEnableAsync(CancellationToken ct = default);

// Called when the plugin is being disabled
public virtual Task OnDisableAsync(CancellationToken ct = default);

// Called when the plugin is being unloaded
public virtual Task OnUnloadAsync(CancellationToken ct = default);

// Dispose resources
public void Dispose();
protected virtual void Dispose(bool disposing);
```

#### Logging Methods

```csharp
protected void LogDebug(string message);
protected void LogInfo(string message);
protected void LogWarning(string message);
protected void LogError(string message, Exception? exception = null);
```

---

### DataAnalysisPluginBase

Base class for data analysis plugins that compute custom statistics.

**Extends:** `PluginBase`, `IDataAnalysisPlugin`

#### Abstract Properties

| Property | Type | Description |
|----------|------|-------------|
| `ProvidedStatistics` | `IReadOnlyCollection<StatisticDefinition>` | Statistics this plugin computes |

#### Abstract Methods

```csharp
// Performs analysis on combat events
public abstract Task<AnalysisResult> AnalyzeAsync(
    IReadOnlyList<LogEvent> events,
    CombatStatistics? baseStatistics,
    AnalysisOptions options,
    CancellationToken cancellationToken = default);
```

#### Helper Methods

```csharp
// Create a successful analysis result
protected AnalysisResult Success(
    Dictionary<string, object> statistics,
    IEnumerable<AnalysisInsight>? insights = null);

// Create an empty result
protected AnalysisResult Empty();

// Define a numeric statistic
protected StatisticDefinition DefineNumericStatistic(
    string id,
    string name,
    string description,
    string category);

// Define a statistic with custom type
protected StatisticDefinition DefineStatistic(
    string id,
    string name,
    string description,
    string category,
    Type valueType);

// Create an analysis insight
protected AnalysisInsight Insight(
    string title,
    string description,
    InsightSeverity severity = InsightSeverity.Info);

// Filter events to damage dealt by combatant
protected IEnumerable<DamageEvent> GetDamageDealt(
    IReadOnlyList<LogEvent> events,
    string combatantName);

// Filter events to damage taken by combatant
protected IEnumerable<DamageEvent> GetDamageTaken(
    IReadOnlyList<LogEvent> events,
    string combatantName);

// Filter events to healing done by combatant
protected IEnumerable<HealingEvent> GetHealingDone(
    IReadOnlyList<LogEvent> events,
    string combatantName);
```

#### Example

```csharp
public class MyAnalysisPlugin : DataAnalysisPluginBase
{
    public override string Id => "my-analysis";
    public override string Name => "My Analysis";
    public override Version Version => new(1, 0, 0);
    public override string Author => "Developer";
    public override string Description => "Custom analysis.";

    public override IReadOnlyCollection<StatisticDefinition> ProvidedStatistics =>
        new[]
        {
            DefineNumericStatistic("total-damage", "Total Damage", "Sum of all damage", "Damage")
        };

    public override Task<AnalysisResult> AnalyzeAsync(
        IReadOnlyList<LogEvent> events,
        CombatStatistics? baseStatistics,
        AnalysisOptions options,
        CancellationToken ct = default)
    {
        var damage = GetDamageDealt(events, options.CombatantName)
            .Sum(e => e.DamageAmount);

        return Task.FromResult(Success(new Dictionary<string, object>
        {
            ["total-damage"] = damage
        }));
    }
}
```

---

### ExportPluginBase

Base class for export format plugins.

**Extends:** `PluginBase`, `IExportPlugin`

#### Abstract Properties

| Property | Type | Description |
|----------|------|-------------|
| `FileExtension` | `string` | File extension (e.g., ".xml") |
| `MimeType` | `string` | MIME type (e.g., "application/xml") |
| `FormatDisplayName` | `string` | Display name for file dialogs |

#### Virtual Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `ExportOptions` | `IReadOnlyCollection<ExportOptionDefinition>` | Empty | Configurable export options |

#### Abstract Methods

```csharp
// Export data to the output stream
public abstract Task<ExportResult> ExportAsync(
    ExportContext context,
    Stream outputStream,
    CancellationToken cancellationToken = default);
```

#### Helper Methods

```csharp
// Create a successful export result
protected ExportResult Success(long bytesWritten);

// Create a failed export result
protected ExportResult Failure(string error);

// Create an export option
protected ExportOptionDefinition Option(
    string id,
    string name,
    string description,
    Type valueType,
    object? defaultValue = null,
    bool required = false);

// Create a boolean option
protected ExportOptionDefinition BoolOption(
    string id,
    string name,
    string description,
    bool defaultValue = false);

// Create a string option
protected ExportOptionDefinition StringOption(
    string id,
    string name,
    string description,
    string? defaultValue = null,
    bool required = false);

// Write text to stream (UTF-8)
protected async Task<long> WriteTextAsync(
    Stream stream,
    string content,
    CancellationToken ct = default);
```

#### Example

```csharp
public class XmlExportPlugin : ExportPluginBase
{
    public override string Id => "xml-export";
    public override string Name => "XML Export";
    public override Version Version => new(1, 0, 0);
    public override string Author => "Developer";
    public override string Description => "Exports to XML.";

    public override string FileExtension => ".xml";
    public override string MimeType => "application/xml";
    public override string FormatDisplayName => "XML Document";

    public override IReadOnlyCollection<ExportOptionDefinition> ExportOptions =>
        new[]
        {
            BoolOption("include-raw", "Include Raw Events", "Include raw event data", false)
        };

    public override async Task<ExportResult> ExportAsync(
        ExportContext context,
        Stream outputStream,
        CancellationToken ct = default)
    {
        var includeRaw = context.Options.TryGetValue("include-raw", out var val)
            && val is bool b && b;

        var xml = BuildXml(context, includeRaw);
        var bytes = await WriteTextAsync(outputStream, xml, ct);
        return Success(bytes);
    }

    private string BuildXml(ExportContext context, bool includeRaw)
    {
        // Build XML string...
    }
}
```

---

### UIPluginBase

Base class for UI component plugins.

**Extends:** `PluginBase`, `IUIComponentPlugin`

#### Abstract Properties

| Property | Type | Description |
|----------|------|-------------|
| `Components` | `IReadOnlyCollection<UIComponentDefinition>` | UI components provided |

#### Virtual Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `MenuItems` | `IReadOnlyCollection<PluginMenuItem>` | Empty | Menu items to add |
| `ToolbarItems` | `IReadOnlyCollection<PluginToolbarItem>` | Empty | Toolbar items to add |

#### Abstract Methods

```csharp
// Create a UI component instance
public abstract Task<object> CreateComponentAsync(
    string componentId,
    IUIComponentContext context,
    CancellationToken cancellationToken = default);
```

#### Virtual Methods

```csharp
// Called when combat data changes
public virtual Task OnDataChangedAsync(
    IReadOnlyList<LogEvent> events,
    CombatStatistics? statistics,
    CancellationToken cancellationToken = default);
```

#### Helper Methods

```csharp
// Create a tab component definition
protected UIComponentDefinition Tab(
    string id,
    string name,
    string description,
    int displayOrder = 100,
    string? iconKey = null);

// Create a side panel component definition
protected UIComponentDefinition SidePanel(
    string id,
    string name,
    string description,
    int displayOrder = 100,
    string? iconKey = null);

// Create a statistics card component definition
protected UIComponentDefinition StatisticsCard(
    string id,
    string name,
    string description,
    int displayOrder = 100,
    string? iconKey = null);

// Create a menu item
protected PluginMenuItem MenuItem(
    string header,
    string menuPath,
    ICommand command,
    int displayOrder = 100,
    string? gesture = null,
    string? iconKey = null);

// Create a toolbar item
protected PluginToolbarItem ToolbarItem(
    string tooltip,
    ICommand command,
    string iconKey,
    int displayOrder = 100,
    string? groupId = null);
```

#### Example

```csharp
public class TimelinePlugin : UIPluginBase
{
    public override string Id => "timeline-view";
    public override string Name => "Timeline View";
    public override Version Version => new(1, 0, 0);
    public override string Author => "Developer";
    public override string Description => "Shows damage timeline.";

    public override IReadOnlyCollection<UIComponentDefinition> Components =>
        new[] { Tab("timeline-tab", "Timeline", "Damage over time chart") };

    public override Task<object> CreateComponentAsync(
        string componentId,
        IUIComponentContext context,
        CancellationToken ct = default)
    {
        if (componentId == "timeline-tab")
        {
            return Task.FromResult<object>(new TimelineView(context));
        }
        throw new ArgumentException($"Unknown component: {componentId}");
    }
}
```

---

### ParserPluginBase

Base class for custom parser plugins.

**Extends:** `PluginBase`, `IParserPlugin`

#### Abstract Properties

| Property | Type | Description |
|----------|------|-------------|
| `CustomEventTypes` | `IReadOnlyCollection<EventTypeDefinition>` | Custom event types |
| `Patterns` | `IReadOnlyCollection<ParsingPatternDefinition>` | Parsing patterns |

#### Virtual Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Priority` | `int` | 0 | Parser priority (higher = checked first) |

#### Abstract Methods

```csharp
// Attempt to parse a log line
public abstract ParseResult TryParse(string line, ParsingContext context);
```

#### Helper Methods

```csharp
// Create a successful parse result
protected ParseSuccess Parsed(LogEvent logEvent);

// Skip this line (try next parser)
protected ParseSkip Skip();

// Report a parse error
protected ParseError Error(string message);

// Define a custom event type
protected EventTypeDefinition DefineEventType(
    string typeName,
    Type eventType,
    string description);

// Define a parsing pattern
protected ParsingPatternDefinition DefinePattern(
    string id,
    string description,
    string regexPattern);

// Extract timestamp from line start
protected TimeOnly? TryExtractTimestamp(string line);
```

#### Example

```csharp
public class CriticalHitParser : ParserPluginBase
{
    public override string Id => "critical-hit-parser";
    public override string Name => "Critical Hit Parser";
    public override Version Version => new(1, 0, 0);
    public override string Author => "Developer";
    public override string Description => "Parses critical hit messages.";

    public override int Priority => 10; // Before built-in parsers

    private static readonly Regex CritPattern = new(
        @"^\[(?<timestamp>\d{2}:\d{2}:\d{2})\]\s+You critically hit (?<target>.+?) for (?<amount>\d+)",
        RegexOptions.Compiled);

    public override IReadOnlyCollection<EventTypeDefinition> CustomEventTypes =>
        new[] { DefineEventType("CriticalHit", typeof(CriticalHitEvent), "Critical hit damage") };

    public override IReadOnlyCollection<ParsingPatternDefinition> Patterns =>
        new[] { DefinePattern("crit-hit", "Critical hit pattern", CritPattern.ToString()) };

    public override ParseResult TryParse(string line, ParsingContext context)
    {
        var match = CritPattern.Match(line);
        if (!match.Success)
            return Skip();

        var timestamp = TimeOnly.Parse(match.Groups["timestamp"].Value);
        var target = match.Groups["target"].Value;
        var amount = int.Parse(match.Groups["amount"].Value);

        return Parsed(new CriticalHitEvent(timestamp, "You", target, amount));
    }
}

// Custom event type
public record CriticalHitEvent(
    TimeOnly Timestamp,
    string Source,
    string Target,
    int DamageAmount) : DamageEvent(Timestamp, Source, Target, DamageAmount, "Critical");
```

---

## Interfaces

### IPluginContext

Provides plugins with access to application services and resources.

```csharp
public interface IPluginContext
{
    // Plugin's isolated storage directory
    string PluginDataDirectory { get; }

    // Logger for the plugin
    IPluginLogger Logger { get; }

    // Permissions granted to this plugin
    IReadOnlyCollection<PluginPermission> GrantedPermissions { get; }

    // Application version
    Version ApplicationVersion { get; }

    // Check if a permission is granted
    bool HasPermission(PluginPermission permission);

    // Get a sandboxed service (returns null if not permitted)
    T? GetService<T>() where T : class;
}
```

**Available Services:**

| Service Type | Permission Required | Description |
|--------------|---------------------|-------------|
| `IFileSystemAccess` | `FileRead`/`FileWrite` | Read/write files |
| `INetworkAccess` | `NetworkAccess` | Make HTTP requests |
| `ICombatDataAccess` | `CombatDataAccess` | Access combat events |
| `IPreferencesAccess` | `SettingsRead`/`SettingsWrite` | Read/write settings |
| `IPluginUIService` | `UIModification` | Show dialogs, notifications |

---

### IPluginLogger

Logging interface for plugins.

```csharp
public interface IPluginLogger
{
    void Debug(string message);
    void Info(string message);
    void Warning(string message);
    void Error(string message, Exception? exception = null);
}
```

---

### IFileSystemAccess

Sandboxed file system access.

```csharp
public interface IFileSystemAccess
{
    Task<string> ReadFileAsync(string path, CancellationToken ct = default);
    Task WriteFileAsync(string path, string content, CancellationToken ct = default);
    Task<byte[]> ReadFileBytesAsync(string path, CancellationToken ct = default);
    Task WriteFileBytesAsync(string path, byte[] content, CancellationToken ct = default);
    Task<bool> FileExistsAsync(string path, CancellationToken ct = default);
    Task<bool> DirectoryExistsAsync(string path, CancellationToken ct = default);
    Task<IReadOnlyList<string>> ListFilesAsync(string directory, string pattern = "*", CancellationToken ct = default);
    Task DeleteFileAsync(string path, CancellationToken ct = default);
    Task CreateDirectoryAsync(string path, CancellationToken ct = default);
}
```

**Notes:**
- Paths are relative to `PluginDataDirectory` unless `FileReadExternal`/`FileWriteExternal` permission is granted
- Paths are normalized and validated to prevent directory traversal

---

### INetworkAccess

Sandboxed network access.

```csharp
public interface INetworkAccess
{
    Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct = default);
    Task<string> GetStringAsync(string url, CancellationToken ct = default);
    Task<byte[]> GetBytesAsync(string url, CancellationToken ct = default);
}
```

**Notes:**
- Requires `NetworkAccess` permission
- May be scoped to specific domains via manifest

---

### ICombatDataAccess

Read-only access to combat data.

```csharp
public interface ICombatDataAccess
{
    // Get all parsed events (immutable copy)
    IReadOnlyList<LogEvent>? GetAllEvents();

    // Get filtered events (immutable copy)
    IReadOnlyList<LogEvent>? GetFilteredEvents();

    // Get current statistics
    CombatStatistics? GetStatistics();

    // Subscribe to data change events
    IDisposable OnDataChanged(Action callback);
}
```

---

### IPreferencesAccess

Plugin preferences storage.

```csharp
public interface IPreferencesAccess
{
    Task<T?> GetAsync<T>(string key, CancellationToken ct = default);
    Task SetAsync<T>(string key, T value, CancellationToken ct = default);
    Task<bool> ContainsAsync(string key, CancellationToken ct = default);
    Task RemoveAsync(string key, CancellationToken ct = default);
}
```

---

### IPluginUIService

UI services for plugins.

```csharp
public interface IPluginUIService
{
    Task ShowMessageAsync(string title, string message);
    Task<bool> ShowConfirmAsync(string title, string message);
    Task<string?> ShowOpenFileDialogAsync(string title, string[] filters);
    Task<string?> ShowSaveFileDialogAsync(string title, string defaultExt, string[] filters);
    void RequestRefresh();
    Task ShowNotificationAsync(string title, string message, NotificationType type);
}
```

---

### IUIComponentContext

Context provided to UI components.

```csharp
public interface IUIComponentContext
{
    IReadOnlyList<LogEvent>? CurrentEvents { get; }
    CombatStatistics? CurrentStatistics { get; }
    IDisposable OnDataChanged(Action callback);
    void RequestRefresh();
    Task ShowNotificationAsync(string title, string message, NotificationType type);
}
```

---

## Data Types

### PluginType

```csharp
public enum PluginType
{
    DataAnalysis,
    ExportFormat,
    UIComponent,
    CustomParser
}
```

### PluginState

```csharp
public enum PluginState
{
    Unloaded,
    Loaded,
    Initialized,
    Enabled,
    Disabled,
    Error
}
```

### PluginPermission

```csharp
[Flags]
public enum PluginPermission
{
    None = 0,
    FileRead = 1 << 0,
    FileWrite = 1 << 1,
    FileReadExternal = 1 << 2,
    FileWriteExternal = 1 << 3,
    NetworkAccess = 1 << 4,
    UIModification = 1 << 5,
    UINotifications = 1 << 6,
    SettingsRead = 1 << 7,
    SettingsWrite = 1 << 8,
    ClipboardAccess = 1 << 9,
    CombatDataAccess = 1 << 10
}
```

### InsightSeverity

```csharp
public enum InsightSeverity
{
    Info,
    Suggestion,
    Warning,
    Critical
}
```

### NotificationType

```csharp
public enum NotificationType
{
    Info,
    Success,
    Warning,
    Error
}
```

### UIComponentLocation

```csharp
public enum UIComponentLocation
{
    MainTab,
    SidePanel,
    StatisticsCard,
    ChartOverlay
}
```

---

## Event Types

Plugins receive and can create events that extend `LogEvent`.

### LogEvent (Abstract Base)

```csharp
public abstract record LogEvent(TimeOnly Timestamp);
```

### DamageEvent

```csharp
public record DamageEvent(
    TimeOnly Timestamp,
    string Source,
    string Target,
    int DamageAmount,
    string DamageType) : LogEvent(Timestamp);
```

### HealingEvent

```csharp
public record HealingEvent(
    TimeOnly Timestamp,
    string Source,
    string Target,
    int HealingAmount) : LogEvent(Timestamp);
```

### CombatStyleEvent

```csharp
public record CombatStyleEvent(
    TimeOnly Timestamp,
    string Source,
    string Target,
    string StyleName) : LogEvent(Timestamp);
```

### SpellCastEvent

```csharp
public record SpellCastEvent(
    TimeOnly Timestamp,
    string Source,
    string Target,
    string SpellName) : LogEvent(Timestamp);
```

### Creating Custom Event Types

Parser plugins can define custom event types by extending `LogEvent`:

```csharp
public record CriticalHitEvent(
    TimeOnly Timestamp,
    string Source,
    string Target,
    int DamageAmount,
    double Multiplier) : LogEvent(Timestamp);

public record BuffAppliedEvent(
    TimeOnly Timestamp,
    string Source,
    string Target,
    string BuffName,
    TimeSpan Duration) : LogEvent(Timestamp);
```

---

## Record Types Reference

### StatisticDefinition

```csharp
public record StatisticDefinition(
    string Id,
    string Name,
    string Description,
    string Category,
    Type ValueType);
```

### AnalysisResult

```csharp
public record AnalysisResult(
    IReadOnlyDictionary<string, object> Statistics,
    IReadOnlyList<AnalysisInsight> Insights);
```

### AnalysisInsight

```csharp
public record AnalysisInsight(
    string Title,
    string Description,
    InsightSeverity Severity);
```

### AnalysisOptions

```csharp
public record AnalysisOptions(
    TimeOnly? StartTime,
    TimeOnly? EndTime,
    string? TargetFilter,
    string? DamageTypeFilter,
    string CombatantName);
```

### ExportContext

```csharp
public record ExportContext(
    CombatStatistics? Statistics,
    IReadOnlyList<LogEvent> Events,
    IReadOnlyList<LogEvent> FilteredEvents,
    IReadOnlyDictionary<string, object> Options,
    string CombatantName,
    IReadOnlyList<CombatStyleInfo> CombatStyles,
    IReadOnlyList<SpellCastInfo> Spells);
```

### ExportResult

```csharp
public record ExportResult(
    bool Success,
    string? ErrorMessage,
    long BytesWritten)
{
    public static ExportResult Succeeded(long bytesWritten);
    public static ExportResult Failed(string error);
}
```

### ExportOptionDefinition

```csharp
public record ExportOptionDefinition(
    string Id,
    string Name,
    string Description,
    Type ValueType,
    object? DefaultValue,
    bool Required);
```

### UIComponentDefinition

```csharp
public record UIComponentDefinition(
    string Id,
    string Name,
    string Description,
    UIComponentLocation Location,
    int DisplayOrder,
    string? IconKey);
```

### PluginMenuItem

```csharp
public record PluginMenuItem(
    string Header,
    string MenuPath,
    ICommand Command,
    string? Gesture,
    string? IconKey,
    int DisplayOrder);
```

### PluginToolbarItem

```csharp
public record PluginToolbarItem(
    string Tooltip,
    ICommand Command,
    string IconKey,
    int DisplayOrder,
    string? GroupId);
```

### EventTypeDefinition

```csharp
public record EventTypeDefinition(
    string TypeName,
    Type EventType,
    string Description);
```

### ParsingPatternDefinition

```csharp
public record ParsingPatternDefinition(
    string Id,
    string Description,
    string RegexPattern);
```

### ParsingContext

```csharp
public record ParsingContext(
    int LineNumber,
    string? PreviousLine,
    IReadOnlyList<LogEvent> RecentEvents);
```

### ParseResult Types

```csharp
public abstract record ParseResult;
public sealed record ParseSuccess(LogEvent Event) : ParseResult;
public sealed record ParseSkip : ParseResult { public static ParseSkip Instance { get; } }
public sealed record ParseError(string Message) : ParseResult;
```

---

## CombatStatistics

The `CombatStatistics` record contains pre-computed statistics from the core application.

```csharp
public record CombatStatistics(
    int TotalDamageDealt,
    int TotalDamageTaken,
    int TotalHealing,
    int HitCount,
    int MissCount,
    TimeOnly? FirstEventTime,
    TimeOnly? LastEventTime,
    Dictionary<string, int> DamageByType,
    Dictionary<string, int> DamageByTarget);
```

---

## Best Practices

### Cancellation Token Handling

Always pass and respect cancellation tokens:

```csharp
public override async Task<AnalysisResult> AnalyzeAsync(
    IReadOnlyList<LogEvent> events,
    CombatStatistics? baseStatistics,
    AnalysisOptions options,
    CancellationToken ct = default)
{
    foreach (var evt in events)
    {
        ct.ThrowIfCancellationRequested();
        // Process event...
    }
    return Success(results);
}
```

### Error Handling

Catch exceptions and return appropriate results:

```csharp
public override async Task<ExportResult> ExportAsync(
    ExportContext context,
    Stream outputStream,
    CancellationToken ct = default)
{
    try
    {
        // Export logic...
        return Success(bytesWritten);
    }
    catch (Exception ex)
    {
        LogError("Export failed", ex);
        return Failure($"Export failed: {ex.Message}");
    }
}
```

### Resource Management

Dispose resources properly:

```csharp
protected override void Dispose(bool disposing)
{
    if (disposing)
    {
        _httpClient?.Dispose();
        _subscription?.Dispose();
    }
    base.Dispose(disposing);
}
```

### Thread Safety

UI components must marshal to UI thread:

```csharp
public override Task OnDataChangedAsync(
    IReadOnlyList<LogEvent> events,
    CombatStatistics? statistics,
    CancellationToken ct = default)
{
    // Use Avalonia's Dispatcher for UI updates
    Dispatcher.UIThread.Post(() =>
    {
        UpdateChart(events);
    });
    return Task.CompletedTask;
}
```
