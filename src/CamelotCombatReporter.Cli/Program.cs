using CamelotCombatReporter.Core.Analysis;
using CamelotCombatReporter.Core.Parsing;
using System;
using System.Linq;

if (args.Length == 0)
{
    Console.WriteLine("Please provide a path to the log file.");
    return;
}

var logParser = new LogParser(args[0]);
var events = logParser.Parse();

var analysis = new CombatAnalysis(events);
var fights = analysis.Analyze();

Console.WriteLine($"Found {fights.Count} fights.");
Console.WriteLine();

for (int i = 0; i < fights.Count; i++)
{
    var fight = fights[i];
    Console.WriteLine($"--- Fight {i + 1} ---");
    Console.WriteLine($"Duration: {fight.Duration}");
    Console.WriteLine($"Total Damage: {fight.TotalDamage}");
    Console.WriteLine($"DPS: {fight.Dps:F2}");
    Console.WriteLine($"Total Healing: {fight.TotalHealing}");
    Console.WriteLine($"HPS: {fight.Hps:F2}");
    Console.WriteLine();
}
