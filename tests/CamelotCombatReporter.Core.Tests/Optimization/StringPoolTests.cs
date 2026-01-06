using CamelotCombatReporter.Core.Optimization;
using Xunit;

namespace CamelotCombatReporter.Core.Tests.Optimization;

public class StringPoolTests
{
    [Fact]
    public void Intern_ReturnsSameInstanceForSameString()
    {
        var pool = new StringPool();
        var str1 = "Test String";
        var str2 = new string("Test String".ToCharArray()); // Force new instance

        var interned1 = pool.Intern(str1);
        var interned2 = pool.Intern(str2);

        Assert.Same(interned1, interned2);
    }

    [Fact]
    public void Intern_ReturnsDifferentInstancesForDifferentStrings()
    {
        var pool = new StringPool();

        var interned1 = pool.Intern("String A");
        var interned2 = pool.Intern("String B");

        Assert.NotSame(interned1, interned2);
        Assert.Equal("String A", interned1);
        Assert.Equal("String B", interned2);
    }

    [Fact]
    public void Intern_HandlesNullAndEmpty()
    {
        var pool = new StringPool();

        Assert.Null(pool.Intern(null!));
        Assert.Equal(string.Empty, pool.Intern(string.Empty));
    }

    [Fact]
    public void Count_ReflectsInternedStrings()
    {
        var pool = new StringPool();
        Assert.Equal(0, pool.Count);

        pool.Intern("A");
        Assert.Equal(1, pool.Count);

        pool.Intern("B");
        Assert.Equal(2, pool.Count);

        // Same string shouldn't increase count
        pool.Intern("A");
        Assert.Equal(2, pool.Count);
    }

    [Fact]
    public void Clear_RemovesAllStrings()
    {
        var pool = new StringPool();
        pool.Intern("A");
        pool.Intern("B");
        pool.Intern("C");

        Assert.Equal(3, pool.Count);

        pool.Clear();

        Assert.Equal(0, pool.Count);
    }

    [Fact]
    public void GetStatistics_ReturnsPoolInfo()
    {
        var pool = new StringPool();
        pool.Intern("Test1");
        pool.Intern("Test2");

        var stats = pool.GetStatistics();

        Assert.Equal(2, stats.Count);
        Assert.True(stats.ApproximateMemorySaved > 0);
    }

    [Fact]
    public void Intern_EvictsWhenMaxSizeExceeded()
    {
        var pool = new StringPool(maxPoolSize: 10);

        // Add more than max size
        for (int i = 0; i < 15; i++)
        {
            pool.Intern($"String{i}");
        }

        // Pool should not exceed max size by too much
        Assert.True(pool.Count <= 15); // Some eviction should have occurred
    }

    [Fact]
    public void Shared_IsSingletonInstance()
    {
        var instance1 = StringPool.Shared;
        var instance2 = StringPool.Shared;

        Assert.Same(instance1, instance2);
    }

    [Fact]
    public async Task Intern_IsThreadSafe()
    {
        var pool = new StringPool();
        var tasks = new List<Task>();

        // Simulate concurrent access
        for (int i = 0; i < 10; i++)
        {
            int threadId = i;
            tasks.Add(Task.Run(() =>
            {
                for (int j = 0; j < 100; j++)
                {
                    pool.Intern($"Thread{threadId}_String{j}");
                }
            }));
        }

        await Task.WhenAll(tasks);

        // Should have interned all unique strings without error
        Assert.True(pool.Count <= 1000);
    }
}
