using System.Reflection;

namespace twitterXcrypto.util;

internal static class EnvironmentVariables
{
    internal const string
        TWITTER_CONSUMERKEY = nameof(TWITTER_CONSUMERKEY),
        TWITTER_CONSUMERSECRET = nameof(TWITTER_CONSUMERSECRET),
        TWITTER_ACCESSTOKEN = nameof(TWITTER_ACCESSTOKEN),
        TWITTER_ACCESSSECRET = nameof(TWITTER_ACCESSSECRET),
        DISCORD_WEBHOOK_URL = nameof(DISCORD_WEBHOOK_URL),
        USERS_TO_FOLLOW = nameof(USERS_TO_FOLLOW),
        XCMC_PRO_API_KEY = nameof(XCMC_PRO_API_KEY),
        NUM_ASSETS_TO_WATCH = nameof(NUM_ASSETS_TO_WATCH),
        DATABASE_IP = nameof(DATABASE_IP),
        DATABASE_PORT = nameof(DATABASE_PORT),
        DATABASE_NAME = nameof(DATABASE_NAME),
        DATABASE_USER = nameof(DATABASE_USER),
        DATABASE_PWD = nameof(DATABASE_PWD),
        TESSERACT_LOCALE = nameof(TESSERACT_LOCALE),
        TESSERACT_WHITELIST = nameof(TESSERACT_WHITELIST);

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
        typeof(EnvironmentVariables).GetFields(BindingFlags.NonPublic | BindingFlags.Static)
                                    .Where(fi => fi.IsLiteral && !fi.IsInitOnly)
                                    .ForEach(constant => _tokens[constant.Name] = Environment.GetEnvironmentVariable(constant.Name));

        string? numAssetsToWatchString = Environment.GetEnvironmentVariable(NUM_ASSETS_TO_WATCH);
        int.TryParse(numAssetsToWatchString, out _numAssetsToWatch);

        string? usersToFollowConcatenated = Environment.GetEnvironmentVariable(USERS_TO_FOLLOW);
        _usersToFollow = usersToFollowConcatenated?.Split(',');
    }

    private static readonly Dictionary<string, string?> _tokens = new();
    private static readonly string[]? _usersToFollow;
    private static readonly int _numAssetsToWatch = -1;
}
