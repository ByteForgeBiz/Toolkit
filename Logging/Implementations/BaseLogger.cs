using System;
using System.Collections.Generic;
using System.Threading;

namespace ByteForge.Toolkit.Logging
{
    /*
     *  ___               _                           
     * | _ ) __ _ ___ ___| |   ___  __ _ __ _ ___ _ _ 
     * | _ \/ _` (_-</ -_) |__/ _ \/ _` / _` / -_) '_|
     * |___/\__,_/__/\___|____\___/\__, \__, \___|_|  
     *                             |___/|___/         
     */
    /// <summary>
    /// Abstract base class for logging functionality.
    /// </summary>
    public abstract class BaseLogger : ILogger
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BaseLogger"/> class with the specified name.
        /// </summary>
        /// <param name="name">The name of the logger.</param>
        protected BaseLogger(string name)
        {
            Name = name;
        }

        /// <summary>
        /// Gets or sets the name of the logger.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the minimum log level for this logger.
        /// </summary>
        public LogLevel MinLogLevel { get; set; } = LogLevel.Info;

        /// <summary>
        /// Writes the log entry to the log destination.
        /// </summary>
        /// <param name="entry">The log entry to write.</param>
        protected internal abstract void RecordLogEntry(LogEntry entry);

        // Logging Methods

        /// <summary>
        /// Logs a message with the specified log level.
        /// </summary>
        /// <param name="level">The log level.</param>
        /// <param name="message">The message to log.</param>
        /// <param name="ex">An optional exception to log.</param>
        public void Log(LogLevel level, string message, Exception ex = null)
        {
            if (level < MinLogLevel) return;

            var entry = new LogEntry
            {
                Timestamp = DateTime.Now,
                Level = level,
                Message = message,
                Source = level.ToString(),
                Exception = ex,
                Properties = new Dictionary<string, object>
                    {
                        { "LoggerName", Name },
                        { "ThreadId", Thread.CurrentThread.ManagedThreadId }
                    }
            };

            RecordLogEntry(entry);
        }

        /// <summary>
        /// Logs a trace message.
        /// </summary>
        /// <param name="message">The message to log.</param>
        public void LogTrace(string message) => Log(LogLevel.Trace, message, null);

        /// <summary>
        /// Logs a debug message.
        /// </summary>
        /// <param name="message">The message to log.</param>
        public void LogDebug(string message) => Log(LogLevel.Debug, message, null);

        /// <summary>
        /// Logs a verbose message.
        /// </summary>
        /// <param name="message">The message to log.</param>
        public void LogVerbose(string message) => Log(LogLevel.Verbose, message, null);

        /// <summary>
        /// Logs an informational message.
        /// </summary>
        /// <param name="message">The message to log.</param>
        public void LogInfo(string message) => Log(LogLevel.Info, message, null);

        /// <summary>
        /// Logs a notice message.
        /// </summary>
        /// <param name="message">The message to log.</param>
        public void LogNotice(string message) => Log(LogLevel.Notice, message, null);

        /// <summary>
        /// Logs a warning message.
        /// </summary>
        /// <param name="message">The message to log.</param>
        public void LogWarning(string message) => Log(LogLevel.Warning, message, null);

        /// <summary>
        /// Logs an error message.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="ex">An optional exception to log.</param>
        public void LogError(string message, Exception ex = null) => Log(LogLevel.Error, message, ex);

        /// <summary>
        /// Logs a critical message.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="ex">An optional exception to log.</param>
        public void LogCritical(string message, Exception ex = null) => Log(LogLevel.Critical, message, ex);

        /// <summary>
        /// Logs a fatal message.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="ex">An optional exception to log.</param>
        public void LogFatal(string message, Exception ex = null) => Log(LogLevel.Fatal, message, ex);

        // Helper Methods

        // Add any additional helper methods here in the future.
    }
}