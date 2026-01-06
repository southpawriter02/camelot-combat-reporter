using System.Text;
using CamelotCombatReporter.Core.Optimization;
using Xunit;

namespace CamelotCombatReporter.Core.Tests.Optimization;

public class ObjectPoolTests
{
    [Fact]
    public void Rent_CreatesNewObjectWhenPoolEmpty()
    {
        var pool = new ObjectPool<StringBuilder>(() => new StringBuilder());

        var item = pool.Rent();

        Assert.NotNull(item);
    }

    [Fact]
    public void Return_AddsObjectToPool()
    {
        var pool = new ObjectPool<StringBuilder>(() => new StringBuilder());
        var item = pool.Rent();

        Assert.Equal(0, pool.AvailableCount);

        pool.Return(item);

        Assert.Equal(1, pool.AvailableCount);
    }

    [Fact]
    public void Rent_ReusesReturnedObject()
    {
        var pool = new ObjectPool<StringBuilder>(() => new StringBuilder());

        var item1 = pool.Rent();
        pool.Return(item1);
        var item2 = pool.Rent();

        Assert.Same(item1, item2);
    }

    [Fact]
    public void Return_CallsResetAction()
    {
        bool resetCalled = false;
        var pool = new ObjectPool<StringBuilder>(
            factory: () => new StringBuilder(),
            reset: sb =>
            {
                resetCalled = true;
                sb.Clear();
            });

        var item = pool.Rent();
        item.Append("test");

        pool.Return(item);

        Assert.True(resetCalled);
        Assert.Equal(0, item.Length);
    }

    [Fact]
    public void Return_DoesNotExceedMaxPoolSize()
    {
        var pool = new ObjectPool<StringBuilder>(
            factory: () => new StringBuilder(),
            maxPoolSize: 5);

        var items = new List<StringBuilder>();
        for (int i = 0; i < 10; i++)
        {
            items.Add(pool.Rent());
        }

        foreach (var item in items)
        {
            pool.Return(item);
        }

        Assert.True(pool.AvailableCount <= 5);
    }

    [Fact]
    public void Return_HandlesNull()
    {
        var pool = new ObjectPool<StringBuilder>(() => new StringBuilder());

        // Should not throw
        pool.Return(null!);

        Assert.Equal(0, pool.AvailableCount);
    }

    [Fact]
    public void Clear_RemovesAllPooledObjects()
    {
        var pool = new ObjectPool<StringBuilder>(() => new StringBuilder());

        var item1 = pool.Rent();
        var item2 = pool.Rent();
        pool.Return(item1);
        pool.Return(item2);

        Assert.Equal(2, pool.AvailableCount);

        pool.Clear();

        Assert.Equal(0, pool.AvailableCount);
    }

    [Fact]
    public void GetStatistics_ReturnsUsageInfo()
    {
        var pool = new ObjectPool<StringBuilder>(() => new StringBuilder());

        pool.Rent();
        pool.Rent();
        var item = pool.Rent();
        pool.Return(item);

        var stats = pool.GetStatistics();

        Assert.Equal(3, stats.RentCount);
        Assert.Equal(1, stats.ReturnCount);
        Assert.Equal(3, stats.CreateCount);
        Assert.Equal(1, stats.AvailableCount);
    }

    [Fact]
    public async Task ObjectPool_IsThreadSafe()
    {
        var pool = new ObjectPool<StringBuilder>(() => new StringBuilder(), maxPoolSize: 50);
        var tasks = new List<Task>();

        for (int i = 0; i < 10; i++)
        {
            tasks.Add(Task.Run(() =>
            {
                for (int j = 0; j < 100; j++)
                {
                    var item = pool.Rent();
                    item.Append("test");
                    pool.Return(item);
                }
            }));
        }

        await Task.WhenAll(tasks);

        var stats = pool.GetStatistics();
        Assert.Equal(1000, stats.RentCount);
        Assert.Equal(1000, stats.ReturnCount);
    }
}

public class StringBuilderPoolTests
{
    [Fact]
    public void StringBuilderPool_ResetsOnReturn()
    {
        var pool = StringBuilderPool.Shared;

        var sb = pool.Rent();
        sb.Append("test content");

        pool.Return(sb);

        // On next rent, should be cleared
        var sb2 = pool.Rent();
        pool.Return(sb2);

        // If same instance reused, it was cleared
        if (ReferenceEquals(sb, sb2))
        {
            Assert.Equal(0, sb2.Length);
        }
    }

    [Fact]
    public void Shared_IsSingletonInstance()
    {
        var instance1 = StringBuilderPool.Shared;
        var instance2 = StringBuilderPool.Shared;

        Assert.Same(instance1, instance2);
    }
}

public class ListPoolTests
{
    [Fact]
    public void ListPool_ClearsOnReturn()
    {
        var pool = new ListPool<int>();

        var list = pool.Rent();
        list.Add(1);
        list.Add(2);
        list.Add(3);

        pool.Return(list);

        var list2 = pool.Rent();
        pool.Return(list2);

        // If same instance reused, it was cleared
        if (ReferenceEquals(list, list2))
        {
            Assert.Empty(list2);
        }
    }

    [Fact]
    public void ListPool_CreatesWithInitialCapacity()
    {
        var pool = new ListPool<int>(initialCapacity: 100);

        var list = pool.Rent();

        // List should have capacity of at least the initial capacity
        Assert.True(list.Capacity >= 100);

        pool.Return(list);
    }
}
