using Tweetinvi;
using Tweetinvi.Streaming;
using twitterXcrypto.text;
using twitterXcrypto.discord;
using twitterXcrypto.twitter;
using twitterXcrypto.util;
using twitterXcrypto.crypto;
using static twitterXcrypto.util.EnvironmentVariables;

try
{
    var missingVariables = Check();
    if (missingVariables.Any())
    {
        Log.Write($"Environment-variables are not configured:{Environment.NewLine}{string.Join(Environment.NewLine, missingVariables)}");
        Environment.Exit(1);
    }

    string imageDirectory = Path.Combine(Environment.CurrentDirectory, "twixcry_images");

    CoinmarketcapClient coinClient = new(Tokens[XCMC_PRO_API_KEY]) { NumAssetsToGet = (uint)NumAsssetsToWatch };
    KeywordFinder? keywordFinder = null;
    try
    {
        await coinClient.RefreshAssets();
        keywordFinder = new();
        keywordFinder.ReadKeywords(coinClient.Assets.Select(asset => (asset.Symbol, asset.Name)));
    }
    catch (Exception e)
    {
        Log.Write("Could not read assets from coinmarketbase", e);
    }

    DirectoryInfo imagedir = Directory.CreateDirectory(imageDirectory);
    TwitterClient userClient = new(Tokens[TWITTER_CONSUMERKEY],
                                   Tokens[TWITTER_CONSUMERSECRET],
                                   Tokens[TWITTER_ACCESSTOKEN],
                                   Tokens[TWITTER_ACCESSSECRET]);
    IFilteredStream stream = userClient.Streams.CreateFilteredStream();
    DiscordClient discordClient = new(ulong.Parse(Tokens[DISCORD_CHANNELID])); // wont be null since we check for missing variables, line 11ff

    UserWatcher watcher = new(userClient, stream);
    watcher.TweetReceived += async (tweet) =>
    {
        Log.Write(tweet);

        string[]? matches = keywordFinder?.Match(tweet.Text);
        bool writeTweet = matches is null || matches.Any();

        if (writeTweet)
        {
            await discordClient.WriteAsync(tweet);
            if (tweet.ContainsImages)
            {
                var pics = tweet.GetImages();
                await pics.ForEachAsync(pic =>
                {
                    try
                    {
                        string path = pic.Save(imagedir);
                        Log.Write($"Saved picture to {path}");
                    }
                    catch (Exception e)
                    {
                        Log.Write("Error saving picture", e);
                    }
                });
            }
            if (matches is not null && matches.Any())
            {
                await coinClient.RefreshAssets();
                var assetsFound = coinClient.Assets.Where(asset => matches.Contains(asset.Name)).Select(asset => asset.ToString(true));
                await discordClient.WriteAsync($"ALERT!{Environment.NewLine}There are cryptos mentioned in this Tweet:{Environment.NewLine}{string.Join(Environment.NewLine, assetsFound)}");
            }
        }
    };
    bool success = await watcher.AddUser(UsersToFollow.ToArray());

    await discordClient.Connect(Tokens[DISCORD_TOKEN]);
    watcher.StartWatching();

    while (Console.ReadKey().KeyChar != 'q') ;

    watcher.StopWatching();
    await discordClient.Disconnect();
}
catch (Exception e)
{
    Log.Write("Unhandled Exception", e, Log.LogLevel.FTL);
    Environment.Exit(1);
}

Environment.Exit(0);
