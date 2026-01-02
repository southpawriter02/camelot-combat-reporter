using CamelotCombatReporter.Core.Models;
using CamelotCombatReporter.Plugins.Abstractions;
using CamelotCombatReporter.Plugins.Loading;
using CamelotCombatReporter.Plugins.Security;

namespace CamelotCombatReporter.Plugins.Sandbox;

/// <summary>
/// Sandboxed combat data access that returns immutable copies of data.
/// </summary>
public sealed class CombatDataProxy : ICombatDataAccess
{
    private readonly string _pluginId;
    private readonly ISecurityAuditLogger _auditLogger;
    private readonly Func<IReadOnlyList<LogEvent>?> _allEventsProvider;
    private readonly Func<IReadOnlyList<LogEvent>?> _filteredEventsProvider;
    private readonly Func<CombatStatistics?> _statisticsProvider;
    private readonly List<Action> _dataChangedCallbacks = new();
    private readonly object _callbackLock = new();

    public CombatDataProxy(
        string pluginId,
        ISecurityAuditLogger auditLogger,
        Func<IReadOnlyList<LogEvent>?> allEventsProvider,
        Func<IReadOnlyList<LogEvent>?> filteredEventsProvider,
        Func<CombatStatistics?> statisticsProvider)
    {
        _pluginId = pluginId;
        _auditLogger = auditLogger;
        _allEventsProvider = allEventsProvider;
        _filteredEventsProvider = filteredEventsProvider;
        _statisticsProvider = statisticsProvider;
    }

    public IReadOnlyList<LogEvent>? GetAllEvents()
    {
        _auditLogger.LogAccess(_pluginId, SecurityAction.DataAccess, "AllEvents");

        var events = _allEventsProvider();
        if (events == null) return null;

        // Return a copy to prevent modification
        return events.ToList().AsReadOnly();
    }

    public IReadOnlyList<LogEvent>? GetFilteredEvents()
    {
        _auditLogger.LogAccess(_pluginId, SecurityAction.DataAccess, "FilteredEvents");

        var events = _filteredEventsProvider();
        if (events == null) return null;

        // Return a copy to prevent modification
        return events.ToList().AsReadOnly();
    }

    public CombatStatistics? GetStatistics()
    {
        _auditLogger.LogAccess(_pluginId, SecurityAction.DataAccess, "Statistics");

        // CombatStatistics is a record (immutable), so we can return directly
        return _statisticsProvider();
    }

    public IDisposable OnDataChanged(Action callback)
    {
        lock (_callbackLock)
        {
            _dataChangedCallbacks.Add(callback);
        }

        return new CallbackDisposer(this, callback);
    }

    /// <summary>
    /// Notifies all registered callbacks that data has changed.
    /// Called by the host application when data updates.
    /// </summary>
    internal void NotifyDataChanged()
    {
        List<Action> callbacks;
        lock (_callbackLock)
        {
            callbacks = _dataChangedCallbacks.ToList();
        }

        foreach (var callback in callbacks)
        {
            try
            {
                callback();
            }
            catch (Exception ex)
            {
                _auditLogger.LogPluginMessage(_pluginId, LogLevel.Error,
                    $"Error in data changed callback: {ex.Message}", ex);
            }
        }
    }

    private void RemoveCallback(Action callback)
    {
        lock (_callbackLock)
        {
            _dataChangedCallbacks.Remove(callback);
        }
    }

    private sealed class CallbackDisposer : IDisposable
    {
        private readonly CombatDataProxy _proxy;
        private readonly Action _callback;
        private bool _disposed;

        public CallbackDisposer(CombatDataProxy proxy, Action callback)
        {
            _proxy = proxy;
            _callback = callback;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _proxy.RemoveCallback(_callback);
        }
    }
}
