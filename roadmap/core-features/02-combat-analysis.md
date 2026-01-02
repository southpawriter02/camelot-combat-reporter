# 2. Combat Analysis

## Status: ✅ Complete

**Implementation Location:** `src/analysis/`

**Key Components:**
- `CombatAnalyzer` - Main analyzer with session detection and fight boundaries
- `DamageAnalyzer` - Damage calculations, DPS, damage type breakdowns
- `HealingAnalyzer` - Healing calculations, HPS, overheal tracking
- `SessionManager` - Combat session detection with configurable thresholds
- `FightSummary` - Per-fight summaries with participants, duration, meters

**Test Coverage:** Comprehensive unit tests in `tests/unit/analysis/`

---

## Description

Once the log data is parsed, the next step is to analyze it to provide meaningful insights into combat encounters. This feature will focus on aggregating the parsed data to generate detailed combat reports.

## Functionality

*   **Combat Session Detection:** Automatically identify the start and end of combat sessions. This could be based on periods of inactivity or manual user input.
*   **Damage and Healing Meters:**
    *   **Damage Dealt:** Total damage, DPS (Damage Per Second), damage breakdown by spell/style, and damage type.
    *   **Damage Taken:** Total damage taken, DTPS (Damage Taken Per Second), and damage breakdown by source.
    *   **Healing Done:** Total healing, HPS (Healing Per Second), and healing breakdown by spell.
    *   **Healing Received:** Total healing received and breakdown by source.
*   **Fight Summaries:** For each distinct fight, provide a summary including:
    *   Duration of the fight.
    *   Participants.
    *   Total damage and healing for each group.
    *   Key events like player deaths.

## Requirements

*   A structured data format for the parsed log information (output from the Log Parsing feature).
*   Algorithms to calculate metrics like DPS, HPS, etc.
*   A way to associate players with their groups/realms.

## Limitations

*   The accuracy of the combat analysis is dependent on the accuracy of the log parser.
*   It might be challenging to accurately determine the start and end of a fight, especially in chaotic RvR (Realm vs. Realm) scenarios.

## Dependencies

*   **01-log-parsing.md:** This feature requires the parsed data from the log parser to function.
