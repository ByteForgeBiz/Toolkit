using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ByteForge.Toolkit.Logging
{
    /*
     *  ___ _ _     _                           
     * | __(_) |___| |   ___  __ _ __ _ ___ _ _ 
     * | _|| | / -_) |__/ _ \/ _` / _` / -_) '_|
     * |_| |_|_\___|____\___/\__, \__, \___|_|  
     *                       |___/|___/         
     */
    /// <summary>
    /// Enhanced file logger that logs messages to a specified file with advanced features.
    /// </summary>
    public class FileLogger : BaseLogger, IDisposable
    {
        private readonly string _baseFilePath;
        private readonly FileLoggerOptions _options;
        private static readonly object _lock = new object();
        private DateTime _currentFileDate;
        private string _currentFilePath;
        private int _currentFileIndex;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly BlockingCollection<LogEntry> _messageQueue;
        private readonly Task _processQueueTask;
        private bool _disposed;

        private static readonly FileLoggerOptions options = Configuration.GetSection<FileLoggerOptions>("FileLogger");

        /// <summary>
        /// Initializes a new instance of the <see cref="FileLogger"/> class.
        /// </summary>
        /// <param name="filePath">The path of the file to log messages to.</param>
        public FileLogger(string filePath) : this(filePath, options) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="FileLogger"/> class with specified options.
        /// </summary>
        /// <param name="filePath">The base path of the file to log messages to.</param>
        /// <param name="options">Configuration options for the logger.</param>
        public FileLogger(string filePath, FileLoggerOptions options)
            : base(Path.GetFileNameWithoutExtension(filePath))
        {
            if (string.IsNullOrEmpty(filePath))
            {
                var asm = Assembly.GetEntryAssembly() ?? Assembly.GetCallingAssembly() ?? Assembly.GetExecutingAssembly();
                filePath = Path.Combine(Path.GetDirectoryName(asm.Location), $"{asm.GetName().Name}.log");
            }
            else
                filePath = Environment.ExpandEnvironmentVariables(filePath);

            _baseFilePath = filePath;
            _options = options ?? new FileLoggerOptions();
            _currentFileDate = DateTime.Now.Date;
            _currentFileIndex = 1;

            if (_options.UseAsyncLogging)
            {
                _messageQueue = new BlockingCollection<LogEntry>(_options.AsyncQueueSize);
                _cancellationTokenSource = new CancellationTokenSource();
                _processQueueTask = Task.Run((Action)ProcessMessageQueue);
            }

            UpdateCurrentFilePath();
            EnsureAccess(_currentFilePath);
            CleanupOldFiles();
        }

        /// <summary>
        /// Updates the current file path based on the configuration settings.
        /// </summary>
        private void UpdateCurrentFilePath()
        {
            var baseNameWithoutExt = Path.Combine(
                Path.GetDirectoryName(_baseFilePath),
                Path.GetFileNameWithoutExtension(_baseFilePath)
            );
            var extension = Path.GetExtension(_baseFilePath);

            if (!_options.UseDaily)
            {
                _currentFilePath = _baseFilePath;
                return;
            }

            var dateStr = DateTime.Now.ToString(_options.DateFormat);
            _currentFilePath = _options.MaxFileSizeMB > 0
                ? $"{baseNameWithoutExt}_{dateStr}_{_currentFileIndex}{extension}"
                : $"{baseNameWithoutExt}_{dateStr}{extension}";
        }

        /// <summary>
        /// Ensures that the specified file path is accessible.
        /// </summary>
        private void EnsureAccess(string filePath)
        {
            var dir = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            if (!File.Exists(filePath))
                File.Create(filePath).Close();

            using (var fs = File.Open(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.Read)) { return; }
        }

        /// <summary>
        /// Clears the contents of the current log file.
        /// </summary>
        /// <remarks>
        /// This method locks the file to ensure thread safety and writes an empty string to the file, effectively clearing its contents.
        /// </remarks>
        public void Clear()
        {
            lock (_lock)
            {
                File.WriteAllText(_currentFilePath, string.Empty);
            }
        }

        /// <summary>
        /// Checks if the current log file needs to be rolled over due to size limits.
        /// </summary>
        private bool NeedsRollover()
        {
            if (_options.MaxFileSizeMB <= 0) return false;

            var fileInfo = new FileInfo(_currentFilePath);
            return fileInfo.Length > _options.MaxFileSizeMB * 1024 * 1024;
        }

        /// <summary>
        /// Cleans up old log files based on retention policy.
        /// </summary>
        private void CleanupOldFiles()
        {
            if (_options.RetentionDays <= 0) return;

            var directory = Path.GetDirectoryName(_baseFilePath);
            var filePattern = $"{Path.GetFileNameWithoutExtension(_baseFilePath)}_*{Path.GetExtension(_baseFilePath)}";
            var cutoffDate = DateTime.Now.AddDays(-_options.RetentionDays);

            var files = Directory.GetFiles(directory, filePattern)
                .Select(f => new FileInfo(f))
                .Where(f => f.LastWriteTime < cutoffDate);

            foreach (var file in files)
            {
                try
                {
                    file.Delete();
                }
                catch (Exception)
                {
                    // Log deletion failure or handle appropriately
                }
            }
        }

        /// <summary>
        /// Processes the message queue for async logging.
        /// </summary>
        private void ProcessMessageQueue()
        {
            foreach (var entry in _messageQueue.GetConsumingEnumerable(_cancellationTokenSource.Token))
            {
                try
                {
                    WriteLogEntry(entry);
                }
                catch (Exception)
                {
                    // Handle logging errors appropriately
                }
            }
        }

        /// <summary>
        /// Writes a log entry to the current file.
        /// </summary>
        private void WriteLogEntry(LogEntry entry)
        {
            var ts = $"{entry.Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{entry.Level}] {(entry.CorrelationId == null ? "" : entry.CorrelationId.ToString() + " ")} - {entry.Source} - ";
            var indent = Environment.NewLine + new string(' ', ts.Length);
            var sb = new StringBuilder($"{ts}{string.Join(indent, entry.Message.Split(Utils.arrCRLF, StringSplitOptions.RemoveEmptyEntries))}");
            var ex = entry.Exception;

            while (ex != null)
            {
                sb.Append(indent + $"Exception: {{{ex.GetType()}}} {ex.Message}");
                if (!string.IsNullOrEmpty(ex.StackTrace))
                    sb.Append(indent + $"Stack Trace: {string.Join(indent + "             ", ex.StackTrace.Split(Utils.arrCRLF, StringSplitOptions.RemoveEmptyEntries))}");

                if (ex.InnerException != null)
                    indent += "  ";

                ex = ex.InnerException;
            }

            var logMessage = sb.ToString();

            lock (_lock)
            {
                // Check for daily rotation
                if (_options.UseDaily && DateTime.Now.Date != _currentFileDate)
                {
                    _currentFileDate = DateTime.Now.Date;
                    _currentFileIndex = 1;
                    UpdateCurrentFilePath();
                    EnsureAccess(_currentFilePath);
                    CleanupOldFiles();
                }

                // Check for size-based rotation
                if (NeedsRollover())
                {
                    _currentFileIndex++;
                    UpdateCurrentFilePath();
                    EnsureAccess(_currentFilePath);
                }

                File.AppendAllText(_currentFilePath, logMessage + Environment.NewLine);
            }
        }

        /// <summary>
        /// Records a log entry.
        /// </summary>
        protected internal override void RecordLogEntry(LogEntry entry)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(FileLogger));

            if (_options.UseAsyncLogging)
            {
                if (!_messageQueue.TryAdd(entry))
                {
                    // Handle queue full scenario
                    // Could implement fallback logging here
                }
            }
            else
            {
                WriteLogEntry(entry);
            }
        }

        /// <summary>
        /// Disposes the logger resources.
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;

            if (_options.UseAsyncLogging)
            {
                _cancellationTokenSource.Cancel();
                _messageQueue.CompleteAdding();
                try
                {
                    _processQueueTask.Wait(TimeSpan.FromSeconds(5));
                }
                catch (Exception)
                {
                    // Handle shutdown errors
                }
                _messageQueue.Dispose();
                _cancellationTokenSource.Dispose();
            }

            _disposed = true;
        }
    }
}