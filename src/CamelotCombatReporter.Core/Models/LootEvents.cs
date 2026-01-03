namespace CamelotCombatReporter.Core.Models;

/// <summary>
/// Base record for all loot-related events.
/// </summary>
/// <param name="Timestamp">The time the event occurred.</param>
public abstract record LootEvent(TimeOnly Timestamp) : LogEvent(Timestamp);

/// <summary>
/// Represents an item dropped by a mob and picked up by the player.
/// </summary>
/// <param name="Timestamp">The time the event occurred.</param>
/// <param name="MobName">The name of the mob that dropped the item.</param>
/// <param name="ItemName">The name of the item dropped.</param>
/// <param name="IsNamedItem">True if the item used "the" article (named/unique item), false for "a/an" (common item).</param>
public record ItemDropEvent(
    TimeOnly Timestamp,
    string MobName,
    string ItemName,
    bool IsNamedItem
) : LootEvent(Timestamp);

/// <summary>
/// Represents currency (gold/silver/copper) picked up by the player.
/// </summary>
/// <param name="Timestamp">The time the event occurred.</param>
/// <param name="MobName">The name of the mob that dropped the currency, or null if standalone pickup.</param>
/// <param name="Gold">Amount of gold pieces.</param>
/// <param name="Silver">Amount of silver pieces.</param>
/// <param name="Copper">Amount of copper pieces.</param>
public record CurrencyDropEvent(
    TimeOnly Timestamp,
    string? MobName,
    int Gold,
    int Silver,
    int Copper
) : LootEvent(Timestamp)
{
    /// <summary>
    /// Gets the total value in copper pieces (1 gold = 10000 copper, 1 silver = 100 copper).
    /// </summary>
    public int TotalCopper => (Gold * 10000) + (Silver * 100) + Copper;

    /// <summary>
    /// Formats the currency as a display string (e.g., "1g 48s 98c").
    /// </summary>
    public string ToDisplayString()
    {
        var parts = new List<string>();
        if (Gold > 0) parts.Add($"{Gold}g");
        if (Silver > 0) parts.Add($"{Silver}s");
        if (Copper > 0 || parts.Count == 0) parts.Add($"{Copper}c");
        return string.Join(" ", parts);
    }
}

/// <summary>
/// Represents an item received from an NPC, trade, or other source.
/// </summary>
/// <param name="Timestamp">The time the event occurred.</param>
/// <param name="ItemName">The name of the item received.</param>
/// <param name="SourceName">The name of the source (NPC, player, etc.).</param>
/// <param name="Quantity">The quantity received, or null if not specified.</param>
public record ItemReceiveEvent(
    TimeOnly Timestamp,
    string ItemName,
    string SourceName,
    int? Quantity
) : LootEvent(Timestamp);

/// <summary>
/// Represents bonus currency from realm ownership or area bonuses.
/// </summary>
/// <param name="Timestamp">The time the event occurred.</param>
/// <param name="Gold">Amount of bonus gold pieces.</param>
/// <param name="Silver">Amount of bonus silver pieces.</param>
/// <param name="Copper">Amount of bonus copper pieces.</param>
/// <param name="BonusSource">The source of the bonus (e.g., "outpost", "area").</param>
public record BonusCurrencyEvent(
    TimeOnly Timestamp,
    int Gold,
    int Silver,
    int Copper,
    string BonusSource
) : LootEvent(Timestamp)
{
    /// <summary>
    /// Gets the total value in copper pieces.
    /// </summary>
    public int TotalCopper => (Gold * 10000) + (Silver * 100) + Copper;
}
