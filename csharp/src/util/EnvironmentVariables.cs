namespace twitterXcrypto.util;

internal static class EnvironmentVariables
{
    internal const string
        TWITTER_CONSUMERKEY     = nameof(TWITTER_CONSUMERKEY),
        TWITTER_CONSUMERSECRET  = nameof(TWITTER_CONSUMERSECRET),
        TWITTER_ACCESSTOKEN     = nameof(TWITTER_ACCESSTOKEN),
        TWITTER_ACCESSSECRET    = nameof(TWITTER_ACCESSSECRET),
        DISCORD_TOKEN           = nameof(DISCORD_TOKEN),
        DISCORD_CHANNELID       = nameof(DISCORD_CHANNELID),
        USERS_TO_FOLLOW         = nameof(USERS_TO_FOLLOW),
        XCMC_PRO_API_KEY        = nameof(XCMC_PRO_API_KEY),
        NUM_ASSETS_TO_WATCH     = nameof(NUM_ASSETS_TO_WATCH),
        DATABASE_IP             = nameof(DATABASE_IP),
        DATABASE_PORT           = nameof(DATABASE_PORT),
        DATABASE_NAME           = nameof(DATABASE_NAME),
        DATABASE_USER           = nameof(DATABASE_USER),
        DATABASE_PWD            = nameof(DATABASE_PWD);

    internal static IReadOnlyDictionary<string, string?> Tokens => _tokens;
    internal static IEnumerable<string>? UsersToFollow => _usersToFollow;
    internal static int NumAsssetsToWatch => _numAssetsToWatch;

    internal static IEnumerable<string> Check()
    {
        List<string> missingVariables = new();

        missingVariables.AddRange(Tokens.Where(tkn => tkn.Value is null).Select(tkn => tkn.Key));
        if (UsersToFollow is null)
            missingVariables.Add(USERS_TO_FOLLOW);
        if (NumAsssetsToWatch == -1)
            missingVariables.Add(NUM_ASSETS_TO_WATCH);

        return missingVariables;
    }

    static EnvironmentVariables()
    {
        _tokens[TWITTER_CONSUMERKEY] = Environment.GetEnvironmentVariable(TWITTER_CONSUMERKEY);
        _tokens[TWITTER_CONSUMERSECRET] = Environment.GetEnvironmentVariable(TWITTER_CONSUMERSECRET);
        _tokens[TWITTER_ACCESSTOKEN] = Environment.GetEnvironmentVariable(TWITTER_ACCESSTOKEN);
        _tokens[TWITTER_ACCESSSECRET] = Environment.GetEnvironmentVariable(TWITTER_ACCESSSECRET);
        _tokens[DISCORD_TOKEN] = Environment.GetEnvironmentVariable(DISCORD_TOKEN);
        _tokens[DISCORD_CHANNELID] = Environment.GetEnvironmentVariable(DISCORD_CHANNELID);
        _tokens[XCMC_PRO_API_KEY] = Environment.GetEnvironmentVariable(XCMC_PRO_API_KEY);
        _tokens[DATABASE_IP] = Environment.GetEnvironmentVariable(DATABASE_IP);
        _tokens[DATABASE_NAME] = Environment.GetEnvironmentVariable(DATABASE_NAME);
        _tokens[DATABASE_PORT] = Environment.GetEnvironmentVariable(DATABASE_PORT);
        _tokens[DATABASE_USER] = Environment.GetEnvironmentVariable(DATABASE_USER);
        _tokens[DATABASE_PWD] = Environment.GetEnvironmentVariable(DATABASE_PWD);

        string? numAssetsToWatchString = Environment.GetEnvironmentVariable(NUM_ASSETS_TO_WATCH);
        try
        {
            _numAssetsToWatch = int.Parse(numAssetsToWatchString);
        }
        catch { }

        string? usersToFollowConcatenated = Environment.GetEnvironmentVariable(USERS_TO_FOLLOW);
        _usersToFollow = usersToFollowConcatenated?.Split(',');
    }

    private static readonly Dictionary<string, string?> _tokens = new();
    private static readonly string[]? _usersToFollow;
    private static readonly int _numAssetsToWatch = -1;
}
