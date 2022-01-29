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
    string imageDirectory = Path.Combine(Environment.CurrentDirectory, "twixcry_images");

    if (Tokens.Values.Any(x => x is null) || UsersToFollow is null)
    {
        Log.Write($"Environment-variables are not configured:" +
            $"{Environment.NewLine}\t" +
            $"{string.Join(Environment.NewLine + "\t", Tokens.Where(tkn => tkn.Value is null).Select(tkn => tkn.Key))}" +
            $"{(UsersToFollow is null ? Environment.NewLine + USERS_TO_FOLLOW : string.Empty)}");
        Environment.Exit(1);
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
