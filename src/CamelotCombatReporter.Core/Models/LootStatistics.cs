namespace CamelotCombatReporter.Core.Models;

/// <summary>
/// Represents statistics for a single item's drop rate from a specific mob.
/// </summary>
/// <param name="ItemName">The name of the item.</param>
/// <param name="TotalDrops">Number of times this item has dropped.</param>
/// <param name="TotalKills">Number of mob kills tracked (denominator for drop rate).</param>
/// <param name="DropRate">Drop rate as a percentage (0-100).</param>
/// <param name="ConfidenceLower">Lower bound of 95% confidence interval.</param>
/// <param name="ConfidenceUpper">Upper bound of 95% confidence interval.</param>
/// <param name="FirstSeen">First time this item was seen.</param>
/// <param name="LastSeen">Most recent time this item was seen.</param>
public record ItemDropStatistic(
    string ItemName,
    int TotalDrops,
    int TotalKills,
    double DropRate,
    double ConfidenceLower,
    double ConfidenceUpper,
    DateTime FirstSeen,
    DateTime LastSeen
)
{
    /// <summary>
    /// Creates an ItemDropStatistic with automatically calculated drop rate and confidence intervals.
    /// </summary>
    public static ItemDropStatistic Create(
        string itemName,
        int totalDrops,
        int totalKills,
        DateTime firstSeen,
        DateTime lastSeen)
    {
        var dropRate = totalKills > 0 ? (double)totalDrops / totalKills * 100 : 0;
        var (lower, upper) = StatisticsHelper.CalculateWilsonInterval(totalDrops, totalKills);

        return new ItemDropStatistic(
            itemName,
            totalDrops,
            totalKills,
            dropRate,
            lower,
            upper,
            firstSeen,
            lastSeen);
    }
}

/// <summary>
/// Represents aggregated loot data for a specific mob type.
/// </summary>
/// <param name="MobName">The name of the mob.</param>
/// <param name="TotalKills">Total number of kills tracked for this mob.</param>
/// <param name="Items">List of items that can drop from this mob with their statistics.</param>
/// <param name="CurrencyDrops">Statistics about currency drops from this mob.</param>
/// <param name="FirstEncounter">When this mob was first encountered.</param>
/// <param name="LastEncounter">When this mob was last encountered.</param>
public record MobLootTable(
    string MobName,
    int TotalKills,
    IReadOnlyList<ItemDropStatistic> Items,
    CurrencyStatistic CurrencyDrops,
    DateTime FirstEncounter,
    DateTime LastEncounter
);

/// <summary>
/// Represents aggregated currency drop statistics.
/// </summary>
/// <param name="TotalDrops">Number of currency drops recorded.</param>
/// <param name="TotalCopperValue">Total value of all currency drops in copper.</param>
/// <param name="AveragePerDrop">Average copper value per drop.</param>
/// <param name="MinDrop">Minimum copper value dropped.</param>
/// <param name="MaxDrop">Maximum copper value dropped.</param>
public record CurrencyStatistic(
    int TotalDrops,
    long TotalCopperValue,
    double AveragePerDrop,
    int MinDrop,
    int MaxDrop
)
{
    /// <summary>
    /// Formats the total currency as a display string.
    /// </summary>
    public string TotalFormatted => FormatCopper(TotalCopperValue);

    /// <summary>
    /// Formats the average per drop as a display string.
    /// </summary>
    public string AverageFormatted => FormatCopper((long)AveragePerDrop);

    private static string FormatCopper(long copper)
    {
        var gold = copper / 10000;
        var silver = (copper % 10000) / 100;
        var copperRemainder = copper % 100;

        var parts = new List<string>();
        if (gold > 0) parts.Add($"{gold}g");
        if (silver > 0) parts.Add($"{silver}s");
        if (copperRemainder > 0 || parts.Count == 0) parts.Add($"{copperRemainder}c");
        return string.Join(" ", parts);
    }
}

/// <summary>
/// Represents a summary of a loot tracking session.
/// </summary>
/// <param name="Id">Unique identifier for this session.</param>
/// <param name="SessionStart">When the session started.</param>
/// <param name="SessionEnd">When the session ended.</param>
/// <param name="TotalItemDrops">Total number of item drops during this session.</param>
/// <param name="TotalCurrencyCopper">Total currency earned in copper.</param>
/// <param name="UniqueMobsKilled">Number of unique mob types killed.</param>
/// <param name="UniqueItemsDropped">Number of unique item types obtained.</param>
/// <param name="NotableDrops">List of notable/named items that dropped.</param>
public record LootSessionSummary(
    Guid Id,
    DateTime SessionStart,
    DateTime SessionEnd,
    int TotalItemDrops,
    long TotalCurrencyCopper,
    int UniqueMobsKilled,
    int UniqueItemsDropped,
    IReadOnlyList<string> NotableDrops
)
{
    /// <summary>
    /// Gets the session duration.
    /// </summary>
    public TimeSpan Duration => SessionEnd - SessionStart;

    /// <summary>
    /// Gets the total currency formatted as a display string.
    /// </summary>
    public string TotalCurrencyFormatted
    {
        get
        {
            var gold = TotalCurrencyCopper / 10000;
            var silver = (TotalCurrencyCopper % 10000) / 100;
            var copper = TotalCurrencyCopper % 100;

            var parts = new List<string>();
            if (gold > 0) parts.Add($"{gold}g");
            if (silver > 0) parts.Add($"{silver}s");
            if (copper > 0 || parts.Count == 0) parts.Add($"{copper}c");
            return string.Join(" ", parts);
        }
    }
}

/// <summary>
/// Raw loot session data stored in JSON files.
/// </summary>
/// <param name="Id">Unique session identifier.</param>
/// <param name="StartTime">When the session started.</param>
/// <param name="EndTime">When the session ended.</param>
/// <param name="LogFilePath">Original log file path.</param>
/// <param name="Events">All loot events from this session.</param>
public record LootSessionData(
    Guid Id,
    DateTime StartTime,
    DateTime EndTime,
    string LogFilePath,
    IReadOnlyList<SerializedLootEvent> Events
);

/// <summary>
/// Serializable representation of a loot event for JSON storage.
/// </summary>
public record SerializedLootEvent(
    string EventType,
    string Timestamp,
    string? MobName,
    string? ItemName,
    bool? IsNamedItem,
    int? Gold,
    int? Silver,
    int? Copper,
    string? SourceName,
    int? Quantity,
    string? BonusSource
)
{
    /// <summary>
    /// Creates a serializable event from an ItemDropEvent.
    /// </summary>
    public static SerializedLootEvent FromItemDrop(ItemDropEvent evt) =>
        new(
            EventType: "ItemDrop",
            Timestamp: evt.Timestamp.ToString("HH:mm:ss"),
            MobName: evt.MobName,
            ItemName: evt.ItemName,
            IsNamedItem: evt.IsNamedItem,
            Gold: null,
            Silver: null,
            Copper: null,
            SourceName: null,
            Quantity: null,
            BonusSource: null);

    /// <summary>
    /// Creates a serializable event from a CurrencyDropEvent.
    /// </summary>
    public static SerializedLootEvent FromCurrencyDrop(CurrencyDropEvent evt) =>
        new(
            EventType: "CurrencyDrop",
            Timestamp: evt.Timestamp.ToString("HH:mm:ss"),
            MobName: evt.MobName,
            ItemName: null,
            IsNamedItem: null,
            Gold: evt.Gold,
            Silver: evt.Silver,
            Copper: evt.Copper,
            SourceName: null,
            Quantity: null,
            BonusSource: null);

    /// <summary>
    /// Creates a serializable event from an ItemReceiveEvent.
    /// </summary>
    public static SerializedLootEvent FromItemReceive(ItemReceiveEvent evt) =>
        new(
            EventType: "ItemReceive",
            Timestamp: evt.Timestamp.ToString("HH:mm:ss"),
            MobName: null,
            ItemName: evt.ItemName,
            IsNamedItem: null,
            Gold: null,
            Silver: null,
            Copper: null,
            SourceName: evt.SourceName,
            Quantity: evt.Quantity,
            BonusSource: null);

    /// <summary>
    /// Creates a serializable event from a BonusCurrencyEvent.
    /// </summary>
    public static SerializedLootEvent FromBonusCurrency(BonusCurrencyEvent evt) =>
        new(
            EventType: "BonusCurrency",
            Timestamp: evt.Timestamp.ToString("HH:mm:ss"),
            MobName: null,
            ItemName: null,
            IsNamedItem: null,
            Gold: evt.Gold,
            Silver: evt.Silver,
            Copper: evt.Copper,
            SourceName: null,
            Quantity: null,
            BonusSource: evt.BonusSource);

    /// <summary>
    /// Creates a serializable event from any LootEvent.
    /// </summary>
    public static SerializedLootEvent FromLootEvent(LootEvent evt) => evt switch
    {
        ItemDropEvent itemDrop => FromItemDrop(itemDrop),
        CurrencyDropEvent currencyDrop => FromCurrencyDrop(currencyDrop),
        ItemReceiveEvent itemReceive => FromItemReceive(itemReceive),
        BonusCurrencyEvent bonusCurrency => FromBonusCurrency(bonusCurrency),
        _ => throw new ArgumentException($"Unknown loot event type: {evt.GetType().Name}")
    };
}

/// <summary>
/// Helper class for statistical calculations.
/// </summary>
public static class StatisticsHelper
{
    /// <summary>
    /// Calculates the Wilson score interval for a proportion.
    /// This provides a more accurate confidence interval for small sample sizes.
    /// </summary>
    /// <param name="successes">Number of successes (e.g., item drops).</param>
    /// <param name="trials">Total number of trials (e.g., kills).</param>
    /// <param name="confidence">Confidence level (default 0.95 for 95% CI).</param>
    /// <returns>Lower and upper bounds of the confidence interval as percentages (0-100).</returns>
    public static (double Lower, double Upper) CalculateWilsonInterval(
        int successes,
        int trials,
        double confidence = 0.95)
    {
        if (trials == 0) return (0, 0);

        // Z-score for 95% confidence (1.96)
        // For other confidence levels: 90%=1.645, 99%=2.576
        double z = confidence switch
        {
            >= 0.99 => 2.576,
            >= 0.95 => 1.96,
            >= 0.90 => 1.645,
            _ => 1.96
        };

        double n = trials;
        double p = (double)successes / trials;

        double denominator = 1 + (z * z / n);
        double center = (p + (z * z / (2 * n))) / denominator;
        double margin = z * Math.Sqrt((p * (1 - p) + (z * z / (4 * n))) / n) / denominator;

        double lower = Math.Max(0, center - margin) * 100;
        double upper = Math.Min(1, center + margin) * 100;

        return (lower, upper);
    }
}
