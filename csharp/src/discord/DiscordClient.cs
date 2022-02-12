using Discord;
using Discord.WebSocket;
using twitterXcrypto.twitter;
using twitterXcrypto.util;

namespace twitterXcrypto.discord;

internal class DiscordClient : IAsyncDisposable, IDisposable
{
    private readonly DiscordSocketClient _client;
    private readonly ulong _channelId;

    internal DiscordClient(ulong channelId)
    {
        _channelId = channelId;
        _client = new DiscordSocketClient();
        _client.Log += msg => Log.WriteAsync(msg.Message, msg.Exception, ToLogLevel(msg.Severity));
        _client.Connected += () => Log.WriteAsync("Connected to Discord!");
        _client.Disconnected += ex => Log.WriteAsync("Disconnected from Discord!", ex, ex is null ? Log.Level.INF : Log.Level.ERR);
        _client.Ready += () => Log.WriteAsync("Discord-Bot ready!");
    }

    internal async Task Connect(string token)
    {
        await _client.LoginAsync(TokenType.Bot, token);
        await _client.StartAsync();
    }

    internal async Task WriteAsync(string message)
    {
        ISocketMessageChannel channel = (ISocketMessageChannel)await _client.GetChannelAsync(_channelId);
        await channel.SendMessageAsync(message);
    }

    internal async Task WriteAsync(Tweet tweet)
    {
        ISocketMessageChannel channel = (ISocketMessageChannel)await _client.GetChannelAsync(_channelId);
        await channel.SendMessageAsync(
            tweet.ToString(
                prependUser: true, 
                replaceLineEndings: true, 
                lineEndingReplacement: Environment.NewLine));

        if (tweet.ContainsImages)
        {
            await tweet.GetImages().ForEachAsync(async img =>
            {
                using MemoryStream ms = new();
                img.Save(ms);
                ms.Position = 0L;
                await channel.SendFileAsync(ms, img.Name);
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
