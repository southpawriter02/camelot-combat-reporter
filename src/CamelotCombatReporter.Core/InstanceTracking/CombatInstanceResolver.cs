using CamelotCombatReporter.Core.Models;

namespace CamelotCombatReporter.Core.InstanceTracking;

/// <summary>
/// Resolves combat events into distinct target instances by tracking death events
/// and time gaps between encounters. Handles multiple same-named mobs by creating
/// separate instances when deaths occur or combat times out.
/// </summary>
public class CombatInstanceResolver : ICombatInstanceResolver
{
    /// <inheritdoc />
    public TimeSpan EncounterTimeoutThreshold { get; set; } = TimeSpan.FromSeconds(15);

    /// <inheritdoc />
    public TimeSpan CombatIdleTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <inheritdoc />
    public IReadOnlyList<TargetTypeStatistics> ResolveInstances(
        IReadOnlyList<LogEvent> events,
        string? playerName = null)
    {
        var encounters = ResolveEncountersInternal(events, playerName);

        // Group encounters by target name
        var grouped = encounters
            .GroupBy(e => e.Instance.TargetName, StringComparer.OrdinalIgnoreCase)
            .Select(g => new TargetTypeStatistics(g.Key, g.ToList()))
            .OrderByDescending(t => t.TotalDamageDealt)
            .ToList();

        return grouped;
    }

    /// <inheritdoc />
    public IReadOnlyList<CombatEncounter> GetAllEncounters(
        IReadOnlyList<LogEvent> events,
        string? playerName = null)
    {
        return ResolveEncountersInternal(events, playerName)
            .OrderBy(e => e.StartTime)
            .ToList();
    }

    private List<CombatEncounter> ResolveEncountersInternal(
        IReadOnlyList<LogEvent> events,
        string? playerName)
    {
        // Track active instances by target name (lowercase for case-insensitive matching)
        var activeInstances = new Dictionary<string, ActiveInstanceState>(StringComparer.OrdinalIgnoreCase);

        // Track instance numbers per target name
        var instanceNumbers = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        // Completed encounters
        var completedEncounters = new List<CombatEncounter>();

        foreach (var evt in events.OrderBy(e => e.Timestamp))
        {
            // First, check for timeout on all active instances
            CheckForTimeouts(activeInstances, completedEncounters, evt.Timestamp);

            switch (evt)
            {
                case DamageEvent dmg:
                    ProcessDamageEvent(dmg, activeInstances, instanceNumbers, playerName);
                    break;

                case PetDamageEvent petDmg:
                    ProcessPetDamageEvent(petDmg, activeInstances, instanceNumbers);
                    break;

                case DeathEvent death:
                    ProcessDeathEvent(death, activeInstances, completedEncounters);
                    break;

                case HealingEvent heal:
                    ProcessHealingEvent(heal, activeInstances, playerName);
                    break;

                case CriticalHitEvent crit:
                    ProcessCriticalHitEvent(crit, activeInstances);
                    break;
            }
        }

        // Close all remaining active instances as session end
        CloseAllActiveInstances(activeInstances, completedEncounters, EncounterEndReason.SessionEnd);

        return completedEncounters;
    }

    private void ProcessDamageEvent(
        DamageEvent dmg,
        Dictionary<string, ActiveInstanceState> activeInstances,
        Dictionary<string, int> instanceNumbers,
        string? playerName)
    {
        // Determine the target name for tracking (the mob being damaged)
        string targetName;
        bool isOutgoingDamage;

        if (playerName != null && dmg.Source.Equals(playerName, StringComparison.OrdinalIgnoreCase))
        {
            // Player dealing damage to mob
            targetName = dmg.Target;
            isOutgoingDamage = true;
        }
        else if (playerName != null && dmg.Target.Equals(playerName, StringComparison.OrdinalIgnoreCase))
        {
            // Mob dealing damage to player
            targetName = dmg.Source;
            isOutgoingDamage = false;
        }
        else
        {
            // No player name specified or unknown scenario - track both as potential targets
            // Default to tracking outgoing damage (source as attacker, target as mob)
            targetName = dmg.Target;
            isOutgoingDamage = true;
        }

        // Get or create instance for this target
        var instance = GetOrCreateInstance(targetName, dmg.Timestamp, activeInstances, instanceNumbers);

        instance.Events.Add(dmg);
        instance.LastEventTime = dmg.Timestamp;

        if (isOutgoingDamage)
        {
            instance.DamageDealt += dmg.DamageAmount;
        }
        else
        {
            instance.DamageTaken += dmg.DamageAmount;
        }
    }

    private void ProcessPetDamageEvent(
        PetDamageEvent petDmg,
        Dictionary<string, ActiveInstanceState> activeInstances,
        Dictionary<string, int> instanceNumbers)
    {
        // Pet damage is always outgoing to the target
        var targetName = petDmg.Target;
        var instance = GetOrCreateInstance(targetName, petDmg.Timestamp, activeInstances, instanceNumbers);

        instance.Events.Add(petDmg);
        instance.LastEventTime = petDmg.Timestamp;
        instance.DamageDealt += petDmg.DamageAmount;
    }

    private void ProcessDeathEvent(
        DeathEvent death,
        Dictionary<string, ActiveInstanceState> activeInstances,
        List<CombatEncounter> completedEncounters)
    {
        var targetName = death.Target;

        if (activeInstances.TryGetValue(targetName, out var instance))
        {
            // Close this instance with death
            instance.Events.Add(death);
            var encounter = instance.ToEncounter(EncounterEndReason.Death, death.Timestamp);
            completedEncounters.Add(encounter);
            activeInstances.Remove(targetName);
        }
        else
        {
            // Death for a target we weren't tracking - create a minimal encounter record
            var instanceNum = GetNextInstanceNumber(targetName, new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase));
            var targetInstance = CombatTargetInstance.Create(targetName, instanceNum);
            var encounter = new CombatEncounter(
                targetInstance,
                death.Timestamp,
                death.Timestamp,
                EncounterEndReason.Death,
                new List<LogEvent> { death },
                0, 0, 0);
            completedEncounters.Add(encounter);
        }
    }

    private void ProcessHealingEvent(
        HealingEvent heal,
        Dictionary<string, ActiveInstanceState> activeInstances,
        string? playerName)
    {
        // Associate healing with any active combat encounter
        // Healing during combat is tracked with the most recent active instance
        if (activeInstances.Count == 0) return;

        // Find the most recently active instance to associate healing with
        var mostRecent = activeInstances.Values
            .OrderByDescending(i => i.LastEventTime)
            .FirstOrDefault();

        if (mostRecent != null)
        {
            mostRecent.Events.Add(heal);
            mostRecent.HealingDone += heal.HealingAmount;
            mostRecent.LastEventTime = heal.Timestamp;
        }
    }

    private void ProcessCriticalHitEvent(
        CriticalHitEvent crit,
        Dictionary<string, ActiveInstanceState> activeInstances)
    {
        // Critical hits follow damage events - associate with the target if active
        if (crit.Target != null && activeInstances.TryGetValue(crit.Target, out var instance))
        {
            instance.Events.Add(crit);
            instance.DamageDealt += crit.DamageAmount;
            instance.LastEventTime = crit.Timestamp;
        }
        else if (activeInstances.Count > 0)
        {
            // Associate with most recent instance if target not specified
            var mostRecent = activeInstances.Values
                .OrderByDescending(i => i.LastEventTime)
                .First();
            mostRecent.Events.Add(crit);
            mostRecent.DamageDealt += crit.DamageAmount;
            mostRecent.LastEventTime = crit.Timestamp;
        }
    }

    private void CheckForTimeouts(
        Dictionary<string, ActiveInstanceState> activeInstances,
        List<CombatEncounter> completedEncounters,
        TimeOnly currentTime)
    {
        var timedOut = activeInstances
            .Where(kvp => (currentTime - kvp.Value.LastEventTime) > EncounterTimeoutThreshold)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var targetName in timedOut)
        {
            var instance = activeInstances[targetName];
            var encounter = instance.ToEncounter(EncounterEndReason.Timeout);
            completedEncounters.Add(encounter);
            activeInstances.Remove(targetName);
        }
    }

    private void CloseAllActiveInstances(
        Dictionary<string, ActiveInstanceState> activeInstances,
        List<CombatEncounter> completedEncounters,
        EncounterEndReason reason)
    {
        foreach (var instance in activeInstances.Values)
        {
            var encounter = instance.ToEncounter(reason);
            completedEncounters.Add(encounter);
        }
        activeInstances.Clear();
    }

    private ActiveInstanceState GetOrCreateInstance(
        string targetName,
        TimeOnly timestamp,
        Dictionary<string, ActiveInstanceState> activeInstances,
        Dictionary<string, int> instanceNumbers)
    {
        if (activeInstances.TryGetValue(targetName, out var existing))
        {
            return existing;
        }

        // Create new instance
        var instanceNum = GetNextInstanceNumber(targetName, instanceNumbers);
        var targetInstance = CombatTargetInstance.Create(targetName, instanceNum);
        var state = new ActiveInstanceState(targetInstance, timestamp);
        activeInstances[targetName] = state;
        return state;
    }

    private int GetNextInstanceNumber(string targetName, Dictionary<string, int> instanceNumbers)
    {
        if (!instanceNumbers.TryGetValue(targetName, out var current))
        {
            current = 0;
        }
        instanceNumbers[targetName] = current + 1;
        return current + 1;
    }
}
