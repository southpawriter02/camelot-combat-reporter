using CamelotCombatReporter.Core.Models;

namespace CamelotCombatReporter.Core.RvR.Models;

/// <summary>
/// Base record for relic-related events.
/// </summary>
public abstract record RelicEvent(
    TimeOnly Timestamp,
    string RelicName,
    RelicType Type
) : LogEvent(Timestamp);

/// <summary>
/// Relic pickup by a player.
/// </summary>
/// <param name="Timestamp">Time of pickup.</param>
/// <param name="RelicName">Name of the relic.</param>
/// <param name="Type">Type of relic (Strength or Power).</param>
/// <param name="CarrierName">Name of the player who picked up the relic.</param>
/// <param name="OriginRealm">Realm the relic originally belongs to.</param>
public record RelicPickupEvent(
    TimeOnly Timestamp,
    string RelicName,
    RelicType Type,
    string CarrierName,
    Realm OriginRealm
) : RelicEvent(Timestamp, RelicName, Type);

/// <summary>
/// Relic dropped (carrier died or intentionally dropped).
/// </summary>
/// <param name="Timestamp">Time of drop.</param>
/// <param name="RelicName">Name of the relic.</param>
/// <param name="Type">Type of relic.</param>
/// <param name="CarrierName">Name of the player who dropped it.</param>
/// <param name="KillerName">Name of who killed the carrier, if applicable.</param>
public record RelicDropEvent(
    TimeOnly Timestamp,
    string RelicName,
    RelicType Type,
    string? CarrierName,
    string? KillerName
) : RelicEvent(Timestamp, RelicName, Type);

/// <summary>
/// Relic successfully captured (placed on pedestal).
/// </summary>
/// <param name="Timestamp">Time of capture.</param>
/// <param name="RelicName">Name of the relic.</param>
/// <param name="Type">Type of relic.</param>
/// <param name="CapturingRealm">Realm that captured the relic.</param>
/// <param name="OriginRealm">Realm the relic originally belongs to.</param>
public record RelicCapturedEvent(
    TimeOnly Timestamp,
    string RelicName,
    RelicType Type,
    Realm CapturingRealm,
    Realm OriginRealm
) : RelicEvent(Timestamp, RelicName, Type);

/// <summary>
/// Relic returned to home keep.
/// </summary>
/// <param name="Timestamp">Time of return.</param>
/// <param name="RelicName">Name of the relic.</param>
/// <param name="Type">Type of relic.</param>
/// <param name="HomeRealm">Realm the relic was returned to.</param>
public record RelicReturnedEvent(
    TimeOnly Timestamp,
    string RelicName,
    RelicType Type,
    Realm HomeRealm
) : RelicEvent(Timestamp, RelicName, Type);
