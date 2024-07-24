using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;

namespace Chorus.WindowsReader.Common.Logger
{
    internal class ChorusFileWriter<T>
    {
        private ChorusFileLoggerProvider<T> _fileLogPrv;
        private string _logFileName;
        private Stream _logFileStream;
        private TextWriter _logFileWriter;
        private readonly object _lock = new object();
        private string _lastBaseLogFileName = null;

        /// <summary>
        /// This is the ChorusFileWriter
        /// Initializes a new instance of ChorusFileWriter for logging purposes,
        /// using the provided ChorusFileLoggerProvider instance to manage log files.
        /// Opens the log file asynchronously if appending is enabled.
        /// </summary>
        /// <param name="fileLogPrv">The ChorusFileLoggerProvider instance for file logging.</param>
        public ChorusFileWriter(ChorusFileLoggerProvider<T> fileLogPrv)
        {
            _fileLogPrv = fileLogPrv;
            _logFileName = fileLogPrv?.LogFileName;

            DetermineLastFileLogName();

            if (_fileLogPrv != null) _ = OpenFileAsync(_fileLogPrv.Append);
        }
        /// <summary>
        /// This is the GetBaseLogFileName
        /// Retrieves the base log file name from the ChorusFileLoggerProvider instance,
        /// optionally formatting it using a specified function.
        /// </summary>
        /// <returns>The base log file name, or null if not available.</returns>
        private string GetBaseLogFileName()
        {
            var fName = _fileLogPrv?.LogFileName;

            if (fName == null) return null;

            if (_fileLogPrv?.FormatLogFileName != null)
                fName = _fileLogPrv.FormatLogFileName(fName);

            return fName;
        }
        /// <summary>
        /// This is the GetLocalStatePath
        /// Retrieves the local storage path for application data.
        /// Sets the global helper's logger files path to the local state path.
        /// </summary>
        /// <returns>The StorageFolder object representing the local state path.</returns>
        private StorageFolder GetLocalStatePath()
        {
            var localStatePath = ApplicationData.Current.LocalFolder;
            GlobalHelper.LoggerFilesPath = localStatePath.Path;
            return localStatePath;
        }

        /// <summary>
        /// This is the DetermineLastFileLogName
        /// Determines the latest log file name based on file size limit and local storage.
        /// </summary>
        /// <returns>An asynchronous Task representing the operation.</returns>
        private async Task DetermineLastFileLogName()
        {
            var baseLogFileName = GetBaseLogFileName();
            _lastBaseLogFileName = baseLogFileName;

            if (_fileLogPrv?.FileSizeLimitBytes > 0)
            {
                var folder = GetLocalStatePath();
                var logFiles = await folder.GetFilesAsync();
                if (logFiles.Count > 0)
                {
                    var fileInfos = await Task.WhenAll(logFiles.Select(async file =>
                    {
                        var properties = await file.GetBasicPropertiesAsync();
                        return new
                        {
                            File = file,
                            Name = file.Name,
                            LastWriteTime = properties.DateModified
                        };
                    }));

                    var lastFileInfo = fileInfos
                        .OrderByDescending(fInfo => fInfo.Name)
                        .ThenByDescending(fInfo => fInfo.LastWriteTime)
                        .First();

                    _logFileName = lastFileInfo.File.Path;
                }
                else
                {
                    // No files yet, use default name
                    _logFileName = baseLogFileName;
                }
            }
            else
            {
                _logFileName = baseLogFileName;
            }
        }
        /// <summary>
        /// This is the OpenFileAsync
        /// Opens or creates a log file asynchronously in the local storage folder.
        /// </summary>
        /// <param name="append">True to append to an existing file, false to replace existing.</param>
        /// <returns>An asynchronous Task representing the operation.</returns>
        private async Task OpenFileAsync(bool append)
        {
            try
            {
                var localFolder = GetLocalStatePath();
                var fileName = Path.GetFileName(_logFileName); // Get just the file name without path
                var option = append ? CreationCollisionOption.OpenIfExists : CreationCollisionOption.ReplaceExisting;
                var logFile = await localFolder.CreateFileAsync(fileName, option);

                _logFileStream = await logFile.OpenStreamForWriteAsync();

                if (append)
                {
                    _logFileStream.Seek(0, SeekOrigin.End);
                }
                else
                {
                    _logFileStream.SetLength(0);
                }
                _logFileWriter = new StreamWriter(_logFileStream);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error opening log file: {ex.Message}");
                throw;
            }
        }
        /// <summary>
        /// This is the GetNextFileLogNameAsync
        /// Generates the next available log file name asynchronously based on size and rolling file settings.
        /// </summary>
        /// <returns>An asynchronous Task that returns the next log file name.</returns>
        private async Task<string> GetNextFileLogNameAsync()
        {
            string baseLogFileName = GetBaseLogFileName();

            var localFolder = ApplicationData.Current.LocalFolder;
            StorageFile baseLogFile = await localFolder.TryGetItemAsync(Path.GetFileName(_logFileName)) as StorageFile;

            if (baseLogFile == null || _fileLogPrv?.FileSizeLimitBytes == null)
                return baseLogFileName;

            var baseFileProperties = await baseLogFile.GetBasicPropertiesAsync();
            long baseFileSize = MapUlongToLong(baseFileProperties.Size);

            if (baseFileSize < _fileLogPrv.FileSizeLimitBytes)
                return baseLogFileName;

            // Determine the next available index for the log file
            int nextFileIndex = 1;
            while (true)
            {
                string nextFileName = $"{Path.GetFileNameWithoutExtension(baseLogFileName)} {nextFileIndex}{Path.GetExtension(baseLogFileName)}";
                StorageFile nextLogFile = await localFolder.TryGetItemAsync(nextFileName) as StorageFile;

                if (nextLogFile == null)
                    return nextFileName;

                nextFileIndex++;
                if (_fileLogPrv.MaxRollingFiles > 0 && nextFileIndex > _fileLogPrv.MaxRollingFiles)
                    nextFileIndex = 1; // Restart index if max rolling files is exceeded
            }
        }
        /// <summary>
        ///  This is the MapUlongToLong
        /// Converts an unsigned long (ulong) value to a signed long (long) value.
        /// </summary>
        /// <param name="ulongValue">The unsigned long value to convert.</param>
        /// <returns>The equivalent signed long value.</returns>
        public static long MapUlongToLong(ulong ulongValue)
        {
            return unchecked((long)ulongValue);
        }
        /// <summary>
        ///  This is the CheckForNewLogFile
        /// Checks if conditions require opening a new log file, and handles file creation accordingly.
        /// </summary>

        private async void CheckForNewLogFile()
        {
            bool openNewFile = false;
            if (isMaxFileSizeThresholdReached() || isBaseFileNameChanged())
                openNewFile = true;

            if (openNewFile)
            {
                if (_logFileWriter != null)
                {
                    _logFileWriter.Flush();
                    _logFileWriter.Dispose();
                    _logFileWriter = null;
                }

                _logFileName = await GetNextFileLogNameAsync();
                await OpenFileAsync(false); // Always create a new file when a new log is required
            }

            bool isMaxFileSizeThresholdReached()
            {
                return _fileLogPrv?.FileSizeLimitBytes > 0 && _logFileStream?.Length > _fileLogPrv.FileSizeLimitBytes;
            }
            bool isBaseFileNameChanged()
            {
                if (_fileLogPrv?.FormatLogFileName != null)
                {
                    var baseLogFileName = GetBaseLogFileName();
                    if (baseLogFileName != _lastBaseLogFileName)
                    {
                        _lastBaseLogFileName = baseLogFileName;
                        return true;
                    }
                    return false;
                }
                return false;
            }
        }
        /// <summary>
        /// This is the WriteMessage
        /// Writes a message to the current log file, optionally flushing the writer.
        /// </summary>
        internal void WriteMessage(string message, bool flush)
        {
            lock (_lock)
            {
                if (_logFileWriter == null) return;
                CheckForNewLogFile();
                if (_logFileWriter != null)
                {
                    _logFileWriter.WriteLine(message);
                    if (flush) _logFileWriter.Flush();
                }
            }
        }
        /// <summary>
        /// This is the Close
        /// Closes the current log file writer.
        /// </summary>
        public void Close()
        {
            lock (_lock)
            {
                _logFileWriter?.Dispose();
                _logFileWriter = null;
            }
        }
    }
}
