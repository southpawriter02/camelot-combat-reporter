namespace CamelotCombatReporter.Gui.Models;

/// <summary>
/// Navigation category for grouping nav items.
/// </summary>
public enum NavCategory
{
    Core,
    Analysis,
    Character,
    Tracking,
    RvR
}

/// <summary>
/// Represents a navigation item in the sidebar.
/// </summary>
public record NavItem
{
    /// <summary>
    /// Display name shown in the sidebar.
    /// </summary>
    public required string Name { get; init; }
    
    /// <summary>
    /// Icon character (emoji or symbol) displayed before the name.
    /// </summary>
    public required string Icon { get; init; }
    
    /// <summary>
    /// Category this item belongs to for grouping.
    /// </summary>
    public required NavCategory Category { get; init; }
    
    /// <summary>
    /// Unique key for navigation routing.
    /// </summary>
    public required string ViewKey { get; init; }
    
    /// <summary>
    /// Optional keyboard shortcut description.
    /// </summary>
    public string? Shortcut { get; init; }
    
    /// <summary>
    /// Display string combining icon and name.
    /// </summary>
    public string DisplayText => $"{Icon}  {Name}";
}

/// <summary>
/// Category header for the navigation sidebar.
/// </summary>
public record NavCategoryHeader
{
    public required string Title { get; init; }
    public required NavCategory Category { get; init; }
    
    public static NavCategoryHeader Core => new() { Title = "CORE", Category = NavCategory.Core };
    public static NavCategoryHeader Analysis => new() { Title = "ANALYSIS", Category = NavCategory.Analysis };
    public static NavCategoryHeader Character => new() { Title = "CHARACTER", Category = NavCategory.Character };
    public static NavCategoryHeader Tracking => new() { Title = "TRACKING", Category = NavCategory.Tracking };
    public static NavCategoryHeader RvR => new() { Title = "RvR", Category = NavCategory.RvR };
}
