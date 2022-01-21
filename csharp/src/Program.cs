using Tweetinvi;
using Tweetinvi.Streaming;
using twitterXcrypto.twitter;
using twitterXcrypto.util;

string[] usersToWatch = { "EmKayMA", "elonmusk", "_christophernst", "iCryptoNetwork", "CryptoBusy" };
string imageDirectory = Path.Combine(Environment.CurrentDirectory, "twixcry_images");

DirectoryInfo imagedir = Directory.CreateDirectory(imageDirectory);
TwitterClient userClient = new(consumerKey: "1HqxAIikTtTkIF2FT1rAu5paw",
                               consumerSecret: "IQ8INgkQshPTvdZPc11tedNOMKjihMdT0V3vnu5WsqF5ZrFpI4",
                               accessToken: "1279713705773674502-B9I0HlC3eC6VvVBsCLsrDKL9xZl7F2",
                               accessSecret: "A0uxTuPai2f8I4p0cYHQQibgSEDRDbl7wfAf83BFX2zNG");
IFilteredStream stream = userClient.Streams.CreateFilteredStream();

UserWatcher watcher = new(userClient, stream);
watcher.TweetReceived += async (tweet) => 
{
    Log.Write($"{tweet}");
    if (tweet.ContainsImages)
    {
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
        }
    }
};
bool success = await watcher.AddUser(usersToWatch);

watcher.StartWatching();

while (Console.ReadKey().KeyChar != 'q') ;

watcher.StopWatching();

Environment.Exit(0);
