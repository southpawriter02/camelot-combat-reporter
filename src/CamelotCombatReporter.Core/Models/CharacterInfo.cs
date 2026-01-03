using System.Text.Json.Serialization;

namespace CamelotCombatReporter.Core.Models;

/// <summary>
/// Represents a player character's configuration for cross-realm analysis.
/// </summary>
public record CharacterInfo
{
    /// <summary>
    /// Character name (optional, for personal tracking).
    /// </summary>
    public string Name { get; init; }

    /// <summary>
    /// The character's realm.
    /// </summary>
    public Realm Realm { get; init; }

    /// <summary>
    /// The character's class.
    /// </summary>
    public CharacterClass Class { get; init; }

    /// <summary>
    /// Character level (1-50).
    /// </summary>
    public int Level { get; init; }

    /// <summary>
    /// Realm rank (0-14, where 0 = no realm rank).
    /// </summary>
    public int RealmRank { get; init; }

    /// <summary>
    /// Creates a new CharacterInfo instance.
    /// </summary>
    public CharacterInfo(
        string name,
        Realm realm,
        CharacterClass characterClass,
        int level = 50,
        int realmRank = 0)
    {
        Name = name;
        Realm = realm;
        Class = characterClass;
        Level = Math.Clamp(level, 1, 50);
        RealmRank = Math.Clamp(realmRank, 0, 14);
    }

    /// <summary>
    /// Default unconfigured character.
    /// </summary>
    public static CharacterInfo Default => new("", Realm.Unknown, CharacterClass.Unknown);

    /// <summary>
    /// Whether the character has been configured with realm and class.
    /// </summary>
    [JsonIgnore]
    public bool IsConfigured => Realm != Realm.Unknown && Class != CharacterClass.Unknown;

    /// <summary>
    /// Gets the class archetype (Tank, Healer, Caster, Stealth).
    /// </summary>
    [JsonIgnore]
    public ClassArchetype Archetype => Class.GetArchetype();

    /// <summary>
    /// Gets a display string for the character.
    /// </summary>
    [JsonIgnore]
    public string DisplayString => IsConfigured
        ? $"{(string.IsNullOrEmpty(Name) ? "Character" : Name)} - {Class.GetDisplayName()} ({Realm})"
        : "Not Configured";

    /// <summary>
    /// Gets a short display string (class and realm only).
    /// </summary>
    [JsonIgnore]
    public string ShortDisplayString => IsConfigured
        ? $"{Class.GetDisplayName()} ({Realm})"
        : "Not Configured";

    /// <summary>
    /// Validates that the class belongs to the specified realm.
    /// </summary>
    public bool IsValid => Class == CharacterClass.Unknown || Class.GetRealm() == Realm;

    /// <summary>
    /// Creates a copy with updated values.
    /// </summary>
    public CharacterInfo With(
        string? name = null,
        Realm? realm = null,
        CharacterClass? characterClass = null,
        int? level = null,
        int? realmRank = null)
    {
        return new CharacterInfo(
            name ?? Name,
            realm ?? Realm,
            characterClass ?? Class,
            level ?? Level,
            realmRank ?? RealmRank);
    }
}
