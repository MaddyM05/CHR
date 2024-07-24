using Microsoft.Extensions.Logging;
using System.Collections.Generic;

namespace Chorus.WindowsReader.Common.Logger
{
    public interface IChorusLogger<T> : ILogger
    {
        /// <summary>
        ///  This is the LogException
        /// Logs an exception with the specified event type, exception details, and optional message.
        /// </summary>
        /// <param name="eventType">The type of the event (error or warning).</param>
        /// <param name="exception">The exception details to log.</param>
        /// <param name="message">An optional message to include in the log.</param>
        void LogException(ChorusEventType eventType, ChorusCustomException exception, string message = null);

        /// <summary>
        ///  This is the LogException
        /// Logs the specified custom exception.
        /// </summary>
        /// <param name="exception">The custom exception to log.</param>
        void LogException(ChorusCustomException exception);

        /// <summary>
        ///  This is the LogInfo
        /// Logs informational messages with optional additional parameters.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="dict">Optional dictionary of additional parameters.</param>
        void LogInfo(string message, Dictionary<string, string> dict = null);

        /// <summary>
        /// This is the LogWarning
        /// Logs a warning message with optional associated event type and exception.
        /// </summary>
        void LogWarning(ChorusEventType eventType, string message, ChorusCustomException exception);
    }
}
