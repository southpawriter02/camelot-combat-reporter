using CamelotCombatReporter.Core.Models;
using CamelotCombatReporter.Core.RvR.Models;

namespace CamelotCombatReporter.Core.RvR;

/// <summary>
/// Information about a realm relic.
/// </summary>
/// <param name="Name">The relic's name.</param>
/// <param name="Type">Type of relic (Strength or Power).</param>
/// <param name="HomeRealm">The realm that originally owns this relic.</param>
/// <param name="HomeKeep">The keep where this relic is housed.</param>
public record RelicInfo(
    string Name,
    RelicType Type,
    Realm HomeRealm,
    string HomeKeep
);

/// <summary>
/// Static database of realm relics.
/// Each realm has 2 relics: one Strength (melee) and one Power (magic).
/// </summary>
public static class RelicDatabase
{
    /// <summary>
    /// All relics in the database.
    /// </summary>
    public static readonly IReadOnlyList<RelicInfo> Relics = new RelicInfo[]
    {
        // Albion Relics
        new("Scabbard of Excalibur", RelicType.Strength, Realm.Albion, "Castle Excalibur"),
        new("Merlin's Staff", RelicType.Power, Realm.Albion, "Castle Myrddin"),

        // Midgard Relics
        new("Thor's Hammer", RelicType.Strength, Realm.Midgard, "Mjollner Faste"),
        new("Horn of Valhalla", RelicType.Power, Realm.Midgard, "Grallarhorn Faste"),

        // Hibernia Relics
        new("Lug's Spear of Lightning", RelicType.Strength, Realm.Hibernia, "Dun Lamfhota"),
        new("Cauldron of Dagda", RelicType.Power, Realm.Hibernia, "Dun Dagda"),
    };

    private static readonly Dictionary<string, RelicInfo> RelicsByName;

    static RelicDatabase()
    {
        RelicsByName = Relics.ToDictionary(
            r => r.Name,
            r => r,
            StringComparer.OrdinalIgnoreCase
        );
    }

    /// <summary>
    /// Lookup a relic by name (case-insensitive).
    /// </summary>
    /// <param name="name">The relic name to search for.</param>
    /// <returns>The relic info, or null if not found.</returns>
    public static RelicInfo? GetByName(string name)
    {
        return RelicsByName.GetValueOrDefault(name);
    }

    /// <summary>
    /// Tries to find a relic by partial name match.
    /// </summary>
    /// <param name="partialName">Partial name to search for.</param>
    /// <returns>Matching relic info, or null if not found.</returns>
    public static RelicInfo? FindByPartialName(string partialName)
    {
        return Relics.FirstOrDefault(r =>
            r.Name.Contains(partialName, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Get all relics for a realm.
    /// </summary>
    /// <param name="realm">The realm to filter by.</param>
    /// <returns>List of relics belonging to that realm.</returns>
    public static IReadOnlyList<RelicInfo> GetByRealm(Realm realm)
    {
        return Relics.Where(r => r.HomeRealm == realm).ToList();
    }

    /// <summary>
    /// Get all relics of a specific type.
    /// </summary>
    /// <param name="type">The relic type to filter by.</param>
    /// <returns>List of relics of that type.</returns>
    public static IReadOnlyList<RelicInfo> GetByType(RelicType type)
    {
        return Relics.Where(r => r.Type == type).ToList();
    }

    /// <summary>
    /// Gets the strength relic for a realm.
    /// </summary>
    /// <param name="realm">The realm.</param>
    /// <returns>The strength relic, or null if not found.</returns>
    public static RelicInfo? GetStrengthRelic(Realm realm)
    {
        return Relics.FirstOrDefault(r =>
            r.HomeRealm == realm && r.Type == RelicType.Strength);
    }

    /// <summary>
    /// Gets the power relic for a realm.
    /// </summary>
    /// <param name="realm">The realm.</param>
    /// <returns>The power relic, or null if not found.</returns>
    public static RelicInfo? GetPowerRelic(Realm realm)
    {
        return Relics.FirstOrDefault(r =>
            r.HomeRealm == realm && r.Type == RelicType.Power);
    }

    /// <summary>
    /// Checks if a relic name exists in the database.
    /// </summary>
    /// <param name="name">The relic name to check.</param>
    /// <returns>True if the relic exists.</returns>
    public static bool Exists(string name)
    {
        return RelicsByName.ContainsKey(name);
    }

    /// <summary>
    /// Determines the relic type from the relic name.
    /// </summary>
    /// <param name="relicName">The relic name.</param>
    /// <returns>The relic type, or null if not found.</returns>
    public static RelicType? GetRelicType(string relicName)
    {
        return GetByName(relicName)?.Type;
    }

    /// <summary>
    /// Determines the home realm from the relic name.
    /// </summary>
    /// <param name="relicName">The relic name.</param>
    /// <returns>The home realm, or null if not found.</returns>
    public static Realm? GetHomeRealm(string relicName)
    {
        return GetByName(relicName)?.HomeRealm;
    }

    /// <summary>
    /// Gets the total count of relics in the database.
    /// </summary>
    public static int Count => Relics.Count;
}
