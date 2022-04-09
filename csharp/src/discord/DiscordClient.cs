using Discord;
using Discord.WebSocket;
using twitterXcrypto.twitter;
using twitterXcrypto.util;

namespace twitterXcrypto.discord;

internal class DiscordClient : IAsyncDisposable, IDisposable
{
    private readonly DiscordSocketClient _client;
    private readonly ulong _channelId;
    private IMessageChannel? _channel;

    internal DiscordClient(ulong channelId)
    {
        _channelId = channelId;

        //DiscordSocketConfig cfg = new() 
        //{ 
        //    GatewayIntents = GatewayIntents.AllUnprivileged | ~GatewayIntents.GuildScheduledEvents | ~GatewayIntents.GuildInvites
        //};

        _client = new DiscordSocketClient(/*cfg*/);
        _client.Log += msg => Log.WriteAsync(msg.Message, msg.Exception, ToLogLevel(msg.Severity));
        _client.Connected += () => Log.WriteAsync("Connected to Discord!");
        _client.Disconnected += ex => Log.WriteAsync("Disconnected from Discord!", ex, ex is null or TaskCanceledException ? Log.Level.INF : Log.Level.ERR);
        _client.Ready += () => Log.WriteAsync("Discord-Bot ready!");
    }

    internal async Task SetBotStatusAsync(IBotStatus botStatus)
    {
        try
        {
            await _client.SetStatusAsync(botStatus.UserStatus);
            await _client.SetActivityAsync(botStatus);
        }
        catch (Exception e)
        {
            await Log.WriteAsync("Could not update bot-status!", e);
        }
    }

    internal async Task ConnectAsync(string token)
    {
        await _client.LoginAsync(TokenType.Bot, token);
        await _client.StartAsync();
        _channel = (IMessageChannel)await _client.GetChannelAsync(_channelId);
    }

    internal IDisposable EnterTypingState() => _channel?.EnterTypingState() ?? throw new InvalidOperationException("Not connected");

    internal Task WriteAsync(string message) => _channel?.SendMessageAsync(message) ?? throw new InvalidOperationException("Not connected");

    internal async Task WriteAsync(Tweet tweet)
    {
        if (_channel is null)
            throw new InvalidOperationException("Not connected");

        await _channel.SendMessageAsync(tweet.ToString(prependUser: true,
                                                      replaceLineEndings: true,
                                                      lineEndingReplacement: Environment.NewLine));

        if (tweet.ContainsImages)
        {
            await tweet.GetImagesAsync().ForEachAsync(async img =>
            {
                using MemoryStream ms = new();
                await img.SaveAsync(ms);
                ms.Position = 0L;
                await _channel.SendFileAsync(ms, img.Name);
            });
        }
    }

    #region IDisposable
    private bool _disposed = false;

    public async ValueTask DisposeAsync()
    {
        await DisposeCoreAsync().ConfigureAwait(false);
        Dispose(false);
        GC.SuppressFinalize(this);
    }

    protected virtual async ValueTask DisposeCoreAsync()
    {
        if (_disposed) return;

        if (_client is not null)
        {
            await _client.StopAsync().ConfigureAwait(false);
            await _client.LogoutAsync().ConfigureAwait(false);
            await _client.DisposeAsync().ConfigureAwait(false);
        }
    }

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
