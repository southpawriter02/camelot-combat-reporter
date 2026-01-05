using System.Text.Json;
using CamelotCombatReporter.Core.Models;
using Microsoft.Extensions.Logging;

namespace CamelotCombatReporter.Core.ServerProfiles;

/// <summary>
/// Service for managing server profiles.
/// </summary>
public class ServerProfileService
{
    private readonly ILogger<ServerProfileService>? _logger;
    private readonly string _profilesDirectory;
    private readonly Dictionary<string, ServerProfile> _profiles = new();
    private string? _activeProfileId;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Event raised when the active profile changes.
    /// </summary>
    public event EventHandler<ServerProfile>? ProfileChanged;

    /// <summary>
    /// Gets all available profiles.
    /// </summary>
    public IReadOnlyList<ServerProfile> AllProfiles => _profiles.Values.ToList();

    /// <summary>
    /// Gets the currently active profile.
    /// </summary>
    public ServerProfile? ActiveProfile =>
        _activeProfileId != null && _profiles.TryGetValue(_activeProfileId, out var profile)
            ? profile
            : null;

    /// <summary>
    /// Creates a new ServerProfileService.
    /// </summary>
    /// <param name="profilesDirectory">Directory to store custom profiles.</param>
    /// <param name="logger">Optional logger.</param>
    public ServerProfileService(string? profilesDirectory = null, ILogger<ServerProfileService>? logger = null)
    {
        _logger = logger;
        _profilesDirectory = profilesDirectory ??
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "CamelotCombatReporter",
                "server-profiles");

        InitializeBuiltInProfiles();
    }

    /// <summary>
    /// Sets the active profile.
    /// </summary>
    /// <param name="profileId">The profile ID to activate.</param>
    public void SetActiveProfile(string profileId)
    {
        if (_profiles.TryGetValue(profileId, out var profile))
        {
            _activeProfileId = profileId;
            ProfileChanged?.Invoke(this, profile);
            _logger?.LogInformation("Active profile set to: {ProfileName}", profile.Name);
        }
        else
        {
            _logger?.LogWarning("Profile not found: {ProfileId}", profileId);
        }
    }

    /// <summary>
    /// Gets a profile by ID.
    /// </summary>
    public ServerProfile? GetProfile(string profileId)
    {
        return _profiles.TryGetValue(profileId, out var profile) ? profile : null;
    }

    /// <summary>
    /// Gets built-in profiles.
    /// </summary>
    public IReadOnlyList<ServerProfile> GetBuiltInProfiles()
    {
        return _profiles.Values.Where(p => p.IsBuiltIn).ToList();
    }

    /// <summary>
    /// Gets custom profiles.
    /// </summary>
    public IReadOnlyList<ServerProfile> GetCustomProfiles()
    {
        return _profiles.Values.Where(p => !p.IsBuiltIn).ToList();
    }

    /// <summary>
    /// Creates a custom profile.
    /// </summary>
    public ServerProfile CreateCustomProfile(
        string name,
        ServerType baseType,
        IEnumerable<CharacterClass> availableClasses,
        bool hasMasterLevels = false,
        bool hasArtifacts = false,
        bool hasChampionLevels = false,
        bool hasMaulers = false)
    {
        var id = Guid.NewGuid().ToString("N")[..8];
        var now = DateTime.UtcNow;

        var profile = new ServerProfile(
            Id: id,
            Name: name,
            BaseType: baseType,
            AvailableClasses: availableClasses.ToHashSet(),
            HasMasterLevels: hasMasterLevels,
            HasArtifacts: hasArtifacts,
            HasChampionLevels: hasChampionLevels,
            HasMaulers: hasMaulers,
            IsBuiltIn: false,
            CreatedUtc: now,
            ModifiedUtc: now
        );

        _profiles[id] = profile;
        _logger?.LogInformation("Created custom profile: {ProfileName}", name);

        return profile;
    }

    /// <summary>
    /// Deletes a custom profile.
    /// </summary>
    public bool DeleteProfile(string profileId)
    {
        if (_profiles.TryGetValue(profileId, out var profile))
        {
            if (profile.IsBuiltIn)
            {
                _logger?.LogWarning("Cannot delete built-in profile: {ProfileName}", profile.Name);
                return false;
            }

            _profiles.Remove(profileId);
            if (_activeProfileId == profileId)
            {
                _activeProfileId = "live"; // Fall back to Live
            }

            _logger?.LogInformation("Deleted custom profile: {ProfileName}", profile.Name);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Exports a profile to JSON.
    /// </summary>
    public string ExportProfile(string profileId)
    {
        if (_profiles.TryGetValue(profileId, out var profile))
        {
            var exportData = new ProfileExportData(
                profile.Name,
                profile.BaseType.ToString(),
                profile.AvailableClasses.Select(c => c.ToString()).ToList(),
                profile.HasMasterLevels,
                profile.HasArtifacts,
                profile.HasChampionLevels,
                profile.HasMaulers
            );

            return JsonSerializer.Serialize(exportData, JsonOptions);
        }

        throw new ArgumentException($"Profile not found: {profileId}");
    }

    /// <summary>
    /// Imports a profile from JSON.
    /// </summary>
    public ServerProfile ImportProfile(string json)
    {
        var data = JsonSerializer.Deserialize<ProfileExportData>(json, JsonOptions)
            ?? throw new ArgumentException("Invalid profile JSON");

        var baseType = Enum.Parse<ServerType>(data.BaseType);
        var classes = data.AvailableClasses
            .Select(c => Enum.Parse<CharacterClass>(c))
            .ToHashSet();

        return CreateCustomProfile(
            data.Name,
            baseType,
            classes,
            data.HasMasterLevels,
            data.HasArtifacts,
            data.HasChampionLevels,
            data.HasMaulers
        );
    }

    /// <summary>
    /// Saves custom profiles to disk.
    /// </summary>
    public async Task SaveCustomProfilesAsync()
    {
        Directory.CreateDirectory(_profilesDirectory);

        var customProfiles = GetCustomProfiles();
        foreach (var profile in customProfiles)
        {
            var filePath = Path.Combine(_profilesDirectory, $"{profile.Id}.json");
            var json = ExportProfile(profile.Id);
            await File.WriteAllTextAsync(filePath, json);
        }

        // Save active profile reference
        var activeFile = Path.Combine(_profilesDirectory, "active-profile.json");
        await File.WriteAllTextAsync(activeFile,
            JsonSerializer.Serialize(new { activeProfileId = _activeProfileId }, JsonOptions));

        _logger?.LogInformation("Saved {Count} custom profiles", customProfiles.Count);
    }

    /// <summary>
    /// Loads custom profiles from disk.
    /// </summary>
    public async Task LoadCustomProfilesAsync()
    {
        if (!Directory.Exists(_profilesDirectory))
            return;

        var files = Directory.GetFiles(_profilesDirectory, "*.json")
            .Where(f => !Path.GetFileName(f).StartsWith("active-profile"));

        foreach (var file in files)
        {
            try
            {
                var json = await File.ReadAllTextAsync(file);
                ImportProfile(json);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to load profile from {File}", file);
            }
        }

        // Load active profile
        var activeFile = Path.Combine(_profilesDirectory, "active-profile.json");
        if (File.Exists(activeFile))
        {
            try
            {
                var json = await File.ReadAllTextAsync(activeFile);
                var data = JsonSerializer.Deserialize<JsonElement>(json);
                if (data.TryGetProperty("activeProfileId", out var idProp))
                {
                    _activeProfileId = idProp.GetString();
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to load active profile setting");
            }
        }

        _logger?.LogInformation("Loaded custom profiles from disk");
    }

    private void InitializeBuiltInProfiles()
    {
        // Classic profile (original 7 classes per realm)
        _profiles["classic"] = CreateBuiltInProfile(
            "classic",
            "Classic (Original)",
            ServerType.Classic,
            GetClassicClasses(),
            hasMasterLevels: false,
            hasArtifacts: false,
            hasChampionLevels: false,
            hasMaulers: false
        );

        // Shrouded Isles profile
        _profiles["si"] = CreateBuiltInProfile(
            "si",
            "Shrouded Isles",
            ServerType.ShroudedIsles,
            GetShroudedIslesClasses(),
            hasMasterLevels: false,
            hasArtifacts: false,
            hasChampionLevels: false,
            hasMaulers: false
        );

        // Trials of Atlantis profile
        _profiles["toa"] = CreateBuiltInProfile(
            "toa",
            "Trials of Atlantis",
            ServerType.TrialsOfAtlantis,
            GetShroudedIslesClasses(), // Same classes as SI
            hasMasterLevels: true,
            hasArtifacts: true,
            hasChampionLevels: true,
            hasMaulers: false
        );

        // New Frontiers profile
        _profiles["nf"] = CreateBuiltInProfile(
            "nf",
            "New Frontiers",
            ServerType.NewFrontiers,
            GetNewFrontiersClasses(),
            hasMasterLevels: true,
            hasArtifacts: true,
            hasChampionLevels: true,
            hasMaulers: true
        );

        // Live profile (all features)
        _profiles["live"] = CreateBuiltInProfile(
            "live",
            "Live",
            ServerType.Live,
            GetAllClasses(),
            hasMasterLevels: true,
            hasArtifacts: true,
            hasChampionLevels: true,
            hasMaulers: true
        );

        // Default to Live
        _activeProfileId = "live";

        _logger?.LogInformation("Initialized {Count} built-in profiles", _profiles.Count);
    }

    private static ServerProfile CreateBuiltInProfile(
        string id,
        string name,
        ServerType serverType,
        HashSet<CharacterClass> classes,
        bool hasMasterLevels,
        bool hasArtifacts,
        bool hasChampionLevels,
        bool hasMaulers)
    {
        return new ServerProfile(
            Id: id,
            Name: name,
            BaseType: serverType,
            AvailableClasses: classes,
            HasMasterLevels: hasMasterLevels,
            HasArtifacts: hasArtifacts,
            HasChampionLevels: hasChampionLevels,
            HasMaulers: hasMaulers,
            IsBuiltIn: true,
            CreatedUtc: DateTime.MinValue,
            ModifiedUtc: DateTime.MinValue
        );
    }

    private static HashSet<CharacterClass> GetClassicClasses()
    {
        return new HashSet<CharacterClass>
        {
            // Albion (7 original)
            CharacterClass.Armsman, CharacterClass.Cabalist, CharacterClass.Cleric,
            CharacterClass.Friar, CharacterClass.Infiltrator, CharacterClass.Mercenary,
            CharacterClass.Minstrel, CharacterClass.Paladin, CharacterClass.Scout,
            CharacterClass.Sorcerer, CharacterClass.Theurgist, CharacterClass.Wizard,

            // Midgard (7 original)
            CharacterClass.Berserker, CharacterClass.Healer, CharacterClass.Hunter,
            CharacterClass.Runemaster, CharacterClass.Shadowblade, CharacterClass.Shaman,
            CharacterClass.Skald, CharacterClass.Spiritmaster, CharacterClass.Thane,
            CharacterClass.Warrior,

            // Hibernia (7 original)
            CharacterClass.Bard, CharacterClass.Blademaster, CharacterClass.Champion,
            CharacterClass.Druid, CharacterClass.Eldritch, CharacterClass.Enchanter,
            CharacterClass.Hero, CharacterClass.Mentalist, CharacterClass.Nightshade,
            CharacterClass.Ranger, CharacterClass.Warden
        };
    }

    private static HashSet<CharacterClass> GetShroudedIslesClasses()
    {
        var classes = GetClassicClasses();

        // Add SI classes
        classes.Add(CharacterClass.Necromancer);
        classes.Add(CharacterClass.Reaver);
        classes.Add(CharacterClass.Heretic);
        classes.Add(CharacterClass.Savage);
        classes.Add(CharacterClass.Bonedancer);
        classes.Add(CharacterClass.Valkyrie);
        classes.Add(CharacterClass.Warlock);
        classes.Add(CharacterClass.Animist);
        classes.Add(CharacterClass.Valewalker);
        classes.Add(CharacterClass.Vampiir);
        classes.Add(CharacterClass.Bainshee);

        return classes;
    }

    private static HashSet<CharacterClass> GetNewFrontiersClasses()
    {
        var classes = GetShroudedIslesClasses();

        // Add Maulers
        classes.Add(CharacterClass.MaulerAlb);
        classes.Add(CharacterClass.MaulerMid);
        classes.Add(CharacterClass.MaulerHib);

        return classes;
    }

    private static HashSet<CharacterClass> GetAllClasses()
    {
        return Enum.GetValues<CharacterClass>().ToHashSet();
    }

    private record ProfileExportData(
        string Name,
        string BaseType,
        List<string> AvailableClasses,
        bool HasMasterLevels,
        bool HasArtifacts,
        bool HasChampionLevels,
        bool HasMaulers
    );
}
