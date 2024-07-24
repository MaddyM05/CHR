using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Windows.ApplicationModel;
using Windows.Storage;

namespace Chorus.WindowsReader.Common.Logger
{
    public class ChorusAppLogger<T> : IChorusLogger<T>
    {
        private static ILogger _logger = null;
        private ILogLevel _logLevel = null;
        private static bool isAnalyticsEnabled, isCrashReportEnabled, isLocalFileLogEnabled;
        private static ChorusFileLoggerProvider<T> _loggerProvider;
        private string _logName;

        /// <summary>
        /// This is the InitLocalFileLogger
        /// Initializes a local file logger for the ChorusAppLogger<T> instance.
        /// Sets up a file-based logging provider using ChorusFileLoggerProvider<T> with a specified log file path and log level.
        /// Logs initialization information once the logger is successfully created.
        /// </summary>
        public void InitLocalFileLogger()
        {
            ChorusAppLoggerFactory.SetFileLoggerProvider();
            string packageInstalledLocation = Package.Current.InstalledLocation.Path;
            var localStatePath = ApplicationData.Current.LocalFolder;
            string logFilePath = localStatePath.Path.ToString();
            if (!string.IsNullOrEmpty(logFilePath))
            {
                ChorusAppLoggerFactory.LoggerFactory.AddProvider(new ChorusFileLoggerProvider<T>(logFilePath, true, _logLevel));
                _logger = ChorusAppLoggerFactory.LoggerFactory.CreateLogger<T>();
                _logger.LogInformation("Initializing logger...");
            }
        }
        /// <summary>
        /// This is the ChorusAppLogger 
        /// Initializes a new instance of the ChorusAppLogger class with the specified log name,
        /// logger provider, and log level. If a log level is provided, it sets the logging level
        /// to Information. This constructor prepares the logger for logging operations based on
        /// the provided parameters.
        /// </summary>
        public ChorusAppLogger(string logName, ChorusFileLoggerProvider<T> loggerProvider, ILogLevel logLevel)
        {
            _logLevel = logLevel;

            if (_logLevel != null)
            {
                _logLevel.SetLogLevel(LogLevel.Information);
            }
            _logName = logName;
            _loggerProvider = loggerProvider;
        }
        /// <summary>
        /// This is the ChorusAppLogger
        /// Initializes a new instance of the ChorusAppLogger class with the specified log level.
        /// Sets the logging level to Information if a valid log level is provided. If the logger
        /// factory is not already initialized, it initializes a local file logger. Finally, creates
        /// a logger instance using the ChorusAppLoggerFactory.
        /// </summary>
        /// <param name="logLevel">The log level interface used to control the verbosity of logging.</param>

        public ChorusAppLogger(ILogLevel logLevel)
        {
            _logLevel = logLevel;
            if (_logLevel != null)
            {
                _logLevel.SetLogLevel(LogLevel.Information);
            }
            if (ChorusAppLoggerFactory.LoggerFactory == null)
            {
                InitLocalFileLogger();
            }

            _logger = ChorusAppLoggerFactory.LoggerFactory.CreateLogger<T>();
        }
        /// <summary>
        /// This is the BeginScope
        /// This method is not implemented in ChorusAppLogger<T> and throws a NotImplementedException.
        /// Logging scopes are not supported by this logger implementation.
        /// </summary>
        /// <typeparam name="TState">The type of the state object for the logging scope.</typeparam>
        /// <param name="state">The state object representing the scope.</param>
        /// <returns>Throws NotImplementedException as logging scopes are not supported.</returns>

        public IDisposable BeginScope<TState>(TState state)
        {
            throw new NotImplementedException();
        }
        /// <summary>
        /// This is the IsEnabled
        /// Determines if logging is enabled for the specified log level based on application settings.
        /// Checks the application settings to determine if file logging is enabled globally.
        /// </summary>
        /// <param name="logLevel">The log level to check if logging is enabled.</param>
        /// <returns>True if logging is enabled for the specified log level; otherwise, false.</returns>

        public bool IsEnabled(LogLevel logLevel)
        {
            isLocalFileLogEnabled = GlobalHelper.AppSettings.EnableFileLog;
            return isLocalFileLogEnabled;
        }
        /// <summary>
        /// This is the Log
        /// Logs a message with the specified log level, event ID, state, exception, and formatter function,
        /// if logging is enabled for the specified log level. Constructs a log message including timestamp,
        /// log level, log name, event ID, and message. Optionally includes exception details and custom
        /// parameters for certain exceptions. Writes the constructed log entry using the configured
        /// logger provider.
        /// </summary>
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }

            string message = state != null ? state.ToString() : null;
            var logBuilder = new StringBuilder();

            if (!string.IsNullOrEmpty(message))
            {
                logBuilder.Append(DateTime.Now.ToString("o"));
                logBuilder.Append('\t');
                logBuilder.Append(logLevel);
                logBuilder.Append("\t[");
                logBuilder.Append(_logName);
                logBuilder.Append("]");
                logBuilder.Append("\t[");
                logBuilder.Append(eventId);
                logBuilder.Append("]\t");
                logBuilder.Append(message);
            }

            if (exception != null)
            {
                logBuilder.AppendLine(exception.ToString());

                if (exception.GetType().Equals(typeof(ChorusCustomException)))
                {
                    var ex = exception as ChorusCustomException;
                    if (ex.Parameters != null && ex.Parameters.Any())
                    {
                        var paramValStr = Newtonsoft.Json.JsonConvert.SerializeObject(ex.Parameters);
                        logBuilder.AppendLine(paramValStr);
                    }
                }
            }

            if (_loggerProvider == null)
            {
                string packageInstalledLocation = Package.Current.InstalledLocation.Path;
                var localStatePath = ApplicationData.Current.LocalFolder;
                string logFilePath = localStatePath.Path.ToString();

                if (!string.IsNullOrEmpty(logFilePath))
                {
                    ChorusAppLoggerFactory.LoggerFactory?.AddProvider(new ChorusFileLoggerProvider<T>(logFilePath, true, _logLevel));
                }
            }
            _loggerProvider?.WriteEntry(logBuilder.ToString());
        }
        /// <summary>
        /// This is the AppCenterLogAction
        /// Logs events to App Center Analytics based on the specified log type and dictionary of parameters.
        /// If the log type is LogLevel.Information, LogLevel.Warning, or LogLevel.Error, the method attempts
        /// to track the corresponding event using Analytics.TrackEvent(). Any exceptions during event tracking
        /// are caught and suppressed ("swallowed").
        /// </summary>
        /// <param name="dict">Dictionary containing parameters to be logged with the event.</param>
        /// <param name="logType">Optional log level type (default: LogLevel.Error) for tracking events.</param>
        private void AppCenterLogAction(Dictionary<string, string> dict, LogLevel logType = LogLevel.Error)
        {
            try
            {
                switch (logType)
                {
                    case LogLevel.Debug:
                        break;
                    case LogLevel.Information:
                        //Analytics.TrackEvent($"{LogLevel.Information.ToString()}", dict);
                        break;
                    case LogLevel.Warning:
                        //Analytics.TrackEvent($"{LogLevel.Warning.ToString()}", dict);
                        break;
                    case LogLevel.Error:
                        //Analytics.TrackEvent($"{LogLevel.Error.ToString()}", dict);
                        break;
                    default:
                        break;
                }
            }
            catch
            {
                //swallow
            }
        }
        /// <summary>
        /// This is the LogException
        /// Logs a ChorusCustomException with associated parameters and optional message.
        /// Handles logging of exception details and parameters.
        /// </summary>
        /// <param name="eventType">Type of Chorus event associated with the exception.</param>
        /// <param name="exception">ChorusCustomException object representing the exception.</param>
        /// <param name="message">Optional additional message for the log entry.</param>
        public void LogException(ChorusEventType eventType, ChorusCustomException exception, string message = null)
        {
            if (GlobalHelper.AppSettings.IsLogRequired)
            {
                if (exception == null) return;

                EventId ChorusEvent = new EventId((int)eventType, eventType.ToString());

                if (exception.Parameters == null)
                {
                    exception.Parameters = new Dictionary<string, string> { { "ErrorDesc", exception.Message } };
                }

                string[] param = exception.Parameters.Values.ToArray();
                _logger?.LogError(ChorusEvent, exception, message, param);

                AppCenterLogAction(exception.Parameters);
            }
        }
        /// <summary>
        /// This is the LogException
        /// Logs a ChorusCustomException with associated parameters to the configured logger.
        /// Also logs exception details to App Center Analytics for tracking purposes.
        /// </summary>
        /// <param name="exception">ChorusCustomException object representing the exception to log.</param>

        public void LogException(ChorusCustomException exception)
        {
            if (GlobalHelper.AppSettings.IsLogRequired)
            {
                if (exception == null) return;

                EventId ChorusEvent = new EventId((int)ChorusEventType.ERROR, ChorusEventType.ERROR.ToString());

                if (exception.Parameters == null)
                {
                    exception.Parameters = new Dictionary<string, string> { { "ErrorDesc", exception.Message } };
                }

                _logger?.LogError(ChorusEvent, exception, exception.Message, null);

                AppCenterLogAction(exception.Parameters);
            }
        }
        /// <summary>
        /// This is the LogWarning
        /// Logs a warning message with associated event type, message, and optional exception.
        /// Also logs warning details to App Center Analytics for tracking purposes.
        /// </summary>
        /// <param name="eventType">Type of Chorus event associated with the warning.</param>
        /// <param name="message">Warning message to log.</param>
        /// <param name="exception">Optional ChorusCustomException associated with the warning.</param>
        public void LogWarning(ChorusEventType eventType, string message, ChorusCustomException exception)
        {
            if (message == null && exception == null) return;

            EventId ChorusEvent = new EventId((int)eventType, eventType.ToString());
            Dictionary<string, string> dict = null;

            if (message == null)
            {
                dict = exception.Parameters ?? new Dictionary<string, string> { { "ErrorDesc", exception.Message } };
            }
            else if (exception == null)
            {
                dict = new Dictionary<string, string> { { "ErrorDesc", message } };
            }

            if (dict != null)
            {
                string[] param = dict.Values.ToArray();
                _logger?.LogWarning(ChorusEvent, exception, message, param);
            }
            else _logger?.LogInformation(message);

            AppCenterLogAction(dict, LogLevel.Warning);
        }
        /// <summary>
        /// This is the LogInfo
        /// Logs an informational message with optional parameters.
        /// Also logs information to App Center Analytics for tracking purposes.
        /// </summary>
        /// <param name="message">Informational message to log.</param>
        /// <param name="dict">Optional dictionary of parameters associated with the message.</param>

        public void LogInfo(string message, Dictionary<string, string> dict = null)
        {
            if (dict != null && dict.Count > 0)
            {
                string[] param = dict.Values.ToArray();
                var logBuilder = new StringBuilder();
                logBuilder.Append(message);

                foreach (var item in param)
                {
                    logBuilder.Append('\n');
                    logBuilder.Append(string.IsNullOrEmpty(item) == true ? string.Empty : item);
                }

                _logger?.LogInformation(logBuilder.ToString());
            }
            else _logger?.LogInformation(message);

            AppCenterLogAction(dict, LogLevel.Information);
        }
    }
}