namespace twitterXcrypto.util;

internal static class EnvironmentVariables
{
    internal const string
        TWITTER_CONSUMERKEY = nameof(TWITTER_CONSUMERKEY),
        TWITTER_CONSUMERSECRET = nameof(TWITTER_CONSUMERSECRET),
        TWITTER_ACCESSTOKEN = nameof(TWITTER_ACCESSTOKEN),
        TWITTER_ACCESSSECRET = nameof(TWITTER_ACCESSSECRET),
        DISCORD_TOKEN = nameof(DISCORD_TOKEN),
        DISCORD_CHANNELID = nameof(DISCORD_CHANNELID);

    public static IReadOnlyDictionary<string, string> Tokens => _tokens;

    static EnvironmentVariables()
    {
        _tokens[TWITTER_CONSUMERKEY] = Environment.GetEnvironmentVariable(TWITTER_CONSUMERKEY) ?? string.Empty;
        _tokens[TWITTER_CONSUMERSECRET] = Environment.GetEnvironmentVariable(TWITTER_CONSUMERSECRET) ?? string.Empty;
        _tokens[TWITTER_ACCESSTOKEN] = Environment.GetEnvironmentVariable(TWITTER_ACCESSTOKEN) ?? string.Empty;
        _tokens[TWITTER_ACCESSSECRET] = Environment.GetEnvironmentVariable(TWITTER_ACCESSSECRET) ?? string.Empty;
        _tokens[DISCORD_TOKEN] = Environment.GetEnvironmentVariable(DISCORD_TOKEN) ?? string.Empty;
        _tokens[DISCORD_CHANNELID] = Environment.GetEnvironmentVariable(DISCORD_CHANNELID) ?? string.Empty;
    }

    private static readonly Dictionary<string, string> _tokens = new();
}
