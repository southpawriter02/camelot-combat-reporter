using CamelotCombatReporter.Core.Models;

namespace CamelotCombatReporter.Plugins.Abstractions;

/// <summary>
/// Interface for plugins that provide custom export formats.
/// </summary>
public interface IExportPlugin : IPlugin
{
    /// <summary>
    /// File extension for the export format (e.g., ".xml", ".html", ".pdf").
    /// </summary>
    string FileExtension { get; }

    /// <summary>
    /// MIME type for the export format.
    /// </summary>
    string MimeType { get; }

    /// <summary>
    /// Human-readable format name for file dialogs.
    /// </summary>
    string FormatDisplayName { get; }

    /// <summary>
    /// Export options schema for configuration UI.
    /// </summary>
    IReadOnlyCollection<ExportOptionDefinition> ExportOptions { get; }

    /// <summary>
    /// Exports the combat data to the specified format.
    /// </summary>
    Task<ExportResult> ExportAsync(
        ExportContext context,
        Stream outputStream,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Context provided to export plugins containing all necessary data.
/// </summary>
/// <param name="Statistics">Combat statistics summary.</param>
/// <param name="Events">All parsed events.</param>
/// <param name="FilteredEvents">Events after applying filters.</param>
/// <param name="Options">User-specified export options.</param>
/// <param name="CombatantName">Name of the player/combatant.</param>
/// <param name="CombatStyles">Combat style usage details.</param>
/// <param name="Spells">Spell cast details.</param>
public record ExportContext(
    CombatStatistics? Statistics,
    IReadOnlyList<LogEvent> Events,
    IReadOnlyList<LogEvent> FilteredEvents,
    IReadOnlyDictionary<string, object> Options,
    string CombatantName,
    IReadOnlyList<CombatStyleInfo> CombatStyles,
    IReadOnlyList<SpellCastInfo> Spells);

/// <summary>
/// Combat style usage information for export.
/// </summary>
/// <param name="StyleName">Name of the combat style.</param>
/// <param name="Count">Number of times used.</param>
public record CombatStyleInfo(string StyleName, int Count);

/// <summary>
/// Spell cast information for export.
/// </summary>
/// <param name="SpellName">Name of the spell.</param>
/// <param name="Count">Number of times cast.</param>
public record SpellCastInfo(string SpellName, int Count);

/// <summary>
/// Result from an export operation.
/// </summary>
/// <param name="Success">Whether the export succeeded.</param>
/// <param name="ErrorMessage">Error message if failed.</param>
/// <param name="BytesWritten">Number of bytes written.</param>
public record ExportResult(
    bool Success,
    string? ErrorMessage,
    long BytesWritten)
{
    public static ExportResult Succeeded(long bytesWritten) =>
        new(true, null, bytesWritten);

    public static ExportResult Failed(string error) =>
        new(false, error, 0);
}

/// <summary>
/// Defines an export option that can be configured by the user.
/// </summary>
/// <param name="Id">Unique identifier for the option.</param>
/// <param name="Name">Display name.</param>
/// <param name="Description">Description of what this option does.</param>
/// <param name="ValueType">Type of value (string, bool, int, etc.).</param>
/// <param name="DefaultValue">Default value.</param>
/// <param name="Required">Whether this option is required.</param>
public record ExportOptionDefinition(
    string Id,
    string Name,
    string Description,
    Type ValueType,
    object? DefaultValue,
    bool Required);
