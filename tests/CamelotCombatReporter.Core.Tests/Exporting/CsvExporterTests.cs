using CamelotCombatReporter.Core.Exporting;
using CamelotCombatReporter.Core.Models;
using Xunit;

namespace CamelotCombatReporter.Core.Tests.Exporting;

public class CsvExporterTests
{
    [Fact]
    public void GenerateCsv_ShouldIncludeStatisticsAndEvents()
    {
        // Arrange
        var exporter = new CsvExporter();
        var stats = new CombatStatistics(
            DurationMinutes: 1.5,
            TotalDamage: 1000,
            Dps: 11.11,
            AverageDamage: 50,
            MedianDamage: 45,
            CombatStylesCount: 5,
            SpellsCastCount: 2
        );

        var events = new List<LogEvent>
        {
            new DamageEvent(new TimeOnly(12, 0, 0), "Me", "Enemy", 100, "Slash"),
            new SpellCastEvent(new TimeOnly(12, 0, 1), "Me", "Enemy", "Fireball")
        };

        // Act
        var csv = exporter.GenerateCsv(stats, events);

        // Assert
        Assert.Contains("--- Combat Statistics ---", csv);
        Assert.Contains("Duration (min),1.50", csv);
        Assert.Contains("Total Damage,1000", csv);
        Assert.Contains("--- Combat Log ---", csv);
        Assert.Contains("12:00:00,Damage,Me,Enemy,100,Slash", csv);
        Assert.Contains("12:00:01,Spell,Me,Enemy,,Fireball", csv);
    }
}
