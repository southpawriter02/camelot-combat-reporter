using CamelotCombatReporter.Core.Models;

namespace CamelotCombatReporter.Core.ServerProfiles;

/// <summary>
/// Represents a server profile with era-specific configuration.
/// </summary>
/// <param name="Id">Unique identifier for the profile.</param>
/// <param name="Name">Display name for the profile.</param>
/// <param name="BaseType">The base server type.</param>
/// <param name="AvailableClasses">Classes available on this server.</param>
/// <param name="HasMasterLevels">Whether Master Levels (ToA) are enabled.</param>
/// <param name="HasArtifacts">Whether Artifacts (ToA) are enabled.</param>
/// <param name="HasChampionLevels">Whether Champion Levels are enabled.</param>
/// <param name="HasMaulers">Whether the Mauler class is available.</param>
/// <param name="IsBuiltIn">Whether this is a built-in profile (cannot be deleted).</param>
/// <param name="CreatedUtc">When the profile was created.</param>
/// <param name="ModifiedUtc">When the profile was last modified.</param>
public record ServerProfile(
    string Id,
    string Name,
    ServerType BaseType,
    IReadOnlySet<CharacterClass> AvailableClasses,
    bool HasMasterLevels,
    bool HasArtifacts,
    bool HasChampionLevels,
    bool HasMaulers,
    bool IsBuiltIn,
    DateTime CreatedUtc,
    DateTime ModifiedUtc
)
{
    /// <summary>
    /// Checks if a character class is available on this server.
    /// </summary>
    /// <param name="characterClass">The class to check.</param>
    /// <returns>True if the class is available.</returns>
    public bool IsClassAvailable(CharacterClass characterClass)
    {
        return AvailableClasses.Contains(characterClass);
    }

    /// <summary>
    /// Gets all available classes for a specific realm.
    /// </summary>
    /// <param name="realm">The realm to filter by.</param>
    /// <returns>Classes available for that realm on this server.</returns>
    public IEnumerable<CharacterClass> GetClassesForRealm(Realm realm)
    {
        return AvailableClasses.Where(c => GetRealmForClass(c) == realm);
    }

    /// <summary>
    /// Gets the realm for a character class.
    /// </summary>
    private static Realm GetRealmForClass(CharacterClass characterClass)
    {
        return characterClass switch
        {
            // Albion classes
            CharacterClass.Armsman or
            CharacterClass.Cabalist or
            CharacterClass.Cleric or
            CharacterClass.Friar or
            CharacterClass.Infiltrator or
            CharacterClass.Mercenary or
            CharacterClass.Minstrel or
            CharacterClass.Necromancer or
            CharacterClass.Paladin or
            CharacterClass.Reaver or
            CharacterClass.Scout or
            CharacterClass.Sorcerer or
            CharacterClass.Theurgist or
            CharacterClass.Wizard or
            CharacterClass.Heretic or
            CharacterClass.MaulerAlb => Realm.Albion,

            // Midgard classes
            CharacterClass.Berserker or
            CharacterClass.Bonedancer or
            CharacterClass.Healer or
            CharacterClass.Hunter or
            CharacterClass.Runemaster or
            CharacterClass.Savage or
            CharacterClass.Shadowblade or
            CharacterClass.Shaman or
            CharacterClass.Skald or
            CharacterClass.Spiritmaster or
            CharacterClass.Thane or
            CharacterClass.Valkyrie or
            CharacterClass.Warlock or
            CharacterClass.Warrior or
            CharacterClass.MaulerMid => Realm.Midgard,

            // Hibernia classes
            CharacterClass.Animist or
            CharacterClass.Bainshee or
            CharacterClass.Bard or
            CharacterClass.Blademaster or
            CharacterClass.Champion or
            CharacterClass.Druid or
            CharacterClass.Eldritch or
            CharacterClass.Enchanter or
            CharacterClass.Hero or
            CharacterClass.Mentalist or
            CharacterClass.Nightshade or
            CharacterClass.Ranger or
            CharacterClass.Valewalker or
            CharacterClass.Vampiir or
            CharacterClass.Warden or
            CharacterClass.MaulerHib => Realm.Hibernia,

            _ => Realm.Albion // Default fallback
        };
    }
}
