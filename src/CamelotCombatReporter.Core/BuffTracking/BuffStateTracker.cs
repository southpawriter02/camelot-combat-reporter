using CamelotCombatReporter.Core.BuffTracking.Models;

namespace CamelotCombatReporter.Core.BuffTracking;

/// <summary>
/// Internal state for tracking an active buff.
/// </summary>
internal record ActiveBuffState(
    BuffDefinition BuffDefinition,
    string TargetName,
    BuffTargetType TargetType,
    TimeOnly AppliedAt,
    TimeOnly ExpiresAt,
    string? SourceName,
    int RefreshCount,
    int? Magnitude
);

/// <summary>
/// Tracks buff state with timer-based expiry estimation.
/// Follows the DRTracker pattern for state management.
/// </summary>
public class BuffStateTracker
{
    private readonly Dictionary<(string Target, string BuffId), ActiveBuffState> _activeBuffs = new();
    private readonly List<BuffEvent> _eventHistory = new();
    private readonly List<BuffGap> _gaps = new();

    /// <summary>
    /// Applies a buff to a target.
    /// </summary>
    /// <param name="target">Target name.</param>
    /// <param name="buff">Buff definition.</param>
    /// <param name="timestamp">When applied.</param>
    /// <param name="targetType">Type of target.</param>
    /// <param name="source">Source name.</param>
    /// <param name="duration">Override duration in seconds.</param>
    /// <param name="magnitude">Magnitude if applicable.</param>
    /// <returns>The event type (Applied or Refreshed).</returns>
    public BuffEventType ApplyBuff(
        string target,
        BuffDefinition buff,
        TimeOnly timestamp,
        BuffTargetType targetType = BuffTargetType.Self,
        string? source = null,
        int? duration = null,
        int? magnitude = null)
    {
        var key = (target, buff.BuffId);
        var effectiveDuration = duration ?? buff.DefaultDuration;
        var expiresAt = effectiveDuration > 0
            ? timestamp.Add(TimeSpan.FromSeconds(effectiveDuration))
            : TimeOnly.MaxValue; // Concentration or permanent buffs

        BuffEventType eventType;

        if (_activeBuffs.TryGetValue(key, out var existing))
        {
            // Buff is being refreshed
            eventType = BuffEventType.Refreshed;

            // Check if there was a gap (buff expired before refresh)
            if (timestamp > existing.ExpiresAt && buff.IsExpectedBuff)
            {
                RecordGap(buff, target, existing.ExpiresAt, timestamp, "Buff expired before refresh");
            }

            // Update with new state
            _activeBuffs[key] = new ActiveBuffState(
                BuffDefinition: buff,
                TargetName: target,
                TargetType: targetType,
                AppliedAt: existing.AppliedAt, // Keep original application time
                ExpiresAt: expiresAt,
                SourceName: source ?? existing.SourceName,
                RefreshCount: existing.RefreshCount + 1,
                Magnitude: magnitude ?? existing.Magnitude
            );
        }
        else
        {
            // New buff application
            eventType = BuffEventType.Applied;

            _activeBuffs[key] = new ActiveBuffState(
                BuffDefinition: buff,
                TargetName: target,
                TargetType: targetType,
                AppliedAt: timestamp,
                ExpiresAt: expiresAt,
                SourceName: source,
                RefreshCount: 0,
                Magnitude: magnitude
            );
        }

        // Record event
        var evt = new BuffEvent(
            Timestamp: timestamp,
            BuffDefinition: buff,
            EventType: eventType,
            TargetType: targetType,
            TargetName: target,
            SourceName: source,
            Duration: duration,
            Magnitude: magnitude
        );
        _eventHistory.Add(evt);

        return eventType;
    }

    /// <summary>
    /// Removes a buff from a target.
    /// </summary>
    /// <param name="target">Target name.</param>
    /// <param name="buff">Buff definition.</param>
    /// <param name="timestamp">When removed.</param>
    /// <param name="wasDispelled">True if actively removed, false if expired naturally.</param>
    public void RemoveBuff(string target, BuffDefinition buff, TimeOnly timestamp, bool wasDispelled = false)
    {
        var key = (target, buff.BuffId);

        if (_activeBuffs.TryGetValue(key, out var existing))
        {
            _activeBuffs.Remove(key);

            var eventType = wasDispelled ? BuffEventType.Removed : BuffEventType.Expired;

            var evt = new BuffEvent(
                Timestamp: timestamp,
                BuffDefinition: buff,
                EventType: eventType,
                TargetType: existing.TargetType,
                TargetName: target,
                SourceName: existing.SourceName,
                Duration: null,
                Magnitude: existing.Magnitude
            );
            _eventHistory.Add(evt);

            // Record gap start for expected buffs
            if (buff.IsExpectedBuff)
            {
                // Gap will be closed when buff is reapplied
                _gaps.Add(new BuffGap(
                    BuffDefinition: buff,
                    TargetName: target,
                    GapStart: timestamp,
                    GapEnd: null,
                    GapDuration: TimeSpan.Zero,
                    Context: wasDispelled ? "Buff was dispelled" : "Buff expired"
                ));
            }
        }
    }

    /// <summary>
    /// Checks if a buff is currently active.
    /// </summary>
    /// <param name="target">Target name.</param>
    /// <param name="buffId">Buff identifier.</param>
    /// <param name="currentTime">Current time for expiry check.</param>
    /// <returns>True if buff is active.</returns>
    public bool IsBuffActive(string target, string buffId, TimeOnly currentTime)
    {
        var key = (target, buffId);
        if (!_activeBuffs.TryGetValue(key, out var state))
            return false;

        // Check if expired based on timer
        if (state.ExpiresAt != TimeOnly.MaxValue && currentTime > state.ExpiresAt)
        {
            // Auto-expire
            _activeBuffs.Remove(key);
            return false;
        }

        return true;
    }

    /// <summary>
    /// Gets all active buffs for a target.
    /// </summary>
    /// <param name="target">Target name.</param>
    /// <param name="currentTime">Current time for expiry check.</param>
    /// <returns>List of active buffs.</returns>
    public IReadOnlyList<ActiveBuff> GetActiveBuffs(string target, TimeOnly currentTime)
    {
        CleanupExpiredBuffs(currentTime);

        return _activeBuffs
            .Where(kvp => kvp.Key.Target.Equals(target, StringComparison.OrdinalIgnoreCase))
            .Select(kvp => new ActiveBuff(
                BuffDefinition: kvp.Value.BuffDefinition,
                TargetName: kvp.Value.TargetName,
                TargetType: kvp.Value.TargetType,
                AppliedAt: kvp.Value.AppliedAt,
                ExpiresAt: kvp.Value.ExpiresAt,
                SourceName: kvp.Value.SourceName,
                RefreshCount: kvp.Value.RefreshCount,
                Magnitude: kvp.Value.Magnitude
            ))
            .ToList();
    }

    /// <summary>
    /// Gets remaining duration for a buff.
    /// </summary>
    /// <param name="target">Target name.</param>
    /// <param name="buffId">Buff identifier.</param>
    /// <param name="currentTime">Current time.</param>
    /// <returns>Remaining duration, or null if not active.</returns>
    public TimeSpan? GetRemainingDuration(string target, string buffId, TimeOnly currentTime)
    {
        var key = (target, buffId);
        if (!_activeBuffs.TryGetValue(key, out var state))
            return null;

        if (state.ExpiresAt == TimeOnly.MaxValue)
            return null; // Permanent/concentration buff

        var remaining = state.ExpiresAt.ToTimeSpan() - currentTime.ToTimeSpan();
        return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
    }

    /// <summary>
    /// Gets the event history.
    /// </summary>
    public IReadOnlyList<BuffEvent> GetEventHistory() => _eventHistory.AsReadOnly();

    /// <summary>
    /// Gets recorded gaps.
    /// </summary>
    public IReadOnlyList<BuffGap> GetGaps() => _gaps.AsReadOnly();

    /// <summary>
    /// Calculates uptime stats for a specific buff on a target.
    /// </summary>
    /// <param name="target">Target name.</param>
    /// <param name="buff">Buff definition.</param>
    /// <param name="combatStart">Combat start time.</param>
    /// <param name="combatEnd">Combat end time.</param>
    /// <returns>Uptime statistics.</returns>
    public BuffUptimeStats CalculateUptime(
        string target,
        BuffDefinition buff,
        TimeOnly combatStart,
        TimeOnly combatEnd)
    {
        var relevantEvents = _eventHistory
            .Where(e => e.BuffDefinition.BuffId == buff.BuffId &&
                       e.TargetName.Equals(target, StringComparison.OrdinalIgnoreCase) &&
                       e.Timestamp >= combatStart &&
                       e.Timestamp <= combatEnd)
            .OrderBy(e => e.Timestamp)
            .ToList();

        var totalCombatTime = combatEnd.ToTimeSpan() - combatStart.ToTimeSpan();
        var buffedTime = TimeSpan.Zero;
        var applicationCount = 0;
        var refreshCount = 0;
        var gaps = new List<BuffGap>();

        TimeOnly? buffStart = null;

        foreach (var evt in relevantEvents)
        {
            switch (evt.EventType)
            {
                case BuffEventType.Applied:
                    applicationCount++;
                    buffStart = evt.Timestamp;
                    break;

                case BuffEventType.Refreshed:
                    refreshCount++;
                    // Extend the current buff period
                    break;

                case BuffEventType.Expired:
                case BuffEventType.Removed:
                    if (buffStart.HasValue)
                    {
                        buffedTime += evt.Timestamp.ToTimeSpan() - buffStart.Value.ToTimeSpan();
                        buffStart = null;
                    }
                    break;
            }
        }

        // If buff is still active at end, count remaining time
        if (buffStart.HasValue)
        {
            buffedTime += combatEnd.ToTimeSpan() - buffStart.Value.ToTimeSpan();
        }

        var uptimePercent = totalCombatTime.TotalSeconds > 0
            ? (buffedTime.TotalSeconds / totalCombatTime.TotalSeconds) * 100
            : 0;

        var buffGaps = _gaps
            .Where(g => g.BuffDefinition.BuffId == buff.BuffId &&
                       g.TargetName.Equals(target, StringComparison.OrdinalIgnoreCase))
            .ToList();

        var avgGapDuration = buffGaps.Count > 0
            ? TimeSpan.FromTicks((long)buffGaps.Average(g => g.GapDuration.Ticks))
            : TimeSpan.Zero;

        var longestGap = buffGaps.MaxBy(g => g.GapDuration);

        return new BuffUptimeStats(
            BuffDefinition: buff,
            TargetName: target,
            TotalCombatTime: totalCombatTime,
            TotalBuffedTime: buffedTime,
            UptimePercent: uptimePercent,
            ApplicationCount: applicationCount,
            RefreshCount: refreshCount,
            AverageGapDuration: avgGapDuration,
            LongestGap: longestGap,
            Gaps: buffGaps
        );
    }

    /// <summary>
    /// Clears all state.
    /// </summary>
    public void Clear()
    {
        _activeBuffs.Clear();
        _eventHistory.Clear();
        _gaps.Clear();
    }

    /// <summary>
    /// Gets the count of currently tracked buffs.
    /// </summary>
    public int ActiveBuffCount => _activeBuffs.Count;

    /// <summary>
    /// Gets the count of recorded events.
    /// </summary>
    public int EventCount => _eventHistory.Count;

    private void CleanupExpiredBuffs(TimeOnly currentTime)
    {
        var expired = _activeBuffs
            .Where(kvp => kvp.Value.ExpiresAt != TimeOnly.MaxValue && currentTime > kvp.Value.ExpiresAt)
            .ToList();

        foreach (var (key, state) in expired)
        {
            _activeBuffs.Remove(key);

            // Record expiry event
            var evt = new BuffEvent(
                Timestamp: state.ExpiresAt,
                BuffDefinition: state.BuffDefinition,
                EventType: BuffEventType.Expired,
                TargetType: state.TargetType,
                TargetName: state.TargetName,
                SourceName: state.SourceName,
                Duration: null,
                Magnitude: state.Magnitude
            );
            _eventHistory.Add(evt);

            // Record gap for expected buffs
            if (state.BuffDefinition.IsExpectedBuff)
            {
                _gaps.Add(new BuffGap(
                    BuffDefinition: state.BuffDefinition,
                    TargetName: state.TargetName,
                    GapStart: state.ExpiresAt,
                    GapEnd: null,
                    GapDuration: TimeSpan.Zero,
                    Context: "Buff expired (timer)"
                ));
            }
        }
    }

    private void RecordGap(BuffDefinition buff, string target, TimeOnly gapStart, TimeOnly gapEnd, string context)
    {
        // Close any open gap for this buff/target
        var openGapIndex = _gaps.FindIndex(g =>
            g.BuffDefinition.BuffId == buff.BuffId &&
            g.TargetName.Equals(target, StringComparison.OrdinalIgnoreCase) &&
            !g.GapEnd.HasValue);

        if (openGapIndex >= 0)
        {
            var openGap = _gaps[openGapIndex];
            _gaps[openGapIndex] = openGap with
            {
                GapEnd = gapEnd,
                GapDuration = gapEnd.ToTimeSpan() - openGap.GapStart.ToTimeSpan()
            };
        }
        else
        {
            // Record new gap
            _gaps.Add(new BuffGap(
                BuffDefinition: buff,
                TargetName: target,
                GapStart: gapStart,
                GapEnd: gapEnd,
                GapDuration: gapEnd.ToTimeSpan() - gapStart.ToTimeSpan(),
                Context: context
            ));
        }
    }
}
