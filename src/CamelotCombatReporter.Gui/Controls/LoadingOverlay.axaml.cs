using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace CamelotCombatReporter.Gui.Controls;

/// <summary>
/// A loading overlay that covers its parent with a semi-transparent background
/// and displays a loading spinner with optional message and progress.
/// </summary>
public partial class LoadingOverlay : UserControl
{
    /// <summary>
    /// Defines the IsLoading property.
    /// </summary>
    public static readonly StyledProperty<bool> IsLoadingProperty =
        AvaloniaProperty.Register<LoadingOverlay, bool>(nameof(IsLoading), false);

    /// <summary>
    /// Defines the Message property.
    /// </summary>
    public static readonly StyledProperty<string?> MessageProperty =
        AvaloniaProperty.Register<LoadingOverlay, string?>(nameof(Message));

    /// <summary>
    /// Defines the Progress property.
    /// </summary>
    public static readonly StyledProperty<double> ProgressProperty =
        AvaloniaProperty.Register<LoadingOverlay, double>(nameof(Progress), 0);

    /// <summary>
    /// Defines the ShowProgress property.
    /// </summary>
    public static readonly StyledProperty<bool> ShowProgressProperty =
        AvaloniaProperty.Register<LoadingOverlay, bool>(nameof(ShowProgress), false);

    /// <summary>
    /// Defines the SpinnerSize property.
    /// </summary>
    public static readonly StyledProperty<double> SpinnerSizeProperty =
        AvaloniaProperty.Register<LoadingOverlay, double>(nameof(SpinnerSize), 48);

    /// <summary>
    /// Defines the OverlayBrush property.
    /// </summary>
    public static readonly StyledProperty<IBrush> OverlayBrushProperty =
        AvaloniaProperty.Register<LoadingOverlay, IBrush>(nameof(OverlayBrush),
            new SolidColorBrush(Color.Parse("#80FFFFFF")));

    /// <summary>
    /// Defines the SpinnerBrush property.
    /// </summary>
    public static readonly StyledProperty<IBrush> SpinnerBrushProperty =
        AvaloniaProperty.Register<LoadingOverlay, IBrush>(nameof(SpinnerBrush),
            new SolidColorBrush(Color.Parse("#1E88E5")));

    /// <summary>
    /// Defines the MessageBrush property.
    /// </summary>
    public static readonly StyledProperty<IBrush> MessageBrushProperty =
        AvaloniaProperty.Register<LoadingOverlay, IBrush>(nameof(MessageBrush),
            new SolidColorBrush(Color.Parse("#424242")));

    /// <summary>
    /// Gets or sets whether loading is in progress.
    /// </summary>
    public bool IsLoading
    {
        get => GetValue(IsLoadingProperty);
        set => SetValue(IsLoadingProperty, value);
    }

    /// <summary>
    /// Gets or sets the loading message.
    /// </summary>
    public string? Message
    {
        get => GetValue(MessageProperty);
        set => SetValue(MessageProperty, value);
    }

    /// <summary>
    /// Gets or sets the progress value (0-100).
    /// </summary>
    public double Progress
    {
        get => GetValue(ProgressProperty);
        set => SetValue(ProgressProperty, value);
    }

    /// <summary>
    /// Gets or sets whether to show the progress bar.
    /// </summary>
    public bool ShowProgress
    {
        get => GetValue(ShowProgressProperty);
        set => SetValue(ShowProgressProperty, value);
    }

    /// <summary>
    /// Gets or sets the spinner size.
    /// </summary>
    public double SpinnerSize
    {
        get => GetValue(SpinnerSizeProperty);
        set => SetValue(SpinnerSizeProperty, value);
    }

    /// <summary>
    /// Gets or sets the overlay background brush.
    /// </summary>
    public IBrush OverlayBrush
    {
        get => GetValue(OverlayBrushProperty);
        set => SetValue(OverlayBrushProperty, value);
    }

    /// <summary>
    /// Gets or sets the spinner brush.
    /// </summary>
    public IBrush SpinnerBrush
    {
        get => GetValue(SpinnerBrushProperty);
        set => SetValue(SpinnerBrushProperty, value);
    }

    /// <summary>
    /// Gets or sets the message text brush.
    /// </summary>
    public IBrush MessageBrush
    {
        get => GetValue(MessageBrushProperty);
        set => SetValue(MessageBrushProperty, value);
    }

    /// <summary>
    /// Gets the progress text in percentage format.
    /// </summary>
    public string ProgressText => $"{Progress:F0}%";

    public LoadingOverlay()
    {
        InitializeComponent();
    }

    static LoadingOverlay()
    {
        ProgressProperty.Changed.AddClassHandler<LoadingOverlay>((x, _) =>
            x.RaisePropertyChanged(nameof(ProgressText)));
    }

    private void RaisePropertyChanged(string propertyName)
    {
        // Manually notify property change for computed properties
    }
}
