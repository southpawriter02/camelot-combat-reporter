using CamelotCombatReporter.Core.Parsing;

// .NET 6+ top-level statements mean we don't need a Program class and Main method.
// The 'args' array is automatically available for command-line arguments.

if (args.Length == 0)
{
    Console.WriteLine("Error: Please provide the path to a log file.");
    Console.WriteLine("Usage: dotnet run --project src/CamelotCombatReporter.Cli -- <path_to_log_file>");
    return 1; // Return a non-zero exit code to indicate an error
}

var logFilePath = args[0];

var logParser = new LogParser(logFilePath);
var eventsFound = 0;

foreach (var logEvent in logParser.Parse())
{
    // C# records provide a default ToString() implementation, which is great for quick output.
    Console.WriteLine(logEvent);
    eventsFound++;
}

Console.WriteLine($"\nParsing complete. Found {eventsFound} event(s).");
return 0;
