namespace DiscordIntegration.Models;

/// <summary>
/// Triggers that can initiate a Discord post.
/// </summary>
[Flags]
public enum PostTrigger
{
    /// <summary>No automatic posting.</summary>
    None = 0,

    /// <summary>Post when a combat session ends.</summary>
    SessionEnd = 1,

    /// <summary>Post when a personal best is achieved.</summary>
    PersonalBest = 2,

    /// <summary>Post for significant kills (configurable threshold).</summary>
    MajorKill = 4,

    /// <summary>Post for milestones like realm rank ups.</summary>
    Milestone = 8,

    /// <summary>Manual posting via UI button.</summary>
    Manual = 16
}

/// <summary>
/// Privacy level for Discord posts.
/// </summary>
public enum PrivacyLevel
{
    /// <summary>Show all details including names.</summary>
    Full,

    /// <summary>Hide enemy player names.</summary>
    Partial,

    /// <summary>Only show aggregate statistics.</summary>
    StatsOnly
}
