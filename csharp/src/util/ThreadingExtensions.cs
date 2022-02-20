using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("twitterXcrypto_tests")]

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

    internal static bool IsCreatedOrRunning(this Task? nullableTask) => nullableTask is Task task && task.Status is not (TaskStatus.Faulted or TaskStatus.Canceled or TaskStatus.RanToCompletion);
}
