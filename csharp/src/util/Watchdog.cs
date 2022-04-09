namespace twitterXcrypto.util;

internal class Watchdog
{
    #region private fields
    private readonly Task _task;
    private readonly CancellationTokenSource _cts = new();
    private DateTime _created = DateTime.Now;

    private volatile int _sedated = 0; // 0: false, 1: true
    #endregion

    #region event

    internal delegate Task TimeoutHandler(TimeSpan timeout, TimeSpan elapsed);
    internal event TimeoutHandler? OnTimeout;

    internal delegate Task PetHandler(TimeSpan elapsed);
    internal event PetHandler? OnPet;

    #endregion

    #region ctor

    internal Watchdog(TimeSpan timeout, bool continueOnAlert)
    {
        _task = Task.Run(async () =>
        {
            while (!_cts.IsCancellationRequested)
            {
                await Task.Delay(timeout);

                if (_sedated == 1)
                {
                    Interlocked.Exchange(ref _sedated, 0);
                    continue;
                }

                TimeSpan elapsed = DateTime.Now - _created;

                Task? invoking = OnTimeout?.GetInvocationList()
                                           .OfType<TimeoutHandler>()
                                           .ToAsyncEnumerable()
                                           .ForEachAwaitWithCancellationAsync(
                                                (handler, token) => handler.Invoke(timeout, elapsed),
                                                _cts.Token);

                if (invoking is not null)
                {
                    try { await invoking; }
                    catch (Exception e) { await Log.WriteAsync($"Watchdog {nameof(OnTimeout)}-Invocation error!", e); }
                }

                if (!continueOnAlert)
                    return;

                _created = DateTime.Now;
            }
        }, _cts.Token);
    }

    #endregion

    #region methods

    internal void Pet()
    {
        Interlocked.Exchange(ref _sedated, 1);
        try { OnPet?.Invoke(DateTime.Now - _created); } catch { }
    }

    #endregion
}
