namespace CamelotCombatReporter.Core.Models;

/// <summary>
/// The three realms of Dark Age of Camelot.
/// </summary>
public enum Realm
{
    Unknown = 0,
    Albion = 1,
    Midgard = 2,
    Hibernia = 3
}

/// <summary>
/// Character classes grouped by realm.
/// Uses numeric ranges for easy realm identification:
/// - Albion: 100-115
/// - Midgard: 200-214
/// - Hibernia: 300-315
/// </summary>
public enum CharacterClass
{
    Unknown = 0,

    // Albion Classes (100-115)
    Armsman = 100,
    Cabalist = 101,
    Cleric = 102,
    Friar = 103,
    Heretic = 104,
    Infiltrator = 105,
    Mercenary = 106,
    Minstrel = 107,
    Necromancer = 108,
    Paladin = 109,
    Reaver = 110,
    Scout = 111,
    Sorcerer = 112,
    Theurgist = 113,
    Wizard = 114,
    MaulerAlb = 115,

    // Midgard Classes (200-214)
    Berserker = 200,
    Bonedancer = 201,
    Healer = 202,
    Hunter = 203,
    Runemaster = 204,
    Savage = 205,
    Shadowblade = 206,
    Shaman = 207,
    Skald = 208,
    Spiritmaster = 209,
    Thane = 210,
    Valkyrie = 211,
    Warlock = 212,
    Warrior = 213,
    MaulerMid = 214,

    // Hibernia Classes (300-315)
    Animist = 300,
    Bainshee = 301,
    Bard = 302,
    Blademaster = 303,
    Champion = 304,
    Druid = 305,
    Eldritch = 306,
    Enchanter = 307,
    Hero = 308,
    Mentalist = 309,
    Nightshade = 310,
    Ranger = 311,
    Valewalker = 312,
    Vampiir = 313,
    Warden = 314,
    MaulerHib = 315
}

/// <summary>
/// Extension methods for game enums.
/// </summary>
public static class GameEnumExtensions
{
    /// <summary>
    /// Gets the realm for a character class based on its numeric range.
    /// </summary>
    public static Realm GetRealm(this CharacterClass characterClass) => characterClass switch
    {
        >= CharacterClass.Armsman and <= CharacterClass.MaulerAlb => Realm.Albion,
        >= CharacterClass.Berserker and <= CharacterClass.MaulerMid => Realm.Midgard,
        >= CharacterClass.Animist and <= CharacterClass.MaulerHib => Realm.Hibernia,
        _ => Realm.Unknown
    };

    /// <summary>
    /// Gets all character classes for a specific realm.
    /// </summary>
    public static IEnumerable<CharacterClass> GetClasses(this Realm realm) => realm switch
    {
        Realm.Albion => Enum.GetValues<CharacterClass>().Where(c => (int)c >= 100 && (int)c <= 115),
        Realm.Midgard => Enum.GetValues<CharacterClass>().Where(c => (int)c >= 200 && (int)c <= 214),
        Realm.Hibernia => Enum.GetValues<CharacterClass>().Where(c => (int)c >= 300 && (int)c <= 315),
        _ => Enumerable.Empty<CharacterClass>()
    };

    /// <summary>
    /// Gets a display-friendly name for a character class.
    /// Handles special cases like realm-specific Maulers.
    /// </summary>
    public static string GetDisplayName(this CharacterClass characterClass) => characterClass switch
    {
        CharacterClass.MaulerAlb or CharacterClass.MaulerMid or CharacterClass.MaulerHib => "Mauler",
        CharacterClass.Bainshee => "Banshee",
        _ => characterClass.ToString()
    };

    /// <summary>
    /// Gets a display-friendly name for a realm.
    /// </summary>
    public static string GetDisplayName(this Realm realm) => realm.ToString();

    /// <summary>
    /// Gets the archetype/role category for a class.
    /// </summary>
    public static ClassArchetype GetArchetype(this CharacterClass characterClass) => characterClass switch
    {
        // Tanks
        CharacterClass.Armsman or CharacterClass.Paladin or CharacterClass.Reaver or
        CharacterClass.Mercenary or CharacterClass.Warrior or CharacterClass.Thane or
        CharacterClass.Berserker or CharacterClass.Savage or CharacterClass.Hero or
        CharacterClass.Champion or CharacterClass.Blademaster or CharacterClass.Valewalker or
        CharacterClass.MaulerAlb or CharacterClass.MaulerMid or CharacterClass.MaulerHib => ClassArchetype.Tank,

        // Healers
        CharacterClass.Cleric or CharacterClass.Friar or CharacterClass.Healer or
        CharacterClass.Shaman or CharacterClass.Druid or CharacterClass.Warden or
        CharacterClass.Bard => ClassArchetype.Healer,

        // Casters
        CharacterClass.Wizard or CharacterClass.Theurgist or CharacterClass.Sorcerer or
        CharacterClass.Cabalist or CharacterClass.Necromancer or CharacterClass.Heretic or
        CharacterClass.Runemaster or CharacterClass.Spiritmaster or CharacterClass.Bonedancer or
        CharacterClass.Warlock or CharacterClass.Eldritch or CharacterClass.Enchanter or
        CharacterClass.Mentalist or CharacterClass.Animist or CharacterClass.Bainshee => ClassArchetype.Caster,

        // Stealthers
        CharacterClass.Infiltrator or CharacterClass.Scout or CharacterClass.Minstrel or
        CharacterClass.Shadowblade or CharacterClass.Hunter or CharacterClass.Skald or
        CharacterClass.Nightshade or CharacterClass.Ranger or CharacterClass.Vampiir or
        CharacterClass.Valkyrie => ClassArchetype.Stealth,

        _ => ClassArchetype.Unknown
    };
}

/// <summary>
/// Class archetype for grouping similar playstyles.
/// </summary>
public enum ClassArchetype
{
    Unknown = 0,
    Tank = 1,
    Healer = 2,
    Caster = 3,
    Stealth = 4
}
