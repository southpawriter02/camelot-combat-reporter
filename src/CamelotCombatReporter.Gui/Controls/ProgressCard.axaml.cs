using Avalonia;
using Avalonia.Controls;

namespace CamelotCombatReporter.Gui.Controls;

/// <summary>
/// A card control that displays progress information with title, description, and progress bar.
/// </summary>
public partial class ProgressCard : UserControl
{
    /// <summary>
    /// Defines the Title property.
    /// </summary>
    public static readonly StyledProperty<string> TitleProperty =
        AvaloniaProperty.Register<ProgressCard, string>(nameof(Title), "Loading...");

    /// <summary>
    /// Defines the Description property.
    /// </summary>
    public static readonly StyledProperty<string?> DescriptionProperty =
        AvaloniaProperty.Register<ProgressCard, string?>(nameof(Description));

    /// <summary>
    /// Defines the StatusText property.
    /// </summary>
    public static readonly StyledProperty<string?> StatusTextProperty =
        AvaloniaProperty.Register<ProgressCard, string?>(nameof(StatusText));

    /// <summary>
    /// Defines the Value property.
    /// </summary>
    public static readonly StyledProperty<double> ValueProperty =
        AvaloniaProperty.Register<ProgressCard, double>(nameof(Value), 0);

    /// <summary>
    /// Defines the Minimum property.
    /// </summary>
    public static readonly StyledProperty<double> MinimumProperty =
        AvaloniaProperty.Register<ProgressCard, double>(nameof(Minimum), 0);

    /// <summary>
    /// Defines the Maximum property.
    /// </summary>
    public static readonly StyledProperty<double> MaximumProperty =
        AvaloniaProperty.Register<ProgressCard, double>(nameof(Maximum), 100);

    /// <summary>
    /// Defines the IsIndeterminate property.
    /// </summary>
    public static readonly StyledProperty<bool> IsIndeterminateProperty =
        AvaloniaProperty.Register<ProgressCard, bool>(nameof(IsIndeterminate), false);

    /// <summary>
    /// Defines the ShowPercentage property.
    /// </summary>
    public static readonly StyledProperty<bool> ShowPercentageProperty =
        AvaloniaProperty.Register<ProgressCard, bool>(nameof(ShowPercentage), true);

    /// <summary>
    /// Gets or sets the title text.
    /// </summary>
    public string Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    /// <summary>
    /// Gets or sets the description text.
    /// </summary>
    public string? Description
    {
        get => GetValue(DescriptionProperty);
        set => SetValue(DescriptionProperty, value);
    }

    /// <summary>
    /// Gets or sets the status text shown in the top right.
    /// </summary>
    public string? StatusText
    {
        get => GetValue(StatusTextProperty);
        set => SetValue(StatusTextProperty, value);
    }

    /// <summary>
    /// Gets or sets the progress value.
    /// </summary>
    public double Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    /// <summary>
    /// Gets or sets the minimum value.
    /// </summary>
    public double Minimum
    {
        get => GetValue(MinimumProperty);
        set => SetValue(MinimumProperty, value);
    }

    /// <summary>
    /// Gets or sets the maximum value.
    /// </summary>
    public double Maximum
    {
        get => GetValue(MaximumProperty);
        set => SetValue(MaximumProperty, value);
    }

    /// <summary>
    /// Gets or sets whether the progress is indeterminate.
    /// </summary>
    public bool IsIndeterminate
    {
        get => GetValue(IsIndeterminateProperty);
        set => SetValue(IsIndeterminateProperty, value);
    }

    /// <summary>
    /// Gets or sets whether to show percentage instead of value/max.
    /// </summary>
    public bool ShowPercentage
    {
        get => GetValue(ShowPercentageProperty);
        set => SetValue(ShowPercentageProperty, value);
    }

    /// <summary>
    /// Gets the formatted progress text.
    /// </summary>
    public string ProgressText
    {
        get
        {
            if (IsIndeterminate) return "";

            if (ShowPercentage)
            {
                var range = Maximum - Minimum;
                var percentage = range > 0 ? ((Value - Minimum) / range) * 100 : 0;
                return $"{percentage:F0}%";
            }

            return $"{Value:F0} / {Maximum:F0}";
        }
    }

    public ProgressCard()
    {
        InitializeComponent();
    }

    static ProgressCard()
    {
        ValueProperty.Changed.AddClassHandler<ProgressCard>((x, _) =>
            x.RaisePropertyChanged(nameof(ProgressText)));
        MinimumProperty.Changed.AddClassHandler<ProgressCard>((x, _) =>
            x.RaisePropertyChanged(nameof(ProgressText)));
        MaximumProperty.Changed.AddClassHandler<ProgressCard>((x, _) =>
            x.RaisePropertyChanged(nameof(ProgressText)));
        IsIndeterminateProperty.Changed.AddClassHandler<ProgressCard>((x, _) =>
            x.RaisePropertyChanged(nameof(ProgressText)));
        ShowPercentageProperty.Changed.AddClassHandler<ProgressCard>((x, _) =>
            x.RaisePropertyChanged(nameof(ProgressText)));
    }

    private void RaisePropertyChanged(string propertyName)
    {
        // Notify Avalonia about the property change
    }
}
