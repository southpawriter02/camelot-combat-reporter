using CamelotCombatReporter.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace CamelotCombatReporter.Core.CharacterBuilding.Services;

/// <summary>
/// Infers character class from combat styles used in combat logs.
/// Uses a comprehensive mapping of DAoC combat styles to their classes.
/// </summary>
public class CombatLogClassDetector : ICombatLogClassDetector
{
    private readonly ILogger<CombatLogClassDetector> _logger;
    
    // Style name → list of classes that can use it
    private static readonly Dictionary<string, List<CharacterClass>> StyleToClasses = new(StringComparer.OrdinalIgnoreCase);
    
    // Class → list of unique styles
    private static readonly Dictionary<CharacterClass, List<string>> ClassToStyles = new();

    static CombatLogClassDetector()
    {
        InitializeStyleMappings();
    }

    public CombatLogClassDetector(ILogger<CombatLogClassDetector>? logger = null)
    {
        _logger = logger ?? NullLogger<CombatLogClassDetector>.Instance;
    }

    /// <inheritdoc/>
    public ClassInferenceResult InferClassFromStyles(IEnumerable<string> styleNames)
    {
        var styleList = styleNames.ToList();
        if (styleList.Count == 0)
        {
            _logger.LogDebug("No styles provided for class inference");
            return new ClassInferenceResult(null, 0, [], new Dictionary<string, int>(), []);
        }

        // Count style usage
        var styleUsage = styleList
            .GroupBy(s => s, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase);

        var detectedStyles = styleUsage.Keys.ToList();
        
        // Score each class based on matching styles
        var classScores = new Dictionary<CharacterClass, int>();
        
        foreach (var style in detectedStyles)
        {
            if (StyleToClasses.TryGetValue(style, out var classes))
            {
                foreach (var cls in classes)
                {
                    classScores.TryGetValue(cls, out var count);
                    classScores[cls] = count + styleUsage[style];
                }
            }
        }

        if (classScores.Count == 0)
        {
            _logger.LogDebug("No class matches found for {StyleCount} styles", detectedStyles.Count);
            return new ClassInferenceResult(null, 0, detectedStyles, styleUsage, []);
        }

        // Sort by score descending
        var ranked = classScores
            .OrderByDescending(kv => kv.Value)
            .ToList();

        var topClass = ranked[0].Key;
        var topScore = ranked[0].Value;
        
        // Calculate confidence based on:
        // 1. Total style matches
        // 2. Margin over second-place class
        double confidence;
        if (ranked.Count == 1)
        {
            // Only one class matched - high confidence if many matches
            confidence = Math.Min(1.0, topScore / 10.0);
        }
        else
        {
            var secondScore = ranked[1].Value;
            var margin = (double)(topScore - secondScore) / topScore;
            confidence = Math.Min(1.0, (topScore / 10.0) * (0.5 + margin * 0.5));
        }

        var candidateClasses = ranked.Select(r => r.Key).ToList();

        _logger.LogDebug(
            "Inferred class {Class} with confidence {Confidence:P0} from {StyleCount} styles",
            topClass, confidence, detectedStyles.Count);

        return new ClassInferenceResult(
            topClass,
            confidence,
            detectedStyles,
            styleUsage,
            candidateClasses
        );
    }

    /// <inheritdoc/>
    public IReadOnlyList<string> GetStylesForClass(CharacterClass charClass)
    {
        return ClassToStyles.TryGetValue(charClass, out var styles)
            ? styles
            : [];
    }

    /// <inheritdoc/>
    public IReadOnlyList<CharacterClass> GetClassesForStyle(string styleName)
    {
        return StyleToClasses.TryGetValue(styleName, out var classes)
            ? classes
            : [];
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Style Mappings
    // ─────────────────────────────────────────────────────────────────────────

    private static void InitializeStyleMappings()
    {
        // ═══════════════════════════════════════════════════════════════════
        // ALBION CLASSES
        // ═══════════════════════════════════════════════════════════════════
        
        // Armsman - Polearm, Slash, Thrust, Crush, Two-Handed, Shield
        AddStyles(CharacterClass.Armsman, 
            // Polearm
            "Defender's Rage", "Phalanx", "Defender's Fury", "Lancer's Fury", 
            "Defender's Aegis", "Tribal Assault",
            // Two-Handed
            "Two-Handed Slash", "Decapitate", "Assault",
            // Crossbow styles
            "Quick Shot", "Volley");

        // Paladin - Slash, Thrust, Crush, Two-Handed, Shield
        AddStyles(CharacterClass.Paladin,
            // Crush
            "Holy Warrior", "Retribution", "Faith's Fury",
            // Two-Handed
            "Holy Vengeance", "Grand Crusade");

        // Mercenary - Dual Wield, Slash, Thrust, Crush
        AddStyles(CharacterClass.Mercenary,
            // Dual Wield
            "Flurry", "Dual Strike", "Shadow's Rain", 
            "Celtic Dual", "Doublefrost", "Hurricane");

        // Reaver - Slash, Thrust, Crush, Flexible
        AddStyles(CharacterClass.Reaver,
            // Flexible
            "Viper's Bite", "Serpent's Wrath", "Cobra Strike",
            "Soul Quench", "Spectral Guard");

        // Cabalist - No melee styles typically
        AddStyles(CharacterClass.Cabalist, "Focus Staff Strike");

        // Sorcerer - No melee styles typically  
        AddStyles(CharacterClass.Sorcerer, "Focus Staff Strike");

        // Theurgist - No melee styles typically
        AddStyles(CharacterClass.Theurgist, "Focus Staff Strike");

        // Wizard - No melee styles typically
        AddStyles(CharacterClass.Wizard, "Focus Staff Strike");

        // Infiltrator - Stealth, Dual Wield, Thrust, Slash, Critical Strike
        AddStyles(CharacterClass.Infiltrator,
            // Critical Strike
            "Perforate Artery", "Leaper", "Creeping Death", "Hamstring",
            "Rib Separation", "Stunning Stab", "Shadow Strike");

        // Minstrel - Instruments, Slash, Thrust
        AddStyles(CharacterClass.Minstrel,
            "Bardic Slash", "Rhapsody", "Lullaby Strike");

        // Scout - Stealth, Longbow, Slash, Thrust, Shield
        AddStyles(CharacterClass.Scout,
            // Longbow
            "Point Blank Shot", "Rapid Fire", "Critical Shot", "Power Shot",
            "Long Shot");

        // Cleric - Crush, Staff, Shield
        AddStyles(CharacterClass.Cleric,
            "Smite", "Holy Staff", "Blessed Strike");

        // Friar - Staff, Crush
        AddStyles(CharacterClass.Friar,
            "Friar's Fury", "Staff Sweep", "Spinning Staff",
            "Solar Flare", "Holy Staff");

        // Heretic - Crush, Flexible
        AddStyles(CharacterClass.Heretic,
            "Viper's Bite", "Serpent's Wrath", "Soul Quench",
            "Spiritual Ruin");

        // Necromancer - No melee styles typically
        AddStyles(CharacterClass.Necromancer, "Focus Staff Strike");

        // ═══════════════════════════════════════════════════════════════════
        // MIDGARD CLASSES
        // ═══════════════════════════════════════════════════════════════════

        // Warrior - Axe, Hammer, Sword, Two-Handed
        AddStyles(CharacterClass.Warrior,
            // Axe
            "Evernight", "Arctic Rift", "Frost's Fury", "Glacial Movement",
            // Hammer
            "Mjolnir's Fury", "Thor's Hammer", "Crushing Blow",
            // Sword
            "Ragnarok", "Heimdall's Watch");

        // Berserker - Axe, Hammer, Sword, Left Axe
        AddStyles(CharacterClass.Berserker,
            // Left Axe
            "Snowsquall", "Frosty Gaze", "Icy Brilliance", 
            "Polar Rift", "Aurora Borealis",
            // Berserker-specific
            "Berserk Strike");

        // Savage - Hand to Hand, Savage
        AddStyles(CharacterClass.Savage,
            "Raging Blow", "Savage Strike", "Tribal Assault",
            "Kelgor's Fist", "Primal Fury");

        // Skald - Axe, Hammer, Sword
        AddStyles(CharacterClass.Skald,
            "Battle Hymn", "War Cry Strike", "Song of Power");

        // Thane - Axe, Hammer, Sword, Shield
        AddStyles(CharacterClass.Thane,
            "Thunder Strike", "Lightning Call", "Storm Bringer");

        // Valkyrie - Sword, Spear, Shield
        AddStyles(CharacterClass.Valkyrie,
            // Spear
            "Odin's Spear", "Valkyrie's Strike", "Spear Lunge",
            "Driving Spear", "Sleeper");

        // Shadowblade - Stealth, Sword, Axe, Left Axe, Critical Strike
        AddStyles(CharacterClass.Shadowblade,
            // Critical Strike
            "Perforate Artery", "Leaper", "Creeping Death", "Hamstring",
            // Left Axe
            "Snowsquall", "Frosty Gaze", "Aurora Borealis");

        // Hunter - Stealth, Spear, Bow, Sword
        AddStyles(CharacterClass.Hunter,
            // Bow
            "Point Blank Shot", "Rapid Fire", "Critical Shot", "Power Shot",
            // Spear
            "Odin's Spear", "Sleeper");

        // Runemaster - No melee styles typically
        AddStyles(CharacterClass.Runemaster, "Focus Staff Strike");

        // Spiritmaster - No melee styles typically
        AddStyles(CharacterClass.Spiritmaster, "Focus Staff Strike");

        // Bonedancer - No melee styles typically
        AddStyles(CharacterClass.Bonedancer, "Focus Staff Strike");

        // Warlock - No melee styles typically
        AddStyles(CharacterClass.Warlock, "Focus Staff Strike");

        // Healer - Hammer, Staff
        AddStyles(CharacterClass.Healer,
            "Healing Strike", "Staff Sweep");

        // Shaman - Hammer, Staff
        AddStyles(CharacterClass.Shaman,
            "Spirit Strike", "Cave Hammer");

        // ═══════════════════════════════════════════════════════════════════
        // HIBERNIA CLASSES
        // ═══════════════════════════════════════════════════════════════════

        // Hero - Large Weapons, Blades, Blunt, Piercing, Celtic Spear, Shield
        AddStyles(CharacterClass.Hero,
            // Celtic Spear
            "Ancient Spear", "Culainn's Thrust", "Spear of Kings",
            // Large Weapons
            "Devastating Blow", "Kelgor's Fist");

        // Champion - Large Weapons, Blades, Blunt, Piercing
        AddStyles(CharacterClass.Champion,
            "Champion's Strike", "Celtic Fury", "Devastating Blow");

        // Blademaster - Blades, Piercing, Celtic Dual
        AddStyles(CharacterClass.Blademaster,
            // Celtic Dual
            "Celtic Dual", "Hurricane", "Doublefrost",
            "Tornado", "Tempest");

        // Warden - Blades, Blunt, Nurture
        AddStyles(CharacterClass.Warden,
            "Nature's Wrath", "Forest Strike", "Warden's Guard");

        // Druid - Blunt, Staff, Nurture
        AddStyles(CharacterClass.Druid,
            "Nature's Blessing", "Staff Sweep", "Druidic Strike");

        // Bard - Blades, Blunt, Music
        AddStyles(CharacterClass.Bard,
            "Bardic Slash", "Song Strike", "Melodic Blow");

        // Nightshade - Stealth, Blades, Piercing, Celtic Dual, Critical Strike
        AddStyles(CharacterClass.Nightshade,
            // Critical Strike
            "Perforate Artery", "Leaper", "Creeping Death", "Hamstring",
            // Celtic Dual
            "Celtic Dual", "Hurricane", "Doublefrost");

        // Ranger - Stealth, Blades, Piercing, Celtic Dual, Archery
        AddStyles(CharacterClass.Ranger,
            // Archery
            "Point Blank Shot", "Rapid Fire", "Critical Shot", "Power Shot",
            // Celtic Dual
            "Celtic Dual", "Hurricane");

        // Eldritch - No melee styles typically
        AddStyles(CharacterClass.Eldritch, "Focus Staff Strike");

        // Enchanter - No melee styles typically
        AddStyles(CharacterClass.Enchanter, "Focus Staff Strike");

        // Mentalist - No melee styles typically
        AddStyles(CharacterClass.Mentalist, "Focus Staff Strike");

        // Animist - No melee styles typically
        AddStyles(CharacterClass.Animist, "Focus Staff Strike");

        // Valewalker - Scythe, Arboreal
        AddStyles(CharacterClass.Valewalker,
            "Scythe Sweep", "Reaping Blade", "Forest's Justice",
            "Arboreal Strike", "Thornwood Slash");

        // Vampiir - Piercing, Shadow Mastery
        AddStyles(CharacterClass.Vampiir,
            "Shadow Strike", "Blood Drain", "Vampire's Kiss",
            "Night Terror", "Crimson Lunge");

        // Bainshee - No melee styles typically
        AddStyles(CharacterClass.Bainshee, "Focus Staff Strike");

        // ═══════════════════════════════════════════════════════════════════
        // SHARED STYLES (available to multiple classes via weapon specs)
        // ═══════════════════════════════════════════════════════════════════

        // Slash (many Albion melee classes)
        AddSharedStyles(new[] { CharacterClass.Armsman, CharacterClass.Paladin, CharacterClass.Mercenary,
            CharacterClass.Reaver, CharacterClass.Minstrel, CharacterClass.Scout },
            "Basilisk Fang", "Northern Lights", "Razor's Edge", "Reflect", "Side Slicer", "Paralyze");

        // Thrust (Albion stealth, some melee)
        AddSharedStyles(new[] { CharacterClass.Infiltrator, CharacterClass.Scout, CharacterClass.Mercenary,
            CharacterClass.Minstrel, CharacterClass.Armsman, CharacterClass.Paladin },
            "Bloody Dance", "Lunging Thrust", "Liontooth", "Riposte", "Backstab", "Stiletto");

        // Crush (Albion weapon line)
        AddSharedStyles(new[] { CharacterClass.Armsman, CharacterClass.Paladin, CharacterClass.Mercenary,
            CharacterClass.Cleric, CharacterClass.Friar, CharacterClass.Heretic },
            "Mangle", "Skull Crusher", "Bone Breaker", "Diamond Hammer", "Bruiser");

        // Shield (most tank classes)
        AddSharedStyles(new[] { CharacterClass.Armsman, CharacterClass.Paladin, CharacterClass.Reaver,
            CharacterClass.Warrior, CharacterClass.Thane, CharacterClass.Hero,
            CharacterClass.Champion, CharacterClass.Warden },
            "Slam", "Numb", "Shield Bash", "Brutalize", "Engage", "Protect");

        // Stealth openers (all assassins)
        AddSharedStyles(new[] { CharacterClass.Infiltrator, CharacterClass.Shadowblade, CharacterClass.Nightshade },
            "Backstab", "Shadow Strike", "Garrote", "Ambush");

        // Blades (Hibernia weapon line)
        AddSharedStyles(new[] { CharacterClass.Hero, CharacterClass.Champion, CharacterClass.Blademaster,
            CharacterClass.Nightshade, CharacterClass.Ranger, CharacterClass.Bard, CharacterClass.Warden },
            "Return Blade", "Onslaught", "Prismatic Blade", "Celtic Crescent", "Spectrum Blade");

        // Piercing (Hibernia weapon line)
        AddSharedStyles(new[] { CharacterClass.Hero, CharacterClass.Champion, CharacterClass.Blademaster,
            CharacterClass.Nightshade, CharacterClass.Ranger, CharacterClass.Vampiir },
            "Celtic Fury", "Diamondback", "Asp's Bite", "Sidewinder", "Copperhead");

        // Blunt (Hibernia weapon line)
        AddSharedStyles(new[] { CharacterClass.Hero, CharacterClass.Champion, CharacterClass.Warden,
            CharacterClass.Bard, CharacterClass.Druid },
            "Crushing Blow", "Stunning Blow", "Heavy Blow");
    }

    private static void AddStyles(CharacterClass charClass, params string[] styles)
    {
        if (!ClassToStyles.ContainsKey(charClass))
        {
            ClassToStyles[charClass] = new List<string>();
        }

        foreach (var style in styles)
        {
            ClassToStyles[charClass].Add(style);

            if (!StyleToClasses.ContainsKey(style))
            {
                StyleToClasses[style] = new List<CharacterClass>();
            }
            if (!StyleToClasses[style].Contains(charClass))
            {
                StyleToClasses[style].Add(charClass);
            }
        }
    }

    private static void AddSharedStyles(CharacterClass[] classes, params string[] styles)
    {
        foreach (var charClass in classes)
        {
            AddStyles(charClass, styles);
        }
    }
}
