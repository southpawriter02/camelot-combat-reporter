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

### Future Enhancements (2/18 Complete)

| Feature | Status | Location |
|---------|--------|----------|
| [Plugin System](future-enhancements/01-plugin-system.md) | ✅ Complete | `src/CamelotCombatReporter.Plugins/`, `src/CamelotCombatReporter.PluginSdk/` |
| [Machine Learning Insights](future-enhancements/02-machine-learning-insights.md) | 📋 Planned | `src/ml/` (TypeScript prototype) |
| [Cross-Realm Analysis](future-enhancements/03-cross-realm-analysis.md) | ✅ Phase 1 | `src/CamelotCombatReporter.Core/CrossRealm/` |
| [Loot Drop Rate Tracking](future-enhancements/04-loot-drop-tracking.md) | 📋 Planned | — |
| [Realm Ability Tracking](future-enhancements/05-realm-ability-tracking.md) | 📋 Planned | — |
| [Server Type Filters](future-enhancements/06-server-type-filters.md) | 📋 Planned | — |
| [Distribution Builds](future-enhancements/07-distribution-builds.md) | 📋 Planned | — |
| [Chat Filtering](future-enhancements/08-chat-filtering.md) | 📋 Planned | — |
| [Group Composition Analysis](future-enhancements/09-group-composition-analysis.md) | 📋 Planned | — |
| [Keep and Siege Tracking](future-enhancements/10-keep-siege-tracking.md) | 📋 Planned | — |
| [Combat Replay System](future-enhancements/11-combat-replay.md) | 📋 Planned | — |
| [Voice Chat Integration](future-enhancements/12-voice-integration.md) | 📋 Planned | — |
| [In-Game Overlay HUD](future-enhancements/13-overlay-hud.md) | 📋 Planned | — |
| [Death Analysis](future-enhancements/14-death-analysis.md) | 📋 Planned | — |
| [Buff/Debuff Tracking](future-enhancements/15-buff-debuff-tracking.md) | 📋 Planned | — |
| [Crowd Control Analysis](future-enhancements/16-crowd-control-analysis.md) | 📋 Planned | — |
| [Combat Alerts](future-enhancements/17-combat-alerts.md) | 📋 Planned | — |
| [Session Comparison](future-enhancements/18-session-comparison.md) | 📋 Planned | — |

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

### Planned Features

#### Loot Drop Rate Tracking
Track and analyze item drop rates by mob type:
- Parse loot messages from combat logs
- Calculate drop rate percentages with confidence intervals
- Build local mob/item database
- Export for community wiki contributions

#### Realm Ability Tracking
Analyze Realm Ability usage and effectiveness:
- Track RA activations and cooldowns
- Measure damage/healing contribution per RA
- Monitor realm point progression
- RA spec optimization suggestions

#### Server Type Filters
Context-aware parsing based on server ruleset:
- Support for Classic, SI, ToA, and Live servers
- Filter classes and abilities by era
- Custom profiles for private servers
- Era-appropriate stat caps and mechanics

#### Distribution Builds
Distributable executables for end users:
- Windows: MSI/MSIX installers, portable .exe
- macOS: DMG with notarization, Homebrew cask
- Linux: AppImage, .deb, .rpm, Snap, Flatpak
- Auto-update mechanism

#### Chat Filtering
Filter non-combat content for cleaner analysis:
- Pre-parse filtering for performance
- Configurable channel filters (say, guild, trade, etc.)
- Keep tactical group messages during combat
- Privacy-safe export options

#### Group Composition Analysis
Analyze group makeup and optimize compositions:
- Detect group members and their classes
- Role distribution analysis (tank, healer, CC, DPS)
- Performance correlation with composition
- Template matching and recommendations

#### Keep and Siege Tracking
Track keep sieges, relic raids, and RvR objectives:
- Door and structure damage tracking
- Siege contribution scoring
- Keep capture history
- Relic raid event detection

#### Combat Replay System
Replay combat encounters in visual timeline format:
- Playback controls (play, pause, speed adjustment)
- Event-by-event stepping
- Annotation and highlight system
- Export to video or shareable format

#### Voice Chat Integration
Integrate with Discord, TeamSpeak, and Mumble:
- Text-to-speech combat announcements
- Voice command support
- Discord Rich Presence and webhooks
- Real-time DPS callouts

#### In-Game Overlay HUD
Real-time statistics overlay on game window:
- Transparent, customizable widget layout
- Live DPS/HPS counters
- Combat timer and K/D display
- Auto-hide when not in combat

#### Death Analysis
Analyze deaths to improve survival:
- Pre-death damage timeline
- Killing blow breakdown
- Missed defensive opportunity detection
- AI-generated survival tips

#### Buff/Debuff Tracking
Track buff uptime and debuff effectiveness:
- Buff duration and expiration monitoring
- Uptime percentage calculations
- Debuff resistance tracking
- Visual buff bar widget

#### Crowd Control Analysis
Analyze CC chains and diminishing returns:
- DR timer tracking per target
- CC chain detection and scoring
- Break rate analysis
- CC contribution metrics

#### Combat Alerts
Real-time notifications for combat events:
- Customizable alert rules and triggers
- Visual, audio, and external notifications
- Low health warnings, kill alerts
- Discord webhook integration

#### Session Comparison
Compare sessions and track trends:
- Side-by-side session analysis
- Trend visualization over time
- Personal best tracking
- Goal setting and progress

### In Progress

#### Machine Learning Insights
- Combat pattern recognition
- Predictive performance analysis
- Personalized improvement suggestions
- Anomaly detection

#### Cross-Realm Analysis Phase 2
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
