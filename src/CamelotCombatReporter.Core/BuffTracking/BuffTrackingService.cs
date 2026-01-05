using CamelotCombatReporter.Core.BuffTracking.Models;
using CamelotCombatReporter.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace CamelotCombatReporter.Core.BuffTracking;

/// <summary>
/// Implementation of buff tracking and analysis.
/// </summary>
public class BuffTrackingService : IBuffTrackingService
{
    private readonly ILogger<BuffTrackingService> _logger;
    private List<string> _expectedBuffIds = new();

    /// <summary>
    /// Creates a new buff tracking service.
    /// </summary>
    /// <param name="logger">Optional logger.</param>
    public BuffTrackingService(ILogger<BuffTrackingService>? logger = null)
    {
        _logger = logger ?? NullLogger<BuffTrackingService>.Instance;
        StateTracker = new BuffStateTracker();
    }

    /// <inheritdoc/>
    public BuffStateTracker StateTracker { get; }

    /// <inheritdoc/>
    public IReadOnlyList<string> ExpectedBuffIds
    {
        get => _expectedBuffIds;
        set => _expectedBuffIds = value.ToList();
    }

    /// <inheritdoc/>
    public IReadOnlyList<BuffEvent> ExtractBuffEvents(IEnumerable<LogEvent> events)
    {
        var buffEvents = new List<BuffEvent>();

        foreach (var evt in events)
        {
            // Handle direct BuffEvent types if they exist
            if (evt is BuffEvent be)
            {
                buffEvents.Add(be);
                continue;
            }

            // TODO: Parse other event types for buff information
            // This would involve pattern matching on damage/healing events
            // to detect buff applications and expirations
        }

        return buffEvents;
    }

    /// <inheritdoc/>
    public IReadOnlyList<BuffTimelineEntry> BuildTimeline(IEnumerable<LogEvent> events)
    {
        var timeline = new List<BuffTimelineEntry>();
        var buffEvents = ExtractBuffEvents(events);

        foreach (var evt in buffEvents.OrderBy(e => e.Timestamp))
        {
            var duration = evt.Duration.HasValue
                ? TimeSpan.FromSeconds(evt.Duration.Value)
                : (TimeSpan?)null;

            timeline.Add(new BuffTimelineEntry(
                Timestamp: evt.Timestamp,
                BuffName: evt.BuffDefinition.Name,
                Category: evt.BuffDefinition.Category,
                EventType: evt.EventType,
                TargetName: evt.TargetName,
                TargetType: evt.TargetType,
                SourceName: evt.SourceName,
                Duration: duration,
                DisplayColor: BuffTimelineEntry.GetColorForCategory(
                    evt.BuffDefinition.Category,
                    evt.BuffDefinition.IsBeneficial),
                IsBeneficial: evt.BuffDefinition.IsBeneficial
            ));
        }

        return timeline;
    }

    /// <inheritdoc/>
    public BuffStatistics CalculateStatistics(
        IEnumerable<LogEvent> events,
        TimeSpan combatDuration,
        string? targetName = null)
    {
        var eventList = events.ToList();
        var buffEvents = ExtractBuffEvents(eventList);

        // Filter by target if specified
        if (!string.IsNullOrEmpty(targetName))
        {
            buffEvents = buffEvents
                .Where(e => e.TargetName.Equals(targetName, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        // Process events through state tracker
        StateTracker.Clear();
        foreach (var evt in buffEvents.OrderBy(e => e.Timestamp))
        {
            ProcessBuffEvent(evt);
        }

        // Calculate statistics
        var beneficialEvents = buffEvents.Where(e => e.BuffDefinition.IsBeneficial).ToList();
        var debuffEventsApplied = buffEvents.Where(e => !e.BuffDefinition.IsBeneficial && e.TargetType == BuffTargetType.Enemy).ToList();
        var debuffEventsReceived = buffEvents.Where(e => !e.BuffDefinition.IsBeneficial && e.TargetType == BuffTargetType.Self).ToList();

        var buffsByCategory = beneficialEvents
            .Where(e => e.EventType == BuffEventType.Applied)
            .GroupBy(e => e.BuffDefinition.Category)
            .ToDictionary(g => g.Key, g => g.Count());

        var debuffsByCategory = debuffEventsApplied.Concat(debuffEventsReceived)
            .Where(e => e.EventType == BuffEventType.Applied)
            .GroupBy(e => e.BuffDefinition.Category)
            .ToDictionary(g => g.Key, g => g.Count());

        // Calculate uptime for expected buffs
        var uptimeByBuff = new Dictionary<string, BuffUptimeStats>();
        var target = targetName ?? "You";

        foreach (var buffId in _expectedBuffIds)
        {
            var buff = BuffDatabase.GetById(buffId);
            if (buff != null && eventList.Any())
            {
                var start = eventList.Min(e => e.Timestamp);
                var end = eventList.Max(e => e.Timestamp);
                var uptime = StateTracker.CalculateUptime(target, buff, start, end);
                uptimeByBuff[buffId] = uptime;
            }
        }

        var overallUptime = uptimeByBuff.Count > 0
            ? uptimeByBuff.Values.Average(u => u.UptimePercent)
            : 100;

        var criticalGaps = StateTracker.GetGaps()
            .Where(g => g.GapDuration >= TimeSpan.FromSeconds(5))
            .ToList();

        var mostUsedBuffs = beneficialEvents
            .Where(e => e.EventType == BuffEventType.Applied)
            .GroupBy(e => e.BuffDefinition)
            .Select(g => (Buff: g.Key, Count: g.Count()))
            .OrderByDescending(x => x.Count)
            .Take(5)
            .ToList();

        var mostUsedDebuffs = debuffEventsApplied
            .Where(e => e.EventType == BuffEventType.Applied)
            .GroupBy(e => e.BuffDefinition)
            .Select(g => (Debuff: g.Key, Count: g.Count()))
            .OrderByDescending(x => x.Count)
            .Take(5)
            .ToList();

        return new BuffStatistics(
            TotalBuffsApplied: beneficialEvents.Count(e => e.EventType == BuffEventType.Applied),
            TotalDebuffsApplied: debuffEventsApplied.Count(e => e.EventType == BuffEventType.Applied),
            TotalDebuffsReceived: debuffEventsReceived.Count(e => e.EventType == BuffEventType.Applied),
            OverallBuffUptime: overallUptime,
            UptimeByBuff: uptimeByBuff,
            BuffsByCategory: buffsByCategory,
            DebuffsByCategory: debuffsByCategory,
            CriticalGaps: criticalGaps,
            MostUsedBuffs: mostUsedBuffs,
            MostUsedDebuffs: mostUsedDebuffs
        );
    }

    /// <inheritdoc/>
    public IReadOnlyList<BuffUptimeStats> CalculateUptimes(
        IEnumerable<LogEvent> events,
        IEnumerable<string> buffIds,
        TimeSpan combatDuration,
        string? targetName = null)
    {
        var eventList = events.ToList();
        if (!eventList.Any())
            return Array.Empty<BuffUptimeStats>();

        var target = targetName ?? "You";
        var start = eventList.Min(e => e.Timestamp);
        var end = eventList.Max(e => e.Timestamp);

        // Process events
        var buffEvents = ExtractBuffEvents(eventList);
        StateTracker.Clear();
        foreach (var evt in buffEvents.OrderBy(e => e.Timestamp))
        {
            ProcessBuffEvent(evt);
        }

        var results = new List<BuffUptimeStats>();

        foreach (var buffId in buffIds)
        {
            var buff = BuffDatabase.GetById(buffId);
            if (buff != null)
            {
                var uptime = StateTracker.CalculateUptime(target, buff, start, end);
                results.Add(uptime);
            }
        }

        return results;
    }

    /// <inheritdoc/>
    public IReadOnlyList<BuffGap> DetectCriticalGaps(
        IEnumerable<LogEvent> events,
        TimeSpan gapThreshold)
    {
        var eventList = events.ToList();
        var buffEvents = ExtractBuffEvents(eventList);

        StateTracker.Clear();
        foreach (var evt in buffEvents.OrderBy(e => e.Timestamp))
        {
            ProcessBuffEvent(evt);
        }

        return StateTracker.GetGaps()
            .Where(g => g.GapDuration >= gapThreshold)
            .OrderByDescending(g => g.GapDuration)
            .ToList();
    }

    /// <inheritdoc/>
    public IReadOnlyList<ActiveBuff> GetActiveBuffsAt(
        IEnumerable<LogEvent> events,
        TimeOnly timestamp,
        string? targetName = null)
    {
        var eventList = events.ToList();
        var buffEvents = ExtractBuffEvents(eventList)
            .Where(e => e.Timestamp <= timestamp)
            .OrderBy(e => e.Timestamp);

        StateTracker.Clear();
        foreach (var evt in buffEvents)
        {
            ProcessBuffEvent(evt);
        }

        if (!string.IsNullOrEmpty(targetName))
        {
            return StateTracker.GetActiveBuffs(targetName, timestamp);
        }

        // Get all active buffs across all targets
        var allBuffs = new List<ActiveBuff>();
        // Note: This would require tracking all targets seen
        // For now, default to "You"
        allBuffs.AddRange(StateTracker.GetActiveBuffs("You", timestamp));

        return allBuffs;
    }

    /// <inheritdoc/>
    public IReadOnlyList<string> GetMissingExpectedBuffs(
        IEnumerable<LogEvent> events,
        TimeOnly timestamp,
        string targetName)
    {
        var activeBuffs = GetActiveBuffsAt(events, timestamp, targetName);
        var activeBuffIds = activeBuffs.Select(b => b.BuffDefinition.BuffId).ToHashSet(StringComparer.OrdinalIgnoreCase);

        return _expectedBuffIds
            .Where(id => !activeBuffIds.Contains(id))
            .ToList();
    }

    /// <inheritdoc/>
    public void Reset()
    {
        StateTracker.Clear();
    }

    private void ProcessBuffEvent(BuffEvent evt)
    {
        switch (evt.EventType)
        {
            case BuffEventType.Applied:
            case BuffEventType.Refreshed:
                StateTracker.ApplyBuff(
                    target: evt.TargetName,
                    buff: evt.BuffDefinition,
                    timestamp: evt.Timestamp,
                    targetType: evt.TargetType,
                    source: evt.SourceName,
                    duration: evt.Duration,
                    magnitude: evt.Magnitude
                );
                break;

            case BuffEventType.Expired:
                StateTracker.RemoveBuff(
                    target: evt.TargetName,
                    buff: evt.BuffDefinition,
                    timestamp: evt.Timestamp,
                    wasDispelled: false
                );
                break;

            case BuffEventType.Removed:
                StateTracker.RemoveBuff(
                    target: evt.TargetName,
                    buff: evt.BuffDefinition,
                    timestamp: evt.Timestamp,
                    wasDispelled: true
                );
                break;
        }
    }
}
