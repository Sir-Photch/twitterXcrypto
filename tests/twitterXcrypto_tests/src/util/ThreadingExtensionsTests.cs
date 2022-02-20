using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading;
using System.Threading.Tasks;
using twitterXcrypto.util;

namespace twitterXcrypto_tests.util;

[TestClass]
public class ThreadingExtensionsTests
{
    [TestMethod]
    public void IsEnterableTest()
    {
        SemaphoreSlim sem = new(0);
        Assert.IsFalse(sem.IsEnterable());

        sem = new(1);
        Assert.IsTrue(sem.IsEnterable());
    }

    [TestMethod]
    public async Task IsEnterableTestAsync()
    {
        SemaphoreSlim sem = new(0);
        Assert.IsFalse(await sem.IsEnterableAsync());

        sem = new(1);
        Assert.IsTrue(await sem.IsEnterableAsync());
    }

    [TestMethod]
    public async Task IsCreatedOrRunningTestAsync()
    {
        Task waiter = Task.Delay(200);
        Assert.IsTrue(waiter.IsCreatedOrRunning());
        await waiter;
        Assert.IsFalse(waiter.IsCreatedOrRunning());

        Task exception = Task.FromException(new());
        Assert.IsFalse(exception.IsCreatedOrRunning());

        Task? @null = null;
        Assert.IsFalse(@null.IsCreatedOrRunning());
    }
}