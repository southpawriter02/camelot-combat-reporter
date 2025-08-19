# 4. Timeline View

## Description

The Timeline View will provide a detailed, chronological breakdown of a combat encounter. This feature allows users to replay a fight event by event, helping them to understand the flow of battle and analyze critical moments.

## Functionality

*   **Event Log:** Display a time-stamped log of all parsed events in a readable format.
*   **Filtering and Searching:**
    *   Filter the timeline by event type (e.g., show only healing events, or only damage from a specific player).
    *   Search for specific events or players within the timeline.
*   **Interactive Timeline:**
    *   A graphical representation of the fight's duration.
    *   Markers on the timeline for significant events like player deaths, key spell usage (e.g., crowd control, interrupts).
    *   Clicking on an event in the timeline could highlight the corresponding log entry.

## Requirements

*   A UI component capable of displaying a large amount of data in a scrollable and filterable list.
*   A graphical library to render the interactive timeline.

## Limitations

*   The timeline can become cluttered in large-scale fights with many participants. The UI needs to be designed to handle this gracefully.
*   The granularity of the timeline is limited by the timestamps in the log file, which are typically per-second.

## Dependencies

*   **01-log-parsing.md:** This feature is entirely dependent on the parsed log data.
*   **02-combat-analysis.md:** The fight detection from combat analysis is needed to scope the timeline to a specific encounter.
