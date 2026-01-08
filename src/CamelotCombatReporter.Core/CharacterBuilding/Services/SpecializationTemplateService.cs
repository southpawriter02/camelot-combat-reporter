using CamelotCombatReporter.Core.CharacterBuilding.Models;
using CamelotCombatReporter.Core.CharacterBuilding.Templates;
using CamelotCombatReporter.Core.Models;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace CamelotCombatReporter.Core.CharacterBuilding.Services;

/// <summary>
/// Provides class-specific specialization templates and validation.
/// </summary>
/// <remarks>
/// <para>
/// This service is the authoritative source for class specialization data,
/// providing templates that define which spec lines are available for each
/// of the 48 character classes across all three realms.
/// </para>
/// <para>
/// Key features include:
/// </para>
/// <list type="bullet">
///   <item><description>Spec line definitions per class (weapon, magic, utility types)</description></item>
///   <item><description>Point cost calculations using DAoC's triangular formula</description></item>
///   <item><description>Validation of spec point allocations against level caps</description></item>
///   <item><description>Realm-to-class mappings for UI filtering</description></item>
/// </list>
/// <para>
/// The DAoC spec point formula is: cost = level × (level + 1) ÷ 2 × multiplier.
/// Standard level 50 characters have 126 spec points to allocate.
/// </para>
/// </remarks>
public class SpecializationTemplateService : ISpecializationTemplateService
{
    private readonly ILogger<SpecializationTemplateService> _logger;
    // Pre-loaded templates indexed by class for O(1) lookup
    private readonly Dictionary<CharacterClass, SpecializationTemplate> _templates;

    /// <summary>
    /// Initializes the service with all class templates pre-loaded.
    /// </summary>
    public SpecializationTemplateService(ILogger<SpecializationTemplateService>? logger = null)
    {
        _logger = logger ?? NullLogger<SpecializationTemplateService>.Instance;
        _templates = InitializeTemplates();
        _logger.LogInformation("Initialized {Count} specialization templates", _templates.Count);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Returns an empty template if the class is not found (defensive).
    /// </remarks>
    public SpecializationTemplate GetTemplateForClass(CharacterClass charClass)
    {
        if (_templates.TryGetValue(charClass, out var template))
        {
            return template;
        }

        _logger.LogWarning("Template requested for unknown or uninitialized class: {Class}", charClass);
        return new SpecializationTemplate { Class = charClass, SpecLines = [] };
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Uses the DAoC formula: (level × 2) + (level ÷ 2) + 1.
    /// At level 50: 100 + 25 + 1 = 126 total spec points.
    /// </remarks>
    public int GetMaxSpecPoints(int level)
    {
        // DAoC spec point formula
        return (level * 2) + (level / 2) + 1;
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Accounts for per-spec multipliers (e.g., some hybrid specs cost 1.5x).
    /// </remarks>
    public int GetAllocatedSpecPoints(CharacterBuild build, CharacterClass charClass)
    {
        if (build.SpecLines.Count == 0) return 0;

        var template = GetTemplateForClass(charClass);
        var specLookup = template.SpecLines.ToDictionary(s => s.Name, s => s);

        // Sum cost of each allocated spec line
        return build.SpecLines.Sum(kvp =>
        {
            if (!specLookup.TryGetValue(kvp.Key, out var spec))
            {
                _logger.LogWarning("Build contains unknown spec line '{SpecLine}' for class {Class}", kvp.Key, charClass);
                // Assume 1.0 multiplier if unknown
                return CalculateSpecPointCost(kvp.Value, 1.0);
            }
            return CalculateSpecPointCost(kvp.Value, spec.PointMultiplier);
        });
    }

    /// <inheritdoc/>
    public int GetRemainingSpecPoints(CharacterBuild build, CharacterClass charClass, int level)
    {
        return GetMaxSpecPoints(level) - GetAllocatedSpecPoints(build, charClass);
    }

    /// <inheritdoc/>
    public bool ValidateSpecAllocation(CharacterBuild build, CharacterClass charClass, int level)
    {
        var allocated = GetAllocatedSpecPoints(build, charClass);
        var max = GetMaxSpecPoints(level);
        
        if (allocated > max)
        {
            _logger.LogDebug("Spec validation failed for {Class}: {Allocated}/{Max} points used", charClass, allocated, max);
            return false;
        }
        
        return true;
    }

    /// <inheritdoc/>
    public IReadOnlyList<CharacterClass> GetClassesForRealm(Realm realm)
    {
        return realm.GetClasses().ToList();
    }

    /// <inheritdoc/>
    /// <remarks>
    /// The triangular number formula: n(n+1)/2 gives the base cost.
    /// This is then multiplied by the spec line's cost multiplier.
    /// </remarks>
    public int CalculateSpecPointCost(int specLevel, double multiplier = 1.0)
    {
        // Triangular number formula: sum of 1 to specLevel
        var baseCost = specLevel * (specLevel + 1) / 2;
        return (int)(baseCost * multiplier);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Template Initialization
    // ─────────────────────────────────────────────────────────────────────────
    // Templates are organized by realm for maintainability.
    // Each class has its available spec lines defined with type and multiplier.

    private static Dictionary<CharacterClass, SpecializationTemplate> InitializeTemplates()
    {
        var templates = new Dictionary<CharacterClass, SpecializationTemplate>();

        // Initialize all 48 classes across 3 realms
        InitializeAlbionTemplates(templates);   // 16 Albion classes
        InitializeMidgardTemplates(templates);  // 16 Midgard classes
        InitializeHiberniaTemplates(templates); // 16 Hibernia classes

        return templates;
    }


    private static void InitializeAlbionTemplates(Dictionary<CharacterClass, SpecializationTemplate> templates)
    {
        templates[CharacterClass.Armsman] = new SpecializationTemplate
        {
            Class = CharacterClass.Armsman,
            SpecLines =
            [
                new SpecLine { Name = "Crush", Type = SpecLineType.Weapon },
                new SpecLine { Name = "Slash", Type = SpecLineType.Weapon },
                new SpecLine { Name = "Thrust", Type = SpecLineType.Weapon },
                new SpecLine { Name = "Polearm", Type = SpecLineType.Weapon },
                new SpecLine { Name = "Two-Handed", Type = SpecLineType.Weapon },
                new SpecLine { Name = "Shield", Type = SpecLineType.Utility },
                new SpecLine { Name = "Parry", Type = SpecLineType.Utility },
                new SpecLine { Name = "Crossbow", Type = SpecLineType.Weapon }
            ]
        };

        templates[CharacterClass.Cabalist] = new SpecializationTemplate
        {
            Class = CharacterClass.Cabalist,
            SpecLines =
            [
                new SpecLine { Name = "Body", Type = SpecLineType.Magic },
                new SpecLine { Name = "Matter", Type = SpecLineType.Magic },
                new SpecLine { Name = "Spirit", Type = SpecLineType.Magic }
            ]
        };

        templates[CharacterClass.Cleric] = new SpecializationTemplate
        {
            Class = CharacterClass.Cleric,
            SpecLines =
            [
                new SpecLine { Name = "Rejuvenation", Type = SpecLineType.Magic },
                new SpecLine { Name = "Enhancement", Type = SpecLineType.Magic },
                new SpecLine { Name = "Smite", Type = SpecLineType.Magic }
            ]
        };

        templates[CharacterClass.Friar] = new SpecializationTemplate
        {
            Class = CharacterClass.Friar,
            SpecLines =
            [
                new SpecLine { Name = "Rejuvenation", Type = SpecLineType.Magic },
                new SpecLine { Name = "Enhancement", Type = SpecLineType.Magic },
                new SpecLine { Name = "Staff", Type = SpecLineType.Weapon },
                new SpecLine { Name = "Parry", Type = SpecLineType.Utility }
            ]
        };

        templates[CharacterClass.Heretic] = new SpecializationTemplate
        {
            Class = CharacterClass.Heretic,
            SpecLines =
            [
                new SpecLine { Name = "Rejuvenation", Type = SpecLineType.Magic },
                new SpecLine { Name = "Enhancement", Type = SpecLineType.Magic },
                new SpecLine { Name = "Crush", Type = SpecLineType.Weapon },
                new SpecLine { Name = "Flexible", Type = SpecLineType.Weapon },
                new SpecLine { Name = "Shield", Type = SpecLineType.Utility }
            ]
        };

        templates[CharacterClass.Infiltrator] = new SpecializationTemplate
        {
            Class = CharacterClass.Infiltrator,
            SpecLines =
            [
                new SpecLine { Name = "Slash", Type = SpecLineType.Weapon },
                new SpecLine { Name = "Thrust", Type = SpecLineType.Weapon },
                new SpecLine { Name = "Dual Wield", Type = SpecLineType.Weapon },
                new SpecLine { Name = "Critical Strike", Type = SpecLineType.Weapon },
                new SpecLine { Name = "Stealth", Type = SpecLineType.Utility },
                new SpecLine { Name = "Envenom", Type = SpecLineType.Utility }
            ]
        };

        templates[CharacterClass.Mercenary] = new SpecializationTemplate
        {
            Class = CharacterClass.Mercenary,
            SpecLines =
            [
                new SpecLine { Name = "Slash", Type = SpecLineType.Weapon },
                new SpecLine { Name = "Thrust", Type = SpecLineType.Weapon },
                new SpecLine { Name = "Crush", Type = SpecLineType.Weapon },
                new SpecLine { Name = "Dual Wield", Type = SpecLineType.Weapon },
                new SpecLine { Name = "Parry", Type = SpecLineType.Utility },
                new SpecLine { Name = "Shield", Type = SpecLineType.Utility }
            ]
        };

        templates[CharacterClass.Minstrel] = new SpecializationTemplate
        {
            Class = CharacterClass.Minstrel,
            SpecLines =
            [
                new SpecLine { Name = "Instruments", Type = SpecLineType.Magic },
                new SpecLine { Name = "Slash", Type = SpecLineType.Weapon },
                new SpecLine { Name = "Thrust", Type = SpecLineType.Weapon },
                new SpecLine { Name = "Stealth", Type = SpecLineType.Utility }
            ]
        };

        templates[CharacterClass.Necromancer] = new SpecializationTemplate
        {
            Class = CharacterClass.Necromancer,
            SpecLines =
            [
                new SpecLine { Name = "Deathsight", Type = SpecLineType.Magic },
                new SpecLine { Name = "Painworking", Type = SpecLineType.Magic },
                new SpecLine { Name = "Death Servant", Type = SpecLineType.Magic }
            ]
        };

        templates[CharacterClass.Paladin] = new SpecializationTemplate
        {
            Class = CharacterClass.Paladin,
            SpecLines =
            [
                new SpecLine { Name = "Slash", Type = SpecLineType.Weapon },
                new SpecLine { Name = "Thrust", Type = SpecLineType.Weapon },
                new SpecLine { Name = "Crush", Type = SpecLineType.Weapon },
                new SpecLine { Name = "Two-Handed", Type = SpecLineType.Weapon },
                new SpecLine { Name = "Shield", Type = SpecLineType.Utility },
                new SpecLine { Name = "Parry", Type = SpecLineType.Utility },
                new SpecLine { Name = "Chants", Type = SpecLineType.Magic }
            ]
        };

        templates[CharacterClass.Reaver] = new SpecializationTemplate
        {
            Class = CharacterClass.Reaver,
            SpecLines =
            [
                new SpecLine { Name = "Slash", Type = SpecLineType.Weapon },
                new SpecLine { Name = "Thrust", Type = SpecLineType.Weapon },
                new SpecLine { Name = "Crush", Type = SpecLineType.Weapon },
                new SpecLine { Name = "Flexible", Type = SpecLineType.Weapon },
                new SpecLine { Name = "Soulrending", Type = SpecLineType.Magic },
                new SpecLine { Name = "Parry", Type = SpecLineType.Utility },
                new SpecLine { Name = "Shield", Type = SpecLineType.Utility }
            ]
        };

        templates[CharacterClass.Scout] = new SpecializationTemplate
        {
            Class = CharacterClass.Scout,
            SpecLines =
            [
                new SpecLine { Name = "Slash", Type = SpecLineType.Weapon },
                new SpecLine { Name = "Thrust", Type = SpecLineType.Weapon },
                new SpecLine { Name = "Shield", Type = SpecLineType.Utility },
                new SpecLine { Name = "Stealth", Type = SpecLineType.Utility },
                new SpecLine { Name = "Longbow", Type = SpecLineType.Weapon }
            ]
        };

        templates[CharacterClass.Sorcerer] = new SpecializationTemplate
        {
            Class = CharacterClass.Sorcerer,
            SpecLines =
            [
                new SpecLine { Name = "Body", Type = SpecLineType.Magic },
                new SpecLine { Name = "Matter", Type = SpecLineType.Magic },
                new SpecLine { Name = "Mind", Type = SpecLineType.Magic }
            ]
        };

        templates[CharacterClass.Theurgist] = new SpecializationTemplate
        {
            Class = CharacterClass.Theurgist,
            SpecLines =
            [
                new SpecLine { Name = "Earth", Type = SpecLineType.Magic },
                new SpecLine { Name = "Ice", Type = SpecLineType.Magic },
                new SpecLine { Name = "Wind", Type = SpecLineType.Magic }
            ]
        };

        templates[CharacterClass.Wizard] = new SpecializationTemplate
        {
            Class = CharacterClass.Wizard,
            SpecLines =
            [
                new SpecLine { Name = "Earth", Type = SpecLineType.Magic },
                new SpecLine { Name = "Ice", Type = SpecLineType.Magic },
                new SpecLine { Name = "Fire", Type = SpecLineType.Magic }
            ]
        };

        templates[CharacterClass.MaulerAlb] = new SpecializationTemplate
        {
            Class = CharacterClass.MaulerAlb,
            SpecLines =
            [
                new SpecLine { Name = "Fist Wraps", Type = SpecLineType.Weapon },
                new SpecLine { Name = "Mauler Staff", Type = SpecLineType.Weapon },
                new SpecLine { Name = "Aura Manipulation", Type = SpecLineType.Magic },
                new SpecLine { Name = "Magnetism", Type = SpecLineType.Magic },
                new SpecLine { Name = "Power Strikes", Type = SpecLineType.Magic }
            ]
        };
    }

    private static void InitializeMidgardTemplates(Dictionary<CharacterClass, SpecializationTemplate> templates)
    {
        templates[CharacterClass.Berserker] = new SpecializationTemplate
        {
            Class = CharacterClass.Berserker,
            SpecLines =
            [
                new SpecLine { Name = "Axe", Type = SpecLineType.Weapon },
                new SpecLine { Name = "Hammer", Type = SpecLineType.Weapon },
                new SpecLine { Name = "Sword", Type = SpecLineType.Weapon },
                new SpecLine { Name = "Left Axe", Type = SpecLineType.Weapon },
                new SpecLine { Name = "Parry", Type = SpecLineType.Utility }
            ]
        };

        templates[CharacterClass.Bonedancer] = new SpecializationTemplate
        {
            Class = CharacterClass.Bonedancer,
            SpecLines =
            [
                new SpecLine { Name = "Darkness", Type = SpecLineType.Magic },
                new SpecLine { Name = "Suppression", Type = SpecLineType.Magic },
                new SpecLine { Name = "Bone Army", Type = SpecLineType.Magic }
            ]
        };

        templates[CharacterClass.Healer] = new SpecializationTemplate
        {
            Class = CharacterClass.Healer,
            SpecLines =
            [
                new SpecLine { Name = "Mending", Type = SpecLineType.Magic },
                new SpecLine { Name = "Augmentation", Type = SpecLineType.Magic },
                new SpecLine { Name = "Pacification", Type = SpecLineType.Magic }
            ]
        };

        templates[CharacterClass.Hunter] = new SpecializationTemplate
        {
            Class = CharacterClass.Hunter,
            SpecLines =
            [
                new SpecLine { Name = "Sword", Type = SpecLineType.Weapon },
                new SpecLine { Name = "Spear", Type = SpecLineType.Weapon },
                new SpecLine { Name = "Stealth", Type = SpecLineType.Utility },
                new SpecLine { Name = "Composite Bow", Type = SpecLineType.Weapon },
                new SpecLine { Name = "Beastcraft", Type = SpecLineType.Magic }
            ]
        };

        templates[CharacterClass.Runemaster] = new SpecializationTemplate
        {
            Class = CharacterClass.Runemaster,
            SpecLines =
            [
                new SpecLine { Name = "Darkness", Type = SpecLineType.Magic },
                new SpecLine { Name = "Suppression", Type = SpecLineType.Magic },
                new SpecLine { Name = "Runecarving", Type = SpecLineType.Magic }
            ]
        };

        templates[CharacterClass.Savage] = new SpecializationTemplate
        {
            Class = CharacterClass.Savage,
            SpecLines =
            [
                new SpecLine { Name = "Sword", Type = SpecLineType.Weapon },
                new SpecLine { Name = "Axe", Type = SpecLineType.Weapon },
                new SpecLine { Name = "Hammer", Type = SpecLineType.Weapon },
                new SpecLine { Name = "Hand to Hand", Type = SpecLineType.Weapon },
                new SpecLine { Name = "Savagery", Type = SpecLineType.Hybrid },
                new SpecLine { Name = "Parry", Type = SpecLineType.Utility }
            ]
        };

        templates[CharacterClass.Shadowblade] = new SpecializationTemplate
        {
            Class = CharacterClass.Shadowblade,
            SpecLines =
            [
                new SpecLine { Name = "Sword", Type = SpecLineType.Weapon },
                new SpecLine { Name = "Axe", Type = SpecLineType.Weapon },
                new SpecLine { Name = "Left Axe", Type = SpecLineType.Weapon },
                new SpecLine { Name = "Critical Strike", Type = SpecLineType.Weapon },
                new SpecLine { Name = "Stealth", Type = SpecLineType.Utility },
                new SpecLine { Name = "Envenom", Type = SpecLineType.Utility }
            ]
        };

        templates[CharacterClass.Shaman] = new SpecializationTemplate
        {
            Class = CharacterClass.Shaman,
            SpecLines =
            [
                new SpecLine { Name = "Mending", Type = SpecLineType.Magic },
                new SpecLine { Name = "Augmentation", Type = SpecLineType.Magic },
                new SpecLine { Name = "Cave Magic", Type = SpecLineType.Magic }
            ]
        };

        templates[CharacterClass.Skald] = new SpecializationTemplate
        {
            Class = CharacterClass.Skald,
            SpecLines =
            [
                new SpecLine { Name = "Sword", Type = SpecLineType.Weapon },
                new SpecLine { Name = "Axe", Type = SpecLineType.Weapon },
                new SpecLine { Name = "Hammer", Type = SpecLineType.Weapon },
                new SpecLine { Name = "Battlesongs", Type = SpecLineType.Magic },
                new SpecLine { Name = "Parry", Type = SpecLineType.Utility }
            ]
        };

        templates[CharacterClass.Spiritmaster] = new SpecializationTemplate
        {
            Class = CharacterClass.Spiritmaster,
            SpecLines =
            [
                new SpecLine { Name = "Darkness", Type = SpecLineType.Magic },
                new SpecLine { Name = "Suppression", Type = SpecLineType.Magic },
                new SpecLine { Name = "Summoning", Type = SpecLineType.Magic }
            ]
        };

        templates[CharacterClass.Thane] = new SpecializationTemplate
        {
            Class = CharacterClass.Thane,
            SpecLines =
            [
                new SpecLine { Name = "Sword", Type = SpecLineType.Weapon },
                new SpecLine { Name = "Axe", Type = SpecLineType.Weapon },
                new SpecLine { Name = "Hammer", Type = SpecLineType.Weapon },
                new SpecLine { Name = "Shield", Type = SpecLineType.Utility },
                new SpecLine { Name = "Parry", Type = SpecLineType.Utility },
                new SpecLine { Name = "Stormcalling", Type = SpecLineType.Magic }
            ]
        };

        templates[CharacterClass.Valkyrie] = new SpecializationTemplate
        {
            Class = CharacterClass.Valkyrie,
            SpecLines =
            [
                new SpecLine { Name = "Sword", Type = SpecLineType.Weapon },
                new SpecLine { Name = "Spear", Type = SpecLineType.Weapon },
                new SpecLine { Name = "Shield", Type = SpecLineType.Utility },
                new SpecLine { Name = "Parry", Type = SpecLineType.Utility },
                new SpecLine { Name = "Mending", Type = SpecLineType.Magic },
                new SpecLine { Name = "Odin's Will", Type = SpecLineType.Magic }
            ]
        };

        templates[CharacterClass.Warlock] = new SpecializationTemplate
        {
            Class = CharacterClass.Warlock,
            SpecLines =
            [
                new SpecLine { Name = "Cursing", Type = SpecLineType.Magic },
                new SpecLine { Name = "Hexing", Type = SpecLineType.Magic },
                new SpecLine { Name = "Witchcraft", Type = SpecLineType.Magic }
            ]
        };

        templates[CharacterClass.Warrior] = new SpecializationTemplate
        {
            Class = CharacterClass.Warrior,
            SpecLines =
            [
                new SpecLine { Name = "Sword", Type = SpecLineType.Weapon },
                new SpecLine { Name = "Axe", Type = SpecLineType.Weapon },
                new SpecLine { Name = "Hammer", Type = SpecLineType.Weapon },
                new SpecLine { Name = "Shield", Type = SpecLineType.Utility },
                new SpecLine { Name = "Parry", Type = SpecLineType.Utility },
                new SpecLine { Name = "Thrown Weapons", Type = SpecLineType.Weapon }
            ]
        };

        templates[CharacterClass.MaulerMid] = new SpecializationTemplate
        {
            Class = CharacterClass.MaulerMid,
            SpecLines =
            [
                new SpecLine { Name = "Fist", Type = SpecLineType.Weapon },
                new SpecLine { Name = "Mauler Staff", Type = SpecLineType.Weapon },
                new SpecLine { Name = "Aura Manipulation", Type = SpecLineType.Magic },
                new SpecLine { Name = "Magnetism", Type = SpecLineType.Magic },
                new SpecLine { Name = "Power Strikes", Type = SpecLineType.Magic }
            ]
        };
    }

    private static void InitializeHiberniaTemplates(Dictionary<CharacterClass, SpecializationTemplate> templates)
    {
        templates[CharacterClass.Animist] = new SpecializationTemplate
        {
            Class = CharacterClass.Animist,
            SpecLines =
            [
                new SpecLine { Name = "Arboreal Path", Type = SpecLineType.Magic },
                new SpecLine { Name = "Creeping Path", Type = SpecLineType.Magic },
                new SpecLine { Name = "Verdant Path", Type = SpecLineType.Magic }
            ]
        };

        templates[CharacterClass.Bainshee] = new SpecializationTemplate
        {
            Class = CharacterClass.Bainshee,
            SpecLines =
            [
                new SpecLine { Name = "Ethereal Shriek", Type = SpecLineType.Magic },
                new SpecLine { Name = "Phantasmal Wail", Type = SpecLineType.Magic },
                new SpecLine { Name = "Spectral Guard", Type = SpecLineType.Magic }
            ]
        };

        templates[CharacterClass.Bard] = new SpecializationTemplate
        {
            Class = CharacterClass.Bard,
            SpecLines =
            [
                new SpecLine { Name = "Regrowth", Type = SpecLineType.Magic },
                new SpecLine { Name = "Nurture", Type = SpecLineType.Magic },
                new SpecLine { Name = "Music", Type = SpecLineType.Magic },
                new SpecLine { Name = "Blades", Type = SpecLineType.Weapon },
                new SpecLine { Name = "Blunt", Type = SpecLineType.Weapon }
            ]
        };

        templates[CharacterClass.Blademaster] = new SpecializationTemplate
        {
            Class = CharacterClass.Blademaster,
            SpecLines =
            [
                new SpecLine { Name = "Blades", Type = SpecLineType.Weapon },
                new SpecLine { Name = "Blunt", Type = SpecLineType.Weapon },
                new SpecLine { Name = "Piercing", Type = SpecLineType.Weapon },
                new SpecLine { Name = "Celtic Dual", Type = SpecLineType.Weapon },
                new SpecLine { Name = "Parry", Type = SpecLineType.Utility },
                new SpecLine { Name = "Shield", Type = SpecLineType.Utility }
            ]
        };

        templates[CharacterClass.Champion] = new SpecializationTemplate
        {
            Class = CharacterClass.Champion,
            SpecLines =
            [
                new SpecLine { Name = "Blades", Type = SpecLineType.Weapon },
                new SpecLine { Name = "Blunt", Type = SpecLineType.Weapon },
                new SpecLine { Name = "Piercing", Type = SpecLineType.Weapon },
                new SpecLine { Name = "Large Weapons", Type = SpecLineType.Weapon },
                new SpecLine { Name = "Shield", Type = SpecLineType.Utility },
                new SpecLine { Name = "Parry", Type = SpecLineType.Utility },
                new SpecLine { Name = "Valor", Type = SpecLineType.Magic }
            ]
        };

        templates[CharacterClass.Druid] = new SpecializationTemplate
        {
            Class = CharacterClass.Druid,
            SpecLines =
            [
                new SpecLine { Name = "Regrowth", Type = SpecLineType.Magic },
                new SpecLine { Name = "Nurture", Type = SpecLineType.Magic },
                new SpecLine { Name = "Nature", Type = SpecLineType.Magic }
            ]
        };

        templates[CharacterClass.Eldritch] = new SpecializationTemplate
        {
            Class = CharacterClass.Eldritch,
            SpecLines =
            [
                new SpecLine { Name = "Light", Type = SpecLineType.Magic },
                new SpecLine { Name = "Mana", Type = SpecLineType.Magic },
                new SpecLine { Name = "Void", Type = SpecLineType.Magic }
            ]
        };

        templates[CharacterClass.Enchanter] = new SpecializationTemplate
        {
            Class = CharacterClass.Enchanter,
            SpecLines =
            [
                new SpecLine { Name = "Light", Type = SpecLineType.Magic },
                new SpecLine { Name = "Mana", Type = SpecLineType.Magic },
                new SpecLine { Name = "Enchantments", Type = SpecLineType.Magic }
            ]
        };

        templates[CharacterClass.Hero] = new SpecializationTemplate
        {
            Class = CharacterClass.Hero,
            SpecLines =
            [
                new SpecLine { Name = "Blades", Type = SpecLineType.Weapon },
                new SpecLine { Name = "Blunt", Type = SpecLineType.Weapon },
                new SpecLine { Name = "Piercing", Type = SpecLineType.Weapon },
                new SpecLine { Name = "Large Weapons", Type = SpecLineType.Weapon },
                new SpecLine { Name = "Celtic Spear", Type = SpecLineType.Weapon },
                new SpecLine { Name = "Shield", Type = SpecLineType.Utility },
                new SpecLine { Name = "Parry", Type = SpecLineType.Utility }
            ]
        };

        templates[CharacterClass.Mentalist] = new SpecializationTemplate
        {
            Class = CharacterClass.Mentalist,
            SpecLines =
            [
                new SpecLine { Name = "Light", Type = SpecLineType.Magic },
                new SpecLine { Name = "Mana", Type = SpecLineType.Magic },
                new SpecLine { Name = "Mentalism", Type = SpecLineType.Magic }
            ]
        };

        templates[CharacterClass.Nightshade] = new SpecializationTemplate
        {
            Class = CharacterClass.Nightshade,
            SpecLines =
            [
                new SpecLine { Name = "Blades", Type = SpecLineType.Weapon },
                new SpecLine { Name = "Piercing", Type = SpecLineType.Weapon },
                new SpecLine { Name = "Celtic Dual", Type = SpecLineType.Weapon },
                new SpecLine { Name = "Critical Strike", Type = SpecLineType.Weapon },
                new SpecLine { Name = "Stealth", Type = SpecLineType.Utility },
                new SpecLine { Name = "Envenom", Type = SpecLineType.Utility }
            ]
        };

        templates[CharacterClass.Ranger] = new SpecializationTemplate
        {
            Class = CharacterClass.Ranger,
            SpecLines =
            [
                new SpecLine { Name = "Blades", Type = SpecLineType.Weapon },
                new SpecLine { Name = "Piercing", Type = SpecLineType.Weapon },
                new SpecLine { Name = "Celtic Dual", Type = SpecLineType.Weapon },
                new SpecLine { Name = "Recurve Bow", Type = SpecLineType.Weapon },
                new SpecLine { Name = "Stealth", Type = SpecLineType.Utility },
                new SpecLine { Name = "Pathfinding", Type = SpecLineType.Magic }
            ]
        };

        templates[CharacterClass.Valewalker] = new SpecializationTemplate
        {
            Class = CharacterClass.Valewalker,
            SpecLines =
            [
                new SpecLine { Name = "Arboreal Path", Type = SpecLineType.Magic },
                new SpecLine { Name = "Parry", Type = SpecLineType.Utility },
                new SpecLine { Name = "Scythe", Type = SpecLineType.Weapon }
            ]
        };

        templates[CharacterClass.Vampiir] = new SpecializationTemplate
        {
            Class = CharacterClass.Vampiir,
            SpecLines =
            [
                new SpecLine { Name = "Dementia", Type = SpecLineType.Magic },
                new SpecLine { Name = "Shadow Mastery", Type = SpecLineType.Magic },
                new SpecLine { Name = "Vampiiric Embrace", Type = SpecLineType.Magic },
                new SpecLine { Name = "Piercing", Type = SpecLineType.Weapon }
            ]
        };

        templates[CharacterClass.Warden] = new SpecializationTemplate
        {
            Class = CharacterClass.Warden,
            SpecLines =
            [
                new SpecLine { Name = "Regrowth", Type = SpecLineType.Magic },
                new SpecLine { Name = "Nurture", Type = SpecLineType.Magic },
                new SpecLine { Name = "Blades", Type = SpecLineType.Weapon },
                new SpecLine { Name = "Blunt", Type = SpecLineType.Weapon },
                new SpecLine { Name = "Shield", Type = SpecLineType.Utility },
                new SpecLine { Name = "Parry", Type = SpecLineType.Utility }
            ]
        };

        templates[CharacterClass.MaulerHib] = new SpecializationTemplate
        {
            Class = CharacterClass.MaulerHib,
            SpecLines =
            [
                new SpecLine { Name = "Fist Wraps", Type = SpecLineType.Weapon },
                new SpecLine { Name = "Mauler Staff", Type = SpecLineType.Weapon },
                new SpecLine { Name = "Aura Manipulation", Type = SpecLineType.Magic },
                new SpecLine { Name = "Magnetism", Type = SpecLineType.Magic },
                new SpecLine { Name = "Power Strikes", Type = SpecLineType.Magic }
            ]
        };
    }
}
