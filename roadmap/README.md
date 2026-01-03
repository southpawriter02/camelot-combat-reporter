# DAoC Log Parser Roadmap

This document outlines the feature roadmap for the DAoC Log Parser project. The project aims to create a powerful tool for players of Dark Age of Camelot to analyze their combat logs, gain insights into their performance, and improve their gameplay.

The roadmap is divided into three main categories:

*   **Core Features:** The essential functionalities that form the foundation of the log parser.
*   **Advanced Features:** More complex features that build upon the core functionalities to provide deeper analysis and a better user experience.
*   **Future Enhancements:** Long-term ideas and suggestions for expanding the project's capabilities.

Each feature is detailed in its own Markdown file within the respective directory. These files provide exhaustive explanations about the intended functionalities, requirements, limitations, and dependencies.

This roadmap is a living document and will be updated as the project evolves. Contributions and suggestions are welcome.

---

## Implementation Status

### Core Features (4/4 Complete)

| Feature | Status | Location |
|---------|--------|----------|
| [Log Parsing](core-features/01-log-parsing.md) | ✅ Complete | `src/CamelotCombatReporter.Core/Parsing/` |
| [Combat Analysis](core-features/02-combat-analysis.md) | ✅ Complete | `src/CamelotCombatReporter.Core/` |
| [Player Statistics](core-features/03-player-statistics.md) | ✅ Complete | `src/CamelotCombatReporter.Core/Models/` |
| [Timeline View](core-features/04-timeline-view.md) | ✅ Complete | `src/CamelotCombatReporter.Gui/` (LiveCharts2) |

### Advanced Features (3/4 Complete)

| Feature | Status | Location |
|---------|--------|----------|
| [UI Dashboard](advanced-features/01-ui-dashboard.md) | ⏸️ Deferred | — |
| [Real-Time Parsing](advanced-features/02-real-time-parsing.md) | ✅ Complete | `src/streaming/` (TypeScript) |
| [Database Integration](advanced-features/03-database-integration.md) | ✅ Complete | `src/database/` (TypeScript) |
| [API Exposure](advanced-features/04-api-exposure.md) | ✅ Complete | `src/api/` (TypeScript) |

### Future Enhancements (2/3 Complete)

| Feature | Status | Location |
|---------|--------|----------|
| [Plugin System](future-enhancements/01-plugin-system.md) | ✅ Complete | `src/CamelotCombatReporter.Plugins/`, `src/CamelotCombatReporter.PluginSdk/` |
| [Machine Learning Insights](future-enhancements/02-machine-learning-insights.md) | 📋 Planned | `src/ml/` (TypeScript prototype) |
| [Cross-Realm Analysis](future-enhancements/03-cross-realm-analysis.md) | ✅ Phase 1 | `src/CamelotCombatReporter.Core/CrossRealm/` |

**Legend:**
- ✅ Complete - Feature fully implemented and tested
- ✅ Phase 1 - Initial implementation complete, future phases planned
- ⏸️ Deferred - Implementation postponed (library-first approach)
- 📋 Planned - Not yet started

---

## Recent Completions

### Plugin System (January 2025)
Full plugin extensibility framework with:
- Plugin SDK for third-party developers
- Sandboxed execution with permission-based security
- Plugin Manager UI in the desktop application
- Comprehensive documentation and examples

See [Plugin Documentation](../docs/plugins/README.md) for details.

### Cross-Realm Analysis Phase 1 (January 2025)
Local-first cross-realm statistics tracking:
- Character configuration (realm, class, level, realm rank)
- Session saving with character context
- Local leaderboards and aggregated statistics
- JSON/CSV export for community sharing

Future phases will add central server support and public leaderboards.

---

## What's Next

### Machine Learning Insights
The only remaining planned feature. Will include:
- Combat pattern recognition
- Predictive performance analysis
- Personalized improvement suggestions
- Anomaly detection

### Cross-Realm Analysis Phase 2
- Central server for community statistics
- Public leaderboards
- Auto-detection of character class from logs
- Opt-in anonymous data sharing

---

## Related Documentation

- [Architecture Overview](../ARCHITECTURE.md)
- [Contributing Guide](../CONTRIBUTING.md)
- [Changelog](../CHANGELOG.md)
- [Plugin Developer Guide](../docs/plugins/getting-started.md)
