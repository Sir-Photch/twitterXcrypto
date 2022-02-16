using System.Text;
using System.Runtime.InteropServices;
using Serilog;
using twitterXcrypto.twitter;

namespace twitterXcrypto.util;

internal static class Log
{
    #region base field
    private static readonly ILogger _logger;
    private const string OUTPUT_TEMPLATE = "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}]{TweetUserName} {Message:lj}{NewLine}{Exception}";
    #endregion

    #region properties
    internal static string? LogPath { get; }
    #endregion

    #region ctor
    static Log()
    {
        DirectoryInfo? logDir = null;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            logDir = Directory.CreateDirectory(@"/var/log/twitterXcrypto/");
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            logDir = new(Environment.CurrentDirectory);
        }
        else
        {
            Console.WriteLine("hehe you are not using supported os");
            Environment.Exit(1);
        }

        LogPath = Path.Combine(logDir.FullName, "twixcry.log");

        _logger = new LoggerConfiguration().MinimumLevel.Verbose()
                                           .WriteTo.Console(outputTemplate: OUTPUT_TEMPLATE)
                                           .WriteTo.File(path: LogPath,
                                                         rollingInterval: RollingInterval.Infinite,
                                                         outputTemplate: OUTPUT_TEMPLATE,
                                                         encoding: Encoding.Unicode,
                                                         shared: true)
                                           .CreateLogger();
    }
    #endregion

    #region enum loglevel
    internal enum Level
    {
        VRB,
        DBG,
        INF,
        WRN,
        ERR,
        FTL
    }
    #endregion

    #region methods
    internal static void Write(string message, Level level = Level.INF)
    {
        switch (level)
        {
            case Level.VRB:
                _logger.Verbose(message);
                break;
            case Level.DBG:
                _logger.Debug(message);
                break;
            case Level.INF:
                _logger.Information(message);
                break;
            case Level.WRN:
                _logger.Warning(message);
                break;
            case Level.ERR:
                _logger.Error(message);
                break;
            case Level.FTL:
                _logger.Fatal(message);
                break;
        }
    }

    internal static void Write(string message, Exception e, Level level = Level.ERR)
    {
        switch (level)
        {
            case Level.VRB:
                _logger.Verbose(e, message);
                break;
            case Level.DBG:
                _logger.Debug(e, message);
                break;
            case Level.INF:
                _logger.Information(e, message);
                break;
            case Level.WRN:
                _logger.Warning(e, message);
                break;
            case Level.ERR:
                _logger.Error(e, message);
                break;
            case Level.FTL:
                _logger.Error(e, message);
                break;
        }
    }

    internal static void Write(Tweet tweet) => _logger.ForContext("TweetUserName", $" [{tweet.User.Name}]:").Verbose(tweet.ToString(replaceLineEndings: true));

    internal static Task WriteAsync(string message, Level level = Level.INF)
        => Task.Run(() => Write(message, level));

    internal static Task WriteAsync(string message, Exception e, Level level = Level.ERR)
        => Task.Run(() => Write(message, e, level));

    #endregion
}
