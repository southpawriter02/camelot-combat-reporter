using System.Text.Json;
using CamelotCombatReporter.Plugins.Manifest;

namespace CamelotCombatReporter.Plugins.Permissions;

/// <summary>
/// Manages plugin permissions, including auto-granting, prompting, and persistence.
/// </summary>
public sealed class PermissionManager : IPermissionManager
{
    private readonly IPermissionStore _store;
    private readonly IPermissionPromptService? _promptService;
    private readonly string _pluginsDirectory;

    /// <summary>
    /// Permissions that are automatically granted (low risk).
    /// </summary>
    private static readonly PluginPermission AutoGrantPermissions =
        PluginPermission.FileRead |
        PluginPermission.FileWrite |
        PluginPermission.SettingsRead |
        PluginPermission.SettingsWrite |
        PluginPermission.CombatDataAccess;

    /// <summary>
    /// Permissions that require user confirmation (medium risk).
    /// </summary>
    private static readonly PluginPermission RequiresApprovalPermissions =
        PluginPermission.NetworkAccess |
        PluginPermission.UIModification |
        PluginPermission.UINotifications |
        PluginPermission.ClipboardAccess |
        PluginPermission.FileReadExternal |
        PluginPermission.FileWriteExternal;

    public PermissionManager(string pluginsDirectory, IPermissionPromptService? promptService = null)
    {
        _pluginsDirectory = pluginsDirectory;
        _store = new FilePermissionStore(Path.Combine(pluginsDirectory, "permissions.json"));
        _promptService = promptService;
    }

    /// <summary>
    /// Requests permissions for a plugin based on its manifest.
    /// </summary>
    public async Task<PermissionRequestResult> RequestPermissionsAsync(
        string pluginId,
        IEnumerable<PermissionRequestConfig> requests,
        PluginTrustLevel trustLevel,
        CancellationToken ct = default)
    {
        var grantedPermissions = new List<PluginPermission>();
        var deniedPermissions = new List<PluginPermission>();
        var pendingApproval = new List<PermissionRequestConfig>();

        foreach (var request in requests)
        {
            // Check stored grants first
            var storedGrant = await _store.GetGrantAsync(pluginId, request.Type);
            if (storedGrant?.GrantType == PermissionGrantType.GrantedPermanent)
            {
                grantedPermissions.Add(request.Type);
                continue;
            }

            // Official/trusted plugins get all requested permissions
            if (trustLevel == PluginTrustLevel.OfficialTrusted ||
                trustLevel == PluginTrustLevel.SignedTrusted)
            {
                grantedPermissions.Add(request.Type);
                await _store.SaveGrantAsync(pluginId, new PermissionGrant(
                    request.Type,
                    request.Scope,
                    DateTime.UtcNow,
                    PermissionGrantType.GrantedPermanent));
                continue;
            }

            // Auto-grant low-risk permissions
            if ((request.Type & AutoGrantPermissions) == request.Type)
            {
                grantedPermissions.Add(request.Type);
                await _store.SaveGrantAsync(pluginId, new PermissionGrant(
                    request.Type,
                    request.Scope,
                    DateTime.UtcNow,
                    PermissionGrantType.GrantedPermanent));
                continue;
            }

            // Queue for user approval
            if ((request.Type & RequiresApprovalPermissions) != 0)
            {
                pendingApproval.Add(request);
            }
            else
            {
                // Unknown permission type - deny by default
                deniedPermissions.Add(request.Type);
            }
        }

        // Prompt user for pending permissions
        if (pendingApproval.Count > 0 && _promptService != null)
        {
            var userDecisions = await _promptService.PromptForPermissionsAsync(
                pluginId,
                pendingApproval,
                ct);

            foreach (var decision in userDecisions)
            {
                if (decision.GrantType != PermissionGrantType.Denied)
                {
                    grantedPermissions.Add(decision.Permission);
                    await _store.SaveGrantAsync(pluginId, decision);
                }
                else
                {
                    deniedPermissions.Add(decision.Permission);
                }
            }
        }
        else if (pendingApproval.Count > 0)
        {
            // No prompt service available - deny all pending permissions
            foreach (var request in pendingApproval)
            {
                deniedPermissions.Add(request.Type);
            }
        }

        return new PermissionRequestResult(
            grantedPermissions.AsReadOnly(),
            deniedPermissions.AsReadOnly());
    }

    /// <summary>
    /// Revokes a permission from a plugin.
    /// </summary>
    public async Task RevokePermissionAsync(
        string pluginId,
        PluginPermission permission,
        CancellationToken ct = default)
    {
        await _store.RemoveGrantAsync(pluginId, permission);
    }

    /// <summary>
    /// Checks if a plugin has a specific permission.
    /// </summary>
    public async Task<bool> HasPermissionAsync(
        string pluginId,
        PluginPermission permission,
        CancellationToken ct = default)
    {
        var grant = await _store.GetGrantAsync(pluginId, permission);
        return grant?.GrantType is PermissionGrantType.GrantedPermanent
            or PermissionGrantType.GrantedSession;
    }

    /// <summary>
    /// Gets all granted permissions for a plugin.
    /// </summary>
    public async Task<IReadOnlyCollection<PermissionGrant>> GetGrantedPermissionsAsync(
        string pluginId,
        CancellationToken ct = default)
    {
        return await _store.GetAllGrantsAsync(pluginId);
    }

    /// <summary>
    /// Revokes all permissions for a plugin.
    /// </summary>
    public async Task RevokeAllPermissionsAsync(
        string pluginId,
        CancellationToken ct = default)
    {
        await _store.RemoveAllGrantsAsync(pluginId);
    }
}

/// <summary>
/// Interface for permission management.
/// </summary>
public interface IPermissionManager
{
    Task<PermissionRequestResult> RequestPermissionsAsync(
        string pluginId,
        IEnumerable<PermissionRequestConfig> requests,
        PluginTrustLevel trustLevel,
        CancellationToken ct = default);

    Task RevokePermissionAsync(string pluginId, PluginPermission permission, CancellationToken ct = default);
    Task<bool> HasPermissionAsync(string pluginId, PluginPermission permission, CancellationToken ct = default);
    Task<IReadOnlyCollection<PermissionGrant>> GetGrantedPermissionsAsync(string pluginId, CancellationToken ct = default);
    Task RevokeAllPermissionsAsync(string pluginId, CancellationToken ct = default);
}

/// <summary>
/// Result of a permission request operation.
/// </summary>
/// <param name="Granted">Permissions that were granted.</param>
/// <param name="Denied">Permissions that were denied.</param>
public sealed record PermissionRequestResult(
    IReadOnlyCollection<PluginPermission> Granted,
    IReadOnlyCollection<PluginPermission> Denied)
{
    public bool AllGranted => Denied.Count == 0;
}

/// <summary>
/// Interface for prompting user for permission approval.
/// </summary>
public interface IPermissionPromptService
{
    Task<IReadOnlyCollection<PermissionGrant>> PromptForPermissionsAsync(
        string pluginId,
        IReadOnlyCollection<PermissionRequestConfig> requests,
        CancellationToken ct = default);
}

/// <summary>
/// Interface for persisting permission grants.
/// </summary>
public interface IPermissionStore
{
    Task<PermissionGrant?> GetGrantAsync(string pluginId, PluginPermission permission);
    Task<IReadOnlyCollection<PermissionGrant>> GetAllGrantsAsync(string pluginId);
    Task SaveGrantAsync(string pluginId, PermissionGrant grant);
    Task RemoveGrantAsync(string pluginId, PluginPermission permission);
    Task RemoveAllGrantsAsync(string pluginId);
}

/// <summary>
/// File-based permission store.
/// </summary>
public sealed class FilePermissionStore : IPermissionStore
{
    private readonly string _filePath;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private Dictionary<string, List<PermissionGrant>>? _cache;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public FilePermissionStore(string filePath)
    {
        _filePath = filePath;
    }

    public async Task<PermissionGrant?> GetGrantAsync(string pluginId, PluginPermission permission)
    {
        var grants = await LoadGrantsAsync();
        if (grants.TryGetValue(pluginId, out var pluginGrants))
        {
            return pluginGrants.FirstOrDefault(g => g.Permission == permission);
        }
        return null;
    }

    public async Task<IReadOnlyCollection<PermissionGrant>> GetAllGrantsAsync(string pluginId)
    {
        var grants = await LoadGrantsAsync();
        if (grants.TryGetValue(pluginId, out var pluginGrants))
        {
            return pluginGrants.AsReadOnly();
        }
        return Array.Empty<PermissionGrant>();
    }

    public async Task SaveGrantAsync(string pluginId, PermissionGrant grant)
    {
        await _lock.WaitAsync();
        try
        {
            var grants = await LoadGrantsAsync();
            if (!grants.ContainsKey(pluginId))
            {
                grants[pluginId] = new List<PermissionGrant>();
            }

            // Remove existing grant for same permission
            grants[pluginId].RemoveAll(g => g.Permission == grant.Permission);
            grants[pluginId].Add(grant);

            await SaveGrantsAsync(grants);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task RemoveGrantAsync(string pluginId, PluginPermission permission)
    {
        await _lock.WaitAsync();
        try
        {
            var grants = await LoadGrantsAsync();
            if (grants.TryGetValue(pluginId, out var pluginGrants))
            {
                pluginGrants.RemoveAll(g => g.Permission == permission);
                await SaveGrantsAsync(grants);
            }
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task RemoveAllGrantsAsync(string pluginId)
    {
        await _lock.WaitAsync();
        try
        {
            var grants = await LoadGrantsAsync();
            grants.Remove(pluginId);
            await SaveGrantsAsync(grants);
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task<Dictionary<string, List<PermissionGrant>>> LoadGrantsAsync()
    {
        if (_cache != null) return _cache;

        if (!File.Exists(_filePath))
        {
            _cache = new Dictionary<string, List<PermissionGrant>>();
            return _cache;
        }

        try
        {
            var json = await File.ReadAllTextAsync(_filePath);
            _cache = JsonSerializer.Deserialize<Dictionary<string, List<PermissionGrant>>>(json, JsonOptions)
                     ?? new Dictionary<string, List<PermissionGrant>>();
        }
        catch
        {
            _cache = new Dictionary<string, List<PermissionGrant>>();
        }

        return _cache;
    }

    private async Task SaveGrantsAsync(Dictionary<string, List<PermissionGrant>> grants)
    {
        _cache = grants;
        var directory = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(grants, JsonOptions);
        await File.WriteAllTextAsync(_filePath, json);
    }
}
