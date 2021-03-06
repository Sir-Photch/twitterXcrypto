using Microsoft.VisualStudio.Threading;
using Tweetinvi;
using Tweetinvi.Models;
using Tweetinvi.Streaming;
using twitterXcrypto.util;
using static twitterXcrypto.util.Log.Level;

namespace twitterXcrypto.twitter;

internal class UserWatcher
{
    #region private fields / properties

    private readonly TwitterClient _client;
    private readonly Dictionary<long, User> _users = new();
    private IFilteredStream? _stream;

    private readonly SemaphoreSlim _heartbeatInvokeSemaphore = new(1);
    private Watchdog? _watchdog;

    private readonly util.AsyncQueue<ITweet> _tweetQueue = new() { CompleteWhenCancelled = false };
    private CancellationTokenSource? _tweetQueueTokenSource;

    private readonly SemaphoreSlim _streamSemaphore = new(1);
    private bool _isWatching = false;
    #endregion

    #region ctor
    internal UserWatcher(TwitterClient client)
    {
        _client = client;
    }
    #endregion

    #region properties

    internal IEnumerable<User> Users => _users.Values;

    internal bool IsWatching => _isWatching;

    internal event Func<Tweet, Task>? TweetReceived;

    internal event Func<Task>? DisconnectTimeout;

    internal event Func<Task>? Connected;

    internal event Func<Task>? Heartbeat;

    internal TimeSpan DisconnectTimeoutSpan { get; set; } = TimeSpan.FromSeconds(30.0);

    #endregion

    #region methods    

    internal void StartWatching()
    {
        _StartWatching();
        _watchdog = new(DisconnectTimeoutSpan, continueOnAlert: true);
        _watchdog.OnTimeout += OnWatchdogTimeoutAsync;
        _watchdog.OnPet += OnWatchdogPetAsync;
        Log.Write($"Started watching Users: {string.Join(", ", _users.Select(kvp => kvp.Value.ToString()))}");
    }

    internal void StopWatching()
    {
        if (_watchdog is not null)
        {
            _watchdog.OnPet -= OnWatchdogPetAsync;
            _watchdog.OnTimeout -= OnWatchdogTimeoutAsync;
        }
        _StopWatching();
        Log.Write("Stopped watching");
    }

    internal async Task<bool> AddUserAsync(params string[] usernames)
    {
        IUser[] iUsers;
        try
        {
            iUsers = await _client.Users.GetUsersAsync(usernames);
        }
        catch (Exception e)
        {
            await Log.WriteAsync("Could not find users", e);
            return false;
        }

        lock (_users)
        {
            if (_users.ContainsEveryKey(iUsers.Select(iusr => iusr.Id)))
                return true;

            IEnumerable<User> users = iUsers.Select(usr => new User { Name = usr.Name, Id = usr.Id });

            foreach (User user in users)
                _users[user.Id] = user;
        }

        return true;
    }

    internal async Task<bool> RemoveUserAsync(params string[] usernames)
    {
        IUser[] iUsers;
        try
        {
            iUsers = await _client.Users.GetUsersAsync(usernames);
        }
        catch (Exception e)
        {
            await Log.WriteAsync("Could not find users", e);
            return false;
        }

        lock (_users)
            iUsers.Select(usr => usr.Id)
                  .Distinct()
                  .ForEach(uid => _users.Remove(uid));

        return true;
    }

    #endregion

    #region private methods

    private async Task OnWatchdogTimeoutAsync(TimeSpan timeout, TimeSpan elapsed)
    {
        await Log.WriteAsync($"Watchdog timeout after {timeout}, lasted {elapsed}");
        if (DisconnectTimeout is not null)
        {
            var tasks = DisconnectTimeout.GetInvocationList()
                                         .OfType<Task>();

            tasks.Where(task => !task.IsCompleted)
                 .ForEach(task => task.Start());

            await Task.WhenAll(tasks);
        }
    }

    private async Task OnWatchdogPetAsync(TimeSpan elapsed)
    {
        if (!await _heartbeatInvokeSemaphore.WaitAsync(0))
            return;

        try
        {
            if (Heartbeat is not null)
                await Heartbeat();
        }
        finally
        {
            _heartbeatInvokeSemaphore.Release();
        }
    }

    private void OnStreamStopped(object? sender, Tweetinvi.Events.StreamStoppedEventArgs e)
    {
        if (e.Exception is not null) // only null when disconnected by user
        {
            Log.Write($"Stream stopped unexpectedly. Restarting...", e.Exception, VRB);
            WaitRestartAsync().Forget();
        }
        _streamSemaphore.Release();
    }

    private void OnMatchingTweetReceived(object? sender, Tweetinvi.Events.MatchedTweetReceivedEventArgs e)
    {
        _watchdog?.Pet();

        bool success = _tweetQueue.Enqueue(e.Tweet);

        if (!success)
            Log.Write("Could not post tweet to queue", VRB);
    }

    private void OnStreamDisconnectMessageReceived(object? sender, Tweetinvi.Events.DisconnectedEventArgs e)
    {
        _streamSemaphore.Release();
        Log.Write($"Twitter aborted connection. Code: {e.DisconnectMessage.Code}, Reason: {e.DisconnectMessage.Reason}; Restarting...", FTL);
        WaitRestartAsync().Forget();
    }

    private async Task WaitRestartAsync()
    {
        await _streamSemaphore.WaitAsync();

        try
        {
            _StopWatching();
        }
        catch (Exception e)
        {
            await Log.WriteAsync($"Stream restart cleanup failed", e);
        }

        _StartWatching();
    }

    private void OnStreamStarted(object? sender, EventArgs e)
    {
        if (Connected is not null)
            Task.Run(Connected).Wait();

        Log.Write("Twitter stream started.", VRB);
    }

    private void _StopWatching()
    {
        if (!IsWatching) return;

        if (_stream is null)
            return;

        _stream.Stop();
        _tweetQueueTokenSource?.Cancel();

        _stream.StreamStarted -= OnStreamStarted;
        _stream.StreamStopped -= OnStreamStopped;
        _stream.MatchingTweetReceived -= OnMatchingTweetReceived;
        _stream.DisconnectMessageReceived -= OnStreamDisconnectMessageReceived;

        _isWatching = false;
    }

    private void _StartWatching()
    {
        if (IsWatching) return;

        _stream = _client.Streams.CreateFilteredStream();

        _users.Keys.ForEach(uid => _stream.AddFollow(uid));

        _stream.DisconnectMessageReceived += OnStreamDisconnectMessageReceived;
        _stream.StreamStopped += OnStreamStopped;
        _stream.MatchingTweetReceived += OnMatchingTweetReceived;
        _stream.StreamStarted += OnStreamStarted;

        _tweetQueueTokenSource = new();
        // task getting and filtering tweets from twitter
        Task.Run(async () =>
        {
            await foreach (ITweet tweet in _tweetQueue.WithCancellation(_tweetQueueTokenSource.Token))
            {
                lock (_users)
                    if (!_users.ContainsKey(tweet.CreatedBy.Id))
                        continue;
                try
                {
                    if (TweetReceived is not null)
                        await TweetReceived(Tweet.FromITweet(tweet));
                }
                catch { }
            }
        }).Forget();
        // task streaming twitter
        _streamSemaphore.Wait();
        Task.Run(_stream.StartMatchingAnyConditionAsync).Forget();
        _isWatching = true;
    }

    #endregion
}
