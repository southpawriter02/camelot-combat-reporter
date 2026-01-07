using CamelotCombatReporter.Core.CharacterBuilding.Models;
using CamelotCombatReporter.Core.CharacterBuilding.Templates;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace CamelotCombatReporter.Core.CharacterBuilding.Services;

/// <summary>
/// Service for comparing character builds and calculating deltas.
/// </summary>
/// <remarks>
/// <para>
/// This service performs side-by-side comparison of two character builds,
/// calculating differences in:
/// </para>
/// <list type="bullet">
///   <item><description>Specialization line allocations (points per spec)</description></item>
///   <item><description>Realm ability selections (added/removed/rank changed)</description></item>
///   <item><description>Performance metrics (if both builds have attached session data)</description></item>
/// </list>
/// <para>
/// The comparison treats Build A as the baseline and Build B as the comparison target.
/// Positive deltas indicate Build B has higher values than Build A.
/// </para>
/// </remarks>
public class BuildComparisonService : IBuildComparisonService
{
    private readonly ILogger<BuildComparisonService> _logger;

    /// <summary>
    /// Initializes a new instance of the BuildComparisonService.
    /// </summary>
    /// <param name="logger">Optional logger for diagnostics. Uses NullLogger if not provided.</param>
    public BuildComparisonService(ILogger<BuildComparisonService>? logger = null)
    {
        _logger = logger ?? NullLogger<BuildComparisonService>.Instance;
    }

    /// <inheritdoc/>
    public BuildComparisonResult CompareBuilds(CharacterBuild buildA, CharacterBuild buildB)
    {
        ArgumentNullException.ThrowIfNull(buildA);
        ArgumentNullException.ThrowIfNull(buildB);

        _logger.LogDebug("Comparing builds: '{BuildA}' vs '{BuildB}'", buildA.Name, buildB.Name);

        // Calculate all delta categories
        var specDeltas = CalculateSpecDeltas(buildA, buildB);
        var raDeltas = CalculateRealmAbilityDeltas(buildA, buildB);
        var perfDeltas = CalculatePerformanceDeltas(buildA, buildB);

        var result = new BuildComparisonResult
        {
            BuildA = buildA,
            BuildB = buildB,
            SpecDeltas = specDeltas,
            RealmAbilityDeltas = raDeltas,
            PerformanceDeltas = perfDeltas
        };

        // Log summary of comparison
        var changedSpecs = specDeltas.Count(s => s.Delta != 0);
        _logger.LogInformation(
            "Build comparison complete: {ChangedSpecs} spec changes, {RAChanges} RA changes, performance data: {HasPerf}",
            changedSpecs, raDeltas.Count, perfDeltas != null);

        return result;
    }

    /// <summary>
    /// Calculates the difference in spec line allocations between builds.
    /// </summary>
    /// <param name="buildA">Baseline build.</param>
    /// <param name="buildB">Comparison build.</param>
    /// <returns>List of spec deltas with value differences.</returns>
    /// <remarks>
    /// Includes all specs from both builds. Specs not present in one build
    /// are treated as having a value of 0.
    /// </remarks>
    private List<SpecDelta> CalculateSpecDeltas(CharacterBuild buildA, CharacterBuild buildB)
    {
        var deltas = new List<SpecDelta>();
        
        // Combine all unique spec names from both builds for complete comparison
        var allSpecs = buildA.SpecLines.Keys
            .Union(buildB.SpecLines.Keys)
            .Distinct()
            .OrderBy(s => s);

        foreach (var specName in allSpecs)
        {
            // Default to 0 if spec doesn't exist in a build
            var valueA = buildA.SpecLines.TryGetValue(specName, out var a) ? a : 0;
            var valueB = buildB.SpecLines.TryGetValue(specName, out var b) ? b : 0;
            
            deltas.Add(new SpecDelta(specName, valueA, valueB, valueB - valueA));
        }

        _logger.LogDebug("Calculated {Count} spec deltas", deltas.Count);
        return deltas;
    }

    /// <summary>
    /// Calculates the difference in realm ability selections between builds.
    /// </summary>
    /// <param name="buildA">Baseline build.</param>
    /// <param name="buildB">Comparison build.</param>
    /// <returns>List of RA deltas categorized by change type.</returns>
    /// <remarks>
    /// Change types are:
    /// <list type="bullet">
    ///   <item><description>Added: RA present in B but not A</description></item>
    ///   <item><description>Removed: RA present in A but not B</description></item>
    ///   <item><description>RankChanged: RA in both, but different ranks</description></item>
    /// </list>
    /// Point costs are calculated using the RealmAbilityCatalog.
    /// </remarks>
    private List<RealmAbilityDelta> CalculateRealmAbilityDeltas(
        CharacterBuild buildA, CharacterBuild buildB)
    {
        var deltas = new List<RealmAbilityDelta>();
        
        // Index RAs by name for O(1) lookups
        var rasA = buildA.RealmAbilities.ToDictionary(ra => ra.AbilityName);
        var rasB = buildB.RealmAbilities.ToDictionary(ra => ra.AbilityName);

        // Check for removed and rank-changed RAs
        foreach (var (name, raA) in rasA)
        {
            if (rasB.TryGetValue(name, out var raB))
            {
                // RA exists in both - check for rank change
                if (raA.Rank != raB.Rank)
                {
                    _logger.LogDebug("RA '{Name}' rank changed: {RankA} → {RankB}", name, raA.Rank, raB.Rank);
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
                // RA was removed in Build B
                _logger.LogDebug("RA '{Name}' removed in Build B", name);
                deltas.Add(new RealmAbilityDelta(
                    name,
                    RealmAbilityChangeType.Removed,
                    raA.Rank,
                    0,
                    RealmAbilityCatalog.GetPointCost(name, raA.Rank),
                    0));
            }
        }

        // Check for added RAs (present in B but not in A)
        foreach (var (name, raB) in rasB)
        {
            if (!rasA.ContainsKey(name))
            {
                _logger.LogDebug("RA '{Name}' added in Build B at rank {Rank}", name, raB.Rank);
                deltas.Add(new RealmAbilityDelta(
                    name,
                    RealmAbilityChangeType.Added,
                    0,
                    raB.Rank,
                    0,
                    RealmAbilityCatalog.GetPointCost(name, raB.Rank)));
            }
        }

        // Sort by change type (Added, Removed, RankChanged) then alphabetically
        return deltas.OrderBy(d => d.ChangeType).ThenBy(d => d.AbilityName).ToList();
    }

    /// <summary>
    /// Calculates the difference in performance metrics between builds.
    /// </summary>
    /// <param name="buildA">Baseline build.</param>
    /// <param name="buildB">Comparison build.</param>
    /// <returns>Performance delta, or null if either build lacks metrics.</returns>
    /// <remarks>
    /// Performance metrics are populated by the PerformanceAnalysisService
    /// after sessions are attached to builds. This comparison is only
    /// meaningful when both builds have sufficient session data.
    /// </remarks>
    private PerformanceDelta? CalculatePerformanceDeltas(
        CharacterBuild buildA, CharacterBuild buildB)
    {
        var metricsA = buildA.PerformanceMetrics;
        var metricsB = buildB.PerformanceMetrics;

        // Only calculate if both builds have performance data
        if (metricsA == null || metricsB == null)
        {
            _logger.LogDebug("Skipping performance comparison - one or both builds lack metrics");
            return null;
        }

        _logger.LogDebug(
            "Calculating performance deltas: DPS {DpsA:F1} vs {DpsB:F1}",
            metricsA.AverageDps, metricsB.AverageDps);

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

