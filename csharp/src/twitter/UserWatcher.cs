using Tweetinvi;
using Tweetinvi.Models;
using Tweetinvi.Streaming;
using twitterXcrypto.util;
using static twitterXcrypto.util.Log.LogLevel;

namespace twitterXcrypto.twitter
{
    internal class UserWatcher
    {
        #region private fields
        private readonly IFilteredStream _stream;
        private readonly TwitterClient _client;
        private readonly Dictionary<long, User> _users = new();
        private bool _isWatching = false;
        private readonly AsyncQueue<ITweet> _tweetQueue = new() { CompleteWhenCancelled = true };
        private CancellationTokenSource? _tokenSource;
        #endregion

        #region ctor
        public UserWatcher(TwitterClient client, IFilteredStream stream)
        {
            _stream = stream;
            _client = client;
        }
        #endregion

        #region properties

        public IEnumerable<User> Users => _users.Values;

        public bool IsWatching => _isWatching;

        public event Action<Tweet>? TweetReceived;

        #endregion

        #region methods

        public void StartWatching()
        {
            if (IsWatching) return;

            _stream.ClearFollows();

            _users.Keys.ForEach(uid => _stream.AddFollow(uid));

            _stream.StreamStopped += OnStreamStopped;
            _stream.MatchingTweetReceived += OnMatchingTweetReceived;

            _tokenSource = new();
            // task getting and filtering tweets from twitter
            Task.Run(async () =>
            {
                await foreach (ITweet tweet in _tweetQueue.WithCancellation(_tokenSource.Token))
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
            });
            // task streaming twitter
            Task.Run(_stream.StartMatchingAnyConditionAsync);

            _isWatching = true;
            Log.Write($"Started watching Users: {string.Join(", ", _users.Select(kvp => kvp.Value.ToString()))}");
        }

        public void StopWatching()
        {
            if (!IsWatching) return;

            _stream.Stop();
            _tokenSource.Cancel();
            _stream.StreamStopped -= OnStreamStopped;
            _stream.MatchingTweetReceived -= OnMatchingTweetReceived;
            _isWatching = false;
            Log.Write("Stopped watching");
        }

        public async Task<bool> AddUser(string username)
        {
            IUser iUser;
            try
            {
                iUser = await _client.Users.GetUserAsync(username);
            }
            catch (Exception e)
            {
                Log.Write($"Could not find user {username}", e);
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

        public async Task<bool> AddUser(string[] usernames)
        {
            IUser[] iUsers;
            try
            {
                iUsers = await _client.Users.GetUsersAsync(usernames);
            }
            catch (Exception e)
            {
                Log.Write("Could not find users", e);
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

        public async Task<bool> RemoveUser(string username)
        {
            IUser iUser;
            try
            {
                iUser = await _client.Users.GetUserAsync(username);
            }
            catch (Exception e)
            {
                Log.Write("Could not find user", e);
                return false;
            }

            lock (_users)
                return _users.Remove(iUser.Id);
        }

        public async Task<bool> RemoveUser(string[] usernames)
        {
            IUser[] iUsers;
            try
            {
                iUsers = await _client.Users.GetUsersAsync(usernames);
            }
            catch (Exception e)
            {
                Log.Write("Could not find users", e);
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

        private void OnStreamStopped(object? sender, Tweetinvi.Events.StreamStoppedEventArgs e)
        {
            if (e.Exception is not null)
            {
                Log.Write($"Stream stopped unexpectedly. Restarting...", e.Exception, WRN);
                Task.Run(_stream.StartMatchingAnyConditionAsync);
            }
            else
            {
                _isWatching = false;
            }
        }

        private void OnMatchingTweetReceived(object? sender, Tweetinvi.Events.MatchedTweetReceivedEventArgs e)
        {
            bool success = _tweetQueue.Enqueue(e.Tweet);
            if (!success)
                Log.Write("Could not post tweet to queue");
        }

        #endregion
    }
}
