using System.Text.Json;
using System.Text.Json.Serialization;
using CamelotCombatReporter.Core.CharacterBuilding.Models;
using CamelotCombatReporter.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace CamelotCombatReporter.Core.CharacterBuilding.Services;

/// <summary>
/// Manages character profiles with JSON file storage.
/// Thread-safe implementation following CrossRealmStatisticsService patterns.
/// </summary>
public class CharacterProfileService : ICharacterProfileService, IDisposable
{
    private readonly string _profilesDirectory;
    private readonly string _indexFilePath;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly ILogger<CharacterProfileService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    private List<ProfileIndexEntry> _index = [];
    private bool _indexLoaded;

    /// <summary>
    /// Creates a new CharacterProfileService with default storage location.
    /// </summary>
    public CharacterProfileService(ILogger<CharacterProfileService>? logger = null)
        : this(GetDefaultProfilesDirectory(), logger)
    {
    }

    /// <summary>
    /// Creates a new CharacterProfileService with custom storage location.
    /// </summary>
    public CharacterProfileService(string profilesDirectory, ILogger<CharacterProfileService>? logger = null)
    {
        _profilesDirectory = profilesDirectory;
        _indexFilePath = Path.Combine(profilesDirectory, "profiles-index.json");
        _logger = logger ?? NullLogger<CharacterProfileService>.Instance;
        
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = { new JsonStringEnumConverter() }
        };

        EnsureDirectoryExists();
    }

    private static string GetDefaultProfilesDirectory()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(appData, "CamelotCombatReporter", "profiles");
    }

    private void EnsureDirectoryExists()
    {
        if (!Directory.Exists(_profilesDirectory))
        {
            Directory.CreateDirectory(_profilesDirectory);
            _logger.LogInformation("Created profiles directory: {Directory}", _profilesDirectory);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Profile CRUD
    // ─────────────────────────────────────────────────────────────────────────

    public async Task<CharacterProfile> CreateProfileAsync(CharacterProfile profile, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            await EnsureIndexLoadedAsync(cancellationToken);

            // Ensure unique ID
            var newProfile = profile with { Id = Guid.NewGuid(), CreatedUtc = DateTime.UtcNow, LastUpdatedUtc = DateTime.UtcNow };

            // Save profile file
            await SaveProfileToFileAsync(newProfile, cancellationToken);

            // Update index
            _index.Add(new ProfileIndexEntry(newProfile.Id, newProfile.Name, newProfile.Realm, newProfile.Class));
            await SaveIndexAsync(cancellationToken);

            _logger.LogInformation("Created profile: {Name} ({Class}, {Realm})", newProfile.Name, newProfile.Class, newProfile.Realm);
            return newProfile;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<CharacterProfile?> GetProfileAsync(Guid profileId, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            return await LoadProfileFromFileAsync(profileId, cancellationToken);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<IReadOnlyList<CharacterProfile>> GetAllProfilesAsync(CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            await EnsureIndexLoadedAsync(cancellationToken);

            var profiles = new List<CharacterProfile>();
            foreach (var entry in _index)
            {
                var profile = await LoadProfileFromFileAsync(entry.Id, cancellationToken);
                if (profile != null)
                {
                    profiles.Add(profile);
                }
            }
            return profiles;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<IReadOnlyList<CharacterProfile>> GetProfilesByRealmAsync(Realm realm, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            await EnsureIndexLoadedAsync(cancellationToken);

            var profiles = new List<CharacterProfile>();
            foreach (var entry in _index.Where(e => e.Realm == realm))
            {
                var profile = await LoadProfileFromFileAsync(entry.Id, cancellationToken);
                if (profile != null)
                {
                    profiles.Add(profile);
                }
            }
            return profiles;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task UpdateProfileAsync(CharacterProfile profile, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            await EnsureIndexLoadedAsync(cancellationToken);

            var updatedProfile = profile with { LastUpdatedUtc = DateTime.UtcNow };
            await SaveProfileToFileAsync(updatedProfile, cancellationToken);

            // Update index entry
            var indexEntry = _index.FirstOrDefault(e => e.Id == profile.Id);
            if (indexEntry != null)
            {
                var idx = _index.IndexOf(indexEntry);
                _index[idx] = new ProfileIndexEntry(profile.Id, profile.Name, profile.Realm, profile.Class);
                await SaveIndexAsync(cancellationToken);
            }

            _logger.LogInformation("Updated profile: {Name}", profile.Name);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<bool> DeleteProfileAsync(Guid profileId, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            await EnsureIndexLoadedAsync(cancellationToken);

            var filePath = GetProfileFilePath(profileId);
            if (!File.Exists(filePath))
            {
                return false;
            }

            File.Delete(filePath);

            // Remove from index
            var entry = _index.FirstOrDefault(e => e.Id == profileId);
            if (entry != null)
            {
                _index.Remove(entry);
                await SaveIndexAsync(cancellationToken);
            }

            _logger.LogInformation("Deleted profile: {ProfileId}", profileId);
            return true;
        }
        finally
        {
            _lock.Release();
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Build Management
    // ─────────────────────────────────────────────────────────────────────────

    public async Task<CharacterBuild> CreateBuildAsync(Guid profileId, CharacterBuild build, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            var profile = await LoadProfileFromFileAsync(profileId, cancellationToken)
                ?? throw new InvalidOperationException($"Profile {profileId} not found");

            var newBuild = build with { Id = Guid.NewGuid(), CreatedUtc = DateTime.UtcNow };

            var updatedHistory = profile.BuildHistory.Append(newBuild).ToList();
            var updatedProfile = profile with
            {
                BuildHistory = updatedHistory,
                ActiveBuild = newBuild,
                LastUpdatedUtc = DateTime.UtcNow
            };

            await SaveProfileToFileAsync(updatedProfile, cancellationToken);

            _logger.LogInformation("Created build '{BuildName}' for profile {ProfileId}", build.Name, profileId);
            return newBuild;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<CharacterBuild> CloneBuildAsync(Guid profileId, Guid buildId, string newName, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            var profile = await LoadProfileFromFileAsync(profileId, cancellationToken)
                ?? throw new InvalidOperationException($"Profile {profileId} not found");

            var sourceBuild = profile.BuildHistory.FirstOrDefault(b => b.Id == buildId)
                ?? throw new InvalidOperationException($"Build {buildId} not found");

            var clonedBuild = sourceBuild with
            {
                Id = Guid.NewGuid(),
                Name = newName,
                CreatedUtc = DateTime.UtcNow,
                PerformanceMetrics = null // Reset metrics for new build
            };

            var updatedHistory = profile.BuildHistory.Append(clonedBuild).ToList();
            var updatedProfile = profile with { BuildHistory = updatedHistory, LastUpdatedUtc = DateTime.UtcNow };

            await SaveProfileToFileAsync(updatedProfile, cancellationToken);

            _logger.LogInformation("Cloned build '{SourceName}' to '{NewName}'", sourceBuild.Name, newName);
            return clonedBuild;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<CharacterBuild> UpdateBuildAsync(Guid profileId, CharacterBuild build, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            var profile = await LoadProfileFromFileAsync(profileId, cancellationToken)
                ?? throw new InvalidOperationException($"Profile {profileId} not found");

            // Builds are immutable - create new version
            var newBuild = build with { Id = Guid.NewGuid(), CreatedUtc = DateTime.UtcNow };

            var updatedHistory = profile.BuildHistory.Append(newBuild).ToList();
            var updatedProfile = profile with
            {
                BuildHistory = updatedHistory,
                ActiveBuild = profile.ActiveBuild?.Id == build.Id ? newBuild : profile.ActiveBuild,
                LastUpdatedUtc = DateTime.UtcNow
            };

            await SaveProfileToFileAsync(updatedProfile, cancellationToken);

            _logger.LogInformation("Updated build '{BuildName}' (new version created)", build.Name);
            return newBuild;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task SetActiveBuildAsync(Guid profileId, Guid buildId, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            var profile = await LoadProfileFromFileAsync(profileId, cancellationToken)
                ?? throw new InvalidOperationException($"Profile {profileId} not found");

            var build = profile.BuildHistory.FirstOrDefault(b => b.Id == buildId)
                ?? throw new InvalidOperationException($"Build {buildId} not found");

            var updatedProfile = profile with { ActiveBuild = build, LastUpdatedUtc = DateTime.UtcNow };
            await SaveProfileToFileAsync(updatedProfile, cancellationToken);

            _logger.LogInformation("Set active build to '{BuildName}' for profile {ProfileId}", build.Name, profileId);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<IReadOnlyList<CharacterBuild>> GetBuildHistoryAsync(Guid profileId, CancellationToken cancellationToken = default)
    {
        var profile = await GetProfileAsync(profileId, cancellationToken);
        return profile?.BuildHistory ?? [];
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Session Attachment
    // ─────────────────────────────────────────────────────────────────────────

    public async Task AttachSessionAsync(Guid profileId, Guid sessionId, Guid? buildId = null, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            var profile = await LoadProfileFromFileAsync(profileId, cancellationToken)
                ?? throw new InvalidOperationException($"Profile {profileId} not found");

            if (profile.AttachedSessionIds.Contains(sessionId))
            {
                return; // Already attached
            }

            var updatedSessions = profile.AttachedSessionIds.Append(sessionId).ToList();
            var updatedProfile = profile with { AttachedSessionIds = updatedSessions, LastUpdatedUtc = DateTime.UtcNow };

            await SaveProfileToFileAsync(updatedProfile, cancellationToken);

            _logger.LogInformation("Attached session {SessionId} to profile {ProfileId}", sessionId, profileId);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task DetachSessionAsync(Guid profileId, Guid sessionId, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            var profile = await LoadProfileFromFileAsync(profileId, cancellationToken)
                ?? throw new InvalidOperationException($"Profile {profileId} not found");

            var updatedSessions = profile.AttachedSessionIds.Where(id => id != sessionId).ToList();
            var updatedProfile = profile with { AttachedSessionIds = updatedSessions, LastUpdatedUtc = DateTime.UtcNow };

            await SaveProfileToFileAsync(updatedProfile, cancellationToken);

            _logger.LogInformation("Detached session {SessionId} from profile {ProfileId}", sessionId, profileId);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<IReadOnlyList<Guid>> GetAttachedSessionIdsAsync(Guid profileId, CancellationToken cancellationToken = default)
    {
        var profile = await GetProfileAsync(profileId, cancellationToken);
        return profile?.AttachedSessionIds ?? [];
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Auto-Matching
    // ─────────────────────────────────────────────────────────────────────────

    public async Task<CharacterProfile?> SuggestProfileForSessionAsync(ExtendedCombatStatistics session, CancellationToken cancellationToken = default)
    {
        var profiles = await GetAllProfilesAsync(cancellationToken);
        if (profiles.Count == 0)
        {
            return null;
        }

        var scored = profiles
            .Select(p => new { Profile = p, Score = CalculateMatchScore(p, session) })
            .Where(x => x.Score > 0.5)
            .OrderByDescending(x => x.Score)
            .FirstOrDefault();

        if (scored != null)
        {
            _logger.LogDebug("Suggested profile '{Name}' for session (score: {Score:F2})", scored.Profile.Name, scored.Score);
        }

        return scored?.Profile;
    }

    private static double CalculateMatchScore(CharacterProfile profile, ExtendedCombatStatistics session)
    {
        double score = 0;

        // Exact class match: +0.4
        if (profile.Class == session.Character.Class)
        {
            score += 0.4;
        }

        // Realm match: +0.2
        if (profile.Realm == session.Character.Realm)
        {
            score += 0.2;
        }

        // Name similarity (simple case-insensitive contains): +0.3
        if (!string.IsNullOrEmpty(session.Character.Name))
        {
            if (profile.Name.Equals(session.Character.Name, StringComparison.OrdinalIgnoreCase))
            {
                score += 0.3;
            }
            else if (profile.Name.Contains(session.Character.Name, StringComparison.OrdinalIgnoreCase) ||
                     session.Character.Name.Contains(profile.Name, StringComparison.OrdinalIgnoreCase))
            {
                score += 0.15;
            }
        }

        // Recent activity bonus: +0.1
        if (profile.AttachedSessionIds.Count > 0)
        {
            score += 0.1;
        }

        return score;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Import/Export
    // ─────────────────────────────────────────────────────────────────────────

    public async Task<string> ExportProfileAsync(Guid profileId, CancellationToken cancellationToken = default)
    {
        var profile = await GetProfileAsync(profileId, cancellationToken)
            ?? throw new InvalidOperationException($"Profile {profileId} not found");

        return JsonSerializer.Serialize(profile, _jsonOptions);
    }

    public async Task<ProfileExportResult> ExportProfileWithOptionsAsync(
        Guid profileId, 
        ProfileExportOptions options,
        CancellationToken cancellationToken = default)
    {
        var profile = await GetProfileAsync(profileId, cancellationToken)
            ?? throw new InvalidOperationException($"Profile {profileId} not found");

        // Apply options to create export version
        var exportProfile = profile;
        var wasAnonymized = false;

        // Anonymize if requested
        if (options.AnonymizeCharacterName)
        {
            exportProfile = exportProfile with { Name = "Anonymous" };
            wasAnonymized = true;
        }

        // Apply custom name if provided
        if (!string.IsNullOrWhiteSpace(options.CustomExportName))
        {
            exportProfile = exportProfile with { Name = options.CustomExportName };
        }

        // Handle build history
        var buildCount = exportProfile.BuildHistory.Count;
        if (!options.IncludeBuildHistory)
        {
            // Keep only active build
            exportProfile = exportProfile with 
            { 
                BuildHistory = exportProfile.ActiveBuild != null 
                    ? [exportProfile.ActiveBuild] 
                    : [] 
            };
            buildCount = exportProfile.BuildHistory.Count;
        }

        // Handle session references
        var sessionCount = exportProfile.AttachedSessionIds.Count;
        if (!options.IncludeSessionReferences)
        {
            exportProfile = exportProfile with { AttachedSessionIds = [] };
            sessionCount = 0;
        }

        // Handle performance metrics (strip from builds if not included)
        if (!options.IncludePerformanceMetrics && exportProfile.BuildHistory.Count > 0)
        {
            var cleanedBuilds = exportProfile.BuildHistory
                .Select(b => b with { PerformanceMetrics = null })
                .ToList();
            exportProfile = exportProfile with { BuildHistory = cleanedBuilds };
            
            if (exportProfile.ActiveBuild != null)
            {
                exportProfile = exportProfile with 
                { 
                    ActiveBuild = exportProfile.ActiveBuild with { PerformanceMetrics = null } 
                };
            }
        }

        var json = JsonSerializer.Serialize(exportProfile, _jsonOptions);
        var sanitizedName = string.Join("", (options.CustomExportName ?? profile.Name)
            .Split(Path.GetInvalidFileNameChars()));

        _logger.LogInformation(
            "Exported profile '{Name}' with {BuildCount} builds, {SessionCount} sessions, anonymized: {Anon}",
            profile.Name, buildCount, sessionCount, wasAnonymized);

        return new ProfileExportResult
        {
            Json = json,
            SuggestedFileName = $"{sanitizedName}_{DateTime.UtcNow:yyyyMMdd}.ccr-profile",
            SizeBytes = System.Text.Encoding.UTF8.GetByteCount(json),
            BuildCount = buildCount,
            SessionReferenceCount = sessionCount,
            WasAnonymized = wasAnonymized
        };
    }

    public async Task<CharacterProfile> ImportProfileAsync(string json, CancellationToken cancellationToken = default)
    {
        var imported = JsonSerializer.Deserialize<CharacterProfile>(json, _jsonOptions)
            ?? throw new InvalidOperationException("Failed to deserialize profile");

        // Create with new ID to avoid conflicts
        var newProfile = imported with { Id = Guid.NewGuid(), CreatedUtc = DateTime.UtcNow, LastUpdatedUtc = DateTime.UtcNow };
        return await CreateProfileAsync(newProfile, cancellationToken);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Utilities
    // ─────────────────────────────────────────────────────────────────────────

    public async Task<int> GetProfileCountAsync(CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            await EnsureIndexLoadedAsync(cancellationToken);
            return _index.Count;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task RebuildIndexAsync(CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            _index.Clear();

            var files = Directory.GetFiles(_profilesDirectory, "*.json")
                .Where(f => !f.EndsWith("profiles-index.json"));

            foreach (var file in files)
            {
                try
                {
                    var json = await File.ReadAllTextAsync(file, cancellationToken);
                    var profile = JsonSerializer.Deserialize<CharacterProfile>(json, _jsonOptions);
                    if (profile != null)
                    {
                        _index.Add(new ProfileIndexEntry(profile.Id, profile.Name, profile.Realm, profile.Class));
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to parse profile file: {File}", file);
                }
            }

            await SaveIndexAsync(cancellationToken);
            _indexLoaded = true;

            _logger.LogInformation("Rebuilt profile index with {Count} profiles", _index.Count);
        }
        finally
        {
            _lock.Release();
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // File Operations
    // ─────────────────────────────────────────────────────────────────────────

    private string GetProfileFilePath(Guid profileId) =>
        Path.Combine(_profilesDirectory, $"{profileId}.json");

    private async Task<CharacterProfile?> LoadProfileFromFileAsync(Guid profileId, CancellationToken cancellationToken)
    {
        var filePath = GetProfileFilePath(profileId);
        if (!File.Exists(filePath))
        {
            return null;
        }

        try
        {
            var json = await File.ReadAllTextAsync(filePath, cancellationToken);
            return JsonSerializer.Deserialize<CharacterProfile>(json, _jsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load profile: {ProfileId}", profileId);
            return null;
        }
    }

    private async Task SaveProfileToFileAsync(CharacterProfile profile, CancellationToken cancellationToken)
    {
        var filePath = GetProfileFilePath(profile.Id);
        var json = JsonSerializer.Serialize(profile, _jsonOptions);
        await File.WriteAllTextAsync(filePath, json, cancellationToken);
    }

    private async Task EnsureIndexLoadedAsync(CancellationToken cancellationToken)
    {
        if (_indexLoaded)
        {
            return;
        }

        if (File.Exists(_indexFilePath))
        {
            try
            {
                var json = await File.ReadAllTextAsync(_indexFilePath, cancellationToken);
                var indexData = JsonSerializer.Deserialize<ProfileIndexData>(json, _jsonOptions);
                _index = indexData?.Profiles?.ToList() ?? [];
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load profile index, rebuilding...");
                await RebuildIndexAsync(cancellationToken);
                return;
            }
        }

        _indexLoaded = true;
    }

    private async Task SaveIndexAsync(CancellationToken cancellationToken)
    {
        var indexData = new ProfileIndexData { Profiles = _index };
        var json = JsonSerializer.Serialize(indexData, _jsonOptions);
        await File.WriteAllTextAsync(_indexFilePath, json, cancellationToken);
    }

    public void Dispose()
    {
        _lock.Dispose();
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// Internal Types
// ─────────────────────────────────────────────────────────────────────────────

internal record ProfileIndexEntry(Guid Id, string Name, Realm Realm, CharacterClass Class);

internal class ProfileIndexData
{
    public List<ProfileIndexEntry> Profiles { get; set; } = [];
}
