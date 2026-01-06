namespace CamelotCombatReporter.Core.Models;

/// <summary>
/// Represents entering combat mode.
/// Example: "[04:33:48] You enter combat mode and target [the siabra mireguard]"
/// </summary>
/// <param name="Timestamp">The time the event occurred.</param>
/// <param name="TargetName">The target when entering combat, if any.</param>
public record CombatModeEnterEvent(
    TimeOnly Timestamp,
    string? TargetName = null
) : LogEvent(Timestamp);

/// <summary>
/// Represents leaving combat mode.
/// Note: DAoC may not have an explicit "leave combat" message,
/// so this may be inferred from time gaps or other signals.
/// </summary>
/// <param name="Timestamp">The time the event occurred.</param>
public record CombatModeExitEvent(
    TimeOnly Timestamp
) : LogEvent(Timestamp);

/// <summary>
/// Represents the player sitting down to rest.
/// Example: "[19:57:09] You sit down.  Type '/stand' or move to stand up."
/// </summary>
/// <param name="Timestamp">The time the event occurred.</param>
public record RestStartEvent(
    TimeOnly Timestamp
) : LogEvent(Timestamp);

/// <summary>
/// Represents the player standing up from rest.
/// Example: "[20:14:33] You stand up."
/// </summary>
/// <param name="Timestamp">The time the event occurred.</param>
public record RestEndEvent(
    TimeOnly Timestamp
) : LogEvent(Timestamp);

/// <summary>
/// Represents a chat log boundary marker.
/// Example: "*** Chat Log Opened: Wed Dec 20 08:26:35 2017"
/// </summary>
/// <param name="Timestamp">The time from the log line (if available).</param>
/// <param name="IsOpened">True if log opened, false if log closed.</param>
/// <param name="LogDateTime">The full datetime from the log boundary message.</param>
public record ChatLogBoundaryEvent(
    TimeOnly Timestamp,
    bool IsOpened,
    DateTime LogDateTime
) : LogEvent(Timestamp);
