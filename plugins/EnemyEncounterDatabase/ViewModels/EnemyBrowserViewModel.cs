using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using CamelotCombatReporter.Core.Models;
using EnemyEncounterDatabase.Models;
using EnemyEncounterDatabase.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace EnemyEncounterDatabase.ViewModels;

/// <summary>
/// View model for the Enemy Browser tab.
/// </summary>
/// <remarks>
/// <para>
/// Provides a searchable, filterable, and sortable view of the enemy database.
/// Implements the MVVM pattern with <see cref="INotifyPropertyChanged"/> for
/// data binding in Avalonia UI.
/// </para>
/// <para>
/// <strong>Key Features</strong>:
/// <list type="bullet">
///   <item><description>Real-time search filtering by enemy name</description></item>
///   <item><description>Type filtering (Mobs, Players, NPCs)</description></item>
///   <item><description>Multiple sort options (encounters, damage, win rate)</description></item>
///   <item><description>Favorites management</description></item>
///   <item><description>Personal notes editing</description></item>
///   <item><description>Ability damage breakdown display</description></item>
/// </list>
/// </para>
/// </remarks>
public sealed class EnemyBrowserViewModel : INotifyPropertyChanged
{
    private readonly IEnemyDatabase _database;
    private readonly ILogger<EnemyBrowserViewModel> _logger;

    private string _searchText = string.Empty;
    private EnemyType? _typeFilter;
    private EnemySortBy _sortBy = EnemySortBy.LastSeen;
    private bool _sortDescending = true;
    private bool _favoritesOnly;
    private EnemyRecord? _selectedEnemy;
    private bool _isLoading;
    private string _editingNotes = string.Empty;

    /// <summary>
    /// Creates a new instance of the enemy browser view model.
    /// </summary>
    /// <param name="database">The enemy database to query.</param>
    /// <param name="logger">Optional logger for diagnostic output.</param>
    public EnemyBrowserViewModel(
        IEnemyDatabase database,
        ILogger<EnemyBrowserViewModel>? logger = null)
    {
        _database = database ?? throw new ArgumentNullException(nameof(database));
        _logger = logger ?? NullLogger<EnemyBrowserViewModel>.Instance;

        Enemies = new ObservableCollection<EnemyRecord>();

        // Initialize commands
        RefreshCommand = new RelayCommand(async () => await LoadAsync());
        SaveNotesCommand = new RelayCommand(async () => await SaveNotesAsync());
        ToggleFavoriteCommand = new RelayCommand(async () => await ToggleFavoriteAsync());
        ClearFiltersCommand = new RelayCommand(ClearFilters);

        _logger.LogDebug("EnemyBrowserViewModel initialized");
    }

    #region Observable Properties

    /// <summary>
    /// Collection of enemies matching current search/filter criteria.
    /// </summary>
    /// <remarks>
    /// Bound to the enemy list UI. Updated when filters change or refresh is triggered.
    /// </remarks>
    public ObservableCollection<EnemyRecord> Enemies { get; }

    /// <summary>
    /// Text to search for in enemy names (case-insensitive contains match).
    /// </summary>
    /// <remarks>
    /// Setting this property automatically triggers a reload of the enemy list.
    /// </remarks>
    public string SearchText
    {
        get => _searchText;
        set
        {
            if (_searchText != value)
            {
                _searchText = value;
                OnPropertyChanged();
                _logger.LogDebug("SearchText changed to: {SearchText}", value);
                _ = LoadAsync();
            }
        }
    }

    /// <summary>
    /// Filter by enemy type. Null means show all types.
    /// </summary>
    public EnemyType? TypeFilter
    {
        get => _typeFilter;
        set
        {
            if (_typeFilter != value)
            {
                _typeFilter = value;
                OnPropertyChanged();
                _logger.LogDebug("TypeFilter changed to: {TypeFilter}", value?.ToString() ?? "All");
                _ = LoadAsync();
            }
        }
    }

    /// <summary>
    /// Property to sort results by.
    /// </summary>
    public EnemySortBy SortBy
    {
        get => _sortBy;
        set
        {
            if (_sortBy != value)
            {
                _sortBy = value;
                OnPropertyChanged();
                _logger.LogDebug("SortBy changed to: {SortBy}", value);
                _ = LoadAsync();
            }
        }
    }

    /// <summary>
    /// Whether to sort in descending order (true) or ascending (false).
    /// </summary>
    public bool SortDescending
    {
        get => _sortDescending;
        set
        {
            if (_sortDescending != value)
            {
                _sortDescending = value;
                OnPropertyChanged();
                _ = LoadAsync();
            }
        }
    }

    /// <summary>
    /// When true, only favorited enemies are shown.
    /// </summary>
    public bool FavoritesOnly
    {
        get => _favoritesOnly;
        set
        {
            if (_favoritesOnly != value)
            {
                _favoritesOnly = value;
                OnPropertyChanged();
                _logger.LogDebug("FavoritesOnly changed to: {FavoritesOnly}", value);
                _ = LoadAsync();
            }
        }
    }

    /// <summary>
    /// The currently selected enemy. Updates detail view when changed.
    /// </summary>
    /// <remarks>
    /// When set, also updates <see cref="EditingNotes"/> to the enemy's current notes.
    /// </remarks>
    public EnemyRecord? SelectedEnemy
    {
        get => _selectedEnemy;
        set
        {
            if (_selectedEnemy != value)
            {
                _selectedEnemy = value;
                _editingNotes = value?.Notes ?? string.Empty;

                OnPropertyChanged();
                OnPropertyChanged(nameof(HasSelectedEnemy));
                OnPropertyChanged(nameof(EditingNotes));
                OnPropertyChanged(nameof(TopAbilities));
                OnPropertyChanged(nameof(RecentEncounters));

                if (value != null)
                {
                    _logger.LogDebug("Selected enemy: {EnemyName}", value.Name);
                }
            }
        }
    }

    /// <summary>
    /// Whether an enemy is currently selected (for visibility bindings).
    /// </summary>
    public bool HasSelectedEnemy => _selectedEnemy != null;

    /// <summary>
    /// The notes being edited for the selected enemy.
    /// </summary>
    /// <remarks>
    /// Changes are not persisted until <see cref="SaveNotesCommand"/> is executed.
    /// </remarks>
    public string EditingNotes
    {
        get => _editingNotes;
        set
        {
            if (_editingNotes != value)
            {
                _editingNotes = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasUnsavedNotes));
            }
        }
    }

    /// <summary>
    /// Whether there are unsaved changes to the notes.
    /// </summary>
    public bool HasUnsavedNotes =>
        _selectedEnemy != null && _editingNotes != (_selectedEnemy.Notes ?? string.Empty);

    /// <summary>
    /// Whether data is currently being loaded.
    /// </summary>
    public bool IsLoading
    {
        get => _isLoading;
        private set
        {
            if (_isLoading != value)
            {
                _isLoading = value;
                OnPropertyChanged();
            }
        }
    }

    #endregion

    #region Computed Properties

    /// <summary>
    /// Top 5 abilities used against the selected enemy by total damage.
    /// </summary>
    /// <remarks>
    /// Each entry includes the ability name, total damage, and percentage of total.
    /// </remarks>
    public IReadOnlyList<AbilityDamage> TopAbilities
    {
        get
        {
            if (_selectedEnemy == null)
                return Array.Empty<AbilityDamage>();

            return _selectedEnemy.Statistics.DamageByAbility
                .OrderByDescending(kvp => kvp.Value)
                .Take(5)
                .Select(kvp => new AbilityDamage(
                    kvp.Key,
                    kvp.Value,
                    CalculateDamagePercentage(kvp.Value)))
                .ToList();
        }
    }

    /// <summary>
    /// Recent encounters with the selected enemy (up to 50 most recent).
    /// </summary>
    public IReadOnlyList<EncounterSummary> RecentEncounters =>
        _selectedEnemy?.RecentEncounters ?? Array.Empty<EncounterSummary>();

    #endregion

    #region Filter/Sort Options

    /// <summary>
    /// Available enemy type filter options for dropdown binding.
    /// </summary>
    public IReadOnlyList<EnemyTypeOption> TypeOptions { get; } =
    [
        new(null, "All Types"),
        new(EnemyType.Player, "Players"),
        new(EnemyType.Mob, "Mobs"),
        new(EnemyType.NPC, "NPCs")
    ];

    /// <summary>
    /// Available sort options for dropdown binding.
    /// </summary>
    public IReadOnlyList<SortOption> SortOptions { get; } =
    [
        new(EnemySortBy.LastSeen, "Last Seen"),
        new(EnemySortBy.FirstSeen, "First Seen"),
        new(EnemySortBy.EncounterCount, "Encounters"),
        new(EnemySortBy.Name, "Name"),
        new(EnemySortBy.DamageDealt, "Damage Dealt"),
        new(EnemySortBy.WinRate, "Win Rate"),
        new(EnemySortBy.Kills, "Kills")
    ];

    #endregion

    #region Commands

    /// <summary>
    /// Command to refresh the enemy list from the database.
    /// </summary>
    public ICommand RefreshCommand { get; }

    /// <summary>
    /// Command to save notes for the selected enemy.
    /// </summary>
    public ICommand SaveNotesCommand { get; }

    /// <summary>
    /// Command to toggle favorite status for the selected enemy.
    /// </summary>
    public ICommand ToggleFavoriteCommand { get; }

    /// <summary>
    /// Command to clear all filters and reset to default view.
    /// </summary>
    public ICommand ClearFiltersCommand { get; }

    #endregion

    #region Public Methods

    /// <summary>
    /// Loads enemies from the database based on current filters.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Task representing the async operation.</returns>
    public async Task LoadAsync(CancellationToken ct = default)
    {
        IsLoading = true;
        try
        {
            var criteria = new EnemySearchCriteria(
                NameContains: string.IsNullOrWhiteSpace(SearchText) ? null : SearchText,
                Type: TypeFilter,
                FavoritesOnly: FavoritesOnly,
                SortBy: SortBy,
                SortDescending: SortDescending,
                Take: 200);

            _logger.LogDebug(
                "Loading enemies: name={Name}, type={Type}, sort={Sort}",
                criteria.NameContains ?? "(any)",
                criteria.Type?.ToString() ?? "(any)",
                criteria.SortBy);

            var results = await _database.SearchAsync(criteria, ct).ConfigureAwait(false);

            Enemies.Clear();
            foreach (var enemy in results)
            {
                Enemies.Add(enemy);
            }

            _logger.LogDebug("Loaded {Count} enemies", results.Count);

            // Preserve selection if still in results
            if (_selectedEnemy != null)
            {
                var stillSelected = Enemies.FirstOrDefault(e => e.Id == _selectedEnemy.Id);
                if (stillSelected != null)
                {
                    SelectedEnemy = stillSelected;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load enemies");
        }
        finally
        {
            IsLoading = false;
        }
    }

    #endregion

    #region Private Helpers

    /// <summary>
    /// Saves the current editing notes to the selected enemy.
    /// </summary>
    private async Task SaveNotesAsync()
    {
        if (_selectedEnemy == null)
        {
            _logger.LogWarning("SaveNotesAsync called with no selected enemy");
            return;
        }

        try
        {
            _logger.LogDebug("Saving notes for {EnemyName}", _selectedEnemy.Name);

            await _database.UpdateNotesAsync(_selectedEnemy.Id, EditingNotes).ConfigureAwait(false);
            await _database.SaveChangesAsync().ConfigureAwait(false);

            _logger.LogInformation("Notes saved for {EnemyName}", _selectedEnemy.Name);

            await LoadAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save notes for {EnemyName}", _selectedEnemy.Name);
        }
    }

    /// <summary>
    /// Toggles the favorite status of the selected enemy.
    /// </summary>
    private async Task ToggleFavoriteAsync()
    {
        if (_selectedEnemy == null)
        {
            _logger.LogWarning("ToggleFavoriteAsync called with no selected enemy");
            return;
        }

        try
        {
            var newValue = !_selectedEnemy.IsFavorite;
            _logger.LogDebug(
                "Toggling favorite for {EnemyName}: {OldValue} -> {NewValue}",
                _selectedEnemy.Name,
                _selectedEnemy.IsFavorite,
                newValue);

            await _database.SetFavoriteAsync(_selectedEnemy.Id, newValue).ConfigureAwait(false);
            await _database.SaveChangesAsync().ConfigureAwait(false);

            _logger.LogInformation(
                "Favorite {Action} for {EnemyName}",
                newValue ? "added" : "removed",
                _selectedEnemy.Name);

            await LoadAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to toggle favorite for {EnemyName}", _selectedEnemy.Name);
        }
    }

    /// <summary>
    /// Clears all filters and resets to default view.
    /// </summary>
    private void ClearFilters()
    {
        _logger.LogDebug("Clearing all filters");

        _searchText = string.Empty;
        _typeFilter = null;
        _favoritesOnly = false;
        _sortBy = EnemySortBy.LastSeen;
        _sortDescending = true;

        OnPropertyChanged(nameof(SearchText));
        OnPropertyChanged(nameof(TypeFilter));
        OnPropertyChanged(nameof(FavoritesOnly));
        OnPropertyChanged(nameof(SortBy));
        OnPropertyChanged(nameof(SortDescending));

        _ = LoadAsync();
    }

    /// <summary>
    /// Calculates the percentage of total damage for an ability.
    /// </summary>
    private double CalculateDamagePercentage(long damage)
    {
        if (_selectedEnemy == null || _selectedEnemy.Statistics.TotalDamageDealt == 0)
            return 0;

        return (double)damage / _selectedEnemy.Statistics.TotalDamageDealt * 100;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    #endregion

    /// <inheritdoc/>
    public event PropertyChangedEventHandler? PropertyChanged;
}

#region Support Types

/// <summary>
/// Represents ability damage breakdown for display in the UI.
/// </summary>
/// <param name="Name">The ability/damage type name.</param>
/// <param name="TotalDamage">Total damage dealt with this ability.</param>
/// <param name="Percentage">Percentage of total damage dealt.</param>
public record AbilityDamage(string Name, long TotalDamage, double Percentage);

/// <summary>
/// Enemy type dropdown option for UI binding.
/// </summary>
/// <param name="Type">The enemy type value (null for "All").</param>
/// <param name="Display">The display text for the dropdown.</param>
public record EnemyTypeOption(EnemyType? Type, string Display);

/// <summary>
/// Sort option for UI binding.
/// </summary>
/// <param name="SortBy">The sort field.</param>
/// <param name="Display">The display text for the dropdown.</param>
public record SortOption(EnemySortBy SortBy, string Display);

/// <summary>
/// Simple ICommand implementation for MVVM commands.
/// </summary>
/// <remarks>
/// Supports both synchronous and asynchronous command execution.
/// Prevents concurrent execution of the same command.
/// </remarks>
internal sealed class RelayCommand : ICommand
{
    private readonly Func<Task>? _executeAsync;
    private readonly Action? _execute;
    private bool _isExecuting;

    /// <summary>
    /// Creates an async relay command.
    /// </summary>
    public RelayCommand(Func<Task> executeAsync)
    {
        _executeAsync = executeAsync ?? throw new ArgumentNullException(nameof(executeAsync));
    }

    /// <summary>
    /// Creates a synchronous relay command.
    /// </summary>
    public RelayCommand(Action execute)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
    }

    /// <inheritdoc/>
    public event EventHandler? CanExecuteChanged;

    /// <inheritdoc/>
    public bool CanExecute(object? parameter) => !_isExecuting;

    /// <inheritdoc/>
    public async void Execute(object? parameter)
    {
        if (_isExecuting) return;

        _isExecuting = true;
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);

        try
        {
            if (_execute != null)
            {
                _execute();
            }
            else if (_executeAsync != null)
            {
                await _executeAsync().ConfigureAwait(false);
            }
        }
        finally
        {
            _isExecuting = false;
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}

#endregion
