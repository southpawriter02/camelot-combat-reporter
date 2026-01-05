using CamelotCombatReporter.Core.Comparison.Models;
using CamelotCombatReporter.Core.CrossRealm;
using CamelotCombatReporter.Core.Models;

namespace CamelotCombatReporter.Core.Comparison;

/// <summary>
/// Service for comparing combat sessions.
/// </summary>
public class SessionComparisonService : ISessionComparisonService
{
    private readonly ICrossRealmStatisticsService? _crossRealmService;

    /// <summary>
    /// Significance threshold for changes (percentage).
    /// Changes below this threshold are considered "Unchanged".
    /// </summary>
    public double SignificanceThreshold { get; set; } = 5.0;

    /// <summary>
    /// Creates a new session comparison service.
    /// </summary>
    /// <param name="crossRealmService">Optional cross-realm service for loading sessions.</param>
    public SessionComparisonService(ICrossRealmStatisticsService? crossRealmService = null)
    {
        _crossRealmService = crossRealmService;
    }

    /// <inheritdoc />
    public SessionComparison Compare(SessionSummary baseSession, SessionSummary compareSession)
    {
        var deltas = CalculateDeltas(baseSession, compareSession);

        var deltasByCategory = deltas
            .GroupBy(d => d.Category)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<MetricDelta>)g.ToList());

        var timeBetween = compareSession.SessionDate - baseSession.SessionDate;

        var comparison = new SessionComparison(
            Id: Guid.NewGuid(),
            BaseSession: baseSession,
            CompareSession: compareSession,
            Deltas: deltas,
            TimeBetweenSessions: timeBetween,
            ComparisonSummary: string.Empty,
            DeltasByCategory: deltasByCategory);

        // Generate summary with the comparison data
        var summary = GenerateComparisonSummary(comparison);

        return comparison with { ComparisonSummary = summary };
    }

    /// <inheritdoc />
    public IReadOnlyList<MetricDelta> CalculateDeltas(SessionSummary baseSession, SessionSummary compareSession)
    {
        var deltas = new List<MetricDelta>();

        // Core damage metrics
        deltas.Add(CalculateDelta("DPS", "Damage", baseSession.DamagePerSecond, compareSession.DamagePerSecond, higherIsBetter: true));
        deltas.Add(CalculateDelta("Total Damage", "Damage", baseSession.TotalDamageDealt, compareSession.TotalDamageDealt, higherIsBetter: true));
        deltas.Add(CalculateDelta("Damage Taken", "Damage", baseSession.TotalDamageReceived, compareSession.TotalDamageReceived, higherIsBetter: false));

        // Healing metrics
        deltas.Add(CalculateDelta("HPS", "Healing", baseSession.HealingPerSecond, compareSession.HealingPerSecond, higherIsBetter: true));
        deltas.Add(CalculateDelta("Total Healing", "Healing", baseSession.TotalHealingDone, compareSession.TotalHealingDone, higherIsBetter: true));
        deltas.Add(CalculateDelta("Healing Received", "Healing", baseSession.TotalHealingReceived, compareSession.TotalHealingReceived, higherIsBetter: null));

        // Combat metrics
        deltas.Add(CalculateDelta("Kills", "Combat", baseSession.Kills, compareSession.Kills, higherIsBetter: true));
        deltas.Add(CalculateDelta("Deaths", "Combat", baseSession.Deaths, compareSession.Deaths, higherIsBetter: false));
        deltas.Add(CalculateDelta("Assists", "Combat", baseSession.Assists, compareSession.Assists, higherIsBetter: true));
        deltas.Add(CalculateDelta("K/D Ratio", "Combat", baseSession.KillDeathRatio, compareSession.KillDeathRatio, higherIsBetter: true));

        // General metrics
        deltas.Add(CalculateDelta("Session Duration", "General", baseSession.Duration.TotalMinutes, compareSession.Duration.TotalMinutes, higherIsBetter: null));

        // Custom metrics
        var allCustomMetrics = baseSession.CustomMetrics.Keys
            .Union(compareSession.CustomMetrics.Keys)
            .Distinct();

        foreach (var metric in allCustomMetrics)
        {
            var baseValue = baseSession.CustomMetrics.GetValueOrDefault(metric, 0);
            var compareValue = compareSession.CustomMetrics.GetValueOrDefault(metric, 0);
            deltas.Add(CalculateDelta(metric, "Custom", baseValue, compareValue, higherIsBetter: true));
        }

        return deltas;
    }

    /// <inheritdoc />
    public string GenerateComparisonSummary(SessionComparison comparison)
    {
        var improvements = comparison.Deltas.Count(d => d.Direction == ChangeDirection.Improved && d.IsSignificant);
        var declines = comparison.Deltas.Count(d => d.Direction == ChangeDirection.Declined && d.IsSignificant);
        var unchanged = comparison.Deltas.Count(d => d.Direction == ChangeDirection.Unchanged || !d.IsSignificant);

        var timeDiff = comparison.TimeBetweenSessions;
        var timeStr = timeDiff.TotalDays >= 1
            ? $"{timeDiff.Days} day(s)"
            : timeDiff.TotalHours >= 1
                ? $"{(int)timeDiff.TotalHours} hour(s)"
                : $"{(int)timeDiff.TotalMinutes} minute(s)";

        var overallAssessment = (improvements, declines) switch
        {
            var (i, d) when i > d * 2 => "Strong improvement",
            var (i, d) when i > d => "Overall improvement",
            var (i, d) when d > i * 2 => "Significant regression",
            var (i, d) when d > i => "Some regression",
            _ => "Mixed results"
        };

        return $"{overallAssessment} over {timeStr}: {improvements} improved, {declines} declined, {unchanged} unchanged";
    }

    /// <inheritdoc />
    public IReadOnlyList<SessionSummary> LoadSessionHistory(int count = 10)
    {
        if (_crossRealmService == null)
            return Array.Empty<SessionSummary>();

        // Use synchronous blocking for now - in production, prefer async
        var task = _crossRealmService.GetSessionsAsync(limit: count);
        task.Wait();

        return task.Result
            .Select(s => new SessionSummary(
                SessionId: s.Id,
                SessionDate: s.SessionStartUtc,
                Duration: TimeSpan.FromMinutes(s.DurationMinutes),
                TotalDamageDealt: 0, // Summary doesn't have full details
                TotalDamageReceived: 0,
                TotalHealingDone: 0,
                TotalHealingReceived: 0,
                Kills: s.Kills,
                Deaths: s.Deaths,
                Assists: 0,
                DamagePerSecond: s.Dps,
                HealingPerSecond: s.Hps,
                KillDeathRatio: s.Deaths > 0 ? (double)s.Kills / s.Deaths : s.Kills,
                CustomMetrics: new Dictionary<string, double>()))
            .ToList();
    }

    /// <inheritdoc />
    public async Task<SessionSummary> CreateSummaryFromSessionAsync(Guid sessionId)
    {
        if (_crossRealmService == null)
            throw new InvalidOperationException("Cross-realm service not configured");

        var stats = await _crossRealmService.GetSessionAsync(sessionId);
        if (stats == null)
            throw new KeyNotFoundException($"Session {sessionId} not found");

        return CreateSummaryFromExtendedStats(stats);
    }

    /// <summary>
    /// Creates a session summary from extended combat statistics.
    /// </summary>
    public static SessionSummary CreateSummaryFromExtendedStats(ExtendedCombatStatistics stats)
    {
        var duration = stats.Duration;
        var dps = duration.TotalSeconds > 0
            ? stats.TotalDamageDealt / duration.TotalSeconds
            : stats.BaseStats.Dps;

        var hps = duration.TotalSeconds > 0
            ? stats.TotalHealingDone / duration.TotalSeconds
            : stats.Hps;

        return new SessionSummary(
            SessionId: stats.Id,
            SessionDate: stats.SessionStartUtc,
            Duration: duration,
            TotalDamageDealt: stats.TotalDamageDealt,
            TotalDamageReceived: stats.TotalDamageTaken,
            TotalHealingDone: stats.TotalHealingDone,
            TotalHealingReceived: stats.TotalHealingReceived,
            Kills: stats.KillCount,
            Deaths: stats.DeathCount,
            Assists: stats.AssistCount,
            DamagePerSecond: dps,
            HealingPerSecond: hps,
            KillDeathRatio: stats.Kdr,
            CustomMetrics: new Dictionary<string, double>()
        );
    }

    private MetricDelta CalculateDelta(
        string name,
        string category,
        double baseValue,
        double compareValue,
        bool? higherIsBetter)
    {
        var absoluteChange = compareValue - baseValue;
        var percentChange = baseValue != 0
            ? (absoluteChange / Math.Abs(baseValue)) * 100
            : (compareValue != 0 ? 100 : 0);

        var direction = DetermineDirection(absoluteChange, percentChange, higherIsBetter);
        var isSignificant = Math.Abs(percentChange) >= SignificanceThreshold;

        var changePrefix = absoluteChange >= 0 ? "+" : "";

        return new MetricDelta(
            MetricName: name,
            Category: category,
            BaseValue: baseValue,
            CompareValue: compareValue,
            AbsoluteChange: absoluteChange,
            PercentChange: percentChange,
            Direction: direction,
            FormattedBase: FormatValue(baseValue, name),
            FormattedCompare: FormatValue(compareValue, name),
            FormattedChange: $"{changePrefix}{percentChange:F1}%",
            IsSignificant: isSignificant
        );
    }

    private ChangeDirection DetermineDirection(double absoluteChange, double percentChange, bool? higherIsBetter)
    {
        // Not significant change
        if (Math.Abs(percentChange) < SignificanceThreshold)
            return ChangeDirection.Unchanged;

        // No preference - just report direction
        if (higherIsBetter == null)
            return absoluteChange > 0 ? ChangeDirection.Improved : ChangeDirection.Declined;

        // Higher is better
        if (higherIsBetter.Value)
            return absoluteChange > 0 ? ChangeDirection.Improved : ChangeDirection.Declined;

        // Lower is better
        return absoluteChange < 0 ? ChangeDirection.Improved : ChangeDirection.Declined;
    }

    private static string FormatValue(double value, string metricName)
    {
        return metricName switch
        {
            "DPS" or "HPS" => $"{value:F1}",
            "K/D Ratio" => $"{value:F2}",
            "Session Duration" => $"{value:F0} min",
            _ when metricName.Contains("Percent") || metricName.Contains("Uptime") => $"{value:F1}%",
            _ => $"{value:N0}"
        };
    }
}
