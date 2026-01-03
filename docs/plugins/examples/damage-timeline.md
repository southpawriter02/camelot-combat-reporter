# Example: Damage Timeline Plugin

A complete UI Component plugin that adds an interactive timeline chart showing damage over time.

## Overview

**Plugin Type:** UI Component
**Complexity:** Advanced
**Permissions:** `UIModification`, `CombatDataAccess`

This plugin demonstrates:
- Extending `UIPluginBase`
- Creating Avalonia UI components
- Subscribing to data changes
- Adding tabs and menu items

## Complete Source Code

### DamageTimelinePlugin.cs

```csharp
using System.Windows.Input;
using CamelotCombatReporter.Core.Models;
using CamelotCombatReporter.Plugins.Abstractions;
using CamelotCombatReporter.Plugins.Permissions;
using CamelotCombatReporter.PluginSdk;
using CommunityToolkit.Mvvm.Input;

namespace DamageTimeline;

/// <summary>
/// Plugin that adds a damage timeline visualization tab.
/// </summary>
public class DamageTimelinePlugin : UIPluginBase
{
    private TimelineViewModel? _viewModel;

    public override string Id => "damage-timeline";
    public override string Name => "Damage Timeline";
    public override Version Version => new(1, 0, 0);
    public override string Author => "Example Author";
    public override string Description =>
        "Adds an interactive timeline chart showing damage over time.";

    public override IReadOnlyCollection<PluginPermission> RequiredPermissions =>
        new[] { PluginPermission.UIModification, PluginPermission.CombatDataAccess };

    public override IReadOnlyCollection<UIComponentDefinition> Components =>
        new[]
        {
            Tab(
                id: "timeline-tab",
                name: "Timeline",
                description: "Interactive damage timeline chart",
                displayOrder: 50,
                iconKey: "ChartIcon")
        };

    public override IReadOnlyCollection<PluginMenuItem> MenuItems =>
        new[]
        {
            MenuItem(
                header: "Show Timeline",
                menuPath: "View",
                command: new RelayCommand(ShowTimeline),
                displayOrder: 100,
                gesture: "Ctrl+T",
                iconKey: "ChartIcon")
        };

    public override async Task InitializeAsync(IPluginContext context, CancellationToken ct = default)
    {
        await base.InitializeAsync(context, ct);

        // Get combat data access
        var dataAccess = context.GetService<ICombatDataAccess>();
        if (dataAccess != null)
        {
            // Subscribe to data changes
            dataAccess.OnDataChanged(() =>
            {
                var events = dataAccess.GetAllEvents();
                if (events != null)
                {
                    _viewModel?.UpdateData(events);
                }
            });
        }

        LogInfo("Damage Timeline plugin initialized");
    }

    public override Task<object> CreateComponentAsync(
        string componentId,
        IUIComponentContext context,
        CancellationToken cancellationToken = default)
    {
        if (componentId != "timeline-tab")
        {
            throw new ArgumentException($"Unknown component: {componentId}");
        }

        _viewModel = new TimelineViewModel();

        // Load initial data
        if (context.CurrentEvents != null)
        {
            _viewModel.UpdateData(context.CurrentEvents);
        }

        // Subscribe to updates
        context.OnDataChanged(() =>
        {
            if (context.CurrentEvents != null)
            {
                _viewModel.UpdateData(context.CurrentEvents);
            }
        });

        return Task.FromResult<object>(new TimelineView { DataContext = _viewModel });
    }

    public override Task OnDataChangedAsync(
        IReadOnlyList<LogEvent> events,
        CombatStatistics? statistics,
        CancellationToken cancellationToken = default)
    {
        _viewModel?.UpdateData(events);
        return Task.CompletedTask;
    }

    private void ShowTimeline()
    {
        // Request UI service to switch to timeline tab
        var uiService = Context.GetService<IPluginUIService>();
        uiService?.RequestRefresh();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _viewModel = null;
        }
        base.Dispose(disposing);
    }
}
```

### TimelineViewModel.cs

```csharp
using System.Collections.ObjectModel;
using Avalonia.Threading;
using CamelotCombatReporter.Core.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace DamageTimeline;

/// <summary>
/// ViewModel for the timeline chart.
/// </summary>
public partial class TimelineViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<TimelineDataPoint> _dataPoints = new();

    [ObservableProperty]
    private double _maxDamage;

    [ObservableProperty]
    private string _summary = "No data loaded";

    [ObservableProperty]
    private TimeSpan _duration;

    [ObservableProperty]
    private int _selectedWindowSeconds = 5;

    public int[] WindowOptions { get; } = { 1, 5, 10, 30, 60 };

    public void UpdateData(IReadOnlyList<LogEvent> events)
    {
        // Marshal to UI thread
        Dispatcher.UIThread.Post(() => ProcessEvents(events));
    }

    private void ProcessEvents(IReadOnlyList<LogEvent> events)
    {
        DataPoints.Clear();

        var damageEvents = events
            .OfType<DamageEvent>()
            .Where(e => e.Source == "You")
            .OrderBy(e => e.Timestamp)
            .ToList();

        if (damageEvents.Count == 0)
        {
            Summary = "No damage events found";
            MaxDamage = 0;
            Duration = TimeSpan.Zero;
            return;
        }

        // Calculate duration
        var startTime = damageEvents.First().Timestamp;
        var endTime = damageEvents.Last().Timestamp;
        Duration = CalculateDuration(startTime, endTime);

        // Group by time window
        var windowSize = TimeSpan.FromSeconds(SelectedWindowSeconds);
        var buckets = GroupIntoBuckets(damageEvents, startTime, windowSize);

        // Create data points
        MaxDamage = 0;
        foreach (var bucket in buckets)
        {
            var point = new TimelineDataPoint
            {
                TimeOffset = bucket.Key,
                Damage = bucket.Value.Sum(e => e.DamageAmount),
                EventCount = bucket.Value.Count,
                Label = FormatTime(bucket.Key)
            };
            DataPoints.Add(point);
            MaxDamage = Math.Max(MaxDamage, point.Damage);
        }

        // Update summary
        var totalDamage = damageEvents.Sum(e => e.DamageAmount);
        var dps = Duration.TotalSeconds > 0 ? totalDamage / Duration.TotalSeconds : 0;
        Summary = $"Total: {totalDamage:N0} damage | DPS: {dps:F1} | Duration: {Duration:mm\\:ss}";
    }

    private static TimeSpan CalculateDuration(TimeOnly start, TimeOnly end)
    {
        if (end < start)
            return TimeSpan.FromHours(24) - (start - end);
        return end - start;
    }

    private static Dictionary<TimeSpan, List<DamageEvent>> GroupIntoBuckets(
        List<DamageEvent> events,
        TimeOnly startTime,
        TimeSpan bucketSize)
    {
        var buckets = new Dictionary<TimeSpan, List<DamageEvent>>();

        foreach (var evt in events)
        {
            var offset = CalculateDuration(startTime, evt.Timestamp);
            var bucketKey = TimeSpan.FromSeconds(
                Math.Floor(offset.TotalSeconds / bucketSize.TotalSeconds) * bucketSize.TotalSeconds);

            if (!buckets.ContainsKey(bucketKey))
                buckets[bucketKey] = new List<DamageEvent>();

            buckets[bucketKey].Add(evt);
        }

        return buckets;
    }

    private static string FormatTime(TimeSpan offset)
    {
        if (offset.TotalMinutes >= 1)
            return $"{(int)offset.TotalMinutes}:{offset.Seconds:D2}";
        return $"{offset.TotalSeconds:F0}s";
    }

    partial void OnSelectedWindowSecondsChanged(int value)
    {
        // Re-process with new window size
        // Note: Would need to store events to reprocess
    }
}

/// <summary>
/// Represents a point on the timeline.
/// </summary>
public class TimelineDataPoint
{
    public TimeSpan TimeOffset { get; init; }
    public int Damage { get; init; }
    public int EventCount { get; init; }
    public string Label { get; init; } = "";

    // For bar chart height calculation
    public double NormalizedHeight { get; set; }
}
```

### TimelineView.axaml

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:local="clr-namespace:DamageTimeline"
             x:Class="DamageTimeline.TimelineView"
             x:DataType="local:TimelineViewModel">

    <Grid RowDefinitions="Auto,*,Auto">

        <!-- Header with controls -->
        <StackPanel Grid.Row="0" Orientation="Horizontal" Margin="10" Spacing="20">
            <TextBlock Text="Window Size:" VerticalAlignment="Center"/>
            <ComboBox ItemsSource="{Binding WindowOptions}"
                      SelectedItem="{Binding SelectedWindowSeconds}"
                      Width="80">
                <ComboBox.ItemTemplate>
                    <DataTemplate>
                        <TextBlock Text="{Binding StringFormat='{0}s'}"/>
                    </DataTemplate>
                </ComboBox.ItemTemplate>
            </ComboBox>

            <TextBlock Text="{Binding Summary}"
                       VerticalAlignment="Center"
                       FontWeight="SemiBold"/>
        </StackPanel>

        <!-- Timeline chart -->
        <Border Grid.Row="1" Background="#1a1a2e" CornerRadius="8" Margin="10" Padding="20">
            <Grid RowDefinitions="*,Auto">
                <!-- Bars -->
                <ItemsControl Grid.Row="0" ItemsSource="{Binding DataPoints}">
                    <ItemsControl.ItemsPanel>
                        <ItemsPanelTemplate>
                            <StackPanel Orientation="Horizontal" Spacing="2"/>
                        </ItemsPanelTemplate>
                    </ItemsControl.ItemsPanel>
                    <ItemsControl.ItemTemplate>
                        <DataTemplate x:DataType="local:TimelineDataPoint">
                            <Grid RowDefinitions="*,Auto" Width="30">
                                <!-- Bar -->
                                <Border Grid.Row="0"
                                        Background="#4a90d9"
                                        VerticalAlignment="Bottom"
                                        CornerRadius="2,2,0,0"
                                        ToolTip.Tip="{Binding Damage, StringFormat='Damage: {0:N0}'}">
                                    <Border.Height>
                                        <MultiBinding Converter="{x:Static local:HeightConverter.Instance}">
                                            <Binding Path="Damage"/>
                                            <Binding Path="$parent[ItemsControl].DataContext.MaxDamage"/>
                                            <Binding Path="$parent[Grid].Bounds.Height"/>
                                        </MultiBinding>
                                    </Border.Height>
                                </Border>
                                <!-- Label -->
                                <TextBlock Grid.Row="1"
                                           Text="{Binding Label}"
                                           FontSize="10"
                                           Foreground="#888"
                                           HorizontalAlignment="Center"
                                           Margin="0,5,0,0"/>
                            </Grid>
                        </DataTemplate>
                    </ItemsControl.ItemTemplate>
                </ItemsControl>

                <!-- X-axis label -->
                <TextBlock Grid.Row="1"
                           Text="Time"
                           HorizontalAlignment="Center"
                           Foreground="#888"
                           Margin="0,10,0,0"/>
            </Grid>
        </Border>

        <!-- Footer -->
        <TextBlock Grid.Row="2"
                   Text="{Binding Duration, StringFormat='Combat Duration: {0:mm\\:ss}'}"
                   HorizontalAlignment="Center"
                   Margin="10"
                   Foreground="#888"/>

    </Grid>

</UserControl>
```

### TimelineView.axaml.cs

```csharp
using Avalonia.Controls;

namespace DamageTimeline;

public partial class TimelineView : UserControl
{
    public TimelineView()
    {
        InitializeComponent();
    }
}
```

### HeightConverter.cs

```csharp
using System.Globalization;
using Avalonia.Data.Converters;

namespace DamageTimeline;

/// <summary>
/// Converts damage value to bar height based on max damage and container height.
/// </summary>
public class HeightConverter : IMultiValueConverter
{
    public static HeightConverter Instance { get; } = new();

    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Count < 3)
            return 0.0;

        if (values[0] is not int damage ||
            values[1] is not double maxDamage ||
            values[2] is not double containerHeight)
        {
            return 0.0;
        }

        if (maxDamage <= 0)
            return 0.0;

        // Leave room for label (30px) and some padding
        var maxBarHeight = containerHeight - 50;
        return Math.Max(5, (damage / maxDamage) * maxBarHeight);
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
```

### plugin.json

```json
{
  "id": "damage-timeline",
  "name": "Damage Timeline",
  "version": "1.0.0",
  "author": "Example Author",
  "description": "Adds an interactive timeline chart showing damage over time.",
  "type": "UIComponent",
  "entryPoint": {
    "assembly": "DamageTimeline.dll",
    "typeName": "DamageTimeline.DamageTimelinePlugin"
  },
  "compatibility": {
    "minAppVersion": "1.0.0"
  },
  "permissions": [
    {
      "type": "UIModification",
      "reason": "Add timeline tab to main window"
    },
    {
      "type": "CombatDataAccess",
      "reason": "Read combat events for visualization"
    }
  ],
  "resources": {
    "maxMemoryMb": 128,
    "maxCpuTimeSeconds": 30
  },
  "metadata": {
    "tags": ["chart", "timeline", "visualization", "dps"],
    "license": "MIT"
  }
}
```

### DamageTimeline.csproj

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

  <!-- Avalonia -->
  <ItemGroup>
    <PackageReference Include="Avalonia" Version="11.0.0" />
    <PackageReference Include="CommunityToolkit.Mvvm" Version="8.2.0" />
  </ItemGroup>

  <!-- XAML files -->
  <ItemGroup>
    <AvaloniaResource Include="**/*.axaml" />
  </ItemGroup>

  <ItemGroup>
    <None Update="plugin.json">
      <CopyToOutputDirectory>Always</CopyToOutputDirectory>
    </None>
  </ItemGroup>

</Project>
```

## Key Concepts Explained

### Defining UI Components

Components are defined in the `Components` property:

```csharp
public override IReadOnlyCollection<UIComponentDefinition> Components =>
    new[]
    {
        Tab("my-tab", "Tab Title", "Description", displayOrder: 50),
        SidePanel("my-panel", "Panel Title", "Description"),
        StatisticsCard("my-card", "Card Title", "Description")
    };
```

### Creating Component Instances

When the application needs your component, it calls `CreateComponentAsync`:

```csharp
public override Task<object> CreateComponentAsync(
    string componentId,
    IUIComponentContext context,
    CancellationToken cancellationToken = default)
{
    return componentId switch
    {
        "timeline-tab" => Task.FromResult<object>(new TimelineView()),
        "side-panel" => Task.FromResult<object>(new SidePanelView()),
        _ => throw new ArgumentException($"Unknown: {componentId}")
    };
}
```

### Subscribing to Data Changes

The `IUIComponentContext` provides access to current data and change notifications:

```csharp
// In CreateComponentAsync
context.OnDataChanged(() =>
{
    var events = context.CurrentEvents;
    var stats = context.CurrentStatistics;
    // Update your view
});
```

### Adding Menu Items

Define menu items with commands:

```csharp
public override IReadOnlyCollection<PluginMenuItem> MenuItems =>
    new[]
    {
        MenuItem(
            header: "My Action",
            menuPath: "Tools",        // Parent menu
            command: new RelayCommand(DoAction),
            gesture: "Ctrl+M",        // Keyboard shortcut
            displayOrder: 100)
    };
```

### Thread Safety

Always marshal UI updates to the UI thread:

```csharp
public void UpdateData(IReadOnlyList<LogEvent> events)
{
    Dispatcher.UIThread.Post(() =>
    {
        // Safe to update UI here
        DataPoints.Clear();
        foreach (var point in ProcessEvents(events))
        {
            DataPoints.Add(point);
        }
    });
}
```

## Testing UI Plugins

UI plugins are harder to unit test, but you can test the ViewModel:

```csharp
[Fact]
public void UpdateData_WithEvents_CalculatesCorrectly()
{
    var vm = new TimelineViewModel();
    var events = new List<LogEvent>
    {
        new DamageEvent(new TimeOnly(12, 0, 0), "You", "Mob", 100, "Slash"),
        new DamageEvent(new TimeOnly(12, 0, 5), "You", "Mob", 200, "Slash"),
    };

    // Note: Would need to mock Dispatcher for this test
    vm.ProcessEventsDirectly(events);

    Assert.Equal(2, vm.DataPoints.Count);
    Assert.Equal(300, vm.DataPoints.Sum(d => d.Damage));
}
```

## Visual Output

The timeline displays:
- Vertical bars representing damage in each time window
- X-axis showing time progression
- Tooltips with exact damage values
- Summary with total damage, DPS, and duration
- Configurable time window size
