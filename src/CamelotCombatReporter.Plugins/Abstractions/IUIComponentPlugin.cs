using System.Windows.Input;
using CamelotCombatReporter.Core.Models;

namespace CamelotCombatReporter.Plugins.Abstractions;

/// <summary>
/// Interface for plugins that add UI components to the application.
/// </summary>
public interface IUIComponentPlugin : IPlugin
{
    /// <summary>
    /// UI components provided by this plugin.
    /// </summary>
    IReadOnlyCollection<UIComponentDefinition> Components { get; }

    /// <summary>
    /// Menu items provided by this plugin.
    /// </summary>
    IReadOnlyCollection<PluginMenuItem> MenuItems { get; }

    /// <summary>
    /// Toolbar items provided by this plugin.
    /// </summary>
    IReadOnlyCollection<PluginToolbarItem> ToolbarItems { get; }

    /// <summary>
    /// Creates an instance of the specified UI component.
    /// </summary>
    Task<object> CreateComponentAsync(
        string componentId,
        IUIComponentContext context,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Called when combat data changes to update components.
    /// </summary>
    Task OnDataChangedAsync(
        IReadOnlyList<LogEvent> events,
        CombatStatistics? statistics,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Defines a UI component provided by a plugin.
/// </summary>
/// <param name="Id">Unique identifier for the component.</param>
/// <param name="Name">Display name for the component (used as tab header, etc.).</param>
/// <param name="Description">Description of what the component shows.</param>
/// <param name="Location">Where to place the component in the UI.</param>
/// <param name="DisplayOrder">Order for sorting (lower = first).</param>
/// <param name="IconKey">Optional icon resource key.</param>
public record UIComponentDefinition(
    string Id,
    string Name,
    string Description,
    UIComponentLocation Location,
    int DisplayOrder,
    string? IconKey);

/// <summary>
/// Locations where UI components can be placed.
/// </summary>
public enum UIComponentLocation
{
    /// <summary>New tab in main content area.</summary>
    MainTab,

    /// <summary>Side panel component.</summary>
    SidePanel,

    /// <summary>Card in statistics section.</summary>
    StatisticsCard,

    /// <summary>Overlay on existing charts.</summary>
    ChartOverlay
}

/// <summary>
/// Defines a menu item provided by a plugin.
/// </summary>
/// <param name="Header">Menu item text.</param>
/// <param name="MenuPath">Parent menu path (e.g., "File", "Tools/Export").</param>
/// <param name="Command">Command to execute.</param>
/// <param name="Gesture">Optional keyboard shortcut.</param>
/// <param name="IconKey">Optional icon resource key.</param>
/// <param name="DisplayOrder">Order within parent menu.</param>
public record PluginMenuItem(
    string Header,
    string MenuPath,
    ICommand Command,
    string? Gesture,
    string? IconKey,
    int DisplayOrder);

/// <summary>
/// Defines a toolbar button provided by a plugin.
/// </summary>
/// <param name="Tooltip">Button tooltip.</param>
/// <param name="Command">Command to execute.</param>
/// <param name="IconKey">Icon resource key.</param>
/// <param name="DisplayOrder">Order in toolbar.</param>
/// <param name="GroupId">Optional group ID for separators.</param>
public record PluginToolbarItem(
    string Tooltip,
    ICommand Command,
    string IconKey,
    int DisplayOrder,
    string? GroupId);

/// <summary>
/// Context provided to UI components.
/// </summary>
public interface IUIComponentContext
{
    /// <summary>
    /// Gets the current combat events.
    /// </summary>
    IReadOnlyList<LogEvent>? CurrentEvents { get; }

    /// <summary>
    /// Gets the current combat statistics.
    /// </summary>
    CombatStatistics? CurrentStatistics { get; }

    /// <summary>
    /// Subscribe to data updates.
    /// </summary>
    IDisposable OnDataChanged(Action callback);

    /// <summary>
    /// Request a UI refresh.
    /// </summary>
    void RequestRefresh();

    /// <summary>
    /// Show a notification to the user.
    /// </summary>
    Task ShowNotificationAsync(string title, string message, NotificationType type);
}
