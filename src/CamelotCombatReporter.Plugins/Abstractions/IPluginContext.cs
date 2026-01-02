using CamelotCombatReporter.Plugins.Permissions;

namespace CamelotCombatReporter.Plugins.Abstractions;

/// <summary>
/// Provides plugins with access to application services and resources.
/// </summary>
public interface IPluginContext
{
    /// <summary>
    /// Plugin's isolated storage directory.
    /// </summary>
    string PluginDataDirectory { get; }

    /// <summary>
    /// Logger instance for the plugin.
    /// </summary>
    IPluginLogger Logger { get; }

    /// <summary>
    /// Granted permissions for this plugin.
    /// </summary>
    IReadOnlyCollection<PluginPermission> GrantedPermissions { get; }

    /// <summary>
    /// Application version for compatibility checks.
    /// </summary>
    Version ApplicationVersion { get; }

    /// <summary>
    /// Checks if a permission is granted.
    /// </summary>
    bool HasPermission(PluginPermission permission);

    /// <summary>
    /// Gets a sandboxed service if available and permitted.
    /// </summary>
    T? GetService<T>() where T : class;
}

/// <summary>
/// Logging interface for plugins.
/// </summary>
public interface IPluginLogger
{
    void Debug(string message);
    void Info(string message);
    void Warning(string message);
    void Error(string message, Exception? exception = null);
}

/// <summary>
/// Sandboxed file system access for plugins.
/// </summary>
public interface IFileSystemAccess
{
    Task<string> ReadFileAsync(string path, CancellationToken ct = default);
    Task WriteFileAsync(string path, string content, CancellationToken ct = default);
    Task<byte[]> ReadFileBytesAsync(string path, CancellationToken ct = default);
    Task WriteFileBytesAsync(string path, byte[] content, CancellationToken ct = default);
    Task<bool> FileExistsAsync(string path, CancellationToken ct = default);
    Task<bool> DirectoryExistsAsync(string path, CancellationToken ct = default);
    Task<IReadOnlyList<string>> ListFilesAsync(string directory, string pattern = "*", CancellationToken ct = default);
    Task DeleteFileAsync(string path, CancellationToken ct = default);
    Task CreateDirectoryAsync(string path, CancellationToken ct = default);
}

/// <summary>
/// Sandboxed network access for plugins.
/// </summary>
public interface INetworkAccess
{
    Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct = default);
    Task<string> GetStringAsync(string url, CancellationToken ct = default);
    Task<byte[]> GetBytesAsync(string url, CancellationToken ct = default);
}

/// <summary>
/// Sandboxed combat data access for plugins.
/// </summary>
public interface ICombatDataAccess
{
    /// <summary>
    /// Gets all parsed events (immutable copy).
    /// </summary>
    IReadOnlyList<Core.Models.LogEvent>? GetAllEvents();

    /// <summary>
    /// Gets currently filtered events (immutable copy).
    /// </summary>
    IReadOnlyList<Core.Models.LogEvent>? GetFilteredEvents();

    /// <summary>
    /// Gets current combat statistics.
    /// </summary>
    Core.Models.CombatStatistics? GetStatistics();

    /// <summary>
    /// Subscribes to data change events.
    /// </summary>
    IDisposable OnDataChanged(Action callback);
}

/// <summary>
/// Sandboxed preferences access for plugins.
/// </summary>
public interface IPreferencesAccess
{
    Task<T?> GetAsync<T>(string key, CancellationToken ct = default);
    Task SetAsync<T>(string key, T value, CancellationToken ct = default);
    Task<bool> ContainsAsync(string key, CancellationToken ct = default);
    Task RemoveAsync(string key, CancellationToken ct = default);
}

/// <summary>
/// UI services available to plugins.
/// </summary>
public interface IPluginUIService
{
    /// <summary>Show a message dialog.</summary>
    Task ShowMessageAsync(string title, string message);

    /// <summary>Show a confirmation dialog.</summary>
    Task<bool> ShowConfirmAsync(string title, string message);

    /// <summary>Show a file open dialog.</summary>
    Task<string?> ShowOpenFileDialogAsync(string title, string[] filters);

    /// <summary>Show a file save dialog.</summary>
    Task<string?> ShowSaveFileDialogAsync(string title, string defaultExt, string[] filters);

    /// <summary>Request a UI refresh.</summary>
    void RequestRefresh();

    /// <summary>Show a notification to the user.</summary>
    Task ShowNotificationAsync(string title, string message, NotificationType type);
}

/// <summary>
/// Notification types.
/// </summary>
public enum NotificationType
{
    Info,
    Success,
    Warning,
    Error
}
