using CamelotCombatReporter.Core.CharacterBuilding.Models;
using CamelotCombatReporter.Core.CharacterBuilding.Templates;
using CamelotCombatReporter.Core.Models;

namespace CamelotCombatReporter.Core.CharacterBuilding.Services;

/// <summary>
/// Provides class-specific specialization templates and validation.
/// </summary>
public class SpecializationTemplateService : ISpecializationTemplateService
{
    private readonly Dictionary<CharacterClass, SpecializationTemplate> _templates;

    public SpecializationTemplateService()
    {
        _templates = InitializeTemplates();
    }

    public SpecializationTemplate GetTemplateForClass(CharacterClass charClass)
    {
        return _templates.TryGetValue(charClass, out var template)
            ? template
            : new SpecializationTemplate { Class = charClass, SpecLines = [] };
    }

    public int GetMaxSpecPoints(int level)
    {
        // DAoC formula: (level * 2) + (level / 2) + 1
        // At level 50: 100 + 25 + 1 = 126
        return (level * 2) + (level / 2) + 1;
    }

    public int GetAllocatedSpecPoints(CharacterBuild build, CharacterClass charClass)
    {
        if (build.SpecLines.Count == 0) return 0;

        var template = GetTemplateForClass(charClass);
        var specLookup = template.SpecLines.ToDictionary(s => s.Name, s => s);

        return build.SpecLines.Sum(kvp =>
        {
            var multiplier = specLookup.TryGetValue(kvp.Key, out var spec) ? spec.PointMultiplier : 1.0;
            return CalculateSpecPointCost(kvp.Value, multiplier);
        });
    }

    public int GetRemainingSpecPoints(CharacterBuild build, CharacterClass charClass, int level)
    {
        return GetMaxSpecPoints(level) - GetAllocatedSpecPoints(build, charClass);
    }

    public bool ValidateSpecAllocation(CharacterBuild build, CharacterClass charClass, int level)
    {
        return GetAllocatedSpecPoints(build, charClass) <= GetMaxSpecPoints(level);
    }

    public IReadOnlyList<CharacterClass> GetClassesForRealm(Realm realm)
    {
        return realm.GetClasses().ToList();
    }

    public int CalculateSpecPointCost(int specLevel, double multiplier = 1.0)
    {
        // Standard formula: sum of 1 to specLevel = n(n+1)/2
        var baseCost = specLevel * (specLevel + 1) / 2;
        return (int)(baseCost * multiplier);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Template Initialization
    // ─────────────────────────────────────────────────────────────────────────

    private static Dictionary<CharacterClass, SpecializationTemplate> InitializeTemplates()
    {
        var templates = new Dictionary<CharacterClass, SpecializationTemplate>();

        // Albion Classes
        InitializeAlbionTemplates(templates);
        
        // Midgard Classes
        InitializeMidgardTemplates(templates);
        
        // Hibernia Classes
        InitializeHiberniaTemplates(templates);

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
                new SpecLine { Name = "Smiting", Type = SpecLineType.Magic }
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
                new SpecLine { Name = "Flexible", Type = SpecLineType.Weapon }
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
                new SpecLine { Name = "Cold", Type = SpecLineType.Magic },
                new SpecLine { Name = "Wind", Type = SpecLineType.Magic }
            ]
        };

        templates[CharacterClass.Wizard] = new SpecializationTemplate
        {
            Class = CharacterClass.Wizard,
            SpecLines =
            [
                new SpecLine { Name = "Earth", Type = SpecLineType.Magic },
                new SpecLine { Name = "Cold", Type = SpecLineType.Magic },
                new SpecLine { Name = "Fire", Type = SpecLineType.Magic }
            ]
        };

        templates[CharacterClass.MaulerAlb] = new SpecializationTemplate
        {
            Class = CharacterClass.MaulerAlb,
            SpecLines =
            [
                new SpecLine { Name = "Fist", Type = SpecLineType.Weapon },
                new SpecLine { Name = "Aura Manipulation", Type = SpecLineType.Magic },
                new SpecLine { Name = "Magnetism", Type = SpecLineType.Magic },
                new SpecLine { Name = "Power Strikes", Type = SpecLineType.Weapon }
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
                new SpecLine { Name = "Subterranean", Type = SpecLineType.Magic }
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
                new SpecLine { Name = "Aura Manipulation", Type = SpecLineType.Magic },
                new SpecLine { Name = "Magnetism", Type = SpecLineType.Magic },
                new SpecLine { Name = "Power Strikes", Type = SpecLineType.Weapon }
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
                new SpecLine { Name = "Fist", Type = SpecLineType.Weapon },
                new SpecLine { Name = "Aura Manipulation", Type = SpecLineType.Magic },
                new SpecLine { Name = "Magnetism", Type = SpecLineType.Magic },
                new SpecLine { Name = "Power Strikes", Type = SpecLineType.Weapon }
            ]
        };
    }
}
