using CamelotCombatReporter.Core.CrowdControlAnalysis.Models;
using CamelotCombatReporter.Core.Models;
using Microsoft.Extensions.Logging;

namespace CamelotCombatReporter.Core.CrowdControlAnalysis;

/// <summary>
/// Service for analyzing crowd control usage and effectiveness.
/// </summary>
public class CCAnalysisService : ICCAnalysisService
{
    private readonly ILogger<CCAnalysisService>? _logger;
    private readonly DRTracker _drTracker = new();

    /// <inheritdoc />
    public DRTracker DRTracker => _drTracker;

    /// <inheritdoc />
    public TimeSpan ChainGapThreshold { get; set; } = TimeSpan.FromSeconds(2);

    /// <summary>
    /// Creates a new CCAnalysisService.
    /// </summary>
    /// <param name="logger">Optional logger.</param>
    public CCAnalysisService(ILogger<CCAnalysisService>? logger = null)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public IReadOnlyList<CCApplication> ExtractCCApplications(IEnumerable<LogEvent> events)
    {
        var applications = new List<CCApplication>();
        _drTracker.Clear();

        var ccEvents = events
            .OfType<CrowdControlEvent>()
            .Where(e => e.IsApplied)
            .OrderBy(e => e.Timestamp)
            .ToList();

        foreach (var ccEvent in ccEvents)
        {
            var ccType = ParseCCType(ccEvent.EffectType);
            if (ccType == null)
                continue;

            // Get current DR and apply CC
            var drLevel = _drTracker.ApplyCC(ccEvent.Target, ccType.Value, ccEvent.Timestamp);

            // Calculate effective duration
            var baseDuration = ccEvent.Duration.HasValue
                ? TimeSpan.FromSeconds(ccEvent.Duration.Value)
                : GetDefaultDuration(ccType.Value);

            var effectiveDuration = DRTracker.CalculateEffectiveDuration(baseDuration, drLevel);

            applications.Add(new CCApplication(
                Id: Guid.NewGuid(),
                Timestamp: ccEvent.Timestamp,
                CrowdControlType: ccType.Value,
                TargetName: ccEvent.Target,
                SourceName: ccEvent.Source,
                BaseDuration: baseDuration,
                DRAtApplication: drLevel,
                EffectiveDuration: effectiveDuration
            ));

            _logger?.LogTrace("CC applied: {Type} on {Target} at {Time}, DR={DR}",
                ccType, ccEvent.Target, ccEvent.Timestamp, drLevel);
        }

        return applications;
    }

    /// <inheritdoc />
    public IReadOnlyList<CCChain> DetectChains(IEnumerable<CCApplication> applications)
    {
        var chains = new List<CCChain>();
        var byTarget = applications.GroupBy(a => a.TargetName);

        foreach (var targetGroup in byTarget)
        {
            var ordered = targetGroup.OrderBy(a => a.Timestamp).ToList();
            var currentChain = new List<CCApplication>();

            foreach (var app in ordered)
            {
                if (currentChain.Count == 0)
                {
                    currentChain.Add(app);
                    continue;
                }

                var lastApp = currentChain.Last();
                var lastEnd = lastApp.Timestamp.Add(lastApp.EffectiveDuration);
                var gap = app.Timestamp - lastEnd;

                if (gap <= ChainGapThreshold)
                {
                    currentChain.Add(app);
                }
                else
                {
                    // End current chain if it has multiple effects
                    if (currentChain.Count > 1)
                    {
                        chains.Add(BuildChain(targetGroup.Key, currentChain));
                    }
                    currentChain = new List<CCApplication> { app };
                }
            }

            // Don't forget the last chain
            if (currentChain.Count > 1)
            {
                chains.Add(BuildChain(targetGroup.Key, currentChain));
            }
        }

        _logger?.LogDebug("Detected {Count} CC chains", chains.Count);
        return chains;
    }

    /// <inheritdoc />
    public CCStatistics CalculateStatistics(
        IEnumerable<LogEvent> events,
        TimeSpan combatDuration)
    {
        var eventsList = events.ToList();
        var ccEvents = eventsList.OfType<CrowdControlEvent>().ToList();

        var applied = ccEvents.Count(e => e.IsApplied);
        var resistEvents = eventsList.OfType<ResistEvent>().Count();

        // Extract applications to calculate durations
        var applications = ExtractCCApplications(eventsList);

        // Calculate total CC duration
        var totalCCDuration = applications.Sum(a => a.EffectiveDuration.TotalSeconds);

        // CC uptime percentage
        var uptimePercent = combatDuration.TotalSeconds > 0
            ? totalCCDuration / combatDuration.TotalSeconds * 100
            : 0;

        // Average duration
        var avgDuration = applications.Any()
            ? TimeSpan.FromSeconds(applications.Average(a => a.EffectiveDuration.TotalSeconds))
            : TimeSpan.Zero;

        // DR efficiency (percentage of CC applied at Full DR)
        var fullDRCount = applications.Count(a => a.DRAtApplication == DRLevel.Full);
        var drEfficiency = applications.Any()
            ? (double)fullDRCount / applications.Count * 100
            : 100;

        // CC by type
        var byType = applications
            .GroupBy(a => a.CrowdControlType)
            .ToDictionary(g => g.Key, g => g.Count());

        // Calculate kills within CC window
        var deathEvents = eventsList.OfType<DeathEvent>().ToList();
        var killsWithinCC = CalculateKillsWithinCCWindow(applications, deathEvents);

        // Calculate damage during CC
        var damageEvents = eventsList.OfType<DamageEvent>().ToList();
        var damageDuringCC = CalculateDamageDuringCC(applications, damageEvents);

        return new CCStatistics(
            TotalCCApplied: applied,
            TotalCCResisted: resistEvents,
            TotalCCBroken: 0, // Would need CC break events to track
            CCUptimePercent: uptimePercent,
            AverageDuration: avgDuration,
            BreakRatePercent: 0,
            DREfficiencyPercent: drEfficiency,
            CCByType: byType,
            KillsWithinCCWindow: killsWithinCC,
            TotalDamageDuringCC: damageDuringCC
        );
    }

    /// <inheritdoc />
    public IReadOnlyList<CCTimelineEntry> BuildTimeline(IEnumerable<LogEvent> events)
    {
        var timeline = new List<CCTimelineEntry>();
        _drTracker.Clear();

        var ccEvents = events
            .OfType<CrowdControlEvent>()
            .OrderBy(e => e.Timestamp)
            .ToList();

        foreach (var ccEvent in ccEvents)
        {
            var ccType = ParseCCType(ccEvent.EffectType);
            if (ccType == null)
                continue;

            CCEventType eventType;
            DRLevel drLevel;

            if (ccEvent.IsApplied)
            {
                drLevel = _drTracker.ApplyCC(ccEvent.Target, ccType.Value, ccEvent.Timestamp);
                eventType = drLevel == DRLevel.Immune ? CCEventType.Immune : CCEventType.Applied;
            }
            else
            {
                drLevel = _drTracker.GetCurrentDR(ccEvent.Target, ccType.Value, ccEvent.Timestamp);
                eventType = CCEventType.Expired;
            }

            var duration = ccEvent.Duration.HasValue
                ? TimeSpan.FromSeconds(ccEvent.Duration.Value)
                : (TimeSpan?)null;

            if (ccEvent.IsApplied && drLevel != DRLevel.Immune && duration.HasValue)
            {
                duration = DRTracker.CalculateEffectiveDuration(duration.Value, drLevel);
            }

            timeline.Add(new CCTimelineEntry(
                Timestamp: ccEvent.Timestamp,
                CrowdControlType: ccType.Value,
                TargetName: ccEvent.Target,
                SourceName: ccEvent.Source,
                EventType: eventType,
                DRLevel: drLevel,
                Duration: duration,
                DisplayColor: CCTimelineEntry.GetColorForDRLevel(drLevel)
            ));
        }

        // Add resist events
        var resistEvents = events.OfType<ResistEvent>().OrderBy(e => e.Timestamp);
        foreach (var resist in resistEvents)
        {
            timeline.Add(new CCTimelineEntry(
                Timestamp: resist.Timestamp,
                CrowdControlType: CCType.Mez, // Unknown, but we mark it
                TargetName: resist.Target,
                SourceName: null,
                EventType: CCEventType.Resisted,
                DRLevel: DRLevel.Full,
                Duration: null,
                DisplayColor: "#9E9E9E" // Gray for resists
            ));
        }

        return timeline.OrderBy(t => t.Timestamp).ToList();
    }

    /// <inheritdoc />
    public void Reset()
    {
        _drTracker.Clear();
    }

    private CCChain BuildChain(string targetName, List<CCApplication> applications)
    {
        var first = applications.First();
        var last = applications.Last();
        var chainEnd = last.Timestamp.Add(last.EffectiveDuration);

        // Calculate total duration and gaps
        var totalDuration = TimeSpan.Zero;
        var gapTime = TimeSpan.Zero;
        var overlapTime = TimeSpan.Zero;

        TimeOnly? previousEnd = null;

        foreach (var app in applications)
        {
            totalDuration += app.EffectiveDuration;

            if (previousEnd.HasValue)
            {
                var gap = app.Timestamp - previousEnd.Value;
                if (gap > TimeSpan.Zero)
                    gapTime += gap;
                else
                    overlapTime += gap.Negate();
            }

            previousEnd = app.Timestamp.Add(app.EffectiveDuration);
        }

        // Chain efficiency: how much of the chain duration was the target actually CC'd
        var chainDuration = chainEnd - first.Timestamp;
        var efficiency = chainDuration.TotalSeconds > 0
            ? (totalDuration.TotalSeconds - overlapTime.TotalSeconds) / chainDuration.TotalSeconds * 100
            : 0;

        return new CCChain(
            Id: Guid.NewGuid(),
            StartTime: first.Timestamp,
            EndTime: chainEnd,
            TargetName: targetName,
            Applications: applications,
            TotalDuration: totalDuration,
            GapTime: gapTime,
            OverlapTime: overlapTime,
            ChainLength: applications.Count,
            EfficiencyPercent: Math.Min(100, efficiency)
        );
    }

    private static int CalculateKillsWithinCCWindow(
        IReadOnlyList<CCApplication> applications,
        List<DeathEvent> deathEvents)
    {
        var killWindow = TimeSpan.FromSeconds(5);
        var kills = 0;

        foreach (var death in deathEvents)
        {
            // Check if any CC ended within 5 seconds before death
            var recentCC = applications.Any(cc =>
                cc.TargetName == death.Target &&
                death.Timestamp - cc.Timestamp.Add(cc.EffectiveDuration) <= killWindow &&
                death.Timestamp >= cc.Timestamp);

            if (recentCC)
                kills++;
        }

        return kills;
    }

    private static int CalculateDamageDuringCC(
        IReadOnlyList<CCApplication> applications,
        List<DamageEvent> damageEvents)
    {
        var totalDamage = 0;

        foreach (var app in applications)
        {
            var ccEnd = app.Timestamp.Add(app.EffectiveDuration);

            var damageInWindow = damageEvents
                .Where(d => d.Target == app.TargetName &&
                           d.Timestamp >= app.Timestamp &&
                           d.Timestamp <= ccEnd)
                .Sum(d => d.DamageAmount);

            totalDamage += damageInWindow;
        }

        return totalDamage;
    }

    private static CCType? ParseCCType(string effectType)
    {
        return effectType.ToLowerInvariant() switch
        {
            "stun" => CCType.Stun,
            "mez" or "mesmerize" => CCType.Mez,
            "root" => CCType.Root,
            "snare" => CCType.Snare,
            "silence" => CCType.Silence,
            "disarm" => CCType.Disarm,
            _ => null
        };
    }

    private static TimeSpan GetDefaultDuration(CCType ccType)
    {
        // Default durations when not specified in the log
        return ccType switch
        {
            CCType.Mez => TimeSpan.FromSeconds(60),
            CCType.Stun => TimeSpan.FromSeconds(9),
            CCType.Root => TimeSpan.FromSeconds(30),
            CCType.Snare => TimeSpan.FromSeconds(30),
            CCType.Silence => TimeSpan.FromSeconds(20),
            CCType.Disarm => TimeSpan.FromSeconds(20),
            _ => TimeSpan.FromSeconds(10)
        };
    }
}
