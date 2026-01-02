using System.Diagnostics;
using CamelotCombatReporter.Plugins.Loading;
using CamelotCombatReporter.Plugins.Manifest;

namespace CamelotCombatReporter.Plugins.Sandbox;

/// <summary>
/// Monitors and enforces resource limits for plugins.
/// </summary>
public sealed class ResourceMonitor : IDisposable
{
    private readonly string _pluginId;
    private readonly ResourceLimitsConfig _limits;
    private readonly Stopwatch _cpuTimeWatch = new();
    private readonly Timer _monitorTimer;
    private long _totalExecutionTimeMs;
    private int _activeOperations;
    private bool _isDisposed;

    /// <summary>
    /// Event raised when a resource limit is exceeded.
    /// </summary>
    public event EventHandler<ResourceLimitExceededEventArgs>? ResourceLimitExceeded;

    public ResourceMonitor(string pluginId, ResourceLimitsConfig limits)
    {
        _pluginId = pluginId;
        _limits = limits;
        _monitorTimer = new Timer(CheckResources, null,
            TimeSpan.FromSeconds(1),
            TimeSpan.FromSeconds(1));
    }

    /// <summary>
    /// Begins tracking an operation.
    /// </summary>
    public IDisposable BeginOperation(string operationName)
    {
        Interlocked.Increment(ref _activeOperations);
        return new OperationScope(this, operationName);
    }

    /// <summary>
    /// Gets the total execution time used by this plugin.
    /// </summary>
    public TimeSpan TotalExecutionTime => TimeSpan.FromMilliseconds(_totalExecutionTimeMs);

    /// <summary>
    /// Gets the number of currently active operations.
    /// </summary>
    public int ActiveOperations => _activeOperations;

    /// <summary>
    /// Resets the execution time counter.
    /// </summary>
    public void ResetExecutionTime()
    {
        Interlocked.Exchange(ref _totalExecutionTimeMs, 0);
    }

    private void CheckResources(object? state)
    {
        if (_isDisposed) return;

        // Check execution time
        var currentTimeMs = Interlocked.Read(ref _totalExecutionTimeMs);
        var limitMs = _limits.MaxCpuTimeSeconds * 1000;

        if (currentTimeMs > limitMs)
        {
            OnResourceLimitExceeded(new ResourceLimitExceededEventArgs(
                _pluginId,
                ResourceLimitType.CpuTime,
                currentTimeMs,
                limitMs,
                $"CPU time exceeded: {currentTimeMs}ms > {limitMs}ms"));
        }
    }

    private void ReportExecutionTime(long elapsedMs)
    {
        Interlocked.Add(ref _totalExecutionTimeMs, elapsedMs);
    }

    private void OnResourceLimitExceeded(ResourceLimitExceededEventArgs e)
    {
        ResourceLimitExceeded?.Invoke(this, e);
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;
        _monitorTimer.Dispose();
    }

    private sealed class OperationScope : IDisposable
    {
        private readonly ResourceMonitor _monitor;
        private readonly string _operationName;
        private readonly Stopwatch _stopwatch;

        public OperationScope(ResourceMonitor monitor, string operationName)
        {
            _monitor = monitor;
            _operationName = operationName;
            _stopwatch = Stopwatch.StartNew();
        }

        public void Dispose()
        {
            _stopwatch.Stop();
            _monitor.ReportExecutionTime(_stopwatch.ElapsedMilliseconds);
            Interlocked.Decrement(ref _monitor._activeOperations);
        }
    }
}

/// <summary>
/// Event args for resource limit exceeded events.
/// </summary>
public sealed class ResourceLimitExceededEventArgs : EventArgs
{
    public string PluginId { get; }
    public ResourceLimitType ResourceType { get; }
    public long CurrentValue { get; }
    public long LimitValue { get; }
    public string Message { get; }

    public ResourceLimitExceededEventArgs(
        string pluginId,
        ResourceLimitType resourceType,
        long currentValue,
        long limitValue,
        string message)
    {
        PluginId = pluginId;
        ResourceType = resourceType;
        CurrentValue = currentValue;
        LimitValue = limitValue;
        Message = message;
    }
}

/// <summary>
/// Guard for executing operations with timeout.
/// </summary>
public sealed class ExecutionTimeGuard
{
    private readonly TimeSpan _defaultTimeout;

    public ExecutionTimeGuard(TimeSpan defaultTimeout)
    {
        _defaultTimeout = defaultTimeout;
    }

    /// <summary>
    /// Executes an async operation with timeout.
    /// </summary>
    public async Task<T> ExecuteWithTimeoutAsync<T>(
        string pluginId,
        string operationName,
        Func<CancellationToken, Task<T>> operation,
        TimeSpan? timeout = null,
        CancellationToken externalCt = default)
    {
        var effectiveTimeout = timeout ?? _defaultTimeout;
        using var timeoutCts = new CancellationTokenSource(effectiveTimeout);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
            timeoutCts.Token, externalCt);

        try
        {
            return await operation(linkedCts.Token);
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested && !externalCt.IsCancellationRequested)
        {
            throw new PluginTimeoutException(pluginId, operationName, effectiveTimeout);
        }
    }

    /// <summary>
    /// Executes an async operation with timeout (no return value).
    /// </summary>
    public async Task ExecuteWithTimeoutAsync(
        string pluginId,
        string operationName,
        Func<CancellationToken, Task> operation,
        TimeSpan? timeout = null,
        CancellationToken externalCt = default)
    {
        await ExecuteWithTimeoutAsync<object?>(
            pluginId,
            operationName,
            async ct =>
            {
                await operation(ct);
                return null;
            },
            timeout,
            externalCt);
    }
}

/// <summary>
/// Exception thrown when a plugin operation times out.
/// </summary>
public sealed class PluginTimeoutException : Exception
{
    public string PluginId { get; }
    public string OperationName { get; }
    public TimeSpan Timeout { get; }

    public PluginTimeoutException(string pluginId, string operationName, TimeSpan timeout)
        : base($"Plugin '{pluginId}' operation '{operationName}' exceeded timeout of {timeout}")
    {
        PluginId = pluginId;
        OperationName = operationName;
        Timeout = timeout;
    }
}
