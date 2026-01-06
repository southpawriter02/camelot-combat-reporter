using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace CamelotCombatReporter.Gui.Controls;

/// <summary>
/// A spinning loading indicator control.
/// </summary>
public partial class LoadingSpinner : UserControl
{
    /// <summary>
    /// Defines the Size property.
    /// </summary>
    public static readonly StyledProperty<double> SizeProperty =
        AvaloniaProperty.Register<LoadingSpinner, double>(nameof(Size), 32);

    /// <summary>
    /// Defines the StrokeThickness property.
    /// </summary>
    public static readonly StyledProperty<double> StrokeThicknessProperty =
        AvaloniaProperty.Register<LoadingSpinner, double>(nameof(StrokeThickness), 3);

    /// <summary>
    /// Defines the SpinnerBrush property.
    /// </summary>
    public static readonly StyledProperty<IBrush> SpinnerBrushProperty =
        AvaloniaProperty.Register<LoadingSpinner, IBrush>(nameof(SpinnerBrush),
            new SolidColorBrush(Color.Parse("#1E88E5")));

    /// <summary>
    /// Defines the TrackBrush property.
    /// </summary>
    public static readonly StyledProperty<IBrush> TrackBrushProperty =
        AvaloniaProperty.Register<LoadingSpinner, IBrush>(nameof(TrackBrush),
            new SolidColorBrush(Color.Parse("#E0E0E0")));

    /// <summary>
    /// Gets or sets the size of the spinner.
    /// </summary>
    public double Size
    {
        get => GetValue(SizeProperty);
        set => SetValue(SizeProperty, value);
    }

    /// <summary>
    /// Gets or sets the stroke thickness.
    /// </summary>
    public double StrokeThickness
    {
        get => GetValue(StrokeThicknessProperty);
        set => SetValue(StrokeThicknessProperty, value);
    }

    /// <summary>
    /// Gets or sets the spinner brush color.
    /// </summary>
    public IBrush SpinnerBrush
    {
        get => GetValue(SpinnerBrushProperty);
        set => SetValue(SpinnerBrushProperty, value);
    }

    /// <summary>
    /// Gets or sets the track brush color.
    /// </summary>
    public IBrush TrackBrush
    {
        get => GetValue(TrackBrushProperty);
        set => SetValue(TrackBrushProperty, value);
    }

    public LoadingSpinner()
    {
        InitializeComponent();
    }
}
