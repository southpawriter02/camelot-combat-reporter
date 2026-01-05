using System.Text.Json;
using System.Text.Json.Serialization;
using CamelotCombatReporter.Core.RealmAbilities.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace CamelotCombatReporter.Core.RealmAbilities;

/// <summary>
/// Implementation of the realm ability database that loads from JSON.
/// </summary>
public class RealmAbilityDatabase : IRealmAbilityDatabase
{
    private readonly string? _jsonFilePath;
    private readonly ILogger<RealmAbilityDatabase> _logger;
    private List<RealmAbility> _abilities = new();
    private Dictionary<string, RealmAbility> _byId = new(StringComparer.OrdinalIgnoreCase);
    private Dictionary<string, RealmAbility> _byName = new(StringComparer.OrdinalIgnoreCase);
    private Dictionary<string, RealmAbility> _byInternalName = new(StringComparer.OrdinalIgnoreCase);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    /// <summary>
    /// Creates a new database instance.
    /// </summary>
    /// <param name="jsonFilePath">Path to the JSON database file.</param>
    /// <param name="logger">Optional logger instance.</param>
    public RealmAbilityDatabase(string? jsonFilePath = null, ILogger<RealmAbilityDatabase>? logger = null)
    {
        _jsonFilePath = jsonFilePath;
        _logger = logger ?? NullLogger<RealmAbilityDatabase>.Instance;
    }

    /// <inheritdoc/>
    public IReadOnlyList<RealmAbility> AllAbilities => _abilities;

    /// <inheritdoc/>
    public int Count => _abilities.Count;

    /// <inheritdoc/>
    public RealmAbility? GetById(string id) =>
        _byId.TryGetValue(id, out var ability) ? ability : null;

    /// <inheritdoc/>
    public RealmAbility? GetByName(string name) =>
        _byName.TryGetValue(name, out var ability) ? ability : null;

    /// <inheritdoc/>
    public RealmAbility? GetByInternalName(string internalName) =>
        _byInternalName.TryGetValue(internalName, out var ability) ? ability : null;

    /// <inheritdoc/>
    public IReadOnlyList<RealmAbility> GetByType(RealmAbilityType type) =>
        _abilities.Where(a => a.Type == type).ToList();

    /// <inheritdoc/>
    public IReadOnlyList<RealmAbility> GetByRealm(RealmAvailability realm) =>
        _abilities.Where(a => a.RealmAvailability == realm || a.RealmAvailability == RealmAvailability.All).ToList();

    /// <inheritdoc/>
    public IReadOnlyList<RealmAbility> GetByEra(GameEra maxEra) =>
        _abilities.Where(a => a.IntroducedIn <= maxEra).ToList();

    /// <inheritdoc/>
    public IReadOnlyList<RealmAbility> GetByRealmAndEra(RealmAvailability realm, GameEra maxEra) =>
        _abilities
            .Where(a => (a.RealmAvailability == realm || a.RealmAvailability == RealmAvailability.All) && a.IntroducedIn <= maxEra)
            .ToList();

    /// <inheritdoc/>
    public async Task ReloadAsync()
    {
        if (string.IsNullOrEmpty(_jsonFilePath))
        {
            _logger.LogWarning("No JSON file path specified for realm ability database");
            return;
        }

        if (!File.Exists(_jsonFilePath))
        {
            _logger.LogWarning("Realm ability database file not found: {Path}", _jsonFilePath);
            return;
        }

        try
        {
            var json = await File.ReadAllTextAsync(_jsonFilePath);
            var data = JsonSerializer.Deserialize<RealmAbilityDatabaseJson>(json, JsonOptions);

            if (data?.Abilities == null)
            {
                _logger.LogWarning("No abilities found in database file");
                return;
            }

            var abilities = new List<RealmAbility>();
            foreach (var dto in data.Abilities)
            {
                var ability = ConvertFromDto(dto);
                if (ability != null)
                {
                    abilities.Add(ability);
                }
            }

            _abilities = abilities;
            RebuildIndexes();

            _logger.LogInformation("Loaded {Count} realm abilities from database", _abilities.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load realm ability database from {Path}", _jsonFilePath);
        }
    }

    /// <summary>
    /// Loads abilities from a JSON string (for testing or embedded resources).
    /// </summary>
    /// <param name="json">The JSON content.</param>
    public void LoadFromJson(string json)
    {
        try
        {
            var data = JsonSerializer.Deserialize<RealmAbilityDatabaseJson>(json, JsonOptions);

            if (data?.Abilities == null)
            {
                _logger.LogWarning("No abilities found in JSON");
                return;
            }

            var abilities = new List<RealmAbility>();
            foreach (var dto in data.Abilities)
            {
                var ability = ConvertFromDto(dto);
                if (ability != null)
                {
                    abilities.Add(ability);
                }
            }

            _abilities = abilities;
            RebuildIndexes();

            _logger.LogInformation("Loaded {Count} realm abilities from JSON", _abilities.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse realm ability JSON");
        }
    }

    private void RebuildIndexes()
    {
        _byId = _abilities.ToDictionary(a => a.Id, StringComparer.OrdinalIgnoreCase);
        _byName = new Dictionary<string, RealmAbility>(StringComparer.OrdinalIgnoreCase);
        _byInternalName = new Dictionary<string, RealmAbility>(StringComparer.OrdinalIgnoreCase);

        foreach (var ability in _abilities)
        {
            _byName.TryAdd(ability.Name, ability);
            _byInternalName.TryAdd(ability.InternalName, ability);
        }
    }

    private static RealmAbility? ConvertFromDto(RealmAbilityDto dto)
    {
        if (string.IsNullOrEmpty(dto.Id) || string.IsNullOrEmpty(dto.Name))
            return null;

        TimeSpan? cooldown = null;
        if (!string.IsNullOrEmpty(dto.BaseCooldown))
        {
            if (TimeSpan.TryParse(dto.BaseCooldown, out var parsed))
                cooldown = parsed;
        }

        var effectDescriptions = dto.EffectDescriptions?
            .Where(kvp => int.TryParse(kvp.Key, out _))
            .ToDictionary(kvp => int.Parse(kvp.Key), kvp => kvp.Value)
            ?? new Dictionary<int, string>();

        return new RealmAbility(
            Id: dto.Id,
            Name: dto.Name,
            InternalName: dto.InternalName ?? dto.Name,
            RealmAvailability: dto.RealmAvailability,
            Type: dto.Type,
            MaxLevel: dto.MaxLevel,
            RealmPointCosts: dto.RealmPointCosts ?? Array.Empty<int>(),
            BaseCooldown: cooldown,
            Prerequisites: dto.Prerequisites ?? Array.Empty<string>(),
            Description: dto.Description ?? string.Empty,
            EffectDescriptions: effectDescriptions,
            IntroducedIn: dto.IntroducedIn,
            IsTimer: dto.IsTimer,
            SharedCooldownGroup: dto.SharedCooldownGroup
        );
    }

    #region JSON DTOs

    private class RealmAbilityDatabaseJson
    {
        public string? Version { get; set; }
        public string? Description { get; set; }
        public List<RealmAbilityDto>? Abilities { get; set; }
    }

    private class RealmAbilityDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? InternalName { get; set; }
        public RealmAvailability RealmAvailability { get; set; }
        public RealmAbilityType Type { get; set; }
        public int MaxLevel { get; set; }
        public int[]? RealmPointCosts { get; set; }
        public string? BaseCooldown { get; set; }
        public string[]? Prerequisites { get; set; }
        public string? Description { get; set; }
        public Dictionary<string, string>? EffectDescriptions { get; set; }
        public GameEra IntroducedIn { get; set; }
        public bool IsTimer { get; set; } = true;
        public string? SharedCooldownGroup { get; set; }
    }

    #endregion
}
