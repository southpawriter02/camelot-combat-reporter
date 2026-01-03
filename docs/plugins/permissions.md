# Plugin Permissions Guide

Camelot Combat Reporter implements a layered permission system to protect user data and system resources while allowing plugins to provide powerful functionality.

## Overview

The permission system operates on three principles:

1. **Least Privilege**: Plugins only receive permissions they explicitly request and need
2. **User Control**: Users must approve sensitive permissions
3. **Trust Levels**: Different trust levels unlock different capabilities

## Permission Categories

### Auto-Granted Permissions (Low Risk)

These permissions are granted automatically when requested, as they pose minimal security risk:

| Permission | Description |
|------------|-------------|
| `FileRead` | Read files within the plugin's data directory |
| `FileWrite` | Write files within the plugin's data directory |
| `SettingsRead` | Read plugin's own settings |
| `SettingsWrite` | Write plugin's own settings |
| `CombatDataAccess` | Read parsed combat data (immutable copies) |

These permissions are sandboxed:
- File access is restricted to the plugin's isolated data directory
- Settings are namespaced per-plugin
- Combat data is provided as immutable copies

### User-Approved Permissions (Medium Risk)

These permissions require explicit user approval:

| Permission | Description | Typical Use Case |
|------------|-------------|------------------|
| `NetworkAccess` | Make HTTP/HTTPS requests | Fetching external data, APIs |
| `UIModification` | Add UI elements (tabs, panels) | Custom visualizations |
| `UINotifications` | Show notifications to user | Alerts, status updates |
| `ClipboardAccess` | Read/write system clipboard | Copy statistics, export |
| `FileReadExternal` | Read files outside plugin directory | Access game logs |
| `FileWriteExternal` | Write files outside plugin directory | Export to user location |

When a plugin requests these permissions, the user sees a prompt explaining:
- What the permission allows
- Why the plugin needs it (from manifest `reason` field)
- Options to grant once, for session, permanently, or deny

### Trust-Based Permissions

Some capabilities are only available to trusted plugins:

| Trust Level | Description |
|-------------|-------------|
| `OfficialTrusted` | Plugins signed by Camelot Combat Reporter team |
| `SignedTrusted` | Plugins signed with a trusted certificate |
| `UserTrusted` | Plugins explicitly trusted by the user |
| `Untrusted` | Default level for unsigned plugins |

Trusted plugins receive automatic approval for medium-risk permissions.

## Declaring Permissions

### In Plugin Manifest (plugin.json)

```json
{
  "permissions": [
    {
      "type": "NetworkAccess",
      "scope": "api.example.com",
      "reason": "Fetch monster database for damage calculations"
    },
    {
      "type": "FileReadExternal",
      "scope": "/logs",
      "reason": "Read additional game log files"
    },
    {
      "type": "UIModification",
      "reason": "Add timeline visualization tab"
    }
  ]
}
```

### In Plugin Code

```csharp
public class MyPlugin : DataAnalysisPluginBase
{
    public override IReadOnlyCollection<PluginPermission> RequiredPermissions =>
        new[]
        {
            PluginPermission.NetworkAccess,
            PluginPermission.CombatDataAccess
        };

    // Plugin implementation...
}
```

## Checking Permissions at Runtime

Always verify permissions before using protected APIs:

```csharp
public override async Task<AnalysisResult> AnalyzeAsync(
    IReadOnlyList<LogEvent> events,
    CombatStatistics? baseStatistics,
    AnalysisOptions options,
    CancellationToken ct = default)
{
    // Check if we have network permission
    if (Context.HasPermission(PluginPermission.NetworkAccess))
    {
        var networkService = Context.GetService<INetworkAccess>();
        if (networkService != null)
        {
            var additionalData = await networkService.GetStringAsync(
                "https://api.example.com/data",
                ct);
            // Use additional data...
        }
    }

    // Always have CombatDataAccess - it's auto-granted
    var dataService = Context.GetService<ICombatDataAccess>();
    var allEvents = dataService?.GetAllEvents();

    // Continue with analysis...
    return Success(results);
}
```

## Permission Scopes

Some permissions support scopes to limit their reach:

### Network Scope

Limit network access to specific domains:

```json
{
  "type": "NetworkAccess",
  "scope": "api.example.com",
  "reason": "Fetch data from example.com API"
}
```

Multiple domains can be specified with semicolons:
```json
"scope": "api.example.com;cdn.example.com"
```

### File Scope

Limit file access to specific paths:

```json
{
  "type": "FileReadExternal",
  "scope": "/logs;/screenshots",
  "reason": "Read game logs and screenshots"
}
```

Paths are relative to:
- **Windows**: `%USERPROFILE%`
- **macOS/Linux**: `$HOME`

## User Experience

### Permission Prompt Dialog

When a plugin requests user-approved permissions, a dialog appears:

```
┌─────────────────────────────────────────────────────┐
│  Permission Request: My Analytics Plugin            │
├─────────────────────────────────────────────────────┤
│                                                     │
│  This plugin is requesting the following            │
│  permissions:                                       │
│                                                     │
│  ☑ Network Access                                   │
│    Fetch monster database for damage calculations   │
│    Scope: api.example.com                          │
│                                                     │
│  ☑ UI Modification                                  │
│    Add timeline visualization tab                   │
│                                                     │
│  ─────────────────────────────────────────────────  │
│                                                     │
│  [Deny]  [Allow Once]  [Allow Session]  [Allow]    │
│                                                     │
└─────────────────────────────────────────────────────┘
```

### Grant Types

| Type | Description |
|------|-------------|
| **Deny** | Permission denied; plugin functionality may be limited |
| **Allow Once** | Granted for this operation only |
| **Allow Session** | Granted until application restart |
| **Allow** | Granted permanently (saved to disk) |

### Managing Permissions

Users can manage plugin permissions in the Plugin Manager:

1. Open **Tools > Plugin Manager**
2. Select a plugin
3. Click **Permissions** tab
4. View/revoke granted permissions

## Security Best Practices

### For Plugin Developers

1. **Request Minimum Permissions**
   ```csharp
   // Bad: Request all permissions upfront
   public override IReadOnlyCollection<PluginPermission> RequiredPermissions =>
       new[] { PluginPermission.NetworkAccess, PluginPermission.FileWriteExternal, ... };

   // Good: Only request what you need
   public override IReadOnlyCollection<PluginPermission> RequiredPermissions =>
       new[] { PluginPermission.CombatDataAccess };
   ```

2. **Provide Clear Reasons**
   ```json
   // Bad
   { "type": "NetworkAccess", "reason": "Needed" }

   // Good
   { "type": "NetworkAccess", "reason": "Fetch spell coefficients from the game database API" }
   ```

3. **Use Scopes When Possible**
   ```json
   // Bad: Unrestricted network access
   { "type": "NetworkAccess" }

   // Good: Scoped to specific domain
   { "type": "NetworkAccess", "scope": "api.myservice.com" }
   ```

4. **Handle Permission Denial Gracefully**
   ```csharp
   var network = Context.GetService<INetworkAccess>();
   if (network == null)
   {
       LogWarning("Network access not available - using cached data");
       return UseCachedData();
   }
   ```

5. **Don't Cache Sensitive Data**
   ```csharp
   // Bad: Store credentials
   await preferences.SetAsync("api-key", userApiKey);

   // Good: Request each session or use secure storage
   ```

### For Users

1. **Review Permissions Carefully**
   - Read the reason provided for each permission
   - Consider if the permission makes sense for the plugin's purpose
   - Deny permissions that seem excessive

2. **Use Session Grants for New Plugins**
   - Start with "Allow Session" to test the plugin
   - Grant permanent permissions after building trust

3. **Revoke Unused Permissions**
   - Periodically review plugin permissions in Plugin Manager
   - Revoke permissions no longer needed

4. **Prefer Signed Plugins**
   - Look for the "Signed" badge in Plugin Manager
   - Signed plugins have been verified

## Code Signing

### Why Sign Plugins?

- **Authenticity**: Proves the plugin is from the claimed author
- **Integrity**: Detects if the plugin was modified
- **Trust**: Enables automatic permission approval

### Signing Your Plugin

1. **Obtain a Code Signing Certificate**
   - Purchase from a trusted Certificate Authority (DigiCert, Sectigo, etc.)
   - Or use a self-signed certificate for development

2. **Sign the Assembly**
   ```bash
   # Using SignTool (Windows)
   signtool sign /f certificate.pfx /p password /t http://timestamp.digicert.com MyPlugin.dll

   # Using osslsigncode (macOS/Linux)
   osslsigncode sign -pkcs12 certificate.pfx -pass password -t http://timestamp.digicert.com -in MyPlugin.dll -out MyPlugin-signed.dll
   ```

3. **Update Manifest**
   ```json
   {
     "signing": {
       "thumbprint": "1234567890ABCDEF1234567890ABCDEF12345678",
       "requireSignature": true
     }
   }
   ```

### Verifying Signatures

The application verifies:
1. The assembly is signed with Authenticode
2. The certificate chain is valid
3. The certificate is not revoked
4. The thumbprint matches the manifest (if specified)

## Permission Reference

### PluginPermission Enum

```csharp
[Flags]
public enum PluginPermission
{
    None = 0,

    // File System (Auto-granted for plugin directory)
    FileRead = 1 << 0,
    FileWrite = 1 << 1,

    // External File System (Requires approval)
    FileReadExternal = 1 << 2,
    FileWriteExternal = 1 << 3,

    // Network (Requires approval)
    NetworkAccess = 1 << 4,

    // UI (Requires approval)
    UIModification = 1 << 5,
    UINotifications = 1 << 6,

    // Settings (Auto-granted)
    SettingsRead = 1 << 7,
    SettingsWrite = 1 << 8,

    // System (Requires approval)
    ClipboardAccess = 1 << 9,

    // Combat Data (Auto-granted)
    CombatDataAccess = 1 << 10,

    // Convenience combinations
    FileReadWrite = FileRead | FileWrite,
    FullUI = UIModification | UINotifications,
    AllSettings = SettingsRead | SettingsWrite
}
```

### Permission to Service Mapping

| Permission | Service Interface | Description |
|------------|-------------------|-------------|
| `FileRead`, `FileWrite` | `IFileSystemAccess` | Plugin directory access |
| `FileReadExternal`, `FileWriteExternal` | `IFileSystemAccess` | Full filesystem access |
| `NetworkAccess` | `INetworkAccess` | HTTP client |
| `UIModification` | `IPluginUIService` | Dialog and UI services |
| `UINotifications` | `IPluginUIService` | Notification service |
| `SettingsRead`, `SettingsWrite` | `IPreferencesAccess` | Plugin preferences |
| `CombatDataAccess` | `ICombatDataAccess` | Combat events and stats |
| `ClipboardAccess` | `IClipboardAccess` | System clipboard |

## Troubleshooting

### Permission Denied Errors

If your plugin receives permission denied errors:

1. **Check Manifest**: Ensure the permission is declared in `plugin.json`
2. **Check User Approval**: User may have denied the permission
3. **Check Scope**: If using scopes, verify the operation is within scope
4. **Check Trust Level**: Some permissions require signed plugins

### Debugging Permission Issues

Enable debug logging to see permission checks:

```csharp
public override Task InitializeAsync(IPluginContext context, CancellationToken ct)
{
    LogDebug($"Granted permissions: {string.Join(", ", context.GrantedPermissions)}");

    foreach (var permission in RequiredPermissions)
    {
        var hasIt = context.HasPermission(permission);
        LogDebug($"Permission {permission}: {(hasIt ? "granted" : "denied")}");
    }

    return base.InitializeAsync(context, ct);
}
```

### Common Issues

| Issue | Cause | Solution |
|-------|-------|----------|
| "Service not available" | Permission not granted | Request permission or handle gracefully |
| "Access to path denied" | File outside allowed scope | Update scope or use plugin data directory |
| "Network request blocked" | Domain not in scope | Add domain to `scope` in manifest |
| "UI modification not permitted" | Missing `UIModification` permission | Add to manifest and request approval |
