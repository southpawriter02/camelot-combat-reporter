using CamelotCombatReporter.Core.GroupAnalysis;
using CamelotCombatReporter.Core.GroupAnalysis.Models;
using CamelotCombatReporter.Core.Models;
using Xunit;

namespace CamelotCombatReporter.Core.Tests;

public class GroupAnalysisTests
{
    #region RoleClassificationService Tests

    [Fact]
    public void RoleClassificationService_AllClassesHavePrimaryRole()
    {
        var service = new RoleClassificationService();

        foreach (var characterClass in Enum.GetValues<CharacterClass>())
        {
            if (characterClass == CharacterClass.Unknown)
                continue;

            var role = service.GetPrimaryRole(characterClass);
            Assert.NotEqual(GroupRole.Unknown, role);
        }
    }

    [Theory]
    [InlineData(CharacterClass.Cleric, GroupRole.Healer)]
    [InlineData(CharacterClass.Armsman, GroupRole.Tank)]
    [InlineData(CharacterClass.Wizard, GroupRole.CasterDps)]
    [InlineData(CharacterClass.Sorcerer, GroupRole.CrowdControl)]
    [InlineData(CharacterClass.Minstrel, GroupRole.Support)]
    [InlineData(CharacterClass.Infiltrator, GroupRole.MeleeDps)]
    [InlineData(CharacterClass.Reaver, GroupRole.Hybrid)]
    public void RoleClassificationService_PrimaryRoleIsCorrect(CharacterClass characterClass, GroupRole expectedRole)
    {
        var service = new RoleClassificationService();
        var role = service.GetPrimaryRole(characterClass);
        Assert.Equal(expectedRole, role);
    }

    [Theory]
    [InlineData(CharacterClass.Paladin, GroupRole.Healer)]
    [InlineData(CharacterClass.Cleric, GroupRole.Support)]
    [InlineData(CharacterClass.Sorcerer, GroupRole.CasterDps)]
    [InlineData(CharacterClass.Mercenary, GroupRole.Tank)]
    public void RoleClassificationService_SecondaryRoleIsCorrect(CharacterClass characterClass, GroupRole expectedRole)
    {
        var service = new RoleClassificationService();
        var secondaryRole = service.GetSecondaryRole(characterClass);
        Assert.NotNull(secondaryRole);
        Assert.Equal(expectedRole, secondaryRole.Value);
    }

    [Fact]
    public void RoleClassificationService_GetClassesForRole_ReturnsCorrectClasses()
    {
        var service = new RoleClassificationService();

        var healers = service.GetClassesForRole(GroupRole.Healer, includePrimary: true, includeSecondary: false);

        Assert.Contains(CharacterClass.Cleric, healers);
        Assert.Contains(CharacterClass.Healer, healers);
        Assert.Contains(CharacterClass.Druid, healers);
        Assert.DoesNotContain(CharacterClass.Wizard, healers);
    }

    [Fact]
    public void RoleClassificationService_CanFulfillRole_ChecksBothRoles()
    {
        var service = new RoleClassificationService();

        // Paladin is Tank primary, Healer secondary
        Assert.True(service.CanFulfillRole(CharacterClass.Paladin, GroupRole.Tank));
        Assert.True(service.CanFulfillRole(CharacterClass.Paladin, GroupRole.Healer));
        Assert.False(service.CanFulfillRole(CharacterClass.Paladin, GroupRole.CasterDps));
    }

    #endregion

    #region GroupDetectionService Tests

    [Fact]
    public void GroupDetectionService_DetectsPlayerAsGroupMember()
    {
        var service = new GroupDetectionService();
        var events = new List<LogEvent>
        {
            new DamageEvent(TimeOnly.Parse("12:00:00"), "You", "Enemy", 100, "Physical", null, null, null)
        };

        var members = service.DetectGroupMembers(events);

        Assert.Contains(members, m => m.IsPlayer);
    }

    [Fact]
    public void GroupDetectionService_DetectsHealerFromHealingEvents()
    {
        var service = new GroupDetectionService { MinInteractions = 2 };
        var events = new List<LogEvent>
        {
            new HealingEvent(TimeOnly.Parse("12:00:00"), "Healer1", "You", 100),
            new HealingEvent(TimeOnly.Parse("12:00:01"), "Healer1", "You", 150),
            new HealingEvent(TimeOnly.Parse("12:00:02"), "Healer1", "You", 200),
        };

        var members = service.DetectGroupMembers(events);

        Assert.Contains(members, m => m.Name == "Healer1");
        var healer = members.First(m => m.Name == "Healer1");
        Assert.Equal(GroupRole.Healer, healer.PrimaryRole);
    }

    [Fact]
    public void GroupDetectionService_ManualMembersAreIncluded()
    {
        var service = new GroupDetectionService();
        service.AddManualMember("ManualPlayer", CharacterClass.Cleric, Realm.Albion);

        var events = new List<LogEvent>
        {
            new DamageEvent(TimeOnly.Parse("12:00:00"), "You", "Enemy", 100, "Physical", null, null, null)
        };

        var members = service.DetectGroupMembers(events);

        Assert.Contains(members, m => m.Name == "ManualPlayer");
        var manual = members.First(m => m.Name == "ManualPlayer");
        Assert.Equal(CharacterClass.Cleric, manual.Class);
        Assert.Equal(GroupMemberSource.Manual, manual.Source);
    }

    [Fact]
    public void GroupDetectionService_RemoveManualMember_Works()
    {
        var service = new GroupDetectionService();
        service.AddManualMember("Player1", CharacterClass.Cleric);
        service.AddManualMember("Player2", CharacterClass.Wizard);

        var removed = service.RemoveManualMember("Player1");

        Assert.True(removed);
        var manualMembers = service.GetManualMembers();
        Assert.Single(manualMembers);
        Assert.Equal("Player2", manualMembers[0].Name);
    }

    [Fact]
    public void GroupDetectionService_RespectsMinInteractionsThreshold()
    {
        var service = new GroupDetectionService { MinInteractions = 5 };
        var events = new List<LogEvent>
        {
            new HealingEvent(TimeOnly.Parse("12:00:00"), "Healer1", "You", 100),
            new HealingEvent(TimeOnly.Parse("12:00:01"), "Healer1", "You", 150),
            // Only 2 interactions, below threshold of 5
        };

        var members = service.DetectGroupMembers(events);

        // Should only have the player, not Healer1
        Assert.DoesNotContain(members, m => m.Name == "Healer1");
    }

    [Fact]
    public void GroupDetectionService_BuildComposition_SetsCorrectSizeCategory()
    {
        var service = new GroupDetectionService();

        // Solo
        var soloMembers = new List<GroupMember>
        {
            new("You", CharacterClass.Armsman, Realm.Albion, GroupRole.Tank, null, GroupMemberSource.Manual, TimeOnly.MinValue, null, true)
        };
        var soloComp = service.BuildComposition(soloMembers, TimeOnly.MinValue);
        Assert.Equal(GroupSizeCategory.Solo, soloComp.SizeCategory);

        // Small-man (3 members)
        var smallManMembers = new List<GroupMember>
        {
            new("You", CharacterClass.Armsman, Realm.Albion, GroupRole.Tank, null, GroupMemberSource.Manual, TimeOnly.MinValue, null, true),
            new("Healer", CharacterClass.Cleric, Realm.Albion, GroupRole.Healer, null, GroupMemberSource.Manual, TimeOnly.MinValue, null, false),
            new("DPS", CharacterClass.Wizard, Realm.Albion, GroupRole.CasterDps, null, GroupMemberSource.Manual, TimeOnly.MinValue, null, false),
        };
        var smallManComp = service.BuildComposition(smallManMembers, TimeOnly.MinValue);
        Assert.Equal(GroupSizeCategory.SmallMan, smallManComp.SizeCategory);

        // 8-man (6 members)
        var eightManMembers = Enumerable.Range(0, 6)
            .Select(i => new GroupMember($"Player{i}", CharacterClass.Armsman, Realm.Albion, GroupRole.Tank, null, GroupMemberSource.Manual, TimeOnly.MinValue, null, i == 0))
            .ToList();
        var eightManComp = service.BuildComposition(eightManMembers, TimeOnly.MinValue);
        Assert.Equal(GroupSizeCategory.EightMan, eightManComp.SizeCategory);
    }

    #endregion

    #region GroupAnalysisService Tests

    [Fact]
    public void GroupAnalysisService_CalculateBalanceScore_PenalizesMissingHealer()
    {
        var service = new GroupAnalysisService();

        // Group with healer
        var withHealer = new GroupComposition(
            Guid.NewGuid(),
            new List<GroupMember>
            {
                new("Tank", CharacterClass.Armsman, Realm.Albion, GroupRole.Tank, null, GroupMemberSource.Manual, TimeOnly.MinValue, null, false),
                new("Healer", CharacterClass.Cleric, Realm.Albion, GroupRole.Healer, null, GroupMemberSource.Manual, TimeOnly.MinValue, null, false),
                new("DPS", CharacterClass.Wizard, Realm.Albion, GroupRole.CasterDps, null, GroupMemberSource.Manual, TimeOnly.MinValue, null, false),
            },
            GroupSizeCategory.SmallMan,
            null,
            0,
            TimeOnly.MinValue,
            null
        );

        // Group without healer
        var withoutHealer = new GroupComposition(
            Guid.NewGuid(),
            new List<GroupMember>
            {
                new("Tank", CharacterClass.Armsman, Realm.Albion, GroupRole.Tank, null, GroupMemberSource.Manual, TimeOnly.MinValue, null, false),
                new("DPS1", CharacterClass.Wizard, Realm.Albion, GroupRole.CasterDps, null, GroupMemberSource.Manual, TimeOnly.MinValue, null, false),
                new("DPS2", CharacterClass.Infiltrator, Realm.Albion, GroupRole.MeleeDps, null, GroupMemberSource.Manual, TimeOnly.MinValue, null, false),
            },
            GroupSizeCategory.SmallMan,
            null,
            0,
            TimeOnly.MinValue,
            null
        );

        var scoreWithHealer = service.CalculateBalanceScore(withHealer);
        var scoreWithoutHealer = service.CalculateBalanceScore(withoutHealer);

        Assert.True(scoreWithHealer > scoreWithoutHealer);
    }

    [Fact]
    public void GroupAnalysisService_MatchTemplate_Finds8ManRvR()
    {
        var service = new GroupAnalysisService();

        var composition = new GroupComposition(
            Guid.NewGuid(),
            new List<GroupMember>
            {
                new("Tank1", CharacterClass.Armsman, Realm.Albion, GroupRole.Tank, null, GroupMemberSource.Manual, TimeOnly.MinValue, null, false),
                new("Tank2", CharacterClass.Paladin, Realm.Albion, GroupRole.Tank, null, GroupMemberSource.Manual, TimeOnly.MinValue, null, false),
                new("Healer1", CharacterClass.Cleric, Realm.Albion, GroupRole.Healer, null, GroupMemberSource.Manual, TimeOnly.MinValue, null, false),
                new("Healer2", CharacterClass.Friar, Realm.Albion, GroupRole.Healer, null, GroupMemberSource.Manual, TimeOnly.MinValue, null, false),
                new("CC", CharacterClass.Sorcerer, Realm.Albion, GroupRole.CrowdControl, null, GroupMemberSource.Manual, TimeOnly.MinValue, null, false),
                new("DPS1", CharacterClass.Wizard, Realm.Albion, GroupRole.CasterDps, null, GroupMemberSource.Manual, TimeOnly.MinValue, null, false),
                new("DPS2", CharacterClass.Infiltrator, Realm.Albion, GroupRole.MeleeDps, null, GroupMemberSource.Manual, TimeOnly.MinValue, null, false),
            },
            GroupSizeCategory.EightMan,
            null,
            0,
            TimeOnly.MinValue,
            null
        );

        var template = service.MatchTemplate(composition);

        Assert.NotNull(template);
        Assert.Equal("8-Man RvR", template.Name);
    }

    [Fact]
    public void GroupAnalysisService_GenerateRecommendations_SuggestsMissingRoles()
    {
        var service = new GroupAnalysisService();

        // Group missing healer
        var composition = new GroupComposition(
            Guid.NewGuid(),
            new List<GroupMember>
            {
                new("Tank", CharacterClass.Armsman, Realm.Albion, GroupRole.Tank, null, GroupMemberSource.Manual, TimeOnly.MinValue, null, false),
                new("DPS1", CharacterClass.Wizard, Realm.Albion, GroupRole.CasterDps, null, GroupMemberSource.Manual, TimeOnly.MinValue, null, false),
                new("DPS2", CharacterClass.Infiltrator, Realm.Albion, GroupRole.MeleeDps, null, GroupMemberSource.Manual, TimeOnly.MinValue, null, false),
            },
            GroupSizeCategory.SmallMan,
            null,
            50,
            TimeOnly.MinValue,
            null
        );

        var recommendations = service.GenerateRecommendations(composition);

        Assert.NotEmpty(recommendations);
        Assert.Contains(recommendations, r =>
            r.Type == RecommendationType.AddRole &&
            r.TargetRole == GroupRole.Healer);
    }

    [Fact]
    public void GroupAnalysisService_AnalyzeRoleCoverage_IdentifiesMissingRoles()
    {
        var service = new GroupAnalysisService();

        var composition = new GroupComposition(
            Guid.NewGuid(),
            new List<GroupMember>
            {
                new("Tank1", CharacterClass.Armsman, Realm.Albion, GroupRole.Tank, null, GroupMemberSource.Manual, TimeOnly.MinValue, null, false),
                new("Tank2", CharacterClass.Paladin, Realm.Albion, GroupRole.Tank, null, GroupMemberSource.Manual, TimeOnly.MinValue, null, false),
                new("Tank3", CharacterClass.Mercenary, Realm.Albion, GroupRole.Tank, null, GroupMemberSource.Manual, TimeOnly.MinValue, null, false),
                new("Tank4", CharacterClass.Warrior, Realm.Midgard, GroupRole.Tank, null, GroupMemberSource.Manual, TimeOnly.MinValue, null, false),
                new("Tank5", CharacterClass.Hero, Realm.Hibernia, GroupRole.Tank, null, GroupMemberSource.Manual, TimeOnly.MinValue, null, false),
            },
            GroupSizeCategory.EightMan,
            null,
            0,
            TimeOnly.MinValue,
            null
        );

        var coverage = service.AnalyzeRoleCoverage(composition);

        var tankCoverage = coverage.First(c => c.Role == GroupRole.Tank);
        Assert.True(tankCoverage.IsOverRepresented);

        var healerCoverage = coverage.First(c => c.Role == GroupRole.Healer);
        Assert.False(healerCoverage.IsCovered);
    }

    [Fact]
    public void GroupAnalysisService_GetAvailableTemplates_ReturnsTemplates()
    {
        var service = new GroupAnalysisService();
        var templates = service.GetAvailableTemplates();

        Assert.NotEmpty(templates);
        Assert.Contains(templates, t => t.Name == "8-Man RvR");
        Assert.Contains(templates, t => t.Name == "Small-Man");
        Assert.Contains(templates, t => t.Name == "Gank Group");
    }

    [Fact]
    public void GroupAnalysisService_ManualMemberManagement_Works()
    {
        var service = new GroupAnalysisService();

        service.AddManualMember("Player1", CharacterClass.Cleric, Realm.Albion);
        service.AddManualMember("Player2", CharacterClass.Wizard, Realm.Albion);

        var members = service.GetManualMembers();
        Assert.Equal(2, members.Count);

        service.RemoveManualMember("Player1");
        members = service.GetManualMembers();
        Assert.Single(members);

        service.ClearManualMembers();
        members = service.GetManualMembers();
        Assert.Empty(members);
    }

    #endregion

    #region GroupTemplate Tests

    [Fact]
    public void GroupTemplate_CalculateMatchScore_ReturnsHighScoreForGoodMatch()
    {
        var template = new GroupTemplate(
            "Test Template",
            "Test",
            new Dictionary<GroupRole, RoleRequirement>
            {
                [GroupRole.Tank] = new(2, 2, true),
                [GroupRole.Healer] = new(2, 2, true),
            },
            4,
            8
        );

        var composition = new GroupComposition(
            Guid.NewGuid(),
            new List<GroupMember>
            {
                new("Tank1", null, null, GroupRole.Tank, null, GroupMemberSource.Manual, TimeOnly.MinValue, null, false),
                new("Tank2", null, null, GroupRole.Tank, null, GroupMemberSource.Manual, TimeOnly.MinValue, null, false),
                new("Healer1", null, null, GroupRole.Healer, null, GroupMemberSource.Manual, TimeOnly.MinValue, null, false),
                new("Healer2", null, null, GroupRole.Healer, null, GroupMemberSource.Manual, TimeOnly.MinValue, null, false),
            },
            GroupSizeCategory.SmallMan,
            null,
            0,
            TimeOnly.MinValue,
            null
        );

        var score = template.CalculateMatchScore(composition);
        Assert.True(score >= 90); // Should be a very good match
    }

    [Fact]
    public void GroupTemplate_CalculateMatchScore_ReturnsLowScoreForBadMatch()
    {
        var template = new GroupTemplate(
            "Test Template",
            "Test",
            new Dictionary<GroupRole, RoleRequirement>
            {
                [GroupRole.Healer] = new(2, 2, true),
            },
            4,
            8
        );

        var composition = new GroupComposition(
            Guid.NewGuid(),
            new List<GroupMember>
            {
                new("DPS1", null, null, GroupRole.MeleeDps, null, GroupMemberSource.Manual, TimeOnly.MinValue, null, false),
                new("DPS2", null, null, GroupRole.MeleeDps, null, GroupMemberSource.Manual, TimeOnly.MinValue, null, false),
                new("DPS3", null, null, GroupRole.CasterDps, null, GroupMemberSource.Manual, TimeOnly.MinValue, null, false),
                new("DPS4", null, null, GroupRole.CasterDps, null, GroupMemberSource.Manual, TimeOnly.MinValue, null, false),
            },
            GroupSizeCategory.SmallMan,
            null,
            0,
            TimeOnly.MinValue,
            null
        );

        var score = template.CalculateMatchScore(composition);
        Assert.True(score < 50); // Should be a poor match (no healers)
    }

    #endregion

    #region Enum Extension Tests

    [Theory]
    [InlineData(1, GroupSizeCategory.Solo)]
    [InlineData(2, GroupSizeCategory.SmallMan)]
    [InlineData(4, GroupSizeCategory.SmallMan)]
    [InlineData(5, GroupSizeCategory.EightMan)]
    [InlineData(8, GroupSizeCategory.EightMan)]
    [InlineData(9, GroupSizeCategory.Battlegroup)]
    [InlineData(24, GroupSizeCategory.Battlegroup)]
    public void GroupSizeCategory_FromMemberCount_ReturnsCorrectCategory(int count, GroupSizeCategory expected)
    {
        var result = GroupSizeCategoryExtensions.FromMemberCount(count);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void GroupRole_GetDisplayName_ReturnsReadableName()
    {
        Assert.Equal("Crowd Control", GroupRole.CrowdControl.GetDisplayName());
        Assert.Equal("Melee DPS", GroupRole.MeleeDps.GetDisplayName());
        Assert.Equal("Caster DPS", GroupRole.CasterDps.GetDisplayName());
    }

    [Fact]
    public void GroupRole_GetAbbreviation_ReturnsShortCode()
    {
        Assert.Equal("CC", GroupRole.CrowdControl.GetAbbreviation());
        Assert.Equal("TNK", GroupRole.Tank.GetAbbreviation());
        Assert.Equal("HLR", GroupRole.Healer.GetAbbreviation());
    }

    #endregion
}
