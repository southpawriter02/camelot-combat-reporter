# Plugin Manifest Reference

The plugin manifest (`plugin.json`) is a required configuration file that describes your plugin to Camelot Combat Reporter. It must be placed in the root of your plugin directory.

## Complete Schema

```json
{
  "id": "string (required)",
  "name": "string (required)",
  "version": "string (required)",
  "author": "string (required)",
  "description": "string (required)",
  "type": "string (required)",
  "entryPoint": {
    "assembly": "string (required)",
    "typeName": "string (required)"
  },
  "compatibility": {
    "minAppVersion": "string (required)",
    "maxAppVersion": "string (optional)"
  },
  "permissions": [
    {
      "type": "string (required)",
      "scope": "string (optional)",
      "reason": "string (recommended)"
    }
  ],
  "resources": {
    "maxMemoryMb": "number (optional, default: 64)",
    "maxCpuTimeSeconds": "number (optional, default: 30)"
  },
  "dependencies": [
    {
      "pluginId": "string",
      "minVersion": "string (optional)",
      "maxVersion": "string (optional)"
    }
  ],
  "signing": {
    "thumbprint": "string (optional)",
    "requireSignature": "boolean (optional)"
  },
  "metadata": {
    "homepage": "string (optional)",
    "repository": "string (optional)",
    "license": "string (optional)",
    "tags": ["string"] (optional),
    "icon": "string (optional)"
  }
}
```

## Required Fields

### `id`

**Type:** `string`
**Required:** Yes

Unique identifier for your plugin. Must be lowercase with hyphens. This ID is used internally for plugin management and must match the `Id` property in your plugin class.

```json
"id": "my-awesome-plugin"
```

**Naming conventions:**
- Use lowercase letters, numbers, and hyphens only
- Start with a letter
- Maximum 50 characters
- Must be unique across all installed plugins

### `name`

**Type:** `string`
**Required:** Yes

Human-readable display name shown in the Plugin Manager.

```json
"name": "My Awesome Plugin"
```

### `version`

**Type:** `string`
**Required:** Yes

Plugin version following [Semantic Versioning](https://semver.org/) (MAJOR.MINOR.PATCH).

```json
"version": "1.2.3"
```

**Version parts:**
- **MAJOR**: Incremented for breaking changes
- **MINOR**: Incremented for new features (backwards-compatible)
- **PATCH**: Incremented for bug fixes

### `author`

**Type:** `string`
**Required:** Yes

Plugin author name or organization.

```json
"author": "Jane Developer"
```

### `description`

**Type:** `string`
**Required:** Yes

Brief description of what the plugin does. Displayed in the Plugin Manager.

```json
"description": "Calculates advanced combat statistics including DPS, HPS, and combat efficiency metrics."
```

### `type`

**Type:** `string`
**Required:** Yes

The type of plugin. Must be one of:

| Type | Description |
|------|-------------|
| `DataAnalysis` | Provides custom statistics and metrics |
| `ExportFormat` | Adds new export file formats |
| `UIComponent` | Adds custom UI elements (tabs, panels) |
| `CustomParser` | Extends log parsing capabilities |

```json
"type": "DataAnalysis"
```

### `entryPoint`

**Type:** `object`
**Required:** Yes

Specifies how to load the plugin.

```json
"entryPoint": {
  "assembly": "MyPlugin.dll",
  "typeName": "MyPlugin.MyAwesomePlugin"
}
```

| Property | Type | Description |
|----------|------|-------------|
| `assembly` | `string` | DLL filename (relative to plugin directory) |
| `typeName` | `string` | Fully qualified type name of the plugin class |

### `compatibility`

**Type:** `object`
**Required:** Yes

Specifies application version compatibility.

```json
"compatibility": {
  "minAppVersion": "1.0.0",
  "maxAppVersion": "2.0.0"
}
```

| Property | Type | Required | Description |
|----------|------|----------|-------------|
| `minAppVersion` | `string` | Yes | Minimum supported app version |
| `maxAppVersion` | `string` | No | Maximum supported app version (exclusive) |

## Optional Fields

### `permissions`

**Type:** `array`
**Required:** No
**Default:** `[]`

List of permissions required by the plugin. See [Permissions Guide](permissions.md) for details.

```json
"permissions": [
  {
    "type": "NetworkAccess",
    "scope": "api.example.com",
    "reason": "Fetch additional monster data from online database"
  },
  {
    "type": "FileReadExternal",
    "scope": "/logs",
    "reason": "Read additional log files from game directory"
  }
]
```

**Permission object properties:**

| Property | Type | Required | Description |
|----------|------|----------|-------------|
| `type` | `string` | Yes | Permission type (see Permissions Guide) |
| `scope` | `string` | No | Limits the permission scope (e.g., specific host) |
| `reason` | `string` | Recommended | Explanation shown to user when requesting approval |

### `resources`

**Type:** `object`
**Required:** No

Resource limits for the plugin sandbox.

```json
"resources": {
  "maxMemoryMb": 64,
  "maxCpuTimeSeconds": 30
}
```

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `maxMemoryMb` | `number` | 64 | Maximum memory allocation in megabytes |
| `maxCpuTimeSeconds` | `number` | 30 | Maximum CPU time per operation in seconds |

**Notes:**
- Exceeding limits will terminate the operation
- Lower limits allow more plugins to run concurrently
- Set appropriate limits based on plugin complexity

### `dependencies`

**Type:** `array`
**Required:** No
**Default:** `[]`

Other plugins that must be loaded before this plugin.

```json
"dependencies": [
  {
    "pluginId": "combat-data-enricher",
    "minVersion": "1.0.0"
  },
  {
    "pluginId": "damage-calculator",
    "minVersion": "2.0.0",
    "maxVersion": "3.0.0"
  }
]
```

| Property | Type | Required | Description |
|----------|------|----------|-------------|
| `pluginId` | `string` | Yes | ID of the required plugin |
| `minVersion` | `string` | No | Minimum version required |
| `maxVersion` | `string` | No | Maximum version allowed (exclusive) |

### `signing`

**Type:** `object`
**Required:** No

Code signing configuration for enhanced security.

```json
"signing": {
  "thumbprint": "1234567890ABCDEF1234567890ABCDEF12345678",
  "requireSignature": true
}
```

| Property | Type | Description |
|----------|------|-------------|
| `thumbprint` | `string` | SHA-1 thumbprint of signing certificate |
| `requireSignature` | `boolean` | If true, plugin must be signed to load |

See [Security Guide](permissions.md#code-signing) for signing instructions.

### `metadata`

**Type:** `object`
**Required:** No

Additional metadata for display and discoverability.

```json
"metadata": {
  "homepage": "https://example.com/my-plugin",
  "repository": "https://github.com/user/my-plugin",
  "license": "MIT",
  "tags": ["dps", "statistics", "performance"],
  "icon": "icon.png"
}
```

| Property | Type | Description |
|----------|------|-------------|
| `homepage` | `string` | URL to plugin homepage |
| `repository` | `string` | URL to source code repository |
| `license` | `string` | SPDX license identifier or license name |
| `tags` | `string[]` | Keywords for searchability |
| `icon` | `string` | Path to icon file (relative to plugin directory) |

## Complete Examples

### Data Analysis Plugin

```json
{
  "id": "advanced-dps-analyzer",
  "name": "Advanced DPS Analyzer",
  "version": "2.1.0",
  "author": "Combat Metrics Team",
  "description": "Comprehensive damage analysis with DPS, burst damage, damage distribution, and efficiency metrics.",
  "type": "DataAnalysis",
  "entryPoint": {
    "assembly": "AdvancedDpsAnalyzer.dll",
    "typeName": "AdvancedDpsAnalyzer.Plugin"
  },
  "compatibility": {
    "minAppVersion": "1.0.0"
  },
  "permissions": [
    {
      "type": "SettingsRead",
      "reason": "Load user preferences for display format"
    }
  ],
  "resources": {
    "maxMemoryMb": 128,
    "maxCpuTimeSeconds": 60
  },
  "metadata": {
    "homepage": "https://example.com/dps-analyzer",
    "repository": "https://github.com/combat-metrics/dps-analyzer",
    "license": "MIT",
    "tags": ["dps", "damage", "analysis", "statistics"]
  }
}
```

### Export Format Plugin

```json
{
  "id": "html-export",
  "name": "HTML Combat Report Exporter",
  "version": "1.0.0",
  "author": "Report Tools",
  "description": "Exports combat data to a beautiful HTML report with charts and tables.",
  "type": "ExportFormat",
  "entryPoint": {
    "assembly": "HtmlExport.dll",
    "typeName": "HtmlExport.HtmlExportPlugin"
  },
  "compatibility": {
    "minAppVersion": "1.0.0"
  },
  "permissions": [
    {
      "type": "FileWriteExternal",
      "reason": "Save HTML report to user-selected location"
    }
  ],
  "resources": {
    "maxMemoryMb": 256,
    "maxCpuTimeSeconds": 120
  }
}
```

### UI Component Plugin

```json
{
  "id": "damage-timeline-chart",
  "name": "Damage Timeline Visualization",
  "version": "1.3.0",
  "author": "Visualization Labs",
  "description": "Adds an interactive timeline chart showing damage events over time.",
  "type": "UIComponent",
  "entryPoint": {
    "assembly": "DamageTimeline.dll",
    "typeName": "DamageTimeline.TimelinePlugin"
  },
  "compatibility": {
    "minAppVersion": "1.0.0"
  },
  "permissions": [
    {
      "type": "UIModification",
      "reason": "Add new tab to main window"
    },
    {
      "type": "CombatDataAccess",
      "reason": "Read combat events for visualization"
    }
  ],
  "resources": {
    "maxMemoryMb": 256,
    "maxCpuTimeSeconds": 30
  }
}
```

### Custom Parser Plugin

```json
{
  "id": "extended-combat-parser",
  "name": "Extended Combat Parser",
  "version": "1.0.0",
  "author": "Parser Experts",
  "description": "Adds support for parsing critical hits, glancing blows, and resist messages.",
  "type": "CustomParser",
  "entryPoint": {
    "assembly": "ExtendedParser.dll",
    "typeName": "ExtendedParser.ExtendedCombatParser"
  },
  "compatibility": {
    "minAppVersion": "1.0.0"
  },
  "permissions": [],
  "resources": {
    "maxMemoryMb": 32,
    "maxCpuTimeSeconds": 10
  }
}
```

## Validation

The manifest is validated when the plugin is discovered. Common validation errors:

| Error | Cause | Solution |
|-------|-------|----------|
| `Missing required field` | A required field is not present | Add the missing field |
| `Invalid plugin type` | Type is not a valid plugin type | Use one of the four valid types |
| `Invalid version format` | Version doesn't follow semver | Use MAJOR.MINOR.PATCH format |
| `Assembly not found` | Entry point assembly doesn't exist | Verify assembly path and filename |
| `Type not found` | Entry point type doesn't exist in assembly | Verify fully qualified type name |
| `Incompatible version` | App version outside compatibility range | Update minAppVersion/maxAppVersion |

## Best Practices

1. **Use semantic versioning** - Helps users understand the impact of updates
2. **Provide clear reasons for permissions** - Users are more likely to approve with explanation
3. **Set appropriate resource limits** - Don't request more than needed
4. **Include metadata** - Helps users find and trust your plugin
5. **Keep IDs stable** - Changing IDs breaks user settings and trust decisions
6. **Test compatibility** - Verify against minimum and maximum app versions
