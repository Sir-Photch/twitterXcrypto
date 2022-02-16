using Tweetinvi;
using Tweetinvi.Streaming;
using twitterXcrypto.text;
using twitterXcrypto.discord;
using twitterXcrypto.twitter;
using twitterXcrypto.util;
using twitterXcrypto.crypto;
using static twitterXcrypto.util.EnvironmentVariables;
using twitterXcrypto.data;

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
    await using DiscordClient discordClient = new(ulong.Parse(Tokens[DISCORD_CHANNELID])); // wont be null since we check for missing variables, line 11ff
    await using MySqlClient dbClient = new(Tokens[DATABASE_IP],
                                             int.Parse(Tokens[DATABASE_PORT]),
                                             Tokens[DATABASE_NAME],
                                             Tokens[DATABASE_USER],
                                             Tokens[DATABASE_PWD]);
    UserWatcher watcher = new(userClient, stream);
    watcher.TweetReceived += async (tweet) =>
    {
        await dbClient.WriteTweet(tweet);

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
                try
                {
                    await coinClient.RefreshAssets();
                }
                catch (Exception e)
                {
                    Log.Write("Could not read assets from coinmarketbase", e);
                }
                var assetsFound = coinClient.Assets.Where(asset => matches.Contains(asset.Name));
                await discordClient.WriteAsync($"ALERT!{Environment.NewLine}There are cryptos mentioned in this Tweet:{Environment.NewLine}{string.Join(Environment.NewLine, assetsFound.Select(asset => asset.ToString(true, 2)))}"); // please lord forgive me for this one-liner
                await dbClient.EnrichTweet(tweet, assetsFound);
            }
        }
    };
    await dbClient.Open();
    await watcher.AddUser(UsersToFollow.ToArray());
    await discordClient.Connect(Tokens[DISCORD_TOKEN]);
    
    watcher.StartWatching();

    while (Console.ReadKey().KeyChar != 'q') ;

    watcher.StopWatching();
}
catch (Exception e)
{
    Log.Write("Unhandled Exception", e, Log.Level.FTL);
    Environment.Exit(1);
}

Environment.Exit(0);
