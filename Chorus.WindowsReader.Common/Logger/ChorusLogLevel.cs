using Microsoft.Extensions.Logging;

namespace Chorus.WindowsReader.Common.Logger
{
    public class ChorusLogLevel : ILogLevel
    {

        private LogLevel _logLevel;

        /// <summary>
        /// This is the ChorusLogLevel
        /// Initializes a new instance of the ChorusLogLevel class with the default log level set to Information.
        /// </summary>
        public ChorusLogLevel()
        {
            _logLevel = LogLevel.Information;
        }
        /// <summary>
        /// This is the ChorusLogLevel
        /// Initializes a new instance of the ChorusLogLevel class with a specified log level.
        /// </summary>
        /// <param name="level">The log level to be set.</param>
        public ChorusLogLevel(LogLevel level)
        {
            _logLevel = level;
        }
        /// <summary>
        /// This is the Level
        /// Gets the current log level stored in the Level property.
        /// </summary>
        public LogLevel Level
        {
            get
            {
                return _logLevel;
            }
        }
        /// <summary>
        ///  This is the SetLogLevel
        /// Sets the log level to the specified LogLevel.
        /// </summary>
        public void SetLogLevel(LogLevel level)
        {
            _logLevel = level;
        }
    }
}
