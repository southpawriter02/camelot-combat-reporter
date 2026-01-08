using CamelotCombatReporter.Core.Models;
using CamelotCombatReporter.Plugins.Abstractions;
using CamelotCombatReporter.Plugins.Permissions;
using CamelotCombatReporter.PluginSdk;
using DamageBreakdownChart.Models;
using DamageBreakdownChart.Services;
using DamageBreakdownChart.ViewModels;
using DamageBreakdownChart.Views;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace DamageBreakdownChart;

/// <summary>
/// Damage Breakdown Chart plugin - interactive visualization for damage analysis.
/// </summary>
/// <remarks>
/// <para>
/// Provides hierarchical charts (treemap, pie) to explore damage breakdown
/// by type, ability category, ability name, and target.
/// </para>
/// <para>
/// <strong>Features</strong>:
/// <list type="bullet">
///   <item><description>Treemap visualization of damage distribution</description></item>
///   <item><description>Interactive drill-down through hierarchy</description></item>
///   <item><description>Color-coded damage types</description></item>
///   <item><description>Statistics panel for selections</description></item>
/// </list>
/// </para>
/// </remarks>
public sealed class DamageChartPlugin : UIPluginBase
{
    private readonly DamageTreeBuilder _treeBuilder;
    private DamageNode? _rootNode;
    private ILogger<DamageChartPlugin>? _logger;
    private IUIComponentContext? _componentContext;

    public DamageChartPlugin()
    {
        _treeBuilder = new DamageTreeBuilder();
    }

    #region Plugin Metadata

    /// <inheritdoc/>
    public override string Id => "damage-breakdown-chart";

    /// <inheritdoc/>
    public override string Name => "Damage Breakdown Chart";

    /// <inheritdoc/>
    public override Version Version => new(1, 0, 0);

    /// <inheritdoc/>
    public override string Author => "CCR Community";

    /// <inheritdoc/>
    public override string Description =>
        "Interactive visualization for exploring damage breakdown by type, ability, and target.";

    /// <inheritdoc/>
    public override IReadOnlyCollection<PluginPermission> RequiredPermissions =>
    [
        PluginPermission.CombatDataAccess,
        PluginPermission.UIModification
    ];

    /// <inheritdoc/>
    public override IReadOnlyCollection<UIComponentDefinition> Components =>
    [
        new UIComponentDefinition(
            "damage-treemap",
            "Damage Treemap",
            "Interactive treemap damage breakdown",
            UIComponentLocation.MainTab,
            60,
            null),
        new UIComponentDefinition(
            "damage-breakdown",
            "Damage Breakdown",
            "Pie chart damage breakdown with drill-down",
            UIComponentLocation.MainTab,
            61,
            null)
    ];

    #endregion

    #region Lifecycle

    /// <inheritdoc/>
    public override Task InitializeAsync(
        IPluginContext context,
        CancellationToken ct = default)
    {
        _logger = context.GetService<ILoggerFactory>()
            ?.CreateLogger<DamageChartPlugin>()
            ?? NullLogger<DamageChartPlugin>.Instance;

        _logger.LogDebug("Damage Chart plugin initialized");
        return Task.CompletedTask;
    }

    #endregion

    #region UI Components

    /// <inheritdoc/>
    public override Task<object> CreateComponentAsync(
        string componentId,
        IUIComponentContext context,
        CancellationToken ct = default)
    {
        _componentContext = context;

        return componentId switch
        {
            "damage-treemap" => CreateTreemapView(),
            "damage-breakdown" => CreateBreakdownView(),
            _ => throw new ArgumentException($"Unknown component: {componentId}")
        };
    }

    private Task<object> CreateTreemapView()
    {
        var viewModel = new TreemapViewModel(_rootNode);
        var view = new TreemapView { DataContext = viewModel };
        return Task.FromResult<object>(view);
    }

    private Task<object> CreateBreakdownView()
    {
        var viewModel = new BreakdownViewModel(_rootNode);
        var view = new BreakdownView { DataContext = viewModel };
        return Task.FromResult<object>(view);
    }

    #endregion

    #region Data Updates

    /// <inheritdoc/>
    public override Task OnDataChangedAsync(
        IReadOnlyList<LogEvent> events,
        CombatStatistics? statistics,
        CancellationToken ct = default)
    {
        _logger?.LogDebug("Data changed, rebuilding damage tree");

        _rootNode = _treeBuilder.BuildTree(events);

        _logger?.LogInformation(
            "Built damage tree: {TotalDamage} total, {Children} types",
            _rootNode.TotalDamage,
            _rootNode.Children.Count);

        _componentContext?.RequestRefresh();

        return Task.CompletedTask;
    }

    /// <summary>
    /// Gets the current damage tree.
    /// </summary>
    public DamageNode? GetDamageTree() => _rootNode;

    #endregion
}
