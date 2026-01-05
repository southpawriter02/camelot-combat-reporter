using CamelotCombatReporter.Core.DeathAnalysis.Models;
using CamelotCombatReporter.Core.Models;
using Microsoft.Extensions.Logging;

namespace CamelotCombatReporter.Core.DeathAnalysis;

/// <summary>
/// Service for analyzing player deaths and generating reports.
/// </summary>
public class DeathAnalysisService : IDeathAnalysisService
{
    private readonly ILogger<DeathAnalysisService>? _logger;

    /// <inheritdoc />
    public TimeSpan PreDeathWindow { get; set; } = TimeSpan.FromSeconds(15);

    /// <summary>
    /// Creates a new DeathAnalysisService.
    /// </summary>
    /// <param name="logger">Optional logger.</param>
    public DeathAnalysisService(ILogger<DeathAnalysisService>? logger = null)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public DeathReport AnalyzeDeath(
        DeathEvent death,
        IEnumerable<LogEvent> allEvents,
        CharacterClass? victimClass = null)
    {
        var eventsList = allEvents.ToList();
        var deathTime = death.Timestamp;
        var windowStart = deathTime.Add(-PreDeathWindow);

        // Collect damage events in the pre-death window where target is "You"
        var damageEvents = eventsList
            .OfType<DamageEvent>()
            .Where(e => e.Target == "You" &&
                       e.Timestamp >= windowStart &&
                       e.Timestamp <= deathTime)
            .OrderBy(e => e.Timestamp)
            .ToList();

        // Collect healing events in the pre-death window
        var healingEvents = eventsList
            .OfType<HealingEvent>()
            .Where(e => e.Target == "You" &&
                       e.Timestamp >= windowStart &&
                       e.Timestamp <= deathTime)
            .ToList();

        // Check for CC events
        var ccEvents = eventsList
            .OfType<CrowdControlEvent>()
            .Where(e => e.Target == "You" &&
                       e.IsApplied &&
                       e.Timestamp >= windowStart &&
                       e.Timestamp <= deathTime)
            .ToList();

        // Calculate totals
        var totalDamage = damageEvents.Sum(e => e.DamageAmount);
        var totalHealing = healingEvents.Sum(e => e.HealingAmount);
        var wasCrowdControlled = ccEvents.Any();

        // Determine time to death
        var firstDamageTime = damageEvents.FirstOrDefault()?.Timestamp;
        var timeToDeath = firstDamageTime.HasValue
            ? deathTime - firstDamageTime.Value
            : TimeSpan.Zero;

        // Group damage by source
        var damageSources = BuildDamageSources(damageEvents, totalDamage);

        // Build damage timeline
        var damageTimeline = BuildDamageTimeline(damageEvents, deathTime);

        // Determine killing blow
        var killingBlow = DetermineKillingBlow(damageEvents);

        // Categorize death
        var category = CategorizeDeathInternal(
            timeToDeath,
            damageSources.Count,
            wasCrowdControlled,
            killingBlow,
            totalDamage,
            totalHealing);

        // Generate recommendations
        var recommendations = GenerateRecommendations(
            new DeathReport(
                Guid.NewGuid(),
                death,
                category,
                timeToDeath,
                killingBlow,
                damageSources,
                damageTimeline,
                Array.Empty<MissedOpportunity>(),
                Array.Empty<Recommendation>(),
                totalDamage,
                totalHealing,
                wasCrowdControlled,
                damageSources.Count),
            victimClass);

        _logger?.LogDebug("Analyzed death at {Time}: {Category}, TTD={TTD:F1}s, Sources={Sources}",
            deathTime, category, timeToDeath.TotalSeconds, damageSources.Count);

        return new DeathReport(
            Id: Guid.NewGuid(),
            DeathEvent: death,
            Category: category,
            TimeToDeath: timeToDeath,
            KillingBlow: killingBlow,
            DamageSources: damageSources,
            DamageTimeline: damageTimeline,
            MissedOpportunities: Array.Empty<MissedOpportunity>(), // Could be extended
            Recommendations: recommendations,
            TotalDamageTaken: totalDamage,
            TotalHealingReceived: totalHealing,
            WasCrowdControlled: wasCrowdControlled,
            AttackerCount: damageSources.Count
        );
    }

    /// <inheritdoc />
    public IReadOnlyList<DeathReport> AnalyzeAllDeaths(
        IEnumerable<LogEvent> allEvents,
        CharacterClass? playerClass = null)
    {
        var eventsList = allEvents.ToList();

        // Find all death events where the target is "You"
        var playerDeaths = eventsList
            .OfType<DeathEvent>()
            .Where(d => d.Target == "You")
            .OrderBy(d => d.Timestamp)
            .ToList();

        _logger?.LogInformation("Found {Count} player deaths to analyze", playerDeaths.Count);

        return playerDeaths
            .Select(death => AnalyzeDeath(death, eventsList, playerClass))
            .ToList();
    }

    /// <inheritdoc />
    public DeathStatistics GetStatistics(
        IEnumerable<DeathReport> deaths,
        TimeSpan? sessionDuration = null)
    {
        var deathsList = deaths.ToList();

        if (deathsList.Count == 0)
        {
            return new DeathStatistics(
                TotalDeaths: 0,
                DeathsPerHour: 0,
                AverageTimeToDeath: TimeSpan.Zero,
                TopKillerClasses: new Dictionary<CharacterClass, int>(),
                TopKillingAbilities: new Dictionary<string, int>(),
                DeathsByCategory: new Dictionary<DeathCategory, int>(),
                AverageDamageTaken: 0,
                CCDeathPercent: 0
            );
        }

        // Calculate deaths per hour
        var deathsPerHour = sessionDuration.HasValue && sessionDuration.Value.TotalHours > 0
            ? deathsList.Count / sessionDuration.Value.TotalHours
            : 0;

        // Average TTD
        var avgTTD = TimeSpan.FromTicks((long)deathsList.Average(d => d.TimeToDeath.Ticks));

        // Top killer classes
        var killerClasses = deathsList
            .Where(d => d.KillingBlow?.AttackerClass.HasValue == true)
            .GroupBy(d => d.KillingBlow!.AttackerClass!.Value)
            .ToDictionary(g => g.Key, g => g.Count());

        // Top killing abilities
        var killingAbilities = deathsList
            .Where(d => !string.IsNullOrEmpty(d.KillingBlow?.AbilityName))
            .GroupBy(d => d.KillingBlow!.AbilityName!)
            .ToDictionary(g => g.Key, g => g.Count());

        // Deaths by category
        var byCategory = deathsList
            .GroupBy(d => d.Category)
            .ToDictionary(g => g.Key, g => g.Count());

        // Average damage taken
        var avgDamage = deathsList.Average(d => d.TotalDamageTaken);

        // CC death percentage
        var ccDeaths = deathsList.Count(d => d.WasCrowdControlled);
        var ccPercent = (double)ccDeaths / deathsList.Count * 100;

        return new DeathStatistics(
            TotalDeaths: deathsList.Count,
            DeathsPerHour: deathsPerHour,
            AverageTimeToDeath: avgTTD,
            TopKillerClasses: killerClasses,
            TopKillingAbilities: killingAbilities,
            DeathsByCategory: byCategory,
            AverageDamageTaken: avgDamage,
            CCDeathPercent: ccPercent
        );
    }

    /// <inheritdoc />
    public IReadOnlyList<Recommendation> GenerateRecommendations(
        DeathReport report,
        CharacterClass? playerClass = null)
    {
        var recommendations = new List<Recommendation>();

        // Category-specific recommendations
        switch (report.Category)
        {
            case DeathCategory.BurstAlphaStrike:
                recommendations.Add(new Recommendation(
                    RecommendationType.Positioning,
                    "Avoid Isolation",
                    "You were killed quickly by a single attacker. Try to stay with your group to avoid being targeted alone.",
                    RecommendationPriority.High));
                break;

            case DeathCategory.BurstCoordinated:
                recommendations.Add(new Recommendation(
                    RecommendationType.Awareness,
                    "Watch for Assist Trains",
                    "Multiple enemies focused you simultaneously. Keep an eye on enemy movements and be ready to use defensive cooldowns early.",
                    RecommendationPriority.High));
                break;

            case DeathCategory.BurstCCChain:
                recommendations.Add(new Recommendation(
                    RecommendationType.AbilityUsage,
                    "Pre-emptive CC Break",
                    "You died while crowd controlled. Consider using Purge or other CC-break abilities before enemies finish you.",
                    RecommendationPriority.Critical));
                break;

            case DeathCategory.AttritionHealingDeficit:
                recommendations.Add(new Recommendation(
                    RecommendationType.Disengagement,
                    "Disengage Earlier",
                    "Damage outpaced healing over time. Consider retreating to reset when healing cannot keep up.",
                    RecommendationPriority.Medium));
                break;

            case DeathCategory.ExecutionDoT:
                recommendations.Add(new Recommendation(
                    RecommendationType.AbilityUsage,
                    "Cleanse DoTs",
                    "You were killed by damage over time effects. Use cleanse abilities or potions to remove harmful DoTs.",
                    RecommendationPriority.Medium));
                break;
        }

        // Multi-attacker recommendations
        if (report.AttackerCount > 2)
        {
            recommendations.Add(new Recommendation(
                RecommendationType.Positioning,
                "Avoid Outnumbered Fights",
                $"You were attacked by {report.AttackerCount} enemies. Reposition to fight more favorable odds.",
                RecommendationPriority.Medium));
        }

        // CC-based recommendations
        if (report.WasCrowdControlled)
        {
            recommendations.Add(new Recommendation(
                RecommendationType.ClassCounter,
                "CC Awareness",
                "You were CC'd before death. Stay at range from CC classes or use CC immunity abilities proactively.",
                RecommendationPriority.High));
        }

        // Sort by priority
        return recommendations
            .OrderByDescending(r => r.Priority)
            .ToList();
    }

    private static IReadOnlyList<DamageSource> BuildDamageSources(
        List<DamageEvent> damageEvents,
        int totalDamage)
    {
        if (totalDamage == 0)
            return Array.Empty<DamageSource>();

        return damageEvents
            .GroupBy(e => e.Source)
            .Select(g => new DamageSource(
                AttackerName: g.Key,
                AttackerClass: null, // Could be determined from game data
                TotalDamage: g.Sum(e => e.DamageAmount),
                PercentOfTotal: g.Sum(e => e.DamageAmount) / (double)totalDamage * 100,
                Events: g.ToList()
            ))
            .OrderByDescending(s => s.TotalDamage)
            .ToList();
    }

    private static IReadOnlyList<DamageTimelineBucket> BuildDamageTimeline(
        List<DamageEvent> damageEvents,
        TimeOnly deathTime)
    {
        if (!damageEvents.Any())
            return Array.Empty<DamageTimelineBucket>();

        return damageEvents
            .GroupBy(e => -(int)Math.Floor((deathTime - e.Timestamp).TotalSeconds))
            .Select(g => new DamageTimelineBucket(
                SecondOffset: g.Key,
                TotalDamage: g.Sum(e => e.DamageAmount),
                Events: g.ToList()
            ))
            .OrderBy(b => b.SecondOffset)
            .ToList();
    }

    private static KillingBlow? DetermineKillingBlow(List<DamageEvent> damageEvents)
    {
        if (!damageEvents.Any())
            return null;

        // The last damage event is assumed to be the killing blow
        var lastHit = damageEvents.Last();

        return new KillingBlow(
            AttackerName: lastHit.Source,
            AttackerClass: null, // Could be determined from game data
            AbilityName: lastHit.WeaponUsed, // Could be combat style or spell
            DamageAmount: lastHit.DamageAmount,
            OverkillAmount: 0, // Would need health tracking to calculate
            DamageType: lastHit.DamageType
        );
    }

    private static DeathCategory CategorizeDeathInternal(
        TimeSpan timeToDeath,
        int sourceCount,
        bool wasCrowdControlled,
        KillingBlow? killingBlow,
        int totalDamage,
        int totalHealing)
    {
        var ttdSeconds = timeToDeath.TotalSeconds;

        // Burst deaths (TTD < 3 seconds)
        if (ttdSeconds < 3)
        {
            if (wasCrowdControlled)
                return DeathCategory.BurstCCChain;
            if (sourceCount == 1)
                return DeathCategory.BurstAlphaStrike;
            return DeathCategory.BurstCoordinated;
        }

        // Attrition deaths (TTD > 10 seconds)
        if (ttdSeconds > 10)
        {
            var healingDeficit = totalDamage - totalHealing;
            if (healingDeficit > totalDamage * 0.5)
                return DeathCategory.AttritionHealingDeficit;
            return DeathCategory.AttritionPositional;
        }

        // Medium TTD - check for execution patterns
        if (killingBlow?.DamageType?.Contains("DoT", StringComparison.OrdinalIgnoreCase) == true)
            return DeathCategory.ExecutionDoT;

        // Default for medium TTD
        if (sourceCount > 1)
            return DeathCategory.BurstCoordinated;

        return DeathCategory.Unknown;
    }
}
