using Tweetinvi;
using Tweetinvi.Models;
using Tweetinvi.Streaming;
using twitterXcrypto.util;

TwitterClient userClient = new(consumerKey: "1HqxAIikTtTkIF2FT1rAu5paw",
                               consumerSecret: "IQ8INgkQshPTvdZPc11tedNOMKjihMdT0V3vnu5WsqF5ZrFpI4",
                               accessToken: "1279713705773674502-B9I0HlC3eC6VvVBsCLsrDKL9xZl7F2",
                               accessSecret: "A0uxTuPai2f8I4p0cYHQQibgSEDRDbl7wfAf83BFX2zNG");

IUser musk = await userClient.Users.GetUserAsync("EmKayMA");

IFilteredStream stream = userClient.Streams.CreateFilteredStream();
stream.AddFollow(musk.Id, tweet =>
{
    Log.Write($"Musk has spoken! {tweet.Text}");
});
await stream.StartMatchingAnyConditionAsync(); // stops here

while (Console.ReadKey().KeyChar != 'q') ;

Environment.Exit(0);

