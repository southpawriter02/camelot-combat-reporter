using CamelotCombatReporter.Core.CharacterBuilding.Models;
using CamelotCombatReporter.Core.Models;

namespace CamelotCombatReporter.Core.CharacterBuilding.Services;

/// <summary>
/// Service interface for managing character profiles and associated builds.
/// </summary>
public interface ICharacterProfileService
{
    // ─────────────────────────────────────────────────────────────────────────
    // Profile CRUD
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Creates a new character profile.
    /// </summary>
    Task<CharacterProfile> CreateProfileAsync(CharacterProfile profile, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a profile by its ID.
    /// </summary>
    Task<CharacterProfile?> GetProfileAsync(Guid profileId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all profiles.
    /// </summary>
    Task<IReadOnlyList<CharacterProfile>> GetAllProfilesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets profiles filtered by realm.
    /// </summary>
    Task<IReadOnlyList<CharacterProfile>> GetProfilesByRealmAsync(Realm realm, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing profile.
    /// </summary>
    Task UpdateProfileAsync(CharacterProfile profile, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a profile by its ID.
    /// </summary>
    Task<bool> DeleteProfileAsync(Guid profileId, CancellationToken cancellationToken = default);

    // ─────────────────────────────────────────────────────────────────────────
    // Build Management
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Creates a new build for a profile and adds it to build history.
    /// </summary>
    Task<CharacterBuild> CreateBuildAsync(Guid profileId, CharacterBuild build, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a copy of an existing build with a new name.
    /// </summary>
    Task<CharacterBuild> CloneBuildAsync(Guid profileId, Guid buildId, string newName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing build. Note: builds are immutable by design,
    /// so this creates a new version and archives the old one.
    /// </summary>
    Task<CharacterBuild> UpdateBuildAsync(Guid profileId, CharacterBuild build, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets the active build for a profile.
    /// </summary>
    Task SetActiveBuildAsync(Guid profileId, Guid buildId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the build history for a profile.
    /// </summary>
    Task<IReadOnlyList<CharacterBuild>> GetBuildHistoryAsync(Guid profileId, CancellationToken cancellationToken = default);

    // ─────────────────────────────────────────────────────────────────────────
    // Session Attachment
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Attaches a combat session to a profile, optionally associating it with a specific build.
    /// </summary>
    Task AttachSessionAsync(Guid profileId, Guid sessionId, Guid? buildId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Detaches a combat session from a profile.
    /// </summary>
    Task DetachSessionAsync(Guid profileId, Guid sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets session IDs attached to a profile.
    /// </summary>
    Task<IReadOnlyList<Guid>> GetAttachedSessionIdsAsync(Guid profileId, CancellationToken cancellationToken = default);

    // ─────────────────────────────────────────────────────────────────────────
    // Auto-Matching
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Suggests a profile that might match a given combat session based on class, realm, and name.
    /// </summary>
    Task<CharacterProfile?> SuggestProfileForSessionAsync(ExtendedCombatStatistics session, CancellationToken cancellationToken = default);

    // ─────────────────────────────────────────────────────────────────────────
    // Import/Export
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Exports a profile to JSON.
    /// </summary>
    Task<string> ExportProfileAsync(Guid profileId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Imports a profile from JSON.
    /// </summary>
    Task<CharacterProfile> ImportProfileAsync(string json, CancellationToken cancellationToken = default);

    // ─────────────────────────────────────────────────────────────────────────
    // Utilities
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Gets the total number of profiles.
    /// </summary>
    Task<int> GetProfileCountAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Rebuilds the profile index from disk.
    /// </summary>
    Task RebuildIndexAsync(CancellationToken cancellationToken = default);
}
