# 1. Plugin System

## Status: ✅ Implemented

**Implementation Complete:**
- ✅ Stable core API (parser, analysis, statistics, timeline)
- ✅ Database integration with adapter pattern
- ✅ REST API for external access
- ✅ Plugin manifest format (`plugin.json`)
- ✅ Plugin loader with `AssemblyLoadContext` sandboxing
- ✅ Plugin hooks for data access and events
- ✅ Plugin registry and manager
- ✅ Permission system with user approval workflow
- ✅ Code signing verification support
- ✅ Plugin SDK for developers

---

## Description

The plugin system enables third-party developers to create and share plugins that add new features, visualizations, or analysis to the core application. Plugins run in isolated sandboxes with permission-based security.

## Implemented Features

### Plugin Types

| Type | Description | Status |
|------|-------------|--------|
| **Data Analysis** | Custom statistics and metrics | ✅ Implemented |
| **Export Format** | New file export formats (XML, HTML, PDF) | ✅ Implemented |
| **UI Component** | Custom tabs, panels, charts | ✅ Implemented |
| **Custom Parser** | New log parsing patterns | ✅ Implemented |

### Plugin Architecture

- **Plugin SDK** (`CamelotCombatReporter.PluginSdk`) - Developer-friendly base classes
- **Plugin Abstractions** - Interface definitions for all plugin types
- **Plugin Manifest** - JSON-based configuration (`plugin.json`)
- **Hot Loading** - Plugins can be installed/removed without restart

### Security Model

| Feature | Description |
|---------|-------------|
| **AssemblyLoadContext Isolation** | Each plugin runs in its own isolated context |
| **Permission System** | Layered permissions (auto-grant, user-approved, trusted-only) |
| **Code Signing** | Authenticode signature verification support |
| **Resource Limits** | Per-plugin CPU time and memory quotas |
| **Sandboxed Services** | Proxy-based access to file system, network, and data |
| **Security Audit Logging** | All plugin access attempts are logged |

### Permission Tiers

| Tier | Permissions | Behavior |
|------|-------------|----------|
| **Auto-Grant** | FileRead, FileWrite, SettingsRead, SettingsWrite, CombatDataAccess | Automatically granted |
| **Requires Approval** | NetworkAccess, UIModification, UINotifications, ClipboardAccess, FileReadExternal, FileWriteExternal | User must approve |
| **Trusted Only** | All permissions | Only for signed/trusted plugins |

### Plugin Manager UI

- Browse installed plugins
- Enable/disable plugins
- View plugin permissions
- Install plugins from files
- Uninstall plugins
- Permission management dialog

## Project Structure

```
src/
├── CamelotCombatReporter.Plugins/       # Core plugin infrastructure
│   ├── Abstractions/                    # Plugin interfaces
│   ├── Loading/                         # Plugin loader and discovery
│   ├── Sandbox/                         # Sandboxing and resource limits
│   ├── Permissions/                     # Permission system
│   ├── Security/                        # Code signing verification
│   ├── Manifest/                        # Manifest parsing
│   └── Registry/                        # Plugin registry
│
├── CamelotCombatReporter.PluginSdk/     # SDK for plugin developers
│   ├── PluginBase.cs
│   ├── DataAnalysisPluginBase.cs
│   ├── ExportPluginBase.cs
│   ├── UIPluginBase.cs
│   └── ParserPluginBase.cs
│
└── CamelotCombatReporter.Gui/
    └── Plugins/                         # Plugin UI integration
        ├── ViewModels/
        │   ├── PluginManagerViewModel.cs
        │   └── PluginItemViewModel.cs
        └── Views/
            ├── PluginManagerView.axaml
            └── PluginManagerWindow.axaml

docs/plugins/                            # Developer documentation
├── README.md
├── getting-started.md
├── manifest.md
├── api-reference.md
├── permissions.md
└── examples/
    ├── dps-calculator.md
    ├── html-exporter.md
    ├── damage-timeline.md
    └── critical-hit-parser.md
```

## Documentation

Comprehensive documentation is available in `docs/plugins/`:

- [README](../../docs/plugins/README.md) - Overview and quick start
- [Getting Started](../../docs/plugins/getting-started.md) - Create your first plugin
- [Manifest Reference](../../docs/plugins/manifest.md) - Plugin configuration
- [API Reference](../../docs/plugins/api-reference.md) - Complete SDK documentation
- [Permissions Guide](../../docs/plugins/permissions.md) - Security and permissions
- [Examples](../../docs/plugins/examples/) - Sample plugins for each type

## Future Enhancements

- [ ] Plugin marketplace/repository
- [ ] Automatic update checking
- [ ] Plugin dependency resolution
- [ ] Plugin settings UI generation
- [ ] Plugin inter-communication API

## Original Requirements (All Met)

*   ✅ **Plugin Architecture:** Clear and well-documented plugin architecture including:
    *   ✅ An API for plugins to access parsed data
    *   ✅ Hooks into the UI for plugins to add tabs, windows, or widgets
    *   ✅ A manifest file for each plugin to declare dependencies and capabilities
*   ✅ **Plugin Manager:** UI for users to browse, install, uninstall, and manage plugins
*   ✅ **Sandboxing:** Security model to sandbox plugins, preventing access to sensitive data

## Technical Notes

- Uses .NET `AssemblyLoadContext` for full assembly isolation and unloading
- Plugins receive immutable copies of combat data (not references)
- All external APIs accessed through sandboxed proxy objects
- Permission grants persisted to `permissions.json`
- Supports Authenticode-signed assemblies for trusted plugins
