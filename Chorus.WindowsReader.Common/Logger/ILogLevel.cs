using Microsoft.Extensions.Logging;

namespace Chorus.WindowsReader.Common.Logger
{
    /// <summary>
    ///  This is the ILogLevel
    /// Interface for managing log levels.
    /// </summary>
    public interface ILogLevel
    {
        LogLevel Level { get; }
        void SetLogLevel(LogLevel level);
    }
}