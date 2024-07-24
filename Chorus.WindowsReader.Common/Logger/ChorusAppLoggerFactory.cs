using Microsoft.Extensions.Logging;

namespace Chorus.WindowsReader.Common.Logger
{
    public class ChorusAppLoggerFactory
    {
        private static ILoggerFactory _loggerFactory = null;

        /// <summary>
        /// This is the LoggerFactory
        /// Gets or sets the ILoggerFactory instance used for creating loggers.
        /// </summary>
        public static ILoggerFactory LoggerFactory
        {
            get => _loggerFactory;
            set => _loggerFactory = value;
        }
        /// <summary>
        /// This is the SetFileLoggerProvider
        /// Gets the ILoggerFactory instance used for creating loggers. If not already initialized,
        /// creates a new instance of LoggerFactory and returns it.
        /// </summary>
        /// <returns>The ILoggerFactory instance used for logging operations.</returns>
        public static ILoggerFactory SetFileLoggerProvider()
        {
            if (LoggerFactory == null)
            {
                _loggerFactory = new LoggerFactory();
            }

            return _loggerFactory;
        }
    }

}
