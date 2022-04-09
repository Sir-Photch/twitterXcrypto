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
    var keywordInitializer = coinClient.RefreshAssetsAsync().ContinueWith(coinTask =>
    {
        if (coinTask.Exception is not null)
        {
            Log.Write("Could not read assets from coinbase!", coinTask.Exception);
            return;
        }

        try
        {
            keywordFinder = new(coinClient.Assets.Select(asset => (asset.Symbol, asset.Name)));
        }
        catch (Exception e)
        {
            Log.Write("Regex error in keyword finder!", e);
        }
    }, TaskScheduler.Default);

    DirectoryInfo imagedir = Directory.CreateDirectory(imageDirectory);

    TwitterClient userClient = new(Tokens[TWITTER_CONSUMERKEY],
                                   Tokens[TWITTER_CONSUMERSECRET],
                                   Tokens[TWITTER_ACCESSTOKEN],
                                   Tokens[TWITTER_ACCESSSECRET]);

    IBotStatus statusWatching = new TwitterStatus
    {
        Name = $"Stalking crypto influencers {char.ConvertFromUtf32(0x1F440)}", // eye emoji
        Details = string.Empty,
        UserStatus = Discord.UserStatus.Online
    };
    IBotStatus statusProblem = new TwitterStatus
    {
        Name = "Houston we have a problem",
        Details = string.Empty,
        UserStatus = Discord.UserStatus.DoNotDisturb
    };
    await using DiscordClient discordClient = new(ulong.Parse(Tokens[DISCORD_CHANNELID]));

    await using MySqlClient? dbClient = new(Tokens[DATABASE_IP],
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

    UserWatcher watcher = new(userClient);
    watcher.Connected += async () => await discordClient.SetBotStatusAsync(statusWatching);
    watcher.DisconnectTimeout += async () =>
    {
        try 
        {
            await discordClient.SetBotStatusAsync(statusProblem);
        }
        catch (Exception e)
        {
            Log.Write("Could not update bot status!", e);
        }
    };

    Task dbWriter = Task.CompletedTask, discordWriter = Task.CompletedTask;
    watcher.TweetReceived += async (tweet) =>
    {
#pragma warning disable VSTHRD003 // thread is not started outside of lambda context
        await dbWriter;
#pragma warning restore VSTHRD003
        dbWriter = dbClient.WriteTweetAsync(tweet);
        IAsyncEnumerable<Image>? pics = tweet.ContainsImages ? tweet.GetImagesAsync() : null;

        StringBuilder textToSearch = new(tweet.Text);

        if (pics is not null && ocr is not null && keywordFinder is not null)
            await pics.ForEachAwaitAsync(pic => ocr.GetTextAsync(pic)
                                                   .ContinueWith(textGetter =>
                                                   {
                                                       if (textGetter.IsCompletedSuccessfully && !string.IsNullOrWhiteSpace(textGetter.Result))
                                                           lock (textToSearch)
                                                               textToSearch.AppendFormat(" {0} ", textGetter.Result);
                                                   }, TaskScheduler.Default));

        var textMatches = keywordFinder?.Match(textToSearch.ToString());
        if (textMatches?.Any() ?? true)
        {
            using var typingState = discordClient.EnterTypingState();
            await discordWriter;
            discordWriter = discordClient.WriteAsync(tweet);
            if (textMatches?.Any() ?? false)
            {
                try
                {
                    await coinClient.RefreshAssetsAsync();
                }
                catch (Exception e)
                {
                    Log.Write("Could not read assets from coinmarketbase", e);
                }
                var assetsFound = coinClient.Assets.Where(asset => textMatches.Contains(asset.Name) || textMatches.Contains(asset.Symbol));
                await discordWriter;
                discordWriter = discordClient.WriteAsync($"ALERT!{Environment.NewLine}There are cryptos mentioned in this Tweet:{Environment.NewLine}{string.Join(Environment.NewLine, assetsFound.Select(asset => asset.ToString(true, 3)))}"); // please lord forgive me for this one-liner

                await dbWriter;
                await dbClient.EnrichTweetAsync(tweet, assetsFound);
                if (pics is not null)
                    dbWriter = dbClient.EnrichTweetAsync(tweet, pics, imagedir);
            }
        }
    };
    await dbClient.OpenAsync();
    await watcher.AddUserAsync(UsersToFollow.ToArray());
    await discordClient.ConnectAsync(Tokens[DISCORD_TOKEN]);
    await keywordInitializer;

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
