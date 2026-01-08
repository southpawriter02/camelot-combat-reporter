using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using EnemyEncounterDatabase.Models;

namespace EnemyEncounterDatabase.Views;

/// <summary>
/// Converts an <see cref="EnemyType"/> to a color brush for visual distinction.
/// </summary>
/// <remarks>
/// Color mapping:
/// - Player: Red (#E74C3C) - danger/enemy
/// - Mob: Green (#27AE60) - PvE
/// - NPC: Blue (#3498DB) - neutral
/// - Unknown: Gray (#95A5A6)
/// </remarks>
public class EnemyTypeToBrushConverter : IValueConverter
{
    public static readonly EnemyTypeToBrushConverter Instance = new();

    private static readonly SolidColorBrush PlayerBrush = new(Color.Parse("#E74C3C"));
    private static readonly SolidColorBrush MobBrush = new(Color.Parse("#27AE60"));
    private static readonly SolidColorBrush NpcBrush = new(Color.Parse("#3498DB"));
    private static readonly SolidColorBrush UnknownBrush = new(Color.Parse("#95A5A6"));

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value switch
        {
            EnemyType.Player => PlayerBrush,
            EnemyType.Mob => MobBrush,
            EnemyType.NPC => NpcBrush,
            _ => UnknownBrush
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>
/// Converts an <see cref="EnemyType"/> to a display label.
/// </summary>
public class EnemyTypeToLabelConverter : IValueConverter
{
    public static readonly EnemyTypeToLabelConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value switch
        {
            EnemyType.Player => "PLAYER",
            EnemyType.Mob => "MOB",
            EnemyType.NPC => "NPC",
            _ => "?"
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>
/// Converts a win rate (0-100) to a color brush.
/// Green for high win rates, red for low.
/// </summary>
public class WinRateToColorConverter : IValueConverter
{
    public static readonly WinRateToColorConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not double winRate)
            return new SolidColorBrush(Color.Parse("#95A5A6"));

        // Gradient from red (0%) to yellow (50%) to green (100%)
        if (winRate >= 70)
            return new SolidColorBrush(Color.Parse("#27AE60")); // Green
        if (winRate >= 50)
            return new SolidColorBrush(Color.Parse("#F39C12")); // Orange/Yellow
        if (winRate >= 30)
            return new SolidColorBrush(Color.Parse("#E67E22")); // Orange
        return new SolidColorBrush(Color.Parse("#E74C3C")); // Red
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>
/// Converts an <see cref="EncounterOutcome"/> to a display icon/text.
/// </summary>
public class OutcomeToDisplayConverter : IValueConverter
{
    public static readonly OutcomeToDisplayConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value switch
        {
            EncounterOutcome.Victory => "✓ Victory",
            EncounterOutcome.Defeat => "✗ Defeat",
            EncounterOutcome.Escaped => "↗ Escaped",
            _ => "? Unknown"
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>
/// Converts an <see cref="EncounterOutcome"/> to a color brush.
/// </summary>
public class OutcomeToColorConverter : IValueConverter
{
    public static readonly OutcomeToColorConverter Instance = new();

    private static readonly SolidColorBrush VictoryBrush = new(Color.Parse("#27AE60"));
    private static readonly SolidColorBrush DefeatBrush = new(Color.Parse("#E74C3C"));
    private static readonly SolidColorBrush EscapedBrush = new(Color.Parse("#F39C12"));
    private static readonly SolidColorBrush UnknownBrush = new(Color.Parse("#95A5A6"));

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value switch
        {
            EncounterOutcome.Victory => VictoryBrush,
            EncounterOutcome.Defeat => DefeatBrush,
            EncounterOutcome.Escaped => EscapedBrush,
            _ => UnknownBrush
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>
/// Converts a boolean favorite status to a star icon.
/// </summary>
public class BoolToStarConverter : IValueConverter
{
    public static readonly BoolToStarConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is true ? "★" : "☆";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>
/// Converts a boolean favorite status to button text.
/// </summary>
public class BoolToFavoriteTextConverter : IValueConverter
{
    public static readonly BoolToFavoriteTextConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is true ? "★ Favorited" : "☆ Add Favorite";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>
/// Converts win rate (0-100) to a normalized value (0-1) for progress bars.
/// </summary>
public class WinRateToProgressConverter : IValueConverter
{
    public static readonly WinRateToProgressConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double winRate)
            return winRate / 100.0;
        return 0.0;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
