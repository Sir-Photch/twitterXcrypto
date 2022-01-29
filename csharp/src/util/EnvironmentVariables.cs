namespace twitterXcrypto.util;

internal static class EnvironmentVariables
{
    internal const string
        TWITTER_CONSUMERKEY = nameof(TWITTER_CONSUMERKEY),
        TWITTER_CONSUMERSECRET = nameof(TWITTER_CONSUMERSECRET),
        TWITTER_ACCESSTOKEN = nameof(TWITTER_ACCESSTOKEN),
        TWITTER_ACCESSSECRET = nameof(TWITTER_ACCESSSECRET),
        DISCORD_TOKEN = nameof(DISCORD_TOKEN),
        DISCORD_CHANNELID = nameof(DISCORD_CHANNELID),
        USERS_TO_FOLLOW = nameof(USERS_TO_FOLLOW);

    internal static IReadOnlyDictionary<string, string?> Tokens => _tokens;

    internal static IEnumerable<string>? UsersToFollow => _usersToFollow;

    static EnvironmentVariables()
    {
        _tokens[TWITTER_CONSUMERKEY] = Environment.GetEnvironmentVariable(TWITTER_CONSUMERKEY);
        _tokens[TWITTER_CONSUMERSECRET] = Environment.GetEnvironmentVariable(TWITTER_CONSUMERSECRET);
        _tokens[TWITTER_ACCESSTOKEN] = Environment.GetEnvironmentVariable(TWITTER_ACCESSTOKEN);
        _tokens[TWITTER_ACCESSSECRET] = Environment.GetEnvironmentVariable(TWITTER_ACCESSSECRET);
        _tokens[DISCORD_TOKEN] = Environment.GetEnvironmentVariable(DISCORD_TOKEN);
        _tokens[DISCORD_CHANNELID] = Environment.GetEnvironmentVariable(DISCORD_CHANNELID);

        string? usersToFollowConcatenated = Environment.GetEnvironmentVariable(USERS_TO_FOLLOW);
        _usersToFollow = usersToFollowConcatenated?.Split(',');
    }

    private static readonly Dictionary<string, string?> _tokens = new();

    private static readonly string[]? _usersToFollow;
}
