namespace twitterXcrypto.util;

internal static class ThreadingExtensions
{
    internal static bool IsEnterable(this SemaphoreSlim semaphore)
    {
        bool isEnterable = semaphore.Wait(0);
        if (isEnterable) semaphore.Release();
        return isEnterable;
    }

    internal static async ValueTask<bool> IsEnterableAsync(this SemaphoreSlim semaphore)
    {
        bool isEnterable = await semaphore.WaitAsync(0);
        if (isEnterable) semaphore.Release();
        return isEnterable;
    }
}
