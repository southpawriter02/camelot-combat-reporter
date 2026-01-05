using System.Text.Json;
using CamelotCombatReporter.Core.Comparison.Models;

namespace CamelotCombatReporter.Core.Comparison;

/// <summary>
/// Service for tracking performance goals with persistence.
/// </summary>
public class GoalTracker : IGoalTracker
{
    private readonly List<PerformanceGoal> _goals = new();
    private readonly string _storagePath;
    private readonly object _lock = new();

    /// <inheritdoc />
    public IReadOnlyList<PerformanceGoal> ActiveGoals
    {
        get
        {
            lock (_lock)
            {
                return _goals
                    .Where(g => g.Status == GoalStatus.InProgress || g.Status == GoalStatus.NotStarted)
                    .ToList();
            }
        }
    }

    /// <summary>
    /// Creates a new goal tracker.
    /// </summary>
    /// <param name="storagePath">Path to the JSON storage file.</param>
    public GoalTracker(string storagePath)
    {
        _storagePath = storagePath ?? throw new ArgumentNullException(nameof(storagePath));
    }

    /// <inheritdoc />
    public PerformanceGoal CreateGoal(
        string name,
        GoalType type,
        double targetValue,
        DateTime? deadline = null,
        string? customMetricName = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty", nameof(name));

        if (targetValue <= 0)
            throw new ArgumentOutOfRangeException(nameof(targetValue), "Target must be positive");

        if (type == GoalType.CustomMetric && string.IsNullOrWhiteSpace(customMetricName))
            throw new ArgumentException("Custom metric name required for CustomMetric type", nameof(customMetricName));

        var goal = new PerformanceGoal(
            Id: Guid.NewGuid(),
            Name: name,
            Type: type,
            CustomMetricName: customMetricName,
            TargetValue: targetValue,
            CurrentValue: null,
            StartingValue: 0,
            CreatedAt: DateTime.UtcNow,
            Deadline: deadline,
            Status: GoalStatus.NotStarted,
            ProgressHistory: new List<GoalProgress>()
        );

        lock (_lock)
        {
            _goals.Add(goal);
        }

        return goal;
    }

    /// <inheritdoc />
    public void UpdateProgress(Guid goalId, double currentValue, Guid? sessionId = null)
    {
        lock (_lock)
        {
            var index = _goals.FindIndex(g => g.Id == goalId);
            if (index < 0)
                return;

            var goal = _goals[index];
            var progressHistory = goal.ProgressHistory.ToList();

            var percentComplete = goal.TargetValue > 0
                ? Math.Min(100, (currentValue / goal.TargetValue) * 100)
                : 100;

            progressHistory.Add(new GoalProgress(
                DateTime.UtcNow,
                currentValue,
                percentComplete,
                sessionId));

            // Determine new status
            var newStatus = goal.Status;
            var startingValue = goal.StartingValue;

            if (goal.Status == GoalStatus.NotStarted)
            {
                newStatus = GoalStatus.InProgress;
                startingValue = currentValue;
            }

            if (currentValue >= goal.TargetValue)
            {
                newStatus = GoalStatus.Achieved;
            }
            else if (goal.Deadline.HasValue && DateTime.UtcNow > goal.Deadline.Value)
            {
                newStatus = GoalStatus.Expired;
            }

            _goals[index] = goal with
            {
                CurrentValue = currentValue,
                StartingValue = startingValue,
                Status = newStatus,
                ProgressHistory = progressHistory
            };
        }
    }

    /// <inheritdoc />
    public void DeleteGoal(Guid goalId)
    {
        lock (_lock)
        {
            _goals.RemoveAll(g => g.Id == goalId);
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<PerformanceGoal> GetGoalHistory()
    {
        lock (_lock)
        {
            return _goals
                .Where(g => g.Status is GoalStatus.Achieved or GoalStatus.Failed or GoalStatus.Expired)
                .OrderByDescending(g => g.CreatedAt)
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
            var loaded = JsonSerializer.Deserialize<List<PerformanceGoalDto>>(json, GetJsonOptions());

            if (loaded != null)
            {
                lock (_lock)
                {
                    _goals.Clear();
                    _goals.AddRange(loaded.Select(FromDto));
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

        List<PerformanceGoalDto> toSave;
        lock (_lock)
        {
            toSave = _goals.Select(ToDto).ToList();
        }

        var json = JsonSerializer.Serialize(toSave, GetJsonOptions());
        await File.WriteAllTextAsync(_storagePath, json);
    }

    private static JsonSerializerOptions GetJsonOptions() => new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    // DTO for JSON serialization
    private record PerformanceGoalDto(
        Guid Id,
        string Name,
        GoalType Type,
        string? CustomMetricName,
        double TargetValue,
        double? CurrentValue,
        double StartingValue,
        DateTime CreatedAt,
        DateTime? Deadline,
        GoalStatus Status,
        List<GoalProgressDto> ProgressHistory
    );

    private record GoalProgressDto(
        DateTime Timestamp,
        double Value,
        double PercentComplete,
        Guid? SessionId
    );

    private static PerformanceGoalDto ToDto(PerformanceGoal goal) => new(
        goal.Id,
        goal.Name,
        goal.Type,
        goal.CustomMetricName,
        goal.TargetValue,
        goal.CurrentValue,
        goal.StartingValue,
        goal.CreatedAt,
        goal.Deadline,
        goal.Status,
        goal.ProgressHistory.Select(p => new GoalProgressDto(p.Timestamp, p.Value, p.PercentComplete, p.SessionId)).ToList()
    );

    private static PerformanceGoal FromDto(PerformanceGoalDto dto) => new(
        dto.Id,
        dto.Name,
        dto.Type,
        dto.CustomMetricName,
        dto.TargetValue,
        dto.CurrentValue,
        dto.StartingValue,
        dto.CreatedAt,
        dto.Deadline,
        dto.Status,
        dto.ProgressHistory.Select(p => new GoalProgress(p.Timestamp, p.Value, p.PercentComplete, p.SessionId)).ToList()
    );
}
