using System.Text;
using CamelotCombatReporter.Core.Models;

namespace CamelotCombatReporter.Core.Exporting;

public class CsvExporter
{
    public string GenerateCsv(CombatStatistics stats, IEnumerable<LogEvent> events)
    {
        var sb = new StringBuilder();

        // Summary
        sb.AppendLine("--- Combat Statistics ---");
        sb.AppendLine($"Duration (min),{stats.DurationMinutes:F2}");
        sb.AppendLine($"Total Damage,{stats.TotalDamage}");
        sb.AppendLine($"DPS,{stats.Dps:F2}");
        sb.AppendLine($"Average Damage,{stats.AverageDamage:F2}");
        sb.AppendLine($"Median Damage,{stats.MedianDamage:F2}");
        sb.AppendLine($"Combat Styles,{stats.CombatStylesCount}");
        sb.AppendLine($"Spells Cast,{stats.SpellsCastCount}");
        sb.AppendLine();

        // Detailed Log
        sb.AppendLine("--- Combat Log ---");
        sb.AppendLine("Timestamp,Type,Source,Target,Amount,Details");

        foreach (var ev in events)
        {
            var line = FormatEvent(ev);
            if (!string.IsNullOrEmpty(line))
            {
                sb.AppendLine(line);
            }
        }

        return sb.ToString();
    }

    private string FormatEvent(LogEvent ev)
    {
        return ev switch
        {
            DamageEvent de => $"{de.Timestamp:HH:mm:ss},Damage,{de.Source},{de.Target},{de.DamageAmount},{de.DamageType}",
            HealingEvent he => $"{he.Timestamp:HH:mm:ss},Healing,{he.Source},{he.Target},{he.HealingAmount},",
            CombatStyleEvent cse => $"{cse.Timestamp:HH:mm:ss},Style,{cse.Source},{cse.Target},,{cse.StyleName}",
            SpellCastEvent sce => $"{sce.Timestamp:HH:mm:ss},Spell,{sce.Source},{sce.Target},,{sce.SpellName}",
            _ => $"{ev.Timestamp:HH:mm:ss},{ev.GetType().Name},,,,"
        };
    }
}
