using CamelotCombatReporter.Core.Models;
using CamelotCombatReporter.Core.Parsing;

if (args.Length == 0)
{
    Console.WriteLine("Error: Please provide the path to a log file.");
    Console.WriteLine("Usage: dotnet run --project src/CamelotCombatReporter.Cli -- <path_to_log_file>");
    return 1;
}

var logFilePath = args[0];
var logParser = new LogParser(logFilePath);
var events = logParser.Parse().ToList();

if (events.Count == 0)
{
    Console.WriteLine("No combat events found in the log.");
    return 0;
}

// Analysis
var firstEventTime = events.First().Timestamp;
var lastEventTime = events.Last().Timestamp;
var duration = lastEventTime - firstEventTime;

var combatStyleCount = events.OfType<CombatStyleEvent>().Count();
var spellCastCount = events.OfType<SpellCastEvent>().Count();

var damageDealtEvents = events.OfType<DamageEvent>().Where(e => e.Source == "You").ToList();
var totalDamageDealt = damageDealtEvents.Sum(e => e.DamageAmount);
var damageDealtAmounts = damageDealtEvents.Select(e => e.DamageAmount).OrderBy(d => d).ToList();

double damageMedian = 0;
if (damageDealtAmounts.Count > 0)
{
    var mid = damageDealtAmounts.Count / 2;
    damageMedian = (damageDealtAmounts.Count % 2 != 0)
        ? damageDealtAmounts[mid]
        : (damageDealtAmounts[mid - 1] + damageDealtAmounts[mid]) / 2.0;
}

var damageAverage = damageDealtEvents.Count > 0 ? totalDamageDealt / (double)damageDealtEvents.Count : 0;
var dps = duration.TotalSeconds > 0 ? totalDamageDealt / duration.TotalSeconds : 0;

// Reporting
Console.WriteLine("--- Combat Report ---");
Console.WriteLine($"Log Duration: {duration.TotalMinutes:F2} minutes");
Console.WriteLine("\n--- Player Statistics ---");
Console.WriteLine($"Total Damage Dealt: {totalDamageDealt}");
Console.WriteLine($"Damage Per Second (DPS): {dps:F2}");
Console.WriteLine($"Average Damage: {damageAverage:F2}");
Console.WriteLine($"Median Damage: {damageMedian:F2}");
Console.WriteLine($"Combat Styles Used: {combatStyleCount}");
Console.WriteLine($"Spells Cast: {spellCastCount}");
Console.WriteLine("\n--- End of Report ---");

return 0;
