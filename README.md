<div align="center">

# Camelot Combat Reporter

### *Master Your Combat. Dominate the Realm.*

[![Version](https://img.shields.io/badge/version-1.4.0-brightgreen.svg)](CHANGELOG.md)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![.NET 9.0](https://img.shields.io/badge/.NET-9.0-purple.svg)](https://dotnet.microsoft.com/download/dotnet/9.0)
[![Platform](https://img.shields.io/badge/platform-Windows%20%7C%20macOS%20%7C%20Linux-lightgrey.svg)](#installation)
[![Tests](https://img.shields.io/badge/tests-189%20passing-success.svg)](#testing)

---

**The ultimate combat analysis suite for Dark Age of Camelot players.**

Parse your `chat.log`, visualize your performance, track personal bests, and receive real-time alerts — all in one powerful, cross-platform application.

[**Download Latest Release**](https://github.com/southpawriter02/camelot-combat-reporter/releases) · [**View Roadmap**](roadmap/README.md) · [**Report Bug**](https://github.com/southpawriter02/camelot-combat-reporter/issues)

</div>

---

## What's New in v1.4.0

> **Group Analysis Release** — January 2026

```
 GROUP COMPOSITION         ROLE CLASSIFICATION        RECOMMENDATIONS
┌─────────────────────┐   ┌─────────────────────┐   ┌─────────────────────┐
│  Members: 6         │   │  Tank     ██░░  2   │   │  + Add Healer       │
│  Category: 8-Man    │   │  Healer   ██░░  2   │   │    Critical         │
│  Balance: 85/100    │   │  CC       █░░░  1   │   │  ~ Reduce Tanks     │
│  Template: 8-Man RvR│   │  DPS      ██░░  2   │   │    Low              │
└─────────────────────┘   └─────────────────────┘   └─────────────────────┘
```

**New Features:**
- **Group Detection** — Automatic member detection from healing, buffs, combat patterns
- **Role Classification** — 7 roles with dual-role system for all 48 classes
- **Group Templates** — 6 pre-defined templates (8-Man RvR, Small-Man, etc.)
- **Balance Scoring** — 0-100 score based on composition quality
- **Role Coverage** — Per-role status indicators and member tracking
- **Recommendations** — Priority-based suggestions for composition improvement

---

## Feature Overview

<table>
<tr>
<td width="50%" valign="top">

### Core Analysis
| Feature | Description |
|---------|-------------|
| **Log Parsing** | Regex-based extraction of all combat events |
| **Damage Tracking** | Dealt/received with source attribution |
| **Healing Analysis** | Done/received with effectiveness metrics |
| **Player Statistics** | DPS, HPS, K/D ratios, and more |
| **Timeline Charts** | Interactive LiveCharts2 visualizations |

</td>
<td width="50%" valign="top">

### Combat Intelligence
| Feature | Version |
|---------|---------|
| **Death Analysis** | v1.1.0 |
| **CC & DR Tracking** | v1.1.0 |
| **Realm Abilities** | v1.2.0 |
| **Buff/Debuff Tracking** | v1.2.0 |
| **Combat Alerts** | v1.3.0 |
| **Group Analysis** | v1.4.0 |

</td>
</tr>
</table>

---

## Feature Highlights

<details>
<summary><b>Real-Time Combat Alerts</b> — Get notified when it matters most</summary>

### Alert Conditions
| Condition | Triggers When... |
|-----------|------------------|
| `HealthBelow` | Health drops below threshold (e.g., 30%) |
| `DamageInWindow` | Burst damage exceeds amount in time window |
| `KillStreak` | Kill streak reaches milestone (3, 5, 10...) |
| `EnemyClass` | Targeting specific class (Minstrel, Cleric...) |
| `AbilityUsed` | Realm ability activated (self or enemy) |
| `DebuffApplied` | Specific debuff detected (Disease, Poison...) |

### Notification Types
- **Sound** — Priority-based audio alerts
- **Screen Flash** — Visual notification with configurable color/duration
- **Text-to-Speech** — Spoken alerts with message templates
- **Discord Webhook** — Post alerts to your Discord channel

### Configuration
```
Rule: "Low Health Warning"
├── Priority: Critical
├── Conditions: Health < 25% AND InCombat
├── Notifications: Sound + ScreenFlash + TTS
└── Cooldown: 10 seconds
```

</details>

<details>
<summary><b>Session Comparison & Analytics</b> — Track your improvement over time</summary>

### Delta Calculations
```
┌─────────────────────────────────────────────────────────┐
│                  SESSION COMPARISON                      │
├─────────────────────────────────────────────────────────┤
│  Metric              Base      Compare     Change       │
│  ─────────────────────────────────────────────────────  │
│  Damage Per Second   142.3     168.7       +18.5% ↑    │
│  Healing Per Second   45.2      48.1       + 6.4% ↑    │
│  Kill/Death Ratio     2.1       2.8        +33.3% ↑    │
│  Deaths               8         5          -37.5% ↑    │
│  Critical Hit Rate   24.3%     27.1%       + 2.8% ↑    │
└─────────────────────────────────────────────────────────┘
```

### Trend Analysis Features
- **Linear Regression** — Detect performance trends (improving/declining)
- **Rolling Averages** — Smoothed visualization over time
- **R² Coefficient** — Measure trend reliability
- **Predictions** — Extrapolate expected future performance

### Goal Tracking
```
┌──────────────────────────────────────┐
│  Goal: Reach 200 DPS                 │
│  ████████████████░░░░  78%           │
│  Current: 156 DPS | Target: 200 DPS  │
│  Status: In Progress                 │
└──────────────────────────────────────┘
```

</details>

<details>
<summary><b>Death Analysis</b> — Understand why you died and how to survive</summary>

### Death Categories
| Category | Subcategory | Description |
|----------|-------------|-------------|
| **Burst** | Alpha Strike | Overwhelming damage in <3 seconds |
| **Burst** | Coordinated | Multiple attackers in quick succession |
| **Burst** | CC Chain | Death during crowd control chain |
| **Attrition** | Healing Deficit | Damage outpaced healing over time |
| **Attrition** | Resource Exhaustion | Out of power/endurance |
| **Execution** | Low Health | Finished while already wounded |
| **Execution** | DoT Finish | Killed by damage over time |

### Pre-Death Timeline
```
T-15.0s ─┬─ Mezmerized by Sorcerer
         │
T-12.2s ─┼─ Root applied
         │
T-10.0s ─┼─ 450 damage from Infiltrator (backstab)
         │
T-08.5s ─┼─ 380 damage from Infiltrator (critical)
         │
T-06.0s ─┼─ Stun applied (3s)
         │
T-03.0s ─┼─ 520 damage from Infiltrator (Purge)
         │
T-00.0s ─┴─ DEATH - Killing blow: Infiltrator (892 damage)

Category: Burst (CC Chain)
Recommendation: Consider Purge timing and positioning
```

</details>

<details>
<summary><b>Realm Ability Tracking</b> — Master your RAs for maximum impact</summary>

### Database Coverage
| Realm | Abilities | Examples |
|-------|-----------|----------|
| **Albion** | 35+ | Purge, IP, SoS, Determination |
| **Midgard** | 35+ | Purge, IP, SoS, Raging Power |
| **Hibernia** | 35+ | Purge, IP, SoS, Thornweed Field |
| **Universal** | 20+ | MOC, BAoD, AoM, First Aid |

### Tracking Features
- **Activation Timeline** — When and how often you use RAs
- **Cooldown Efficiency** — Are you using abilities optimally?
- **Damage/Healing Attribution** — Total impact per ability
- **Era Gating** — Classic, SI, ToA, NF, Live ability filtering

</details>

<details>
<summary><b>Buff/Debuff Tracking</b> — Maximize your uptime</summary>

### Categories (40+ definitions)
- Damage Buffs (STR/DEX buffs, damage adds)
- Defensive Buffs (AF, ABS, resistances)
- Speed Buffs (Sprint, Speed)
- Healing Buffs (HoTs, heal procs)
- Crowd Control Effects (Mez, Root, Stun)
- Debuffs (Disease, Poison, Stat debuffs)

### Uptime Analysis
```
┌─────────────────────────────────────────────────────────┐
│  BUFF UPTIME REPORT                                     │
├─────────────────────────────────────────────────────────┤
│  Damage Add          ████████████████████░  95.2%       │
│  AF Buff             ██████████████████░░░  87.3%       │
│  Haste               ████████████████░░░░░  78.1%       │
│  Resistance Buff     ██████████░░░░░░░░░░░  52.4%  ⚠️   │
└─────────────────────────────────────────────────────────┘
                       ⚠️ Critical gap detected!
```

</details>

<details>
<summary><b>Crowd Control Analysis</b> — Track DR and optimize CC chains</summary>

### Diminishing Returns System
| DR Level | Duration | After |
|----------|----------|-------|
| **Full** | 100% | First CC |
| **Reduced** | 50% | Second CC |
| **Minimal** | 25% | Third CC |
| **Immune** | 0% | Fourth+ CC |

*DR resets after 60 seconds without CC of that type*

### CC Chain Detection
```
Target: EnemyPlayer
├── [00:00.0] Mez (9.0s) ── DR: Full
├── [00:08.5] Stun (4.5s) ── DR: Full (different type)
├── [00:13.0] Mez (4.5s) ── DR: Reduced
└── [00:17.0] Mez (2.3s) ── DR: Minimal

Chain Duration: 19.3s | Efficiency: 87%
```

</details>

---

## Installation

### Download Pre-Built Releases

Download the latest release for your platform from [**Releases**](https://github.com/southpawriter02/camelot-combat-reporter/releases):

| Platform | Download | Requirements |
|----------|----------|--------------|
| **Windows** | `CamelotCombatReporter-win-x64.zip` | Windows 10+ |
| **macOS (Intel)** | `CamelotCombatReporter-osx-x64.zip` | macOS 11+ |
| **macOS (Apple Silicon)** | `CamelotCombatReporter-osx-arm64.zip` | macOS 11+ |
| **Linux** | `CamelotCombatReporter-linux-x64.zip` | Ubuntu 20.04+ or equivalent |

> **Note:** All releases are self-contained — no .NET runtime installation required.

### Build from Source

```bash
# Clone the repository
git clone https://github.com/southpawriter02/camelot-combat-reporter.git
cd camelot-combat-reporter

# Build all projects
dotnet build

# Run tests
dotnet test

# Run the GUI application
dotnet run --project src/CamelotCombatReporter.Gui
```

**Requirements:** [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) or later

---

## Quick Start

### 1. Launch the Application

```bash
# GUI (recommended)
dotnet run --project src/CamelotCombatReporter.Gui

# CLI
dotnet run --project src/CamelotCombatReporter.Cli -- <path_to_log_file>
```

### 2. Import Your Combat Log

- **Drag & Drop** your `chat.log` file into the application
- Or use **File → Open** to browse

### 3. Explore Your Data

| Tab | What You'll Find |
|-----|------------------|
| **Dashboard** | Overview statistics and timeline |
| **Damage** | Dealt/received breakdown by source |
| **Healing** | Healing analysis with effectiveness |
| **Deaths** | Death analysis with recommendations |
| **CC Analysis** | Crowd control and DR tracking |
| **Realm Abilities** | RA usage statistics |
| **Buff Tracking** | Buff uptime and gap analysis |
| **Alerts** | Configure real-time notifications |
| **Session Comparison** | Compare sessions and track trends |
| **Group Analysis** | Composition analysis and role coverage |

---

## Project Architecture

```
camelot-combat-reporter/
├── src/
│   ├── CamelotCombatReporter.Core/     # Core parsing and analysis
│   │   ├── Parsing/                    # Log parser and event extraction
│   │   ├── Models/                     # Data models and events
│   │   ├── DeathAnalysis/              # Death categorization
│   │   ├── CrowdControlAnalysis/       # CC and DR tracking
│   │   ├── RealmAbilities/             # RA database and tracking
│   │   ├── BuffTracking/               # Buff/debuff monitoring
│   │   ├── Alerts/                     # Alert engine and conditions
│   │   ├── Comparison/                 # Session comparison services
│   │   └── GroupAnalysis/              # Group composition analysis
│   │
│   ├── CamelotCombatReporter.Gui/      # Avalonia UI application
│   │   ├── Views/                      # XAML views
│   │   ├── ViewModels/                 # MVVM view models
│   │   └── [Feature]/                  # Feature-specific GUI
│   │
│   ├── CamelotCombatReporter.Cli/      # Command-line interface
│   ├── CamelotCombatReporter.Plugins/  # Plugin host infrastructure
│   └── CamelotCombatReporter.PluginSdk/# Plugin development SDK
│
├── tests/                              # Unit tests (189 tests)
├── data/                               # Sample logs and resources
├── docs/                               # Documentation
└── roadmap/                            # Feature roadmap
```

---

## Roadmap

<table>
<tr>
<th>Version</th>
<th>Focus</th>
<th>Status</th>
</tr>
<tr>
<td><b>v1.0.0</b></td>
<td>Core Parsing, GUI, Plugins, Loot Tracking</td>
<td>✅ Released</td>
</tr>
<tr>
<td><b>v1.1.0</b></td>
<td>Death Analysis, CC Tracking, Server Profiles</td>
<td>✅ Released</td>
</tr>
<tr>
<td><b>v1.2.0</b></td>
<td>Realm Abilities, Buff/Debuff Tracking</td>
<td>✅ Released</td>
</tr>
<tr>
<td><b>v1.3.0</b></td>
<td>Combat Alerts, Session Comparison, Trends</td>
<td>✅ Released</td>
</tr>
<tr>
<td><b>v1.4.0</b></td>
<td>Group Composition Analysis, Role Classification</td>
<td>✅ Released</td>
</tr>
<tr>
<td><b>v1.5.0</b></td>
<td>Keep/Siege Tracking, RvR Features</td>
<td>📋 Planned</td>
</tr>
<tr>
<td><b>v2.0.0</b></td>
<td>Combat Replay, Overlay HUD, ML Insights</td>
<td>📋 Planned</td>
</tr>
</table>

**Progress:** 13/18 major features complete

See the [**Full Roadmap**](roadmap/README.md) for detailed feature specifications.

---

## Plugin System

Extend Camelot Combat Reporter with custom plugins:

```csharp
[Plugin("MyPlugin", "1.0.0", "Your Name")]
public class MyPlugin : PluginBase
{
    public override void OnSessionLoaded(SessionData session)
    {
        // React to session data
        var totalDamage = session.Events
            .OfType<DamageEvent>()
            .Sum(e => e.Amount);

        Logger.Info($"Total damage: {totalDamage}");
    }
}
```

**Plugin Capabilities:**
- Access parsed combat data
- Register custom UI panels
- Export to custom formats
- Integrate with external services

See [**Plugin Development Guide**](docs/plugins/README.md) for full documentation.

---

## Documentation

| Document | Description |
|----------|-------------|
| [**Architecture**](ARCHITECTURE.md) | System design, data flow, component interaction |
| [**Contributing**](CONTRIBUTING.md) | Development setup, coding standards, PR process |
| [**Changelog**](CHANGELOG.md) | Version history and detailed release notes |
| [**Roadmap**](roadmap/README.md) | Feature specifications and implementation status |
| [**Plugin Guide**](docs/plugins/README.md) | Build and distribute custom plugins |

---

## Testing

```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run specific test project
dotnet test tests/CamelotCombatReporter.Core.Tests
```

**Current Status:** 121 tests passing (101 Core + 20 GUI)

---

## Contributing

We welcome contributions! Please see our [**Contributing Guide**](CONTRIBUTING.md) for:

- Setting up your development environment
- Code style and conventions
- Submitting pull requests
- Reporting bugs and requesting features

---

## License

This project is licensed under the **MIT License** — see the [LICENSE](LICENSE) file for details.

---

<div align="center">

### Built with

[![.NET](https://img.shields.io/badge/.NET_9.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![Avalonia](https://img.shields.io/badge/Avalonia_UI-7B2BFC?style=for-the-badge&logo=data:image/svg+xml;base64,PHN2ZyB4bWxucz0iaHR0cDovL3d3dy53My5vcmcvMjAwMC9zdmciIHZpZXdCb3g9IjAgMCAyNCAyNCI+PHBhdGggZmlsbD0iI2ZmZiIgZD0iTTEyIDJMMiAyMmgyMEwxMiAyeiIvPjwvc3ZnPg==&logoColor=white)](https://avaloniaui.net/)
[![LiveCharts](https://img.shields.io/badge/LiveCharts2-FF6B6B?style=for-the-badge&logoColor=white)](https://livecharts.dev/)

---

**Made for the DAoC community**

*"For Albion! For Midgard! For Hibernia!"*

</div>
