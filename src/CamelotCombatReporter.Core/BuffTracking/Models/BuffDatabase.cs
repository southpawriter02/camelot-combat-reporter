namespace CamelotCombatReporter.Core.BuffTracking.Models;

/// <summary>
/// Database of known buff and debuff definitions.
/// </summary>
public static class BuffDatabase
{
    /// <summary>
    /// All known buff definitions.
    /// </summary>
    public static IReadOnlyList<BuffDefinition> AllBuffs { get; } = CreateBuffDefinitions();

    /// <summary>
    /// Gets a buff by ID.
    /// </summary>
    public static BuffDefinition? GetById(string buffId) =>
        AllBuffs.FirstOrDefault(b => string.Equals(b.BuffId, buffId, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Gets a buff by name.
    /// </summary>
    public static BuffDefinition? GetByName(string name) =>
        AllBuffs.FirstOrDefault(b => string.Equals(b.Name, name, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Gets all buffs in a category.
    /// </summary>
    public static IReadOnlyList<BuffDefinition> GetByCategory(BuffCategory category) =>
        AllBuffs.Where(b => b.Category == category).ToList();

    /// <summary>
    /// Gets all expected buffs (for uptime tracking).
    /// </summary>
    public static IReadOnlyList<BuffDefinition> GetExpectedBuffs() =>
        AllBuffs.Where(b => b.IsExpectedBuff).ToList();

    /// <summary>
    /// Gets all beneficial buffs.
    /// </summary>
    public static IReadOnlyList<BuffDefinition> GetBeneficialBuffs() =>
        AllBuffs.Where(b => b.IsBeneficial).ToList();

    /// <summary>
    /// Gets all debuffs.
    /// </summary>
    public static IReadOnlyList<BuffDefinition> GetDebuffs() =>
        AllBuffs.Where(b => !b.IsBeneficial).ToList();

    private static List<BuffDefinition> CreateBuffDefinitions()
    {
        return new List<BuffDefinition>
        {
            // Stat Buffs (15 minutes, concentration-based)
            new("str_buff", "Strength Buff", BuffCategory.StatBuff, 900,
                BuffStackingRule.HigherWins, ConcentrationType.GroupSlot, true,
                new[] { "feel stronger", "strength increases" }, "Increases Strength stat"),

            new("con_buff", "Constitution Buff", BuffCategory.StatBuff, 900,
                BuffStackingRule.HigherWins, ConcentrationType.GroupSlot, true,
                new[] { "feel healthier", "constitution increases" }, "Increases Constitution stat"),

            new("dex_buff", "Dexterity Buff", BuffCategory.StatBuff, 900,
                BuffStackingRule.HigherWins, ConcentrationType.GroupSlot, true,
                new[] { "feel more agile", "dexterity increases" }, "Increases Dexterity stat"),

            new("qui_buff", "Quickness Buff", BuffCategory.StatBuff, 900,
                BuffStackingRule.HigherWins, ConcentrationType.GroupSlot, true,
                new[] { "feel quicker", "quickness increases" }, "Increases Quickness stat"),

            new("acu_buff", "Acuity Buff", BuffCategory.StatBuff, 900,
                BuffStackingRule.HigherWins, ConcentrationType.GroupSlot, true,
                new[] { "feel smarter", "intelligence increases", "piety increases" }, "Increases casting stat"),

            // Armor/Defense Buffs
            new("af_buff", "Armor Factor Buff", BuffCategory.ArmorBuff, 900,
                BuffStackingRule.HigherWins, ConcentrationType.GroupSlot, true,
                new[] { "armor strengthens", "feel more protected" }, "Increases Armor Factor"),

            new("abs_buff", "Absorption Buff", BuffCategory.ArmorBuff, 900,
                BuffStackingRule.HigherWins, ConcentrationType.None, false,
                new[] { "absorb damage" }, "Increases damage absorption"),

            new("bladeturn", "Bladeturn", BuffCategory.ArmorBuff, 30,
                BuffStackingRule.NoStack, ConcentrationType.Self, false,
                new[] { "bladeturn", "bubble" }, "Blocks next melee hit"),

            // Resistance Buffs
            new("body_resist", "Body Resist Buff", BuffCategory.ResistanceBuff, 900,
                BuffStackingRule.HigherWins, ConcentrationType.None, false,
                new[] { "body resistance increases" }, "Increases Body resistance"),

            new("cold_resist", "Cold Resist Buff", BuffCategory.ResistanceBuff, 900,
                BuffStackingRule.HigherWins, ConcentrationType.None, false,
                new[] { "cold resistance increases" }, "Increases Cold resistance"),

            new("heat_resist", "Heat Resist Buff", BuffCategory.ResistanceBuff, 900,
                BuffStackingRule.HigherWins, ConcentrationType.None, false,
                new[] { "heat resistance increases" }, "Increases Heat resistance"),

            new("energy_resist", "Energy Resist Buff", BuffCategory.ResistanceBuff, 900,
                BuffStackingRule.HigherWins, ConcentrationType.None, false,
                new[] { "energy resistance increases" }, "Increases Energy resistance"),

            new("matter_resist", "Matter Resist Buff", BuffCategory.ResistanceBuff, 900,
                BuffStackingRule.HigherWins, ConcentrationType.None, false,
                new[] { "matter resistance increases" }, "Increases Matter resistance"),

            new("spirit_resist", "Spirit Resist Buff", BuffCategory.ResistanceBuff, 900,
                BuffStackingRule.HigherWins, ConcentrationType.None, false,
                new[] { "spirit resistance increases" }, "Increases Spirit resistance"),

            new("crush_resist", "Crush Resist Buff", BuffCategory.ResistanceBuff, 900,
                BuffStackingRule.HigherWins, ConcentrationType.None, false,
                new[] { "crush resistance increases" }, "Increases Crush resistance"),

            new("slash_resist", "Slash Resist Buff", BuffCategory.ResistanceBuff, 900,
                BuffStackingRule.HigherWins, ConcentrationType.None, false,
                new[] { "slash resistance increases" }, "Increases Slash resistance"),

            new("thrust_resist", "Thrust Resist Buff", BuffCategory.ResistanceBuff, 900,
                BuffStackingRule.HigherWins, ConcentrationType.None, false,
                new[] { "thrust resistance increases" }, "Increases Thrust resistance"),

            // Damage/Speed Buffs
            new("damage_add", "Damage Add", BuffCategory.DamageAddBuff, 900,
                BuffStackingRule.HigherWins, ConcentrationType.GroupSlot, true,
                new[] { "damage add", "damage increases" }, "Adds flat damage to attacks"),

            new("celerity", "Celerity", BuffCategory.HasteBuff, 900,
                BuffStackingRule.HigherWins, ConcentrationType.GroupSlot, true,
                new[] { "attack faster", "celerity", "haste" }, "Increases attack speed"),

            new("speed", "Speed", BuffCategory.SpeedBuff, -1,
                BuffStackingRule.HigherWins, ConcentrationType.GroupSlot, true,
                new[] { "run faster", "speed" }, "Increases movement speed"),

            new("speed_of_sound", "Speed of Sound", BuffCategory.SpeedBuff, 30,
                BuffStackingRule.NoStack, ConcentrationType.RealmAbility, false,
                new[] { "speed of sound" }, "Instant maximum speed (RA)"),

            // Regeneration Buffs
            new("hp_regen", "Health Regeneration", BuffCategory.RegenerationBuff, 900,
                BuffStackingRule.HigherWins, ConcentrationType.None, false,
                new[] { "health regeneration", "hp regen" }, "Increases HP regeneration"),

            new("power_regen", "Power Regeneration", BuffCategory.RegenerationBuff, 900,
                BuffStackingRule.HigherWins, ConcentrationType.None, false,
                new[] { "power regeneration", "mana regen" }, "Increases Power regeneration"),

            new("end_regen", "Endurance Regeneration", BuffCategory.RegenerationBuff, 900,
                BuffStackingRule.HigherWins, ConcentrationType.None, false,
                new[] { "endurance regeneration", "end regen" }, "Increases Endurance regeneration"),

            // Realm Ability Buffs
            new("soldiers_barricade", "Soldier's Barricade", BuffCategory.RealmAbilityBuff, 30,
                BuffStackingRule.NoStack, ConcentrationType.RealmAbility, false,
                new[] { "soldier's barricade" }, "+50 AF, +10% absorb"),

            new("the_empty_mind", "The Empty Mind", BuffCategory.RealmAbilityBuff, 20,
                BuffStackingRule.NoStack, ConcentrationType.RealmAbility, false,
                new[] { "empty mind" }, "CC immunity"),

            new("juggernaut", "Juggernaut", BuffCategory.RealmAbilityBuff, 20,
                BuffStackingRule.NoStack, ConcentrationType.RealmAbility, false,
                new[] { "juggernaut" }, "CC immunity + damage boost"),

            new("divine_intervention", "Divine Intervention", BuffCategory.RealmAbilityBuff, -1,
                BuffStackingRule.NoStack, ConcentrationType.RealmAbility, false,
                new[] { "divine intervention", "valhalla's blessing", "dana's blessing" }, "Prevents death once"),

            // Stat Debuffs
            new("str_debuff", "Strength Debuff", BuffCategory.StatDebuff, 60,
                BuffStackingRule.HigherWins, ConcentrationType.None, false,
                new[] { "strength decreases", "feel weaker" }, "Decreases Strength stat"),

            new("con_debuff", "Constitution Debuff", BuffCategory.StatDebuff, 60,
                BuffStackingRule.HigherWins, ConcentrationType.None, false,
                new[] { "constitution decreases" }, "Decreases Constitution stat"),

            new("dex_debuff", "Dexterity Debuff", BuffCategory.StatDebuff, 60,
                BuffStackingRule.HigherWins, ConcentrationType.None, false,
                new[] { "dexterity decreases" }, "Decreases Dexterity stat"),

            // Resistance Debuffs
            new("body_resist_debuff", "Body Resist Debuff", BuffCategory.ResistDebuff, 60,
                BuffStackingRule.HigherWins, ConcentrationType.None, false,
                new[] { "body resistance decreases" }, "Decreases Body resistance"),

            new("cold_resist_debuff", "Cold Resist Debuff", BuffCategory.ResistDebuff, 60,
                BuffStackingRule.HigherWins, ConcentrationType.None, false,
                new[] { "cold resistance decreases" }, "Decreases Cold resistance"),

            new("heat_resist_debuff", "Heat Resist Debuff", BuffCategory.ResistDebuff, 60,
                BuffStackingRule.HigherWins, ConcentrationType.None, false,
                new[] { "heat resistance decreases" }, "Decreases Heat resistance"),

            new("energy_resist_debuff", "Energy Resist Debuff", BuffCategory.ResistDebuff, 60,
                BuffStackingRule.HigherWins, ConcentrationType.None, false,
                new[] { "energy resistance decreases" }, "Decreases Energy resistance"),

            new("matter_resist_debuff", "Matter Resist Debuff", BuffCategory.ResistDebuff, 60,
                BuffStackingRule.HigherWins, ConcentrationType.None, false,
                new[] { "matter resistance decreases" }, "Decreases Matter resistance"),

            new("spirit_resist_debuff", "Spirit Resist Debuff", BuffCategory.ResistDebuff, 60,
                BuffStackingRule.HigherWins, ConcentrationType.None, false,
                new[] { "spirit resistance decreases" }, "Decreases Spirit resistance"),

            // Armor Debuffs
            new("af_debuff", "AF Debuff", BuffCategory.ArmorDebuff, 60,
                BuffStackingRule.HigherWins, ConcentrationType.None, false,
                new[] { "armor weakens", "armor factor decreases" }, "Decreases Armor Factor"),

            // DoT Effects
            new("disease", "Disease", BuffCategory.Disease, 60,
                BuffStackingRule.HigherWins, ConcentrationType.None, false,
                new[] { "diseased", "disease" }, "HP debuff + reduced healing"),

            new("poison", "Poison", BuffCategory.DamageOverTime, 30,
                BuffStackingRule.StackDuration, ConcentrationType.None, false,
                new[] { "poisoned", "poison" }, "Periodic poison damage"),

            new("bleed", "Bleed", BuffCategory.Bleed, 20,
                BuffStackingRule.StackDuration, ConcentrationType.None, false,
                new[] { "bleeding", "bleed" }, "Periodic bleed damage"),

            // Speed Debuffs
            new("snare", "Snare", BuffCategory.SpeedDebuff, 30,
                BuffStackingRule.NoStack, ConcentrationType.None, false,
                new[] { "snared", "slowed" }, "Reduces movement speed")
        };
    }
}
