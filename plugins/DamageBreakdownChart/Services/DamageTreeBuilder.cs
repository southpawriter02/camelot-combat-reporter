using CamelotCombatReporter.Core.Models;
using DamageBreakdownChart.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace DamageBreakdownChart.Services;

/// <summary>
/// Builds a hierarchical damage tree from combat events.
/// </summary>
/// <remarks>
/// <para>
/// Creates a tree structure for visualization:
/// <list type="bullet">
///   <item><description>Level 0: Root (all damage)</description></item>
///   <item><description>Level 1: Damage Type (Slash, Heat, etc.)</description></item>
///   <item><description>Level 2: Ability Category (Combat Style, Spell, Auto)</description></item>
///   <item><description>Level 3: Source (ability/weapon name)</description></item>
///   <item><description>Level 4: Target</description></item>
/// </list>
/// </para>
/// </remarks>
public class DamageTreeBuilder
{
    private readonly ILogger<DamageTreeBuilder> _logger;

    public DamageTreeBuilder(ILogger<DamageTreeBuilder>? logger = null)
    {
        _logger = logger ?? NullLogger<DamageTreeBuilder>.Instance;
    }

    /// <summary>
    /// Builds a damage tree from log events.
    /// </summary>
    public DamageNode BuildTree(IReadOnlyList<LogEvent> events)
    {
        var damageEvents = events
            .OfType<DamageEvent>()
            .Where(e => e.DamageAmount > 0)
            .ToList();

        if (damageEvents.Count == 0)
        {
            _logger.LogDebug("No damage events found");
            return DamageNode.EmptyRoot;
        }

        var totalDamage = damageEvents.Sum(e => (long)e.DamageAmount);
        _logger.LogDebug("Building tree from {Count} damage events, total {Damage}", damageEvents.Count, totalDamage);

        // Group by damage type
        var byType = damageEvents
            .GroupBy(e => e.DamageType ?? "Unknown")
            .OrderByDescending(g => g.Sum(e => e.DamageAmount))
            .Select(g => BuildTypeNode(g.Key, g.ToList(), totalDamage))
            .ToList();

        return new DamageNode(
            Id: "root",
            Name: "All Damage",
            Type: DamageNodeType.Root,
            TotalDamage: totalDamage,
            HitCount: damageEvents.Count,
            Percentage: 100.0,
            Children: byType);
    }

    private DamageNode BuildTypeNode(string damageType, List<DamageEvent> events, long totalDamage)
    {
        var typeDamage = events.Sum(e => (long)e.DamageAmount);
        var percentage = totalDamage > 0 ? (double)typeDamage / totalDamage * 100 : 0;

        // Group by ability category
        var byCategory = events
            .GroupBy(e => ClassifyAbility(e))
            .OrderByDescending(g => g.Sum(e => e.DamageAmount))
            .Select(g => BuildCategoryNode(g.Key, g.ToList(), totalDamage))
            .ToList();

        return new DamageNode(
            Id: $"type-{damageType.ToLowerInvariant()}",
            Name: damageType,
            Type: DamageNodeType.DamageType,
            TotalDamage: typeDamage,
            HitCount: events.Count,
            Percentage: percentage,
            Children: byCategory);
    }

    private DamageNode BuildCategoryNode(string category, List<DamageEvent> events, long totalDamage)
    {
        var categoryDamage = events.Sum(e => (long)e.DamageAmount);
        var percentage = totalDamage > 0 ? (double)categoryDamage / totalDamage * 100 : 0;

        // Group by source (ability/weapon)
        var bySource = events
            .GroupBy(e => e.Source ?? "Unknown")
            .OrderByDescending(g => g.Sum(e => e.DamageAmount))
            .Take(20) // Limit to top 20 sources per category
            .Select(g => BuildSourceNode(g.Key, g.ToList(), totalDamage))
            .ToList();

        return new DamageNode(
            Id: $"cat-{category.ToLowerInvariant().Replace(" ", "-")}",
            Name: category,
            Type: DamageNodeType.AbilityCategory,
            TotalDamage: categoryDamage,
            HitCount: events.Count,
            Percentage: percentage,
            Children: bySource);
    }

    private DamageNode BuildSourceNode(string source, List<DamageEvent> events, long totalDamage)
    {
        var sourceDamage = events.Sum(e => (long)e.DamageAmount);
        var percentage = totalDamage > 0 ? (double)sourceDamage / totalDamage * 100 : 0;

        // Group by target
        var byTarget = events
            .GroupBy(e => e.Target ?? "Unknown")
            .OrderByDescending(g => g.Sum(e => e.DamageAmount))
            .Take(10) // Limit to top 10 targets per source
            .Select(g => BuildTargetNode(g.Key, g.ToList(), totalDamage))
            .ToList();

        return new DamageNode(
            Id: $"source-{source.ToLowerInvariant().Replace(" ", "-")}",
            Name: source,
            Type: DamageNodeType.AbilityName,
            TotalDamage: sourceDamage,
            HitCount: events.Count,
            Percentage: percentage,
            Children: byTarget);
    }

    private DamageNode BuildTargetNode(string target, List<DamageEvent> events, long totalDamage)
    {
        var targetDamage = events.Sum(e => (long)e.DamageAmount);
        var percentage = totalDamage > 0 ? (double)targetDamage / totalDamage * 100 : 0;

        return new DamageNode(
            Id: $"target-{target.ToLowerInvariant().Replace(" ", "-")}",
            Name: target,
            Type: DamageNodeType.Target,
            TotalDamage: targetDamage,
            HitCount: events.Count,
            Percentage: percentage,
            Children: Array.Empty<DamageNode>());
    }

    private static string ClassifyAbility(DamageEvent evt)
    {
        var source = evt.Source?.ToLowerInvariant() ?? string.Empty;

        // Combat styles typically have specific patterns
        if (source.Contains("style") || source.Contains("taunt") || source.Contains("slam") ||
            source.Contains("garrote") || source.Contains("backstab") || source.Contains("pierce"))
            return "Combat Style";

        // Spells
        if (source.Contains("bolt") || source.Contains("blast") || source.Contains("nuke") ||
            source.Contains("spell") || source.Contains("cast") || source.Contains("dd"))
            return "Spell";

        // DoTs
        if (source.Contains("dot") || source.Contains("bleed") || source.Contains("poison"))
            return "DoT";

        // Procs
        if (source.Contains("proc") || source.Contains("reactive"))
            return "Proc";

        // Pet damage
        if (source.Contains("pet") || source.Contains("minion") || source.Contains("summon"))
            return "Pet";

        // Default to auto-attack
        if (string.IsNullOrEmpty(source) || source == "attack" || source == "hit")
            return "Auto-attack";

        return "Other";
    }
}
