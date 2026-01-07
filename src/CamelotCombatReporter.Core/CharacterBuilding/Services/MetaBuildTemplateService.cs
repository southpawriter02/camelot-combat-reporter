using CamelotCombatReporter.Core.CharacterBuilding.Models;
using CamelotCombatReporter.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace CamelotCombatReporter.Core.CharacterBuilding.Services;

/// <summary>
/// Provides community meta build templates for character classes.
/// Templates are embedded in code for easy distribution.
/// </summary>
public class MetaBuildTemplateService : IMetaBuildTemplateService
{
    private readonly ILogger<MetaBuildTemplateService> _logger;
    private readonly List<MetaBuildTemplate> _templates;
    private readonly Dictionary<CharacterClass, List<MetaBuildTemplate>> _byClass;
    private readonly Dictionary<Realm, List<MetaBuildTemplate>> _byRealm;
    private readonly Dictionary<string, MetaBuildTemplate> _byId;

    public MetaBuildTemplateService(ILogger<MetaBuildTemplateService>? logger = null)
    {
        _logger = logger ?? NullLogger<MetaBuildTemplateService>.Instance;
        _templates = InitializeTemplates();
        
        // Build indexes
        _byClass = _templates
            .GroupBy(t => t.TargetClass)
            .ToDictionary(g => g.Key, g => g.ToList());
            
        _byRealm = _templates
            .GroupBy(t => t.Realm)
            .ToDictionary(g => g.Key, g => g.ToList());
            
        _byId = _templates.ToDictionary(t => t.Id, StringComparer.OrdinalIgnoreCase);
        
        _logger.LogInformation("Loaded {Count} meta build templates", _templates.Count);
    }

    /// <inheritdoc/>
    public IReadOnlyList<MetaBuildTemplate> GetAllTemplates() => _templates;

    /// <inheritdoc/>
    public IReadOnlyList<MetaBuildTemplate> GetTemplatesForClass(CharacterClass charClass)
    {
        return _byClass.TryGetValue(charClass, out var templates) ? templates : [];
    }

    /// <inheritdoc/>
    public IReadOnlyList<MetaBuildTemplate> GetTemplatesForRealm(Realm realm)
    {
        return _byRealm.TryGetValue(realm, out var templates) ? templates : [];
    }

    /// <inheritdoc/>
    public MetaBuildTemplate? GetTemplateById(string templateId)
    {
        return _byId.TryGetValue(templateId, out var template) ? template : null;
    }

    /// <inheritdoc/>
    public CharacterBuild CreateBuildFromTemplate(MetaBuildTemplate template)
    {
        return new CharacterBuild
        {
            Id = Guid.NewGuid(),
            Name = template.Name,
            Notes = $"Created from template: {template.Name}\n{template.Description}",
            RealmRank = template.RecommendedRealmRank,
            RealmPoints = CalculateRealmPoints(template.RecommendedRealmRank),
            SpecLines = new Dictionary<string, int>(template.SpecLines),
            RealmAbilities = template.RealmAbilities.ToList(),
            CreatedUtc = DateTime.UtcNow
        };
    }

    /// <inheritdoc/>
    public IReadOnlyList<MetaBuildTemplate> SearchTemplates(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return _templates;
            
        var searchTerms = query.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        
        return _templates
            .Where(t => searchTerms.All(term =>
                t.Name.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                t.Description.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                t.Role.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                t.Tags.Any(tag => tag.Contains(term, StringComparison.OrdinalIgnoreCase)) ||
                t.TargetClass.ToString().Contains(term, StringComparison.OrdinalIgnoreCase)))
            .ToList();
    }

    private static long CalculateRealmPoints(int realmRank)
    {
        // Approximate RP requirements per RR
        return realmRank switch
        {
            1 => 0,
            2 => 25_000,
            3 => 125_000,
            4 => 350_000,
            5 => 750_000,
            6 => 1_375_000,
            7 => 2_275_000,
            8 => 3_500_000,
            9 => 5_100_000,
            10 => 7_125_000,
            11 => 9_625_000,
            12 => 12_650_000,
            13 => 16_250_000,
            _ => 0
        };
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Template Initialization
    // ─────────────────────────────────────────────────────────────────────────

    private static List<MetaBuildTemplate> InitializeTemplates()
    {
        var templates = new List<MetaBuildTemplate>();
        
        // Add all realm templates
        templates.AddRange(CreateAlbionTemplates());
        templates.AddRange(CreateMidgardTemplates());
        templates.AddRange(CreateHiberniaTemplates());
        
        return templates;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // ALBION TEMPLATES
    // ═══════════════════════════════════════════════════════════════════════

    private static IEnumerable<MetaBuildTemplate> CreateAlbionTemplates()
    {
        // ─── Armsman ─────────────────────────────────────────────────────────
        yield return new MetaBuildTemplate
        {
            Id = "alb-arms-polearm",
            Name = "Polearm Tank",
            Description = "High damage polearm spec with excellent reach and positional styles. Great for group play.",
            TargetClass = CharacterClass.Armsman,
            Realm = Realm.Albion,
            Role = "Melee DPS / Tank",
            RecommendedRealmRank = 6,
            SpecLines = new Dictionary<string, int>
            {
                { "Polearm", 50 },
                { "Shield", 42 },
                { "Parry", 35 }
            },
            RealmAbilities = CreateRAs(("Determination", 5), ("Purge", 3), ("Ignore Pain", 3), ("Mastery of Pain", 3)),
            Tags = ["tank", "damage", "polearm", "group"]
        };
        
        yield return new MetaBuildTemplate
        {
            Id = "alb-arms-twohanded",
            Name = "Two-Handed Devastator",
            Description = "Maximum damage output with two-handed weapons. Excellent for solo and small scale.",
            TargetClass = CharacterClass.Armsman,
            Realm = Realm.Albion,
            Role = "Melee DPS",
            RecommendedRealmRank = 5,
            SpecLines = new Dictionary<string, int>
            {
                { "Two Handed", 50 },
                { "Slash", 39 },
                { "Parry", 35 }
            },
            RealmAbilities = CreateRAs(("Determination", 5), ("Purge", 3), ("Ignore Pain", 3)),
            Tags = ["damage", "solo", "two-handed"]
        };

        // ─── Paladin ─────────────────────────────────────────────────────────
        yield return new MetaBuildTemplate
        {
            Id = "alb-pala-twohanded",
            Name = "Crusader",
            Description = "Two-handed holy warrior with strong self-heals and damage. Effective in all scenarios.",
            TargetClass = CharacterClass.Paladin,
            Realm = Realm.Albion,
            Role = "Hybrid Tank/Healer",
            RecommendedRealmRank = 6,
            SpecLines = new Dictionary<string, int>
            {
                { "Two Handed", 50 },
                { "Chants", 42 },
                { "Parry", 24 }
            },
            RealmAbilities = CreateRAs(("Determination", 5), ("Purge", 3), ("Mastery of Healing", 3)),
            Tags = ["hybrid", "tank", "healer", "solo"]
        };
        
        yield return new MetaBuildTemplate
        {
            Id = "alb-pala-shield",
            Name = "Defensive Guardian",
            Description = "Shield-focused tank with maximum survivability. Ideal for protecting casters.",
            TargetClass = CharacterClass.Paladin,
            Realm = Realm.Albion,
            Role = "Main Tank",
            RecommendedRealmRank = 5,
            SpecLines = new Dictionary<string, int>
            {
                { "Shield", 50 },
                { "Slash", 39 },
                { "Chants", 28 }
            },
            RealmAbilities = CreateRAs(("Determination", 5), ("Purge", 3), ("Ignore Pain", 5)),
            Tags = ["tank", "shield", "protect", "group"]
        };

        // ─── Mercenary ───────────────────────────────────────────────────────
        yield return new MetaBuildTemplate
        {
            Id = "alb-merc-dual",
            Name = "Dual Wield Assassin",
            Description = "Fast dual wield attacks with excellent burst damage. Great for hit-and-run.",
            TargetClass = CharacterClass.Mercenary,
            Realm = Realm.Albion,
            Role = "Melee DPS",
            RecommendedRealmRank = 6,
            SpecLines = new Dictionary<string, int>
            {
                { "Dual Wield", 50 },
                { "Slash", 44 },
                { "Parry", 18 }
            },
            RealmAbilities = CreateRAs(("Determination", 5), ("Purge", 3), ("Mastery of Pain", 5)),
            Tags = ["damage", "dual wield", "fast", "solo"]
        };

        // ─── Reaver ────────────────────────────────────────────────────────────
        yield return new MetaBuildTemplate
        {
            Id = "alb-reav-flex",
            Name = "Flexible Reaver",
            Description = "Flexible weapon specialist with Soulrending magic. High damage hybrid.",
            TargetClass = CharacterClass.Reaver,
            Realm = Realm.Albion,
            Role = "Hybrid DPS",
            RecommendedRealmRank = 6,
            SpecLines = new Dictionary<string, int>
            {
                { "Flexible", 50 },
                { "Shield", 42 },
                { "Soulrending", 36 }
            },
            RealmAbilities = CreateRAs(("Determination", 5), ("Purge", 3), ("Mastery of Pain", 3)),
            Tags = ["hybrid", "melee", "flexible", "lifetap"]
        };

        // ─── Infiltrator ─────────────────────────────────────────────────────
        yield return new MetaBuildTemplate
        {
            Id = "alb-inf-cs",
            Name = "Critical Strike Assassin",
            Description = "Maximum critical strike damage for one-shot potential. Classic assassin build.",
            TargetClass = CharacterClass.Infiltrator,
            Realm = Realm.Albion,
            Role = "Stealth DPS",
            RecommendedRealmRank = 7,
            SpecLines = new Dictionary<string, int>
            {
                { "Critical Strike", 50 },
                { "Stealth", 37 },
                { "Dual Wield", 35 }
            },
            RealmAbilities = CreateRAs(("Mastery of Stealth", 3), ("Vanish", 1), ("Purge", 3), ("Determination", 3)),
            Tags = ["stealth", "assassin", "burst", "solo"]
        };
        
        yield return new MetaBuildTemplate
        {
            Id = "alb-inf-hybrid",
            Name = "Hybrid Shadow",
            Description = "Balanced stealth and dual wield for sustained combat after opener.",
            TargetClass = CharacterClass.Infiltrator,
            Realm = Realm.Albion,
            Role = "Stealth DPS",
            RecommendedRealmRank = 6,
            SpecLines = new Dictionary<string, int>
            {
                { "Critical Strike", 44 },
                { "Dual Wield", 44 },
                { "Stealth", 34 }
            },
            RealmAbilities = CreateRAs(("Mastery of Stealth", 3), ("Vanish", 1), ("Determination", 5)),
            Tags = ["stealth", "dual wield", "sustained"]
        };

        // ─── Scout ───────────────────────────────────────────────────────────
        yield return new MetaBuildTemplate
        {
            Id = "alb-scout-bow",
            Name = "Longbow Sniper",
            Description = "Maximum bow damage for ranged assassination. Devastating opening volleys.",
            TargetClass = CharacterClass.Scout,
            Realm = Realm.Albion,
            Role = "Ranged DPS",
            RecommendedRealmRank = 6,
            SpecLines = new Dictionary<string, int>
            {
                { "Longbow", 50 },
                { "Stealth", 37 },
                { "Shield", 28 }
            },
            RealmAbilities = CreateRAs(("Mastery of Stealth", 3), ("Long Shot", 3), ("Purge", 3)),
            Tags = ["stealth", "ranged", "bow", "sniper"]
        };

        // ─── Cleric ──────────────────────────────────────────────────────────
        yield return new MetaBuildTemplate
        {
            Id = "alb-cler-rejuv",
            Name = "Rejuvenation Healer",
            Description = "Full heal spec for maximum group healing output. Essential 8-man healer.",
            TargetClass = CharacterClass.Cleric,
            Realm = Realm.Albion,
            Role = "Main Healer",
            RecommendedRealmRank = 6,
            SpecLines = new Dictionary<string, int>
            {
                { "Rejuvenation", 48 },
                { "Enhancement", 35 },
                { "Smite", 6 }
            },
            RealmAbilities = CreateRAs(("Mastery of Concentration", 5), ("Purge", 3), ("Mastery of Healing", 3)),
            Tags = ["healer", "support", "group", "8man"]
        };
        
        yield return new MetaBuildTemplate
        {
            Id = "alb-cler-smite",
            Name = "Smite Cleric",
            Description = "Damage-focused cleric with solid healing. Great for solo and small scale.",
            TargetClass = CharacterClass.Cleric,
            Realm = Realm.Albion,
            Role = "Hybrid DPS/Healer",
            RecommendedRealmRank = 5,
            SpecLines = new Dictionary<string, int>
            {
                { "Smite", 44 },
                { "Rejuvenation", 34 },
                { "Enhancement", 24 }
            },
            RealmAbilities = CreateRAs(("Mastery of Concentration", 3), ("Purge", 3), ("Wild Power", 3)),
            Tags = ["hybrid", "damage", "healer", "solo"]
        };

        // ─── Wizard ──────────────────────────────────────────────────────────
        yield return new MetaBuildTemplate
        {
            Id = "alb-wiz-fire",
            Name = "Fire Wizard",
            Description = "AoE fire damage spec. Excellent for large-scale RvR and keep defense.",
            TargetClass = CharacterClass.Wizard,
            Realm = Realm.Albion,
            Role = "Ranged AoE DPS",
            RecommendedRealmRank = 6,
            SpecLines = new Dictionary<string, int>
            {
                { "Fire", 48 },
                { "Earth", 26 },
                { "Ice", 6 }
            },
            RealmAbilities = CreateRAs(("Wild Power", 5), ("Mastery of Concentration", 3), ("Purge", 3)),
            Tags = ["caster", "aoe", "fire", "zerg"]
        };
        
        yield return new MetaBuildTemplate
        {
            Id = "alb-wiz-ice",
            Name = "Ice Wizard",
            Description = "Single-target ice and PBAE spec. Strong for 8-man and small scale.",
            TargetClass = CharacterClass.Wizard,
            Realm = Realm.Albion,
            Role = "Ranged DPS",
            RecommendedRealmRank = 6,
            SpecLines = new Dictionary<string, int>
            {
                { "Ice", 48 },
                { "Fire", 22 },
                { "Earth", 18 }
            },
            RealmAbilities = CreateRAs(("Wild Power", 5), ("Mastery of Concentration", 3), ("Purge", 3)),
            Tags = ["caster", "single target", "ice", "8man"]
        };

        // ─── Cabalist ──────────────────────────────────────────────────────────
        yield return new MetaBuildTemplate
        {
            Id = "alb-cab-matter",
            Name = "Matter Cabalist",
            Description = "DoT and pet focus with Nearsight. Excellent for PvE and solo RvR.",
            TargetClass = CharacterClass.Cabalist,
            Realm = Realm.Albion,
            Role = "Pet Caster / DoT",
            RecommendedRealmRank = 5,
            SpecLines = new Dictionary<string, int>
            {
                { "Matter", 46 },
                { "Body", 26 },
                { "Spirit", 6 }
            },
            RealmAbilities = CreateRAs(("Mastery of Concentration", 3), ("Purge", 3), ("Wild Power", 3)),
            Tags = ["caster", "pet", "dot", "nearsight"]
        };

        // ─── Sorcerer ────────────────────────────────────────────────────────
        yield return new MetaBuildTemplate
        {
            Id = "alb-sorc-matter",
            Name = "Matter Sorcerer",
            Description = "Matter DD and pet spec. Strong kiting potential with pet assist.",
            TargetClass = CharacterClass.Sorcerer,
            Realm = Realm.Albion,
            Role = "Ranged DPS / CC",
            RecommendedRealmRank = 6,
            SpecLines = new Dictionary<string, int>
            {
                { "Matter", 47 },
                { "Mind", 28 },
                { "Body", 10 }
            },
            RealmAbilities = CreateRAs(("Wild Power", 5), ("Mastery of Concentration", 3), ("Purge", 3)),
            Tags = ["caster", "pet", "cc", "kiting"]
        };

        // ─── Theurgist ───────────────────────────────────────────────────────
        yield return new MetaBuildTemplate
        {
            Id = "alb-theu-earth",
            Name = "Earth Theurgist",
            Description = "Earth pet swarm for area denial. Excellent keep defense.",
            TargetClass = CharacterClass.Theurgist,
            Realm = Realm.Albion,
            Role = "Pet Caster",
            RecommendedRealmRank = 5,
            SpecLines = new Dictionary<string, int>
            {
                { "Earth", 48 },
                { "Wind", 24 },
                { "Ice", 6 }
            },
            RealmAbilities = CreateRAs(("Wild Power", 3), ("Mastery of Concentration", 3), ("Purge", 3)),
            Tags = ["caster", "pet", "siege", "keep"]
        };

        // ─── Necromancer ────────────────────────────────────────────────────
        yield return new MetaBuildTemplate
        {
            Id = "alb-necro-deathsight",
            Name = "Deathsight Necromancer",
            Description = "Lifetap and AoE damage focus. Best for PvE and solo RvR.",
            TargetClass = CharacterClass.Necromancer,
            Realm = Realm.Albion,
            Role = "Pet Caster",
            RecommendedRealmRank = 5,
            SpecLines = new Dictionary<string, int>
            {
                { "Deathsight", 47 },
                { "Death Servant", 25 },
                { "Painworking", 6 }
            },
            RealmAbilities = CreateRAs(("Wild Power", 3), ("Mastery of Concentration", 3), ("Purge", 3)),
            Tags = ["caster", "pet", "lifetap", "solo"]
        };
        
        yield return new MetaBuildTemplate
        {
            Id = "alb-necro-servant",
            Name = "Death Servant Necromancer",
            Description = "Pet-centric build with tanking servant. Strong in group play.",
            TargetClass = CharacterClass.Necromancer,
            Realm = Realm.Albion,
            Role = "Pet Caster",
            RecommendedRealmRank = 5,
            SpecLines = new Dictionary<string, int>
            {
                { "Death Servant", 47 },
                { "Deathsight", 25 },
                { "Painworking", 6 }
            },
            RealmAbilities = CreateRAs(("Wild Power", 3), ("Mastery of Concentration", 3), ("Purge", 3)),
            Tags = ["caster", "pet", "group", "support"]
        };

        // ─── Friar ───────────────────────────────────────────────────────────
        yield return new MetaBuildTemplate
        {
            Id = "alb-friar-staff",
            Name = "Staff Friar",
            Description = "Melee hybrid with staff combat and heals. Great solo survivability.",
            TargetClass = CharacterClass.Friar,
            Realm = Realm.Albion,
            Role = "Melee Hybrid",
            RecommendedRealmRank = 6,
            SpecLines = new Dictionary<string, int>
            {
                { "Staff", 50 },
                { "Rejuvenation", 32 },
                { "Enhancement", 24 }
            },
            RealmAbilities = CreateRAs(("Determination", 5), ("Purge", 3), ("Mastery of Healing", 3)),
            Tags = ["hybrid", "melee", "healer", "solo"]
        };

        // ─── Heretic ───────────────────────────────────────────────────────────
        yield return new MetaBuildTemplate
        {
            Id = "alb-here-focus",
            Name = "Focus Heretic",
            Description = "Focus DD spells with strong buffs. Caster-oriented hybrid.",
            TargetClass = CharacterClass.Heretic,
            Realm = Realm.Albion,
            Role = "Hybrid Caster",
            RecommendedRealmRank = 6,
            SpecLines = new Dictionary<string, int>
            {
                { "Rejuvenation", 48 },
                { "Enhancement", 50 },
                { "Crush", 18 }
            },
            RealmAbilities = CreateRAs(("Mastery of Concentration", 5), ("Purge", 3), ("Wild Power", 3)),
            Tags = ["hybrid", "caster", "focus", "buffs"]
        };

        // ─── Minstrel ────────────────────────────────────────────────────────
        yield return new MetaBuildTemplate
        {
            Id = "alb-mins-speed",
            Name = "Speed Minstrel",
            Description = "Group speed and CC with solid melee. Essential group support.",
            TargetClass = CharacterClass.Minstrel,
            Realm = Realm.Albion,
            Role = "Support / CC",
            RecommendedRealmRank = 6,
            SpecLines = new Dictionary<string, int>
            {
                { "Instruments", 45 },
                { "Slash", 39 },
                { "Stealth", 21 }
            },
            RealmAbilities = CreateRAs(("Purge", 3), ("Determination", 3), ("Mastery of Concentration", 3)),
            Tags = ["support", "speed", "cc", "group"]
        };

        // ─── Mauler (Albion) ───────────────────────────────────────────────────
        yield return new MetaBuildTemplate
        {
            Id = "alb-maul-fist",
            Name = "Fist Wraps Brawler",
            Description = "Melee hybrid with earth magic. Power from taking damage.",
            TargetClass = CharacterClass.MaulerAlb,
            Realm = Realm.Albion,
            Role = "Melee Hybrid",
            RecommendedRealmRank = 5,
            SpecLines = new Dictionary<string, int>
            {
                { "Fist Wraps", 50 },
                { "Power Strikes", 42 },
                { "Magnetism", 28 }
            },
            RealmAbilities = CreateRAs(("Determination", 5), ("Purge", 3), ("Mastery of Pain", 3)),
            Tags = ["melee", "hybrid", "brawler"]
        };
    }

    // ═══════════════════════════════════════════════════════════════════════
    // MIDGARD TEMPLATES
    // ═══════════════════════════════════════════════════════════════════════

    private static IEnumerable<MetaBuildTemplate> CreateMidgardTemplates()
    {
        // ─── Warrior ─────────────────────────────────────────────────────────
        yield return new MetaBuildTemplate
        {
            Id = "mid-warr-hammer",
            Name = "Hammer Warrior",
            Description = "High damage hammer spec with stun styles. Devastating in melee trains.",
            TargetClass = CharacterClass.Warrior,
            Realm = Realm.Midgard,
            Role = "Melee DPS / Tank",
            RecommendedRealmRank = 6,
            SpecLines = new Dictionary<string, int>
            {
                { "Hammer", 50 },
                { "Shield", 42 },
                { "Parry", 28 }
            },
            RealmAbilities = CreateRAs(("Determination", 5), ("Purge", 3), ("Ignore Pain", 3)),
            Tags = ["tank", "damage", "hammer", "group"]
        };
        
        yield return new MetaBuildTemplate
        {
            Id = "mid-warr-sword",
            Name = "Sword Warrior",
            Description = "Balanced sword and shield for versatile tanking and damage.",
            TargetClass = CharacterClass.Warrior,
            Realm = Realm.Midgard,
            Role = "Tank",
            RecommendedRealmRank = 5,
            SpecLines = new Dictionary<string, int>
            {
                { "Sword", 50 },
                { "Shield", 42 },
                { "Parry", 28 }
            },
            RealmAbilities = CreateRAs(("Determination", 5), ("Purge", 3), ("Ignore Pain", 3)),
            Tags = ["tank", "sword", "shield", "group"]
        };

        // ─── Berserker ───────────────────────────────────────────────────────
        yield return new MetaBuildTemplate
        {
            Id = "mid-zerk-leftaxe",
            Name = "Left Axe Berserker",
            Description = "Dual axe fury with explosive damage. Classic berserker playstyle.",
            TargetClass = CharacterClass.Berserker,
            Realm = Realm.Midgard,
            Role = "Melee DPS",
            RecommendedRealmRank = 6,
            SpecLines = new Dictionary<string, int>
            {
                { "Left Axe", 50 },
                { "Axe", 44 },
                { "Parry", 18 }
            },
            RealmAbilities = CreateRAs(("Determination", 5), ("Purge", 3), ("Mastery of Pain", 3)),
            Tags = ["damage", "dual wield", "axe", "zerk"]
        };

        // ─── Bonedancer ───────────────────────────────────────────────────────
        yield return new MetaBuildTemplate
        {
            Id = "mid-bd-bone",
            Name = "Bone Army Bonedancer",
            Description = "Pet army commander with DoT damage. Strong leveling and solo class.",
            TargetClass = CharacterClass.Bonedancer,
            Realm = Realm.Midgard,
            Role = "Pet Caster",
            RecommendedRealmRank = 5,
            SpecLines = new Dictionary<string, int>
            {
                { "Bone Army", 48 },
                { "Suppression", 24 },
                { "Darkness", 13 }
            },
            RealmAbilities = CreateRAs(("Wild Power", 3), ("Mastery of Concentration", 3), ("Purge", 3)),
            Tags = ["caster", "pet", "dot", "solo"]
        };

        // ─── Savage ──────────────────────────────────────────────────────────
        yield return new MetaBuildTemplate
        {
            Id = "mid-sav-hth",
            Name = "Hand to Hand Savage",
            Description = "Pure hand to hand damage with savage buffs. Fast and deadly.",
            TargetClass = CharacterClass.Savage,
            Realm = Realm.Midgard,
            Role = "Melee DPS",
            RecommendedRealmRank = 6,
            SpecLines = new Dictionary<string, int>
            {
                { "Hand to Hand", 50 },
                { "Savagery", 44 },
                { "Parry", 18 }
            },
            RealmAbilities = CreateRAs(("Determination", 5), ("Purge", 3), ("Mastery of Pain", 3)),
            Tags = ["damage", "hand to hand", "fast", "solo"]
        };

        // ─── Shadowblade ─────────────────────────────────────────────────────
        yield return new MetaBuildTemplate
        {
            Id = "mid-sb-cs",
            Name = "Critical Strike Shadowblade",
            Description = "Maximum PA damage for assassination. High risk, high reward.",
            TargetClass = CharacterClass.Shadowblade,
            Realm = Realm.Midgard,
            Role = "Stealth DPS",
            RecommendedRealmRank = 7,
            SpecLines = new Dictionary<string, int>
            {
                { "Critical Strike", 50 },
                { "Stealth", 37 },
                { "Left Axe", 35 }
            },
            RealmAbilities = CreateRAs(("Mastery of Stealth", 3), ("Vanish", 1), ("Purge", 3), ("Determination", 3)),
            Tags = ["stealth", "assassin", "burst", "solo"]
        };
        
        yield return new MetaBuildTemplate
        {
            Id = "mid-sb-leftaxe",
            Name = "Left Axe Shadowblade",
            Description = "Sustained left axe damage after opener. More forgiving than pure CS.",
            TargetClass = CharacterClass.Shadowblade,
            Realm = Realm.Midgard,
            Role = "Stealth DPS",
            RecommendedRealmRank = 6,
            SpecLines = new Dictionary<string, int>
            {
                { "Left Axe", 50 },
                { "Critical Strike", 39 },
                { "Stealth", 34 }
            },
            RealmAbilities = CreateRAs(("Mastery of Stealth", 3), ("Determination", 5), ("Purge", 3)),
            Tags = ["stealth", "dual wield", "sustained"]
        };

        // ─── Hunter ──────────────────────────────────────────────────────────
        yield return new MetaBuildTemplate
        {
            Id = "mid-hunt-bow",
            Name = "Composite Bow Hunter",
            Description = "Maximum bow damage with pet support. Deadly ranged assassin.",
            TargetClass = CharacterClass.Hunter,
            Realm = Realm.Midgard,
            Role = "Ranged DPS",
            RecommendedRealmRank = 6,
            SpecLines = new Dictionary<string, int>
            {
                { "Composite Bow", 50 },
                { "Stealth", 35 },
                { "Beastcraft", 25 }
            },
            RealmAbilities = CreateRAs(("Mastery of Stealth", 3), ("Long Shot", 3), ("Purge", 3)),
            Tags = ["stealth", "ranged", "bow", "pet"]
        };

        // ─── Skald ───────────────────────────────────────────────────────────
        yield return new MetaBuildTemplate
        {
            Id = "mid-skald-speed",
            Name = "Speed Skald",
            Description = "Group speed and damage songs. Essential Midgard group support.",
            TargetClass = CharacterClass.Skald,
            Realm = Realm.Midgard,
            Role = "Support / Melee",
            RecommendedRealmRank = 6,
            SpecLines = new Dictionary<string, int>
            {
                { "Battlesongs", 44 },
                { "Hammer", 44 },
                { "Parry", 24 }
            },
            RealmAbilities = CreateRAs(("Determination", 5), ("Purge", 3)),
            Tags = ["support", "speed", "melee", "group"]
        };

        // ─── Thane ───────────────────────────────────────────────────────────
        yield return new MetaBuildTemplate
        {
            Id = "mid-thane-sc",
            Name = "Stormcalling Thane",
            Description = "Hybrid melee and lightning magic. Versatile damage dealer.",
            TargetClass = CharacterClass.Thane,
            Realm = Realm.Midgard,
            Role = "Hybrid DPS",
            RecommendedRealmRank = 6,
            SpecLines = new Dictionary<string, int>
            {
                { "Stormcalling", 44 },
                { "Hammer", 44 },
                { "Shield", 28 }
            },
            RealmAbilities = CreateRAs(("Determination", 5), ("Purge", 3), ("Wild Power", 3)),
            Tags = ["hybrid", "caster", "melee", "lightning"]
        };

        // ─── Valkyrie ───────────────────────────────────────────────────────
        yield return new MetaBuildTemplate
        {
            Id = "mid-valk-spear",
            Name = "Odin's Will Valkyrie",
            Description = "Spear melee with Odin's Will magic and Shield slam. Hybrid fighter.",
            TargetClass = CharacterClass.Valkyrie,
            Realm = Realm.Midgard,
            Role = "Hybrid DPS",
            RecommendedRealmRank = 6,
            SpecLines = new Dictionary<string, int>
            {
                { "Spear", 50 },
                { "Odin's Will", 35 },
                { "Shield", 42 }
            },
            RealmAbilities = CreateRAs(("Determination", 5), ("Purge", 3), ("Mastery of Focus", 2)),
            Tags = ["hybrid", "melee", "caster", "shield"]
        };

        // ─── Healer ──────────────────────────────────────────────────────────
        yield return new MetaBuildTemplate
        {
            Id = "mid-heal-mend",
            Name = "Mending Healer",
            Description = "Full healing spec for maximum throughput. Core 8-man healer.",
            TargetClass = CharacterClass.Healer,
            Realm = Realm.Midgard,
            Role = "Main Healer",
            RecommendedRealmRank = 6,
            SpecLines = new Dictionary<string, int>
            {
                { "Mending", 48 },
                { "Augmentation", 34 },
                { "Pacification", 6 }
            },
            RealmAbilities = CreateRAs(("Mastery of Concentration", 5), ("Purge", 3), ("Mastery of Healing", 3)),
            Tags = ["healer", "support", "group", "8man"]
        };

        // ─── Shaman ──────────────────────────────────────────────────────────
        yield return new MetaBuildTemplate
        {
            Id = "mid-sham-aug",
            Name = "Augmentation Shaman",
            Description = "Buff-focused shaman with endurance support. Group enhancer.",
            TargetClass = CharacterClass.Shaman,
            Realm = Realm.Midgard,
            Role = "Buffer / Healer",
            RecommendedRealmRank = 5,
            SpecLines = new Dictionary<string, int>
            {
                { "Augmentation", 44 },
                { "Mending", 36 },
                { "Cave Magic", 10 }
            },
            RealmAbilities = CreateRAs(("Mastery of Concentration", 3), ("Purge", 3), ("Wild Healing", 3)),
            Tags = ["buffer", "support", "healer", "group"]
        };
        
        yield return new MetaBuildTemplate
        {
            Id = "mid-sham-cave",
            Name = "Cave Shaman",
            Description = "Disease and damage over time focus. Strong in large scale.",
            TargetClass = CharacterClass.Shaman,
            Realm = Realm.Midgard,
            Role = "Hybrid Caster",
            RecommendedRealmRank = 5,
            SpecLines = new Dictionary<string, int>
            {
                { "Cave Magic", 44 },
                { "Mending", 32 },
                { "Augmentation", 22 }
            },
            RealmAbilities = CreateRAs(("Wild Power", 3), ("Purge", 3), ("Mastery of Concentration", 3)),
            Tags = ["caster", "disease", "dot", "zerg"]
        };

        // ─── Runemaster ──────────────────────────────────────────────────────
        yield return new MetaBuildTemplate
        {
            Id = "mid-rm-dark",
            Name = "Darkness Runemaster",
            Description = "Darkness bolt spec for high single-target damage. Classic nuker.",
            TargetClass = CharacterClass.Runemaster,
            Realm = Realm.Midgard,
            Role = "Ranged DPS",
            RecommendedRealmRank = 6,
            SpecLines = new Dictionary<string, int>
            {
                { "Darkness", 48 },
                { "Suppression", 24 },
                { "Runecarving", 8 }
            },
            RealmAbilities = CreateRAs(("Wild Power", 5), ("Mastery of Concentration", 3), ("Purge", 3)),
            Tags = ["caster", "bolt", "single target", "8man"]
        };

        // ─── Spiritmaster ────────────────────────────────────────────────────
        yield return new MetaBuildTemplate
        {
            Id = "mid-sm-supp",
            Name = "Suppression Spiritmaster",
            Description = "Spirit pet and suppression damage. Strong pet class.",
            TargetClass = CharacterClass.Spiritmaster,
            Realm = Realm.Midgard,
            Role = "Pet Caster",
            RecommendedRealmRank = 5,
            SpecLines = new Dictionary<string, int>
            {
                { "Suppression", 46 },
                { "Summoning", 28 },
                { "Darkness", 10 }
            },
            RealmAbilities = CreateRAs(("Wild Power", 3), ("Mastery of Concentration", 3), ("Purge", 3)),
            Tags = ["caster", "pet", "suppression"]
        };

        // ─── Warlock ──────────────────────────────────────────────────────────
        yield return new MetaBuildTemplate
        {
            Id = "mid-wlck-curse",
            Name = "Cursing Warlock",
            Description = "DPS-focused warlock with chamber system. High burst damage caster.",
            TargetClass = CharacterClass.Warlock,
            Realm = Realm.Midgard,
            Role = "Caster DPS",
            RecommendedRealmRank = 6,
            SpecLines = new Dictionary<string, int>
            {
                { "Cursing", 48 },
                { "Hexing", 25 },
                { "Witchcraft", 20 }
            },
            RealmAbilities = CreateRAs(("Wild Power", 3), ("Mastery of Concentration", 3), ("Purge", 3)),
            Tags = ["caster", "burst", "chamber", "dps"]
        };

        // ─── Mauler ─────────────────────────────────────────────────────────
        yield return new MetaBuildTemplate
        {
            Id = "mid-mauler-fist",
            Name = "Fist Mauler",
            Description = "Fist combat with Magnetism control. High damage with CC utility.",
            TargetClass = CharacterClass.MaulerMid,
            Realm = Realm.Midgard,
            Role = "Melee DPS / CC",
            RecommendedRealmRank = 6,
            SpecLines = new Dictionary<string, int>
            {
                { "Fist", 50 },
                { "Magnetism", 39 },
                { "Aura Manipulation", 20 }
            },
            RealmAbilities = CreateRAs(("Determination", 5), ("Purge", 3), ("Mastery of Pain", 3)),
            Tags = ["melee", "fist", "cc", "control"]
        };
        
        yield return new MetaBuildTemplate
        {
            Id = "mid-mauler-staff",
            Name = "Staff Mauler",
            Description = "Mauler Staff with Power Strikes. High burst damage build.",
            TargetClass = CharacterClass.MaulerMid,
            Realm = Realm.Midgard,
            Role = "Melee DPS",
            RecommendedRealmRank = 5,
            SpecLines = new Dictionary<string, int>
            {
                { "Mauler Staff", 50 },
                { "Power Strikes", 39 },
                { "Aura Manipulation", 20 }
            },
            RealmAbilities = CreateRAs(("Determination", 5), ("Purge", 3), ("Mastery of Pain", 3)),
            Tags = ["melee", "staff", "burst", "damage"]
        };
    }

    // ═══════════════════════════════════════════════════════════════════════
    // HIBERNIA TEMPLATES
    // ═══════════════════════════════════════════════════════════════════════

    private static IEnumerable<MetaBuildTemplate> CreateHiberniaTemplates()
    {
        // ─── Hero ────────────────────────────────────────────────────────────
        yield return new MetaBuildTemplate
        {
            Id = "hib-hero-spear",
            Name = "Celtic Spear Hero",
            Description = "Spear spec with reach and positional styles. Classic Hibernia tank.",
            TargetClass = CharacterClass.Hero,
            Realm = Realm.Hibernia,
            Role = "Tank / Melee DPS",
            RecommendedRealmRank = 6,
            SpecLines = new Dictionary<string, int>
            {
                { "Celtic Spear", 50 },
                { "Shield", 42 },
                { "Parry", 28 }
            },
            RealmAbilities = CreateRAs(("Determination", 5), ("Purge", 3), ("Ignore Pain", 3)),
            Tags = ["tank", "spear", "group", "frontline"]
        };
        
        yield return new MetaBuildTemplate
        {
            Id = "hib-hero-large",
            Name = "Large Weapons Hero",
            Description = "Two-handed large weapons for maximum damage output.",
            TargetClass = CharacterClass.Hero,
            Realm = Realm.Hibernia,
            Role = "Melee DPS",
            RecommendedRealmRank = 6,
            SpecLines = new Dictionary<string, int>
            {
                { "Large Weapons", 50 },
                { "Shield", 42 },
                { "Parry", 28 }
            },
            RealmAbilities = CreateRAs(("Determination", 5), ("Purge", 3), ("Mastery of Pain", 3)),
            Tags = ["damage", "large weapons", "twohanded"]
        };

        // ─── Champion ────────────────────────────────────────────────────────
        yield return new MetaBuildTemplate
        {
            Id = "hib-champ-valor",
            Name = "Valor Champion",
            Description = "Valor magic and large weapons. Powerful hybrid frontliner.",
            TargetClass = CharacterClass.Champion,
            Realm = Realm.Hibernia,
            Role = "Hybrid Tank",
            RecommendedRealmRank = 6,
            SpecLines = new Dictionary<string, int>
            {
                { "Valor", 44 },
                { "Large Weapons", 44 },
                { "Parry", 28 }
            },
            RealmAbilities = CreateRAs(("Determination", 5), ("Purge", 3), ("Wild Power", 3)),
            Tags = ["hybrid", "tank", "caster", "valor"]
        };

        // ─── Blademaster ─────────────────────────────────────────────────────
        yield return new MetaBuildTemplate
        {
            Id = "hib-bm-dual",
            Name = "Celtic Dual Blademaster",
            Description = "Fast dual wield with triple-wield styles. High sustained damage.",
            TargetClass = CharacterClass.Blademaster,
            Realm = Realm.Hibernia,
            Role = "Melee DPS",
            RecommendedRealmRank = 6,
            SpecLines = new Dictionary<string, int>
            {
                { "Celtic Dual", 50 },
                { "Blades", 44 },
                { "Parry", 18 }
            },
            RealmAbilities = CreateRAs(("Determination", 5), ("Purge", 3), ("Mastery of Pain", 3)),
            Tags = ["damage", "dual wield", "triple wield", "fast"]
        };

        // ─── Nightshade ──────────────────────────────────────────────────────
        yield return new MetaBuildTemplate
        {
            Id = "hib-ns-cs",
            Name = "Critical Strike Nightshade",
            Description = "Maximum PA damage with Celtic Dual backup. Classic assassin.",
            TargetClass = CharacterClass.Nightshade,
            Realm = Realm.Hibernia,
            Role = "Stealth DPS",
            RecommendedRealmRank = 7,
            SpecLines = new Dictionary<string, int>
            {
                { "Critical Strike", 50 },
                { "Stealth", 37 },
                { "Celtic Dual", 35 }
            },
            RealmAbilities = CreateRAs(("Mastery of Stealth", 3), ("Vanish", 1), ("Purge", 3), ("Determination", 3)),
            Tags = ["stealth", "assassin", "burst", "solo"]
        };
        
        yield return new MetaBuildTemplate
        {
            Id = "hib-ns-cd",
            Name = "Celtic Dual Nightshade",
            Description = "Sustained Celtic Dual damage after opener. Versatile assassin.",
            TargetClass = CharacterClass.Nightshade,
            Realm = Realm.Hibernia,
            Role = "Stealth DPS",
            RecommendedRealmRank = 6,
            SpecLines = new Dictionary<string, int>
            {
                { "Celtic Dual", 50 },
                { "Critical Strike", 39 },
                { "Stealth", 34 }
            },
            RealmAbilities = CreateRAs(("Mastery of Stealth", 3), ("Determination", 5), ("Purge", 3)),
            Tags = ["stealth", "dual wield", "sustained"]
        };

        // ─── Ranger ──────────────────────────────────────────────────────────
        yield return new MetaBuildTemplate
        {
            Id = "hib-rang-bow",
            Name = "Recurve Bow Ranger",
            Description = "Maximum bow damage with stealth. Deadly from range.",
            TargetClass = CharacterClass.Ranger,
            Realm = Realm.Hibernia,
            Role = "Ranged DPS",
            RecommendedRealmRank = 6,
            SpecLines = new Dictionary<string, int>
            {
                { "Recurve Bow", 50 },
                { "Stealth", 37 },
                { "Pathfinding", 25 }
            },
            RealmAbilities = CreateRAs(("Mastery of Stealth", 3), ("Long Shot", 3), ("Purge", 3)),
            Tags = ["stealth", "ranged", "bow", "sniper"]
        };

        // ─── Druid ───────────────────────────────────────────────────────────
        yield return new MetaBuildTemplate
        {
            Id = "hib-druid-regrowth",
            Name = "Regrowth Druid",
            Description = "Full healing spec for maximum group healing. Primary healer.",
            TargetClass = CharacterClass.Druid,
            Realm = Realm.Hibernia,
            Role = "Main Healer",
            RecommendedRealmRank = 6,
            SpecLines = new Dictionary<string, int>
            {
                { "Regrowth", 48 },
                { "Nurture", 34 },
                { "Nature", 6 }
            },
            RealmAbilities = CreateRAs(("Mastery of Concentration", 5), ("Purge", 3), ("Mastery of Healing", 3)),
            Tags = ["healer", "support", "group", "8man"]
        };
        
        yield return new MetaBuildTemplate
        {
            Id = "hib-druid-nature",
            Name = "Nature Druid",
            Description = "Damage and pet focused with backup heals. Versatile hybrid.",
            TargetClass = CharacterClass.Druid,
            Realm = Realm.Hibernia,
            Role = "Hybrid Caster",
            RecommendedRealmRank = 5,
            SpecLines = new Dictionary<string, int>
            {
                { "Nature", 44 },
                { "Regrowth", 32 },
                { "Nurture", 24 }
            },
            RealmAbilities = CreateRAs(("Wild Power", 3), ("Purge", 3), ("Mastery of Concentration", 3)),
            Tags = ["hybrid", "pet", "caster", "solo"]
        };

        // ─── Bard ────────────────────────────────────────────────────────────
        yield return new MetaBuildTemplate
        {
            Id = "hib-bard-speed",
            Name = "Speed Bard",
            Description = "Group speed and CC songs. Essential Hibernia support.",
            TargetClass = CharacterClass.Bard,
            Realm = Realm.Hibernia,
            Role = "Support / CC",
            RecommendedRealmRank = 6,
            SpecLines = new Dictionary<string, int>
            {
                { "Music", 45 },
                { "Blades", 39 },
                { "Nurture", 21 }
            },
            RealmAbilities = CreateRAs(("Purge", 3), ("Determination", 3), ("Mastery of Concentration", 3)),
            Tags = ["support", "speed", "cc", "group"]
        };

        // ─── Warden ──────────────────────────────────────────────────────────
        yield return new MetaBuildTemplate
        {
            Id = "hib-ward-nurture",
            Name = "Nurture Warden",
            Description = "Bladeturn and heal over time support. Group protector.",
            TargetClass = CharacterClass.Warden,
            Realm = Realm.Hibernia,
            Role = "Support / Tank",
            RecommendedRealmRank = 6,
            SpecLines = new Dictionary<string, int>
            {
                { "Nurture", 44 },
                { "Blades", 39 },
                { "Shield", 28 }
            },
            RealmAbilities = CreateRAs(("Determination", 5), ("Purge", 3)),
            Tags = ["support", "tank", "bladeturn", "group"]
        };

        // ─── Eldritch ────────────────────────────────────────────────────────
        yield return new MetaBuildTemplate
        {
            Id = "hib-eld-light",
            Name = "Light Eldritch",
            Description = "Light magic for high single-target damage. Classic nuker.",
            TargetClass = CharacterClass.Eldritch,
            Realm = Realm.Hibernia,
            Role = "Ranged DPS",
            RecommendedRealmRank = 6,
            SpecLines = new Dictionary<string, int>
            {
                { "Light", 48 },
                { "Mana", 24 },
                { "Void", 8 }
            },
            RealmAbilities = CreateRAs(("Wild Power", 5), ("Mastery of Concentration", 3), ("Purge", 3)),
            Tags = ["caster", "bolt", "single target", "8man"]
        };
        
        yield return new MetaBuildTemplate
        {
            Id = "hib-eld-void",
            Name = "Void Eldritch",
            Description = "Void AoE damage for large scale. Keep defense specialist.",
            TargetClass = CharacterClass.Eldritch,
            Realm = Realm.Hibernia,
            Role = "Ranged AoE DPS",
            RecommendedRealmRank = 6,
            SpecLines = new Dictionary<string, int>
            {
                { "Void", 48 },
                { "Light", 22 },
                { "Mana", 18 }
            },
            RealmAbilities = CreateRAs(("Wild Power", 5), ("Mastery of Concentration", 3), ("Purge", 3)),
            Tags = ["caster", "aoe", "void", "zerg"]
        };

        // ─── Enchanter ───────────────────────────────────────────────────────
        yield return new MetaBuildTemplate
        {
            Id = "hib-ench-light",
            Name = "Light Enchanter",
            Description = "Damage and pet with strong CC. Versatile caster.",
            TargetClass = CharacterClass.Enchanter,
            Realm = Realm.Hibernia,
            Role = "Pet Caster / CC",
            RecommendedRealmRank = 6,
            SpecLines = new Dictionary<string, int>
            {
                { "Light", 46 },
                { "Enchantments", 28 },
                { "Mana", 10 }
            },
            RealmAbilities = CreateRAs(("Wild Power", 3), ("Mastery of Concentration", 3), ("Purge", 3)),
            Tags = ["caster", "pet", "cc", "versatile"]
        };

        // ─── Mentalist ───────────────────────────────────────────────────────
        yield return new MetaBuildTemplate
        {
            Id = "hib-ment-light",
            Name = "Light Mentalist",
            Description = "Direct damage and charm pet. Solid nuker with utility.",
            TargetClass = CharacterClass.Mentalist,
            Realm = Realm.Hibernia,
            Role = "Ranged DPS",
            RecommendedRealmRank = 5,
            SpecLines = new Dictionary<string, int>
            {
                { "Light", 46 },
                { "Mentalism", 24 },
                { "Mana", 14 }
            },
            RealmAbilities = CreateRAs(("Wild Power", 5), ("Mastery of Concentration", 3), ("Purge", 3)),
            Tags = ["caster", "charm", "single target"]
        };

        // ─── Animist ─────────────────────────────────────────────────────────
        yield return new MetaBuildTemplate
        {
            Id = "hib-anim-creeping",
            Name = "Creeping Path Animist",
            Description = "Stationary turret pets for area denial. Siege specialist.",
            TargetClass = CharacterClass.Animist,
            Realm = Realm.Hibernia,
            Role = "Pet Caster",
            RecommendedRealmRank = 5,
            SpecLines = new Dictionary<string, int>
            {
                { "Creeping Path", 48 },
                { "Verdant Path", 22 },
                { "Arboreal Path", 14 }
            },
            RealmAbilities = CreateRAs(("Wild Power", 3), ("Mastery of Concentration", 3), ("Purge", 3)),
            Tags = ["caster", "pet", "siege", "turret"]
        };

        // ─── Bainshee ───────────────────────────────────────────────────────
        yield return new MetaBuildTemplate
        {
            Id = "hib-bain-shriek",
            Name = "Ethereal Shriek Bainshee",
            Description = "Ranged AoE nuker with sound-based magic. Zerg specialist.",
            TargetClass = CharacterClass.Bainshee,
            Realm = Realm.Hibernia,
            Role = "Ranged AoE DPS",
            RecommendedRealmRank = 6,
            SpecLines = new Dictionary<string, int>
            {
                { "Ethereal Shriek", 48 },
                { "Spectral Guard", 24 },
                { "Phantasmal Wail", 10 }
            },
            RealmAbilities = CreateRAs(("Wild Power", 5), ("Mastery of Concentration", 3), ("Purge", 3)),
            Tags = ["caster", "aoe", "shriek", "zerg"]
        };

        // ─── Valewalker ──────────────────────────────────────────────────────
        yield return new MetaBuildTemplate
        {
            Id = "hib-vw-scythe",
            Name = "Scythe Valewalker",
            Description = "Scythe melee with arboreal magic. Unique hybrid fighter.",
            TargetClass = CharacterClass.Valewalker,
            Realm = Realm.Hibernia,
            Role = "Hybrid Melee",
            RecommendedRealmRank = 6,
            SpecLines = new Dictionary<string, int>
            {
                { "Scythe", 50 },
                { "Arboreal Path", 34 },
                { "Parry", 28 }
            },
            RealmAbilities = CreateRAs(("Determination", 5), ("Purge", 3), ("Mastery of Pain", 3)),
            Tags = ["hybrid", "melee", "scythe", "caster"]
        };

        // ─── Vampiir ─────────────────────────────────────────────────────────
        yield return new MetaBuildTemplate
        {
            Id = "hib-vamp-pierce",
            Name = "Piercing Vampiir",
            Description = "Fast piercing attacks with shadow magic. Life-stealing assassin.",
            TargetClass = CharacterClass.Vampiir,
            Realm = Realm.Hibernia,
            Role = "Melee DPS",
            RecommendedRealmRank = 6,
            SpecLines = new Dictionary<string, int>
            {
                { "Piercing", 50 },
                { "Shadow Mastery", 38 },
                { "Dementia", 24 }
            },
            RealmAbilities = CreateRAs(("Determination", 5), ("Purge", 3), ("Mastery of Pain", 3)),
            Tags = ["melee", "piercing", "lifesteal", "solo"]
        };

        // ─── Mauler (Hibernia) ──────────────────────────────────────────────
        yield return new MetaBuildTemplate
        {
            Id = "hib-maul-fist",
            Name = "Fist Wraps Brawler",
            Description = "Fist wraps with Power Strikes. Martial artist fighter.",
            TargetClass = CharacterClass.MaulerHib,
            Realm = Realm.Hibernia,
            Role = "Melee DPS",
            RecommendedRealmRank = 6,
            SpecLines = new Dictionary<string, int>
            {
                { "Fist Wraps", 50 },
                { "Power Strikes", 42 },
                { "Magnetism", 28 }
            },
            RealmAbilities = CreateRAs(("Determination", 5), ("Purge", 3), ("Mastery of Pain", 3)),
            Tags = ["melee", "fist", "brawler", "martial"]
        };
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────────────────────

    private static List<RealmAbilitySelection> CreateRAs(params (string Name, int Rank)[] abilities)
    {
        return abilities.Select(a => new RealmAbilitySelection
        {
            AbilityName = a.Name,
            Rank = a.Rank,
            PointCost = CalculateRACost(a.Name, a.Rank),
            Category = DetermineRACategory(a.Name)
        }).ToList();
    }

    private static int CalculateRACost(string abilityName, int rank)
    {
        // Simplified cost calculation (actual costs vary by ability)
        // Passives typically: 1, 2, 3, 4, 5
        // Actives typically: 5, 3, 2 or 10, 5
        return abilityName.ToLower() switch
        {
            "determination" => rank * 2 + 2,  // 4, 6, 8, 10, 12
            "purge" => rank switch { 1 => 5, 2 => 8, 3 => 10, _ => 10 },
            "ignore pain" => rank * 2,
            "mastery of pain" => rank * 2,
            "mastery of concentration" => rank * 2 + 1,
            "mastery of healing" => rank * 2,
            "mastery of stealth" => rank * 2 + 1,
            "wild power" => rank * 2 + 1,
            "wild healing" => rank * 2,
            "vanish" => 5,
            "long shot" => rank * 2 + 1,
            _ => rank * 2
        };
    }

    private static RealmAbilityCategory DetermineRACategory(string abilityName)
    {
        var activeAbilities = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Purge", "Vanish", "Ignore Pain", "Determination", "First Aid"
        };
        
        return activeAbilities.Contains(abilityName)
            ? RealmAbilityCategory.Active
            : RealmAbilityCategory.Passive;
    }
}
