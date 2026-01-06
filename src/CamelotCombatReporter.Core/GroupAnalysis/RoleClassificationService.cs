using CamelotCombatReporter.Core.GroupAnalysis.Models;
using CamelotCombatReporter.Core.Models;

namespace CamelotCombatReporter.Core.GroupAnalysis;

/// <summary>
/// Service for classifying character classes into group roles.
/// Provides mapping from CharacterClass to primary and secondary GroupRole.
/// </summary>
public class RoleClassificationService
{
    /// <summary>
    /// Static mapping of all character classes to their roles.
    /// Each class has a primary role and an optional secondary role.
    /// </summary>
    private static readonly Dictionary<CharacterClass, (GroupRole Primary, GroupRole? Secondary)> ClassRoleMap = new()
    {
        // ═══════════════════════════════════════════════════════════════════
        // ALBION CLASSES
        // ═══════════════════════════════════════════════════════════════════

        // Tanks
        [CharacterClass.Armsman] = (GroupRole.Tank, GroupRole.MeleeDps),
        [CharacterClass.Paladin] = (GroupRole.Tank, GroupRole.Healer),
        [CharacterClass.Mercenary] = (GroupRole.MeleeDps, GroupRole.Tank),
        [CharacterClass.Reaver] = (GroupRole.Hybrid, GroupRole.Tank),

        // Healers
        [CharacterClass.Cleric] = (GroupRole.Healer, GroupRole.Support),
        [CharacterClass.Friar] = (GroupRole.Healer, GroupRole.MeleeDps),

        // Casters
        [CharacterClass.Wizard] = (GroupRole.CasterDps, null),
        [CharacterClass.Theurgist] = (GroupRole.CasterDps, GroupRole.Support),
        [CharacterClass.Sorcerer] = (GroupRole.CrowdControl, GroupRole.CasterDps),
        [CharacterClass.Cabalist] = (GroupRole.CasterDps, GroupRole.CrowdControl),
        [CharacterClass.Necromancer] = (GroupRole.CasterDps, null),
        [CharacterClass.Heretic] = (GroupRole.Hybrid, GroupRole.CasterDps),

        // Stealthers/DPS
        [CharacterClass.Infiltrator] = (GroupRole.MeleeDps, null),
        [CharacterClass.Scout] = (GroupRole.MeleeDps, null),
        [CharacterClass.Minstrel] = (GroupRole.Support, GroupRole.MeleeDps),

        // Mauler
        [CharacterClass.MaulerAlb] = (GroupRole.Hybrid, GroupRole.MeleeDps),

        // ═══════════════════════════════════════════════════════════════════
        // MIDGARD CLASSES
        // ═══════════════════════════════════════════════════════════════════

        // Tanks
        [CharacterClass.Warrior] = (GroupRole.Tank, GroupRole.MeleeDps),
        [CharacterClass.Thane] = (GroupRole.Tank, GroupRole.CasterDps),
        [CharacterClass.Berserker] = (GroupRole.MeleeDps, GroupRole.Tank),
        [CharacterClass.Savage] = (GroupRole.MeleeDps, null),
        [CharacterClass.Valkyrie] = (GroupRole.Hybrid, GroupRole.Healer),

        // Healers
        [CharacterClass.Healer] = (GroupRole.Healer, GroupRole.CrowdControl),
        [CharacterClass.Shaman] = (GroupRole.Healer, GroupRole.Support),

        // Casters
        [CharacterClass.Runemaster] = (GroupRole.CrowdControl, GroupRole.CasterDps),
        [CharacterClass.Spiritmaster] = (GroupRole.CasterDps, GroupRole.CrowdControl),
        [CharacterClass.Bonedancer] = (GroupRole.CasterDps, null),
        [CharacterClass.Warlock] = (GroupRole.CasterDps, GroupRole.Support),

        // Stealthers/Support
        [CharacterClass.Shadowblade] = (GroupRole.MeleeDps, null),
        [CharacterClass.Hunter] = (GroupRole.MeleeDps, null),
        [CharacterClass.Skald] = (GroupRole.Support, GroupRole.MeleeDps),

        // Mauler
        [CharacterClass.MaulerMid] = (GroupRole.Hybrid, GroupRole.MeleeDps),

        // ═══════════════════════════════════════════════════════════════════
        // HIBERNIA CLASSES
        // ═══════════════════════════════════════════════════════════════════

        // Tanks
        [CharacterClass.Hero] = (GroupRole.Tank, GroupRole.MeleeDps),
        [CharacterClass.Champion] = (GroupRole.Tank, GroupRole.Support),
        [CharacterClass.Blademaster] = (GroupRole.MeleeDps, GroupRole.Tank),
        [CharacterClass.Valewalker] = (GroupRole.Hybrid, GroupRole.MeleeDps),
        [CharacterClass.Vampiir] = (GroupRole.Hybrid, GroupRole.MeleeDps),

        // Healers
        [CharacterClass.Druid] = (GroupRole.Healer, GroupRole.Support),
        [CharacterClass.Warden] = (GroupRole.Healer, GroupRole.Tank),
        [CharacterClass.Bard] = (GroupRole.Support, GroupRole.Healer),

        // Casters
        [CharacterClass.Eldritch] = (GroupRole.CasterDps, null),
        [CharacterClass.Mentalist] = (GroupRole.CrowdControl, GroupRole.CasterDps),
        [CharacterClass.Enchanter] = (GroupRole.Support, GroupRole.CrowdControl),
        [CharacterClass.Animist] = (GroupRole.CasterDps, GroupRole.Support),
        [CharacterClass.Bainshee] = (GroupRole.CasterDps, null),

        // Stealthers
        [CharacterClass.Nightshade] = (GroupRole.MeleeDps, null),
        [CharacterClass.Ranger] = (GroupRole.MeleeDps, null),

        // Mauler
        [CharacterClass.MaulerHib] = (GroupRole.Hybrid, GroupRole.MeleeDps)
    };

    /// <summary>
    /// Gets the primary role for a character class.
    /// </summary>
    public GroupRole GetPrimaryRole(CharacterClass characterClass)
    {
        if (ClassRoleMap.TryGetValue(characterClass, out var roles))
        {
            return roles.Primary;
        }

        return GroupRole.Unknown;
    }

    /// <summary>
    /// Gets the secondary role for a character class, if any.
    /// </summary>
    public GroupRole? GetSecondaryRole(CharacterClass characterClass)
    {
        if (ClassRoleMap.TryGetValue(characterClass, out var roles))
        {
            return roles.Secondary;
        }

        return null;
    }

    /// <summary>
    /// Gets both primary and secondary roles for a character class.
    /// </summary>
    public (GroupRole Primary, GroupRole? Secondary) GetRolesForClass(CharacterClass characterClass)
    {
        if (ClassRoleMap.TryGetValue(characterClass, out var roles))
        {
            return roles;
        }

        return (GroupRole.Unknown, null);
    }

    /// <summary>
    /// Gets all character classes that can fill a specific role.
    /// </summary>
    /// <param name="role">The role to find classes for.</param>
    /// <param name="includePrimary">Include classes with this as primary role.</param>
    /// <param name="includeSecondary">Include classes with this as secondary role.</param>
    /// <returns>List of character classes.</returns>
    public IReadOnlyList<CharacterClass> GetClassesForRole(
        GroupRole role,
        bool includePrimary = true,
        bool includeSecondary = true)
    {
        var result = new List<CharacterClass>();

        foreach (var (characterClass, roles) in ClassRoleMap)
        {
            if (includePrimary && roles.Primary == role)
            {
                result.Add(characterClass);
            }
            else if (includeSecondary && roles.Secondary == role)
            {
                result.Add(characterClass);
            }
        }

        return result;
    }

    /// <summary>
    /// Gets classes grouped by their primary role.
    /// </summary>
    public IReadOnlyDictionary<GroupRole, IReadOnlyList<CharacterClass>> GetClassesByPrimaryRole()
    {
        return ClassRoleMap
            .GroupBy(kvp => kvp.Value.Primary)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<CharacterClass>)g.Select(kvp => kvp.Key).ToList()
            );
    }

    /// <summary>
    /// Checks if a class can fulfill a specific role (primary or secondary).
    /// </summary>
    public bool CanFulfillRole(CharacterClass characterClass, GroupRole role)
    {
        if (ClassRoleMap.TryGetValue(characterClass, out var roles))
        {
            return roles.Primary == role || roles.Secondary == role;
        }

        return false;
    }

    /// <summary>
    /// Gets all roles a class can fulfill.
    /// </summary>
    public IReadOnlyList<GroupRole> GetAllRolesForClass(CharacterClass characterClass)
    {
        if (ClassRoleMap.TryGetValue(characterClass, out var roles))
        {
            var result = new List<GroupRole> { roles.Primary };
            if (roles.Secondary.HasValue)
            {
                result.Add(roles.Secondary.Value);
            }
            return result;
        }

        return Array.Empty<GroupRole>();
    }

    /// <summary>
    /// Gets statistics about role distribution across all classes.
    /// </summary>
    public IReadOnlyDictionary<GroupRole, int> GetRoleDistributionStatistics()
    {
        var distribution = new Dictionary<GroupRole, int>();

        foreach (var role in Enum.GetValues<GroupRole>())
        {
            if (role != GroupRole.Unknown)
            {
                var count = ClassRoleMap.Count(kvp =>
                    kvp.Value.Primary == role || kvp.Value.Secondary == role);
                distribution[role] = count;
            }
        }

        return distribution;
    }

    /// <summary>
    /// Gets a role suggestion based on what the group is missing.
    /// </summary>
    public IReadOnlyList<CharacterClass> SuggestClassesForMissingRole(
        GroupRole missingRole,
        Realm? preferredRealm = null)
    {
        var classes = GetClassesForRole(missingRole, includePrimary: true, includeSecondary: false);

        if (preferredRealm.HasValue)
        {
            var realmClasses = classes
                .Where(c => c.GetRealm() == preferredRealm.Value)
                .ToList();

            if (realmClasses.Count > 0)
            {
                return realmClasses;
            }
        }

        return classes;
    }
}
