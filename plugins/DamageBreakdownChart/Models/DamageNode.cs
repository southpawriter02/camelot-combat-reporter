namespace DamageBreakdownChart.Models;

/// <summary>
/// Hierarchical node representing damage aggregated at a specific level.
/// </summary>
/// <param name="Id">Unique identifier for the node.</param>
/// <param name="Name">Display name.</param>
/// <param name="Type">Type of this node in the hierarchy.</param>
/// <param name="TotalDamage">Sum of all damage in this node and children.</param>
/// <param name="HitCount">Number of damage events.</param>
/// <param name="Percentage">Percentage of parent's total damage.</param>
/// <param name="Children">Child nodes in the hierarchy.</param>
public record DamageNode(
    string Id,
    string Name,
    DamageNodeType Type,
    long TotalDamage,
    int HitCount,
    double Percentage,
    IReadOnlyList<DamageNode> Children)
{
    /// <summary>
    /// Average damage per hit.
    /// </summary>
    public double AverageDamage => HitCount > 0 ? (double)TotalDamage / HitCount : 0;

    /// <summary>
    /// Creates an empty root node.
    /// </summary>
    public static DamageNode EmptyRoot => new(
        "root",
        "All Damage",
        DamageNodeType.Root,
        0,
        0,
        100.0,
        Array.Empty<DamageNode>());
}
