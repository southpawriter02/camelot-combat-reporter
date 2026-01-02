using System.Windows.Input;
using CamelotCombatReporter.Core.Models;
using CamelotCombatReporter.Plugins.Abstractions;

namespace CamelotCombatReporter.PluginSdk;

/// <summary>
/// Base class for UI component plugins.
/// Extend this class to add custom tabs, panels, or visualizations.
/// </summary>
public abstract class UIPluginBase : PluginBase, IUIComponentPlugin
{
    /// <inheritdoc/>
    public sealed override PluginType Type => PluginType.UIComponent;

    /// <summary>
    /// UI components provided by this plugin.
    /// </summary>
    public abstract IReadOnlyCollection<UIComponentDefinition> Components { get; }

    /// <summary>
    /// Menu items to add to the application.
    /// Override to add menu items.
    /// </summary>
    public virtual IReadOnlyCollection<PluginMenuItem> MenuItems { get; } =
        Array.Empty<PluginMenuItem>();

    /// <summary>
    /// Toolbar items to add to the application.
    /// Override to add toolbar items.
    /// </summary>
    public virtual IReadOnlyCollection<PluginToolbarItem> ToolbarItems { get; } =
        Array.Empty<PluginToolbarItem>();

    /// <summary>
    /// Creates a UI component asynchronously.
    /// </summary>
    public abstract Task<object> CreateComponentAsync(
        string componentId,
        IUIComponentContext context,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Called when combat data changes to update components.
    /// </summary>
    public virtual Task OnDataChangedAsync(
        IReadOnlyList<LogEvent> events,
        CombatStatistics? statistics,
        CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Creates a tab component definition.
    /// </summary>
    protected UIComponentDefinition Tab(
        string id,
        string name,
        string description,
        int displayOrder = 100,
        string? iconKey = null)
    {
        return new UIComponentDefinition(
            id,
            name,
            description,
            UIComponentLocation.MainTab,
            displayOrder,
            iconKey);
    }

    /// <summary>
    /// Creates a side panel component definition.
    /// </summary>
    protected UIComponentDefinition SidePanel(
        string id,
        string name,
        string description,
        int displayOrder = 100,
        string? iconKey = null)
    {
        return new UIComponentDefinition(
            id,
            name,
            description,
            UIComponentLocation.SidePanel,
            displayOrder,
            iconKey);
    }

    /// <summary>
    /// Creates a statistics card component definition.
    /// </summary>
    protected UIComponentDefinition StatisticsCard(
        string id,
        string name,
        string description,
        int displayOrder = 100,
        string? iconKey = null)
    {
        return new UIComponentDefinition(
            id,
            name,
            description,
            UIComponentLocation.StatisticsCard,
            displayOrder,
            iconKey);
    }

    /// <summary>
    /// Creates a menu item.
    /// </summary>
    protected PluginMenuItem MenuItem(
        string header,
        string menuPath,
        ICommand command,
        int displayOrder = 100,
        string? gesture = null,
        string? iconKey = null)
    {
        return new PluginMenuItem(
            header,
            menuPath,
            command,
            gesture,
            iconKey,
            displayOrder);
    }

    /// <summary>
    /// Creates a toolbar item.
    /// </summary>
    protected PluginToolbarItem ToolbarItem(
        string tooltip,
        ICommand command,
        string iconKey,
        int displayOrder = 100,
        string? groupId = null)
    {
        return new PluginToolbarItem(
            tooltip,
            command,
            iconKey,
            displayOrder,
            groupId);
    }
}
