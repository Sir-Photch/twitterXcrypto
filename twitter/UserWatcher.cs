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
        private readonly HashSet<User> _users = new();
        private bool _isWatching = false;
        #endregion

        #region ctor
        public UserWatcher(TwitterClient client, IFilteredStream stream)
        {
            _stream = stream;
            _client = client;
        }
        #endregion

        #region properties

        public IEnumerable<User> Users => _users;

        public bool IsWatching => _isWatching;

        public event Action<Tweet>? TweetReceived;

        #endregion

        #region methods

        public void StartWatching()
        {
            if (IsWatching) return;

            _stream.ClearFollows();

            _users.ForEach(usr => _stream.AddFollow(usr.Id));

            _stream.StreamStopped += OnStreamStopped;
            _stream.MatchingTweetReceived += OnMatchingTweetReceived;

            Task.Run(_stream.StartMatchingAnyConditionAsync);
            _isWatching = true;
            Log.Write($"Started watching {_users.Count} users");
        }

        public void StopWatching()
        {
            if (!IsWatching) return;

            _stream.Stop();
            _stream.StreamStopped -= OnStreamStopped;
            _stream.MatchingTweetReceived -= OnMatchingTweetReceived;
            _isWatching = false;
            Log.Write("Stopped watching");
        }

        public async Task<bool> AddUser(string username)
        {
            IUser user;
            try
            {
                user = await _client.Users.GetUserAsync(username);
            }
            catch (Exception e)
            {
                Log.Write($"Could not find user {username}", e);
                return false;
            }

            lock (_users)
            {
                if (_users.Any(usr => usr.Id == user.Id))
                    return true;

                _users.Add(new User { Name = user.Name, Id = user.Id });
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
                Log.Write($"Could not find users", e);
                return false;
            }

            lock (_users)
            {
                if (_users.Select(usr => usr.Id).ContainsEvery(iUsers.Select(iusr => iusr.Id)))
                    return true;

                IEnumerable<User> users = iUsers.Select(usr => new User { Name = usr.Name, Id = usr.Id });

                foreach (User user in users)
                    _users.Add(user);
            }

            return true;
        }

        public void RemoveUser(string username)
        {
            lock (_users)
                _users.RemoveWhere(usr => usr.Name == username);
        }

        public void RemoveUser(string[] usernames) => usernames.ForEach(usr => RemoveUser(usr));

        #endregion

        #region private methods

        private void OnStreamStopped(object? sender, Tweetinvi.Events.StreamStoppedEventArgs e)
        {
            _isWatching = false;
            Log.Write($"Stream stopped {e.DisconnectMessage}", e.Exception, WRN);
        }

        private void OnMatchingTweetReceived(object? sender, Tweetinvi.Events.MatchedTweetReceivedEventArgs e)
        {
            lock (_users)
                if (_users.Select(usr => usr.Id).Contains(e.Tweet.CreatedBy.Id)) // HACK check this another way. callback takes too long
                    TweetReceived?.Invoke(Tweet.FromITweet(e.Tweet));
        }

        #endregion
    }
}
