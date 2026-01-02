using CamelotCombatReporter.Plugins.Abstractions;
using CamelotCombatReporter.Plugins.Loading;
using CamelotCombatReporter.Plugins.Security;

namespace CamelotCombatReporter.Plugins.Sandbox;

/// <summary>
/// Sandboxed file system access that restricts operations to the plugin's data directory.
/// </summary>
public sealed class FileSystemProxy : IFileSystemAccess
{
    private readonly string _pluginId;
    private readonly string _baseDirectory;
    private readonly bool _canRead;
    private readonly bool _canWrite;
    private readonly ISecurityAuditLogger _auditLogger;

    public FileSystemProxy(
        string pluginId,
        string baseDirectory,
        bool canRead,
        bool canWrite,
        ISecurityAuditLogger auditLogger)
    {
        _pluginId = pluginId;
        _baseDirectory = Path.GetFullPath(baseDirectory);
        _canRead = canRead;
        _canWrite = canWrite;
        _auditLogger = auditLogger;
    }

    public async Task<string> ReadFileAsync(string path, CancellationToken ct = default)
    {
        ValidateReadAccess(path);
        var fullPath = GetSafePath(path);

        _auditLogger.LogAccess(_pluginId, SecurityAction.FileRead, fullPath);
        return await File.ReadAllTextAsync(fullPath, ct);
    }

    public async Task WriteFileAsync(string path, string content, CancellationToken ct = default)
    {
        ValidateWriteAccess(path);
        var fullPath = GetSafePath(path);

        // Ensure directory exists
        var directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        _auditLogger.LogAccess(_pluginId, SecurityAction.FileWrite, fullPath);
        await File.WriteAllTextAsync(fullPath, content, ct);
    }

    public async Task<byte[]> ReadFileBytesAsync(string path, CancellationToken ct = default)
    {
        ValidateReadAccess(path);
        var fullPath = GetSafePath(path);

        _auditLogger.LogAccess(_pluginId, SecurityAction.FileRead, fullPath);
        return await File.ReadAllBytesAsync(fullPath, ct);
    }

    public async Task WriteFileBytesAsync(string path, byte[] content, CancellationToken ct = default)
    {
        ValidateWriteAccess(path);
        var fullPath = GetSafePath(path);

        var directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        _auditLogger.LogAccess(_pluginId, SecurityAction.FileWrite, fullPath);
        await File.WriteAllBytesAsync(fullPath, content, ct);
    }

    public Task<bool> FileExistsAsync(string path, CancellationToken ct = default)
    {
        ValidateReadAccess(path);
        var fullPath = GetSafePath(path);
        return Task.FromResult(File.Exists(fullPath));
    }

    public Task<bool> DirectoryExistsAsync(string path, CancellationToken ct = default)
    {
        ValidateReadAccess(path);
        var fullPath = GetSafePath(path);
        return Task.FromResult(Directory.Exists(fullPath));
    }

    public Task<IReadOnlyList<string>> ListFilesAsync(string directory, string pattern = "*", CancellationToken ct = default)
    {
        ValidateReadAccess(directory);
        var fullPath = GetSafePath(directory);

        if (!Directory.Exists(fullPath))
        {
            return Task.FromResult<IReadOnlyList<string>>(Array.Empty<string>());
        }

        var files = Directory.GetFiles(fullPath, pattern)
            .Select(f => Path.GetRelativePath(_baseDirectory, f))
            .ToList();

        _auditLogger.LogAccess(_pluginId, SecurityAction.FileRead, $"{fullPath} (list)");
        return Task.FromResult<IReadOnlyList<string>>(files);
    }

    public Task DeleteFileAsync(string path, CancellationToken ct = default)
    {
        ValidateWriteAccess(path);
        var fullPath = GetSafePath(path);

        if (File.Exists(fullPath))
        {
            _auditLogger.LogAccess(_pluginId, SecurityAction.FileWrite, $"{fullPath} (delete)");
            File.Delete(fullPath);
        }

        return Task.CompletedTask;
    }

    public Task CreateDirectoryAsync(string path, CancellationToken ct = default)
    {
        ValidateWriteAccess(path);
        var fullPath = GetSafePath(path);

        _auditLogger.LogAccess(_pluginId, SecurityAction.FileWrite, $"{fullPath} (mkdir)");
        Directory.CreateDirectory(fullPath);

        return Task.CompletedTask;
    }

    private string GetSafePath(string path)
    {
        // Combine with base directory and normalize
        var fullPath = Path.GetFullPath(Path.Combine(_baseDirectory, path));

        // Verify the path is within the base directory (prevent directory traversal)
        if (!fullPath.StartsWith(_baseDirectory, StringComparison.OrdinalIgnoreCase))
        {
            _auditLogger.LogViolation(_pluginId, SecurityAction.FileRead, path);
            throw new PluginSecurityException(_pluginId,
                $"Plugin '{_pluginId}' attempted to access path outside allowed directory: {path}");
        }

        return fullPath;
    }

    private void ValidateReadAccess(string path)
    {
        if (!_canRead)
        {
            _auditLogger.LogViolation(_pluginId, SecurityAction.FileRead, path);
            throw new PluginSecurityException(_pluginId,
                $"Plugin '{_pluginId}' does not have file read permission");
        }
    }

    private void ValidateWriteAccess(string path)
    {
        if (!_canWrite)
        {
            _auditLogger.LogViolation(_pluginId, SecurityAction.FileWrite, path);
            throw new PluginSecurityException(_pluginId,
                $"Plugin '{_pluginId}' does not have file write permission");
        }
    }
}
