using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace Chorus.WindowsReader.Common.Logger
{
    public class ChorusFileLoggerProvider<T> : ILoggerProvider
    {
        public string LogFileName { get; set; }
        public bool Append { get; set; } = true;
        private ILogLevel _logLevel;

        public Func<string, string> FormatLogFileName { get; set; }

        private readonly ConcurrentDictionary<string, ChorusAppLogger<T>> _loggers =
            new ConcurrentDictionary<string, ChorusAppLogger<T>>();
        private readonly BlockingCollection<string> _entryQueue = new BlockingCollection<string>(1024);
        private readonly Task _processQueueTask;
        private readonly ChorusFileWriter<T> _fWriter;
        private long _fileSizeLimitBytes = 2097152;
        private int _maxRollingFiles = 1;

        /// <summary>
        /// This is the ChorusFileLoggerProvider
        /// Initializes a new instance of the ChorusFileLoggerProvider class with the specified
        /// file name and log level, using default settings for file size limit and maximum rolling files.
        /// </summary>
        /// <param name="fileName">The name of the log file.</param>
        /// <param name="logLevel">The log level interface used to control the verbosity of logging.</param>
        public ChorusFileLoggerProvider(string fileName, ILogLevel logLevel) : this(fileName, true, logLevel)
        {
            _fileSizeLimitBytes = GlobalHelper.AppSettings.FileSizeLimitBytes;
            _maxRollingFiles = GlobalHelper.AppSettings.MaxRollingFiles;
        }
        /// <summary>
        /// This is the ChorusFileLoggerProvider
        /// Initializes a new instance of the ChorusFileLoggerProvider class with the specified
        /// file name, append mode, and log level. Configures the log file name with today's date,
        /// sets logging options, initializes a file writer, and starts a background task for
        /// processing log entries.
        /// </summary>
        /// <param name="fileName">The base name of the log file.</param>
        /// <param name="append">True to append to an existing log file, false to overwrite.</param>
        /// <param name="logLevel">The log level interface used to control the verbosity of logging.</param>

        public ChorusFileLoggerProvider(string fileName, bool append, ILogLevel logLevel)
        {
            // Ensure log file name includes today's date
            LogFileName = $"{fileName}-{DateTime.UtcNow:yyyy-MM-dd}";
            Append = append;
            _logLevel = logLevel;
            _fileSizeLimitBytes = GlobalHelper.AppSettings.FileSizeLimitBytes;
            _maxRollingFiles = GlobalHelper.AppSettings.MaxRollingFiles;

            _fWriter = new ChorusFileWriter<T>(this);
            _processQueueTask = Task.Factory.StartNew(
                ProcessQueue,
                this,
                TaskCreationOptions.LongRunning);
        }
        /// <summary>
        /// This is the FileSizeLimitBytes
        /// Gets or sets the maximum size in bytes that a log file can reach before it is rolled over or truncated.
        /// </summary>
        public long FileSizeLimitBytes
        {
            get => _fileSizeLimitBytes;
            set => _fileSizeLimitBytes = value;
        }
        /// <summary>
        /// This is the MaxRollingFiles
        /// Gets or sets the maximum number of log files to retain when rolling over logs.
        /// </summary>
        public int MaxRollingFiles
        {
            get => _maxRollingFiles;
            set => _maxRollingFiles = value;
        }
        /// <summary>
        /// This is the CreateLogger
        /// Creates a logger instance for the specified category name if it doesn't already exist,
        /// using a factory method to generate new loggers as needed.
        /// </summary>
        /// <param name="categoryName">The name of the category for which a logger is requested.</param>
        /// <returns>An ILogger instance associated with the specified category.</returns>
        public ILogger CreateLogger(string categoryName)
        {
            return _loggers.GetOrAdd(categoryName, CreateLoggerImplementation);
        }
        /// <summary>
        /// This is the Dispose
        /// Disposes of resources used by the ChorusFileLoggerProvider instance.
        /// Completes the entry queue, waits for the processing task to complete within a specified timeout,
        /// clears cached loggers, and closes the file writer.
        /// </summary>
        public void Dispose()
        {
            _entryQueue.CompleteAdding();
            try
            {
                int timeoutLimit = 1500;
                timeoutLimit = GlobalHelper.AppSettings.ProcessTaskTimeout;

                _processQueueTask.Wait(timeoutLimit);  // the same as in ConsoleLogger
            }
            catch (TaskCanceledException) { }
            catch (AggregateException ex) when (ex.InnerExceptions.Count == 1 && ex.InnerExceptions[0] is TaskCanceledException) { }

            _loggers.Clear();
            _fWriter.Close();
        }
        /// <summary>
        /// This is the ChorusAppLogger
        /// Creates a new ChorusAppLogger instance for the specified logger name.
        /// </summary>
        /// <param name="name">The name of the logger.</param>
        /// <returns>A new ChorusAppLogger instance.</returns>
        private ChorusAppLogger<T> CreateLoggerImplementation(string name)
        {
            return new ChorusAppLogger<T>(name, this, _logLevel);
        }
        /// <summary>
        /// This is the WriteEntry
        /// Writes a log message to the entry queue if it is not completed.
        /// </summary>
        /// <param name="message">The log message to be written.</param>
        internal void WriteEntry(string message)
        {
            if (!_entryQueue.IsAddingCompleted)
            {
                try
                {
                    _entryQueue.Add(message);
                    return;
                }
                catch (InvalidOperationException) { }
            }
        }
        /// <summary>
        /// This is the ProcessQueue
        /// Processes messages from the entry queue by consuming each message and writing it to the file writer.
        /// </summary>
        private void ProcessQueue()
        {
            foreach (var message in _entryQueue.GetConsumingEnumerable())
            {
                _fWriter.WriteMessage(message, _entryQueue.Count == 0);
            }
        }
        /// <summary>
        /// This is the ProcessQueue
        /// Callback method to invoke the processing of log messages from the entry queue.
        /// </summary>
        /// <param name="state">The state object containing the ChorusFileLoggerProvider instance.</param>
        private static void ProcessQueue(object state)
        {
            var fileLogger = state as ChorusFileLoggerProvider<T>;
            fileLogger?.ProcessQueue();
        }
    }
}
