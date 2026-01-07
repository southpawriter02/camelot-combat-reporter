using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Data.Converters;

namespace CamelotCombatReporter.Gui.Converters;

/// <summary>
/// Converts navigation key strings to TabControl indices.
/// </summary>
public class NavKeyToIndexConverter : IValueConverter
{
    private static readonly Dictionary<string, int> NavKeyToIndex = new()
    {
        ["CombatAnalysis"] = 0,
        ["CrossRealm"] = 1,
        ["CharacterProfiles"] = 2,
        ["LootTracking"] = 3,
        ["DeathAnalysis"] = 4,
        ["CCAnalysis"] = 5,
        ["RealmAbilities"] = 6,
        ["BuffTracking"] = 7,
        ["Alerts"] = 8,
        ["SessionComparison"] = 9,
        ["GroupAnalysis"] = 10,
        ["SiegeTracking"] = 11,
        ["RelicTracking"] = 12,
        ["Battlegrounds"] = 13,
    };

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string navKey && NavKeyToIndex.TryGetValue(navKey, out var index))
        {
            return index;
        }
        return 0;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int index)
        {
            foreach (var kvp in NavKeyToIndex)
            {
                if (kvp.Value == index)
                    return kvp.Key;
            }
        }
        return "CombatAnalysis";
    }
}
