using Discord;
using Discord.WebSocket;
using twitterXcrypto.util;

namespace twitterXcrypto.discord;

internal class DiscordClient
{
    private readonly DiscordSocketClient _client;
    private readonly ulong _channelId;

    internal DiscordClient(ulong channelId)
    {
        _client = new DiscordSocketClient();
        _client.Log += msg => Task.Run(() => Log.Write(msg.Message, msg.Exception, ToLogLevel(msg.Severity)));
        _client.Connected += () => Log.WriteAsync("Connected to Discord!");
        _client.Disconnected += ex => Log.WriteAsync("Disconnected from Discord!", ex, ex is null ? Log.LogLevel.INF : Log.LogLevel.ERR);
        _client.Ready += () => Log.WriteAsync("Discord-Bot ready!");
    }

    public async Task Connect(string token)
    {
        await _client.LoginAsync(TokenType.Bot, token);
        await _client.StartAsync();
    }

    public async Task WriteAsync(string message)
    {
        ISocketMessageChannel channel = (ISocketMessageChannel)await _client.GetChannelAsync(_channelId);
        await channel.SendMessageAsync(message);
    }

    public async Task WriteAsync(string message, imaging.Image image)
    {
        ISocketMessageChannel channel = (ISocketMessageChannel)await _client.GetChannelAsync(_channelId);

        using MemoryStream ms = new();
        image.Save(ms);
        await channel.SendFileAsync(ms, image.Name, message);
    }

    public async Task Disconnect()
    {
        await _client.StopAsync();
        await _client.LogoutAsync();
    }

    private static Log.LogLevel ToLogLevel(LogSeverity discordSeverity) => discordSeverity switch
    {
        LogSeverity.Verbose => Log.LogLevel.VRB,
        LogSeverity.Debug => Log.LogLevel.DBG,
        LogSeverity.Info => Log.LogLevel.INF,
        LogSeverity.Warning => Log.LogLevel.WRN,
        LogSeverity.Error => Log.LogLevel.ERR,
        LogSeverity.Critical => Log.LogLevel.FTL,
        _ => throw new NotImplementedException(discordSeverity.ToString())
    };
}
