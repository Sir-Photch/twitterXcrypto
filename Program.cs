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

// important stuff
IWebhook webhook = await client.AccountActivity.CreateAccountActivityWebhookAsync("foo", "bar");

await server.WaitUntilDisposed();
Environment.Exit(0);



