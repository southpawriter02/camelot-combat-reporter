using CamelotCombatReporter.Core.CharacterBuilding.Services;
using CamelotCombatReporter.Core.Models;

namespace CamelotCombatReporter.Core.Tests.CharacterBuilding;

/// <summary>
/// Tests for the MetaBuildTemplateService.
/// </summary>
public class MetaBuildTemplateServiceTests
{
    private readonly MetaBuildTemplateService _service = new();

    [Fact]
    public void GetAllTemplates_ReturnsNonEmptyList()
    {
        // Act
        var templates = _service.GetAllTemplates();

        // Assert
        Assert.NotEmpty(templates);
        Assert.True(templates.Count > 30); // Should have 45+ templates
    }

    [Fact]
    public void GetAllTemplates_ContainsTemplatesForAllRealms()
    {
        // Act
        var templates = _service.GetAllTemplates();

        // Assert
        Assert.Contains(templates, t => t.Realm == Realm.Albion);
        Assert.Contains(templates, t => t.Realm == Realm.Midgard);
        Assert.Contains(templates, t => t.Realm == Realm.Hibernia);
    }

    [Fact]
    public void GetTemplatesForClass_ReturnsTemplatesForArmsman()
    {
        // Act
        var templates = _service.GetTemplatesForClass(CharacterClass.Armsman);

        // Assert
        Assert.NotEmpty(templates);
        Assert.All(templates, t => Assert.Equal(CharacterClass.Armsman, t.TargetClass));
    }

    [Fact]
    public void GetTemplatesForClass_ReturnsMultipleBuildsForPopularClasses()
    {
        // Act
        var armsmanTemplates = _service.GetTemplatesForClass(CharacterClass.Armsman);
        var infiltratorTemplates = _service.GetTemplatesForClass(CharacterClass.Infiltrator);

        // Assert - Popular classes should have 2+ builds
        Assert.True(armsmanTemplates.Count >= 2);
        Assert.True(infiltratorTemplates.Count >= 2);
    }

    [Fact]
    public void GetTemplatesForClass_ReturnsEmptyForUnknown()
    {
        // Act
        var templates = _service.GetTemplatesForClass(CharacterClass.Unknown);

        // Assert
        Assert.Empty(templates);
    }

    [Fact]
    public void GetTemplatesForRealm_ReturnsAlbionTemplates()
    {
        // Act
        var templates = _service.GetTemplatesForRealm(Realm.Albion);

        // Assert
        Assert.NotEmpty(templates);
        Assert.All(templates, t => Assert.Equal(Realm.Albion, t.Realm));
    }

    [Fact]
    public void GetTemplatesForRealm_ReturnsMidgardTemplates()
    {
        // Act
        var templates = _service.GetTemplatesForRealm(Realm.Midgard);

        // Assert
        Assert.NotEmpty(templates);
        Assert.All(templates, t => Assert.Equal(Realm.Midgard, t.Realm));
    }

    [Fact]
    public void GetTemplatesForRealm_ReturnsHiberniaTemplates()
    {
        // Act
        var templates = _service.GetTemplatesForRealm(Realm.Hibernia);

        // Assert
        Assert.NotEmpty(templates);
        Assert.All(templates, t => Assert.Equal(Realm.Hibernia, t.Realm));
    }

    [Fact]
    public void GetTemplateById_ReturnsExistingTemplate()
    {
        // Arrange
        var templateId = "alb-arms-polearm";

        // Act
        var template = _service.GetTemplateById(templateId);

        // Assert
        Assert.NotNull(template);
        Assert.Equal(templateId, template!.Id);
        Assert.Equal(CharacterClass.Armsman, template.TargetClass);
    }

    [Fact]
    public void GetTemplateById_ReturnsNullForUnknownId()
    {
        // Act
        var template = _service.GetTemplateById("unknown-template-id");

        // Assert
        Assert.Null(template);
    }

    [Fact]
    public void GetTemplateById_IsCaseInsensitive()
    {
        // Act
        var template = _service.GetTemplateById("ALB-ARMS-POLEARM");

        // Assert
        Assert.NotNull(template);
    }

    [Fact]
    public void CreateBuildFromTemplate_CreatesBuildWithCorrectName()
    {
        // Arrange
        var template = _service.GetTemplateById("alb-arms-polearm")!;

        // Act
        var build = _service.CreateBuildFromTemplate(template);

        // Assert
        Assert.NotNull(build);
        Assert.Equal(template.Name, build.Name);
        Assert.NotEqual(Guid.Empty, build.Id);
    }

    [Fact]
    public void CreateBuildFromTemplate_CopiesSpecLines()
    {
        // Arrange
        var template = _service.GetTemplateById("alb-arms-polearm")!;

        // Act
        var build = _service.CreateBuildFromTemplate(template);

        // Assert
        Assert.NotEmpty(build.SpecLines);
        Assert.True(build.SpecLines.ContainsKey("Polearm"));
    }

    [Fact]
    public void CreateBuildFromTemplate_CopiesRealmAbilities()
    {
        // Arrange
        var template = _service.GetTemplateById("alb-arms-polearm")!;

        // Act
        var build = _service.CreateBuildFromTemplate(template);

        // Assert
        Assert.NotEmpty(build.RealmAbilities);
        Assert.Contains(build.RealmAbilities, ra => ra.AbilityName == "Determination");
    }

    [Fact]
    public void CreateBuildFromTemplate_SetsRealmRank()
    {
        // Arrange
        var template = _service.GetTemplateById("alb-arms-polearm")!;

        // Act
        var build = _service.CreateBuildFromTemplate(template);

        // Assert
        Assert.Equal(template.RecommendedRealmRank, build.RealmRank);
        Assert.True(build.RealmPoints > 0);
    }

    [Fact]
    public void SearchTemplates_FindsByName()
    {
        // Act
        var results = _service.SearchTemplates("Polearm");

        // Assert
        Assert.NotEmpty(results);
        Assert.Contains(results, t => t.Name.Contains("Polearm"));
    }

    [Fact]
    public void SearchTemplates_FindsByRole()
    {
        // Act
        var results = _service.SearchTemplates("tank");

        // Assert
        Assert.NotEmpty(results);
    }

    [Fact]
    public void SearchTemplates_FindsByTag()
    {
        // Act
        var results = _service.SearchTemplates("stealth");

        // Assert
        Assert.NotEmpty(results);
    }

    [Fact]
    public void SearchTemplates_FindsByClassName()
    {
        // Act
        var results = _service.SearchTemplates("infiltrator");

        // Assert
        Assert.NotEmpty(results);
        Assert.Contains(results, t => t.TargetClass == CharacterClass.Infiltrator);
    }

    [Fact]
    public void SearchTemplates_ReturnsAllForEmptyQuery()
    {
        // Act
        var allTemplates = _service.GetAllTemplates();
        var searchResults = _service.SearchTemplates("");

        // Assert
        Assert.Equal(allTemplates.Count, searchResults.Count);
    }

    [Fact]
    public void AllTemplates_HaveRequiredFields()
    {
        // Act
        var templates = _service.GetAllTemplates();

        // Assert
        foreach (var template in templates)
        {
            Assert.False(string.IsNullOrEmpty(template.Id));
            Assert.False(string.IsNullOrEmpty(template.Name));
            Assert.False(string.IsNullOrEmpty(template.Description));
            Assert.False(string.IsNullOrEmpty(template.Role));
            Assert.NotEmpty(template.SpecLines);
            Assert.NotEmpty(template.RealmAbilities);
            Assert.True(template.RecommendedRealmRank > 0);
        }
    }

    [Fact]
    public void AllTemplates_HaveUniqueIds()
    {
        // Act
        var templates = _service.GetAllTemplates();
        var ids = templates.Select(t => t.Id).ToList();
        var uniqueIds = ids.Distinct().ToList();

        // Assert
        Assert.Equal(ids.Count, uniqueIds.Count);
    }
}
