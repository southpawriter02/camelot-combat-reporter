using CamelotCombatReporter.Plugins.Abstractions;

namespace CamelotCombatReporter.PluginSdk;

/// <summary>
/// Base class for export format plugins.
/// Extend this class to create custom export formats (XML, HTML, PDF, etc.).
/// </summary>
public abstract class ExportPluginBase : PluginBase, IExportPlugin
{
    /// <inheritdoc/>
    public sealed override PluginType Type => PluginType.ExportFormat;

    /// <summary>
    /// File extension for the export format (e.g., ".xml", ".html", ".pdf").
    /// </summary>
    public abstract string FileExtension { get; }

    /// <summary>
    /// MIME type for the export format.
    /// </summary>
    public abstract string MimeType { get; }

    /// <summary>
    /// Human-readable format name for file dialogs.
    /// </summary>
    public abstract string FormatDisplayName { get; }

    /// <summary>
    /// Export options that can be configured by the user.
    /// Override to provide configurable options.
    /// </summary>
    public virtual IReadOnlyCollection<ExportOptionDefinition> ExportOptions { get; } =
        Array.Empty<ExportOptionDefinition>();

    /// <summary>
    /// Exports the data to the specified format.
    /// </summary>
    public abstract Task<ExportResult> ExportAsync(
        ExportContext context,
        Stream outputStream,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a successful export result.
    /// </summary>
    protected ExportResult Success(long bytesWritten)
    {
        return ExportResult.Succeeded(bytesWritten);
    }

    /// <summary>
    /// Creates a failed export result.
    /// </summary>
    protected ExportResult Failure(string error)
    {
        return ExportResult.Failed(error);
    }

    /// <summary>
    /// Creates an export option definition.
    /// </summary>
    protected ExportOptionDefinition Option(
        string id,
        string name,
        string description,
        Type valueType,
        object? defaultValue = null,
        bool required = false)
    {
        return new ExportOptionDefinition(id, name, description, valueType, defaultValue, required);
    }

    /// <summary>
    /// Creates a boolean export option.
    /// </summary>
    protected ExportOptionDefinition BoolOption(
        string id,
        string name,
        string description,
        bool defaultValue = false)
    {
        return new ExportOptionDefinition(id, name, description, typeof(bool), defaultValue, false);
    }

    /// <summary>
    /// Creates a string export option.
    /// </summary>
    protected ExportOptionDefinition StringOption(
        string id,
        string name,
        string description,
        string? defaultValue = null,
        bool required = false)
    {
        return new ExportOptionDefinition(id, name, description, typeof(string), defaultValue, required);
    }

    /// <summary>
    /// Writes text to the output stream with UTF-8 encoding.
    /// </summary>
    protected async Task<long> WriteTextAsync(
        Stream stream,
        string content,
        CancellationToken ct = default)
    {
        using var writer = new StreamWriter(stream, leaveOpen: true);
        await writer.WriteAsync(content.AsMemory(), ct);
        await writer.FlushAsync(ct);
        return stream.Position;
    }
}
