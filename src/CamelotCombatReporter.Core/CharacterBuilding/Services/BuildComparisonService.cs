using CamelotCombatReporter.Core.CharacterBuilding.Models;
using CamelotCombatReporter.Core.CharacterBuilding.Templates;

namespace CamelotCombatReporter.Core.CharacterBuilding.Services;

/// <summary>
/// Service for comparing character builds and calculating deltas.
/// </summary>
public class BuildComparisonService : IBuildComparisonService
{
    public BuildComparisonResult CompareBuilds(CharacterBuild buildA, CharacterBuild buildB)
    {
        ArgumentNullException.ThrowIfNull(buildA);
        ArgumentNullException.ThrowIfNull(buildB);

        var specDeltas = CalculateSpecDeltas(buildA, buildB);
        var raDeltas = CalculateRealmAbilityDeltas(buildA, buildB);
        var perfDeltas = CalculatePerformanceDeltas(buildA, buildB);

        return new BuildComparisonResult
        {
            BuildA = buildA,
            BuildB = buildB,
            SpecDeltas = specDeltas,
            RealmAbilityDeltas = raDeltas,
            PerformanceDeltas = perfDeltas
        };
    }

    private static List<SpecDelta> CalculateSpecDeltas(CharacterBuild buildA, CharacterBuild buildB)
    {
        var deltas = new List<SpecDelta>();
        
        // Get all unique spec names from both builds
        var allSpecs = buildA.SpecLines.Keys
            .Union(buildB.SpecLines.Keys)
            .Distinct()
            .OrderBy(s => s);

        foreach (var specName in allSpecs)
        {
            var valueA = buildA.SpecLines.TryGetValue(specName, out var a) ? a : 0;
            var valueB = buildB.SpecLines.TryGetValue(specName, out var b) ? b : 0;
            
            deltas.Add(new SpecDelta(specName, valueA, valueB, valueB - valueA));
        }

        return deltas;
    }

    private static List<RealmAbilityDelta> CalculateRealmAbilityDeltas(
        CharacterBuild buildA, CharacterBuild buildB)
    {
        var deltas = new List<RealmAbilityDelta>();
        
        var rasA = buildA.RealmAbilities.ToDictionary(ra => ra.AbilityName);
        var rasB = buildB.RealmAbilities.ToDictionary(ra => ra.AbilityName);

        // Find removed and changed RAs
        foreach (var (name, raA) in rasA)
        {
            if (rasB.TryGetValue(name, out var raB))
            {
                // RA exists in both - check for rank change
                if (raA.Rank != raB.Rank)
                {
                    deltas.Add(new RealmAbilityDelta(
                        name,
                        RealmAbilityChangeType.RankChanged,
                        raA.Rank,
                        raB.Rank,
                        RealmAbilityCatalog.GetPointCost(name, raA.Rank),
                        RealmAbilityCatalog.GetPointCost(name, raB.Rank)));
                }
            }
            else
            {
                // RA removed in B
                deltas.Add(new RealmAbilityDelta(
                    name,
                    RealmAbilityChangeType.Removed,
                    raA.Rank,
                    0,
                    RealmAbilityCatalog.GetPointCost(name, raA.Rank),
                    0));
            }
        }

        // Find added RAs
        foreach (var (name, raB) in rasB)
        {
            if (!rasA.ContainsKey(name))
            {
                deltas.Add(new RealmAbilityDelta(
                    name,
                    RealmAbilityChangeType.Added,
                    0,
                    raB.Rank,
                    0,
                    RealmAbilityCatalog.GetPointCost(name, raB.Rank)));
            }
        }

        return deltas.OrderBy(d => d.ChangeType).ThenBy(d => d.AbilityName).ToList();
    }

    private static PerformanceDelta? CalculatePerformanceDeltas(
        CharacterBuild buildA, CharacterBuild buildB)
    {
        var metricsA = buildA.PerformanceMetrics;
        var metricsB = buildB.PerformanceMetrics;

        // Only calculate if both builds have metrics
        if (metricsA == null || metricsB == null)
            return null;

        return new PerformanceDelta
        {
            DpsDelta = metricsB.AverageDps - metricsA.AverageDps,
            HpsDelta = metricsB.AverageHps - metricsA.AverageHps,
            KdRatioDelta = metricsB.KillDeathRatio - metricsA.KillDeathRatio,
            KillsDelta = metricsB.Kills - metricsA.Kills,
            DeathsDelta = metricsB.Deaths - metricsA.Deaths
        };
    }
}
