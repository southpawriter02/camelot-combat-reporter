using System.Collections.Concurrent;
using System.Text.Json;
using System.Threading.Channels;
using CamelotCombatReporter.Plugins.Sandbox;

namespace CamelotCombatReporter.Plugins.Security;

/// <summary>
/// Security audit logger interface.
/// </summary>
public interface ISecurityAuditLogger
{
    void LogAccess(string pluginId, SecurityAction action, string resource);
    void LogViolation(string pluginId, SecurityAction action, string resource);
    void LogSecurityEvent(string pluginId, SecurityEventType eventType, string details);
    void LogPluginMessage(string pluginId, LogLevel level, string message, Exception? exception = null);
    void LogPluginLifecycle(string pluginId, PluginLifecycleEvent lifecycleEvent, string? details = null);

    IAsyncEnumerable<SecurityAuditEntry> GetRecentEntriesAsync(
        string? pluginId = null,
        SecuritySeverity? minSeverity = null,
        int maxEntries = 100,
        CancellationToken ct = default);
}

/// <summary>
/// File-based security audit logger.
/// </summary>
public sealed class SecurityAuditLogger : ISecurityAuditLogger, IAsyncDisposable
{
    private readonly string _logDirectory;
    private readonly Channel<SecurityAuditEntry> _logChannel;
    private readonly ConcurrentQueue<SecurityAuditEntry> _recentEntries;
    private readonly int _maxRecentEntries;
    private readonly Task _writerTask;
    private readonly CancellationTokenSource _cts;
    private bool _disposed;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false
    };

    public SecurityAuditLogger(string logDirectory, int maxRecentEntries = 1000)
    {
        _logDirectory = logDirectory;
        _maxRecentEntries = maxRecentEntries;
        _recentEntries = new ConcurrentQueue<SecurityAuditEntry>();
        _logChannel = Channel.CreateBounded<SecurityAuditEntry>(
            new BoundedChannelOptions(1000)
            {
                FullMode = BoundedChannelFullMode.DropOldest
            });
        _cts = new CancellationTokenSource();

        Directory.CreateDirectory(_logDirectory);
        _writerTask = StartWriterAsync(_cts.Token);
    }

    public void LogAccess(string pluginId, SecurityAction action, string resource)
    {
        var entry = new SecurityAuditEntry
        {
            Timestamp = DateTime.UtcNow,
            PluginId = pluginId,
            EventType = SecurityEventType.ResourceAccess,
            Action = action,
            Resource = resource,
            Severity = SecuritySeverity.Info,
            WasAllowed = true
        };

        EnqueueEntry(entry);
    }

    public void LogViolation(string pluginId, SecurityAction action, string resource)
    {
        var entry = new SecurityAuditEntry
        {
            Timestamp = DateTime.UtcNow,
            PluginId = pluginId,
            EventType = SecurityEventType.AccessViolation,
            Action = action,
            Resource = resource,
            Severity = SecuritySeverity.Warning,
            WasAllowed = false
        };

        EnqueueEntry(entry);
    }

    public void LogSecurityEvent(string pluginId, SecurityEventType eventType, string details)
    {
        var severity = eventType switch
        {
            SecurityEventType.SignatureValid => SecuritySeverity.Info,
            SecurityEventType.UnsignedPlugin => SecuritySeverity.Warning,
            SecurityEventType.InvalidSignature => SecuritySeverity.Critical,
            SecurityEventType.UnknownSigningKey => SecuritySeverity.Warning,
            SecurityEventType.SignatureError => SecuritySeverity.Error,
            SecurityEventType.PluginTerminated => SecuritySeverity.Warning,
            SecurityEventType.ResourceLimitExceeded => SecuritySeverity.Warning,
            _ => SecuritySeverity.Info
        };

        var entry = new SecurityAuditEntry
        {
            Timestamp = DateTime.UtcNow,
            PluginId = pluginId,
            EventType = eventType,
            Details = details,
            Severity = severity
        };

        EnqueueEntry(entry);
    }

    public void LogPluginMessage(string pluginId, LogLevel level, string message, Exception? exception = null)
    {
        var severity = level switch
        {
            LogLevel.Debug => SecuritySeverity.Debug,
            LogLevel.Info => SecuritySeverity.Info,
            LogLevel.Warning => SecuritySeverity.Warning,
            LogLevel.Error => SecuritySeverity.Error,
            _ => SecuritySeverity.Info
        };

        var entry = new SecurityAuditEntry
        {
            Timestamp = DateTime.UtcNow,
            PluginId = pluginId,
            EventType = SecurityEventType.PluginLog,
            Details = exception != null ? $"{message}\n{exception}" : message,
            Severity = severity
        };

        EnqueueEntry(entry);
    }

    public void LogPluginLifecycle(string pluginId, PluginLifecycleEvent lifecycleEvent, string? details = null)
    {
        var entry = new SecurityAuditEntry
        {
            Timestamp = DateTime.UtcNow,
            PluginId = pluginId,
            EventType = SecurityEventType.PluginLifecycle,
            Details = details != null ? $"{lifecycleEvent}: {details}" : lifecycleEvent.ToString(),
            Severity = SecuritySeverity.Info
        };

        EnqueueEntry(entry);
    }

    public async IAsyncEnumerable<SecurityAuditEntry> GetRecentEntriesAsync(
        string? pluginId = null,
        SecuritySeverity? minSeverity = null,
        int maxEntries = 100,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        var count = 0;
        foreach (var entry in _recentEntries.Reverse())
        {
            if (ct.IsCancellationRequested) yield break;
            if (count >= maxEntries) yield break;

            if (pluginId != null && entry.PluginId != pluginId) continue;
            if (minSeverity.HasValue && entry.Severity < minSeverity.Value) continue;

            count++;
            yield return entry;
        }

        await Task.CompletedTask;
    }

    private void EnqueueEntry(SecurityAuditEntry entry)
    {
        _recentEntries.Enqueue(entry);
        while (_recentEntries.Count > _maxRecentEntries)
        {
            _recentEntries.TryDequeue(out _);
        }

        _logChannel.Writer.TryWrite(entry);
    }

    private async Task StartWriterAsync(CancellationToken ct)
    {
        var logFilePath = Path.Combine(_logDirectory, $"security-audit-{DateTime.UtcNow:yyyyMMdd}.log");

        try
        {
            await using var writer = new StreamWriter(logFilePath, append: true);

            await foreach (var entry in _logChannel.Reader.ReadAllAsync(ct))
            {
                var line = JsonSerializer.Serialize(entry, JsonOptions);
                await writer.WriteLineAsync(line);
                await writer.FlushAsync(ct);
            }
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        _cts.Cancel();
        _logChannel.Writer.Complete();

        try
        {
            await _writerTask;
        }
        catch (OperationCanceledException)
        {
            // Expected
        }

        _cts.Dispose();
    }
}

/// <summary>
/// Security audit entry.
/// </summary>
public sealed record SecurityAuditEntry
{
    public DateTime Timestamp { get; init; }
    public required string PluginId { get; init; }
    public SecurityEventType EventType { get; init; }
    public SecurityAction? Action { get; init; }
    public string? Resource { get; init; }
    public string? Details { get; init; }
    public SecuritySeverity Severity { get; init; }
    public bool? WasAllowed { get; init; }
}

/// <summary>
/// Types of security events.
/// </summary>
public enum SecurityEventType
{
    ResourceAccess,
    AccessViolation,
    ResourceLimitExceeded,
    SignatureValid,
    InvalidSignature,
    UnsignedPlugin,
    UnknownSigningKey,
    SignatureError,
    PluginLifecycle,
    PluginTerminated,
    PluginError,
    PluginLog
}

/// <summary>
/// Security actions that can be logged.
/// </summary>
public enum SecurityAction
{
    FileRead,
    FileWrite,
    NetworkConnect,
    NetworkSend,
    UiModification,
    DataAccess,
    PreferencesRead,
    PreferencesWrite
}

/// <summary>
/// Security severity levels.
/// </summary>
public enum SecuritySeverity
{
    Debug = 0,
    Info = 1,
    Warning = 2,
    Error = 3,
    Critical = 4
}

/// <summary>
/// Plugin lifecycle events.
/// </summary>
public enum PluginLifecycleEvent
{
    Loading,
    Loaded,
    Initializing,
    Initialized,
    Enabling,
    Enabled,
    Disabling,
    Disabled,
    Unloading,
    Unloaded,
    Error
}
