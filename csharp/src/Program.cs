using System.Text;
using Tweetinvi;
using Tweetinvi.Streaming;
using twitterXcrypto.text;
using twitterXcrypto.util;
using twitterXcrypto.data;
using twitterXcrypto.crypto;
using twitterXcrypto.discord;
using twitterXcrypto.twitter;
using twitterXcrypto.imaging;
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
    await using DiscordClient discordClient = new(ulong.Parse(Tokens[DISCORD_CHANNELID])); // wont be null since we check for missing variables, line 13ff
    await using MySqlClient dbClient = new(Tokens[DATABASE_IP],
                                             int.Parse(Tokens[DATABASE_PORT]),
                                             Tokens[DATABASE_NAME],
                                             Tokens[DATABASE_USER],
                                             Tokens[DATABASE_PWD]);
    OCR.Whitelist = Tokens[TESSERACT_WHITELIST];
    OCR.Locale = Tokens[TESSERACT_LOCALE];
    OCR? ocr = null;
    try
    {
        ocr = await OCR.InitializeAsync();
    }
    catch (Exception e)
    {
        Log.Write("Could not initialize OCR. It will be disabled for this session", e, Log.Level.WRN);
    }
    UserWatcher watcher = new(userClient, stream);
    watcher.TweetReceived += async (tweet) =>
    {
        await dbClient.WriteTweet(tweet);
        IAsyncEnumerable<Image>? pics = tweet.ContainsImages ? tweet.GetImages() : null;

        StringBuilder textToSearch = new(tweet.Text);

        if (pics is not null && ocr is not null && keywordFinder is not null)
            await pics.ForEachAwaitAsync(pic => ocr.GetText(pic)
                                                   .ContinueWith(textGetter =>
                                                   {
                                                       if (textGetter.IsCompletedSuccessfully && !string.IsNullOrWhiteSpace(textGetter.Result))
                                                           lock (textToSearch)
                                                               textToSearch.AppendFormat(" {0} ", textGetter.Result);
                                                   }));

        string[]? textMatches = keywordFinder?.Match(textToSearch.ToString());

        if (textMatches?.Any() ?? true)
        {
            await discordClient.WriteAsync(tweet);
            if (textMatches?.Any() ?? false)
            {
                try
                {
                    await coinClient.RefreshAssets();
                }
                catch (Exception e)
                {
                    Log.Write("Could not read assets from coinmarketbase", e);
                }
                var assetsFound = coinClient.Assets.Where(asset => textMatches.Contains(asset.Name));
                await discordClient.WriteAsync($"ALERT!{Environment.NewLine}There are cryptos mentioned in this Tweet:{Environment.NewLine}{string.Join(Environment.NewLine, assetsFound.Select(asset => asset.ToString(true, 2)))}"); // please lord forgive me for this one-liner

                await dbClient.EnrichTweet(tweet, assetsFound);
                if (pics is not null)
                    await dbClient.EnrichTweet(tweet, pics, imagedir);
            }
        }
    };
    await dbClient.Open();
    await watcher.AddUser(UsersToFollow.ToArray());
    await discordClient.Connect(Tokens[DISCORD_TOKEN]);

    watcher.StartWatching();

    while (Console.ReadKey().KeyChar != 'q') ;

    watcher.StopWatching();
    ocr?.Dispose();
}
catch (Exception e)
{
    Log.Write("Unhandled Exception", e, Log.Level.FTL);
    Environment.Exit(1);
}

Environment.Exit(0);
