using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;

namespace DamageBreakdownChart.Models;

/// <summary>
/// Color schemes for damage chart visualization.
/// </summary>
public static class ChartColors
{
    /// <summary>
    /// Colors for specific damage types (DAoC damage types).
    /// </summary>
    public static readonly Dictionary<string, SKColor> DamageTypeColors = new()
    {
        ["Slash"] = SKColor.Parse("#E53935"),
        ["Crush"] = SKColor.Parse("#8E24AA"),
        ["Thrust"] = SKColor.Parse("#1E88E5"),
        ["Heat"] = SKColor.Parse("#FF5722"),
        ["Cold"] = SKColor.Parse("#00BCD4"),
        ["Matter"] = SKColor.Parse("#795548"),
        ["Body"] = SKColor.Parse("#4CAF50"),
        ["Spirit"] = SKColor.Parse("#9C27B0"),
        ["Energy"] = SKColor.Parse("#FFEB3B"),
        ["Physical"] = SKColor.Parse("#607D8B"),
        ["Magical"] = SKColor.Parse("#673AB7")
    };

    /// <summary>
    /// General category colors for ability types.
    /// </summary>
    public static readonly SKColor[] CategoryColors =
    [
        SKColor.Parse("#2196F3"), // Blue
        SKColor.Parse("#4CAF50"), // Green
        SKColor.Parse("#FF9800"), // Orange
        SKColor.Parse("#9C27B0"), // Purple
        SKColor.Parse("#00BCD4"), // Cyan
        SKColor.Parse("#F44336"), // Red
        SKColor.Parse("#3F51B5"), // Indigo
        SKColor.Parse("#FFEB3B"), // Yellow
        SKColor.Parse("#795548"), // Brown
        SKColor.Parse("#607D8B")  // Blue Grey
    ];

    /// <summary>
    /// Gets the color for a damage node.
    /// </summary>
    public static SKColor GetColor(DamageNode node, int index)
    {
        if (node.Type == DamageNodeType.DamageType &&
            DamageTypeColors.TryGetValue(node.Name, out var color))
        {
            return color;
        }

        return CategoryColors[index % CategoryColors.Length];
    }

    /// <summary>
    /// Creates a SolidColorPaint for LiveCharts.
    /// </summary>
    public static SolidColorPaint GetPaint(DamageNode node, int index)
    {
        return new SolidColorPaint(GetColor(node, index));
    }

    /// <summary>
    /// Creates a slightly lighter version of a color for hover effects.
    /// </summary>
    public static SKColor Lighten(SKColor color, float amount = 0.2f)
    {
        color.ToHsl(out float h, out float s, out float l);
        l = Math.Min(1.0f, l + amount);
        return SKColor.FromHsl(h, s * 100, l * 100);
    }
}
