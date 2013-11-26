using System;
using CallWall.Web;
using log4net;
using log4net.Core;

namespace CallWall.WebHost.Logging
{
    /// <summary>
    /// Implementation of the CallWall <see cref="ILogger"/> interface.
    /// </summary>
    public sealed class Log4NetLogger : CallWall.Web.ILogger
    {
        private readonly ILog _log;

        static Log4NetLogger()
        {
            log4net.Config.XmlConfigurator.Configure();
        }

        public Log4NetLogger(Type callingType)
        {
            if (callingType == null) throw new ArgumentNullException("callingType");
            _log = LogManager.GetLogger(callingType.Name);
        }

        /// <summary>
        /// Writes the message and optional exception to the log for the given level.
        /// </summary>
        /// <param name="level">The Logging level to apply. Useful for filtering messages</param>
        /// <param name="message">The message to be logged</param>
        /// <param name="exception">An optional exception to be logged with the message</param>
        /// <remarks>
        /// It is preferable to use the <see cref="log4net.Core.ILogger"/> extension methods found in the <see cref="Web.LoggerExtensions"/> static type.
        /// </remarks>
        public void Write(LogLevel level, string message, Exception exception)
        {
            _log.Logger.Log(null, ToLog4Net(level), message, exception);
        }

        private static Level ToLog4Net(LogLevel level)
        {
            switch (level)
            {
                case LogLevel.Verbose:
                    return Level.Verbose;
                case LogLevel.Trace:
                    return Level.Trace;
                case LogLevel.Debug:
                    return Level.Debug;
                case LogLevel.Warn:
                    return Level.Warn;
                case LogLevel.Info:
                    return Level.Info;
                case LogLevel.Error:
                    return Level.Error;
                case LogLevel.Fatal:
                    return Level.Fatal;
                default:
                    throw new ArgumentOutOfRangeException("level");
            }
        }
    }
}
