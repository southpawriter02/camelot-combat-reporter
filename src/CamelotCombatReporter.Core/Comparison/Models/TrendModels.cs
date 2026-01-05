namespace CamelotCombatReporter.Core.Comparison.Models;

/// <summary>
/// A single data point in a trend analysis.
/// </summary>
/// <param name="Timestamp">When this data point was recorded.</param>
/// <param name="Value">The metric value at this point.</param>
/// <param name="Label">Optional label for display (e.g., session name).</param>
public record TrendDataPoint(
    DateTime Timestamp,
    double Value,
    string? Label = null
);

/// <summary>
/// Statistical analysis of a trend.
/// </summary>
/// <param name="MetricName">Name of the metric being analyzed.</param>
/// <param name="Slope">Linear regression slope (rate of change).</param>
/// <param name="Intercept">Linear regression intercept.</param>
/// <param name="RSquared">Coefficient of determination (0-1, how well the line fits).</param>
/// <param name="StandardDeviation">Standard deviation of values.</param>
/// <param name="Mean">Average value.</param>
/// <param name="Median">Median value.</param>
/// <param name="Min">Minimum value.</param>
/// <param name="Max">Maximum value.</param>
/// <param name="OverallTrend">Direction of the overall trend.</param>
/// <param name="RollingAverage">Rolling average values for smoothing.</param>
/// <param name="DataPoints">The original data points.</param>
public record TrendStatistics(
    string MetricName,
    double Slope,
    double Intercept,
    double RSquared,
    double StandardDeviation,
    double Mean,
    double Median,
    double Min,
    double Max,
    ChangeDirection OverallTrend,
    IReadOnlyList<double> RollingAverage,
    IReadOnlyList<TrendDataPoint> DataPoints
);

/// <summary>
/// Complete trend analysis for a metric.
/// </summary>
/// <param name="MetricName">Name of the metric being analyzed.</param>
/// <param name="DataPoints">The data points used in analysis.</param>
/// <param name="Statistics">Statistical analysis results.</param>
/// <param name="Interpretation">Human-readable interpretation of the trend.</param>
/// <param name="PredictedNextValue">Predicted value for the next session (if confidence is high enough).</param>
public record TrendAnalysis(
    string MetricName,
    IReadOnlyList<TrendDataPoint> DataPoints,
    TrendStatistics Statistics,
    string Interpretation,
    double? PredictedNextValue
);
