using CamelotCombatReporter.Core.Comparison.Models;

namespace CamelotCombatReporter.Core.Comparison;

/// <summary>
/// Service for analyzing trends in performance metrics over time.
/// </summary>
public interface ITrendAnalysisService
{
    /// <summary>
    /// Performs a full trend analysis on a set of data points.
    /// </summary>
    /// <param name="metricName">Name of the metric being analyzed.</param>
    /// <param name="dataPoints">Data points to analyze.</param>
    /// <returns>Complete trend analysis with statistics and interpretation.</returns>
    TrendAnalysis AnalyzeTrend(string metricName, IEnumerable<TrendDataPoint> dataPoints);

    /// <summary>
    /// Calculates basic statistics for a set of values.
    /// </summary>
    /// <param name="values">Values to analyze.</param>
    /// <returns>Statistical summary.</returns>
    TrendStatistics CalculateStatistics(IEnumerable<double> values);

    /// <summary>
    /// Calculates a rolling average for smoothing data.
    /// </summary>
    /// <param name="values">Values to smooth.</param>
    /// <param name="windowSize">Size of the rolling window.</param>
    /// <returns>Rolling average values.</returns>
    IReadOnlyList<double> CalculateRollingAverage(IEnumerable<double> values, int windowSize = 3);

    /// <summary>
    /// Calculates linear regression parameters.
    /// </summary>
    /// <param name="points">Data points for regression.</param>
    /// <returns>Slope, intercept, and R-squared value.</returns>
    (double Slope, double Intercept, double RSquared) CalculateLinearRegression(IEnumerable<TrendDataPoint> points);

    /// <summary>
    /// Predicts the next value based on trend statistics.
    /// </summary>
    /// <param name="statistics">Statistics from trend analysis.</param>
    /// <returns>Predicted next value, or null if confidence is too low.</returns>
    double? PredictNextValue(TrendStatistics statistics);
}
