# Plugin Ideas

This directory contains detailed specifications for plugins that can be developed using the Camelot Combat Reporter Plugin SDK. These serve as inspiration for third-party developers and as a roadmap for official plugin development.

## Plugin System Overview

The Plugin SDK supports four types of plugins:

| Type | Base Class | Purpose |
|------|------------|---------|
| **Data Analysis** | `DataAnalysisPluginBase` | Compute custom statistics and insights |
| **Export Format** | `ExportPluginBase` | Export to custom file formats |
| **UI Component** | `UIPluginBase` | Add tabs, panels, charts, and widgets |
| **Custom Parser** | `ParserPluginBase` | Parse additional log message types |

See the [Plugin Developer Guide](../../docs/plugins/getting-started.md) for development instructions.

---

## Plugin Ideas

### Data Analysis Plugins

| Plugin | Description | Priority |
|--------|-------------|----------|
| [Combat Style Optimizer](01-combat-style-optimizer.md) | Analyze combat style chains and recommend optimal rotations | High |
| [Realm Points Calculator](02-realm-points-calculator.md) | Calculate RP gains and project realm rank progression | High |
| [Group Performance Analyzer](03-group-performance-analyzer.md) | Evaluate individual contribution to group success | Medium |
| [Heal Efficiency Tracker](09-heal-efficiency-tracker.md) | Track overhealing, effective healing, and heal targets | High |
| [PvP Matchup Analyzer](10-pvp-matchup-analyzer.md) | Win/loss tracking against specific classes and players | Medium |

### Export Plugins

| Plugin | Description | Priority |
|--------|-------------|----------|
| [HTML Report Exporter](04-html-report-exporter.md) | Generate shareable HTML combat reports | High |
| [Combat Log Merger](07-combat-log-merger.md) | Merge multiple log files into unified analysis | Medium |

### UI Component Plugins

| Plugin | Description | Priority |
|--------|-------------|----------|
| [Damage Breakdown Chart](05-damage-breakdown-chart.md) | Interactive sunburst/treemap damage visualizations | High |
| [Enemy Encounter Database](06-enemy-encounter-database.md) | Track and browse historical enemy encounters | Medium |

### Integration Plugins

| Plugin | Description | Priority |
|--------|-------------|----------|
| [Discord Bot Integration](08-discord-bot-integration.md) | Post combat summaries and stats to Discord | Medium |

---

## Priority Legend

- **High**: Core functionality that enhances the main application significantly
- **Medium**: Useful feature that adds value for specific use cases
- **Low**: Nice-to-have feature for niche scenarios

---

## Contributing Plugin Ideas

Have an idea for a plugin? Consider:

1. What problem does it solve?
2. Which plugin type is most appropriate?
3. What permissions would it require?
4. How does it integrate with existing features?

Submit ideas via GitHub Issues or create a PR with a new plugin specification document.

---

## Development Resources

- [Getting Started Guide](../../docs/plugins/getting-started.md)
- [API Reference](../../docs/plugins/api-reference.md)
- [Manifest Reference](../../docs/plugins/manifest.md)
- [Permissions Guide](../../docs/plugins/permissions.md)
- [Example Plugins](../../docs/plugins/examples/)
