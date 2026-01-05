using CamelotCombatReporter.Core.Models;
using CamelotCombatReporter.Core.RealmAbilities.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace CamelotCombatReporter.Core.RealmAbilities;

/// <summary>
/// Implementation of realm ability tracking and analysis.
/// </summary>
public class RealmAbilityService : IRealmAbilityService
{
    private readonly ILogger<RealmAbilityService> _logger;
    private readonly Dictionary<string, List<RealmAbilityActivation>> _activationsByAbility = new();

    /// <summary>
    /// Creates a new realm ability service.
    /// </summary>
    /// <param name="database">The realm ability database.</param>
    /// <param name="logger">Optional logger.</param>
    public RealmAbilityService(IRealmAbilityDatabase database, ILogger<RealmAbilityService>? logger = null)
    {
        Database = database ?? throw new ArgumentNullException(nameof(database));
        _logger = logger ?? NullLogger<RealmAbilityService>.Instance;
    }

    /// <inheritdoc/>
    public IRealmAbilityDatabase Database { get; }

    /// <inheritdoc/>
    public IReadOnlyList<RealmAbilityActivation> ExtractActivations(IEnumerable<LogEvent> events)
    {
        var activations = new List<RealmAbilityActivation>();
        var eventList = events.ToList();

        // Find all RA events
        var raEvents = eventList.OfType<RealmAbilityEvent>().Where(e => e.IsActivation).ToList();

        foreach (var raEvent in raEvents)
        {
            var ability = MatchAbility(raEvent.AbilityName);
            if (ability == null)
            {
                _logger.LogDebug("Unknown realm ability: {Name}", raEvent.AbilityName);
                continue;
            }

            // Find associated effect events (damage, healing) within a short window
            var associatedEvents = FindAssociatedEvents(eventList, raEvent, ability);

            var cooldownEnds = ability.BaseCooldown.HasValue
                ? raEvent.Timestamp.Add(ability.BaseCooldown.Value)
                : raEvent.Timestamp;

            var activation = new RealmAbilityActivation(
                Id: Guid.NewGuid(),
                Timestamp: raEvent.Timestamp,
                Ability: ability,
                Level: 1, // Default to level 1, could be inferred from effect magnitude
                SourceName: raEvent.SourceName,
                CooldownEnds: cooldownEnds,
                AssociatedEvents: associatedEvents
            );

            activations.Add(activation);
        }

        return activations.OrderBy(a => a.Timestamp).ToList();
    }

    /// <inheritdoc/>
    public RealmAbilitySessionStats CalculateStatistics(IEnumerable<RealmAbilityActivation> activations, TimeSpan sessionDuration)
    {
        var activationList = activations.ToList();

        if (activationList.Count == 0)
        {
            return new RealmAbilitySessionStats(
                TotalActivations: 0,
                TotalRAsUsed: 0,
                MostUsedAbility: null,
                HighestDamageAbility: null,
                OverallCooldownEfficiency: 0,
                UsageByType: new Dictionary<RealmAbilityType, int>(),
                PerAbilityStats: Array.Empty<RealmAbilityUsageStats>(),
                SessionDuration: sessionDuration
            );
        }

        // Group by ability
        var grouped = activationList.GroupBy(a => a.Ability.Id);
        var perAbilityStats = new List<RealmAbilityUsageStats>();

        foreach (var group in grouped)
        {
            var abilityActivations = group.ToList();
            var ability = abilityActivations.First().Ability;

            var totalDamage = abilityActivations
                .SelectMany(a => a.AssociatedEvents)
                .OfType<DamageEvent>()
                .Sum(d => d.DamageAmount);

            var totalHealing = abilityActivations
                .SelectMany(a => a.AssociatedEvents)
                .OfType<HealingEvent>()
                .Sum(h => h.HealingAmount);

            var targets = abilityActivations
                .SelectMany(a => a.AssociatedEvents)
                .Select(e => GetTargetFromEvent(e))
                .Where(t => !string.IsNullOrEmpty(t))
                .Distinct()
                .Count();

            var avgEffectiveness = abilityActivations.Count > 0
                ? (totalDamage + totalHealing) / (double)abilityActivations.Count
                : 0;

            var cooldownEfficiency = CalculateCooldownEfficiency(ability, abilityActivations, sessionDuration);

            perAbilityStats.Add(new RealmAbilityUsageStats(
                Ability: ability,
                TotalActivations: abilityActivations.Count,
                TotalDamage: totalDamage,
                TotalHealing: totalHealing,
                TotalTargetsAffected: targets,
                AverageEffectiveness: avgEffectiveness,
                CooldownEfficiency: cooldownEfficiency,
                Activations: abilityActivations
            ));
        }

        var usageByType = activationList
            .GroupBy(a => a.Ability.Type)
            .ToDictionary(g => g.Key, g => g.Count());

        var mostUsed = perAbilityStats.MaxBy(s => s.TotalActivations);
        var highestDamage = perAbilityStats.MaxBy(s => s.TotalDamage);

        var overallEfficiency = perAbilityStats.Count > 0
            ? perAbilityStats.Average(s => s.CooldownEfficiency)
            : 0;

        return new RealmAbilitySessionStats(
            TotalActivations: activationList.Count,
            TotalRAsUsed: perAbilityStats.Count,
            MostUsedAbility: mostUsed,
            HighestDamageAbility: highestDamage?.TotalDamage > 0 ? highestDamage : null,
            OverallCooldownEfficiency: overallEfficiency,
            UsageByType: usageByType,
            PerAbilityStats: perAbilityStats.OrderByDescending(s => s.TotalActivations).ToList(),
            SessionDuration: sessionDuration
        );
    }

    /// <inheritdoc/>
    public IReadOnlyList<RealmAbilityTimelineEntry> BuildTimeline(IEnumerable<RealmAbilityActivation> activations)
    {
        var entries = new List<RealmAbilityTimelineEntry>();
        var activationList = activations.OrderBy(a => a.Timestamp).ToList();

        // Track last activation per ability for cooldown efficiency calculation
        var lastActivation = new Dictionary<string, RealmAbilityActivation>();

        foreach (var activation in activationList)
        {
            var wasOptimal = true;

            if (lastActivation.TryGetValue(activation.Ability.Id, out var prev) && activation.Ability.BaseCooldown.HasValue)
            {
                // Check if used within a reasonable window after cooldown ended
                var idealUseTime = prev.CooldownEnds;
                var actualDelay = activation.Timestamp.ToTimeSpan() - idealUseTime.ToTimeSpan();

                // Consider "optimal" if used within 10 seconds of cooldown ending
                wasOptimal = actualDelay.TotalSeconds <= 10;
            }

            lastActivation[activation.Ability.Id] = activation;

            var effectValue = activation.AssociatedEvents
                .OfType<DamageEvent>()
                .Sum(d => d.DamageAmount);

            if (effectValue == 0)
            {
                effectValue = activation.AssociatedEvents
                    .OfType<HealingEvent>()
                    .Sum(h => h.HealingAmount);
            }

            entries.Add(new RealmAbilityTimelineEntry(
                Timestamp: activation.Timestamp,
                AbilityName: activation.Ability.Name,
                Type: activation.Ability.Type,
                EffectValue: effectValue > 0 ? effectValue : null,
                WasOnOptimalCooldown: wasOptimal,
                DisplayColor: RealmAbilityTimelineEntry.GetColorForType(activation.Ability.Type)
            ));
        }

        return entries;
    }

    /// <inheritdoc/>
    public IReadOnlyList<CooldownState> GetCooldownStates(IEnumerable<RealmAbilityActivation> activations, TimeOnly currentTime)
    {
        var states = new List<CooldownState>();
        var lastUseByAbility = new Dictionary<string, RealmAbilityActivation>();

        foreach (var activation in activations.OrderBy(a => a.Timestamp))
        {
            lastUseByAbility[activation.Ability.Id] = activation;
        }

        foreach (var (abilityId, lastActivation) in lastUseByAbility)
        {
            var isReady = currentTime >= lastActivation.CooldownEnds;

            states.Add(new CooldownState(
                AbilityId: abilityId,
                AbilityName: lastActivation.Ability.Name,
                LastUsed: lastActivation.Timestamp,
                CooldownEnds: lastActivation.CooldownEnds,
                IsReady: isReady
            ));
        }

        return states.OrderBy(s => s.AbilityName).ToList();
    }

    /// <inheritdoc/>
    public IReadOnlyList<RealmAbility> GetAbilitiesForEra(GameEra era) =>
        Database.GetByEra(era);

    /// <inheritdoc/>
    public IReadOnlyList<RealmAbility> GetAbilitiesForRealm(RealmAvailability realm) =>
        Database.GetByRealm(realm);

    /// <inheritdoc/>
    public RealmAbility? MatchAbility(string abilityName)
    {
        if (string.IsNullOrEmpty(abilityName))
            return null;

        // Try exact internal name match first
        var ability = Database.GetByInternalName(abilityName);
        if (ability != null)
            return ability;

        // Try display name
        ability = Database.GetByName(abilityName);
        if (ability != null)
            return ability;

        // Try case-insensitive partial match
        var normalized = abilityName.ToLowerInvariant();
        return Database.AllAbilities.FirstOrDefault(a =>
            a.Name.ToLowerInvariant().Contains(normalized) ||
            a.InternalName.ToLowerInvariant().Contains(normalized));
    }

    /// <inheritdoc/>
    public void Reset()
    {
        _activationsByAbility.Clear();
    }

    private List<LogEvent> FindAssociatedEvents(List<LogEvent> allEvents, RealmAbilityEvent raEvent, RealmAbility ability)
    {
        var associated = new List<LogEvent>();

        // Look for effect events within a short window (3 seconds) after activation
        var windowStart = raEvent.Timestamp;
        var windowEnd = raEvent.Timestamp.Add(TimeSpan.FromSeconds(3));

        foreach (var evt in allEvents)
        {
            if (evt.Timestamp < windowStart || evt.Timestamp > windowEnd)
                continue;

            // Check for RA effect events with matching ability name
            if (evt is RealmAbilityEvent effectEvent && !effectEvent.IsActivation &&
                string.Equals(effectEvent.AbilityName, raEvent.AbilityName, StringComparison.OrdinalIgnoreCase))
            {
                associated.Add(evt);
                continue;
            }

            // For damage abilities, look for damage events from "You"
            if (ability.Type == RealmAbilityType.Damage && evt is DamageEvent dmg && dmg.Source == "You")
            {
                // Only associate if this is within the very short window
                if (evt.Timestamp <= raEvent.Timestamp.Add(TimeSpan.FromSeconds(1)))
                {
                    associated.Add(evt);
                }
            }

            // For healing abilities, look for healing events
            if (ability.Type == RealmAbilityType.Healing && evt is HealingEvent heal)
            {
                if (evt.Timestamp <= raEvent.Timestamp.Add(TimeSpan.FromSeconds(1)))
                {
                    associated.Add(evt);
                }
            }
        }

        return associated;
    }

    private static double CalculateCooldownEfficiency(RealmAbility ability, List<RealmAbilityActivation> activations, TimeSpan sessionDuration)
    {
        if (!ability.BaseCooldown.HasValue || ability.BaseCooldown.Value == TimeSpan.Zero)
            return 100; // Passives or no-cooldown abilities are always "efficient"

        if (activations.Count <= 1)
            return activations.Count > 0 ? 100 : 0; // Single use is fine, no use is 0%

        // Calculate the maximum possible uses given session duration and cooldown
        var maxPossibleUses = (int)(sessionDuration.TotalSeconds / ability.BaseCooldown.Value.TotalSeconds) + 1;

        if (maxPossibleUses <= 0)
            return 100;

        // Efficiency is actual uses / max possible uses
        var efficiency = (activations.Count / (double)maxPossibleUses) * 100;
        return Math.Min(100, efficiency); // Cap at 100%
    }

    private static string? GetTargetFromEvent(LogEvent evt)
    {
        return evt switch
        {
            DamageEvent d => d.Target,
            HealingEvent h => h.Target,
            RealmAbilityEvent r => r.TargetName,
            _ => null
        };
    }
}
