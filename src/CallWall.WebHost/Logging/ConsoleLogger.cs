using System;
using CallWall.Web;

namespace CallWall.WebHost.Logging
{
    public sealed class ConsoleLogger : ILogger
    {
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
            if (exception == null)
                Console.WriteLine("{0} - {1}", level, message);
            else
            {
                Console.WriteLine("{0} - {1}", level, message);
                Console.WriteLine(exception);
            }

        }
    }
}