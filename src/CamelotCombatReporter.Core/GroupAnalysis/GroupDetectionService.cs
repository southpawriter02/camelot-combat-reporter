using CamelotCombatReporter.Core.BuffTracking.Models;
using CamelotCombatReporter.Core.GroupAnalysis.Models;
using CamelotCombatReporter.Core.Models;
using Microsoft.Extensions.Logging;

namespace CamelotCombatReporter.Core.GroupAnalysis;

/// <summary>
/// Service for detecting group members from combat log events.
/// Uses multiple detection strategies including healing, buffs, and shared targets.
/// </summary>
public class GroupDetectionService : IGroupDetectionService
{
    private readonly ILogger<GroupDetectionService>? _logger;
    private readonly RoleClassificationService _roleClassifier;
    private readonly List<ManualMember> _manualMembers = new();

    private record ManualMember(string Name, CharacterClass? Class, Realm? Realm);

    /// <inheritdoc />
    public TimeSpan ProximityWindow { get; set; } = TimeSpan.FromSeconds(10);

    /// <inheritdoc />
    public int MinInteractions { get; set; } = 3;

    public GroupDetectionService(ILogger<GroupDetectionService>? logger = null)
    {
        _logger = logger;
        _roleClassifier = new RoleClassificationService();
    }

    /// <inheritdoc />
    public IReadOnlyList<GroupMember> DetectGroupMembers(IEnumerable<LogEvent> events)
    {
        var eventList = events.ToList();
        _logger?.LogDebug("Detecting group members from {Count} events", eventList.Count);

        var interactionTracker = new Dictionary<string, MemberInteractions>(StringComparer.OrdinalIgnoreCase);

        // Track the player
        interactionTracker["You"] = new MemberInteractions
        {
            Name = "You",
            IsPlayer = true,
            FirstSeen = eventList.FirstOrDefault()?.Timestamp ?? TimeOnly.MinValue,
            LastSeen = eventList.LastOrDefault()?.Timestamp
        };

        // Strategy 1: Detect healers from healing received
        DetectFromHealing(eventList, interactionTracker);

        // Strategy 2: Detect from buffs applied
        DetectFromBuffs(eventList, interactionTracker);

        // Strategy 3: Detect from shared combat targets
        DetectFromSharedTargets(eventList, interactionTracker);

        // Strategy 4: Detect from damage patterns (allies attacking same targets)
        DetectFromDamagePatterns(eventList, interactionTracker);

        // Merge with manual members
        MergeManualMembers(interactionTracker);

        // Filter to members with sufficient interactions
        var groupMembers = interactionTracker.Values
            .Where(m => m.IsPlayer || m.InteractionCount >= MinInteractions || m.IsManual)
            .Select(CreateGroupMember)
            .ToList();

        _logger?.LogInformation("Detected {Count} group members", groupMembers.Count);

        return groupMembers;
    }

    private void DetectFromHealing(List<LogEvent> events, Dictionary<string, MemberInteractions> tracker)
    {
        foreach (var evt in events.OfType<HealingEvent>())
        {
            // Someone healed the player
            if (evt.Target == "You" && !string.IsNullOrEmpty(evt.Source) && evt.Source != "You")
            {
                AddInteraction(tracker, evt.Source, InteractionType.HealedPlayer, evt.Timestamp);
            }
            // Player healed someone
            else if (evt.Source == "You" && !string.IsNullOrEmpty(evt.Target) && evt.Target != "You")
            {
                AddInteraction(tracker, evt.Target, InteractionType.HealedByPlayer, evt.Timestamp);
            }
        }
    }

    private void DetectFromBuffs(List<LogEvent> events, Dictionary<string, MemberInteractions> tracker)
    {
        foreach (var evt in events.OfType<BuffEvent>())
        {
            // Someone buffed the player
            if (evt.TargetName == "You" && !string.IsNullOrEmpty(evt.SourceName) && evt.SourceName != "You")
            {
                AddInteraction(tracker, evt.SourceName, InteractionType.BuffedPlayer, evt.Timestamp);
            }
        }
    }

    private void DetectFromSharedTargets(List<LogEvent> events, Dictionary<string, MemberInteractions> tracker)
    {
        // Find targets the player attacked
        var playerTargets = events
            .OfType<DamageEvent>()
            .Where(e => e.Source == "You")
            .Select(e => (e.Target, e.Timestamp))
            .ToList();

        if (playerTargets.Count == 0)
            return;

        // Find others who attacked the same targets in proximity
        foreach (var damage in events.OfType<DamageEvent>())
        {
            if (damage.Source == "You" || string.IsNullOrEmpty(damage.Source))
                continue;

            // Check if this entity attacked a target the player also attacked within the proximity window
            var matchingTargets = playerTargets
                .Where(pt => pt.Target == damage.Target &&
                            Math.Abs((damage.Timestamp - pt.Timestamp).TotalSeconds) <= ProximityWindow.TotalSeconds)
                .ToList();

            if (matchingTargets.Count > 0)
            {
                AddInteraction(tracker, damage.Source, InteractionType.SharedTarget, damage.Timestamp);
            }
        }
    }

    private void DetectFromDamagePatterns(List<LogEvent> events, Dictionary<string, MemberInteractions> tracker)
    {
        // Group damage events by target and time window
        var damageWindows = events
            .OfType<DamageEvent>()
            .GroupBy(e => new
            {
                e.Target,
                Window = (int)(e.Timestamp.ToTimeSpan().TotalSeconds / ProximityWindow.TotalSeconds)
            })
            .Where(g => g.Any(e => e.Source == "You"))
            .ToList();

        foreach (var window in damageWindows)
        {
            var allies = window
                .Where(e => e.Source != "You" && !string.IsNullOrEmpty(e.Source))
                .Select(e => e.Source)
                .Distinct();

            foreach (var ally in allies)
            {
                var timestamp = window.First().Timestamp;
                AddInteraction(tracker, ally!, InteractionType.SharedTarget, timestamp);
            }
        }
    }

    private void MergeManualMembers(Dictionary<string, MemberInteractions> tracker)
    {
        foreach (var manual in _manualMembers)
        {
            if (tracker.TryGetValue(manual.Name, out var existing))
            {
                existing.IsManual = true;
                existing.ManualClass = manual.Class;
                existing.ManualRealm = manual.Realm;
            }
            else
            {
                tracker[manual.Name] = new MemberInteractions
                {
                    Name = manual.Name,
                    IsManual = true,
                    ManualClass = manual.Class,
                    ManualRealm = manual.Realm,
                    FirstSeen = TimeOnly.MinValue
                };
            }
        }
    }

    private void AddInteraction(
        Dictionary<string, MemberInteractions> tracker,
        string name,
        InteractionType type,
        TimeOnly timestamp)
    {
        if (!tracker.TryGetValue(name, out var interactions))
        {
            interactions = new MemberInteractions
            {
                Name = name,
                FirstSeen = timestamp
            };
            tracker[name] = interactions;
        }

        interactions.InteractionCount++;
        interactions.LastSeen = timestamp;
        interactions.InteractionTypes.Add(type);

        if (timestamp < interactions.FirstSeen)
            interactions.FirstSeen = timestamp;
    }

    private GroupMember CreateGroupMember(MemberInteractions interactions)
    {
        var characterClass = interactions.ManualClass ?? DetectClassFromInteractions(interactions);
        var realm = interactions.ManualRealm ?? characterClass?.GetRealm();

        var (primaryRole, secondaryRole) = characterClass.HasValue
            ? _roleClassifier.GetRolesForClass(characterClass.Value)
            : (InferRoleFromInteractions(interactions), null);

        var source = interactions.IsManual
            ? (interactions.InteractionCount >= MinInteractions ? GroupMemberSource.Both : GroupMemberSource.Manual)
            : GroupMemberSource.Inferred;

        return new GroupMember(
            Name: interactions.Name,
            Class: characterClass,
            Realm: realm,
            PrimaryRole: primaryRole,
            SecondaryRole: secondaryRole,
            Source: source,
            FirstSeen: interactions.FirstSeen,
            LastSeen: interactions.LastSeen,
            IsPlayer: interactions.IsPlayer
        );
    }

    private CharacterClass? DetectClassFromInteractions(MemberInteractions interactions)
    {
        // Basic heuristic: if they primarily healed, they're likely a healer class
        // This is a simplified approach - could be expanded with spell/ability name detection
        var healCount = interactions.InteractionTypes.Count(t => t == InteractionType.HealedPlayer);
        var buffCount = interactions.InteractionTypes.Count(t => t == InteractionType.BuffedPlayer);

        // Can't reliably determine class without more information
        // Return null and let the user manually configure if needed
        return null;
    }

    private GroupRole InferRoleFromInteractions(MemberInteractions interactions)
    {
        var healCount = interactions.InteractionTypes.Count(t => t == InteractionType.HealedPlayer);
        var buffCount = interactions.InteractionTypes.Count(t => t == InteractionType.BuffedPlayer);
        var damageCount = interactions.InteractionTypes.Count(t =>
            t == InteractionType.SharedTarget || t == InteractionType.DamagedSameTarget);

        var total = interactions.InteractionCount;
        if (total == 0)
            return GroupRole.Unknown;

        // Primarily healing = Healer role
        if (healCount > 0 && healCount >= buffCount && healCount >= damageCount)
            return GroupRole.Healer;

        // Primarily buffing = Support role
        if (buffCount > 0 && buffCount > healCount && buffCount >= damageCount)
            return GroupRole.Support;

        // Primarily damage = DPS (can't distinguish melee vs caster)
        if (damageCount > 0)
            return GroupRole.MeleeDps; // Default to melee, could be refined

        return GroupRole.Unknown;
    }

    /// <inheritdoc />
    public GroupComposition BuildComposition(IReadOnlyList<GroupMember> members, TimeOnly timestamp)
    {
        var sizeCategory = GroupSizeCategoryExtensions.FromMemberCount(members.Count);
        var balanceScore = CalculateBalanceScore(members);

        return new GroupComposition(
            Id: Guid.NewGuid(),
            Members: members,
            SizeCategory: sizeCategory,
            MatchedTemplate: null, // Will be set by GroupAnalysisService
            BalanceScore: balanceScore,
            FormationTime: members.Min(m => m.FirstSeen),
            DisbandTime: members.Max(m => m.LastSeen)
        );
    }

    private double CalculateBalanceScore(IReadOnlyList<GroupMember> members)
    {
        if (members.Count <= 1)
            return 50; // Solo - neutral score

        var roleCounts = members
            .GroupBy(m => m.PrimaryRole)
            .ToDictionary(g => g.Key, g => g.Count());

        double score = 100;

        // Penalize missing healer
        if (!roleCounts.ContainsKey(GroupRole.Healer))
            score -= 30;

        // Penalize missing tank (less critical)
        if (!roleCounts.ContainsKey(GroupRole.Tank))
            score -= 15;

        // Penalize no DPS
        var hasDps = roleCounts.ContainsKey(GroupRole.MeleeDps) ||
                     roleCounts.ContainsKey(GroupRole.CasterDps) ||
                     roleCounts.ContainsKey(GroupRole.Hybrid);
        if (!hasDps)
            score -= 20;

        // Bonus for CC
        if (roleCounts.ContainsKey(GroupRole.CrowdControl))
            score += 5;

        // Bonus for support
        if (roleCounts.ContainsKey(GroupRole.Support))
            score += 5;

        // Penalize heavy imbalance (one role > 50% of group)
        var maxRoleCount = roleCounts.Values.DefaultIfEmpty(0).Max();
        if (maxRoleCount > members.Count / 2.0)
            score -= 10;

        return Math.Clamp(score, 0, 100);
    }

    /// <inheritdoc />
    public void AddManualMember(string name, CharacterClass? characterClass = null, Realm? realm = null)
    {
        // Remove existing entry if present
        _manualMembers.RemoveAll(m => m.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        _manualMembers.Add(new ManualMember(name, characterClass, realm));
        _logger?.LogDebug("Added manual member: {Name}", name);
    }

    /// <inheritdoc />
    public bool RemoveManualMember(string name)
    {
        var removed = _manualMembers.RemoveAll(m => m.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        if (removed > 0)
        {
            _logger?.LogDebug("Removed manual member: {Name}", name);
        }
        return removed > 0;
    }

    /// <inheritdoc />
    public IReadOnlyList<(string Name, CharacterClass? Class, Realm? Realm)> GetManualMembers()
    {
        return _manualMembers.Select(m => (m.Name, m.Class, m.Realm)).ToList();
    }

    /// <inheritdoc />
    public void ClearManualMembers()
    {
        _manualMembers.Clear();
        _logger?.LogDebug("Cleared all manual members");
    }

    /// <inheritdoc />
    public void Reset()
    {
        _manualMembers.Clear();
        _logger?.LogDebug("Group detection service reset");
    }

    private enum InteractionType
    {
        HealedPlayer,
        HealedByPlayer,
        BuffedPlayer,
        SharedTarget,
        DamagedSameTarget
    }

    private class MemberInteractions
    {
        public string Name { get; set; } = string.Empty;
        public int InteractionCount { get; set; }
        public TimeOnly FirstSeen { get; set; }
        public TimeOnly? LastSeen { get; set; }
        public bool IsPlayer { get; set; }
        public bool IsManual { get; set; }
        public CharacterClass? ManualClass { get; set; }
        public Realm? ManualRealm { get; set; }
        public List<InteractionType> InteractionTypes { get; } = new();
    }
}
