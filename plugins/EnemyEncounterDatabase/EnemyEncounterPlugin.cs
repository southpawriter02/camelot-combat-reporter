using CamelotCombatReporter.Core.Models;
using CamelotCombatReporter.Plugins.Abstractions;
using CamelotCombatReporter.Plugins.Permissions;
using CamelotCombatReporter.PluginSdk;
using EnemyEncounterDatabase.Analysis;
using EnemyEncounterDatabase.Models;
using EnemyEncounterDatabase.Services;
using Microsoft.Extensions.Logging;

namespace EnemyEncounterDatabase;

/// <summary>
/// Enemy Encounter Database plugin - tracks and catalogs all enemy encounters.
/// </summary>
/// <remarks>
/// <para>
/// This plugin combines data analysis (processing combat logs) with UI components
/// (providing an enemy browser tab). It automatically detects enemies from combat
/// logs, classifies them as mobs or players, and maintains a persistent database
/// of encounter statistics.
/// </para>
/// <para>
/// <strong>Features</strong>:
/// <list type="bullet">
///   <item><description>Automatic enemy detection from combat logs</description></item>
///   <item><description>Mob vs Player classification using naming heuristics</description></item>
///   <item><description>Per-enemy statistics: damage, kills, deaths, win rate</description></item>
///   <item><description>Ability damage breakdown by type</description></item>
///   <item><description>Personal notes and favorites per enemy</description></item>
///   <item><description>Searchable, filterable, sortable enemy browser</description></item>
/// </list>
/// </para>
/// <para>
/// <strong>Data Storage</strong>:
/// Enemy data is persisted to <c>{PluginDataDirectory}/enemies.json</c> using
/// thread-safe JSON serialization.
/// </para>
/// </remarks>
/// <example>
/// The plugin is loaded automatically when installed to the plugins directory.
/// It provides an "Enemy Database" tab in the main application where users can
/// browse their encounter history.
/// </example>
public sealed class EnemyEncounterPlugin : DataAnalysisPluginBase, IUIComponentPlugin
{
    private IEnemyDatabase _database = null!;
    private readonly List<EnemyRecord> _cachedEnemies = new();
    private ILogger<EnemyEncounterPlugin>? _pluginLogger;

    #region Plugin Metadata

    /// <inheritdoc/>
    /// <remarks>
    /// Unique identifier used for plugin resolution and data storage.
    /// </remarks>
    public override string Id => "enemy-encounter-database";

    /// <inheritdoc/>
    public override string Name => "Enemy Encounter Database";

    /// <inheritdoc/>
    public override Version Version => new(1, 0, 0);

    /// <inheritdoc/>
    public override string Author => "CCR Community";

    /// <inheritdoc/>
    public override string Description =>
        "Tracks and catalogs all enemy encounters with detailed statistics. " +
        "Build a personal database of every enemy you've fought, with win/loss " +
        "records, damage statistics, and personal notes.";

    /// <inheritdoc/>
    /// <remarks>
    /// <list type="bullet">
    ///   <item><term>CombatDataAccess</term><description>Required to read combat log events</description></item>
    ///   <item><term>FileRead/FileWrite</term><description>Required to persist enemy database</description></item>
    ///   <item><term>UIModification</term><description>Required to display enemy browser tab</description></item>
    /// </list>
    /// </remarks>
    public override IReadOnlyCollection<PluginPermission> RequiredPermissions =>
    [
        PluginPermission.CombatDataAccess,
        PluginPermission.FileRead,
        PluginPermission.FileWrite,
        PluginPermission.UIModification
    ];

    #endregion

    #region Statistics

    /// <inheritdoc/>
    /// <remarks>
    /// Provides aggregate statistics about the enemy database that can be
    /// displayed in the application's statistics panels.
    /// </remarks>
    public override IReadOnlyCollection<StatisticDefinition> ProvidedStatistics =>
    [
        DefineNumericStatistic(
            "unique-enemies",
            "Unique Enemies",
            "Number of unique enemies encountered",
            "Encounters"),
        DefineNumericStatistic(
            "total-encounters",
            "Total Encounters",
            "Total number of enemy encounters",
            "Encounters"),
        DefineNumericStatistic(
            "player-enemies",
            "Player Enemies",
            "Number of unique player enemies",
            "Encounters"),
        DefineNumericStatistic(
            "mob-enemies",
            "Mob Enemies",
            "Number of unique mob types",
            "Encounters")
    ];

    #endregion

    #region UI Components

    /// <inheritdoc/>
    public IReadOnlyCollection<UIComponentDefinition> Components =>
    [
        new UIComponentDefinition(
            Id: "enemy-browser",
            Name: "Enemy Database",
            Description: "Browse and search all encountered enemies",
            Location: UIComponentLocation.MainTab,
            DisplayOrder: 150,
            IconKey: "EnemyIcon")
    ];

    /// <inheritdoc/>
    public IReadOnlyCollection<PluginMenuItem> MenuItems => [];

    /// <inheritdoc/>
    public IReadOnlyCollection<PluginToolbarItem> ToolbarItems => [];

    #endregion

    #region Lifecycle Methods

    /// <inheritdoc/>
    /// <remarks>
    /// Initializes the enemy database using the plugin data directory from context.
    /// The database uses lazy loading - data is not read until first access.
    /// </remarks>
    public override async Task InitializeAsync(
        IPluginContext context,
        CancellationToken ct = default)
    {
        await base.InitializeAsync(context, ct).ConfigureAwait(false);

        // Create logger for database (uses context logger if available)
        _pluginLogger = context.Logger as ILogger<EnemyEncounterPlugin>;

        // Initialize database with plugin data directory
        _database = new JsonEnemyDatabase(context.PluginDataDirectory);

        LogInfo("Enemy Encounter Database initialized");
        LogDebug($"Database path: {context.PluginDataDirectory}/enemies.json");
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Saves any pending changes and disposes the database connection.
    /// </remarks>
    public override async Task OnUnloadAsync(CancellationToken ct = default)
    {
        LogDebug("Unloading Enemy Encounter Database plugin");

        // Persist any pending changes
        await _database.SaveChangesAsync(ct).ConfigureAwait(false);

        if (_database is IDisposable disposable)
        {
            disposable.Dispose();
        }

        LogInfo("Enemy Encounter Database unloaded successfully");
        await base.OnUnloadAsync(ct).ConfigureAwait(false);
    }

    #endregion

    #region Analysis

    /// <inheritdoc/>
    /// <remarks>
    /// <para>
    /// Processes combat log events to detect and catalog enemy encounters.
    /// For each encounter, updates or creates an enemy record in the database.
    /// </para>
    /// <para>
    /// The analysis algorithm:
    /// <list type="number">
    ///   <item><description>Detects encounters using <see cref="EncounterAnalyzer"/></description></item>
    ///   <item><description>Creates or updates enemy records in the database</description></item>
    ///   <item><description>Updates per-enemy statistics</description></item>
    ///   <item><description>Persists changes to disk</description></item>
    ///   <item><description>Returns aggregate statistics</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    public override async Task<AnalysisResult> AnalyzeAsync(
        IReadOnlyList<LogEvent> events,
        CombatStatistics? baseStatistics,
        AnalysisOptions options,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate combatant name is available
            if (string.IsNullOrEmpty(options.CombatantName))
            {
                LogWarning("No combatant name provided, skipping encounter analysis");
                return Empty();
            }

            LogDebug($"Analyzing {events.Count} events for {options.CombatantName}");

            // Detect encounters from combat events
            var encounters = EncounterAnalyzer.DetectEncounters(events, options.CombatantName);

            LogInfo($"Detected {encounters.Count} enemy encounters");

            // Process each encounter - update database
            foreach (var encounter in encounters)
            {
                await ProcessEncounterAsync(encounter, cancellationToken).ConfigureAwait(false);
            }

            // Persist changes
            await _database.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            // Build aggregate statistics for return
            var totalCount = await _database.GetTotalCountAsync(cancellationToken).ConfigureAwait(false);
            var allEnemies = await _database.SearchAsync(
                new EnemySearchCriteria(Take: 1000),
                cancellationToken).ConfigureAwait(false);

            var playerCount = allEnemies.Count(e => e.Type == EnemyType.Player);
            var mobCount = allEnemies.Count(e => e.Type == EnemyType.Mob);
            var totalEncounters = allEnemies.Sum(e => e.EncounterCount);

            LogDebug($"Statistics: {totalCount} unique enemies, {totalEncounters} total encounters");

            return Success(new Dictionary<string, object>
            {
                ["unique-enemies"] = totalCount,
                ["total-encounters"] = totalEncounters,
                ["player-enemies"] = playerCount,
                ["mob-enemies"] = mobCount
            });
        }
        catch (Exception ex)
        {
            LogError($"Error analyzing encounters: {ex.Message}", ex);
            return Empty();
        }
    }

    #endregion

    #region UI Component Factory

    /// <inheritdoc/>
    /// <remarks>
    /// Creates and initializes the enemy browser view model.
    /// The host application is responsible for creating the corresponding view.
    /// </remarks>
    public async Task<object> CreateComponentAsync(
        string componentId,
        IUIComponentContext context,
        CancellationToken cancellationToken = default)
    {
        LogDebug($"Creating UI component: {componentId}");

        if (componentId == "enemy-browser")
        {
            var viewModel = new ViewModels.EnemyBrowserViewModel(_database);
            await viewModel.LoadAsync(cancellationToken).ConfigureAwait(false);

            LogDebug("Enemy browser view model created and loaded");
            return viewModel;
        }

        LogWarning($"Unknown component requested: {componentId}");
        throw new NotSupportedException($"Unknown component: {componentId}");
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Called when combat data changes. Updates the cached enemy list for UI display.
    /// </remarks>
    public async Task OnDataChangedAsync(
        IReadOnlyList<LogEvent> events,
        CombatStatistics? statistics,
        CancellationToken cancellationToken = default)
    {
        LogDebug("Combat data changed, refreshing enemy cache");

        var enemies = await _database.SearchAsync(
            new EnemySearchCriteria(Take: 100),
            cancellationToken).ConfigureAwait(false);

        lock (_cachedEnemies)
        {
            _cachedEnemies.Clear();
            _cachedEnemies.AddRange(enemies);
        }

        LogDebug($"Enemy cache updated with {enemies.Count} enemies");
    }

    #endregion

    #region Public API

    /// <summary>
    /// Searches for enemies matching the specified criteria.
    /// </summary>
    /// <param name="criteria">The search/filter/sort criteria.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of matching enemy records.</returns>
    /// <remarks>
    /// This method is exposed for external access by view models
    /// or other components that need to query the enemy database.
    /// </remarks>
    public Task<IReadOnlyList<EnemyRecord>> SearchEnemiesAsync(
        EnemySearchCriteria criteria,
        CancellationToken ct = default)
        => _database.SearchAsync(criteria, ct);

    #endregion

    #region Private Helpers

    /// <summary>
    /// Processes a detected encounter by updating the enemy database.
    /// </summary>
    /// <param name="encounter">The detected encounter to process.</param>
    /// <param name="ct">Cancellation token.</param>
    private async Task ProcessEncounterAsync(
        DetectedEncounter encounter,
        CancellationToken ct)
    {
        // Get or create enemy record
        var enemy = await _database.GetOrCreateAsync(
            encounter.EnemyName,
            encounter.Type,
            encounter.Realm,
            ct).ConfigureAwait(false);

        // Create encounter summary
        var summary = new EncounterSummary(
            Timestamp: encounter.Timestamp,
            Duration: encounter.Duration,
            DamageDealt: encounter.DamageDealt,
            DamageTaken: encounter.DamageTaken,
            Outcome: encounter.Outcome,
            Location: null);

        // Add encounter to database
        await _database.AddEncounterAsync(
            enemy.Id,
            summary,
            encounter.DamageByAbility,
            encounter.DamageTakenByAbility,
            ct).ConfigureAwait(false);

        LogDebug($"Processed encounter with {encounter.EnemyName} ({encounter.Type})");
    }

    #endregion
}
