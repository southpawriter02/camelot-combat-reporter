using CamelotCombatReporter.Core.RealmAbilities.Models;

namespace CamelotCombatReporter.Core.RealmAbilities;

/// <summary>
/// Interface for accessing the realm ability database.
/// </summary>
public interface IRealmAbilityDatabase
{
    /// <summary>
    /// Gets all abilities in the database.
    /// </summary>
    IReadOnlyList<RealmAbility> AllAbilities { get; }

    /// <summary>
    /// Gets an ability by its unique ID.
    /// </summary>
    /// <param name="id">The ability ID.</param>
    /// <returns>The ability, or null if not found.</returns>
    RealmAbility? GetById(string id);

    /// <summary>
    /// Gets an ability by its display name.
    /// </summary>
    /// <param name="name">The ability name.</param>
    /// <returns>The ability, or null if not found.</returns>
    RealmAbility? GetByName(string name);

    /// <summary>
    /// Gets an ability by its internal name (for log parsing).
    /// </summary>
    /// <param name="internalName">The internal name.</param>
    /// <returns>The ability, or null if not found.</returns>
    RealmAbility? GetByInternalName(string internalName);

    /// <summary>
    /// Gets all abilities of a specific type.
    /// </summary>
    /// <param name="type">The ability type.</param>
    /// <returns>List of matching abilities.</returns>
    IReadOnlyList<RealmAbility> GetByType(RealmAbilityType type);

    /// <summary>
    /// Gets all abilities available to a specific realm.
    /// </summary>
    /// <param name="realm">The realm availability filter.</param>
    /// <returns>List of matching abilities.</returns>
    IReadOnlyList<RealmAbility> GetByRealm(RealmAvailability realm);

    /// <summary>
    /// Gets all abilities available in a specific game era or earlier.
    /// </summary>
    /// <param name="maxEra">The maximum era to include.</param>
    /// <returns>List of abilities available in the specified era.</returns>
    IReadOnlyList<RealmAbility> GetByEra(GameEra maxEra);

    /// <summary>
    /// Gets all abilities available to a realm in a specific era.
    /// </summary>
    /// <param name="realm">The realm availability filter.</param>
    /// <param name="maxEra">The maximum era to include.</param>
    /// <returns>List of matching abilities.</returns>
    IReadOnlyList<RealmAbility> GetByRealmAndEra(RealmAvailability realm, GameEra maxEra);

    /// <summary>
    /// Reloads the database from the source file.
    /// </summary>
    Task ReloadAsync();

    /// <summary>
    /// Gets the total number of abilities in the database.
    /// </summary>
    int Count { get; }
}
