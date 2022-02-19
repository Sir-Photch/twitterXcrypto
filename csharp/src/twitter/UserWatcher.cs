using Microsoft.VisualStudio.Threading;
using Tweetinvi;
using Tweetinvi.Models;
using Tweetinvi.Streaming;
using twitterXcrypto.util;
using static twitterXcrypto.util.Log.Level;

namespace twitterXcrypto.twitter;

internal class UserWatcher : IDisposable
{
    #region private fields / properties
    private readonly IFilteredStream _stream;
    private readonly TwitterClient _client;
    private readonly Dictionary<long, User> _users = new();

    private readonly util.AsyncQueue<ITweet> _tweetQueue = new() { CompleteWhenCancelled = true };
    private CancellationTokenSource? _tweetQueueTokenSource;

    private readonly SemaphoreSlim _streamSemaphore = new(1);
    private bool _isWatching = false;
    private Task? _streamWatchdog;
    private CancellationTokenSource? _streamWatchdogTokenSource;
    #endregion

    #region ctor
    internal UserWatcher(TwitterClient client, IFilteredStream stream)
    {
        _stream = stream;
        _client = client;
    }
    #endregion

    #region properties

    internal IEnumerable<User> Users => _users.Values;

    internal bool IsWatching => _isWatching;

    internal event Action<Tweet>? TweetReceived;

    internal event Action? DisconnectTimeout;

    internal event Action? Connected;

    internal TimeSpan DisconnectTimeoutSpan { get; set; } = TimeSpan.FromSeconds(30.0);

    #endregion

    #region methods

    internal void StartWatching()
    {
        if (IsWatching) return;

        _stream.ClearFollows();

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
                    TweetReceived?.Invoke(Tweet.FromITweet(tweet));
                }
                catch { }
            }
        }).Forget();
        // task streaming twitter
        _streamSemaphore.Wait();
        Task.Run(_stream.StartMatchingAnyConditionAsync).Forget();
        _isWatching = true;

        Log.Write($"Started watching Users: {string.Join(", ", _users.Select(kvp => kvp.Value.ToString()))}");
    }

    internal void StopWatching()
    {
        if (!IsWatching) return;

        _stream.Stop();
        _tweetQueueTokenSource?.Cancel();
        
        _stream.StreamStarted -= OnStreamStarted;
        _stream.StreamStopped -= OnStreamStopped;
        _stream.MatchingTweetReceived -= OnMatchingTweetReceived;
        _stream.DisconnectMessageReceived -= OnStreamDisconnectMessageReceived;
        Log.Write("Stopped watching");

        _isWatching = false;
    }

    internal async Task<bool> AddUserAsync(string username)
    {
        IUser iUser;
        try
        {
            iUser = await _client.Users.GetUserAsync(username);
        }
        catch (Exception e)
        {
            await Log.WriteAsync($"Could not find user {username}", e);
            return false;
        }

        lock (_users)
        {
            if (_users.ContainsKey(iUser.Id))
                return true;

            _users[iUser.Id] = new User { Name = iUser.Name, Id = iUser.Id };
        }

        return true;
    }

    internal async Task<bool> AddUserAsync(string[] usernames)
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

    internal async Task<bool> RemoveUserAsync(string username)
    {
        IUser iUser;
        try
        {
            iUser = await _client.Users.GetUserAsync(username);
        }
        catch (Exception e)
        {
            await Log.WriteAsync("Could not find user", e);
            return false;
        }

        lock (_users)
            return _users.Remove(iUser.Id);
    }

    internal async Task<bool> RemoveUserAsync(string[] usernames)
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

    public void Dispose()
    {
        StopWatching();
        _tweetQueueTokenSource?.Dispose();
        _streamWatchdogTokenSource?.Dispose();
        _streamSemaphore?.Dispose();
    }

    #endregion

    #region private methods

    private void OnStreamStopped(object? sender, Tweetinvi.Events.StreamStoppedEventArgs e)
    {
        if (e.Exception is not null) // only null when disconnected by user
        {
            if (!_streamWatchdog.IsCreatedOrRunning())
            {
                _streamWatchdogTokenSource = new();
                _streamWatchdog = WatchdogActivityAsync();
            }
            
            Log.Write($"Stream stopped unexpectedly. Code: {e.DisconnectMessage.Code}, Reason: {e.DisconnectMessage.Reason}; Restarting...", e.Exception, WRN);
            WaitRestartAsync().Forget();
        }
        _streamSemaphore.Release();
    }

    private void OnMatchingTweetReceived(object? sender, Tweetinvi.Events.MatchedTweetReceivedEventArgs e)
    {
        _streamWatchdogTokenSource?.Cancel();

        bool success = _tweetQueue.Enqueue(e.Tweet);
        if (!success)
            Log.Write("Could not post tweet to queue");
    }

    private void OnStreamDisconnectMessageReceived(object? sender, Tweetinvi.Events.DisconnectedEventArgs e)
    {
        if (!_streamWatchdog.IsCreatedOrRunning())
        {
            _streamWatchdogTokenSource = new();
            _streamWatchdog = WatchdogActivityAsync();
        }

        _streamSemaphore.Release();
        Log.Write($"Twitter aborted connection. Code: {e.DisconnectMessage.Code}, Reason: {e.DisconnectMessage.Reason}; Restarting...", FTL);
        WaitRestartAsync().Forget();
    }

    private async Task WaitRestartAsync()
    {
        var enterSemaphore = _streamSemaphore.WaitAsync();
        await Task.Delay(500);
        await enterSemaphore;
        await Task.Run(_stream.StartMatchingAnyConditionAsync);
    }

    private void OnStreamStarted(object? sender, EventArgs e)
    {
        _streamWatchdogTokenSource?.Cancel();
        Connected?.Invoke();
        Log.Write("Twitter stream started.");
    }

    private async Task WatchdogActivityAsync()
    {
        await Task.Delay(DisconnectTimeoutSpan);
        if (!_streamWatchdogTokenSource?.IsCancellationRequested ?? true)
            DisconnectTimeout?.Invoke();
    }

    #endregion
}
