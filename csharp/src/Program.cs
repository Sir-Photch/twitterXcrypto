using Tweetinvi;
using Tweetinvi.Streaming;
using twitterXcrypto.discord;
using twitterXcrypto.twitter;
using twitterXcrypto.util;
using static twitterXcrypto.util.EnvironmentVariables;

string[] usersToWatch = { "EmKayMA", "elonmusk", "_christophernst", "iCryptoNetwork", "CryptoBusy" };
string imageDirectory = Path.Combine(Environment.CurrentDirectory, "twixcry_images");

if (Tokens.Values.Any(x => x is null))
{
    Log.Write($"Environment-variables are not configured:{Environment.NewLine}\t{string.Join(Environment.NewLine + "\t", Tokens.Where(tkn => tkn.Value is null).Select(tkn => tkn.Key))}");
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
    Log.Write($"{tweet}");
    if (tweet.User.Name == "Elon Musk")
    {
        try
        {
            await discordClient.WriteAsync("Musk has spoken\n" + tweet.Text);
        }
        catch (Exception e)
        {
            Log.Write("Could not write message to discord", e);
        }
    }
    if (tweet.ContainsImages)
    {
        int pictureIndex = 1;
        await foreach (var pic in tweet.GetImages())
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
            if (tweet.User.Name == "Elon Musk")
            {
                try
                {
                    await discordClient.WriteAsync($"Pic {pictureIndex++}", pic);
                }
                catch (Exception e)
                {
                    Log.Write("Could not write picture to discord", e);
                }
            }
        }
    }
};
bool success = await watcher.AddUser(usersToWatch);

await discordClient.Connect(Tokens[DISCORD_TOKEN]);
watcher.StartWatching();

while (Console.ReadKey().KeyChar != 'q') ;

watcher.StopWatching();
await discordClient.Disconnect();

Environment.Exit(0);
