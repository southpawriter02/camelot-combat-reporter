namespace CamelotCombatReporter.Core.Models;

/// <summary>
/// Aggregated combat statistics calculated from a combat log.
/// </summary>
/// <param name="DurationMinutes">
/// Total duration of the combat session in minutes.
/// Calculated from the first to last combat event timestamp.
/// </param>
/// <param name="TotalDamage">
/// Sum of all damage dealt during the session.
/// </param>
/// <param name="Dps">
/// Damage per second. Calculated as TotalDamage / (DurationMinutes * 60).
/// </param>
/// <param name="AverageDamage">
/// Mean damage per hit across all damage events.
/// </param>
/// <param name="MedianDamage">
/// Median damage value, representing the middle value when all hits are sorted.
/// Useful for understanding typical damage when outliers are present.
/// </param>
/// <param name="CombatStylesCount">
/// Number of unique combat styles used during the session.
/// </param>
/// <param name="SpellsCastCount">
/// Number of unique spells cast during the session.
/// </param>
public record CombatStatistics(
    double DurationMinutes,
    int TotalDamage,
    double Dps,
    double AverageDamage,
    double MedianDamage,
    int CombatStylesCount,
    int SpellsCastCount
);
