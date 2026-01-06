using CamelotCombatReporter.Core.InstanceTracking;
using CamelotCombatReporter.Core.Models;
using Xunit;

namespace CamelotCombatReporter.Core.Tests.InstanceTracking;

public class CombatInstanceResolverTests
{
    private readonly CombatInstanceResolver _resolver;
    private const string PlayerName = "TestPlayer";
    private const string MobName = "spraggonet";

    public CombatInstanceResolverTests()
    {
        _resolver = new CombatInstanceResolver();
    }

    [Fact]
    public void SingleMobCleanKill_ReturnsOneEncounter()
    {
        // Arrange
        var events = new List<LogEvent>
        {
            new DamageEvent(new TimeOnly(10, 0, 0), PlayerName, MobName, 100, "Crush"),
            new DamageEvent(new TimeOnly(10, 0, 1), PlayerName, MobName, 150, "Crush"),
            new DamageEvent(new TimeOnly(10, 0, 2), PlayerName, MobName, 120, "Crush"),
            new DeathEvent(new TimeOnly(10, 0, 3), MobName, PlayerName)
        };

        // Act
        var result = _resolver.ResolveInstances(events, PlayerName);

        // Assert
        Assert.Single(result);
        Assert.Equal(MobName, result[0].TargetName);
        Assert.Equal(1, result[0].TotalKills);
        Assert.Single(result[0].Encounters);

        var encounter = result[0].Encounters[0];
        Assert.Equal(370, encounter.TotalDamageDealt);
        Assert.Equal(EncounterEndReason.Death, encounter.EndReason);
        Assert.Equal(1, encounter.Instance.InstanceNumber);
        Assert.Equal(MobName, encounter.Instance.DisplayName); // No suffix for #1
    }

    [Fact]
    public void ThreeSameNamedMobsKilledSequentially_ReturnsThreeEncounters()
    {
        // Arrange
        var events = new List<LogEvent>
        {
            // First mob
            new DamageEvent(new TimeOnly(10, 0, 0), PlayerName, MobName, 100, "Crush"),
            new DamageEvent(new TimeOnly(10, 0, 2), PlayerName, MobName, 100, "Crush"),
            new DeathEvent(new TimeOnly(10, 0, 3), MobName, PlayerName),

            // Second mob
            new DamageEvent(new TimeOnly(10, 0, 5), PlayerName, MobName, 150, "Slash"),
            new DamageEvent(new TimeOnly(10, 0, 6), PlayerName, MobName, 150, "Slash"),
            new DeathEvent(new TimeOnly(10, 0, 7), MobName, PlayerName),

            // Third mob
            new DamageEvent(new TimeOnly(10, 0, 10), PlayerName, MobName, 200, "Thrust"),
            new DeathEvent(new TimeOnly(10, 0, 12), MobName, PlayerName)
        };

        // Act
        var result = _resolver.ResolveInstances(events, PlayerName);

        // Assert
        Assert.Single(result); // One target type
        Assert.Equal(MobName, result[0].TargetName);
        Assert.Equal(3, result[0].TotalKills);
        Assert.Equal(3, result[0].Encounters.Count);

        // Verify individual encounters
        var encounters = result[0].Encounters.OrderBy(e => e.StartTime).ToList();

        Assert.Equal(1, encounters[0].Instance.InstanceNumber);
        Assert.Equal(200, encounters[0].TotalDamageDealt);
        Assert.Equal(MobName, encounters[0].Instance.DisplayName);

        Assert.Equal(2, encounters[1].Instance.InstanceNumber);
        Assert.Equal(300, encounters[1].TotalDamageDealt);
        Assert.Equal($"{MobName} #2", encounters[1].Instance.DisplayName);

        Assert.Equal(3, encounters[2].Instance.InstanceNumber);
        Assert.Equal(200, encounters[2].TotalDamageDealt);
        Assert.Equal($"{MobName} #3", encounters[2].Instance.DisplayName);
    }

    [Fact]
    public void TimeGapBetweenDamage_CreatesSeparateEncounters()
    {
        // Arrange - 20 second gap (exceeds default 15s timeout)
        var events = new List<LogEvent>
        {
            new DamageEvent(new TimeOnly(10, 0, 0), PlayerName, MobName, 100, "Crush"),
            new DamageEvent(new TimeOnly(10, 0, 2), PlayerName, MobName, 100, "Crush"),
            // No death, but 20 second gap
            new DamageEvent(new TimeOnly(10, 0, 22), PlayerName, MobName, 150, "Crush"),
            new DeathEvent(new TimeOnly(10, 0, 25), MobName, PlayerName)
        };

        // Act
        var result = _resolver.ResolveInstances(events, PlayerName);

        // Assert
        Assert.Single(result);
        Assert.Equal(2, result[0].Encounters.Count);

        var encounters = result[0].Encounters.OrderBy(e => e.StartTime).ToList();

        // First encounter timed out
        Assert.Equal(EncounterEndReason.Timeout, encounters[0].EndReason);
        Assert.Equal(200, encounters[0].TotalDamageDealt);

        // Second encounter killed
        Assert.Equal(EncounterEndReason.Death, encounters[1].EndReason);
        Assert.Equal(150, encounters[1].TotalDamageDealt);
    }

    [Fact]
    public void MobDespawnsWithoutDeath_EncounterClosesAsTimeout()
    {
        // Arrange - damage but no death, then session ends
        var events = new List<LogEvent>
        {
            new DamageEvent(new TimeOnly(10, 0, 0), PlayerName, MobName, 100, "Crush"),
            new DamageEvent(new TimeOnly(10, 0, 2), PlayerName, MobName, 150, "Crush")
        };

        // Act
        var result = _resolver.ResolveInstances(events, PlayerName);

        // Assert
        Assert.Single(result);
        Assert.Single(result[0].Encounters);
        Assert.Equal(EncounterEndReason.SessionEnd, result[0].Encounters[0].EndReason);
        Assert.Equal(250, result[0].Encounters[0].TotalDamageDealt);
        Assert.Equal(0, result[0].TotalKills); // Not a kill
    }

    [Fact]
    public void MultipleDifferentMobs_TrackedSeparately()
    {
        // Arrange
        var events = new List<LogEvent>
        {
            new DamageEvent(new TimeOnly(10, 0, 0), PlayerName, "spraggonet", 100, "Crush"),
            new DamageEvent(new TimeOnly(10, 0, 1), PlayerName, "wee wolf", 80, "Slash"),
            new DamageEvent(new TimeOnly(10, 0, 2), PlayerName, "spraggonet", 100, "Crush"),
            new DeathEvent(new TimeOnly(10, 0, 3), "spraggonet", PlayerName),
            new DamageEvent(new TimeOnly(10, 0, 4), PlayerName, "wee wolf", 80, "Slash"),
            new DeathEvent(new TimeOnly(10, 0, 5), "wee wolf", PlayerName)
        };

        // Act
        var result = _resolver.ResolveInstances(events, PlayerName);

        // Assert
        Assert.Equal(2, result.Count);

        var spraggonet = result.First(r => r.TargetName == "spraggonet");
        var weeWolf = result.First(r => r.TargetName == "wee wolf");

        Assert.Single(spraggonet.Encounters);
        Assert.Equal(200, spraggonet.TotalDamageDealt);

        Assert.Single(weeWolf.Encounters);
        Assert.Equal(160, weeWolf.TotalDamageDealt);
    }

    [Fact]
    public void IncomingDamage_TrackedAsDamageTaken()
    {
        // Arrange
        var events = new List<LogEvent>
        {
            new DamageEvent(new TimeOnly(10, 0, 0), PlayerName, MobName, 100, "Crush"),
            new DamageEvent(new TimeOnly(10, 0, 1), MobName, PlayerName, 50, "Crush"), // Mob hits player
            new DamageEvent(new TimeOnly(10, 0, 2), PlayerName, MobName, 100, "Crush"),
            new DeathEvent(new TimeOnly(10, 0, 3), MobName, PlayerName)
        };

        // Act
        var result = _resolver.ResolveInstances(events, PlayerName);

        // Assert
        Assert.Single(result);
        var encounter = result[0].Encounters[0];
        Assert.Equal(200, encounter.TotalDamageDealt);
        Assert.Equal(50, encounter.TotalDamageTaken);
    }

    [Fact]
    public void PetDamage_TrackedWithPlayerDamage()
    {
        // Arrange
        var events = new List<LogEvent>
        {
            new DamageEvent(new TimeOnly(10, 0, 0), PlayerName, MobName, 100, "Crush"),
            new PetDamageEvent(new TimeOnly(10, 0, 1), "spirit warrior", MobName, 75),
            new DamageEvent(new TimeOnly(10, 0, 2), PlayerName, MobName, 100, "Crush"),
            new DeathEvent(new TimeOnly(10, 0, 3), MobName, PlayerName)
        };

        // Act
        var result = _resolver.ResolveInstances(events, PlayerName);

        // Assert
        Assert.Single(result);
        Assert.Equal(275, result[0].TotalDamageDealt); // 200 player + 75 pet
    }

    [Fact]
    public void CriticalHits_AddedToDamage()
    {
        // Arrange
        var events = new List<LogEvent>
        {
            new DamageEvent(new TimeOnly(10, 0, 0), PlayerName, MobName, 100, "Crush"),
            new CriticalHitEvent(new TimeOnly(10, 0, 0), MobName, 25, 20), // 25 extra crit damage
            new DamageEvent(new TimeOnly(10, 0, 2), PlayerName, MobName, 100, "Crush"),
            new DeathEvent(new TimeOnly(10, 0, 3), MobName, PlayerName)
        };

        // Act
        var result = _resolver.ResolveInstances(events, PlayerName);

        // Assert
        Assert.Single(result);
        Assert.Equal(225, result[0].TotalDamageDealt); // 200 base + 25 crit
    }

    [Fact]
    public void HealingDuringCombat_TrackedWithEncounter()
    {
        // Arrange
        var events = new List<LogEvent>
        {
            new DamageEvent(new TimeOnly(10, 0, 0), PlayerName, MobName, 100, "Crush"),
            new HealingEvent(new TimeOnly(10, 0, 1), PlayerName, PlayerName, 50),
            new DamageEvent(new TimeOnly(10, 0, 2), PlayerName, MobName, 100, "Crush"),
            new DeathEvent(new TimeOnly(10, 0, 3), MobName, PlayerName)
        };

        // Act
        var result = _resolver.ResolveInstances(events, PlayerName);

        // Assert
        Assert.Single(result);
        var encounter = result[0].Encounters[0];
        Assert.Equal(50, encounter.TotalHealingDone);
    }

    [Fact]
    public void GetAllEncounters_ReturnsChronologicalOrder()
    {
        // Arrange
        var events = new List<LogEvent>
        {
            new DamageEvent(new TimeOnly(10, 0, 0), PlayerName, "mob1", 100, "Crush"),
            new DamageEvent(new TimeOnly(10, 0, 5), PlayerName, "mob2", 100, "Slash"),
            new DeathEvent(new TimeOnly(10, 0, 2), "mob1", PlayerName),
            new DeathEvent(new TimeOnly(10, 0, 7), "mob2", PlayerName)
        };

        // Act
        var encounters = _resolver.GetAllEncounters(events, PlayerName);

        // Assert
        Assert.Equal(2, encounters.Count);
        Assert.True(encounters[0].StartTime < encounters[1].StartTime);
    }

    [Fact]
    public void EncounterDuration_CalculatedCorrectly()
    {
        // Arrange
        var events = new List<LogEvent>
        {
            new DamageEvent(new TimeOnly(10, 0, 0), PlayerName, MobName, 100, "Crush"),
            new DamageEvent(new TimeOnly(10, 0, 5), PlayerName, MobName, 100, "Crush"),
            new DeathEvent(new TimeOnly(10, 0, 10), MobName, PlayerName)
        };

        // Act
        var result = _resolver.ResolveInstances(events, PlayerName);

        // Assert
        var encounter = result[0].Encounters[0];
        Assert.Equal(TimeSpan.FromSeconds(10), encounter.Duration);
    }

    [Fact]
    public void DpsCalculation_Accurate()
    {
        // Arrange - 200 damage over 10 seconds = 20 DPS
        var events = new List<LogEvent>
        {
            new DamageEvent(new TimeOnly(10, 0, 0), PlayerName, MobName, 100, "Crush"),
            new DamageEvent(new TimeOnly(10, 0, 5), PlayerName, MobName, 100, "Crush"),
            new DeathEvent(new TimeOnly(10, 0, 10), MobName, PlayerName)
        };

        // Act
        var result = _resolver.ResolveInstances(events, PlayerName);

        // Assert
        var encounter = result[0].Encounters[0];
        Assert.Equal(20.0, encounter.Dps, 0.01);
    }

    [Fact]
    public void TargetTypeStatistics_AggregatesCorrectly()
    {
        // Arrange - Kill 3 of the same mob type
        var events = new List<LogEvent>
        {
            // Kill 1: 100 damage, 2 seconds
            new DamageEvent(new TimeOnly(10, 0, 0), PlayerName, MobName, 100, "Crush"),
            new DeathEvent(new TimeOnly(10, 0, 2), MobName, PlayerName),

            // Kill 2: 200 damage, 4 seconds
            new DamageEvent(new TimeOnly(10, 0, 5), PlayerName, MobName, 200, "Crush"),
            new DeathEvent(new TimeOnly(10, 0, 9), MobName, PlayerName),

            // Kill 3: 150 damage, 3 seconds
            new DamageEvent(new TimeOnly(10, 0, 15), PlayerName, MobName, 150, "Crush"),
            new DeathEvent(new TimeOnly(10, 0, 18), MobName, PlayerName)
        };

        // Act
        var result = _resolver.ResolveInstances(events, PlayerName);

        // Assert
        Assert.Single(result);
        var stats = result[0];

        Assert.Equal(3, stats.TotalKills);
        Assert.Equal(3, stats.TotalEncounters);
        Assert.Equal(450, stats.TotalDamageDealt);
        Assert.Equal(150.0, stats.AverageDamagePerKill, 0.01);
        Assert.Equal(3.0, stats.AverageTimeToKill, 0.01); // (2+4+3)/3
        Assert.Equal(2.0, stats.FastestKill); // 2 seconds
    }

    [Fact]
    public void CustomTimeoutThreshold_Respected()
    {
        // Arrange - use 5 second timeout
        _resolver.EncounterTimeoutThreshold = TimeSpan.FromSeconds(5);

        var events = new List<LogEvent>
        {
            new DamageEvent(new TimeOnly(10, 0, 0), PlayerName, MobName, 100, "Crush"),
            // 6 second gap - should timeout with 5s threshold
            new DamageEvent(new TimeOnly(10, 0, 6), PlayerName, MobName, 100, "Crush"),
            new DeathEvent(new TimeOnly(10, 0, 8), MobName, PlayerName)
        };

        // Act
        var result = _resolver.ResolveInstances(events, PlayerName);

        // Assert
        Assert.Equal(2, result[0].Encounters.Count);
        Assert.Equal(EncounterEndReason.Timeout, result[0].Encounters[0].EndReason);
        Assert.Equal(EncounterEndReason.Death, result[0].Encounters[1].EndReason);
    }

    [Fact]
    public void CaseInsensitiveTargetMatching()
    {
        // Arrange - different case for same mob name
        var events = new List<LogEvent>
        {
            new DamageEvent(new TimeOnly(10, 0, 0), PlayerName, "Spraggonet", 100, "Crush"),
            new DamageEvent(new TimeOnly(10, 0, 1), PlayerName, "SPRAGGONET", 100, "Crush"),
            new DeathEvent(new TimeOnly(10, 0, 2), "spraggonet", PlayerName)
        };

        // Act
        var result = _resolver.ResolveInstances(events, PlayerName);

        // Assert
        Assert.Single(result);
        Assert.Single(result[0].Encounters);
        Assert.Equal(200, result[0].TotalDamageDealt);
    }

    [Fact]
    public void EmptyEvents_ReturnsEmptyResult()
    {
        // Act
        var result = _resolver.ResolveInstances(new List<LogEvent>(), PlayerName);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void DeathWithoutPriorDamage_CreatesMinimalEncounter()
    {
        // Arrange - just a death event with no prior damage tracked
        var events = new List<LogEvent>
        {
            new DeathEvent(new TimeOnly(10, 0, 0), MobName, PlayerName)
        };

        // Act
        var result = _resolver.ResolveInstances(events, PlayerName);

        // Assert
        Assert.Single(result);
        Assert.Equal(1, result[0].TotalKills);
        Assert.Equal(0, result[0].TotalDamageDealt);
    }
}
