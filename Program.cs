using Tweetinvi;
using Tweetinvi.AspNet;
using Tweetinvi.Models;
using twitterXcrypto.util;

Plugins.Add<AspNetPlugin>();

var client = new TwitterClient(consumerKey: "1HqxAIikTtTkIF2FT1rAu5paw",
                               consumerSecret: "IQ8INgkQshPTvdZPc11tedNOMKjihMdT0V3vnu5WsqF5ZrFpI4",
                               bearerToken: "AAAAAAAAAAAAAAAAAAAAAJxOYQEAAAAACSMLy7fNoyv8ybUIGpKN6tpghoA%3DHqbB1VUNkQNb9eH4Nfe6UDCzK5W7oW9UObF6ah7IiXcjGYwnT0");
var requestHandler = client.AccountActivity.CreateRequestHandler();
var server = new SimpleHttpServer(8042);
server.OnRequest += async (sender, context) =>
{
    Log.Write($"Incoming Request from {context.Request.UserHostAddress}; Name: {context.Request.UserHostName}");
    var webhookRequest = WebhookRequestFactory.Create(context);

    if (await requestHandler.IsRequestManagedByTweetinviAsync(webhookRequest))
    {
        if (await requestHandler.TryRouteRequestAsync(webhookRequest))
        {
            Log.Write("Handled Twitter-request sucessfully!");
        }
        else
        {
            Log.Write("Failed handling Twitter-request. Exiting...", Log.LogLevel.FTL);
            Environment.Exit(1);
        }
    }
    else
    {
        Log.Write("Request was not from twitter!", Log.LogLevel.WRN);
        StreamWriter sw = new(context.Response.OutputStream);
        await sw.WriteAsync("Get out!!");
        await sw.FlushAsync();
        context.Response.StatusCode = 401;
        context.Response.Close();
    }
};

server.Start();
IWebhook webhook = await client.AccountActivity.CreateAccountActivityWebhookAsync("sandbox", "https://e0ae-188-192-200-126.ngrok.io");

await client.AccountActivity.SubscribeToAccountActivityAsync("sandbox");
var environmentState = await client.AccountActivity.GetAccountActivitySubscriptionsAsync("sandbox");
long uid = long.Parse(environmentState.Subscriptions.First().UserId);
var accountActivityStream = requestHandler.GetAccountActivityStream(uid, "sandbox");
accountActivityStream.TweetCreated += (sender, tweetCreatedEvent) =>
{
    Log.Write($"A tweet was created by {tweetCreatedEvent.Tweet.CreatedBy.Name}: {tweetCreatedEvent.Tweet.Text}");
};

// wait for user-abort with 'q'
while (Console.ReadKey().KeyChar != 'q') ;

// cleanup
var webhooks = await client.AccountActivity.GetAccountActivityEnvironmentWebhooksAsync("sandbox");
foreach (var hook in webhooks)
{
    await client.AccountActivity.DeleteAccountActivityWebhookAsync("sandbox", hook);
}

server.Stop();
Environment.Exit(0);
