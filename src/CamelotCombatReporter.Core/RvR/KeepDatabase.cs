using CamelotCombatReporter.Core.Models;
using CamelotCombatReporter.Core.RvR.Models;

namespace CamelotCombatReporter.Core.RvR;

/// <summary>
/// Information about a keep structure.
/// </summary>
/// <param name="Name">The keep's name.</param>
/// <param name="Type">Type of keep (Border, Relic, Tower, etc.).</param>
/// <param name="HomeRealm">The realm that originally owns this keep.</param>
/// <param name="Zone">The zone the keep is located in.</param>
/// <param name="BaseLevel">The base keep level.</param>
/// <param name="DoorCount">Number of doors in the keep.</param>
public record KeepInfo(
    string Name,
    KeepType Type,
    Realm HomeRealm,
    string Zone,
    int BaseLevel,
    int DoorCount
);

/// <summary>
/// Static database of DAoC keeps and structures.
/// Contains core keeps from all three realms.
/// </summary>
public static class KeepDatabase
{
    /// <summary>
    /// All keeps in the database.
    /// </summary>
    public static readonly IReadOnlyList<KeepInfo> Keeps = new KeepInfo[]
    {
        // ==================== ALBION ====================

        // Albion Border Keeps
        new("Castle Sauvage", KeepType.BorderKeep, Realm.Albion, "Forest Sauvage", 5, 2),
        new("Snowdonia Fortress", KeepType.BorderKeep, Realm.Albion, "Snowdonia", 5, 2),
        new("Caer Benowyc", KeepType.BorderKeep, Realm.Albion, "Pennine Mountains", 5, 2),
        new("Caer Berkstead", KeepType.BorderKeep, Realm.Albion, "Pennine Mountains", 5, 2),
        new("Caer Erasleigh", KeepType.BorderKeep, Realm.Albion, "Pennine Mountains", 5, 2),
        new("Caer Boldiam", KeepType.BorderKeep, Realm.Albion, "Hadrian's Wall", 5, 2),
        new("Caer Renaris", KeepType.BorderKeep, Realm.Albion, "Hadrian's Wall", 5, 2),
        new("Caer Hurbury", KeepType.BorderKeep, Realm.Albion, "Hadrian's Wall", 5, 2),

        // Albion Relic Keeps
        new("Castle Myrddin", KeepType.RelicKeep, Realm.Albion, "Hadrian's Wall", 10, 3),
        new("Castle Excalibur", KeepType.RelicKeep, Realm.Albion, "Forest Sauvage", 10, 3),

        // ==================== MIDGARD ====================

        // Midgard Border Keeps
        new("Svasud Faste", KeepType.BorderKeep, Realm.Midgard, "Odin's Gate", 5, 2),
        new("Vindsaul Faste", KeepType.BorderKeep, Realm.Midgard, "Jamtland Mountains", 5, 2),
        new("Nottmoor Faste", KeepType.BorderKeep, Realm.Midgard, "Uppland", 5, 2),
        new("Hlidskialf Faste", KeepType.BorderKeep, Realm.Midgard, "Uppland", 5, 2),
        new("Blendrake Faste", KeepType.BorderKeep, Realm.Midgard, "Uppland", 5, 2),
        new("Glenlock Faste", KeepType.BorderKeep, Realm.Midgard, "Odin's Gate", 5, 2),
        new("Arvakr Faste", KeepType.BorderKeep, Realm.Midgard, "Odin's Gate", 5, 2),
        new("Fensalir Faste", KeepType.BorderKeep, Realm.Midgard, "Odin's Gate", 5, 2),

        // Midgard Relic Keeps
        new("Mjollner Faste", KeepType.RelicKeep, Realm.Midgard, "Odin's Gate", 10, 3),
        new("Grallarhorn Faste", KeepType.RelicKeep, Realm.Midgard, "Jamtland Mountains", 10, 3),

        // ==================== HIBERNIA ====================

        // Hibernia Border Keeps
        new("Druim Ligen", KeepType.BorderKeep, Realm.Hibernia, "Emain Macha", 5, 2),
        new("Druim Cain", KeepType.BorderKeep, Realm.Hibernia, "Cruachan Gorge", 5, 2),
        new("Dun nGed", KeepType.BorderKeep, Realm.Hibernia, "Breifine", 5, 2),
        new("Dun Crauchon", KeepType.BorderKeep, Realm.Hibernia, "Cruachan Gorge", 5, 2),
        new("Dun Crimthain", KeepType.BorderKeep, Realm.Hibernia, "Cruachan Gorge", 5, 2),
        new("Dun Bolg", KeepType.BorderKeep, Realm.Hibernia, "Emain Macha", 5, 2),
        new("Dun da Behnn", KeepType.BorderKeep, Realm.Hibernia, "Emain Macha", 5, 2),
        new("Dun Scathaig", KeepType.BorderKeep, Realm.Hibernia, "Emain Macha", 5, 2),

        // Hibernia Relic Keeps
        new("Dun Lamfhota", KeepType.RelicKeep, Realm.Hibernia, "Cruachan Gorge", 10, 3),
        new("Dun Dagda", KeepType.RelicKeep, Realm.Hibernia, "Emain Macha", 10, 3),
    };

    private static readonly Dictionary<string, KeepInfo> KeepsByName;

    static KeepDatabase()
    {
        KeepsByName = Keeps.ToDictionary(
            k => k.Name,
            k => k,
            StringComparer.OrdinalIgnoreCase
        );
    }

    /// <summary>
    /// Lookup a keep by name (case-insensitive).
    /// </summary>
    /// <param name="name">The keep name to search for.</param>
    /// <returns>The keep info, or null if not found.</returns>
    public static KeepInfo? GetByName(string name)
    {
        return KeepsByName.GetValueOrDefault(name);
    }

    /// <summary>
    /// Tries to find a keep by partial name match.
    /// </summary>
    /// <param name="partialName">Partial name to search for.</param>
    /// <returns>Matching keep info, or null if not found.</returns>
    public static KeepInfo? FindByPartialName(string partialName)
    {
        return Keeps.FirstOrDefault(k =>
            k.Name.Contains(partialName, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Get all keeps for a realm.
    /// </summary>
    /// <param name="realm">The realm to filter by.</param>
    /// <returns>List of keeps belonging to that realm.</returns>
    public static IReadOnlyList<KeepInfo> GetByRealm(Realm realm)
    {
        return Keeps.Where(k => k.HomeRealm == realm).ToList();
    }

    /// <summary>
    /// Get all keeps of a specific type.
    /// </summary>
    /// <param name="type">The keep type to filter by.</param>
    /// <returns>List of keeps of that type.</returns>
    public static IReadOnlyList<KeepInfo> GetByType(KeepType type)
    {
        return Keeps.Where(k => k.Type == type).ToList();
    }

    /// <summary>
    /// Get all keeps in a specific zone.
    /// </summary>
    /// <param name="zone">The zone name to filter by.</param>
    /// <returns>List of keeps in that zone.</returns>
    public static IReadOnlyList<KeepInfo> GetByZone(string zone)
    {
        return Keeps.Where(k =>
            k.Zone.Equals(zone, StringComparison.OrdinalIgnoreCase)).ToList();
    }

    /// <summary>
    /// Gets all relic keeps.
    /// </summary>
    /// <returns>List of relic keeps.</returns>
    public static IReadOnlyList<KeepInfo> GetRelicKeeps()
    {
        return GetByType(KeepType.RelicKeep);
    }

    /// <summary>
    /// Gets all border keeps.
    /// </summary>
    /// <returns>List of border keeps.</returns>
    public static IReadOnlyList<KeepInfo> GetBorderKeeps()
    {
        return GetByType(KeepType.BorderKeep);
    }

    /// <summary>
    /// Checks if a keep name exists in the database.
    /// </summary>
    /// <param name="name">The keep name to check.</param>
    /// <returns>True if the keep exists.</returns>
    public static bool Exists(string name)
    {
        return KeepsByName.ContainsKey(name);
    }

    /// <summary>
    /// Gets the total count of keeps in the database.
    /// </summary>
    public static int Count => Keeps.Count;
}
