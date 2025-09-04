using CamelotCombatReporter.Core.Analysis;
using CamelotCombatReporter.Core.Models;
using System;
using System.Collections.Generic;
using Xunit;

namespace CamelotCombatReporter.Core.Tests.Analysis
{
    public class CombatAnalysisTests
    {
        [Fact]
        public void Analyze_WithEmptyEvents_ReturnsNoFights()
        {
            // Arrange
            var events = new List<LogEvent>();
            var analysis = new CombatAnalysis(events);

            // Act
            var fights = analysis.Analyze();

            // Assert
            Assert.Empty(fights);
        }

        [Fact]
        public void Analyze_WithSingleFight_GroupsEventsCorrectly()
        {
            // Arrange
            var events = new List<LogEvent>
            {
                new DamageEvent(new TimeOnly(10, 0, 0), "You", "Enemy", 10, "Slash"),
                new HealingEvent(new TimeOnly(10, 0, 2), "You", "You", 20),
                new DamageEvent(new TimeOnly(10, 0, 5), "You", "Enemy", 15, "Slash")
            };
            var analysis = new CombatAnalysis(events);

            // Act
            var fights = analysis.Analyze();

            // Assert
            Assert.Single(fights);
            Assert.Equal(3, fights[0].Events.Count);
        }

        [Fact]
        public void Analyze_WithMultipleFights_SeparatedByInactivity()
        {
            // Arrange
            var events = new List<LogEvent>
            {
                // Fight 1
                new DamageEvent(new TimeOnly(10, 0, 0), "You", "Enemy1", 10, "Slash"),
                new DamageEvent(new TimeOnly(10, 0, 2), "You", "Enemy1", 12, "Slash"),

                // Inactivity
                new DamageEvent(new TimeOnly(10, 0, 15), "You", "Enemy2", 20, "Crush"),
                new DamageEvent(new TimeOnly(10, 0, 18), "You", "Enemy2", 25, "Crush"),
            };
            var analysis = new CombatAnalysis(events);

            // Act
            var fights = analysis.Analyze();

            // Assert
            Assert.Equal(2, fights.Count);
            Assert.Equal(2, fights[0].Events.Count);
            Assert.Equal(2, fights[1].Events.Count);
        }

        [Fact]
        public void Fight_CalculatesStatisticsCorrectly()
        {
            // Arrange
            var events = new List<LogEvent>
            {
                new DamageEvent(new TimeOnly(10, 0, 0), "You", "Enemy", 50, "Slash"),
                new HealingEvent(new TimeOnly(10, 0, 2), "You", "You", 30),
                new DamageEvent(new TimeOnly(10, 0, 4), "You", "Enemy", 70, "Slash")
            };
            var fight = new Fight();
            fight.Events.AddRange(events);

            // Act & Assert
            Assert.Equal(120, fight.TotalDamage);
            Assert.Equal(30, fight.TotalHealing);
            Assert.Equal(TimeSpan.FromSeconds(4), fight.Duration);
            Assert.Equal(30, fight.Dps);
            Assert.Equal(7.5, fight.Hps);
        }
    }
}
