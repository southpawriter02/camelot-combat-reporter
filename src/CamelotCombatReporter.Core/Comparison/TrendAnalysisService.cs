using CamelotCombatReporter.Core.Comparison.Models;

namespace CamelotCombatReporter.Core.Comparison;

/// <summary>
/// Service for analyzing trends in performance metrics over time.
/// </summary>
public class TrendAnalysisService : ITrendAnalysisService
{
    /// <summary>
    /// Minimum R-squared value for predictions to be considered reliable.
    /// </summary>
    public double MinConfidenceForPrediction { get; set; } = 0.5;

    /// <summary>
    /// Minimum slope magnitude to consider a trend significant.
    /// </summary>
    public double MinSlopeForTrend { get; set; } = 0.01;

    /// <inheritdoc />
    public TrendAnalysis AnalyzeTrend(string metricName, IEnumerable<TrendDataPoint> dataPoints)
    {
        var points = dataPoints.OrderBy(p => p.Timestamp).ToList();

        if (points.Count < 2)
        {
            var basicStats = CalculateBasicStatistics(metricName, points);
            return new TrendAnalysis(
                metricName,
                points,
                basicStats,
                "Insufficient data for trend analysis",
                null);
        }

        var statistics = CalculateDetailedStatistics(metricName, points);
        var interpretation = InterpretTrend(statistics);
        var prediction = PredictNextValue(statistics);

        return new TrendAnalysis(metricName, points, statistics, interpretation, prediction);
    }

    /// <inheritdoc />
    public TrendStatistics CalculateStatistics(IEnumerable<double> values)
    {
        var valueList = values.ToList();
        var points = valueList
            .Select((v, i) => new TrendDataPoint(DateTime.Now.AddDays(-valueList.Count + i + 1), v))
            .ToList();

        return CalculateDetailedStatistics("Metric", points);
    }

    /// <inheritdoc />
    public IReadOnlyList<double> CalculateRollingAverage(IEnumerable<double> values, int windowSize = 3)
    {
        if (windowSize < 1)
            throw new ArgumentOutOfRangeException(nameof(windowSize), "Window size must be at least 1");

        var valueList = values.ToList();
        var result = new List<double>();

        for (int i = 0; i < valueList.Count; i++)
        {
            var windowStart = Math.Max(0, i - windowSize + 1);
            var windowValues = valueList.Skip(windowStart).Take(i - windowStart + 1);
            result.Add(windowValues.Average());
        }

        return result;
    }

    /// <inheritdoc />
    public (double Slope, double Intercept, double RSquared) CalculateLinearRegression(
        IEnumerable<TrendDataPoint> points)
    {
        var pointList = points.ToList();

        if (pointList.Count < 2)
            return (0, pointList.FirstOrDefault()?.Value ?? 0, 0);

        var n = pointList.Count;
        var xValues = Enumerable.Range(0, n).Select(i => (double)i).ToList();
        var yValues = pointList.Select(p => p.Value).ToList();

        var sumX = xValues.Sum();
        var sumY = yValues.Sum();
        var sumXY = xValues.Zip(yValues, (x, y) => x * y).Sum();
        var sumX2 = xValues.Sum(x => x * x);
        var sumY2 = yValues.Sum(y => y * y);

        var denominator = n * sumX2 - sumX * sumX;
        if (Math.Abs(denominator) < double.Epsilon)
            return (0, sumY / n, 0);

        var slope = (n * sumXY - sumX * sumY) / denominator;
        var intercept = (sumY - slope * sumX) / n;

        // Calculate R-squared (coefficient of determination)
        var yMean = sumY / n;
        var ssTotal = yValues.Sum(y => (y - yMean) * (y - yMean));
        var ssResidual = xValues.Zip(yValues, (x, y) =>
        {
            var predicted = slope * x + intercept;
            return (y - predicted) * (y - predicted);
        }).Sum();

        var rSquared = ssTotal > double.Epsilon
            ? 1 - (ssResidual / ssTotal)
            : 0;

        // Clamp R-squared to valid range
        rSquared = Math.Max(0, Math.Min(1, rSquared));

        return (slope, intercept, rSquared);
    }

    /// <inheritdoc />
    public double? PredictNextValue(TrendStatistics statistics)
    {
        // Don't predict if confidence is too low or not enough data
        if (statistics.RSquared < MinConfidenceForPrediction || statistics.DataPoints.Count < 3)
            return null;

        var nextX = statistics.DataPoints.Count;
        return statistics.Slope * nextX + statistics.Intercept;
    }

    private TrendStatistics CalculateDetailedStatistics(string metricName, IReadOnlyList<TrendDataPoint> points)
    {
        var values = points.Select(p => p.Value).ToList();
        var (slope, intercept, rSquared) = CalculateLinearRegression(points);
        var rollingAvg = CalculateRollingAverage(values);

        var direction = DetermineOverallTrend(slope, rSquared);

        return new TrendStatistics(
            MetricName: metricName,
            Slope: slope,
            Intercept: intercept,
            RSquared: rSquared,
            StandardDeviation: CalculateStdDev(values),
            Mean: values.Average(),
            Median: CalculateMedian(values),
            Min: values.Min(),
            Max: values.Max(),
            OverallTrend: direction,
            RollingAverage: rollingAvg,
            DataPoints: points
        );
    }

    private TrendStatistics CalculateBasicStatistics(string metricName, IReadOnlyList<TrendDataPoint> points)
    {
        var values = points.Select(p => p.Value).ToList();

        if (values.Count == 0)
        {
            return new TrendStatistics(
                metricName, 0, 0, 0, 0, 0, 0, 0, 0,
                ChangeDirection.Unchanged,
                Array.Empty<double>(),
                points);
        }

        return new TrendStatistics(
            MetricName: metricName,
            Slope: 0,
            Intercept: values.First(),
            RSquared: 0,
            StandardDeviation: values.Count > 1 ? CalculateStdDev(values) : 0,
            Mean: values.Average(),
            Median: CalculateMedian(values),
            Min: values.Min(),
            Max: values.Max(),
            OverallTrend: ChangeDirection.Unchanged,
            RollingAverage: values,
            DataPoints: points
        );
    }

    private ChangeDirection DetermineOverallTrend(double slope, double rSquared)
    {
        // If the fit is poor, consider it unchanged
        if (rSquared < 0.3)
            return ChangeDirection.Unchanged;

        // If slope is too small, consider it unchanged
        if (Math.Abs(slope) < MinSlopeForTrend)
            return ChangeDirection.Unchanged;

        return slope > 0 ? ChangeDirection.Improved : ChangeDirection.Declined;
    }

    private string InterpretTrend(TrendStatistics stats)
    {
        var trendWord = stats.OverallTrend switch
        {
            ChangeDirection.Improved => "improving",
            ChangeDirection.Declined => "declining",
            _ => "stable"
        };

        var confidence = stats.RSquared switch
        {
            >= 0.8 => "high confidence",
            >= 0.5 => "moderate confidence",
            >= 0.3 => "low confidence",
            _ => "very low confidence"
        };

        var variability = (stats.StandardDeviation / Math.Max(0.001, Math.Abs(stats.Mean))) switch
        {
            < 0.1 => "very consistent",
            < 0.25 => "fairly consistent",
            < 0.5 => "somewhat variable",
            _ => "highly variable"
        };

        return $"Trend is {trendWord} ({confidence}, R² = {stats.RSquared:F2}). Performance is {variability}.";
    }

    private static double CalculateStdDev(IReadOnlyList<double> values)
    {
        if (values.Count < 2)
            return 0;

        var mean = values.Average();
        var sumSquares = values.Sum(v => (v - mean) * (v - mean));
        return Math.Sqrt(sumSquares / (values.Count - 1));
    }

    private static double CalculateMedian(IReadOnlyList<double> values)
    {
        if (values.Count == 0)
            return 0;

        var sorted = values.OrderBy(v => v).ToList();
        var mid = sorted.Count / 2;

        return sorted.Count % 2 == 0
            ? (sorted[mid - 1] + sorted[mid]) / 2
            : sorted[mid];
    }
}
