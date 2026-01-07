using CamelotCombatReporter.Core.CharacterBuilding.Models;
using CamelotCombatReporter.Core.Models;

namespace CamelotCombatReporter.Core.CharacterBuilding.Services;

/// <summary>
/// A pre-configured meta build template for a class.
/// </summary>
public record MetaBuildTemplate
{
    /// <summary>Unique identifier for the template.</summary>
    public required string Id { get; init; }
    
    /// <summary>Display name of the build.</summary>
    public required string Name { get; init; }
    
    /// <summary>Description of the build and playstyle.</summary>
    public required string Description { get; init; }
    
    /// <summary>Target class for this build.</summary>
    public required CharacterClass TargetClass { get; init; }
    
    /// <summary>Realm of the target class.</summary>
    public required Realm Realm { get; init; }
    
    /// <summary>Recommended minimum realm rank for this build.</summary>
    public int RecommendedRealmRank { get; init; } = 5;
    
    /// <summary>The playstyle or role this build fulfills.</summary>
    public required string Role { get; init; }
    
    /// <summary>Spec line allocations.</summary>
    public required IReadOnlyDictionary<string, int> SpecLines { get; init; }
    
    /// <summary>Recommended realm abilities.</summary>
    public required IReadOnlyList<RealmAbilitySelection> RealmAbilities { get; init; }
    
    /// <summary>Author of the template.</summary>
    public string Author { get; init; } = "Community";
    
    /// <summary>When the template was last updated.</summary>
    public DateTime LastUpdated { get; init; } = DateTime.UtcNow;
    
    /// <summary>Tags for searching/filtering.</summary>
    public IReadOnlyList<string> Tags { get; init; } = [];
}

/// <summary>
/// Provides community meta build templates for character classes.
/// </summary>
public interface IMetaBuildTemplateService
{
    /// <summary>
    /// Gets all available meta build templates.
    /// </summary>
    IReadOnlyList<MetaBuildTemplate> GetAllTemplates();
    
    /// <summary>
    /// Gets meta build templates for a specific class.
    /// </summary>
    IReadOnlyList<MetaBuildTemplate> GetTemplatesForClass(CharacterClass charClass);
    
    /// <summary>
    /// Gets meta build templates for a realm.
    /// </summary>
    IReadOnlyList<MetaBuildTemplate> GetTemplatesForRealm(Realm realm);
    
    /// <summary>
    /// Gets a template by its ID.
    /// </summary>
    MetaBuildTemplate? GetTemplateById(string templateId);
    
    /// <summary>
    /// Creates a CharacterBuild from a meta template.
    /// </summary>
    CharacterBuild CreateBuildFromTemplate(MetaBuildTemplate template);
    
    /// <summary>
    /// Searches templates by name or tags.
    /// </summary>
    IReadOnlyList<MetaBuildTemplate> SearchTemplates(string query);
}
