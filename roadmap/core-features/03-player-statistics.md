# 3. Player Statistics

## Status: ✅ Complete

**Implementation Location:** `src/statistics/`

**Key Components:**
- `PlayerStatistics` - Per-player aggregate statistics
- `StatisticsCalculator` - Calculates totals, averages, trends
- `PerformanceMetrics` - DPS/HPS/DTPS calculations with time windows
- `LeaderboardGenerator` - Rankings by various metrics
- Integration with database for persistent storage

**Test Coverage:** Comprehensive unit tests in `tests/unit/statistics/`

---

## Description

This feature focuses on tracking and presenting long-term statistics for the player. While combat analysis provides per-session insights, player statistics will offer a historical view of performance, allowing players to track their progress over time.

## Functionality

*   **Persistent Storage:** Store parsed data from multiple sessions in a local database or file.
*   **Overall Statistics:**
    *   Total damage dealt, healing done, and damage taken across all sessions.
    *   Kill/Death ratio.
    *   Most used spells/styles.
    *   Statistics against specific classes or players.
*   **Performance Graphs:** Visualize player statistics over time with graphs and charts. For example:
    *   DPS trends over the last month.
    *   Healing output per session.
    *   Damage taken from different sources.
*   **Filtering and Comparison:** Allow users to filter their statistics by date range, character, or specific encounters. They should also be able to compare their performance across different time periods.

## Requirements

*   A mechanism for storing and retrieving historical data (e.g., SQLite, JSON files).
*   A library for creating charts and graphs (e.g., D3.js, Chart.js if a web-based UI is used).

## Limitations

*   The accuracy of historical data depends on the user consistently using the log parser for all their play sessions.
*   Storing large amounts of historical data could lead to performance issues if not managed correctly.

## Dependencies

*   **01-log-parsing.md:** Requires parsed data.
*   **02-combat-analysis.md:** Builds upon the analysis concepts to generate long-term stats.
