# Example: Critical Hit Parser Plugin

A complete Custom Parser plugin that adds support for parsing critical hits, glancing blows, and other special damage messages.

## Overview

**Plugin Type:** Custom Parser
**Complexity:** Intermediate
**Permissions:** None (parsers don't need special permissions)

This plugin demonstrates:
- Extending `ParserPluginBase`
- Defining custom event types
- Using regex for pattern matching
- Parser priority and chaining

## Complete Source Code

### CriticalHitParserPlugin.cs

```csharp
using System.Text.RegularExpressions;
using CamelotCombatReporter.Core.Models;
using CamelotCombatReporter.Plugins.Abstractions;
using CamelotCombatReporter.PluginSdk;

namespace CriticalHitParser;

/// <summary>
/// Parser plugin that recognizes critical hits, glancing blows, and other special damage.
/// </summary>
public class CriticalHitParserPlugin : ParserPluginBase
{
    public override string Id => "critical-hit-parser";
    public override string Name => "Critical Hit Parser";
    public override Version Version => new(1, 0, 0);
    public override string Author => "Example Author";
    public override string Description =>
        "Parses critical hit, glancing blow, and special damage messages.";

    // Higher priority = checked before built-in patterns (-100)
    public override int Priority => 10;

    // Regex patterns for different damage types
    private static readonly Regex CriticalHitPattern = new(
        @"^\[(?<timestamp>\d{2}:\d{2}:\d{2})\]\s+You critically hit (?<target>.+?) for (?<amount>\d+) points of(?: (?<type>\w+))? damage[!.]?$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex GlancingBlowPattern = new(
        @"^\[(?<timestamp>\d{2}:\d{2}:\d{2})\]\s+You land a glancing blow on (?<target>.+?) for (?<amount>\d+) points of(?: (?<type>\w+))? damage[!.]?$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex CriticalHitReceivedPattern = new(
        @"^\[(?<timestamp>\d{2}:\d{2}:\d{2})\]\s+(?<source>.+?) critically hits you for (?<amount>\d+) points of(?: (?<type>\w+))? damage[!.]?$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex ResistPattern = new(
        @"^\[(?<timestamp>\d{2}:\d{2}:\d{2})\]\s+(?<target>.+?) resists your (?<ability>.+?)[!.]?$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex MissPattern = new(
        @"^\[(?<timestamp>\d{2}:\d{2}:\d{2})\]\s+You miss (?<target>.+?)[!.]?$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex DodgePattern = new(
        @"^\[(?<timestamp>\d{2}:\d{2}:\d{2})\]\s+(?<target>.+?) dodges your attack[!.]?$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex ParryPattern = new(
        @"^\[(?<timestamp>\d{2}:\d{2}:\d{2})\]\s+(?<target>.+?) parries your attack[!.]?$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex BlockPattern = new(
        @"^\[(?<timestamp>\d{2}:\d{2}:\d{2})\]\s+(?<target>.+?) blocks your attack[!.]?$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public override IReadOnlyCollection<EventTypeDefinition> CustomEventTypes =>
        new[]
        {
            DefineEventType(
                typeName: "CriticalHit",
                eventType: typeof(CriticalHitEvent),
                description: "Critical hit damage event"),

            DefineEventType(
                typeName: "GlancingBlow",
                eventType: typeof(GlancingBlowEvent),
                description: "Glancing blow damage event"),

            DefineEventType(
                typeName: "Resist",
                eventType: typeof(ResistEvent),
                description: "Ability resisted by target"),

            DefineEventType(
                typeName: "Miss",
                eventType: typeof(MissEvent),
                description: "Attack missed"),

            DefineEventType(
                typeName: "Dodge",
                eventType: typeof(DodgeEvent),
                description: "Attack dodged by target"),

            DefineEventType(
                typeName: "Parry",
                eventType: typeof(ParryEvent),
                description: "Attack parried by target"),

            DefineEventType(
                typeName: "Block",
                eventType: typeof(BlockEvent),
                description: "Attack blocked by target")
        };

    public override IReadOnlyCollection<ParsingPatternDefinition> Patterns =>
        new[]
        {
            DefinePattern("crit-dealt", "Critical hit dealt", CriticalHitPattern.ToString()),
            DefinePattern("glancing", "Glancing blow dealt", GlancingBlowPattern.ToString()),
            DefinePattern("crit-received", "Critical hit received", CriticalHitReceivedPattern.ToString()),
            DefinePattern("resist", "Ability resisted", ResistPattern.ToString()),
            DefinePattern("miss", "Attack missed", MissPattern.ToString()),
            DefinePattern("dodge", "Attack dodged", DodgePattern.ToString()),
            DefinePattern("parry", "Attack parried", ParryPattern.ToString()),
            DefinePattern("block", "Attack blocked", BlockPattern.ToString())
        };

    public override Task InitializeAsync(IPluginContext context, CancellationToken ct = default)
    {
        LogInfo("Critical Hit Parser initialized");
        return base.InitializeAsync(context, ct);
    }

    public override ParseResult TryParse(string line, ParsingContext context)
    {
        // Try critical hit dealt
        var match = CriticalHitPattern.Match(line);
        if (match.Success)
        {
            return ParseCriticalHitDealt(match);
        }

        // Try glancing blow
        match = GlancingBlowPattern.Match(line);
        if (match.Success)
        {
            return ParseGlancingBlow(match);
        }

        // Try critical hit received
        match = CriticalHitReceivedPattern.Match(line);
        if (match.Success)
        {
            return ParseCriticalHitReceived(match);
        }

        // Try resist
        match = ResistPattern.Match(line);
        if (match.Success)
        {
            return ParseResist(match);
        }

        // Try miss
        match = MissPattern.Match(line);
        if (match.Success)
        {
            return ParseMiss(match);
        }

        // Try dodge
        match = DodgePattern.Match(line);
        if (match.Success)
        {
            return ParseDodge(match);
        }

        // Try parry
        match = ParryPattern.Match(line);
        if (match.Success)
        {
            return ParseParry(match);
        }

        // Try block
        match = BlockPattern.Match(line);
        if (match.Success)
        {
            return ParseBlock(match);
        }

        // Not recognized by this parser
        return Skip();
    }

    private ParseResult ParseCriticalHitDealt(Match match)
    {
        try
        {
            var timestamp = TimeOnly.Parse(match.Groups["timestamp"].Value);
            var target = match.Groups["target"].Value.Trim();
            var amount = int.Parse(match.Groups["amount"].Value);
            var damageType = match.Groups["type"].Success
                ? match.Groups["type"].Value.Trim()
                : "Physical";

            return Parsed(new CriticalHitEvent(
                Timestamp: timestamp,
                Source: "You",
                Target: target,
                DamageAmount: amount,
                DamageType: damageType,
                Multiplier: 2.0 // Default crit multiplier
            ));
        }
        catch (Exception ex)
        {
            return Error($"Failed to parse critical hit: {ex.Message}");
        }
    }

    private ParseResult ParseGlancingBlow(Match match)
    {
        try
        {
            var timestamp = TimeOnly.Parse(match.Groups["timestamp"].Value);
            var target = match.Groups["target"].Value.Trim();
            var amount = int.Parse(match.Groups["amount"].Value);
            var damageType = match.Groups["type"].Success
                ? match.Groups["type"].Value.Trim()
                : "Physical";

            return Parsed(new GlancingBlowEvent(
                Timestamp: timestamp,
                Source: "You",
                Target: target,
                DamageAmount: amount,
                DamageType: damageType,
                Reduction: 0.5 // Glancing blows typically do 50% damage
            ));
        }
        catch (Exception ex)
        {
            return Error($"Failed to parse glancing blow: {ex.Message}");
        }
    }

    private ParseResult ParseCriticalHitReceived(Match match)
    {
        try
        {
            var timestamp = TimeOnly.Parse(match.Groups["timestamp"].Value);
            var source = match.Groups["source"].Value.Trim();
            var amount = int.Parse(match.Groups["amount"].Value);
            var damageType = match.Groups["type"].Success
                ? match.Groups["type"].Value.Trim()
                : "Physical";

            return Parsed(new CriticalHitEvent(
                Timestamp: timestamp,
                Source: source,
                Target: "You",
                DamageAmount: amount,
                DamageType: damageType,
                Multiplier: 2.0
            ));
        }
        catch (Exception ex)
        {
            return Error($"Failed to parse critical hit received: {ex.Message}");
        }
    }

    private ParseResult ParseResist(Match match)
    {
        var timestamp = TimeOnly.Parse(match.Groups["timestamp"].Value);
        var target = match.Groups["target"].Value.Trim();
        var ability = match.Groups["ability"].Value.Trim();

        return Parsed(new ResistEvent(timestamp, "You", target, ability));
    }

    private ParseResult ParseMiss(Match match)
    {
        var timestamp = TimeOnly.Parse(match.Groups["timestamp"].Value);
        var target = match.Groups["target"].Value.Trim();

        return Parsed(new MissEvent(timestamp, "You", target));
    }

    private ParseResult ParseDodge(Match match)
    {
        var timestamp = TimeOnly.Parse(match.Groups["timestamp"].Value);
        var target = match.Groups["target"].Value.Trim();

        return Parsed(new DodgeEvent(timestamp, "You", target));
    }

    private ParseResult ParseParry(Match match)
    {
        var timestamp = TimeOnly.Parse(match.Groups["timestamp"].Value);
        var target = match.Groups["target"].Value.Trim();

        return Parsed(new ParryEvent(timestamp, "You", target));
    }

    private ParseResult ParseBlock(Match match)
    {
        var timestamp = TimeOnly.Parse(match.Groups["timestamp"].Value);
        var target = match.Groups["target"].Value.Trim();

        return Parsed(new BlockEvent(timestamp, "You", target));
    }
}
```

### CustomEvents.cs

```csharp
using CamelotCombatReporter.Core.Models;

namespace CriticalHitParser;

/// <summary>
/// A critical hit damage event.
/// </summary>
/// <param name="Timestamp">When the event occurred.</param>
/// <param name="Source">Who dealt the damage.</param>
/// <param name="Target">Who received the damage.</param>
/// <param name="DamageAmount">Amount of damage dealt.</param>
/// <param name="DamageType">Type of damage (Slash, Crush, etc.).</param>
/// <param name="Multiplier">Critical hit damage multiplier.</param>
public record CriticalHitEvent(
    TimeOnly Timestamp,
    string Source,
    string Target,
    int DamageAmount,
    string DamageType,
    double Multiplier) : DamageEvent(Timestamp, Source, Target, DamageAmount, DamageType)
{
    /// <summary>
    /// Whether this is a critical hit (always true for this type).
    /// </summary>
    public bool IsCritical => true;

    /// <summary>
    /// Estimated base damage before critical multiplier.
    /// </summary>
    public int BaseDamage => (int)(DamageAmount / Multiplier);
}

/// <summary>
/// A glancing blow damage event (reduced damage).
/// </summary>
public record GlancingBlowEvent(
    TimeOnly Timestamp,
    string Source,
    string Target,
    int DamageAmount,
    string DamageType,
    double Reduction) : DamageEvent(Timestamp, Source, Target, DamageAmount, DamageType)
{
    /// <summary>
    /// Whether this is a glancing blow (always true).
    /// </summary>
    public bool IsGlancing => true;

    /// <summary>
    /// Estimated full damage before reduction.
    /// </summary>
    public int FullDamage => (int)(DamageAmount / (1 - Reduction));
}

/// <summary>
/// An ability was resisted by the target.
/// </summary>
public record ResistEvent(
    TimeOnly Timestamp,
    string Source,
    string Target,
    string AbilityName) : LogEvent(Timestamp);

/// <summary>
/// An attack missed the target.
/// </summary>
public record MissEvent(
    TimeOnly Timestamp,
    string Source,
    string Target) : LogEvent(Timestamp);

/// <summary>
/// The target dodged an attack.
/// </summary>
public record DodgeEvent(
    TimeOnly Timestamp,
    string Source,
    string Target) : LogEvent(Timestamp);

/// <summary>
/// The target parried an attack.
/// </summary>
public record ParryEvent(
    TimeOnly Timestamp,
    string Source,
    string Target) : LogEvent(Timestamp);

/// <summary>
/// The target blocked an attack.
/// </summary>
public record BlockEvent(
    TimeOnly Timestamp,
    string Source,
    string Target) : LogEvent(Timestamp);
```

### plugin.json

```json
{
  "id": "critical-hit-parser",
  "name": "Critical Hit Parser",
  "version": "1.0.0",
  "author": "Example Author",
  "description": "Parses critical hit, glancing blow, and special damage messages.",
  "type": "CustomParser",
  "entryPoint": {
    "assembly": "CriticalHitParser.dll",
    "typeName": "CriticalHitParser.CriticalHitParserPlugin"
  },
  "compatibility": {
    "minAppVersion": "1.0.0"
  },
  "permissions": [],
  "resources": {
    "maxMemoryMb": 16,
    "maxCpuTimeSeconds": 5
  },
  "metadata": {
    "tags": ["parser", "critical", "damage", "combat"],
    "license": "MIT"
  }
}
```

### CriticalHitParser.csproj

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="path/to/CamelotCombatReporter.PluginSdk.csproj" />
  </ItemGroup>

  <ItemGroup>
    <None Update="plugin.json">
      <CopyToOutputDirectory>Always</CopyToOutputDirectory>
    </None>
  </ItemGroup>

</Project>
```

## Key Concepts Explained

### Parser Priority

Parsers are called in priority order (highest first). Built-in patterns have priority -100:

```csharp
// Called before built-in patterns
public override int Priority => 10;

// Called after built-in patterns
public override int Priority => -200;

// Default (called between -100 and plugins with higher priority)
public override int Priority => 0;
```

### Parse Results

Return one of three result types:

```csharp
// Successfully parsed - return the event
return Parsed(new CriticalHitEvent(...));

// Not recognized - let next parser try
return Skip();

// Parse error - log and continue
return Error("Failed to parse: invalid format");
```

### Custom Event Types

Extend `LogEvent` or one of its subclasses:

```csharp
// Extend DamageEvent for damage-related events
public record CriticalHitEvent(...) : DamageEvent(...)
{
    // Add custom properties
    public double Multiplier { get; init; }
}

// Extend LogEvent for non-damage events
public record ResistEvent(
    TimeOnly Timestamp,
    string Source,
    string Target,
    string AbilityName) : LogEvent(Timestamp);
```

### Pattern Matching

Use compiled regex for performance:

```csharp
private static readonly Regex Pattern = new(
    @"^\[(?<timestamp>\d{2}:\d{2}:\d{2})\]\s+You (?<action>.+)$",
    RegexOptions.Compiled | RegexOptions.IgnoreCase);

public override ParseResult TryParse(string line, ParsingContext context)
{
    var match = Pattern.Match(line);
    if (!match.Success)
        return Skip();

    // Extract values from named groups
    var timestamp = TimeOnly.Parse(match.Groups["timestamp"].Value);
    var action = match.Groups["action"].Value;

    // Create and return event
    return Parsed(new MyEvent(timestamp, action));
}
```

### Using Parser Context

The `ParsingContext` provides contextual information:

```csharp
public override ParseResult TryParse(string line, ParsingContext context)
{
    // Access previous line for multi-line messages
    if (context.PreviousLine?.Contains("casting") == true)
    {
        // This might be a spell result
    }

    // Access recent events for context
    var lastDamage = context.RecentEvents
        .OfType<DamageEvent>()
        .LastOrDefault();

    // Use line number for debugging
    LogDebug($"Parsing line {context.LineNumber}");

    return Skip();
}
```

## Testing

```csharp
using Xunit;

public class CriticalHitParserTests
{
    private readonly CriticalHitParserPlugin _parser = new();

    [Fact]
    public void ParsesCriticalHit()
    {
        var line = "[12:30:45] You critically hit the goblin for 250 points of Slash damage!";
        var context = new ParsingContext(1, null, new List<LogEvent>());

        var result = _parser.TryParse(line, context);

        Assert.IsType<ParseSuccess>(result);
        var evt = ((ParseSuccess)result).Event as CriticalHitEvent;
        Assert.NotNull(evt);
        Assert.Equal("You", evt.Source);
        Assert.Equal("the goblin", evt.Target);
        Assert.Equal(250, evt.DamageAmount);
        Assert.Equal("Slash", evt.DamageType);
        Assert.True(evt.IsCritical);
    }

    [Fact]
    public void ParsesGlancingBlow()
    {
        var line = "[12:30:45] You land a glancing blow on the troll for 50 points of damage!";
        var context = new ParsingContext(1, null, new List<LogEvent>());

        var result = _parser.TryParse(line, context);

        Assert.IsType<ParseSuccess>(result);
        var evt = ((ParseSuccess)result).Event as GlancingBlowEvent;
        Assert.NotNull(evt);
        Assert.Equal(50, evt.DamageAmount);
        Assert.True(evt.IsGlancing);
    }

    [Fact]
    public void SkipsUnrecognizedLines()
    {
        var line = "[12:30:45] The weather is nice today.";
        var context = new ParsingContext(1, null, new List<LogEvent>());

        var result = _parser.TryParse(line, context);

        Assert.IsType<ParseSkip>(result);
    }

    [Fact]
    public void ParsesMissEvent()
    {
        var line = "[12:30:45] You miss the skeleton!";
        var context = new ParsingContext(1, null, new List<LogEvent>());

        var result = _parser.TryParse(line, context);

        Assert.IsType<ParseSuccess>(result);
        var evt = ((ParseSuccess)result).Event as MissEvent;
        Assert.NotNull(evt);
        Assert.Equal("the skeleton", evt.Target);
    }
}
```

## Integration with Analysis Plugins

Analysis plugins can now use these custom events:

```csharp
public override Task<AnalysisResult> AnalyzeAsync(
    IReadOnlyList<LogEvent> events,
    CombatStatistics? baseStatistics,
    AnalysisOptions options,
    CancellationToken ct = default)
{
    // Count critical hits
    var criticalHits = events.OfType<CriticalHitEvent>().Count();
    var glancingBlows = events.OfType<GlancingBlowEvent>().Count();

    // Calculate crit damage bonus
    var critDamage = events.OfType<CriticalHitEvent>().Sum(e => e.DamageAmount);
    var critBaseDamage = events.OfType<CriticalHitEvent>().Sum(e => e.BaseDamage);
    var bonusDamage = critDamage - critBaseDamage;

    // Count avoidance events
    var misses = events.OfType<MissEvent>().Count();
    var dodges = events.OfType<DodgeEvent>().Count();
    var parries = events.OfType<ParryEvent>().Count();

    return Task.FromResult(Success(new Dictionary<string, object>
    {
        ["critical-hits"] = criticalHits,
        ["glancing-blows"] = glancingBlows,
        ["crit-bonus-damage"] = bonusDamage,
        ["total-misses"] = misses,
        ["total-dodges"] = dodges,
        ["total-parries"] = parries
    }));
}
```
