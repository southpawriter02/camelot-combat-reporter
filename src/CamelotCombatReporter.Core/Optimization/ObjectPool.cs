using System.Collections.Concurrent;

namespace CamelotCombatReporter.Core.Optimization;

/// <summary>
/// Generic thread-safe object pool for reducing allocations of frequently created objects.
/// </summary>
/// <typeparam name="T">The type of object to pool.</typeparam>
public class ObjectPool<T> where T : class
{
    private readonly ConcurrentBag<T> _pool = new();
    private readonly Func<T> _factory;
    private readonly Action<T>? _reset;
    private readonly int _maxPoolSize;
    private int _rentCount;
    private int _returnCount;
    private int _createCount;

    /// <summary>
    /// Creates a new object pool.
    /// </summary>
    /// <param name="factory">Factory function to create new instances.</param>
    /// <param name="reset">Optional action to reset objects before returning to pool.</param>
    /// <param name="maxPoolSize">Maximum number of objects to keep in the pool.</param>
    public ObjectPool(Func<T> factory, Action<T>? reset = null, int maxPoolSize = 100)
    {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        _reset = reset;
        _maxPoolSize = maxPoolSize;
    }

    /// <summary>
    /// Gets the current number of available objects in the pool.
    /// </summary>
    public int AvailableCount => _pool.Count;

    /// <summary>
    /// Rents an object from the pool, creating a new one if none available.
    /// </summary>
    /// <returns>A pooled or new object instance.</returns>
    public T Rent()
    {
        Interlocked.Increment(ref _rentCount);

        if (_pool.TryTake(out var item))
            return item;

        Interlocked.Increment(ref _createCount);
        return _factory();
    }

    /// <summary>
    /// Returns an object to the pool.
    /// </summary>
    /// <param name="item">The object to return.</param>
    public void Return(T item)
    {
        if (item == null)
            return;

        Interlocked.Increment(ref _returnCount);

        // Don't exceed max pool size
        if (_pool.Count >= _maxPoolSize)
            return;

        _reset?.Invoke(item);
        _pool.Add(item);
    }

    /// <summary>
    /// Clears the pool.
    /// </summary>
    public void Clear()
    {
        while (_pool.TryTake(out _)) { }
    }

    /// <summary>
    /// Gets statistics about pool usage.
    /// </summary>
    public (int RentCount, int ReturnCount, int CreateCount, int AvailableCount) GetStatistics()
    {
        return (_rentCount, _returnCount, _createCount, _pool.Count);
    }
}

/// <summary>
/// Object pool for StringBuilder instances.
/// </summary>
public class StringBuilderPool : ObjectPool<System.Text.StringBuilder>
{
    /// <summary>
    /// Creates a new StringBuilder pool.
    /// </summary>
    /// <param name="initialCapacity">Initial capacity of each StringBuilder.</param>
    /// <param name="maxPoolSize">Maximum number of StringBuilders to keep.</param>
    public StringBuilderPool(int initialCapacity = 256, int maxPoolSize = 50)
        : base(
            factory: () => new System.Text.StringBuilder(initialCapacity),
            reset: sb => sb.Clear(),
            maxPoolSize: maxPoolSize)
    {
    }

    /// <summary>
    /// Gets the default shared instance.
    /// </summary>
    public static StringBuilderPool Shared { get; } = new();
}

/// <summary>
/// Object pool for List instances.
/// </summary>
/// <typeparam name="T">The type of list elements.</typeparam>
public class ListPool<T> : ObjectPool<List<T>>
{
    /// <summary>
    /// Creates a new List pool.
    /// </summary>
    /// <param name="initialCapacity">Initial capacity of each List.</param>
    /// <param name="maxPoolSize">Maximum number of Lists to keep.</param>
    public ListPool(int initialCapacity = 32, int maxPoolSize = 50)
        : base(
            factory: () => new List<T>(initialCapacity),
            reset: list => list.Clear(),
            maxPoolSize: maxPoolSize)
    {
    }
}
