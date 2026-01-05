using CamelotCombatReporter.Core.CrowdControlAnalysis.Models;

namespace CamelotCombatReporter.Core.CrowdControlAnalysis;

/// <summary>
/// Tracks Diminishing Returns (DR) state for crowd control effects.
/// DR follows the pattern: Full (100%) → Reduced (50%) → Minimal (25%) → Immune (0%)
/// DR resets to Full after 60 seconds without a CC of that type on the target.
/// </summary>
public class DRTracker
{
    /// <summary>
    /// Time in seconds before DR resets to Full.
    /// </summary>
    public static readonly TimeSpan DecayTime = TimeSpan.FromSeconds(60);

    private readonly Dictionary<(string Target, CCType Type), DRStateInternal> _states = new();

    private record DRStateInternal(DRLevel Level, TimeOnly LastCCTime);

    /// <summary>
    /// Gets the current DR level for a target and CC type.
    /// </summary>
    /// <param name="target">The target name.</param>
    /// <param name="ccType">The type of crowd control.</param>
    /// <param name="currentTime">The current time for decay calculation.</param>
    /// <returns>The current DR level.</returns>
    public DRLevel GetCurrentDR(string target, CCType ccType, TimeOnly currentTime)
    {
        var key = (target, ccType);
        if (!_states.TryGetValue(key, out var state))
        {
            return DRLevel.Full;
        }

        // Check 60-second decay
        var elapsed = currentTime - state.LastCCTime;
        if (elapsed >= DecayTime)
        {
            _states.Remove(key);
            return DRLevel.Full;
        }

        return state.Level;
    }

    /// <summary>
    /// Gets the current DR level for a target and CC type (string version for parser integration).
    /// </summary>
    /// <param name="target">The target name.</param>
    /// <param name="ccTypeString">The CC type as a string (e.g., "stun", "mez").</param>
    /// <param name="currentTime">The current time for decay calculation.</param>
    /// <returns>The current DR level.</returns>
    public DRLevel GetCurrentDR(string target, string ccTypeString, TimeOnly currentTime)
    {
        if (!TryParseCCType(ccTypeString, out var ccType))
        {
            return DRLevel.Full;
        }
        return GetCurrentDR(target, ccType, currentTime);
    }

    /// <summary>
    /// Applies a CC to a target, advancing the DR state.
    /// </summary>
    /// <param name="target">The target name.</param>
    /// <param name="ccType">The type of crowd control.</param>
    /// <param name="timestamp">The timestamp of the CC application.</param>
    /// <returns>The DR level at which this CC was applied (before advancement).</returns>
    public DRLevel ApplyCC(string target, CCType ccType, TimeOnly timestamp)
    {
        var currentDR = GetCurrentDR(target, ccType, timestamp);

        // Advance to next DR level
        var nextLevel = currentDR switch
        {
            DRLevel.Full => DRLevel.Reduced,
            DRLevel.Reduced => DRLevel.Minimal,
            DRLevel.Minimal => DRLevel.Immune,
            _ => DRLevel.Immune
        };

        _states[(target, ccType)] = new DRStateInternal(nextLevel, timestamp);

        return currentDR;
    }

    /// <summary>
    /// Applies a CC to a target (string version for parser integration).
    /// </summary>
    /// <param name="target">The target name.</param>
    /// <param name="ccTypeString">The CC type as a string.</param>
    /// <param name="timestamp">The timestamp of the CC application.</param>
    /// <returns>The DR level at which this CC was applied, or null if CC type is unknown.</returns>
    public DRLevel? ApplyCC(string target, string ccTypeString, TimeOnly timestamp)
    {
        if (!TryParseCCType(ccTypeString, out var ccType))
        {
            return null;
        }
        return ApplyCC(target, ccType, timestamp);
    }

    /// <summary>
    /// Calculates the effective duration of a CC effect after DR reduction.
    /// </summary>
    /// <param name="baseDuration">The base duration before DR.</param>
    /// <param name="drLevel">The DR level at application.</param>
    /// <returns>The effective duration after DR reduction.</returns>
    public static TimeSpan CalculateEffectiveDuration(TimeSpan baseDuration, DRLevel drLevel)
    {
        var multiplier = (int)drLevel / 100.0;
        return TimeSpan.FromTicks((long)(baseDuration.Ticks * multiplier));
    }

    /// <summary>
    /// Calculates the effective duration of a CC effect after DR reduction.
    /// </summary>
    /// <param name="baseDurationSeconds">The base duration in seconds before DR.</param>
    /// <param name="drLevel">The DR level at application.</param>
    /// <returns>The effective duration in seconds after DR reduction.</returns>
    public static double CalculateEffectiveDurationSeconds(int baseDurationSeconds, DRLevel drLevel)
    {
        return baseDurationSeconds * ((int)drLevel / 100.0);
    }

    /// <summary>
    /// Resets DR for a specific target and CC type.
    /// </summary>
    /// <param name="target">The target name.</param>
    /// <param name="ccType">The type of crowd control.</param>
    public void ResetDR(string target, CCType ccType)
    {
        _states.Remove((target, ccType));
    }

    /// <summary>
    /// Resets all DR for a specific target.
    /// </summary>
    /// <param name="target">The target name.</param>
    public void ResetDRForTarget(string target)
    {
        var keysToRemove = _states.Keys.Where(k => k.Target == target).ToList();
        foreach (var key in keysToRemove)
        {
            _states.Remove(key);
        }
    }

    /// <summary>
    /// Gets the full DR state for a target and CC type.
    /// </summary>
    /// <param name="target">The target name.</param>
    /// <param name="ccType">The type of crowd control.</param>
    /// <param name="currentTime">The current time.</param>
    /// <returns>The DR state, or null if not tracked.</returns>
    public DRState? GetDRState(string target, CCType ccType, TimeOnly currentTime)
    {
        var key = (target, ccType);
        if (!_states.TryGetValue(key, out var state))
        {
            return null;
        }

        var elapsed = currentTime - state.LastCCTime;
        if (elapsed >= DecayTime)
        {
            _states.Remove(key);
            return null;
        }

        var timeUntilReset = DecayTime - elapsed;

        return new DRState(
            target,
            ccType,
            state.Level,
            state.LastCCTime,
            timeUntilReset
        );
    }

    /// <summary>
    /// Gets all current DR states.
    /// </summary>
    /// <param name="currentTime">The current time for decay calculation.</param>
    /// <returns>All active DR states.</returns>
    public IReadOnlyList<DRState> GetAllDRStates(TimeOnly currentTime)
    {
        var result = new List<DRState>();

        // Clean up expired states and collect active ones
        var keysToRemove = new List<(string, CCType)>();

        foreach (var (key, state) in _states)
        {
            var elapsed = currentTime - state.LastCCTime;
            if (elapsed >= DecayTime)
            {
                keysToRemove.Add(key);
            }
            else
            {
                result.Add(new DRState(
                    key.Target,
                    key.Type,
                    state.Level,
                    state.LastCCTime,
                    DecayTime - elapsed
                ));
            }
        }

        foreach (var key in keysToRemove)
        {
            _states.Remove(key);
        }

        return result;
    }

    /// <summary>
    /// Clears all DR state.
    /// </summary>
    public void Clear()
    {
        _states.Clear();
    }

    /// <summary>
    /// Gets the number of tracked DR states.
    /// </summary>
    public int Count => _states.Count;

    /// <summary>
    /// Tries to parse a CC type string to CCType enum.
    /// </summary>
    private static bool TryParseCCType(string ccTypeString, out CCType ccType)
    {
        ccType = ccTypeString.ToLowerInvariant() switch
        {
            "stun" => CCType.Stun,
            "mez" or "mesmerize" => CCType.Mez,
            "root" => CCType.Root,
            "snare" => CCType.Snare,
            "silence" => CCType.Silence,
            "disarm" => CCType.Disarm,
            _ => default
        };

        return ccTypeString.ToLowerInvariant() is "stun" or "mez" or "mesmerize"
            or "root" or "snare" or "silence" or "disarm";
    }
}
