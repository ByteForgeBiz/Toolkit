using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ByteForge.Toolkit.Logging;
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
    private static readonly object _lock = new object();
    private DateTime _currentFileDate;
    private int _currentFileIndex;
    private readonly CancellationTokenSource? _cancellationTokenSource;
    private readonly BlockingCollection<LogEntry>? _messageQueue;
    private readonly Task? _processQueueTask;
    private bool _disposed;

    /// <summary>
    /// Gets the file path currently in use by the application.
    /// </summary>
    public string CurrentFilePath
    {
        get
        {
            if (string.IsNullOrEmpty(currentFilePath))
                UpdateCurrentFilePath();
            return currentFilePath ?? string.Empty;
        }
        protected set => currentFilePath = value ?? throw new ArgumentNullException(nameof(value));
    }
    private string? currentFilePath;

    /// <summary>
    /// Initializes a new instance of the <see cref="FileLogger"/> class.
    /// </summary>
    /// <param name="filePath">The path of the file to log messages to.</param>
    public FileLogger(string? filePath) : this(filePath, null) { }

    /// <summary>
    /// Gets the configuration settings for the file logger.
    /// </summary>
    public FileLoggerOptions Settings { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="FileLogger"/> class with specified options.
    /// </summary>
    /// <param name="filePath">The base path of the file to log messages to.</param>
    /// <param name="options">Configuration options for the logger.</param>
    /// <param name="delayInitialization">If true, delays file initialization (useful for derived classes).</param>
    protected FileLogger(string? filePath, FileLoggerOptions? options, bool delayInitialization = false) : base(Path.GetFileNameWithoutExtension(filePath ?? ""))
    {
        if (string.IsNullOrEmpty(filePath))
        {
            var asm = Assembly.GetEntryAssembly() ?? Assembly.GetCallingAssembly() ?? Assembly.GetExecutingAssembly();
            filePath = Path.Combine(Path.GetDirectoryName(asm.Location)!, $"{asm.GetName().Name}.log");
        }
        else
            filePath = Environment.ExpandEnvironmentVariables(filePath);

        Settings = options ?? (Configuration.IsInitialized ? Configuration.GetSection<FileLoggerOptions>("FileLogger") : null) ?? new FileLoggerOptions();
        _baseFilePath = filePath;
        _currentFileDate = DateTime.Now.Date;
        _currentFileIndex = 1;

        if (Settings.UseAsyncLogging)
        {
            _messageQueue = new BlockingCollection<LogEntry>(Settings.AsyncQueueSize);
            _cancellationTokenSource = new CancellationTokenSource();
            _processQueueTask = Task.Run((Action)ProcessMessageQueue);
        }

        if (delayInitialization) return;

        InitializeFileLogger(Settings);
    }

    /// <summary>
    /// Initializes the file logger with the specified configuration options.
    /// </summary>
    /// <param name="options">The configuration options for the file logger. This parameter cannot be <see langword="null"/>.</param>
    /// <remarks>
    /// This method configures the file logger based on the provided <see cref="FileLoggerOptions"/>. 
    /// It updates the logger's settings, including file size limits, retention policies, and naming patterns, 
    /// and performs any necessary cleanup of old log files.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="options"/> is <see langword="null"/>.</exception>
    protected void InitializeFileLogger(FileLoggerOptions options)
    {
        if (options == null) throw new ArgumentNullException(nameof(options));
        Settings.MaxFileSizeMB = options.MaxFileSizeMB;
        Settings.RetentionDays = options.RetentionDays;
        Settings.FileNamingPattern = options.FileNamingPattern;
        Settings.UseDaily = options.UseDaily;
        UpdateCurrentFilePath();
        CleanupOldFiles();
    }

    /// <summary>
    /// Updates the current file path based on the configuration settings.
    /// </summary>
    protected void UpdateCurrentFilePath()
    {
        // Use custom provider if available
        if (Settings.CustomFileNameProvider != null)
        {
            CurrentFilePath = Settings.CustomFileNameProvider(_baseFilePath, Settings);
            return;
        }

        // Use pattern-based naming
        CurrentFilePath = GenerateFileName(_baseFilePath, Settings);
        EnsureAccess(CurrentFilePath);
    }

    /// <summary>
    /// Generates a file name using the specified pattern and base file path.
    /// </summary>
    /// <param name="baseFilePath">The base file path.</param>
    /// <param name="options">The file logger options containing the naming pattern.</param>
    /// <returns>The generated file path.</returns>
    protected virtual string GenerateFileName(string baseFilePath, FileLoggerOptions options)
    {
        var directory = Path.GetDirectoryName(baseFilePath);
        var baseNameWithoutExt = Path.GetFileNameWithoutExtension(baseFilePath);
        var extension = Path.GetExtension(baseFilePath);

        var pattern = options.FileNamingPattern ?? "{basename}";
        if (options.UseDaily && !pattern.ToLowerInvariant().Contains("{date"))
            pattern += "-{date:yyyy-MM-dd}";

        // Replace basic placeholders
        var fileName = pattern
            .Replace("{basename}", baseNameWithoutExt)
            .Replace("{timestamp}", DateTime.Now.ToString("yyyyMMdd-HHmmss"))
            .Replace("{index}", _currentFileIndex.ToString())
            .Replace("{pid}", System.Diagnostics.Process.GetCurrentProcess().Id.ToString())
            .Replace("{guid}", Guid.NewGuid().ToString("N").Substring(0, 8));

        // Handle date format placeholder with optional format specifier
        fileName = ProcessDatePlaceholder(fileName);

        // Handle additional placeholders that might be added by derived classes
        fileName = ProcessAdditionalPlaceholders(fileName, options);

        return Path.Combine(directory, fileName + extension);
    }

    /// <summary>
    /// Processes date placeholders in the file name pattern.
    /// Supports {date:format} syntax where format is optional (defaults to yyyy-MM-dd).
    /// </summary>
    /// <param name="fileName">The file name to process.</param>
    /// <returns>The file name with date placeholders processed.</returns>
    private string ProcessDatePlaceholder(string fileName)
    {
        // Handle {date:format} pattern
        var datePattern = System.Text.RegularExpressions.Regex.Match(fileName, @"\{date(?::([^}]+))?\}");
        if (datePattern.Success)
        {
            var format = datePattern.Groups[1].Success ? datePattern.Groups[1].Value : "yyyy-MM-dd";
            var dateValue = DateTime.Now.ToString(format);
            fileName = fileName.Replace(datePattern.Value, dateValue);
        }

        return fileName;
    }

    /// <summary>
    /// Processes additional placeholders that might be specific to derived classes.
    /// Override this method in derived classes to handle custom placeholders.
    /// </summary>
    /// <param name="fileName">The file name with basic placeholders already processed.</param>
    /// <param name="options">The file logger options.</param>
    /// <returns>The file name with additional placeholders processed.</returns>
    protected virtual string ProcessAdditionalPlaceholders(string fileName, FileLoggerOptions options)
    {
        return fileName;
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

        var count = 0;
        // Retry a few times if the file is locked
        while (count < 5)
        {
            try
            {
                using var fs = File.Open(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.Read);
                return;
            }
            catch (IOException)
            {
                count++;
                Thread.Sleep(100);
            }
        }
        return;
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
            File.WriteAllText(CurrentFilePath, string.Empty);
        }
    }

    /// <summary>
    /// Checks if the current log file needs to be rolled over due to size limits.
    /// </summary>
    private bool NeedsRollover()
    {
        if (Settings.MaxFileSizeMB <= 0) return false;

        var fileInfo = new FileInfo(CurrentFilePath);
        return fileInfo.Length > Settings.MaxFileSizeMB * 1024 * 1024;
    }

    /// <summary>
    /// Cleans up old log files based on retention policy.
    /// </summary>
    private void CleanupOldFiles()
    {
        if (Settings.RetentionDays <= 0) return;

        try
        {
            var directory = Path.GetDirectoryName(_baseFilePath);
            var filePattern = $"{Path.GetFileNameWithoutExtension(_baseFilePath)}*{Path.GetExtension(_baseFilePath)}";
            var cutoffDate = DateTime.Now.AddDays(-Settings.RetentionDays);

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
        catch (Exception)
        {
            // Don't let cleanup failures affect logging
        }
    }

    /// <summary>
    /// Processes the message queue for async logging.
    /// </summary>
    private void ProcessMessageQueue()
    {
        foreach (var entry in _messageQueue!.GetConsumingEnumerable(_cancellationTokenSource!.Token))
            WriteLogEntry(entry);
    }

    /// <summary>
    /// Writes a log entry to the current file.
    /// </summary>
    private void WriteLogEntry(LogEntry entry)
    {
        var ts = $"{(entry.CorrelationId ?? "")} - {entry.Timestamp:yyyy.MM.dd HH:mm:ss.fff} [{entry.Level,-9}] - ";
        var indent = Environment.NewLine + new string(' ', ts.Length);
        var sb = new StringBuilder($"{ts}{string.Join(indent, entry.Message?.Split(Utils.arrCRLF, StringSplitOptions.RemoveEmptyEntries) ?? [])}");
        var ex = entry.Exception;

        while (ex != null)
        {
            sb.Append(indent + $"Exception: {{{ex.GetType()}}} {ex.Message}");
            if (!string.IsNullOrEmpty(ex.StackTrace))
                sb.Append(indent + $"Stack Trace: {string.Join(indent + "             ", ex.StackTrace.Split(Utils.arrCRLF, StringSplitOptions.RemoveEmptyEntries))}");

            if (ex.Data?.Count > 0)
            {
                var keys = ex.Data.Keys.OfType<string>().ToArray();
                sb.Append(indent + $"Data: {keys[0]}: {ex.Data[keys[0]]}");
                foreach (var key in keys.Skip(1))
                    sb.Append(indent + $"      {key}: {ex.Data[key]}");
            }

            if (ex.InnerException != null)
                indent += "  ";

            ex = ex.InnerException;
        }

        var logMessage = sb.ToString();

        lock (_lock)
        {
            // Check for daily rotation
            if (Settings.UseDaily && DateTime.Now.Date != _currentFileDate)
            {
                _currentFileDate = DateTime.Now.Date;
                _currentFileIndex = 1;
                UpdateCurrentFilePath();
                EnsureAccess(CurrentFilePath);
                CleanupOldFiles();
            }

            // Check for size-based rotation
            if (NeedsRollover())
            {
                _currentFileIndex++;
                UpdateCurrentFilePath();
                EnsureAccess(CurrentFilePath);
            }

            Debug.Assert(!string.IsNullOrEmpty(CurrentFilePath), "CurrentFilePath should not be null or empty here.");

            File.AppendAllText(CurrentFilePath, logMessage + Environment.NewLine, Settings.FileEncoding);
        }
    }

    /// <summary>
    /// Records a log entry.
    /// </summary>
    protected internal override void RecordLogEntry(LogEntry entry)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(FileLogger));

        if (Settings.UseAsyncLogging)
        {
            if (!_messageQueue!.TryAdd(entry))
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
    /// Releases the resources used by the logger.
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;

        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Releases the unmanaged resources used by the logger and optionally releases the managed resources.
    /// </summary>
    /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;

        if (disposing)
        {
            if (Settings.UseAsyncLogging)
            {
                _cancellationTokenSource!.Cancel();
                _messageQueue!.CompleteAdding();
                try
                {
                    _processQueueTask!.Wait(TimeSpan.FromSeconds(5));
                }
                catch (Exception)
                {
                    // Handle shutdown errors
                }
                _messageQueue.Dispose();
                _cancellationTokenSource.Dispose();
            }
        }

        _disposed = true;
    }
}
