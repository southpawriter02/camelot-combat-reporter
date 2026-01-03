# Combat Log Merger Plugin

## Plugin Type: Export Format + Data Analysis

## Overview

Merge multiple combat log files into a unified analysis, enabling group-wide statistics from individual player logs. Essential for raid leaders wanting complete group performance data.

## Problem Statement

In group content, each player only sees their own combat log. Raid leaders and analysts want to:
- Combine logs from all group members
- See complete damage/healing across the raid
- Analyze group coordination
- Build comprehensive encounter reports
- Compare individual perspectives

## Features

### Log Merging
- Import multiple log files simultaneously
- Align timestamps across files
- Detect and merge duplicate events
- Handle timezone differences
- Support drag-and-drop import

### Unified Analysis
- Combined damage/healing totals
- Group-wide statistics
- Per-player breakdowns
- Event correlation across players

### Conflict Resolution
- Detect conflicting damage values
- Flag potential log corruption
- Use most complete data source
- Report merge statistics

### Export
- Merged log file export
- Combined statistics report
- Per-perspective comparison

## Technical Specification

### Plugin Manifest

```json
{
  "id": "combat-log-merger",
  "name": "Combat Log Merger",
  "version": "1.0.0",
  "author": "CCR Community",
  "description": "Merges multiple combat logs for unified group analysis",
  "type": "ExportFormat",
  "entryPoint": {
    "assembly": "CombatLogMerger.dll",
    "typeName": "CombatLogMerger.MergerPlugin"
  },
  "compatibility": {
    "minAppVersion": "1.0.0"
  },
  "permissions": [
    "CombatDataAccess",
    "FileRead",
    "FileReadExternal",
    "FileWrite",
    "UIModification",
    "UINotifications"
  ],
  "resources": {
    "maxMemoryMb": 256,
    "maxCpuTimeSeconds": 120
  }
}
```

### Data Structures

```csharp
public record MergeSource(
    string FilePath,
    string PlayerName,
    DateTime FileTimestamp,
    TimeSpan TimezoneOffset,
    int EventCount,
    MergeSourceStatus Status
);

public enum MergeSourceStatus
{
    Pending,
    Loaded,
    Merged,
    Error
}

public record MergeResult(
    int TotalEvents,
    int UniqueEvents,
    int DuplicatesRemoved,
    int ConflictsDetected,
    int ConflictsResolved,
    TimeSpan TimeRange,
    IReadOnlyList<MergeSource> Sources,
    IReadOnlyList<MergeConflict> UnresolvedConflicts
);

public record MergeConflict(
    DateTime Timestamp,
    string EventDescription,
    string Source1,
    string Source2,
    object Value1,
    object Value2,
    ConflictResolution Resolution
);

public enum ConflictResolution
{
    UseFirst,
    UseSecond,
    Average,
    Skip,
    Manual
}

public record MergedLogEvent(
    LogEvent Event,
    string SourceFile,
    string SourcePlayer,
    bool IsDuplicate,
    MergeConflict? Conflict
);
```

### Implementation Outline

```csharp
public class MergerPlugin : ExportPluginBase
{
    private List<MergeSource> _sources = new();
    private List<MergedLogEvent> _mergedEvents = new();

    public override string FileExtension => ".merged.json";
    public override string MimeType => "application/json";
    public override string FormatDisplayName => "Merged Combat Log";

    // Custom method to add source files
    public async Task AddSourceAsync(string filePath, CancellationToken ct = default)
    {
        var source = new MergeSource(
            filePath,
            DetectPlayerName(filePath),
            File.GetLastWriteTime(filePath),
            TimeSpan.Zero, // Will be calculated during alignment
            0,
            MergeSourceStatus.Pending
        );

        _sources.Add(source);
        await LoadSourceAsync(source, ct);
    }

    private async Task LoadSourceAsync(MergeSource source, CancellationToken ct)
    {
        try
        {
            // Parse the log file
            var parser = new LogParser();
            var events = await parser.ParseFileAsync(source.FilePath, ct);

            // Tag events with source info
            foreach (var evt in events)
            {
                _mergedEvents.Add(new MergedLogEvent(
                    evt,
                    source.FilePath,
                    source.PlayerName,
                    false,
                    null
                ));
            }

            // Update source status
            UpdateSource(source with
            {
                EventCount = events.Count,
                Status = MergeSourceStatus.Loaded
            });
        }
        catch (Exception ex)
        {
            UpdateSource(source with { Status = MergeSourceStatus.Error });
            LogError($"Failed to load {source.FilePath}", ex);
        }
    }

    public MergeResult Merge()
    {
        var result = new MergeResult(
            _mergedEvents.Count,
            0, 0, 0, 0,
            TimeSpan.Zero,
            _sources,
            new List<MergeConflict>()
        );

        // Step 1: Align timestamps
        AlignTimestamps();

        // Step 2: Sort all events
        _mergedEvents = _mergedEvents
            .OrderBy(e => e.Event.Timestamp)
            .ToList();

        // Step 3: Detect duplicates
        var duplicates = DetectDuplicates();
        result = result with { DuplicatesRemoved = duplicates.Count };

        // Step 4: Detect conflicts
        var conflicts = DetectConflicts();
        result = result with { ConflictsDetected = conflicts.Count };

        // Step 5: Resolve conflicts
        var resolved = ResolveConflicts(conflicts);
        result = result with
        {
            ConflictsResolved = resolved.Count,
            UnresolvedConflicts = conflicts.Except(resolved).ToList()
        };

        // Step 6: Remove duplicates (keep first occurrence)
        _mergedEvents = _mergedEvents
            .Where(e => !e.IsDuplicate)
            .ToList();

        result = result with
        {
            UniqueEvents = _mergedEvents.Count,
            TimeRange = CalculateTimeRange()
        };

        return result;
    }

    private void AlignTimestamps()
    {
        // Find reference point (first common event across sources)
        var commonEvents = _mergedEvents
            .GroupBy(e => GetEventSignature(e.Event))
            .Where(g => g.Select(e => e.SourceFile).Distinct().Count() > 1)
            .FirstOrDefault();

        if (commonEvents == null)
        {
            LogWarning("No common events found for timestamp alignment");
            return;
        }

        // Calculate offset for each source relative to first source
        var referenceTime = commonEvents.First().Event.Timestamp;
        foreach (var source in _sources.Skip(1))
        {
            var sourceEvent = commonEvents
                .FirstOrDefault(e => e.SourceFile == source.FilePath);

            if (sourceEvent != null)
            {
                var offset = referenceTime.ToTimeSpan() -
                    sourceEvent.Event.Timestamp.ToTimeSpan();

                ApplyTimestampOffset(source.FilePath, offset);
            }
        }
    }

    private List<MergedLogEvent> DetectDuplicates()
    {
        var duplicates = new List<MergedLogEvent>();

        var groups = _mergedEvents
            .GroupBy(e => GetEventSignature(e.Event))
            .Where(g => g.Count() > 1);

        foreach (var group in groups)
        {
            // Keep first, mark rest as duplicates
            foreach (var dupe in group.Skip(1))
            {
                dupe = dupe with { IsDuplicate = true };
                duplicates.Add(dupe);
            }
        }

        return duplicates;
    }

    private string GetEventSignature(LogEvent evt)
    {
        // Create a unique signature for event matching
        return evt switch
        {
            DamageEvent d =>
                $"DMG|{d.Timestamp:HHmmss}|{d.Source}|{d.Target}|{d.DamageAmount}",
            HealingEvent h =>
                $"HEAL|{h.Timestamp:HHmmss}|{h.Source}|{h.Target}|{h.HealingAmount}",
            _ => $"{evt.GetType().Name}|{evt.Timestamp:HHmmss}"
        };
    }

    public override async Task<ExportResult> ExportAsync(
        ExportContext context,
        Stream outputStream,
        CancellationToken ct = default)
    {
        var mergeResult = Merge();

        var output = new
        {
            mergeInfo = mergeResult,
            events = _mergedEvents.Select(e => e.Event).ToList()
        };

        var json = JsonSerializer.Serialize(output, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        var bytes = await WriteTextAsync(outputStream, json, ct);
        return Success(bytes);
    }
}
```

### UI Component

```csharp
public class MergerView : UserControl
{
    private readonly MergerPlugin _plugin;
    private ObservableCollection<MergeSource> _sources = new();

    public MergerView(MergerPlugin plugin)
    {
        _plugin = plugin;
        InitializeComponent();
    }

    private async void OnFilesDropped(object sender, DragEventArgs e)
    {
        var files = e.Data.GetFileNames();
        foreach (var file in files)
        {
            if (file.EndsWith(".log") || file.EndsWith(".txt"))
            {
                await _plugin.AddSourceAsync(file);
            }
        }
        RefreshSourceList();
    }

    private void OnMergeClicked(object sender, RoutedEventArgs e)
    {
        var result = _plugin.Merge();
        ShowMergeResult(result);
    }
}
```

## UI Layout

```
┌─────────────────────────────────────────────────────────────┐
│  Combat Log Merger                                          │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  ┌─────────────────────────────────────────────────────┐   │
│  │                                                     │   │
│  │     Drop combat log files here                      │   │
│  │     or click to browse                              │   │
│  │                                                     │   │
│  └─────────────────────────────────────────────────────┘   │
│                                                             │
│  SOURCE FILES                                               │
│  ┌─────────────────────────────────────────────────────┐   │
│  │ ✓ warrior_combat.log   | Warrior1  | 12,450 events │   │
│  │ ✓ healer_log.txt       | Healer2   | 8,320 events  │   │
│  │ ⟳ caster_20250102.log  | Loading...                 │   │
│  │ ✗ corrupted.log        | Error: Invalid format      │   │
│  └─────────────────────────────────────────────────────┘   │
│                                                             │
│  MERGE OPTIONS                                              │
│  ○ Auto-align timestamps    ☑ Remove duplicates           │
│  ○ Conflict resolution: [Average ▼]                        │
│                                                             │
│  [Merge Logs]                                               │
│                                                             │
│  ─────────────────────────────────────────────────────────  │
│  MERGE RESULT                                               │
│  Total Events: 24,890  |  Unique: 22,150  |  Dupes: 2,740  │
│  Conflicts: 45 (42 resolved, 3 manual review needed)        │
│  Time Range: 2h 15m (14:30 - 16:45)                         │
│                                                             │
│  [Export Merged Log]  [View Analysis]  [Review Conflicts]   │
└─────────────────────────────────────────────────────────────┘
```

## Dependencies

- Core log parsing
- File system access (including external paths)
- UI framework for drag-drop

## Complexity

**High** - Timestamp alignment and duplicate detection across files with potential clock drift is challenging.

## Future Enhancements

- [ ] Real-time collaborative log sharing
- [ ] Cloud-based merge (upload from multiple users)
- [ ] Automatic player name detection
- [ ] Boss encounter segmentation
- [ ] Discord integration for collecting logs
- [ ] Conflict visualization timeline
