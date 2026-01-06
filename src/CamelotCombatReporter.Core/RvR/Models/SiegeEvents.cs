using CamelotCombatReporter.Core.Models;

namespace CamelotCombatReporter.Core.RvR.Models;

/// <summary>
/// Base record for all siege-related events.
/// </summary>
public abstract record SiegeEvent(
    TimeOnly Timestamp,
    string KeepName
) : LogEvent(Timestamp);

/// <summary>
/// Damage dealt to a keep door or structure.
/// </summary>
/// <param name="Timestamp">Time the damage occurred.</param>
/// <param name="KeepName">Name of the keep being attacked.</param>
/// <param name="DoorName">Name of the door (e.g., "Outer Door", "Inner Door").</param>
/// <param name="DamageAmount">Amount of damage dealt.</param>
/// <param name="Source">Who dealt the damage ("You" or player name).</param>
/// <param name="IsDestroyed">Whether this damage destroyed the door.</param>
public record DoorDamageEvent(
    TimeOnly Timestamp,
    string KeepName,
    string DoorName,
    int DamageAmount,
    string Source,
    bool IsDestroyed
) : SiegeEvent(Timestamp, KeepName);

/// <summary>
/// Keep capture/claim event.
/// </summary>
/// <param name="Timestamp">Time of capture.</param>
/// <param name="KeepName">Name of the captured keep.</param>
/// <param name="NewOwner">Realm that captured the keep.</param>
/// <param name="PreviousOwner">Realm that lost the keep.</param>
/// <param name="ClaimingGuild">Guild that claimed the keep, if any.</param>
public record KeepCapturedEvent(
    TimeOnly Timestamp,
    string KeepName,
    Realm NewOwner,
    Realm? PreviousOwner,
    string? ClaimingGuild
) : SiegeEvent(Timestamp, KeepName);

/// <summary>
/// Guard or Lord NPC kill during siege.
/// </summary>
/// <param name="Timestamp">Time of the kill.</param>
/// <param name="KeepName">Name of the keep.</param>
/// <param name="GuardName">Name of the guard or lord.</param>
/// <param name="Killer">Who killed the guard ("You" or player name).</param>
/// <param name="IsLordKill">Whether this was the keep lord.</param>
public record GuardKillEvent(
    TimeOnly Timestamp,
    string KeepName,
    string GuardName,
    string Killer,
    bool IsLordKill
) : SiegeEvent(Timestamp, KeepName);

/// <summary>
/// Siege weapon deployment or destruction.
/// </summary>
/// <param name="Timestamp">Time of the event.</param>
/// <param name="KeepName">Name of the keep being sieged.</param>
/// <param name="WeaponType">Type of siege weapon.</param>
/// <param name="PlayerName">Player who deployed or destroyed the weapon.</param>
/// <param name="IsDeployed">True if deployed, false if destroyed.</param>
public record SiegeWeaponEvent(
    TimeOnly Timestamp,
    string KeepName,
    SiegeWeaponType WeaponType,
    string PlayerName,
    bool IsDeployed
) : SiegeEvent(Timestamp, KeepName);

/// <summary>
/// Tower capture event (simpler than keep capture).
/// </summary>
/// <param name="Timestamp">Time of capture.</param>
/// <param name="TowerName">Name of the tower.</param>
/// <param name="NewOwner">Realm that captured the tower.</param>
public record TowerCapturedEvent(
    TimeOnly Timestamp,
    string TowerName,
    Realm NewOwner
) : LogEvent(Timestamp);

/// <summary>
/// Zone entry event for tracking battleground and frontier entry.
/// </summary>
/// <param name="Timestamp">Time of zone entry.</param>
/// <param name="ZoneName">Name of the zone entered.</param>
public record ZoneEntryEvent(
    TimeOnly Timestamp,
    string ZoneName
) : LogEvent(Timestamp);
