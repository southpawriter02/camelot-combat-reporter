using Microsoft.Extensions.Logging;

namespace CamelotCombatReporter.Core.Logging;

/// <summary>
/// Extension methods for logging operations throughout the application.
/// Provides high-performance logging using source generators.
/// </summary>
public static partial class LoggingExtensions
{
    // ==================== Parsing ====================

    /// <summary>
    /// Logs the start of parsing a log file.
    /// </summary>
    [LoggerMessage(Level = LogLevel.Information, Message = "Starting to parse log file: {FilePath}")]
    public static partial void LogParsingStarted(this ILogger logger, string filePath);

    /// <summary>
    /// Logs successful completion of parsing.
    /// </summary>
    [LoggerMessage(Level = LogLevel.Information, Message = "Parsed {EventCount} events from log file in {ElapsedMs}ms")]
    public static partial void LogParsingCompleted(this ILogger logger, int eventCount, long elapsedMs);

    /// <summary>
    /// Logs a parsing error for a specific line.
    /// </summary>
    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to parse line {LineNumber}: {Line}")]
    public static partial void LogParsingLineError(this ILogger logger, int lineNumber, string line);

    /// <summary>
    /// Logs a file not found error.
    /// </summary>
    [LoggerMessage(Level = LogLevel.Error, Message = "Log file not found: {FilePath}")]
    public static partial void LogFileNotFound(this ILogger logger, string filePath);

    // ==================== Loot Tracking ====================

    /// <summary>
    /// Logs the saving of a loot session.
    /// </summary>
    [LoggerMessage(Level = LogLevel.Information, Message = "Saving loot session with {EventCount} events from {LogFile}")]
    public static partial void LogSavingLootSession(this ILogger logger, int eventCount, string logFile);

    /// <summary>
    /// Logs successful loot session save.
    /// </summary>
    [LoggerMessage(Level = LogLevel.Information, Message = "Loot session saved: {SessionId} with {ItemDrops} item drops, {Currency} currency")]
    public static partial void LogLootSessionSaved(this ILogger logger, Guid sessionId, int itemDrops, string currency);

    /// <summary>
    /// Logs loading of loot statistics.
    /// </summary>
    [LoggerMessage(Level = LogLevel.Debug, Message = "Loading loot statistics for mob: {MobName}")]
    public static partial void LogLoadingMobStats(this ILogger logger, string mobName);

    /// <summary>
    /// Logs an error during loot session loading.
    /// </summary>
    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to load loot session from {FilePath}")]
    public static partial void LogLootSessionLoadError(this ILogger logger, string filePath, Exception ex);

    /// <summary>
    /// Logs cache rebuild start.
    /// </summary>
    [LoggerMessage(Level = LogLevel.Information, Message = "Rebuilding loot tracking caches from {SessionCount} sessions")]
    public static partial void LogCacheRebuildStarted(this ILogger logger, int sessionCount);

    /// <summary>
    /// Logs cache rebuild completion.
    /// </summary>
    [LoggerMessage(Level = LogLevel.Information, Message = "Cache rebuild completed in {ElapsedMs}ms")]
    public static partial void LogCacheRebuildCompleted(this ILogger logger, long elapsedMs);

    // ==================== Cross-Realm ====================

    /// <summary>
    /// Logs saving a cross-realm session.
    /// </summary>
    [LoggerMessage(Level = LogLevel.Information, Message = "Saving cross-realm session for {CharacterName} ({Realm})")]
    public static partial void LogSavingCrossRealmSession(this ILogger logger, string characterName, string realm);

    /// <summary>
    /// Logs successful cross-realm session save.
    /// </summary>
    [LoggerMessage(Level = LogLevel.Information, Message = "Cross-realm session saved: {SessionId}")]
    public static partial void LogCrossRealmSessionSaved(this ILogger logger, Guid sessionId);

    /// <summary>
    /// Logs an error during cross-realm session loading.
    /// </summary>
    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to load cross-realm session from {FilePath}")]
    public static partial void LogCrossRealmSessionLoadError(this ILogger logger, string filePath, Exception ex);

    /// <summary>
    /// Logs loading cross-realm sessions with filter.
    /// </summary>
    [LoggerMessage(Level = LogLevel.Debug, Message = "Loading cross-realm sessions with filter: Realm={Realm}, Class={Class}")]
    public static partial void LogLoadingCrossRealmSessions(this ILogger logger, string? realm, string? @class);

    // ==================== Export ====================

    /// <summary>
    /// Logs export start.
    /// </summary>
    [LoggerMessage(Level = LogLevel.Information, Message = "Exporting data to {Format} format: {FilePath}")]
    public static partial void LogExportStarted(this ILogger logger, string format, string filePath);

    /// <summary>
    /// Logs export completion.
    /// </summary>
    [LoggerMessage(Level = LogLevel.Information, Message = "Export completed: {RecordCount} records written to {FilePath}")]
    public static partial void LogExportCompleted(this ILogger logger, int recordCount, string filePath);

    /// <summary>
    /// Logs an export error.
    /// </summary>
    [LoggerMessage(Level = LogLevel.Error, Message = "Export failed for {FilePath}")]
    public static partial void LogExportError(this ILogger logger, string filePath, Exception ex);

    // ==================== Service Lifecycle ====================

    /// <summary>
    /// Logs service initialization start.
    /// </summary>
    [LoggerMessage(Level = LogLevel.Debug, Message = "Initializing {ServiceName}")]
    public static partial void LogServiceInitializing(this ILogger logger, string serviceName);

    /// <summary>
    /// Logs service initialization completion.
    /// </summary>
    [LoggerMessage(Level = LogLevel.Information, Message = "{ServiceName} initialized in {ElapsedMs}ms")]
    public static partial void LogServiceInitialized(this ILogger logger, string serviceName, long elapsedMs);

    /// <summary>
    /// Logs service disposal.
    /// </summary>
    [LoggerMessage(Level = LogLevel.Debug, Message = "Disposing {ServiceName}")]
    public static partial void LogServiceDisposing(this ILogger logger, string serviceName);

    // ==================== General Errors ====================

    /// <summary>
    /// Logs an unexpected error.
    /// </summary>
    [LoggerMessage(Level = LogLevel.Error, Message = "Unexpected error in {Operation}")]
    public static partial void LogUnexpectedError(this ILogger logger, string operation, Exception ex);

    /// <summary>
    /// Logs a warning about recoverable issues.
    /// </summary>
    [LoggerMessage(Level = LogLevel.Warning, Message = "{Message}")]
    public static partial void LogWarning(this ILogger logger, string message);

    /// <summary>
    /// Logs debug information.
    /// </summary>
    [LoggerMessage(Level = LogLevel.Debug, Message = "{Message}")]
    public static partial void LogDebug(this ILogger logger, string message);

    // ==================== File Operations ====================

    /// <summary>
    /// Logs skipping a corrupted session file.
    /// </summary>
    [LoggerMessage(Level = LogLevel.Warning, Message = "Skipping corrupted session file: {FilePath}")]
    public static partial void LogCorruptedSessionFile(this ILogger logger, string filePath, Exception ex);

    /// <summary>
    /// Logs an index rebuild start.
    /// </summary>
    [LoggerMessage(Level = LogLevel.Information, Message = "Rebuilding session index from {FileCount} files")]
    public static partial void LogIndexRebuildStarted(this ILogger logger, int fileCount);

    /// <summary>
    /// Logs index load failure.
    /// </summary>
    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to load index file, creating new index: {FilePath}")]
    public static partial void LogIndexLoadFailed(this ILogger logger, string filePath, Exception ex);

    // ==================== Preferences ====================

    /// <summary>
    /// Logs loading user preferences.
    /// </summary>
    [LoggerMessage(Level = LogLevel.Debug, Message = "Loading user preferences from {FilePath}")]
    public static partial void LogLoadingPreferences(this ILogger logger, string filePath);

    /// <summary>
    /// Logs preferences load failure.
    /// </summary>
    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to load preferences, using defaults")]
    public static partial void LogPreferencesLoadFailed(this ILogger logger, Exception ex);

    /// <summary>
    /// Logs preferences save failure.
    /// </summary>
    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to save preferences")]
    public static partial void LogPreferencesSaveFailed(this ILogger logger, Exception ex);

    // ==================== GUI Operations ====================

    /// <summary>
    /// Logs analyzing a log file.
    /// </summary>
    [LoggerMessage(Level = LogLevel.Information, Message = "Analyzing log file: {FilePath}")]
    public static partial void LogAnalyzingFile(this ILogger logger, string filePath);

    /// <summary>
    /// Logs analysis completion.
    /// </summary>
    [LoggerMessage(Level = LogLevel.Information, Message = "Analysis completed: {EventCount} events, {Duration}s duration, {Dps} DPS")]
    public static partial void LogAnalysisCompleted(this ILogger logger, int eventCount, string duration, string dps);

    /// <summary>
    /// Logs analysis error.
    /// </summary>
    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to analyze log file: {FilePath}")]
    public static partial void LogAnalysisError(this ILogger logger, string filePath, Exception ex);

    /// <summary>
    /// Logs a file selection.
    /// </summary>
    [LoggerMessage(Level = LogLevel.Debug, Message = "File selected: {FilePath}")]
    public static partial void LogFileSelected(this ILogger logger, string filePath);
}
