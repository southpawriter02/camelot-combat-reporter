namespace CamelotCombatReporter.Core.Models;

public record CombatStatistics(
    double DurationMinutes,
    int TotalDamage,
    double Dps,
    double AverageDamage,
    double MedianDamage,
    int CombatStylesCount,
    int SpellsCastCount
);
