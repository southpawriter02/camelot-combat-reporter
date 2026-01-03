using CamelotCombatReporter.Core.Models;
using CamelotCombatReporter.Core.Parsing;
using Xunit;

namespace CamelotCombatReporter.Core.Tests;

public class LogParserTests
{
    [Fact]
    public void Parse_ShouldCorrectlyParsePlayerDamageEvent()
    {
        // Arrange
        var logContent = "[15:30:10] You hit the training dummy for 123 points of crush damage!";
        var logFilePath = Path.GetTempFileName();
        File.WriteAllText(logFilePath, logContent);

        var parser = new LogParser(logFilePath);

        // Act
        var events = parser.Parse().ToList();

        // Assert
        Assert.Single(events);
        var damageEvent = Assert.IsType<DamageEvent>(events.First());

        Assert.Equal(new TimeOnly(15, 30, 10), damageEvent.Timestamp);
        Assert.Equal("You", damageEvent.Source);
        Assert.Equal("training dummy", damageEvent.Target);
        Assert.Equal(123, damageEvent.DamageAmount);
        Assert.Equal("crush", damageEvent.DamageType);

        // Clean up the temporary file
        File.Delete(logFilePath);
    }

    [Fact]
    public void Parse_ShouldReturnUnknownDamageType_WhenTypeIsMissing()
    {
        // Arrange
        var logContent = "[12:00:00] You hit the training dummy for 50 points of damage.";
        var logFilePath = Path.GetTempFileName();
        File.WriteAllText(logFilePath, logContent);

        var parser = new LogParser(logFilePath);

        // Act
        var events = parser.Parse().ToList();

        // Assert
        Assert.Single(events);
        var damageEvent = Assert.IsType<DamageEvent>(events.First());
        Assert.Equal("Unknown", damageEvent.DamageType);

        // Clean up
        File.Delete(logFilePath);
    }

    [Fact]
    public void Parse_ShouldCorrectlyParsePlayerDamageTakenEvent()
    {
        // Arrange
        var logContent = "[08:45:15] A goblin hits you for 78 points of slash damage!";
        var logFilePath = Path.GetTempFileName();
        File.WriteAllText(logFilePath, logContent);

        var parser = new LogParser(logFilePath);

        // Act
        var events = parser.Parse().ToList();

        // Assert
        Assert.Single(events);
        var damageEvent = Assert.IsType<DamageEvent>(events.First());

        Assert.Equal(new TimeOnly(8, 45, 15), damageEvent.Timestamp);
        Assert.Equal("A goblin", damageEvent.Source);
        Assert.Equal("You", damageEvent.Target);
        Assert.Equal(78, damageEvent.DamageAmount);
        Assert.Equal("slash", damageEvent.DamageType);

        // Clean up
        File.Delete(logFilePath);
    }

    [Fact]
    public void Parse_ShouldHandleMixedDamageEvents()
    {
        // Arrange
        var logContent =
            "[15:30:10] You hit the training dummy for 123 points of crush damage!\n" +
            "[15:30:11] The training dummy hits you for 10 points of damage.\n" +
            "[15:30:12] You hit a goblin for 55 points of heat damage.";

        var logFilePath = Path.GetTempFileName();
        File.WriteAllText(logFilePath, logContent);

        var parser = new LogParser(logFilePath);

        // Act
        var events = parser.Parse().ToList();

        // Assert
        Assert.Equal(3, events.Count);

        // Event 1: Damage Dealt
        var event1 = Assert.IsType<DamageEvent>(events[0]);
        Assert.Equal("You", event1.Source);
        Assert.Equal("training dummy", event1.Target);
        Assert.Equal(123, event1.DamageAmount);

        // Event 2: Damage Taken
        var event2 = Assert.IsType<DamageEvent>(events[1]);
        Assert.Equal("The training dummy", event2.Source);
        Assert.Equal("You", event2.Target);
        Assert.Equal(10, event2.DamageAmount);
        Assert.Equal("Unknown", event2.DamageType);

        // Event 3: Damage Dealt
        var event3 = Assert.IsType<DamageEvent>(events[2]);
        Assert.Equal("You", event3.Source);
        Assert.Equal("a goblin", event3.Target);
        Assert.Equal(55, event3.DamageAmount);


        // Clean up
        File.Delete(logFilePath);
    }

    [Fact]
    public void Parse_ShouldHandleEmptyFile()
    {
        // Arrange
        var logFilePath = Path.GetTempFileName();
        // The file is empty by default

        var parser = new LogParser(logFilePath);

        // Act
        var events = parser.Parse().ToList();

        // Assert
        Assert.Empty(events);

        // Clean up
        File.Delete(logFilePath);
    }

    [Fact]
    public void Parse_ShouldCorrectlyParseHealingDoneEvent()
    {
        // Arrange
        var logContent = "[16:45:30] You heal Merlin for 250 hit points!";
        var logFilePath = Path.GetTempFileName();
        File.WriteAllText(logFilePath, logContent);

        var parser = new LogParser(logFilePath);

        // Act
        var events = parser.Parse().ToList();

        // Assert
        Assert.Single(events);
        var healingEvent = Assert.IsType<HealingEvent>(events.First());

        Assert.Equal(new TimeOnly(16, 45, 30), healingEvent.Timestamp);
        Assert.Equal("You", healingEvent.Source);
        Assert.Equal("Merlin", healingEvent.Target);
        Assert.Equal(250, healingEvent.HealingAmount);

        // Clean up
        File.Delete(logFilePath);
    }

    [Fact]
    public void Parse_ShouldCorrectlyParseHealingReceivedEvent()
    {
        // Arrange
        var logContent = "[17:00:15] A cleric heals you for 175 hit points.";
        var logFilePath = Path.GetTempFileName();
        File.WriteAllText(logFilePath, logContent);

        var parser = new LogParser(logFilePath);

        // Act
        var events = parser.Parse().ToList();

        // Assert
        Assert.Single(events);
        var healingEvent = Assert.IsType<HealingEvent>(events.First());

        Assert.Equal(new TimeOnly(17, 0, 15), healingEvent.Timestamp);
        Assert.Equal("A cleric", healingEvent.Source);
        Assert.Equal("You", healingEvent.Target);
        Assert.Equal(175, healingEvent.HealingAmount);

        // Clean up
        File.Delete(logFilePath);
    }

    [Fact]
    public void Parse_ShouldHandleMixedCombatAndHealingEvents()
    {
        // Arrange
        var logContent =
            "[15:30:10] You hit the goblin for 100 points of crush damage!\n" +
            "[15:30:12] The goblin hits you for 50 points of slash damage!\n" +
            "[15:30:14] You heal yourself for 75 hit points.\n" +
            "[15:30:16] A healer heals you for 120 hit points!\n" +
            "[15:30:18] You use Backstab on the goblin!\n" +
            "[15:30:20] You cast Fireball on the goblin.";

        var logFilePath = Path.GetTempFileName();
        File.WriteAllText(logFilePath, logContent);

        var parser = new LogParser(logFilePath);

        // Act
        var events = parser.Parse().ToList();

        // Assert
        Assert.Equal(6, events.Count);

        // Event 1: Damage Dealt
        var event1 = Assert.IsType<DamageEvent>(events[0]);
        Assert.Equal("You", event1.Source);
        Assert.Equal(100, event1.DamageAmount);

        // Event 2: Damage Taken
        var event2 = Assert.IsType<DamageEvent>(events[1]);
        Assert.Equal("You", event2.Target);
        Assert.Equal(50, event2.DamageAmount);

        // Event 3: Healing Done
        var event3 = Assert.IsType<HealingEvent>(events[2]);
        Assert.Equal("You", event3.Source);
        Assert.Equal("yourself", event3.Target);
        Assert.Equal(75, event3.HealingAmount);

        // Event 4: Healing Received
        var event4 = Assert.IsType<HealingEvent>(events[3]);
        Assert.Equal("A healer", event4.Source);
        Assert.Equal("You", event4.Target);
        Assert.Equal(120, event4.HealingAmount);

        // Event 5: Combat Style
        var event5 = Assert.IsType<CombatStyleEvent>(events[4]);
        Assert.Equal("Backstab", event5.StyleName);

        // Event 6: Spell Cast
        var event6 = Assert.IsType<SpellCastEvent>(events[5]);
        Assert.Equal("Fireball", event6.SpellName);

        // Clean up
        File.Delete(logFilePath);
    }

    [Fact]
    public void Parse_ShouldCorrectlyParseCombatStyleEvent()
    {
        // Arrange
        var logContent = "[10:15:30] You use Evade on the training dummy!";
        var logFilePath = Path.GetTempFileName();
        File.WriteAllText(logFilePath, logContent);

        var parser = new LogParser(logFilePath);

        // Act
        var events = parser.Parse().ToList();

        // Assert
        Assert.Single(events);
        var styleEvent = Assert.IsType<CombatStyleEvent>(events.First());

        Assert.Equal(new TimeOnly(10, 15, 30), styleEvent.Timestamp);
        Assert.Equal("You", styleEvent.Source);
        Assert.Equal("training dummy", styleEvent.Target);
        Assert.Equal("Evade", styleEvent.StyleName);

        // Clean up
        File.Delete(logFilePath);
    }

    [Fact]
    public void Parse_ShouldCorrectlyParseSpellCastEvent()
    {
        // Arrange
        var logContent = "[11:20:45] You cast Greater Heal on Merlin.";
        var logFilePath = Path.GetTempFileName();
        File.WriteAllText(logFilePath, logContent);

        var parser = new LogParser(logFilePath);

        // Act
        var events = parser.Parse().ToList();

        // Assert
        Assert.Single(events);
        var spellEvent = Assert.IsType<SpellCastEvent>(events.First());

        Assert.Equal(new TimeOnly(11, 20, 45), spellEvent.Timestamp);
        Assert.Equal("You", spellEvent.Source);
        Assert.Equal("Merlin", spellEvent.Target);
        Assert.Equal("Greater Heal", spellEvent.SpellName);

        // Clean up
        File.Delete(logFilePath);
    }

    #region Loot Parsing Tests

    [Theory]
    [InlineData("[06:55:52] The bright aurora drops a crystalline orb, which you pick up.", "bright aurora", "crystalline orb", false)]
    [InlineData("[17:33:50] Llyn Chythraul drops the Afanc Tongue, which you pick up.", "Llyn Chythraul", "Afanc Tongue", true)]
    [InlineData("[20:43:07] Svartmoln drops a mauler claw, which you pick up.", "Svartmoln", "mauler claw", false)]
    [InlineData("[23:29:33] The bright aurora drops a Heatbender Padded Cap, which you pick up.", "bright aurora", "Heatbender Padded Cap", false)]
    [InlineData("[07:08:20] The bright aurora drops the Aurora Corpse, which you pick up.", "bright aurora", "Aurora Corpse", true)]
    [InlineData("[17:33:50] Llyn Chythraul drops an afanc eye, which you pick up.", "Llyn Chythraul", "afanc eye", false)]
    public void Parse_ItemDrop_ShouldExtractMobAndItem(string line, string expectedMob, string expectedItem, bool expectedIsNamed)
    {
        // Arrange
        var logFilePath = Path.GetTempFileName();
        File.WriteAllText(logFilePath, line);
        var parser = new LogParser(logFilePath);

        // Act
        var events = parser.Parse().ToList();

        // Assert
        Assert.Single(events);
        var itemDropEvent = Assert.IsType<ItemDropEvent>(events.First());
        Assert.Equal(expectedMob, itemDropEvent.MobName);
        Assert.Equal(expectedItem, itemDropEvent.ItemName);
        Assert.Equal(expectedIsNamed, itemDropEvent.IsNamedItem);

        // Clean up
        File.Delete(logFilePath);
    }

    [Theory]
    [InlineData("[06:55:52] You pick up 1 gold, 48 silver and 98 copper pieces.", 1, 48, 98)]
    [InlineData("[23:32:42] You pick up 1 gold and 15 silver pieces.", 1, 15, 0)]
    [InlineData("[06:58:44] You pick up 75 silver and 90 copper pieces.", 0, 75, 90)]
    [InlineData("[04:37:09] You pick up 9 silver and 6 copper pieces.", 0, 9, 6)]
    public void Parse_CurrencyPickup_ShouldExtractAmounts(string line, int expectedGold, int expectedSilver, int expectedCopper)
    {
        // Arrange
        var logFilePath = Path.GetTempFileName();
        File.WriteAllText(logFilePath, line);
        var parser = new LogParser(logFilePath);

        // Act
        var events = parser.Parse().ToList();

        // Assert
        Assert.Single(events);
        var currencyEvent = Assert.IsType<CurrencyDropEvent>(events.First());
        Assert.Equal(expectedGold, currencyEvent.Gold);
        Assert.Equal(expectedSilver, currencyEvent.Silver);
        Assert.Equal(expectedCopper, currencyEvent.Copper);

        // Clean up
        File.Delete(logFilePath);
    }

    [Fact]
    public void Parse_CurrencyDropEvent_TotalCopper_ShouldCalculateCorrectly()
    {
        // Arrange
        var logFilePath = Path.GetTempFileName();
        File.WriteAllText(logFilePath, "[06:55:52] You pick up 1 gold, 48 silver and 98 copper pieces.");
        var parser = new LogParser(logFilePath);

        // Act
        var events = parser.Parse().ToList();

        // Assert
        var currencyEvent = Assert.IsType<CurrencyDropEvent>(events.First());
        // 1 gold = 10000 copper, 48 silver = 4800 copper, 98 copper = 98
        Assert.Equal(14898, currencyEvent.TotalCopper);

        // Clean up
        File.Delete(logFilePath);
    }

    [Theory]
    [InlineData("[08:59:32] You receive the Biting Wind Eye from the biting wind!", "Biting Wind Eye", "the biting wind")]
    [InlineData("[06:23:28] You receive the Aurora Corpse from Kvasir!", "Aurora Corpse", "Kvasir")]
    [InlineData("[13:43:12] You receive the Sable Rune Vest from Hi Testers!!", "Sable Rune Vest", "Hi Testers!")]
    public void Parse_ItemReceive_ShouldExtractItemAndSource(string line, string expectedItem, string expectedSource)
    {
        // Arrange
        var logFilePath = Path.GetTempFileName();
        File.WriteAllText(logFilePath, line);
        var parser = new LogParser(logFilePath);

        // Act
        var events = parser.Parse().ToList();

        // Assert
        Assert.Single(events);
        var receiveEvent = Assert.IsType<ItemReceiveEvent>(events.First());
        Assert.Equal(expectedItem, receiveEvent.ItemName);
        Assert.Equal(expectedSource, receiveEvent.SourceName);
        Assert.Null(receiveEvent.Quantity);

        // Clean up
        File.Delete(logFilePath);
    }

    [Theory]
    [InlineData("[21:51:29] You received 23 Soil of Albion as your reward!", 23, "Soil of Albion")]
    [InlineData("[21:31:14] You received 63 Snow of Midgard as your reward!", 63, "Snow of Midgard")]
    [InlineData("[21:31:14] You received 58 Phoenix Egg as your reward!", 58, "Phoenix Egg")]
    public void Parse_RewardReceive_ShouldExtractQuantityAndItem(string line, int expectedQty, string expectedItem)
    {
        // Arrange
        var logFilePath = Path.GetTempFileName();
        File.WriteAllText(logFilePath, line);
        var parser = new LogParser(logFilePath);

        // Act
        var events = parser.Parse().ToList();

        // Assert
        Assert.Single(events);
        var receiveEvent = Assert.IsType<ItemReceiveEvent>(events.First());
        Assert.Equal(expectedItem, receiveEvent.ItemName);
        Assert.Equal("Reward", receiveEvent.SourceName);
        Assert.Equal(expectedQty, receiveEvent.Quantity);

        // Clean up
        File.Delete(logFilePath);
    }

    [Fact]
    public void Parse_BonusCurrencyOutpost_ShouldExtractAmountsAndSource()
    {
        // Arrange
        var logFilePath = Path.GetTempFileName();
        File.WriteAllText(logFilePath, "[04:37:09] You find an additional 8 copper pieces thanks to your realm owning outposts!");
        var parser = new LogParser(logFilePath);

        // Act
        var events = parser.Parse().ToList();

        // Assert
        Assert.Single(events);
        var bonusEvent = Assert.IsType<BonusCurrencyEvent>(events.First());
        Assert.Equal(0, bonusEvent.Gold);
        Assert.Equal(0, bonusEvent.Silver);
        Assert.Equal(8, bonusEvent.Copper);
        Assert.Equal("outpost", bonusEvent.BonusSource);

        // Clean up
        File.Delete(logFilePath);
    }

    [Fact]
    public void Parse_BonusCurrencyArea_ShouldExtractAmountsAndSource()
    {
        // Arrange
        var logFilePath = Path.GetTempFileName();
        File.WriteAllText(logFilePath, "[04:37:09] You gain an additional 4 silver and 53 copper pieces for adventuring in this area!");
        var parser = new LogParser(logFilePath);

        // Act
        var events = parser.Parse().ToList();

        // Assert
        Assert.Single(events);
        var bonusEvent = Assert.IsType<BonusCurrencyEvent>(events.First());
        Assert.Equal(0, bonusEvent.Gold);
        Assert.Equal(4, bonusEvent.Silver);
        Assert.Equal(53, bonusEvent.Copper);
        Assert.Equal("area", bonusEvent.BonusSource);

        // Clean up
        File.Delete(logFilePath);
    }

    [Fact]
    public void Parse_BagOfCoins_ShouldBeSkipped()
    {
        // Bag of coins is not an item we want to track - currency is handled separately
        // Arrange
        var logFilePath = Path.GetTempFileName();
        File.WriteAllText(logFilePath, "[06:55:52] The bright aurora drops a bag of coins, which you pick up.");
        var parser = new LogParser(logFilePath);

        // Act
        var events = parser.Parse().ToList();

        // Assert
        Assert.Empty(events);

        // Clean up
        File.Delete(logFilePath);
    }

    [Fact]
    public void Parse_MixedLootEvents_ShouldParseAllTypes()
    {
        // Arrange
        var logContent =
            "[06:58:44] The bright aurora drops a crystalline orb, which you pick up.\n" +
            "[06:58:44] You pick up 75 silver and 90 copper pieces.\n" +
            "[06:58:44] The bright aurora drops a bag of coins, which you pick up.\n" +
            "[06:58:44] You find an additional 56 copper pieces thanks to your realm owning outposts!\n" +
            "[06:58:44] You gain an additional 29 silver and 4 copper pieces for adventuring in this area!";

        var logFilePath = Path.GetTempFileName();
        File.WriteAllText(logFilePath, logContent);
        var parser = new LogParser(logFilePath);

        // Act
        var events = parser.Parse().ToList();

        // Assert
        Assert.Equal(4, events.Count); // 5 lines, but bag of coins is skipped
        Assert.IsType<ItemDropEvent>(events[0]);
        Assert.IsType<CurrencyDropEvent>(events[1]);
        Assert.IsType<BonusCurrencyEvent>(events[2]);
        Assert.IsType<BonusCurrencyEvent>(events[3]);

        // Clean up
        File.Delete(logFilePath);
    }

    [Fact]
    public void Parse_CurrencyReceiveAlternateFormat_ShouldParse()
    {
        // Arrange
        var logFilePath = Path.GetTempFileName();
        File.WriteAllText(logFilePath, "[23:34:49] You receive 1 gold, 3 silver, 76 copper");
        var parser = new LogParser(logFilePath);

        // Act
        var events = parser.Parse().ToList();

        // Assert
        Assert.Single(events);
        var currencyEvent = Assert.IsType<CurrencyDropEvent>(events.First());
        Assert.Equal(1, currencyEvent.Gold);
        Assert.Equal(3, currencyEvent.Silver);
        Assert.Equal(76, currencyEvent.Copper);

        // Clean up
        File.Delete(logFilePath);
    }

    [Fact]
    public void Parse_RealLogFile_ShouldExtractAllLootEventTypes()
    {
        // Arrange - create a realistic multi-line log file snippet
        var logContent = """
            [06:55:52] You hit the bright aurora for 432 damage!
            [06:55:52] The bright aurora dies!
            [06:55:52] You find an additional 1 silver and 46 copper pieces thanks to your realm owning outposts!
            [06:55:52] You gain an additional 74 silver and 49 copper pieces for adventuring in this area!
            [06:55:52] You pick up 1 gold, 48 silver and 98 copper pieces.
            [06:55:52] The bright aurora drops a bag of coins, which you pick up.
            [06:55:52] The bright aurora drops a crystalline orb, which you pick up.
            [07:00:00] You received 23 Soil of Albion as your reward!
            [07:01:00] You receive the Aurora Corpse from Kvasir!
            """;

        var logFilePath = Path.GetTempFileName();
        File.WriteAllText(logFilePath, logContent);
        var parser = new LogParser(logFilePath);

        // Act
        var events = parser.Parse().ToList();

        // Assert - should have: 1 damage, 2 bonus currency, 1 currency drop, 1 item drop, 1 reward, 1 item receive = 7 events
        // (bag of coins is skipped, "dies!" is not parsed)
        // Actually 6 events since we aren't parsing damage without type
        Assert.Equal(6, events.Count);

        // Verify we got all the loot types
        Assert.Equal(2, events.OfType<BonusCurrencyEvent>().Count());
        Assert.Single(events.OfType<CurrencyDropEvent>());
        Assert.Single(events.OfType<ItemDropEvent>());
        Assert.Equal(2, events.OfType<ItemReceiveEvent>().Count()); // reward + item receive

        // Verify specific values
        var currencyDrop = events.OfType<CurrencyDropEvent>().First();
        Assert.Equal(1, currencyDrop.Gold);
        Assert.Equal(48, currencyDrop.Silver);
        Assert.Equal(98, currencyDrop.Copper);
        Assert.Equal(14898, currencyDrop.TotalCopper);

        var itemDrop = events.OfType<ItemDropEvent>().First();
        Assert.Equal("bright aurora", itemDrop.MobName);
        Assert.Equal("crystalline orb", itemDrop.ItemName);
        Assert.False(itemDrop.IsNamedItem);

        var outpostBonus = events.OfType<BonusCurrencyEvent>().First(b => b.BonusSource == "outpost");
        Assert.Equal(0, outpostBonus.Gold);
        Assert.Equal(1, outpostBonus.Silver);
        Assert.Equal(46, outpostBonus.Copper);

        var areaBonus = events.OfType<BonusCurrencyEvent>().First(b => b.BonusSource == "area");
        Assert.Equal(0, areaBonus.Gold);
        Assert.Equal(74, areaBonus.Silver);
        Assert.Equal(49, areaBonus.Copper);

        var reward = events.OfType<ItemReceiveEvent>().First(r => r.SourceName == "Reward");
        Assert.Equal("Soil of Albion", reward.ItemName);
        Assert.Equal(23, reward.Quantity);

        var itemReceive = events.OfType<ItemReceiveEvent>().First(r => r.SourceName != "Reward");
        Assert.Equal("Aurora Corpse", itemReceive.ItemName);
        Assert.Equal("Kvasir", itemReceive.SourceName);

        // Clean up
        File.Delete(logFilePath);
    }

    #endregion
}
