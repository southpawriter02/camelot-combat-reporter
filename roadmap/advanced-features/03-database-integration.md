# 3. Database Integration

## Status: ✅ Complete

**Implementation Location:** `src/database/`

**Key Components:**
- `DatabaseAdapter` - Abstract interface for database operations
- `SQLiteAdapter` - SQLite implementation with full query builder support
- `EventQueryBuilder` - Fluent query API for combat events
- `SessionQueryBuilder` - Fluent query API for combat sessions
- `StatsQueryBuilder` - Aggregation queries for statistics and leaderboards
- Schema with migrations support

**Supported Databases:**
- SQLite (included) - Zero-dependency local storage
- PostgreSQL support planned via adapter pattern

**Test Coverage:** Comprehensive unit tests in `tests/unit/database/`

---

## Description

Instead of relying on local files for storing historical data, this feature proposes integrating a proper database system. This would provide a more robust, scalable, and performant solution for data storage and retrieval.

## Functionality

*   **Database Schema:** Design a database schema to store parsed log data, combat sessions, player statistics, and other relevant information.
*   **Data Persistence:**
    *   Save all parsed data to the database.
    *   Update player statistics and other records as new data is parsed.
*   **Querying Interface:** Create an abstraction layer for querying the database, allowing other features to easily access the data they need.
*   **Database Options:** Support for different database backends, such as:
    *   **Local:** SQLite for single-user, local installations.
    *   **Server-based:** PostgreSQL or MySQL for a centralized, multi-user setup (e.g., for a guild).

## Requirements

*   A database management system (e.g., SQLite, PostgreSQL).
*   An ORM (Object-Relational Mapping) library or a database driver for the chosen language (e.g., SQLAlchemy for Python, Entity Framework for C#).
*   Knowledge of SQL and database design principles.

## Limitations

*   Adds a dependency on a database system, which can complicate the installation and setup process for users.
*   Requires careful schema design and data migration strategies if the schema evolves over time.

## Dependencies

*   **03-player-statistics.md:** This feature would replace the file-based storage mechanism of the player statistics feature.
*   Could be a foundational component for other advanced features like **04-api-exposure.md**.
