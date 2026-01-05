using System.Text.Json;
using CamelotCombatReporter.Core.Comparison.Models;

namespace CamelotCombatReporter.Core.Comparison;

/// <summary>
/// Service for tracking personal best records with persistence.
/// </summary>
public class PersonalBestTracker : IPersonalBestTracker
{
    private readonly Dictionary<string, PersonalBest> _currentBests = new();
    private readonly List<PersonalBest> _history = new();
    private readonly string _storagePath;
    private readonly object _lock = new();

    /// <inheritdoc />
    public IReadOnlyDictionary<string, PersonalBest> CurrentBests
    {
        get
        {
            lock (_lock)
            {
                return new Dictionary<string, PersonalBest>(_currentBests);
            }
        }
    }

    /// <inheritdoc />
    public event EventHandler<PersonalBestEventArgs>? NewPersonalBest;

    /// <summary>
    /// Creates a new personal best tracker.
    /// </summary>
    /// <param name="storagePath">Path to the JSON storage file.</param>
    public PersonalBestTracker(string storagePath)
    {
        _storagePath = storagePath ?? throw new ArgumentNullException(nameof(storagePath));
    }

    /// <inheritdoc />
    public PersonalBest? CheckAndUpdateBest(string metricName, double value, Guid sessionId)
    {
        if (string.IsNullOrWhiteSpace(metricName))
            throw new ArgumentException("Metric name cannot be empty", nameof(metricName));

        lock (_lock)
        {
            var previousBest = _currentBests.GetValueOrDefault(metricName);

            // Not a new best if current is higher or equal
            if (previousBest != null && value <= previousBest.Value)
                return null;

            var improvementPercent = previousBest != null && previousBest.Value > 0
                ? ((value - previousBest.Value) / previousBest.Value) * 100
                : (double?)null;

            var newBest = new PersonalBest(
                Id: Guid.NewGuid(),
                MetricName: metricName,
                Value: value,
                AchievedAt: DateTime.UtcNow,
                SessionId: sessionId,
                PreviousBest: previousBest?.Value,
                ImprovementPercent: improvementPercent
            );

            _currentBests[metricName] = newBest;
            _history.Add(newBest);

            // Keep history bounded
            if (_history.Count > 1000)
                _history.RemoveRange(0, 100);

            NewPersonalBest?.Invoke(this, new PersonalBestEventArgs(newBest, previousBest));

            return newBest;
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<PersonalBest> GetBestHistory(string metricName, int count = 10)
    {
        lock (_lock)
        {
            return _history
                .Where(b => b.MetricName.Equals(metricName, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(b => b.AchievedAt)
                .Take(count)
                .ToList();
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<PersonalBest> GetRecentBests(int count = 10)
    {
        lock (_lock)
        {
            return _history
                .OrderByDescending(b => b.AchievedAt)
                .Take(count)
                .ToList();
        }
    }

    /// <inheritdoc />
    public async Task LoadAsync()
    {
        if (!File.Exists(_storagePath))
            return;

        try
        {
            var json = await File.ReadAllTextAsync(_storagePath);
            var data = JsonSerializer.Deserialize<PersonalBestData>(json, GetJsonOptions());

            if (data != null)
            {
                lock (_lock)
                {
                    _currentBests.Clear();
                    foreach (var best in data.CurrentBests)
                        _currentBests[best.MetricName] = best;

                    _history.Clear();
                    _history.AddRange(data.History);
                }
            }
        }
        catch
        {
            // If loading fails, start fresh
        }
    }

    /// <inheritdoc />
    public async Task SaveAsync()
    {
        var directory = Path.GetDirectoryName(_storagePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        PersonalBestData toSave;
        lock (_lock)
        {
            toSave = new PersonalBestData(_currentBests.Values.ToList(), _history.ToList());
        }

        var json = JsonSerializer.Serialize(toSave, GetJsonOptions());
        await File.WriteAllTextAsync(_storagePath, json);
    }

    private static JsonSerializerOptions GetJsonOptions() => new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private record PersonalBestData(
        List<PersonalBest> CurrentBests,
        List<PersonalBest> History
    );
}
