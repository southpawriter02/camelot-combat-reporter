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
| [Log Parsing](core-features/01-log-parsing.md) | ✅ Complete | `src/parser/` |
| [Combat Analysis](core-features/02-combat-analysis.md) | ✅ Complete | `src/analysis/` |
| [Player Statistics](core-features/03-player-statistics.md) | ✅ Complete | `src/statistics/` |
| [Timeline View](core-features/04-timeline-view.md) | ✅ Complete | `src/timeline/` |

### Advanced Features (4/4 Complete)

| Feature | Status | Location |
|---------|--------|----------|
| [UI Dashboard](advanced-features/01-ui-dashboard.md) | ⏸️ Deferred | — |
| [Real-Time Parsing](advanced-features/02-real-time-parsing.md) | ✅ Complete | `src/streaming/` |
| [Database Integration](advanced-features/03-database-integration.md) | ✅ Complete | `src/database/` |
| [API Exposure](advanced-features/04-api-exposure.md) | ✅ Complete | `src/api/` |

### Future Enhancements (0/3 Complete)

| Feature | Status | Location |
|---------|--------|----------|
| [Plugin System](future-enhancements/01-plugin-system.md) | 📋 Planned | — |
| [Machine Learning Insights](future-enhancements/02-machine-learning-insights.md) | 📋 Planned | — |
| [Cross-Realm Analysis](future-enhancements/03-cross-realm-analysis.md) | 📋 Planned | — |

**Legend:**
- ✅ Complete - Feature fully implemented and tested
- ⏸️ Deferred - Implementation postponed (library-first approach)
- 📋 Planned - Not yet started
