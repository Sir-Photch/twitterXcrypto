using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading;
using System.Threading.Tasks;
using twitterXcrypto.util;

namespace twitterXcrypto_tests.util;

[TestClass]
public class WatchdogTests
{
    [TestMethod]
    public async Task TestWatchdogTimeoutAsync()
    {
        TimeSpan delay = TimeSpan.FromMilliseconds(500);
        bool triggered = false;
        Watchdog doggo = new(delay, false);
        doggo.OnTimeout += (_, __) => Task.Run(() => triggered = true);

        await Task.Delay(delay + TimeSpan.FromMilliseconds(100));

        Assert.IsTrue(triggered);
    }

    [TestMethod]
    public async Task TestWatchdogPetAsync()
    {
        TimeSpan delay = TimeSpan.FromMilliseconds(500);
        bool triggered = false;
        Watchdog doggo = new(delay, false);
        doggo.OnTimeout -= (_, __) => Task.Run(() => triggered = true);

        await Task.Delay(400);
        doggo.Pet();
        await Task.Delay(200);

        Assert.IsFalse(triggered);
    }

    [TestMethod]
    public async Task TestWatchdogContinueOnAlertAsync()
    {
        TimeSpan delay = TimeSpan.FromMilliseconds(100);
        int triggerCount = 0;
        Watchdog doggo = new(delay, true);
        doggo.OnTimeout += (_, __) => Task.Run(() => Interlocked.Increment(ref triggerCount));

        await Task.Delay(3 * delay);

        Assert.IsTrue(triggerCount > 0);
    }
}
