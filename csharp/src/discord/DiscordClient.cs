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
        await _client.SetStatusAsync(botStatus.UserStatus);
        await _client.SetActivityAsync(botStatus);
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
                img.Save(ms);
                ms.Position = 0L;
                await _channel.SendFileAsync(ms, img.Name);
            });
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _client.StopAsync();
        await _client.LogoutAsync();
        await _client.DisposeAsync();
        GC.SuppressFinalize(this);
    }

    public void Dispose()
    {
        _client.Dispose();
        GC.SuppressFinalize(this);
    }

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
