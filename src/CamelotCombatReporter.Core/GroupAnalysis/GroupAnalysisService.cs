using CamelotCombatReporter.Core.GroupAnalysis.Models;
using CamelotCombatReporter.Core.Models;
using Microsoft.Extensions.Logging;

namespace CamelotCombatReporter.Core.GroupAnalysis;

/// <summary>
/// Service for analyzing group composition, calculating metrics, and generating recommendations.
/// </summary>
public class GroupAnalysisService : IGroupAnalysisService
{
    private readonly ILogger<GroupAnalysisService>? _logger;
    private readonly IGroupDetectionService _detectionService;
    private readonly RoleClassificationService _roleClassifier;
    private readonly List<GroupTemplate> _templates;

    public GroupAnalysisService(ILogger<GroupAnalysisService>? logger = null)
    {
        _logger = logger;
        _detectionService = new GroupDetectionService();
        _roleClassifier = new RoleClassificationService();
        _templates = InitializeTemplates();
    }

    public GroupAnalysisService(
        IGroupDetectionService detectionService,
        ILogger<GroupAnalysisService>? logger = null)
    {
        _logger = logger;
        _detectionService = detectionService;
        _roleClassifier = new RoleClassificationService();
        _templates = InitializeTemplates();
    }

    private static List<GroupTemplate> InitializeTemplates() => new()
    {
        new GroupTemplate(
            Name: "8-Man RvR",
            Description: "Balanced 8-man group for organized realm vs realm combat",
            RoleRequirements: new Dictionary<GroupRole, RoleRequirement>
            {
                [GroupRole.Tank] = new(2, 3, IsRequired: true),
                [GroupRole.Healer] = new(2, 2, IsRequired: true),
                [GroupRole.CrowdControl] = new(1, 2, IsRequired: true),
                [GroupRole.MeleeDps] = new(1, 2, IsRequired: false),
                [GroupRole.CasterDps] = new(1, 2, IsRequired: false),
                [GroupRole.Support] = new(0, 1, IsRequired: false)
            },
            MinSize: 6,
            MaxSize: 8
        ),
        new GroupTemplate(
            Name: "Small-Man",
            Description: "Agile roaming group for hit-and-run tactics",
            RoleRequirements: new Dictionary<GroupRole, RoleRequirement>
            {
                [GroupRole.Tank] = new(0, 1, IsRequired: false),
                [GroupRole.Healer] = new(1, 1, IsRequired: true),
                [GroupRole.MeleeDps] = new(2, 3, IsRequired: true),
                [GroupRole.Support] = new(0, 1, IsRequired: false)
            },
            MinSize: 2,
            MaxSize: 4
        ),
        new GroupTemplate(
            Name: "Zerg Support",
            Description: "Healer-heavy composition for battlegroup warfare",
            RoleRequirements: new Dictionary<GroupRole, RoleRequirement>
            {
                [GroupRole.Healer] = new(3, 4, IsRequired: true),
                [GroupRole.Support] = new(2, 3, IsRequired: true),
                [GroupRole.Tank] = new(1, 2, IsRequired: false),
                [GroupRole.CrowdControl] = new(1, 2, IsRequired: false)
            },
            MinSize: 6,
            MaxSize: 8
        ),
        new GroupTemplate(
            Name: "Gank Group",
            Description: "High burst damage composition for quick assassinations",
            RoleRequirements: new Dictionary<GroupRole, RoleRequirement>
            {
                [GroupRole.MeleeDps] = new(3, 5, IsRequired: true),
                [GroupRole.Healer] = new(0, 1, IsRequired: false),
                [GroupRole.CrowdControl] = new(1, 2, IsRequired: false)
            },
            MinSize: 3,
            MaxSize: 5
        ),
        new GroupTemplate(
            Name: "Keep Defense",
            Description: "CC-heavy composition for defensive siege warfare",
            RoleRequirements: new Dictionary<GroupRole, RoleRequirement>
            {
                [GroupRole.CrowdControl] = new(2, 3, IsRequired: true),
                [GroupRole.Healer] = new(2, 3, IsRequired: true),
                [GroupRole.Tank] = new(2, 3, IsRequired: true),
                [GroupRole.CasterDps] = new(1, 2, IsRequired: false)
            },
            MinSize: 6,
            MaxSize: 8
        ),
        new GroupTemplate(
            Name: "Duo",
            Description: "Two-person team with healer support",
            RoleRequirements: new Dictionary<GroupRole, RoleRequirement>
            {
                [GroupRole.Healer] = new(1, 1, IsRequired: true),
                [GroupRole.MeleeDps] = new(1, 1, IsRequired: false),
                [GroupRole.Hybrid] = new(0, 1, IsRequired: false)
            },
            MinSize: 2,
            MaxSize: 2
        )
    };

    /// <inheritdoc />
    public GroupComposition AnalyzeComposition(IEnumerable<LogEvent> events)
    {
        var eventList = events.ToList();
        _logger?.LogDebug("Analyzing group composition from {Count} events", eventList.Count);

        var members = _detectionService.DetectGroupMembers(eventList);
        var timestamp = eventList.FirstOrDefault()?.Timestamp ?? TimeOnly.MinValue;
        var composition = _detectionService.BuildComposition(members, timestamp);

        // Match template
        var matchedTemplate = MatchTemplate(composition);
        if (matchedTemplate != null)
        {
            composition = composition with { MatchedTemplate = matchedTemplate };
        }

        _logger?.LogInformation(
            "Analyzed composition: {Members} members, {Category} group, Balance: {Score:F1}",
            composition.MemberCount,
            composition.SizeCategory.GetDisplayName(),
            composition.BalanceScore);

        return composition;
    }

    /// <inheritdoc />
    public GroupPerformanceMetrics CalculateMetrics(GroupComposition composition, IEnumerable<LogEvent> events)
    {
        var eventList = events.ToList();
        var memberNames = composition.Members.Select(m => m.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);

        // Calculate session duration
        var firstEvent = eventList.FirstOrDefault()?.Timestamp;
        var lastEvent = eventList.LastOrDefault()?.Timestamp;
        var duration = firstEvent.HasValue && lastEvent.HasValue
            ? lastEvent.Value - firstEvent.Value
            : TimeSpan.Zero;

        // Calculate individual contributions
        var contributions = new Dictionary<string, MemberContribution>();
        foreach (var member in composition.Members)
        {
            var contribution = CalculateMemberContribution(member, eventList);
            contributions[member.Name] = contribution;
        }

        // Calculate totals
        var totalDamage = contributions.Values.Sum(c => c.DamageDealt);
        var totalHealing = contributions.Values.Sum(c => c.HealingDone);
        var totalKills = contributions.Values.Sum(c => c.Kills);
        var totalDeaths = contributions.Values.Sum(c => c.Deaths);

        var durationSeconds = Math.Max(1, duration.TotalSeconds);

        return new GroupPerformanceMetrics(
            CompositionId: composition.Id,
            TotalDps: totalDamage / durationSeconds,
            TotalHps: totalHealing / durationSeconds,
            TotalKills: totalKills,
            TotalDeaths: totalDeaths,
            KillDeathRatio: totalDeaths > 0 ? (double)totalKills / totalDeaths : totalKills,
            AverageMemberDps: composition.MemberCount > 0 ? (totalDamage / durationSeconds) / composition.MemberCount : 0,
            AverageMemberHps: composition.MemberCount > 0 ? (totalHealing / durationSeconds) / composition.MemberCount : 0,
            CombatDuration: duration,
            MemberContributions: contributions
        );
    }

    private MemberContribution CalculateMemberContribution(GroupMember member, List<LogEvent> events)
    {
        var memberName = member.Name;
        var isPlayer = member.IsPlayer;

        // Damage dealt
        var damageDealt = events
            .OfType<DamageEvent>()
            .Where(e => isPlayer ? e.Source == "You" : e.Source.Equals(memberName, StringComparison.OrdinalIgnoreCase))
            .Sum(e => e.DamageAmount);

        // Healing done
        var healingDone = events
            .OfType<HealingEvent>()
            .Where(e => isPlayer ? e.Source == "You" : e.Source.Equals(memberName, StringComparison.OrdinalIgnoreCase))
            .Sum(e => e.HealingAmount);

        // Kills (simplistic - attributed to last hitter)
        var kills = events
            .OfType<DeathEvent>()
            .Count(e => isPlayer
                ? e.Killer == "You"
                : e.Killer.Equals(memberName, StringComparison.OrdinalIgnoreCase));

        // Deaths
        var deaths = events
            .OfType<DeathEvent>()
            .Count(e => isPlayer
                ? e.Target == "You"
                : e.Target.Equals(memberName, StringComparison.OrdinalIgnoreCase));

        // Calculate percentages (will be updated after all members are calculated)
        return new MemberContribution(
            MemberName: memberName,
            Role: member.PrimaryRole,
            DamageDealt: damageDealt,
            HealingDone: healingDone,
            Kills: kills,
            Deaths: deaths,
            DpsContributionPercent: 0, // Set later
            HpsContributionPercent: 0  // Set later
        );
    }

    /// <inheritdoc />
    public IReadOnlyList<RoleCoverage> AnalyzeRoleCoverage(GroupComposition composition)
    {
        var coverage = new List<RoleCoverage>();
        var membersByRole = composition.MembersByRole;
        var memberCount = composition.MemberCount;

        foreach (var role in Enum.GetValues<GroupRole>())
        {
            if (role == GroupRole.Unknown)
                continue;

            var membersInRole = membersByRole.GetValueOrDefault(role, Array.Empty<GroupMember>());
            var count = membersInRole.Count;

            // Determine coverage status based on group size and role importance
            bool isCovered;
            bool isOverRepresented;

            switch (role)
            {
                case GroupRole.Healer:
                    isCovered = count >= (memberCount > 4 ? 2 : 1);
                    isOverRepresented = count > Math.Ceiling(memberCount * 0.3);
                    break;
                case GroupRole.Tank:
                    isCovered = memberCount <= 4 || count >= 1;
                    isOverRepresented = count > Math.Ceiling(memberCount * 0.4);
                    break;
                case GroupRole.CrowdControl:
                    isCovered = memberCount <= 4 || count >= 1;
                    isOverRepresented = count > Math.Ceiling(memberCount * 0.25);
                    break;
                default:
                    isCovered = true; // DPS/Support roles are always "covered" if present
                    isOverRepresented = count > Math.Ceiling(memberCount * 0.5);
                    break;
            }

            coverage.Add(new RoleCoverage(
                Role: role,
                MemberCount: count,
                IsCovered: isCovered,
                IsOverRepresented: isOverRepresented && count > 0,
                MemberNames: membersInRole.Select(m => m.Name).ToList()
            ));
        }

        return coverage;
    }

    /// <inheritdoc />
    public double CalculateBalanceScore(GroupComposition composition)
    {
        if (composition.MemberCount <= 1)
            return 50;

        var roleCounts = composition.Members
            .GroupBy(m => m.PrimaryRole)
            .ToDictionary(g => g.Key, g => g.Count());

        double score = 100;

        // Critical: Missing healer
        if (!roleCounts.ContainsKey(GroupRole.Healer) && composition.MemberCount > 2)
            score -= 35;

        // Important: Missing tank for larger groups
        if (!roleCounts.ContainsKey(GroupRole.Tank) && composition.MemberCount >= 5)
            score -= 20;

        // Check for DPS presence
        var hasDps = roleCounts.ContainsKey(GroupRole.MeleeDps) ||
                     roleCounts.ContainsKey(GroupRole.CasterDps) ||
                     roleCounts.ContainsKey(GroupRole.Hybrid);
        if (!hasDps)
            score -= 25;

        // Bonus for CC in larger groups
        if (roleCounts.ContainsKey(GroupRole.CrowdControl) && composition.MemberCount >= 5)
            score += 5;

        // Bonus for support
        if (roleCounts.ContainsKey(GroupRole.Support))
            score += 5;

        // Penalize heavy imbalance
        var maxRolePercent = roleCounts.Values.DefaultIfEmpty(0).Max() / (double)composition.MemberCount;
        if (maxRolePercent > 0.6)
            score -= 15;
        else if (maxRolePercent > 0.5)
            score -= 10;

        // Penalize having too many unknowns
        var unknownCount = roleCounts.GetValueOrDefault(GroupRole.Unknown, 0);
        if (unknownCount > composition.MemberCount * 0.3)
            score -= 10;

        return Math.Clamp(score, 0, 100);
    }

    /// <inheritdoc />
    public GroupTemplate? MatchTemplate(GroupComposition composition)
    {
        GroupTemplate? bestMatch = null;
        double bestScore = 50; // Minimum threshold for a match

        foreach (var template in _templates)
        {
            var score = template.CalculateMatchScore(composition);
            if (score > bestScore)
            {
                bestScore = score;
                bestMatch = template;
            }
        }

        _logger?.LogDebug(
            "Best template match: {Template} with score {Score:F1}",
            bestMatch?.Name ?? "None",
            bestScore);

        return bestMatch;
    }

    /// <inheritdoc />
    public IReadOnlyList<CompositionRecommendation> GenerateRecommendations(GroupComposition composition)
    {
        var recommendations = new List<CompositionRecommendation>();
        var coverage = AnalyzeRoleCoverage(composition);
        var memberCount = composition.MemberCount;

        // Check for missing healers
        var healerCoverage = coverage.First(c => c.Role == GroupRole.Healer);
        if (!healerCoverage.IsCovered && memberCount > 2)
        {
            recommendations.Add(new CompositionRecommendation(
                Type: RecommendationType.AddRole,
                TargetRole: GroupRole.Healer,
                Message: memberCount > 4
                    ? "Critical: Add at least 2 healers for sustained combat"
                    : "Add a healer for group sustainability",
                Priority: RecommendationPriority.Critical
            ));
        }

        // Check for missing tanks in larger groups
        var tankCoverage = coverage.First(c => c.Role == GroupRole.Tank);
        if (!tankCoverage.IsCovered && memberCount >= 5)
        {
            recommendations.Add(new CompositionRecommendation(
                Type: RecommendationType.AddRole,
                TargetRole: GroupRole.Tank,
                Message: "Add a tank to absorb damage and protect healers",
                Priority: RecommendationPriority.High
            ));
        }

        // Check for missing CC
        var ccCoverage = coverage.First(c => c.Role == GroupRole.CrowdControl);
        if (ccCoverage.MemberCount == 0 && memberCount >= 5)
        {
            recommendations.Add(new CompositionRecommendation(
                Type: RecommendationType.AddRole,
                TargetRole: GroupRole.CrowdControl,
                Message: "Consider adding crowd control for enemy lockdown",
                Priority: RecommendationPriority.Medium
            ));
        }

        // Check for over-representation
        foreach (var role in coverage.Where(c => c.IsOverRepresented))
        {
            recommendations.Add(new CompositionRecommendation(
                Type: RecommendationType.ReduceRole,
                TargetRole: role.Role,
                Message: $"Consider diversifying: {role.MemberCount} {role.Role.GetDisplayName()}s may be too many",
                Priority: RecommendationPriority.Low
            ));
        }

        // Template matching suggestions
        if (composition.MatchedTemplate != null && composition.BalanceScore < 80)
        {
            var template = composition.MatchedTemplate;
            foreach (var (role, requirement) in template.RoleRequirements)
            {
                var actualCount = coverage.First(c => c.Role == role).MemberCount;
                if (actualCount < requirement.MinCount && requirement.IsRequired)
                {
                    recommendations.Add(new CompositionRecommendation(
                        Type: RecommendationType.TemplateMatch,
                        TargetRole: role,
                        Message: $"For {template.Name}: Need {requirement.MinCount - actualCount} more {role.GetDisplayName()}",
                        Priority: RecommendationPriority.Medium
                    ));
                }
            }
        }

        // Check for no DPS
        var hasDps = coverage.Any(c =>
            (c.Role == GroupRole.MeleeDps || c.Role == GroupRole.CasterDps || c.Role == GroupRole.Hybrid) &&
            c.MemberCount > 0);
        if (!hasDps && memberCount > 1)
        {
            recommendations.Add(new CompositionRecommendation(
                Type: RecommendationType.AddRole,
                TargetRole: GroupRole.MeleeDps,
                Message: "Add damage dealers to increase kill speed",
                Priority: RecommendationPriority.High
            ));
        }

        // Sort by priority (highest first)
        return recommendations
            .OrderByDescending(r => r.Priority)
            .ThenBy(r => r.Type)
            .ToList();
    }

    /// <inheritdoc />
    public IReadOnlyList<GroupTemplate> GetAvailableTemplates() => _templates;

    /// <inheritdoc />
    public GroupAnalysisSummary PerformFullAnalysis(IEnumerable<LogEvent> events)
    {
        var eventList = events.ToList();

        var composition = AnalyzeComposition(eventList);
        var metrics = CalculateMetrics(composition, eventList);
        var roleCoverage = AnalyzeRoleCoverage(composition);
        var recommendations = GenerateRecommendations(composition);

        // Update contribution percentages in metrics
        var totalDamage = metrics.MemberContributions.Values.Sum(c => c.DamageDealt);
        var totalHealing = metrics.MemberContributions.Values.Sum(c => c.HealingDone);

        var updatedContributions = metrics.MemberContributions.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value with
            {
                DpsContributionPercent = totalDamage > 0 ? (kvp.Value.DamageDealt / (double)totalDamage) * 100 : 0,
                HpsContributionPercent = totalHealing > 0 ? (kvp.Value.HealingDone / (double)totalHealing) * 100 : 0
            }
        );

        var updatedMetrics = metrics with { MemberContributions = updatedContributions };

        return new GroupAnalysisSummary(
            Composition: composition,
            Metrics: updatedMetrics,
            RoleCoverage: roleCoverage,
            Recommendations: recommendations
        );
    }

    /// <inheritdoc />
    public void AddManualMember(string name, CharacterClass? characterClass = null, Realm? realm = null)
    {
        _detectionService.AddManualMember(name, characterClass, realm);
    }

    /// <inheritdoc />
    public bool RemoveManualMember(string name)
    {
        return _detectionService.RemoveManualMember(name);
    }

    /// <inheritdoc />
    public IReadOnlyList<(string Name, CharacterClass? Class, Realm? Realm)> GetManualMembers()
    {
        return _detectionService.GetManualMembers();
    }

    /// <inheritdoc />
    public void ClearManualMembers()
    {
        _detectionService.ClearManualMembers();
    }

    /// <inheritdoc />
    public void Reset()
    {
        _detectionService.Reset();
        _logger?.LogDebug("Group analysis service reset");
    }
}
