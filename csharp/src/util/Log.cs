using System.Runtime.InteropServices;
using Serilog;

namespace twitterXcrypto.util
{
    internal static class Log
    {
        #region base field
        private static readonly ILogger _logger;
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

            _logger = new LoggerConfiguration().WriteTo.Console()
                                               .WriteTo.File(path: LogPath,
                                                             rollingInterval: RollingInterval.Infinite,
                                                             outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
                                               .CreateLogger();
        }
        #endregion

        #region enum loglevel
        public enum LogLevel
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
        public static void Write(string message, LogLevel level = LogLevel.INF)
        {
            switch (level)
            {
                case LogLevel.VRB:
                    _logger.Verbose(message);
                    break;
                case LogLevel.DBG:
                    _logger.Debug(message);
                    break;
                case LogLevel.INF:
                    _logger.Information(message);
                    break;
                case LogLevel.WRN:
                    _logger.Warning(message);
                    break;
                case LogLevel.ERR:
                    _logger.Error(message);
                    break;
                case LogLevel.FTL:
                    _logger.Fatal(message);
                    break;
            }
        }

        public static void Write(string message, Exception e, LogLevel level = LogLevel.ERR)
        {
            switch (level)
            {
                case LogLevel.VRB:
                    _logger.Verbose(e, message);
                    break;
                case LogLevel.DBG:
                    _logger.Debug(e, message);
                    break;
                case LogLevel.INF:
                    _logger.Information(e, message);
                    break;
                case LogLevel.WRN:
                    _logger.Warning(e, message);
                    break;
                case LogLevel.ERR:
                    _logger.Error(e, message);
                    break;
                case LogLevel.FTL:
                    _logger.Error(e, message);
                    break;
            }
        }

        public static Task WriteAsync(string message, LogLevel level = LogLevel.INF)
            => Task.Run(() => Write(message, level));

        public static Task WriteAsync(string message, Exception e, LogLevel level = LogLevel.ERR)
            => Task.Run(() => Write(message, e, level));
        #endregion
    }
}
