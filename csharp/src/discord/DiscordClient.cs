using Discord;
using Discord.Webhook;
using twitterXcrypto.twitter;
using twitterXcrypto.util;

namespace twitterXcrypto.discord;

internal class DiscordClient : IDisposable
{
    private readonly DiscordWebhookClient _client;
    private readonly ulong _channelId;
    private readonly SemaphoreSlim _botStatusSemaphore = new(1);

    internal DiscordClient(string webhookUrl)
    {
        _client = new DiscordWebhookClient(webhookUrl);

        _client.Log += msg => Log.WriteAsync(msg.Message, msg.Exception, ToLogLevel(msg.Severity));
    }

    internal async Task WriteAsync(string text) =>
        await _client.SendMessageAsync(text);

    internal async Task WriteAsync(Tweet tweet)
    {

        await _client.SendMessageAsync(tweet.ToString(prependUser: true,
                                                      replaceLineEndings: true,
                                                      lineEndingReplacement: Environment.NewLine));

        if (tweet.ContainsImages)
        {
            var images = await tweet.GetImagesAsync().Select(image =>
            {
                MemoryStream ms = new();
                image.Save(ms);
                return new FileAttachment(ms, image.Name, isSpoiler: true);
            }).ToArrayAsync();

            await _client.SendFilesAsync(images, "Images in tweet");
        }
    }

    #region IDisposable
    private bool _disposed = false;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;

        if (disposing)
        {
            _client.Dispose();
        }

        _disposed = true;
    }

    ~DiscordClient()
    {
        Dispose(false);
    }
    #endregion

    private static Log.Level ToLogLevel(LogSeverity discordSeverity) => discordSeverity switch
    {
        LogSeverity.Verbose => Log.Level.VRB,
        LogSeverity.Debug => Log.Level.DBG,
        LogSeverity.Info => Log.Level.INF,
        LogSeverity.Warning => Log.Level.WRN,
        LogSeverity.Error => Log.Level.ERR,
        LogSeverity.Critical => Log.Level.FTL,
        _ => throw new NotImplementedException(discordSeverity.ToString())
    };
}
