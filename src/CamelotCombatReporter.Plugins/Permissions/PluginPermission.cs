namespace CamelotCombatReporter.Plugins.Permissions;

/// <summary>
/// Defines available plugin permissions.
/// </summary>
[Flags]
public enum PluginPermission
{
    /// <summary>No permissions.</summary>
    None = 0,

    // File System permissions
    /// <summary>Read files within plugin data directory.</summary>
    FileRead = 1 << 0,

    /// <summary>Write files within plugin data directory.</summary>
    FileWrite = 1 << 1,

    /// <summary>Read files outside plugin directory (requires user approval).</summary>
    FileReadExternal = 1 << 2,

    /// <summary>Write files outside plugin directory (requires user approval).</summary>
    FileWriteExternal = 1 << 3,

    // Network permissions
    /// <summary>Make HTTP/HTTPS requests.</summary>
    NetworkAccess = 1 << 4,

    // UI permissions
    /// <summary>Modify application UI (add tabs, panels).</summary>
    UIModification = 1 << 5,

    /// <summary>Show notifications to user.</summary>
    UINotifications = 1 << 6,

    // Settings permissions
    /// <summary>Read application settings.</summary>
    SettingsRead = 1 << 7,

    /// <summary>Write plugin settings.</summary>
    SettingsWrite = 1 << 8,

    // System permissions
    /// <summary>Access system clipboard.</summary>
    ClipboardAccess = 1 << 9,

    /// <summary>Access parsed combat data.</summary>
    CombatDataAccess = 1 << 10,

    // Combinations
    /// <summary>Read and write files within plugin directory.</summary>
    FileReadWrite = FileRead | FileWrite,

    /// <summary>Full UI access.</summary>
    FullUI = UIModification | UINotifications,

    /// <summary>Read and write settings.</summary>
    AllSettings = SettingsRead | SettingsWrite
}

/// <summary>
/// Permission level for categorizing risk.
/// </summary>
public enum PermissionLevel
{
    /// <summary>No access.</summary>
    None,

    /// <summary>Access restricted to specific paths/hosts.</summary>
    Restricted,

    /// <summary>Full unrestricted access (trusted plugins only).</summary>
    Full
}

/// <summary>
/// Type of permission grant.
/// </summary>
public enum PermissionGrantType
{
    /// <summary>Permission was denied.</summary>
    Denied,

    /// <summary>Permission granted for this session only.</summary>
    GrantedOnce,

    /// <summary>Permission granted for the current session.</summary>
    GrantedSession,

    /// <summary>Permission granted permanently.</summary>
    GrantedPermanent
}

/// <summary>
/// Represents a permission request from a plugin.
/// </summary>
/// <param name="Permission">The permission being requested.</param>
/// <param name="Scope">Optional scope restriction (e.g., file paths, domains).</param>
/// <param name="Reason">User-facing explanation for why permission is needed.</param>
public record PermissionRequest(
    PluginPermission Permission,
    string? Scope,
    string Reason);

/// <summary>
/// Represents a granted permission.
/// </summary>
/// <param name="Permission">The permission that was granted.</param>
/// <param name="Scope">Optional scope restriction.</param>
/// <param name="GrantedAt">When the permission was granted.</param>
/// <param name="GrantType">Type of grant (session, permanent, etc.).</param>
public record PermissionGrant(
    PluginPermission Permission,
    string? Scope,
    DateTime GrantedAt,
    PermissionGrantType GrantType);
