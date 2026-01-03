# 4. Loot Drop Rate Tracking

## Status: 📋 Planned

**Prerequisites:**
- ✅ Log parsing infrastructure
- ✅ Database integration (TypeScript)
- ⬚ Loot event parsing patterns

---

## Description

Track and analyze loot drop rates by mob type, providing statistical insights into item acquisition. This feature enables players to understand drop frequencies, plan farming sessions, and share community drop rate data.

## Functionality

### Core Features

* **Loot Event Parsing:**
  * Parse loot messages from combat logs (e.g., "You receive [Item Name] from [Mob Name]")
  * Support for multiple loot message formats across game versions
  * Track currency drops (gold, silver, copper)
  * Capture realm points and bounty points from kills

* **Drop Rate Statistics:**
  * Calculate drop rate percentages per item per mob
  * Track sample size (kills) for statistical confidence
  * Display confidence intervals for rare drops
  * Separate statistics by mob difficulty/type (normal, named, boss)

* **Mob Database:**
  * Build local database of encountered mobs
  * Group mobs by zone, level range, and type
  * Track total kills per mob across sessions
  * Associate loot tables with specific mobs

* **Item Tracking:**
  * Categorize items by type (weapon, armor, consumable, material)
  * Track item rarity/quality indicators
  * Support for ROG (randomly-generated) item tracking
  * Identify valuable drops based on user preferences

### Analysis Views

* **Mob Loot Table View:**
  * Select a mob to see all recorded drops
  * Show drop rate, total drops, and sample size
  * Sort by rarity, value, or frequency
  * Filter by item type

* **Item Source View:**
  * Select an item to see which mobs drop it
  * Compare drop rates across different sources
  * Identify optimal farming locations

* **Session Summary:**
  * Total loot value per session
  * Drops per hour metrics
  * Highlight notable drops

### Export & Sharing

* **Export Formats:**
  * CSV for spreadsheet analysis
  * JSON for community databases
  * Markdown tables for wiki contributions

* **Community Integration:**
  * Anonymized drop rate sharing
  * Import community drop rate data
  * Merge personal data with community averages

## Requirements

* **Log Parsing:** New regex patterns for loot messages
* **Storage:** SQLite or JSON for local loot database
* **UI:** New "Loot Tracking" tab in GUI

## Limitations

* Accuracy depends on consistent log formatting
* Some item names may be ambiguous without context
* Server-specific loot tables may vary
* ROG items have variable properties not captured by name alone

## Dependencies

* **01-log-parsing.md:** Core parsing infrastructure
* **03-database-integration.md:** Data storage for loot history
* **06-server-type-filters.md:** Different servers may have different loot tables

## Implementation Phases

### Phase 1: Core Parsing
- [ ] Identify loot message patterns in DAoC logs
- [ ] Create LootEvent model class
- [ ] Add loot parsing to LogParser
- [ ] Basic loot event display in UI

### Phase 2: Statistics Engine
- [ ] Create LootStatisticsService
- [ ] Implement drop rate calculations
- [ ] Add confidence interval computation
- [ ] Build mob/item database schema

### Phase 3: GUI Integration
- [ ] Design Loot Tracking tab
- [ ] Implement mob browser view
- [ ] Create item search functionality
- [ ] Add session loot summary

### Phase 4: Export & Community
- [ ] CSV/JSON export functionality
- [ ] Community data import
- [ ] Wiki-format export

## Technical Notes

* Consider using bloom filters for efficient item lookup
* Store raw events separately from aggregated statistics
* Use background processing for large log files
* Consider incremental parsing for real-time tracking
